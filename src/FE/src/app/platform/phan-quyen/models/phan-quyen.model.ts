// ===== Wire (DTO) — camelCase, xem doc/contracts/permissions.md =====

export interface IPermissionRowDto {
  sysMenuId: string;
  sysMenuCode: string;
  sysMenuName: string;
  parentId: string | null;
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
