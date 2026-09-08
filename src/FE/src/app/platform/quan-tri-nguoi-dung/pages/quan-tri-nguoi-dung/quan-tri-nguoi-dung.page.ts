import { ChangeDetectionStrategy, Component, computed, effect, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Params, Router } from '@angular/router';
import {
  Observable,
  Subject,
  catchError,
  debounceTime,
  distinctUntilChanged,
  map,
  of,
  switchMap,
  tap,
} from 'rxjs';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { QuanTriNguoiDungService } from '../../services/quan-tri-nguoi-dung.service';
import { ToastService } from '../../../../core/toast/toast.service';
import { CurrentUserService } from '../../../../core/auth/current-user.service';
import { ApiFieldError, IApiResult, IHttpErrorWithApiResult } from '../../../../core/http/api-result.model';
import { IPagedResult } from '../../../../core/http/paged-result.model';
import { ICreateUserPayload, IUpdateUserPayload, IUser, IUserListParams } from '../../models/quan-tri-nguoi-dung.model';
import { LanguageService } from '../../../../core/i18n/language.service';
import { UserGridTable } from '../../components/user-grid-table/user-grid-table';
import { UserFormDialog, IUserFormSaveEvent } from '../../components/user-form-dialog/user-form-dialog';
import { IToolbarChip, Toolbar } from '../../../../shared/components/toolbar/toolbar';
import { ConfirmDialog } from '../../../../shared/components/confirm-dialog/confirm-dialog';

const DEBOUNCE_MS = 300;
const DEFAULT_PAGE_SIZE = 10;
/** KHOÁ DỊCH — câu nằm ở `public/i18n/<mã>.json`. Xem `loadError`. */
const LOAD_ERROR_KEY = 'quan-tri-nguoi-dung.error.loadFailed';

/**
 * Kết quả MỘT lượt tải danh sách. Lỗi được gói vào GIÁ TRỊ (`Ok: false`) chứ không để nó thoát ra
 * ngoài dưới dạng lỗi của Observable: một lỗi lọt qua `switchMap` sẽ giết luôn dòng chảy, và từ đó
 * mọi lần đổi bộ lọc về sau im lặng không gọi API nữa — hỏng một lần thành hỏng vĩnh viễn.
 */
type ListOutcome = { readonly Ok: true; readonly Result: IPagedResult<IUser> } | { readonly Ok: false };

/** Giá trị ô "Trạng thái" trong bảng lọc — chuỗi rỗng = không lọc. */
type UserStatusFilter = '' | 'active' | 'locked';

/** Khoá của một điều kiện đang áp, dùng cho nút gỡ trên chip. */
type UserFilterKey = 'role' | 'status';

/** Đọc một số nguyên dương từ query param; giá trị rác (chữ, số âm, 0) rơi về `fallback`. */
function readPositiveInt(raw: string | null, fallback: number): number {
  const value = Number(raw);
  return Number.isInteger(value) && value > 0 ? value : fallback;
}

/**
 * SMART — route `/quan-tri/nguoi-dung`. Gate `authGuard`+`mustChangePasswordGuard`+`adminGuard`.
 * Điều phối `toolbar` + `user-grid-table` + `user-form-dialog` + `confirm-dialog`, gọi
 * `QuanTriNguoiDungService`. Bộ lọc search debounce 300ms, phân trang server-side — xem
 * doc/contracts/users.md.
 *
 * **URL là nguồn sự thật của bộ lọc** (di trú 2026-08-29 theo
 * doc/huong_dan/quy-uoc/fe-routing-guard.md §8 — "màn hình cũ di trú khi có dịp chạm vào").
 * `searchText`/`role`/`isLocked`/`page`/`pageSize` đọc từ `queryParams` và ghi ngược lại bằng
 * `replaceUrl: true`. Không giữ bản sao trong `signal()`: hai nguồn cho cùng một giá trị sẽ lệch
 * nhau, và đó chính là thứ làm F5 mất bộ lọc, link gửi đi mở ra danh sách khác, nút Back nhảy
 * khỏi trang thay vì lùi một bước lọc.
 *
 * NGOẠI LỆ có chủ đích, đều nằm đúng cột "ở lại trong signal" của §8: bản nháp bộ lọc
 * (`roleDraft`/`statusDraft` — chưa bấm "Áp dụng"), chuỗi đang gõ trong ô tìm kiếm (đã có URL
 * làm bản chốt sau debounce), dữ liệu đã tải, cờ loading và trạng thái dialog.
 */
