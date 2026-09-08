// ===== Wire (DTO) — camelCase, xem doc/contracts/permissions.md =====

// `parentId?:` KHÔNG thừa — BE bật `DefaultIgnoreCondition = WhenWritingNull` nên menu GỐC
// (`ParentId` C# = null) đến FE là key VẮNG MẶT (`undefined`), không phải `null`. Khai
// `parentId: string | null` (thiếu `?`) từng khiến mapper gán thẳng `undefined` vào model, làm
// `PermissionMatrix.toDisplayOrder()` (so khớp bằng `=== null`/`Map.get(null)`) không nhận ra
// menu gốc nào — ma trận PERM-1 render RỖNG HOÀN TOÀN. Xem permission-matrix-wire.spec.ts.
export interface IPermissionRowDto {
  sysMenuId: string;
  sysMenuCode: string;
  sysMenuName: string;
  parentId?: string | null;
  assignedRoles: string[];
}

// `version` (thêm 2026-09-01) — token phiên bản của TOÀN BỘ ma trận, do BE băm từ chính dữ liệu
// (`MatrixVersion.Compute`, chuỗi hex 64 ký tự). FE giữ NGUYÊN chuỗi nhận được ở `GET` rồi gửi lại
// y hệt trong `PUT`; KHÔNG tự sinh, KHÔNG diễn giải nội dung, KHÔNG so sánh từng phần.
export interface IPermissionMatrixDto {
  roles: string[];
  rows: IPermissionRowDto[];
  version: string;
}

export interface ISavePermissionEntryDto {
  sysMenuId: string;
  roles: string[];
}

// `version` BẮT BUỘC, không phải tuỳ chọn: BE so khớp Ordinal với token nó tính lại từ DB, và
// `null`/vắng mặt KHÔNG BAO GIỜ khớp (hàm băm luôn trả chuỗi 64 ký tự, kể cả cho ma trận rỗng) ⇒
// mọi `PUT` thiếu khoá này nhận 409 và không ghi gì. Khai `string` (không `?`) để lỗi đó bị chặn
// từ lúc biên dịch — đúng chỗ nó rẻ nhất.
export interface ISavePermissionMatrixRequestDto {
  entries: ISavePermissionEntryDto[];
  version: string;
}

// ===== Model app — PascalCase + prefix I =====

export interface IPermissionRow {
  SysMenuId: string;
  SysMenuCode: string;
  SysMenuName: string;
  ParentId: string | null;
  AssignedRoles: string[];
}

export interface IPermissionMatrix {
  Roles: string[];
  Rows: IPermissionRow[];
  /** Chuỗi mờ (opaque) — chỉ để gửi lại nguyên văn ở `PUT`. Đừng hiển thị, đừng phân tích. */
  Version: string;
}

// ===== PERM-2 (tài nguyên) — xem doc/contracts/permissions.md CONTRACT PERM-2 =====
// ĐÃ NỐI VÀO 2026-08-29: `PhanQuyenService.getResourceMatrix()/saveResourceMatrix()` gọi thật
// `GET|PUT /api/admin/permissions/resources` (BE: `PermissionsController.GetResources/
// UpdateResources`, `ResourcePermissionMatrixDto.cs`). Shape dưới đây khớp 1:1 record C# đó.
//
// KHÁC PERM-1 ở hai điểm đừng nhầm: (1) danh sách PHẲNG, không có `parentId`; (2) ngữ nghĩa mặc
// định NGƯỢC nhau — PERM-1 vắng mặt = mở cho mọi người đã đăng nhập, PERM-2 vắng mặt = TỪ CHỐI.

export interface IResourcePermissionRowDto {
  resourceKey: string;
  resourceName: string;
  assignedRoles: string[];
}

export interface IResourcePermissionMatrixDto {
  roles: string[];
  rows: IResourcePermissionRowDto[];
  /** Cùng cơ chế `version` của PERM-1, nhưng băm trên bảng `RolePermissions` — hai token RIÊNG
   * biệt, không thay thế nhau. Gửi token của tab này sang endpoint kia luôn ra 409. */
  version: string;
}

export interface IResourcePermissionRow {
  ResourceKey: string;
  ResourceName: string;
  AssignedRoles: string[];
}

export interface IResourcePermissionMatrix {
  Roles: string[];
  Rows: IResourcePermissionRow[];
  Version: string;
}

// Request PUT — `entries` ghi ĐÈ TOÀN BỘ `RolePermission`, giống hệt PERM-1. Không có DTO nào cho
// response vì BE trả `data: true`.
export interface ISaveResourcePermissionEntryDto {
  resourceKey: string;
  roles: string[];
}

export interface ISaveResourcePermissionMatrixRequestDto {
  entries: ISaveResourcePermissionEntryDto[];
  version: string;
}
