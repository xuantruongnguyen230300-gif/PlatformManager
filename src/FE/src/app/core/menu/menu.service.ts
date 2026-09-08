import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, Signal, computed, inject, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { Observable, catchError, map, of, shareReplay, tap } from 'rxjs';
import { IApiResult, unwrapData } from '../http/api-result.model';
import { CurrentUserService } from '../../core/auth/current-user.service';
import { ICurrentUser } from '../../core/auth/current-user.model';
import { ToastService } from '../toast/toast.service';
import { IMenuItem, IMenuItemDto } from './menu-item.model';

/**
 * `?? null` cho field nullable — BE bật `DefaultIgnoreCondition = WhenWritingNull`
 * (src/BE/PlatformManager.Api/Program.cs) nên `parentId`/`icon`/`route` bằng `null` phía C# về tới
 * đây là **key vắng mặt** (`undefined`), không phải `null`. `buildMenuTree` bên dưới dùng
 * truthy-check nên vẫn chạy đúng kể cả khi là `undefined` — chuẩn hoá ở đây để model app đúng như
 * type đã khai (`string | null`), tránh lần sau ai đó so `=== null` thì gãy im lặng (đúng lỗi thật
 * đã xảy ra ở platform/phan-quyen/services/phan-quyen.mapper.ts).
 */
function mapMenuItemDtoToModel(dto: IMenuItemDto): IMenuItem {
  return {
    Id: dto.id,
    ParentId: dto.parentId ?? null,
    Code: dto.code,
    Label: dto.label,
    Icon: dto.icon ?? null,
    Route: dto.route ?? null,
    DisplayOrder: dto.displayOrder,
    Children: [],
  };
}

/** Dựng cây 1 cấp từ danh sách phẳng — item không có `ParentId` khớp nào trong tập hợp = root. */
function buildMenuTree(items: IMenuItem[]): IMenuItem[] {
  const byId = new Map(items.map((item) => [item.Id, item]));
  const sorted = [...items].sort((a, b) => a.DisplayOrder - b.DisplayOrder);
  const roots: IMenuItem[] = [];

  for (const item of sorted) {
    const parent = item.ParentId ? byId.get(item.ParentId) : undefined;
    if (parent) {
      parent.Children.push(item);
    } else {
      roots.push(item);
    }
  }
  return roots;
}

const ANONYMOUS_SESSION_KEY = '<anonymous>';

/**
 * Khoá dịch của câu báo "không tải được menu". Chuỗi viết trong `core/` không thuộc màn nào nên
 * đoạn giữa lấy từ TÊN DỊCH VỤ chứa nó (`menu.service.ts` → `menu`) — cùng khuôn
 * `shared.httpError.*` của `http-error.interceptor.ts`, xem
 * doc/huong_dan/wiki-core/fe/08-i18n.md §Khuôn khoá dịch §2, đoạn "Chuỗi viết trong `shared/`
 * hoặc `core/`".
 */
const MENU_LOAD_FAILED_KEY = 'shared.menu.loadFailed';

/**
 * Khoá định danh PHIÊN đăng nhập mà một bản menu thuộc về. Gồm cả `Roles` vì BE lọc `SysMenuRole`
 * theo role — cùng 1 user nhưng role đổi thì menu cũng phải khác, không được dùng lại bản cũ.
 */
function sessionKeyOf(user: ICurrentUser | null): string {
  if (!user) return ANONYMOUS_SESSION_KEY;
  return `${user.Id}|${[...user.Roles].sort().join(',')}`;
}

interface IMenuCacheEntry {
  SessionKey: string;
  Items: IMenuItem[];
}

