import { IPermissionMatrix, IPermissionMatrixDto, IPermissionRow, IPermissionRowDto } from '../models/phan-quyen.model';

function mapRowDtoToModel(dto: IPermissionRowDto): IPermissionRow {
  return {
    SysMenuId: dto.sysMenuId,
    SysMenuCode: dto.sysMenuCode,
    SysMenuName: dto.sysMenuName,
    ParentId: dto.parentId,
    AssignedRoles: dto.assignedRoles,
  };
}

export function mapPermissionMatrixDtoToModel(dto: IPermissionMatrixDto): IPermissionMatrix {
  return { Roles: dto.roles, Rows: dto.rows.map(mapRowDtoToModel) };
}
