// ===== Wire (DTO) — camelCase, xem doc/contracts/users.md =====

export interface IUserDto {
  id: string;
  userName: string;
  // Nullable phía BE (`UserDto.Email`) — khớp core-reviewer audit F3, xem
  // doc/huong_dan/wiki-core/audit/2026-08-16-fe.md.
  email: string | null;
  fullName: string;
  roles: string[];
  isLocked: boolean;
  mustChangePassword: boolean;
  dateCreate: string;
}

// `PagedList<UserDto>` — LƯU Ý: khác `IPagedResultDto<T>` (core/http/paged-result.model.ts, dùng
// cho danh-muc-dti) — contract Users dùng field `total` (không phải `totalCount`) và KHÔNG có
// `totalPages`, xem doc/contracts/users.md. Không tái dùng nhầm type chung.
export interface IUserPagedListDto {
  items: IUserDto[];
  total: number;
  page: number;
  pageSize: number;
}

export interface ICreateUserRequestDto {
  userName: string;
  email: string;
  fullName: string;
  tempPassword: string;
  roles: string[];
}

export interface IUpdateUserRequestDto {
  email: string;
  fullName: string;
  roles: string[];
}

// ===== Model app — PascalCase + prefix I =====

export interface IUser {
  Id: string;
  UserName: string;
  Email: string | null;
  FullName: string;
  Roles: string[];
  IsLocked: boolean;
  MustChangePassword: boolean;
  DateCreate: string;
}

export interface IUserPagedList {
  Items: IUser[];
  Total: number;
  Page: number;
  PageSize: number;
}

export interface IUserListParams {
  Page: number;
  PageSize: number;
  SearchText?: string;
}

export interface ICreateUserPayload {
  UserName: string;
  Email: string;
  FullName: string;
  TempPassword: string;
  Roles: string[];
}

export interface IUpdateUserPayload {
  Email: string;
  FullName: string;
  Roles: string[];
}

/**
 * Role có thể gán qua màn hình này — CHỦ ĐỘNG bỏ `SuperAdmin` (xem
 * doc/ke-hoach-xay-lai-corebase.md §"Mô hình phân quyền": "SuperAdmin là vai trò dành riêng cho
 * tài khoản khởi tạo/vận hành hệ thống, không cấp đại trà") — dù BE gate controller này cho cả
 * `Admin`+`SuperAdmin` (2 role đều vào được màn), KHÔNG có nghĩa Admin được phép tự cấp
 * `SuperAdmin` cho người khác qua đây. Quyết định của frontend-expert, ghi rõ để dễ xem lại nếu
 * cần đổi.
 */
export const ASSIGNABLE_ROLES = ['Admin', 'User'] as const;
