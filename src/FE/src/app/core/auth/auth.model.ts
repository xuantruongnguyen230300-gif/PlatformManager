// Request DTO (camelCase, FLAT) — xem doc/contracts/auth.md.
export interface ILoginRequestDto {
  userName: string;
  password: string;
}

export interface IChangePasswordRequestDto {
  currentPassword: string;
  newPassword: string;
}
