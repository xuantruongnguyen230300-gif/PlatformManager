import { Injectable, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable, Subject, catchError, map, of, switchMap, tap } from 'rxjs';
import { DanhMucDtiService } from '../../services/danh-muc-dti.service';
import { ICriteriaGrid, ICriteriaListParams, ICriteriaRow, ICriteriaWriteAccess } from '../../models/danh-muc-dti.model';

/** KHOÁ DỊCH — câu nằm ở `public/i18n-app/<mã>.json`. Xem `loadError`. */
const LOAD_ERROR_KEY = 'danh-muc-dti.error.loadFailed';

/**
 * Trạng thái quyền lúc CHƯA biết gì: mọi affordance ghi ẩn.
 *
 * 🛑 An toàn mặc định theo chiều ẨN, không theo chiều hiện. Đoán `canWrite = true` trong lúc chờ
 * response sẽ nhấp nháy hai nút ghi rồi giấu đi — và người không có quyền vẫn kịp bấm.
 */
const NO_ACCESS: ICriteriaWriteAccess = {
  CanWrite: false,
  IsEditable: false,
  EditBlockedBy: [],
  IsCurrentYear: false,
  // Q72 — chuỗi RỖNG là giá trị "chưa biết", và nó chỉ tồn tại ở trạng thái CHƯA TẢI / TẢI HỎNG,
  // không bao giờ đến từ server (card chốt hai trường này luôn có mặt). Mọi nơi tiêu thụ — dải băng
  // "nhắc kỳ đích" và câu kỳ đích của hai hộp thoại ghi — phải kiểm rỗng trước khi in, y như mọi
  // chỗ khác trên màn không vẽ gì khi chưa có dữ liệu.
  CurrentPeriod: '',
  CurrentPeriodLabel: '',
};

type ListOutcome = { readonly Ok: true; readonly Result: ICriteriaGrid } | { readonly Ok: false };

/**
 * TRẠNG THÁI LƯỚI của màn Danh mục DTI — dòng đang hiện, tổng số, khối quyền cấp màn, cờ tải, lỗi
 * tải — cùng dòng chảy request nuôi nó.
 *
 * Cùng khuôn với `platform/quan-tri-nguoi-dung/pages/quan-tri-nguoi-dung/user-list-feed.ts`, và
 * cùng lý do: lớp này **không biết gì về URL, về bộ lọc, về dải băng**. Trang quyết định KHI NÀO
 * tải; lớp này quyết định tải RA SAO.
 *
 * 🛑 `@Injectable()` KHÔNG `providedIn: 'root'` — khai trong `providers` của trang nên vòng đời
 * trùng vòng đời trang, y hệt các `signal()` mà nó thay thế. Lên `root` là để dữ liệu của lượt xem
 * trước sống sót qua điều hướng rồi hiện ra một nhịp trước khi lượt mới về.
 */
@Injectable()
export class CriteriaListFeed {
  private readonly service = inject(DanhMucDtiService);

  readonly rows = signal<ICriteriaRow[]>([]);
  readonly totalCount = signal(0);
  readonly loading = signal(false);

  /**
   * Khối quyền GHI cấp màn — nguồn DUY NHẤT cho ẩn/`disabled`/dải băng.
   *
   * Nó sống ở đây chứ không ở trang vì nó về **cùng response** với lưới: `canWrite` là một trường
   * của DM-2, không phải một lời gọi riêng. Đó cũng là lý do nó vẫn có mặt khi lưới RỖNG (ca T9) —
   * đúng lúc nút `Import CSV/Excel` cần biết ẩn hay hiện nhất.
   */
  readonly access = signal<ICriteriaWriteAccess>(NO_ACCESS);

  /**
   * Lượt tải gần nhất HỎNG — giữ KHOÁ DỊCH, không giữ câu, để khối lỗi đổi theo ngôn ngữ ngay cả
   * khi nó đã hiện sẵn từ trước.
   */
  readonly loadError = signal<string | null>(null);