/**
 * `GET /api/meta/menu` — xem doc/contracts/meta-menu.md. Dùng cho `Sidebar` (thuộc
 * ngoại lệ "app-shell", xem doc/huong_dan/wiki-core/fe/05-component-library.md) — sidebar KHÔNG
 * hard-code menu, luôn tải động theo role hiện tại (BE đã lọc `SysMenuRole` sẵn).
 *
 * CACHE (finding B3, doc/huong_dan/wiki-core/be/11-performance-caching.md §6.3): menu là dữ liệu
 * đọc-nhiều/ghi-hiếm nhưng `Sidebar` bị dựng lại mỗi lần app-shell bật/tắt (route khai
 * `data: { noShell: true }` — hai màn auth, xem `app.ts`) → trước đây bắn lại `GET /meta/menu`
 * mỗi lần. Nay cache
 * trong `signal()` theo đúng tiền lệ `MetadataService`
 * (doc/huong_dan/wiki-core/fe/11-grid-and-metadata.md) — CHƯA cần `signalStore()` vì state chỉ là
 * 1 danh sách đọc-nhiều, không có derive phức tạp (ngưỡng ở fe/03-state-management.md).
 *
 * 🔴 Cache gắn CHẶT với phiên đăng nhập — 2 lớp bảo vệ, cố ý trùng nhau:
 * 1. Entry cache mang `SessionKey`; `menu()`/`getMenu()` chỉ chấp nhận entry khớp phiên HIỆN TẠI.
 *    Đây là lớp bảo đảm chính: dù ai quên gọi `invalidate()`, menu của user A vẫn KHÔNG BAO GIỜ
 *    đọc được khi đang là user B (rò rỉ thông tin phân quyền, không phải lỗi hiển thị nhỏ).
 * 2. `AuthService.login()/logout()` gọi `invalidate()` để xoá sớm khỏi bộ nhớ, không đợi tới lần
 *    `getMenu()` kế tiếp.
 *
 * ## Đường lùi — vì sao service NÀY nuốt lỗi, còn service nghiệp vụ thì không (chốt 2026-09-08)
 *
 * `getMenu()` KHÔNG BAO GIỜ đi ra nhánh `error`: hỏng thì nó toast một lần rồi trả `[]`. Đây là
 * NGOẠI LỆ có điều kiện với luật chung ở doc/huong_dan/quy-uoc/fe-api-client.md §Đường lùi —
 * cấp cho thành phần hạ tầng dùng chung mà hỏng nó không được phép chặn cả ứng dụng. Menu là
 * khung điều hướng của mọi màn hình; ném lỗi ra từ đây nghĩa là mỗi nơi gọi (kể cả
 * `menu.refresh().subscribe()` trần ở `platform/phan-quyen/pages/phan-quyen/phan-quyen.page.ts`)
 * phải tự dựng lại cùng một handler, và quên một chỗ là một lỗi không ai bắt.
 *
 * Điều kiện đi kèm, KHÔNG được bỏ: lỗi phải **hiện ra đúng một lần** (toast) trước khi trả `[]`.
 * Đây chính là chỗ khác khuôn cũ (toán tử `??` kèm một mảng rỗng dựng sẵn): khuôn đó cũng trả
 * `[]`, nhưng không nói gì với ai.
 */
@Injectable({ providedIn: 'root' })
export class MenuService {
  private readonly http = inject(HttpClient);
  private readonly currentUserService = inject(CurrentUserService);
  private readonly toast = inject(ToastService);
  private readonly translate = inject(TranslateService);

  private readonly entry = signal<IMenuCacheEntry | null>(null);

  /**
   * Request đang bay — gộp nhiều người gọi đồng thời (vd `Sidebar` dựng lại trong lúc request đầu
   * chưa về) vào ĐÚNG 1 lần gọi HTTP. Không phải signal vì chỉ là chi tiết điều phối, không có UI
   * nào đọc.
   */
  private inFlight: { Token: object; SessionKey: string; Request$: Observable<IMenuItem[]> } | null = null;

  /**
   * Menu của phiên hiện tại, `[]` khi chưa tải xong hoặc entry cache thuộc phiên khác. Component
   * app-shell đọc signal này thay vì tự giữ bản sao — nhờ vậy `refresh()` (sau khi lưu phân quyền)
   * đẩy được menu mới ra sidebar ngay, không cần F5.
   */
  readonly menu: Signal<IMenuItem[]> = computed(() => {
    const entry = this.entry();
    return entry !== null && entry.SessionKey === this.currentSessionKey() ? entry.Items : [];
  });

  private currentSessionKey(): string {
    return sessionKeyOf(this.currentUserService.currentUser());
  }