@Component({
  selector: 'app-quan-tri-nguoi-dung-page',
  standalone: true,
  imports: [Toolbar, ConfirmDialog, UserGridTable, UserFormDialog, TranslatePipe],
  templateUrl: './quan-tri-nguoi-dung.page.html',
  // `page-fill` đặt lên CHÍNH thẻ host, không phải một <div> bọc thêm: thẻ host vốn đã là con
  // trực tiếp của <main> (đã là flex column, xem app.scss), nên `.card` bên trong vẫn là con
  // trực tiếp của `.page-fill` — khớp đúng selector `.page-fill > .card` ở styles.scss. Cách
  // này khỏi cần `:host { display: contents }` lẫn một file .scss chỉ để khai đúng một dòng,
  // giữ nguyên chủ đích "trang này không có styleUrl riêng" ghi ngay dưới đây.
  host: { class: 'page-fill' },
  // Không còn `styleUrl`: toàn bộ giao diện trang dùng lại class của styles.scss
  // (.card/.title/.toolbar/.filter/.input-icon.search), nên file SCSS riêng đã trống và bị xoá.
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class QuanTriNguoiDungPage {
  /**
   * `localeId` để grid định dạng ngày theo ngôn ngữ đang chọn. Inject Ở ĐÂY chứ không ở
   * `UserGridTable`: page là smart nên được phép inject; grid nằm trong `components/` nên LUẬT G4
   * cấm (luật đã chốt ở doc/huong_dan/wiki-core/fe/trien-khai/05-gate.md; cổng tự động thì chưa
   * có) — xem JSDoc của `UserGridTable.localeId`.
   */
  private readonly language = inject(LanguageService);
  protected readonly localeId = this.language.localeId;
  private readonly translate = inject(TranslateService);

  private readonly service = inject(QuanTriNguoiDungService);
  private readonly toast = inject(ToastService);
  private readonly currentUser = inject(CurrentUserService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly currentUserId = computed(() => this.currentUser.currentUser()?.Id ?? null);

  /** `requireSync` được: `queryParamMap` phát ngay khi subscribe, nên trang không có một nhịp
   * "chưa biết bộ lọc" để lỡ gọi API bằng bộ lọc rỗng rồi gọi lại lần hai. */
  private readonly queryMap = toSignal(this.route.queryParamMap, { requireSync: true });

  // ===== Nguồn sự thật: URL =====
  protected readonly searchText = computed(() => this.queryMap().get('searchText') ?? '');
  protected readonly roleFilter = computed(() => this.queryMap().get('role') ?? '');
  protected readonly statusFilter = computed<UserStatusFilter>(() => {
    const raw = this.queryMap().get('isLocked');
    if (raw === 'true') return 'locked';
    if (raw === 'false') return 'active';
    return '';
  });
  protected readonly page = computed(() => readPositiveInt(this.queryMap().get('page'), 1));
  protected readonly pageSize = computed(() => readPositiveInt(this.queryMap().get('pageSize'), DEFAULT_PAGE_SIZE));

  /**
   * Chuỗi ĐANG GÕ trong ô tìm kiếm. Vẫn là signal (không đọc thẳng `searchText()`) vì URL chỉ được
   * cập nhật sau debounce 300ms — bắt ô nhập chờ URL sẽ làm chữ hiện ra giật một nhịp sau phím gõ.
   */
  readonly searchInput = signal('');
  private readonly searchSubject = new Subject<string>();

  /**
   * Giá trị `searchText` mà CHÍNH TRANG NÀY vừa đẩy lên URL. Dùng để phân biệt hai chiều thay đổi:
   * URL đổi do ta đẩy lên (không được kéo ngược về ô nhập — người dùng có thể đã gõ thêm trong lúc
   * điều hướng chạy) với URL đổi từ bên ngoài (Back/Forward, dán link) — chiều đó thì ô nhập PHẢI
   * cập nhật theo.
   */
  private lastPushedSearch = '';

  // Bản nháp bộ lọc — giá trị đang chọn trong panel, CHƯA áp. Tách khỏi URL vì bảng lọc có 2 điều
  // kiện: đổi tới đâu ghi URL tới đó sẽ gọi API 2 lần cho một lần người dùng đổi ý, và danh sách
  // nhảy ngay dưới tay trong lúc còn đang chọn dở.
  protected readonly roleDraft = signal('');
  protected readonly statusDraft = signal<UserStatusFilter>('');

  /** Số điều kiện đang áp — hiện trên nút Lọc để biết danh sách đang bị lọc kể cả khi
   * bảng điều kiện đã đóng. */
  protected readonly activeFilterCount = computed(
    () => (this.roleFilter() ? 1 : 0) + (this.statusFilter() ? 1 : 0),
  );

  /**
   * Chip gỡ được, dựng từ điều kiện ĐANG ÁP (không phải bản nháp).
   *
   * `Label` là CÂU đã dịch chứ không phải khoá: `IToolbarChip` đi thẳng vào `<app-toolbar>`, một
   * component dumb chỉ biết vẽ chữ nó nhận được. Nhãn ở đây cũng không phải chuỗi cố định mà là câu
   * CÓ THAM SỐ, nên phải ráp ở đây rồi mới gửi đi.
   *
   * 🛑 Ráp bằng khoá + tham số, KHÔNG nối chuỗi kiểu `'Vai trò: ' + role`: nối chuỗi khoá cứng trật
   * tự từ theo tiếng Việt, và bản `en` không còn cách nào đặt nhãn ở vị trí khác giá trị.
   *
   * `this.language.current()` làm phụ thuộc signal tường minh tại chỗ đọc. ⚠️ Nó KHÔNG phải thứ
   * làm reactivity chạy được — `instant()` của @ngx-translate/core v18 tự đọc `currentLang` bên
   * trong (đo 2026-09-06, xem `core/title/page-title.strategy.ts`). Giữ lại để không buộc màn hình
   * vào một chi tiết bên trong thư viện.
   */
  protected readonly filterChips = computed<IToolbarChip[]>(() => {
    this.language.current();
    const chips: IToolbarChip[] = [];
    const role = this.roleFilter();
    if (role) {
      chips.push({
        Key: 'role',
        Label: this.translate.instant('quan-tri-nguoi-dung.filter.chipRole', { value: role }) as string,
      });
    }
    const status = this.statusFilter();
    if (status) {
      const statusLabel = this.translate.instant(
        status === 'locked' ? 'quan-tri-nguoi-dung.grid.locked' : 'quan-tri-nguoi-dung.grid.active',
      ) as string;
      chips.push({
        Key: 'status',
        Label: this.translate.instant('quan-tri-nguoi-dung.filter.chipStatus', {
          value: statusLabel,
        }) as string,
      });
    }
    return chips;
  });

  protected readonly rows = signal<IUser[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly loading = signal(false);
  /**
   * Lượt tải gần nhất HỎNG — giữ KHOÁ DỊCH, không giữ câu. Không phải `boolean` vì khối lỗi hiện
   * thẳng câu tương ứng ra màn hình; template dịch nó bằng `| translate` nên câu đổi theo ngôn ngữ
   * ngay cả khi khối lỗi đã hiện sẵn từ trước.
   *
   * Ba trạng thái của lưới phải phân biệt được bằng mắt (fe/11-grid-and-metadata.md §"Ba trạng
   * thái của lưới"): đang tải (overlay của `p-table`), không có kết quả (dòng "không khớp bộ lọc"
   * trong bảng), tải hỏng (khối `.notice.bad` + nút thử lại, BẢNG BỊ GỠ HẲN).
   */
  protected readonly loadError = signal<string | null>(null);

  /**
   * Mọi lượt tải danh sách đi qua ĐÚNG một dòng chảy có `switchMap` — cả đường `effect()` theo URL
   * lẫn đường `reload()` sau CUD. `switchMap` huỷ request cũ khi có request mới; thiếu nó thì kết
   * quả của bộ lọc CŨ về sau có thể đè lên kết quả của bộ lọc MỚI và bảng hiện dữ liệu không khớp
   * thứ đang ghi trong ô tìm kiếm (fe/02-http-envelope.md, chốt 2026-08-31). Debounce 300ms chỉ làm
   * chuyện đó hiếm đi, không loại bỏ.
   */
  private readonly listRequests = new Subject<IUserListParams>();

  protected readonly formOpen = signal(false);
  /** Request tạo/sửa đang bay → khoá nút Lưu của hộp thoại (fe/09-forms-validation.md). */
  protected readonly formSaving = signal(false);
  protected readonly formEditing = signal<IUser | null>(null);
  protected readonly formServerError = signal<string | null>(null);
  /** `fields` nguyên văn của envelope lỗi gần nhất — truyền thẳng xuống form để bind vào từng ô. */
  /**
   * `fieldErrors` của envelope lỗi form gần nhất — truyền thẳng xuống `UserFormDialog`.
   *
   * Đổi 2026-09-06 từ `fields`: lỗi nghiệp vụ (mã Identity) chỉ có ở `fieldErrors`, `fields` để
   * trống. Xem doc/huong_dan/wiki-core/fe/02-http-envelope.md §"Lỗi theo ô".
   */
  protected readonly formServerFieldErrors = signal<Record<string, ApiFieldError[]> | null>(null);

  /**
   * Người dùng đang chờ xác nhận KHOÁ. `null` = không có câu hỏi nào đang mở. Giữ ở đây (không đọc
   * lại từ grid) vì dữ liệu có thể được nạp lại trong lúc hộp thoại đang mở.
   */
  private readonly pendingLock = signal<IUser | null>(null);
  private readonly lockConfirm = viewChild.required(ConfirmDialog);

  /**
   * Câu mô tả trong hộp thoại xác nhận khoá. Nó nói ĐÚNG độ trễ đã ghi ở doc/contracts/users.md
   * §"Khoá KHÔNG có hiệu lực tức thì": hệ thống dùng cookie session, phiên đang chạy còn sống tối
   * đa ~30 phút. Hứa "đã đăng xuất ngay" là để quản trị viên tin đã chặn xong trong khi người bị
   * khoá vẫn đang thao tác.
   *
   * 🛑 HAI KHOÁ TRỌN CÂU, không phải một câu ghép với một mảnh chủ ngữ thay được. Ghép mảnh buộc
   * mọi bản dịch phải nhận đúng trật tự từ của tiếng Việt; hai câu đủ thì mỗi ngôn ngữ tự đặt chủ
   * ngữ vào chỗ của nó. Cùng khuôn với `trang-chu.greeting` / `trang-chu.greetingNamed` đã có sẵn
   * trong bảng dịch.
   */
  protected readonly lockConfirmDescription = computed(() => {
    this.language.current();
    const user = this.pendingLock();
    return user
      ? (this.translate.instant('quan-tri-nguoi-dung.dialog.lockDescriptionNamed', {
          name: user.FullName,
        }) as string)
      : (this.translate.instant('quan-tri-nguoi-dung.dialog.lockDescription') as string);
  });

  private readonly requestParams = computed<IUserListParams>(() => {
    const status = this.statusFilter();
    return {
      Page: this.page(),
      PageSize: this.pageSize(),
      SearchText: this.searchText() || undefined,
      Role: this.roleFilter() || undefined,
      // `undefined` (không lọc) khác hẳn `false` (chỉ tài khoản đang hoạt động) — không rút gọn
      // thành boolean, nếu không "Tất cả trạng thái" sẽ hoá thành "chỉ đang hoạt động".
      IsLocked: status === '' ? undefined : status === 'locked',
    };
  });

  constructor() {
    // Ô tìm kiếm: gõ → chờ 300ms → GHI LÊN URL. Debounce nằm ở đây (không ở `Toolbar`) vì mỗi màn
    // có ngưỡng chờ khác nhau; và nó chặn được cả request thừa lẫn mục history thừa.
    //
    // Regression đã có test chốt (quan-tri-nguoi-dung.page.spec.ts): bản cũ gọi thẳng `loadList()`
    // trong handler mỗi phím gõ — vừa bắn 1 request/phím (sai
    // doc/huong_dan/wiki-core/fe/13-performance.md §6) vừa gửi SAI chuỗi tìm kiếm (giá trị TRƯỚC
    // debounce).
    this.searchSubject
      .pipe(debounceTime(DEBOUNCE_MS), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe((text) => {
        this.lastPushedSearch = text;
        this.patchQueryParams({ searchText: text || null, page: null });
      });

    // URL đổi từ bên ngoài (Back/Forward, dán link) → kéo ô nhập theo. Xem `lastPushedSearch`.
    effect(() => {
      const fromUrl = this.searchText();
      if (fromUrl === this.lastPushedSearch) return;
      this.lastPushedSearch = fromUrl;
      this.searchInput.set(fromUrl);
    });

    // Bản nháp luôn khởi lại từ điều kiện ĐANG ÁP: mở panel ra phải thấy đúng thứ đang lọc, kể cả
    // khi điều kiện vừa bị gỡ bằng chip hoặc bị đổi bằng nút Back.
    effect(() => {
      this.roleDraft.set(this.roleFilter());
      this.statusDraft.set(this.statusFilter());
    });

    this.listRequests
      .pipe(
        tap(() => {
          this.loading.set(true);
          this.loadError.set(null);
        }),
        switchMap((params) => this.fetchList(params)),
        takeUntilDestroyed(),
      )
      .subscribe((outcome) => this.applyListOutcome(outcome));

    // `effect()` gọi API theo đúng bộ lọc/phân trang ĐANG Ở TRÊN URL — side-effect thật (gọi HTTP),
    // không derive state. Chạy ĐÚNG 1 LẦN mỗi khi `requestParams()` thực sự đổi giá trị.
    effect(() => {
      this.listRequests.next(this.requestParams());
    });
  }

  /** `catchError` nằm TRONG inner observable — xem `ListOutcome`. */
  private fetchList(params: IUserListParams): Observable<ListOutcome> {
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
  private applyListOutcome(outcome: ListOutcome): void {
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

  /**
   * Ghi bộ lọc lên URL. `merge` để mỗi lời gọi chỉ nói về tham số nó quan tâm; `null` xoá hẳn
   * tham số khỏi URL (giá trị mặc định không nên chiếm chỗ trên thanh địa chỉ).
   * `replaceUrl: true` — đổi bộ lọc không được đẻ ra mục history mới, nếu không nút Back sẽ phải
   * bấm hàng chục lần mới rời được trang (fe-routing-guard.md §8).
   */
  private patchQueryParams(patch: Params): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: patch,
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }

  /**
   * Nạp lại NGOÀI effect — dùng sau CUD (tạo/sửa/khoá) khi `requestParams()` không đổi giá trị nên
   * effect không tự kích lại, nhưng dữ liệu server đã đổi. Cũng là handler của nút "Thử lại" ở
   * khối lỗi, nên `protected` chứ không `private`.
   */
  protected reload(): void {
    this.listRequests.next(this.requestParams());
  }

  /** Ô tìm kiếm của `<app-toolbar>` đổi giá trị (model `searchValue`). */
  onSearchValueChange(value: string): void {
    this.searchInput.set(value);
    this.searchSubject.next(value);
  }

  onRoleDraftChange(event: Event): void {
    this.roleDraft.set((event.target as HTMLSelectElement).value);
  }

  onStatusDraftChange(event: Event): void {
    this.statusDraft.set((event.target as HTMLSelectElement).value as UserStatusFilter);
  }

  /**
   * Áp bản nháp thành điều kiện thật. Không còn nhận `HTMLDetailsElement`: `<app-toolbar>` tự đóng
   * panel của mình trước khi phát sự kiện — trạng thái mở/đóng thuộc về component đó, trang cha
   * thò tay vào là hai nơi cùng quản một thứ.
   */
  onApplyFilters(): void {
    // Lọc lại thì trang hiện tại vô nghĩa — trang 5 của kết quả cũ thường rỗng ở kết quả mới.
    this.patchQueryParams({
      role: this.roleDraft() || null,
      isLocked: this.toIsLockedParam(this.statusDraft()),
      page: null,
    });
  }

  onClearFilters(): void {
    this.patchQueryParams({ role: null, isLocked: null, page: null });
  }

  /** Gỡ 1 điều kiện từ chip — điều kiện còn lại giữ nguyên (nhờ `queryParamsHandling: 'merge'`). */
  onRemoveFilter(key: string): void {
    const filterKey = key as UserFilterKey;
    this.patchQueryParams(filterKey === 'role' ? { role: null, page: null } : { isLocked: null, page: null });
  }

  /** `''` (tất cả) → `null` để xoá hẳn tham số, KHÔNG phải `'false'` (= chỉ đang hoạt động). */
  private toIsLockedParam(status: UserStatusFilter): string | null {
    if (status === '') return null;
    return status === 'locked' ? 'true' : 'false';
  }

  onGridPageChange(event: { Page: number; PageSize: number }): void {
    this.patchQueryParams({
      page: event.Page === 1 ? null : event.Page,
      pageSize: event.PageSize === DEFAULT_PAGE_SIZE ? null : event.PageSize,
    });
  }

  openCreateForm(): void {
    this.formEditing.set(null);
    this.clearFormErrors();
    this.formSaving.set(false);
    this.formOpen.set(true);
  }

  openEditForm(user: IUser): void {
    this.formEditing.set(user);
    this.clearFormErrors();
    this.formSaving.set(false);
    this.formOpen.set(true);
  }

  onFormClosed(): void {
    this.formOpen.set(false);
  }

  onFormSaved(event: IUserFormSaveEvent): void {
    const editing = this.formEditing();
    if (event.IsEditing && editing && event.Update) {
      // `Version` ghép Ở ĐÂY chứ không để form phát ra: token chống ghi đè thuộc về BẢN GHI đang
      // sửa (`formEditing`), không phải một ô nhập nào. Dùng đúng bản ghi đã mở form — KHÔNG tra
      // lại từ `rows()` — vì thứ cần so là trạng thái mà người này ĐÃ NHÌN THẤY lúc bấm Sửa; tra
      // lại danh sách hiện tại sẽ lấy nhầm bản vừa bị người khác ghi đè và làm 409 không bao giờ
      // xảy ra, tức vô hiệu hoá đúng lớp bảo vệ này.
      this.submitUpdate(editing.Id, { ...event.Update, Version: editing.Version });
    } else if (!event.IsEditing) {
      this.submitCreate(event.Create as ICreateUserPayload);
    }
  }

  private clearFormErrors(): void {
    this.formServerError.set(null);
    this.formServerFieldErrors.set(null);
  }

  /**
   * Tách lỗi envelope thành 2 phần cho form: `fieldErrors` (bind vào từng ô) và `message` (câu chung).
   * Trước đây chỉ lấy `message`, nên mọi lỗi 400 hiện đúng một câu "Dữ liệu không hợp lệ." dù BE
   * đã nói rõ ô nào sai — trái quy tắc ở doc/huong_dan/quy-uoc/fe-api-client.md §Envelope.
   */
  private applyFormError(err: IHttpErrorWithApiResult, fallbackKey: string): void {
    const result: IApiResult<unknown> | null | undefined = err.apiResult;
    this.formServerFieldErrors.set(result?.fieldErrors ?? null);
    // Câu dự phòng dịch NGAY tại đây: `formServerError` cũng mang `message` của BE, tức đã là câu —
    // một trường, một loại giá trị. Nhánh này chỉ chạy khi envelope KHÔNG có `message` (lỗi mạng /
    // hạ tầng), lúc đó người dùng bấm Lưu lại chứ không đứng đổi ngôn ngữ.
    this.formServerError.set(result?.message ?? (this.translate.instant(fallbackKey) as string));
  }

  private submitCreate(payload: ICreateUserPayload): void {
    this.formSaving.set(true);
    this.service.create(payload).subscribe({
      next: () => {
        this.formSaving.set(false);
        this.formOpen.set(false);
        this.reload();
        this.toast.success(this.translate.instant('quan-tri-nguoi-dung.toast.created') as string);
      },
      error: (err: IHttpErrorWithApiResult) => {
        this.formSaving.set(false);
        this.applyFormError(err, 'quan-tri-nguoi-dung.error.createFailed');
      },
    });
  }

  /**
   * **409 `USER.VERSION_CONFLICT` đi CHUNG đường với mọi lỗi khác của form** — không có nhánh
   * riêng, và đó là lựa chọn có chủ đích (2026-09-08).
   *
   * doc/contracts/users.md chốt đúng một hành vi cho ca tranh chấp ghi: *"lệch ⇒ 409, và handler
   * KHÔNG ghi gì"*. Nó KHÔNG mô tả màn hình phải làm gì thêm. Nên FE giữ nguyên đường đã có —
   * `applyFormError` hiện `message` của BE trong form, `httpErrorInterceptor` hiện cùng câu ấy trên
   * toast — y hệt 400/422. Tự thêm "đóng form", "tải lại rồi mở lại", hay "trộn dữ liệu mới vào ô
   * đang gõ" đều là phát minh hành vi ngoài hợp đồng, và cái cuối còn xoá mất thứ người dùng vừa gõ.
   *
   * ⚠️ Hệ quả đã biết, KHÔNG che giấu: form giữ nguyên `Version` cũ sau 409, nên bấm Lưu lại sẽ
   * 409 tiếp cho tới khi người dùng đóng form và mở lại. Câu của BE nói đúng lối ra đó ("Hãy tải
   * lại danh sách rồi thực hiện lại"). Rút ngắn vòng này là một quyết định UX cần chốt riêng.
   */
  private submitUpdate(id: string, payload: IUpdateUserPayload): void {
    this.formSaving.set(true);
    this.service.update(id, payload).subscribe({
      next: (succeeded) => {
        this.formSaving.set(false);
        if (!succeeded) {
          // Envelope không báo thành công → KHÔNG đóng form, KHÔNG báo "đã cập nhật".
          this.formServerError.set(
            this.translate.instant('quan-tri-nguoi-dung.error.updateNotSaved') as string,
          );
          return;
        }
        this.formOpen.set(false);
        this.reload();
        this.toast.success(this.translate.instant('quan-tri-nguoi-dung.toast.updated') as string);
      },
      error: (err: IHttpErrorWithApiResult) => {
        this.formSaving.set(false);
        this.applyFormError(err, 'quan-tri-nguoi-dung.error.updateFailed');
      },
    });
  }

  /**
   * KHOÁ hỏi lại, MỞ KHOÁ thì không. Ranh giới cố ý: khoá là chiều gây hậu quả (người kia mất
   * đường vào hệ thống) và hậu quả đó có phần người bấm không đoán được — độ trễ 30 phút của phiên
   * đang chạy; mở khoá là chiều khôi phục, chặn thêm một bước chỉ làm chậm việc sửa sai.
   */
  onToggleLock(user: IUser): void {
    if (user.IsLocked) {
      this.submitUnlock(user);
      return;
    }
    this.pendingLock.set(user);
    this.lockConfirm().open();
  }

  /**
   * Toast thành công CHỈ khi envelope báo thành công — không suy từ "gọi xong không lỗi mạng"
   * (doc/contracts/users.md §"Thao tác hỏng nay là LỖI", finding BE-4). Với BE hiện tại, thao tác
   * hỏng là **422** nên rơi vào nhánh `error` và `httpErrorInterceptor` hiện thẳng `message` của
   * `USER.LOCK_FAILED` — câu đó cố ý nhắc kiểm lại trạng thái tài khoản, vì `lock` đổi con dấu bảo
   * mật TRƯỚC khi đặt lockout nên hỏng giữa chừng để lại trạng thái nửa vời (phiên bị chấm dứt
   * nhưng vẫn đăng nhập lại được).
   */
  onLockConfirmed(): void {
    const user = this.pendingLock();
    this.pendingLock.set(null);
    if (!user) return;
    this.service.lock(user.Id).subscribe({
      next: (succeeded) => {
        this.reload();
        if (succeeded) {
          this.toast.success(this.translate.instant('quan-tri-nguoi-dung.toast.locked') as string);
        } else {
          this.toast.error(this.translate.instant('quan-tri-nguoi-dung.toast.lockFailed') as string);
        }
      },
      error: () => {
        // 422/403/404: httpErrorInterceptor đã hiện `message` của envelope.
      },
    });
  }

  onLockCancelled(): void {
    this.pendingLock.set(null);
  }

  private submitUnlock(user: IUser): void {
    this.service.unlock(user.Id).subscribe({
      next: (succeeded) => {
        this.reload();
        if (succeeded) {
          this.toast.success(this.translate.instant('quan-tri-nguoi-dung.toast.unlocked') as string);
        } else {
          this.toast.error(this.translate.instant('quan-tri-nguoi-dung.toast.unlockFailed') as string);
        }
      },
      error: () => {
        // 422/404: httpErrorInterceptor đã hiện `message` của envelope.
      },
    });
  }
}
