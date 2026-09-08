// Request DTO (camelCase, FLAT) — xem doc/contracts/auth.md.
export interface ILoginRequestDto {
  userName: string;
  password: string;
  /**
   * Tuổi thọ cookie phiên do NGƯỜI DÙNG chọn (chốt 2026-08-31, doc/contracts/auth.md
   * §`POST /api/auth/login`): `false` ⇒ cookie phiên, chết khi đóng trình duyệt; `true` ⇒ cookie
   * 14 ngày trượt. KHÔNG optional: bỏ trống thì BE rơi về `default(bool)` mà không ai đọc được
   * ý định của người dùng từ payload — luôn gửi tường minh.
   */
  rememberMe: boolean;
}

export interface IChangePasswordRequestDto {
  currentPassword: string;
  newPassword: string;
}
