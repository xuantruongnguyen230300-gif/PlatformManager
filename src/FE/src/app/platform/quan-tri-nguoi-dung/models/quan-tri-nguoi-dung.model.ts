// ===== Wire (DTO) — camelCase, xem doc/contracts/users.md =====

export interface IUserDto {
  id: string;
  userName: string;
  // Nullable phía BE (`UserDto.Email`) — chốt sau một lượt core-reviewer (finding F3). Khu tài
  // liệu audit đã xoá nên không còn đường dẫn để trỏ tới; ràng buộc thật nằm ở
  // doc/contracts/users.md.
  email: string | null;
  fullName: string;
  roles: string[];
  isLocked: boolean;
  mustChangePassword: boolean;
  /**
   * `DateTimeOffset?` phía BE (`src/BE/Core/PlatformManager.Core.Application/Users/UserDto.cs:20`),
   * và `Program.cs` bật `DefaultIgnoreCondition = WhenWritingNull` — nên `null` phía C# về tới đây
   * là **key VẮNG MẶT** (`undefined`), không phải `null`. Vì vậy khai `?:` **và** `| null`: hai
   * hình dạng khác nhau cùng nghĩa "không có ngày tạo", mapper chuẩn hoá về một.
   */
  dateCreate?: string | null;
  /**
   * `ConcurrencyStamp` của Identity — token chống hai admin ghi đè lẫn nhau
   * (`UserDto.cs:21`, doc/contracts/users.md §"Quyết định người dùng 2026-08-30" quyết định 3).
   * Cùng lý do `?:` như `dateCreate`: `string?` + `WhenWritingNull` ⇒ vắng mặt chứ không `null`.
   */
  version?: string | null;
}

// `PagedList<UserDto>` dùng chung shape `IPagedResultDto<T>` (core/http/paged-result.model.ts) —
// shape phân trang đã CHỐT một bản duy nhất cho mọi endpoint list, xem
// doc/huong_dan/quy-uoc/be-cqrs-handler.md §"Shape phân trang" + doc/contracts/users.md. Trước
// đây có type riêng `IUserPagedListDto` (field `total`) vì tưởng contract Users khác — không còn
// đúng sau khi CHỐT, đã gộp lại dùng `IPagedResultDto<IUserDto>`/`IPagedResult<IUser>` chung.

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
  /** Xem `IUpdateUserPayload.Version`. Gửi `null` = xin BE bỏ qua bước kiểm tranh chấp ghi. */
  version: string | null;
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
  /** `null` = BE không trả ngày tạo. Đã chuẩn hoá khỏi `undefined` ở mapper — xem `IUserDto`. */
  DateCreate: string | null;
  /**
   * Token chống ghi đè, gửi NGUYÊN VĂN lại trong `PUT /api/users/{id}`; lệch ⇒ 409
   * `USER.VERSION_CONFLICT`. `null` = BE không cấp token (bản BE cũ) — lúc đó handler bỏ qua bước
   * kiểm và ta quay lại đúng hành vi ghi đè im lặng cũ, xem `IUpdateUserPayload.Version`.
   */
  Version: string | null;
}

export interface IUserListParams {
  Page: number;
  PageSize: number;
  SearchText?: string;
  /**
   * Lọc theo vai trò — tên role đúng casing của BE (`SuperAdmin`/`Admin`/`User`). Bỏ trống =
   * không lọc. `GET /api/users` nhận tham số này: CONTRACT USER-6 **AGREED** và BE đã hiện thực
   * hoá 2026-08-29 (`GetUsersListQuery.Role`, lọc bằng subquery ở tầng truy vấn) — xem
   * doc/contracts/users.md.
   * ⚠️ Casing phải khớp chính xác: giá trị lạ bị validator BE chặn bằng 400 `ValidationError`
   * kèm `fields.role`, không phải "trả rỗng".
   */
  Role?: string;
  /**
   * `true` = chỉ tài khoản đã khoá · `false` = chỉ tài khoản đang hoạt động · `undefined` = tất
   * cả. Cùng contract USER-6 với `Role` ở trên — BE đã bind và lọc thật từ 2026-08-29.
   */
  IsLocked?: boolean;
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
  /**
   * `Version` của **chính bản ghi đang mở form** (`IUser.Version`), gửi lại nguyên văn.
   *
   * Vì sao trường này bắt buộc chứ không tuỳ chọn: BE chỉ kiểm khi client THẬT SỰ gửi token
   * (`UpdateUserCommand.cs:62` — `cmd.Version is not null`), nên "quên truyền" không gây lỗi nào,
   * không đỏ test nào, chỉ lặng lẽ quay về đúng lỗ hổng ghi đè mà nó sinh ra để bịt. Khai bắt buộc
   * biến chỗ quên đó thành lỗi biên dịch.
   *
   * `null` là giá trị HỢP LỆ và có nghĩa riêng — "BE không cấp token cho bản ghi này" — chứ không
   * phải "chưa điền".
   */
  Version: string | null;
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
