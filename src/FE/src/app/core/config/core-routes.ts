import { EnvironmentProviders, InjectionToken, makeEnvironmentProviders } from '@angular/core';

/**
 * Ba đường dẫn mà tầng `core/` — **và `shared/`** — cần để trỏ tới ba màn hình nền tảng. Khai theo
 * **ngữ nghĩa** (màn đăng nhập / màn đổi mật khẩu / màn mặc định), không theo chuỗi đường dẫn — vì
 * chuỗi là thứ của riêng từng sản phẩm dựng trên nền tảng này, còn LUẬT "chưa đăng nhập thì về màn
 * đăng nhập" thì không.
 *
 * Không chỉ guard đọc nó: `httpErrorInterceptor` đọc `signIn` (401 giữa phiên), và Topbar đọc
 * `changePassword` cho một `routerLink`. 📖 `doc/huong_dan/quy-uoc/fe-routing-guard.md` §10.
 *
 * Vì sao phải tách: `core/` là CoreBase dùng lại cho sản phẩm khác (doc/kien-truc-core-module.md).
 * Sản phẩm sau có thể đặt `/login`, `/change-password`, `/home`, hoặc đường dẫn của một ngôn ngữ
 * khác hẳn. Trước 2026-09-02 ba guard khai cứng ba đường dẫn tiếng Việt của riêng dự án này, nên
 * dùng lại `core/` là phải SỬA VÀO TRONG `core/` — đúng thứ mà định nghĩa "CoreBase xong" loại trừ.
 *
 * Ràng buộc G9 (eslint.config.js) cấm `core/` import ngược lên `platform/`, nên chỗ duy nhất đặt
 * được hợp đồng này là chính `core/`; giá trị thì do app bơm vào từ `app.config.ts`.
 */
export interface ICoreRoutes {
  /**
   * Màn đăng nhập. Đích của `authGuard` (kèm `returnUrl`) và của `httpErrorInterceptor` khi phiên
   * chết giữa chừng (401).
   */
  readonly signIn: string;

  /**
   * Màn đổi mật khẩu.
   *
   * HAI người tiêu thụ, và đừng thu hẹp chú thích này về một: đích của `mustChangePasswordGuard`
   * khi cờ `mustChangePassword` bật (luồng **ép buộc**), **và** đích của liên kết `Đổi mật khẩu`
   * trên Topbar (luồng **tự nguyện** — lối vào duy nhất của app sau khi trang chủ cũ bị gỡ, xem
   * `shared/components/topbar/topbar.ts`).
   */
  readonly changePassword: string;

  /**
   * Màn mặc định sau khi đã đăng nhập — nơi "về chỗ an toàn" khi `roleGuard` thấy thiếu quyền.
   */
  readonly home: string;
}

/**
 * CỐ Ý không có `factory` mặc định. App quên khai `provideCoreRoutes()` thì Angular ném
 * `NullInjectorError` ngay lần điều hướng đầu tiên — ồn ào và chỉ đúng chỗ. Nếu đặt một mặc định
 * (vd `/login`) thì app quên khai sẽ chuyển hướng êm ru tới một route KHÔNG TỒN TẠI, rồi rơi vào
 * `**` — lỗi im lặng, không gate nào bắt được.
 */
export const CORE_ROUTES = new InjectionToken<ICoreRoutes>('CORE_ROUTES');

/**
 * Wire ở `app.config.ts`. Ba giá trị truyền vào PHẢI là đường dẫn có thật trong `app.routes.ts` —
 * ràng buộc đó được khoá bằng máy ở `src/FE/src/app/app.routes.spec.ts` (đường dẫn sai chỉ lộ ra
 * lúc chạy, và lộ ra dưới dạng "bấm vào thì về trang chủ", rất khó lần ra nguyên nhân).
 */
export function provideCoreRoutes(routes: ICoreRoutes): EnvironmentProviders {
  return makeEnvironmentProviders([{ provide: CORE_ROUTES, useValue: routes }]);
}
