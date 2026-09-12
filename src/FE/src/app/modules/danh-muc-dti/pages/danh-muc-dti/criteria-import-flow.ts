import { Injectable, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { Observable, Subject, catchError, interval, map, of, switchMap, takeWhile } from 'rxjs';
import { IHttpErrorWithApiResult } from '../../../../core/http/api-result.model';
import { ApiErrorMessageService } from '../../../../core/i18n/api-error-message.service';
import { ToastService } from '../../../../core/toast/toast.service';
import { IImportResult, IWritePeriod } from '../../models/danh-muc-dti.model';
import { DanhMucDtiImportService } from '../../services/danh-muc-dti-import.service';

/** Nhịp poll. Đủ thưa để không dội server, đủ dày để một file 62 dòng thấy kết quả gần như tức thì. */
const POLL_INTERVAL_MS = 1500;

/**
 * Kết quả một nhịp của dòng chảy import — bốn lối ra, rời nhau, không lối nào suy ra từ lối nào.
 *
 * Dùng union thay vì một object nhiều cờ (`busy`/`failed`/`notFound`/`result`): bốn cờ boolean cho
 * 16 tổ hợp mà chỉ 4 hợp lệ, và chính 12 tổ hợp vô nghĩa kia là chỗ một trạng thái treo lọt qua.
 */
type ImportOutcome =
  | { readonly Kind: 'running' }
  | { readonly Kind: 'succeeded'; readonly Result: IImportResult }
  /** `Code` = `errorCode` của job, `null` khi BE không cấp mã nào. */
  | { readonly Kind: 'failed'; readonly Code: string | null }
  /** Lỗi HTTP — bước 1 (chưa có `jobId`) hoặc một nhịp poll hỏng. */
  | { readonly Kind: 'error'; readonly Message: string; readonly Polling: boolean };

/**
 * LUỒNG IMPORT của màn Danh mục DTI (DM-7): trạng thái hai hộp thoại, lời gọi bước 1, và vòng poll.
 *
 * Tách khỏi `DanhMucDtiPage` theo đúng ranh giới nghiệp vụ — cùng khuôn và cùng lý do với
 * `platform/quan-tri-nguoi-dung/pages/quan-tri-nguoi-dung/user-form-flow.ts`: lớp này không biết
 * trang đang lọc gì, đang ở trang mấy, hay lưới đang ở trạng thái nào. Nó nhận một `File` + một kỳ
 * đích, và một callback "nạp xong thì tải lại lưới".
 *
 * ## Vòng poll phải chết cùng TRANG — đây là ràng buộc, không phải dọn dẹp cho gọn
 *
 * 🛑 `@Injectable()` **không** `providedIn: 'root'`: khai trong `providers` của trang nên
 * `takeUntilDestroyed()` trong constructor gắn vào đúng vòng đời trang. Lên `root` thì vòng poll
 * sống tới khi tab đóng — khác gọi API thường (một lần rồi tự hoàn thành), poll chạy **vô hạn** cho
 * tới khi job xong, nên rời trang giữa chừng mà không huỷ là một request nền lặp mãi mãi.
 *
 * ## Ba lối kết thúc, ba cách xử lý khác nhau
 *
 * | Tín hiệu | Nghĩa | Làm gì |
 * | --- | --- | --- |
 * | `Succeeded` | job xong, kể cả khi có lỗi DÒNG | đóng V11, mở V12 với `result` |
 * | `Failed` | lỗi **hạ tầng** (file hỏng, job crash) — `result` vắng mặt | toast, **không** mở V12 |
 * | `404 IMPORT.JOB_NOT_FOUND` | sai id, hoặc job đã bị dọn theo retention | **DỪNG poll**, giữ V11 mở để nạp lại |
 *
 * Ranh giới `Failed` ↔ lỗi dòng thuộc **hợp đồng**, không phải lựa chọn hiển thị: lỗi từng dòng đi
 * vào `result.errors` và job vẫn `Succeeded`.
 */
@Injectable()
export class CriteriaImportFlow {
  private readonly service = inject(DanhMucDtiImportService);
  private readonly toast = inject(ToastService);
  private readonly translate = inject(TranslateService);
  private readonly errorMessages = inject(ApiErrorMessageService);

  readonly dialogOpen = signal(false);
  readonly resultOpen = signal(false);

  /** Bước 1 đang bay **hoặc** job đang `Pending`/`Running` — một trạng thái với người dùng. */
  readonly busy = signal(false);

  /** Câu lỗi **cấp request** của bước 1, hiện trong V11. Lỗi dòng thì không đi qua đây. */
  readonly errorMessage = signal<string | null>(null);

  readonly result = signal<IImportResult | null>(null);

  private readonly requests = new Subject<{ File: File; Period: IWritePeriod }>();

  /** Gọi sau khi một lượt nạp kết thúc thành công — trang dùng để tải lại lưới. */
  private onImported: () => void = () => undefined;

  constructor() {
    this.requests
      .pipe(
        // `switchMap`: bấm nạp lượt thứ hai thì vòng poll của lượt trước bị huỷ. Thiếu nó, hai
        // vòng poll cùng chạy và vòng CŨ về sau sẽ mở hộp thoại kết quả của lượt đã bị bỏ.
        switchMap((request) => this.run(request.File, request.Period)),
        takeUntilDestroyed(),
      )
      .subscribe((outcome) => this.apply(outcome));
  }

  open(): void {
    this.errorMessage.set(null);
    this.busy.set(false);
    this.result.set(null);
    this.dialogOpen.set(true);
  }

  close(): void {
    this.dialogOpen.set(false);
  }

  /**
   * Đóng V12 ⇒ **tải lại lưới**. Không thay dòng tại chỗ như đường sửa: import đụng tới bao nhiêu
   * dòng thì chỉ server biết, và nó còn **tạo mới** chỉ tiêu (`criteriaCreatedCount`) — tức số
   * dòng và cả phân trang đều đổi.
   */
  closeResult(): void {
    this.resultOpen.set(false);
    this.result.set(null);
    this.onImported();
  }

  /**
   * `period` là cặp `{ Period, Year }` do `buildWritePeriod` dựng — `null` nghĩa là ô lọc đang
   * chọn một **tháng**, và lúc đó không có lời ghi nào để gửi (Q37).
   */
  submit(file: File, period: IWritePeriod | null, onImported: () => void): void {
    if (!period || this.busy()) return;
    this.onImported = onImported;
    this.errorMessage.set(null);
    this.busy.set(true);
    this.requests.next({ File: file, Period: period });
  }

  /**
   * Bước 1 rồi bước 2. `takeWhile(..., true)` — tham số `inclusive` **bắt buộc** `true`: thiếu nó
   * thì đúng nhịp mang kết quả cuối cùng bị cắt trước khi tới `subscribe`, và người dùng thấy vòng
   * quay dừng lại mà không có hộp thoại kết quả nào.
   */
  private run(file: File, period: IWritePeriod): Observable<ImportOutcome> {
    return this.service.startImport(file, period).pipe(
      switchMap((jobId) =>
        interval(POLL_INTERVAL_MS).pipe(
          switchMap(() => this.service.getJobStatus(jobId)),
          takeWhile((status) => status.Status === 'Pending' || status.Status === 'Running', true),
          map((status): ImportOutcome => {
            if (status.Status === 'Succeeded') {
              // `Succeeded` mà `Result` vắng mặt là hợp đồng gãy. Coi như hạ tầng hỏng thay vì mở
              // một hộp thoại kết quả với bốn số 0 — câu "Tổng 0 dòng, 0 lỗi" nói dối trắng trợn
              // về một file vừa được nạp.
              return status.Result
                ? { Kind: 'succeeded', Result: status.Result }
                : { Kind: 'failed', Code: null };
            }
            if (status.Status === 'Failed') return { Kind: 'failed', Code: status.ErrorCode };
            return { Kind: 'running' };
          }),
          // `catchError` nằm TRONG inner observable: lỗi lọt qua `switchMap` sẽ giết luôn dòng
          // chảy, và lượt nạp kế tiếp không bao giờ chạy nữa — hỏng im lặng, không có gì trên màn
          // hình nói ra.
          catchError((err: IHttpErrorWithApiResult) => of(this.errorOutcome(err, true))),
        ),
      ),
      catchError((err: IHttpErrorWithApiResult) => of(this.errorOutcome(err, false))),
    );
  }

  /**
   * Envelope lỗi → câu người dùng đọc, theo đúng thứ tự ưu tiên của
   * `ApiErrorMessageService`: bản dịch của `businessCode` → `message` của BE → câu dự phòng.
   *
   * Không gọi `messageFor` vì nó cần mã HTTP, mà ở đây mã đó không thêm thông tin nào: bốn mã lỗi
   * bước 1 đều là `400` và đều đã có bản dịch riêng. Câu dự phòng cuối cùng là câu của **màn này**,
   * không phải câu chung theo status.
   */
  private errorOutcome(err: IHttpErrorWithApiResult, polling: boolean): ImportOutcome {
    const result = err.apiResult;
    const message =
      this.errorMessages.translateCode(result?.businessCode, result?.messageParams) ??
      result?.message ??
      (this.translate.instant('danh-muc-dti.import.startFailed') as string);
    return { Kind: 'error', Message: message, Polling: polling };
  }

  private apply(outcome: ImportOutcome): void {
    if (outcome.Kind === 'running') return;

    this.busy.set(false);

    if (outcome.Kind === 'succeeded') {
      this.dialogOpen.set(false);
      this.result.set(outcome.Result);
      this.resultOpen.set(true);
      return;
    }

    if (outcome.Kind === 'failed') {
      // `errorMessage` của job là **dev-facing** (DM-7 bước 2) — ghi log thì được, hiện cho người
      // dùng cuối thì không.
      //
      // `errorCode` (thêm 2026-09-11) là đường ra cho những ca `Failed` CÓ mã nghiệp vụ — ca đầu
      // tiên là `IMPORT.FILE_TOO_MANY_ROWS` (Q75). Vắng mã vẫn là đường đi hợp lệ (job crash thật
      // thì không có gì để dịch), nên câu chung ở lại làm đường lùi.
      this.dialogOpen.set(false);
      this.toast.error(
        this.errorMessages.translateCode(outcome.Code) ??
          (this.translate.instant('danh-muc-dti.import.jobFailed') as string),
      );
      return;
    }

    // Lỗi HTTP. `httpErrorInterceptor` đã hiện toast cho cả hai ca; V11 giữ nguyên trạng thái mở để
    // người dùng chọn lại file ngay tại chỗ — đó chính là lối ra mà câu của `IMPORT.JOB_NOT_FOUND`
    // ("Hãy nạp lại file") và của bốn mã bước 1 đều chỉ tới.
    this.errorMessage.set(outcome.Message);
    if (outcome.Polling) this.dialogOpen.set(true);
  }
}
