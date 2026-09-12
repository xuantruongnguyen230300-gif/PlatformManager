import { Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { Subject, catchError, debounceTime, distinctUntilChanged, of, startWith, switchMap } from 'rxjs';
import { IHttpErrorWithApiResult } from '../../../../core/http/api-result.model';
import { ApiErrorMessageService } from '../../../../core/i18n/api-error-message.service';
import { ToastService } from '../../../../core/toast/toast.service';
import { ICriteriaFormValue } from '../../components/criteria-form-dialog/criteria-form-dialog';
import { ICriteriaRow, IOwnerOption, IWritePeriod } from '../../models/danh-muc-dti.model';
import { CriteriaOwnerService } from '../../services/criteria-owner.service';
import { DanhMucDtiService } from '../../services/danh-muc-dti.service';

const OWNER_SEARCH_DEBOUNCE_MS = 300;

/**
 * LUỒNG HỘP THOẠI THÊM/SỬA (DM-3 + DM-4): trạng thái hộp thoại, nạp danh sách `Phụ trách`, gửi
 * lệnh ghi, và ánh xạ lỗi envelope về câu hiển thị.
 *
 * Cùng khuôn `platform/quan-tri-nguoi-dung/pages/quan-tri-nguoi-dung/user-form-flow.ts`: lớp này
 * không biết trang đang lọc gì hay lưới đang ở trạng thái nào. Nó nhận "người dùng bấm Lưu" + kỳ
 * đích, và gọi **một trong hai** callback tuỳ nhánh.
 *
 * ## Hai nhánh, hai callback — cố ý KHÔNG gộp (Q49)
 *
 * | Nhánh | Response | Callback | Vì sao |
 * | --- | --- | --- | --- |
 * | **Sửa** `PUT` | một dòng lưới đầy đủ | `onUpdated(row)` → thay dòng tại chỗ | dòng đã ở đúng vị trí của nó; refetch chỉ làm bảng nhấp nháy |
 * | **Thêm** `POST` | chỉ 6 trường danh mục, KHÔNG có trường đánh giá nào | `onCreated()` → **tải lại lưới** | vị trí dòng mới do sắp xếp + phân trang **phía server** quyết — trang đang xem có thể không chứa nó |
 *
 * FE tự chèn dòng mới là FE tự sắp, tức dựng nguồn sự thật thứ hai cho thứ tự dòng.
 *
 * 🛑 `@Injectable()` KHÔNG `providedIn: 'root'` — vòng đời trùng vòng đời trang. Lên `root` là để
 * một hộp thoại đang mở dở, kèm lỗi của lần trước, hiện lại ở lượt vào màn sau.
 */
@Injectable()
export class CriteriaFormFlow {
  private readonly service = inject(DanhMucDtiService);
  private readonly owners = inject(CriteriaOwnerService);
  private readonly toast = inject(ToastService);
  private readonly translate = inject(TranslateService);
  private readonly errorMessages = inject(ApiErrorMessageService);

  readonly open = signal(false);
  readonly saving = signal(false);
  readonly editing = signal<ICriteriaRow | null>(null);
  readonly serverError = signal<string | null>(null);

  readonly ownerOptions = signal<readonly IOwnerOption[]>([]);

  /** Tải danh sách người dùng hỏng — thường là `403` (xem `CriteriaOwnerService`). */
  readonly ownersFailed = signal(false);

  private readonly ownerTotal = signal(0);

  /**
   * Danh sách đã bị cắt theo trần ⇒ hộp thoại hiện ô tìm kiếm.
   *
   * So `TotalCount` với số option nhận được chứ không so với hằng số `OWNER_PAGE_SIZE`: hằng số
   * đó sống ở service, và một bản sao của nó ở đây sẽ lệch đúng vào lần ai đó chỉnh trần.
   */
  readonly ownersTruncated = computed(() => this.ownerTotal() > this.ownerOptions().length);

  private readonly ownerSearches = new Subject<string>();

  constructor() {
    this.ownerSearches
      .pipe(
        // `startWith('')` nạp lượt đầu ngay khi trang dựng — hộp thoại mở ra phải có sẵn danh
        // sách, không phải chờ người dùng gõ một ký tự mới thấy ai.
        startWith(''),
        debounceTime(OWNER_SEARCH_DEBOUNCE_MS),
        distinctUntilChanged(),
        // `switchMap` + `catchError` TRONG inner observable: lỗi lọt qua `switchMap` sẽ giết dòng
        // chảy, và ô `Phụ trách` chết vĩnh viễn sau một lần `403`.
        switchMap((text) => this.owners.search(text).pipe(catchError(() => of(null)))),
        takeUntilDestroyed(),
      )
      .subscribe((page) => {
        this.ownersFailed.set(page === null);
        this.ownerOptions.set(page?.Items ?? []);
        this.ownerTotal.set(page?.TotalCount ?? 0);
      });
  }

  openCreate(): void {
    this.editing.set(null);
    this.serverError.set(null);
    this.saving.set(false);
    this.open.set(true);
  }

  openEdit(row: ICriteriaRow): void {
    this.editing.set(row);
    this.serverError.set(null);
    this.saving.set(false);
    this.open.set(true);
  }

  close(): void {
    this.open.set(false);
  }

  searchOwners(text: string): void {
    this.ownerSearches.next(text);
  }

  /**
   * @param period cặp `{ Period, Year }` do `buildWritePeriod` dựng. `null` ⇒ ô lọc đang chọn một
   *   **tháng** (Q37): lúc đó **không** gửi `assessment` nào cả, chứ không phải chặn cả lời ghi —
   *   bốn trường danh mục vẫn sửa được. Luồng bình thường không tới đây vì `isEditable = false`.
   */
  save(value: ICriteriaFormValue, period: IWritePeriod | null, onCreated: () => void, onUpdated: (row: ICriteriaRow) => void): void {
    if (this.saving()) return;
    this.saving.set(true);
    this.serverError.set(null);

    // `assessment` VẮNG MẶT ≠ `assessment` rỗng: cái đầu là "không đụng tới dữ liệu đánh giá",
    // cái sau là "xoá trắng". Không có kỳ đích thì không có ca nào của hai vế đó áp dụng được —
    // gửi một `assessment` không kèm kỳ hợp lệ chỉ nhận `400 ASSESSMENT_PERIOD_*`.
    const payload = {
      Code: value.Code,
      Name: value.Name,
      GroupId: value.GroupId,
      MaxScore: value.MaxScore,
      Assessment: period
        ? {
            Period: period.Period,
            Year: period.Year,
            SelfScore: value.SelfScore,
            VerifiedScore: value.VerifiedScore,
            Status: value.Status,
            OwnerId: value.OwnerId,
            Deadline: value.Deadline,
            Note: value.Note,
            // `Version` thuộc BẢN GHI đang sửa, không thuộc ô nhập nào — và phải là bản ghi mà
            // người này ĐÃ NHÌN THẤY lúc bấm Sửa. Tra lại từ lưới hiện tại sẽ lấy nhầm bản vừa bị
            // người khác ghi đè, tức vô hiệu hoá đúng lớp bảo vệ `ASSESSMENT_CONFLICT`.
            Version: this.editing()?.Version ?? null,
          }
        : undefined,
    };

    const editing = this.editing();
    if (editing) {
      this.service.update(editing.CriteriaId, payload).subscribe({
        next: (row) => {
          this.saving.set(false);
          this.open.set(false);
          onUpdated(row);
          this.toast.success(this.translate.instant('danh-muc-dti.toast.updated') as string);
        },
        error: (err: IHttpErrorWithApiResult) => this.applyError(err, 'danh-muc-dti.error.updateFailed'),
      });
      return;
    }

    this.service.create(payload).subscribe({
      next: () => {
        this.saving.set(false);
        this.open.set(false);
        onCreated();
        this.toast.success(this.translate.instant('danh-muc-dti.toast.created') as string);
      },
      error: (err: IHttpErrorWithApiResult) => this.applyError(err, 'danh-muc-dti.error.createFailed'),
    });
  }

  /**
   * Envelope lỗi → câu trong `.form-error`. Hộp thoại **không** đóng: người dùng giữ nguyên thứ
   * vừa gõ và sửa đúng chỗ sai.
   *
   * Thứ tự ưu tiên là của `ApiErrorMessageService`: bản dịch của `businessCode` → `message` của BE
   * → câu dự phòng của màn này. Catalog DTI trả mã cấp **request**, không trả `fieldErrors` theo
   * ô, nên không có gì để bind xuống từng ô — bind đại một mã vào một ô là đoán.
   */
  private applyError(err: IHttpErrorWithApiResult, fallbackKey: string): void {
    this.saving.set(false);
    const result = err.apiResult;
    this.serverError.set(
      this.errorMessages.translateCode(result?.businessCode, result?.messageParams) ??
        result?.message ??
        (this.translate.instant(fallbackKey) as string),
    );
  }
}
