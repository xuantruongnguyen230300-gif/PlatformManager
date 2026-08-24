import { HttpClient } from '@angular/common/http';
import { Injectable, Signal, computed, inject, signal } from '@angular/core';
import { Observable, catchError, map, of, shareReplay, tap, throwError } from 'rxjs';
import { IApiResult } from '../http/api-result.model';
import { CurrentUserService } from '../../core/auth/current-user.service';
import { ICurrentUser } from '../../core/auth/current-user.model';
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
 * đọc-nhiều/ghi-hiếm nhưng `Sidebar` bị dựng lại mỗi lần app-shell bật/tắt (route `noShell` của
 * `login`/`doi-mat-khau`, xem `app.ts`) → trước đây bắn lại `GET /meta/menu` mỗi lần. Nay cache
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
 */
@Injectable({ providedIn: 'root' })
export class MenuService {
  private readonly http = inject(HttpClient);
  private readonly currentUserService = inject(CurrentUserService);

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
      map((res) => buildMenuTree((res.data ?? []).map(mapMenuItemDtoToModel))),
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
      // Lỗi KHÔNG được cache: xoá `inFlight` để lần gọi sau thử lại thật (sidebar hiện rỗng lúc
      // này, `httpErrorInterceptor` đã lo toast) — không nuốt lỗi ở đây, nơi gọi tự quyết định.
      catchError((err: unknown) => {
        if (isStillMine()) this.inFlight = null;
        return throwError(() => err);
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