  /**
   * Mọi lượt tải đi qua ĐÚNG một dòng chảy có `switchMap`. Thiếu nó thì kết quả của bộ lọc CŨ về
   * sau có thể đè lên kết quả của bộ lọc MỚI, và bảng hiện dữ liệu không khớp thứ đang ghi trong ô
   * tìm kiếm. Debounce 300 ms chỉ làm chuyện đó hiếm đi, không loại bỏ.
   */
  private readonly requests = new Subject<ICriteriaListParams>();

  constructor() {
    this.requests
      .pipe(
        tap(() => {
          this.loading.set(true);
          this.loadError.set(null);
        }),
        switchMap((params) => this.fetch(params)),
        takeUntilDestroyed(),
      )
      .subscribe((outcome) => this.apply(outcome));
  }

  load(params: ICriteriaListParams): void {
    this.requests.next(params);
  }

  /**
   * Thay MỘT dòng tại chỗ, sau khi DM-4 hoặc DM-6 trả về một dòng lưới đầy đủ.
   *
   * Vì sao không tải lại cả lưới cho chắc: người dùng sửa inline thường sửa liên tiếp nhiều dòng,
   * và một lượt refetch sau mỗi ô sẽ nhấp nháy bảng, kéo lại đúng bộ lọc đó, rồi có thể **đổi thứ
   * tự/trang** giữa hai lần gõ. Đường THÊM (DM-3) thì ngược lại — nó chỉ trả sáu trường danh mục,
   * không đủ dựng một dòng, và nó đổi cả `totalCount` lẫn phân trang — nên đường đó **phải**
   * refetch (Q49).
   *
   * 🛑 Không tìm thấy dòng ⇒ **không làm gì**, KHÔNG chèn thêm. Dòng biến mất khỏi trang hiện tại
   * giữa chừng (người khác đổi mã, bộ lọc vừa đổi) là ca thật; chèn nó vào cuối bảng sẽ dựng ra
   * một hàng không thuộc bộ lọc đang hiện.
   */
  replaceRow(row: ICriteriaRow): void {
    this.rows.update((rows) => {
      const index = rows.findIndex((item) => item.CriteriaId === row.CriteriaId);
      if (index < 0) return rows;
      const next = [...rows];
      next[index] = row;
      return next;
    });
  }

  /** `catchError` nằm TRONG inner observable — lỗi lọt qua `switchMap` sẽ giết luôn dòng chảy. */
  private fetch(params: ICriteriaListParams): Observable<ListOutcome> {
    return this.service.getGrid(params).pipe(
      map((result): ListOutcome => ({ Ok: true, Result: result })),
      catchError((): Observable<ListOutcome> => of({ Ok: false })),
    );
  }

  /**
   * Tải hỏng ⇒ **XOÁ BẢNG** rồi hiện khối lỗi. Giữ dữ liệu của lượt trước là ca nguy hiểm nhất:
   * đổi kỳ sang `Tuần 30` mà request hỏng thì bảng vẫn hiện số của kỳ TRƯỚC, trông y như đó là
   * kết quả của kỳ mới. Toast của interceptor biến mất sau vài giây, bảng thì ở lại.
   *
   * Khối quyền cũng về `NO_ACCESS`: một cờ `canWrite` của lượt tải trước không nói gì về bộ lọc
   * hiện tại, mà nó lại là thứ quyết `disabled`.
   */
  private apply(outcome: ListOutcome): void {
    this.loading.set(false);
    if (!outcome.Ok) {
      this.rows.set([]);
      this.totalCount.set(0);
      this.access.set(NO_ACCESS);
      this.loadError.set(LOAD_ERROR_KEY);
      return;
    }
    this.loadError.set(null);
    this.rows.set(outcome.Result.Items);
    this.totalCount.set(outcome.Result.TotalCount);
    this.access.set(outcome.Result.Access);
  }
}