  /** Trả cache nếu còn hợp lệ cho phiên hiện tại; nếu không thì gọi API và ghi cache. */
  getMenu(): Observable<IMenuItem[]> {
    const sessionKey = this.currentSessionKey();

    const entry = this.entry();
    if (entry !== null && entry.SessionKey === sessionKey) {
      return of(entry.Items);
    }

    if (this.inFlight !== null && this.inFlight.SessionKey === sessionKey) {
      return this.inFlight.Request$;
    }

    // Danh tính riêng của lần gọi này — so sánh bằng tham chiếu nên chính xác tuyệt đối, kể cả
    // khi phiên A → B → A lặp lại nhanh (so khoá phiên thôi thì 2 lần gọi của A trông giống nhau).
    const token = {};

    /** Request này có còn là request đang chờ của service không (chưa bị lần gọi khác thay chỗ). */
    const isStillMine = (): boolean => this.inFlight !== null && this.inFlight.Token === token;

    const request$ = this.http.get<IApiResult<IMenuItemDto[]>>('/meta/menu').pipe(
      // `unwrapData` chứ KHÔNG toán tử `??` kèm một mảng rỗng dựng sẵn (khuôn cũ, đổi 2026-09-08):
      // một envelope hỏng và một tài khoản không được cấp menu nào cho ra CÙNG một hình ảnh —
      // sidebar rỗng — nên khuôn đó biến lỗi hạ tầng thành một câu trả lời trông hợp lệ. Xem
      // doc/huong_dan/quy-uoc/fe-api-client.md §"cũng bị cấm — và đây là dạng nguy hiểm nhất".
      map((res) => buildMenuTree(unwrapData(res).map(mapMenuItemDtoToModel))),
      tap((items) => {
        // Response của phiên CŨ về muộn (user đã logout/đăng nhập tài khoản khác trong lúc chờ)
        // KHÔNG được ghi đè cache của phiên hiện tại — hậu quả tuy nhẹ (lớp khoá phiên vẫn chặn
        // đọc nhầm, chỉ là sidebar rỗng tạm + 1 request thừa) nhưng không có lý do gì để giữ.
        if (this.currentSessionKey() === sessionKey) {
          this.entry.set({ SessionKey: sessionKey, Items: items });
        }
        // Chỉ dọn `inFlight` nếu nó VẪN là của mình — nếu không, ta sẽ xoá mất request đang chờ
        // của phiên mới và làm nó bị gọi lại lần nữa.
        if (isStillMine()) this.inFlight = null;
      }),
      // Đường lùi đặt NGAY TẠI ĐÂY, không lan lỗi ra nơi gọi — xem khối tài liệu của lớp ở trên
      // (§"Đường lùi") để biết vì sao service này được phép, còn service nghiệp vụ thì không.
      //
      // Lỗi vẫn KHÔNG được cache: xoá `inFlight` để lần gọi sau thử lại thật. `entry` cũng không
      // bị ghi — `tap` nằm TRƯỚC `catchError` nên nó không chạy ở nhánh này.
      catchError((err: unknown) => {
        if (isStillMine()) this.inFlight = null;

        // "Hiện ra ĐÚNG MỘT LẦN": lỗi HTTP đã được `httpErrorInterceptor` biến thành toast rồi
        // (hoặc cố ý im lặng — 401 phiên chết đang điều hướng về màn đăng nhập, thêm một toast
        // "không tải được menu" chỉ là nhiễu). Toast của service này dành cho nhánh CÒN LẠI: lỗi
        // sinh sau interceptor — envelope 200 nhưng thiếu `data` (`unwrapData` ném), mapper hỏng —
        // vốn là nhánh trước đây không có tiếng nói nào.
        if (!(err instanceof HttpErrorResponse)) {
          this.toast.error(this.translate.instant(MENU_LOAD_FAILED_KEY) as string);
        }

        return of<IMenuItem[]>([]);
      }),
      shareReplay({ bufferSize: 1, refCount: false }),
    );

    this.inFlight = { Token: token, SessionKey: sessionKey, Request$: request$ };
    return request$;
  }

  /** Xoá cache — gọi khi đổi phiên đăng nhập (`AuthService`) hoặc trước khi ép tải lại. */
  invalidate(): void {
    this.entry.set(null);
    this.inFlight = null;
  }

  /** Ép tải lại ngay (dùng sau khi lưu phân quyền màn hình thành công). */
  refresh(): Observable<IMenuItem[]> {
    this.invalidate();
    return this.getMenu();
  }
}
