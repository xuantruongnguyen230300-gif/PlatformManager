import { Injectable, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable, Subject, catchError, map, of, switchMap, tap } from 'rxjs';
import { QuanTriNguoiDungService } from '../../services/quan-tri-nguoi-dung.service';
import { IPagedResult } from '../../../../core/http/paged-result.model';
import { IUser, IUserListParams } from '../../models/quan-tri-nguoi-dung.model';

/** KHOÁ DỊCH — câu nằm ở `public/i18n/<mã>.json`. Xem `loadError`. */
const LOAD_ERROR_KEY = 'quan-tri-nguoi-dung.error.loadFailed';

/**
 * Kết quả MỘT lượt tải danh sách. Lỗi được gói vào GIÁ TRỊ (`Ok: false`) chứ không để nó thoát ra
 * ngoài dưới dạng lỗi của Observable: một lỗi lọt qua `switchMap` sẽ giết luôn dòng chảy, và từ đó
 * mọi lần đổi bộ lọc về sau im lặng không gọi API nữa — hỏng một lần thành hỏng vĩnh viễn.
 */
type ListOutcome = { readonly Ok: true; readonly Result: IPagedResult<IUser> } | { readonly Ok: false };

/**
 * TRẠNG THÁI LƯỚI của màn Quản trị người dùng — dữ liệu đang hiện, cờ tải, lỗi tải — cùng dòng
 * chảy request nuôi nó.
 *
 * Tách khỏi `QuanTriNguoiDungPage` (2026-09-10) vì đây là một RANH GIỚI THẬT, không phải một lát
 * cắt cho vừa ngưỡng số dòng: nó không biết gì về URL, về bộ lọc, về hộp thoại nào — nó chỉ nhận
 * một bộ tham số rồi trả về ba trạng thái mà lưới phải phân biệt được bằng mắt. Trang là nơi duy
 * nhất quyết định KHI NÀO tải; lớp này quyết định tải RA SAO.
 *
 * 🛑 `@Injectable()` KHÔNG `providedIn: 'root'` — khai trong `providers` của trang, nên vòng đời
 * trùng đúng vòng đời trang, y hệt các field `signal()` mà nó thay thế. Đưa lên `root` là để dữ
 * liệu của lượt xem trước sống sót qua điều hướng rồi hiện ra một nhịp trước khi lượt mới về.
 */
@Injectable()
export class UserListFeed {
  private readonly service = inject(QuanTriNguoiDungService);

  readonly rows = signal<IUser[]>([]);
  readonly totalCount = signal(0);
  readonly loading = signal(false);
  /**
   * Lượt tải gần nhất HỎNG — giữ KHOÁ DỊCH, không giữ câu. Không phải `boolean` vì khối lỗi hiện
   * thẳng câu tương ứng ra màn hình; template dịch nó bằng `| translate` nên câu đổi theo ngôn ngữ
   * ngay cả khi khối lỗi đã hiện sẵn từ trước.
   *
   * Ba trạng thái của lưới phải phân biệt được bằng mắt (fe/11-grid-and-metadata.md §"Ba trạng
   * thái của lưới"): đang tải (overlay của `p-table`), không có kết quả (dòng "không khớp bộ lọc"
   * trong bảng), tải hỏng (khối `.notice.bad` + nút thử lại, BẢNG BỊ GỠ HẲN).
   */
  readonly loadError = signal<string | null>(null);

  /**
   * Mọi lượt tải danh sách đi qua ĐÚNG một dòng chảy có `switchMap` — cả đường `effect()` theo URL
   * lẫn đường nạp lại sau CUD. `switchMap` huỷ request cũ khi có request mới; thiếu nó thì kết
   * quả của bộ lọc CŨ về sau có thể đè lên kết quả của bộ lọc MỚI và bảng hiện dữ liệu không khớp
   * thứ đang ghi trong ô tìm kiếm (fe/02-http-envelope.md, chốt 2026-08-31). Debounce 300ms chỉ làm
   * chuyện đó hiếm đi, không loại bỏ.
   */
  private readonly requests = new Subject<IUserListParams>();

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

  /** Xếp một lượt tải vào dòng chảy. Lượt đang bay (nếu có) bị huỷ — xem `requests`. */
  load(params: IUserListParams): void {
    this.requests.next(params);
  }

  /** `catchError` nằm TRONG inner observable — xem `ListOutcome`. */
  private fetch(params: IUserListParams): Observable<ListOutcome> {
    return this.service.getList(params).pipe(
      map((result): ListOutcome => ({ Ok: true, Result: result })),
      catchError((): Observable<ListOutcome> => of({ Ok: false })),
    );
  }

  /**
   * Tải hỏng ⇒ **XOÁ BẢNG** rồi hiện khối lỗi (fe/11-grid-and-metadata.md §"Ba trạng thái của
   * lưới", chốt 2026-08-31). Giữ lại dữ liệu của lượt tải trước là ca nguy hiểm nhất của màn này:
   * đổi bộ lọc sang "Đã khoá" mà request hỏng thì bảng vẫn hiện danh sách của bộ lọc TRƯỚC, trông
   * y như đó là kết quả của bộ lọc mới. Toast của interceptor biến mất sau vài giây, bảng thì ở
   * lại. Nguyên tắc được giữ: KHÔNG hiển thị dữ liệu mà ta không biết có còn đúng hay không.
   */
  private apply(outcome: ListOutcome): void {
    this.loading.set(false);
    if (!outcome.Ok) {
      this.rows.set([]);
      this.totalCount.set(0);
      this.loadError.set(LOAD_ERROR_KEY);
      return;
    }
    this.loadError.set(null);
    this.rows.set(outcome.Result.Items);
    this.totalCount.set(outcome.Result.TotalCount);
  }
}
