import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { CORE_ROUTES } from '../config/core-routes';
import { CurrentUserService } from './current-user.service';

/**
 * Factory guard theo role — KHÔNG viết tay từng guard một.
 * Xem doc/huong_dan/quy-uoc/fe-routing-guard.md §5.
 *
 * Thiếu quyền → điều hướng về màn mặc định (`CORE_ROUTES.home`), KHÔNG có trang 403 (hành vi đã
 * chốt ở doc/Design/Frontend/PlatformManager/Screens/04-phan-quyen.md §States).
 *
 * Luôn đặt SAU `authGuard` và `mustChangePasswordGuard` trong `canActivate` (thứ tự §6) — đảo
 * lên trước sẽ đá người CHƯA đăng nhập về màn mặc định thay vì màn đăng nhập.
 *
 * Ẩn UI theo role KHÔNG thay cho kiểm quyền phía BE: sidebar lọc menu, guard chặn URL gõ tay, BE
 * vẫn phải trả 403 — ba lớp độc lập.
 *
 * Đường dẫn màn mặc định đến từ `CORE_ROUTES` (core/config/core-routes.ts), KHÔNG khai cứng ở
 * đây — `core/` giữ luật chuyển hướng, app cung cấp đường dẫn.
 */
export const roleGuard = (...roles: string[]): CanActivateFn => {
  return () => {
    const currentUser = inject(CurrentUserService);
    const router = inject(Router);
    const coreRoutes = inject(CORE_ROUTES);

    return currentUser.hasAnyRole(...roles) ? true : router.createUrlTree([coreRoutes.home]);
  };
};

/** "Quản trị hệ thống > Người dùng" — khớp BE `[Authorize(Roles="SuperAdmin,Admin")]`, xem doc/contracts/users.md. */
export const adminGuard = roleGuard('Admin', 'SuperAdmin');

/**
 * "Quản trị hệ thống > Phân quyền" — CHỈ `SuperAdmin`, kể cả `Admin` cũng bị chặn (khớp BE
 * `[Authorize(Roles="SuperAdmin")]`, xem doc/contracts/permissions.md). Đây là biện pháp chống
 * leo thang quyền qua UI — đừng "sửa cho tiện" thành `adminGuard`.
 */
export const superAdminGuard = roleGuard('SuperAdmin');
