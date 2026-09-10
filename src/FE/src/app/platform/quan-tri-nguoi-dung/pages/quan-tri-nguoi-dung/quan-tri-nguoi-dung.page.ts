import { ChangeDetectionStrategy, Component, computed, effect, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Params, Router } from '@angular/router';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { CurrentUserService } from '../../../../core/auth/current-user.service';
import { IUser, IUserListParams } from '../../models/quan-tri-nguoi-dung.model';
import { LanguageService } from '../../../../core/i18n/language.service';
import { UserGridTable } from '../../components/user-grid-table/user-grid-table';
import { UserFormDialog, IUserFormSaveEvent } from '../../components/user-form-dialog/user-form-dialog';
import { IToolbarChip, Toolbar } from '../../../../shared/components/toolbar/toolbar';
import { ConfirmDialog } from '../../../../shared/components/confirm-dialog/confirm-dialog';
import { UserListFeed } from './user-list-feed';
import { UserFormFlow } from './user-form-flow';
import { UserLockFlow } from './user-lock-flow';

const DEBOUNCE_MS = 300;
const DEFAULT_PAGE_SIZE = 10;

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
 *
 * ## Ba cộng tác viên — tách 2026-09-10 (doc/huong_dan/quy-uoc/fe-architecture.md §Chốt chặn
 * chống god component)
 *
 * Trang từng dài 579 dòng và ôm bốn máy trạng thái độc lập. Nay nó giữ đúng MỘT việc — dịch giữa
 * URL và bộ lọc, rồi quyết định khi nào phải tải lại — còn ba việc kia ở ba file cạnh bên:
 *
 * | Cộng tác viên | Giữ gì | Không biết gì |
 * |---|---|---|
 * | `UserListFeed`  | trạng thái lưới + dòng chảy request | URL, bộ lọc, hộp thoại |
 * | `UserFormFlow`  | hộp thoại tạo/sửa + ánh xạ lỗi form | lưới, bộ lọc |
 * | `UserLockFlow`  | xác nhận khoá + gọi khoá/mở khoá | lưới, bộ lọc, form |
 *
 * Cả ba khai trong `providers` của chính trang (KHÔNG `providedIn: 'root'`) nên vòng đời trùng
 * vòng đời trang — y hệt các field `signal()` mà chúng thay thế. Ranh giới chọn theo thứ **không
 * dùng chung trạng thái với nhau**, không theo số dòng: ba lớp này không đọc state của nhau, chỗ
 * duy nhất chúng gặp nhau là `afterWrite` — "ghi xong thì nạp lại danh sách".
 */
@Component({
  selector: 'app-quan-tri-nguoi-dung-page',
  standalone: true,
  imports: [Toolbar, ConfirmDialog, UserGridTable, UserFormDialog, TranslatePipe],
  templateUrl: './quan-tri-nguoi-dung.page.html',
  providers: [UserListFeed, UserFormFlow, UserLockFlow],
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

  private readonly currentUser = inject(CurrentUserService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  /** Ba cộng tác viên — xem bảng ở JSDoc của lớp. Template bind thẳng vào signal của chúng. */
  protected readonly feed = inject(UserListFeed);
  protected readonly form = inject(UserFormFlow);
  protected readonly lock = inject(UserLockFlow);

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

  private readonly lockConfirm = viewChild.required(ConfirmDialog);

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

  /**
   * Điểm hẹn DUY NHẤT giữa ba cộng tác viên: ghi xong (tạo/sửa/khoá/mở khoá) thì nạp lại danh sách.
   * Khai một lần thành arrow field để mọi nơi truyền đi cùng một tham chiếu, và để `this` không
   * phụ thuộc chỗ gọi.
   */
  private readonly afterWrite = (): void => this.reload();

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

    // `effect()` gọi API theo đúng bộ lọc/phân trang ĐANG Ở TRÊN URL — side-effect thật (gọi HTTP),
    // không derive state. Chạy ĐÚNG 1 LẦN mỗi khi `requestParams()` thực sự đổi giá trị.
    effect(() => {
      this.feed.load(this.requestParams());
    });
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
    this.feed.load(this.requestParams());
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
    this.form.openCreate();
  }

  openEditForm(user: IUser): void {
    this.form.openEdit(user);
  }

  onFormClosed(): void {
    this.form.close();
  }

  onFormSaved(event: IUserFormSaveEvent): void {
    this.form.save(event, this.afterWrite);
  }

  /**
   * KHOÁ hỏi lại, MỞ KHOÁ thì không. Ranh giới cố ý: khoá là chiều gây hậu quả (người kia mất
   * đường vào hệ thống) và hậu quả đó có phần người bấm không đoán được — độ trễ 30 phút của phiên
   * đang chạy; mở khoá là chiều khôi phục, chặn thêm một bước chỉ làm chậm việc sửa sai.
   */
  onToggleLock(user: IUser): void {
    if (user.IsLocked) {
      this.lock.unlock(user, this.afterWrite);
      return;
    }
    this.lock.arm(user);
    this.lockConfirm().open();
  }

  onLockConfirmed(): void {
    this.lock.confirm(this.afterWrite);
  }

  onLockCancelled(): void {
    this.lock.disarm();
  }
}
