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

export interface IPermissionMatrixDto {
  roles: string[];
  rows: IPermissionRowDto[];
}

export interface ISavePermissionEntryDto {
  sysMenuId: string;
  roles: string[];
}

export interface ISavePermissionMatrixRequestDto {
  entries: ISavePermissionEntryDto[];
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
}

// ===== PERM-2 (tài nguyên) — xem doc/contracts/permissions.md CONTRACT PERM-2 =====
// Status: DRAFT (chưa AGREED) — type khai ở đây chỉ để `ResourcePermissionMatrix` compile được,
// KHÔNG có service/mapper đi kèm cho tới khi contract chuyển AGREED. Đừng tự chế service gọi
// `/api/admin/permissions/resources` dựa trên các type này.

export interface IResourcePermissionRowDto {
  resourceKey: string;
  resourceName: string;
  assignedRoles: string[];
}

export interface IResourcePermissionMatrixDto {
  roles: string[];
  rows: IResourcePermissionRowDto[];
}

export interface IResourcePermissionRow {
  ResourceKey: string;
  ResourceName: string;
  AssignedRoles: string[];
}

export interface IResourcePermissionMatrix {
  Roles: string[];
  Rows: IResourcePermissionRow[];
}
