import {
  IPermissionMatrix,
  IPermissionMatrixDto,
  IPermissionRow,
  IPermissionRowDto,
  IResourcePermissionMatrix,
  IResourcePermissionMatrixDto,
  IResourcePermissionRow,
  IResourcePermissionRowDto,
} from '../models/phan-quyen.model';

// ===== PERM-1 — ma trận menu (SysMenuRole) =====

function mapRowDtoToModel(dto: IPermissionRowDto): IPermissionRow {
  return {
    SysMenuId: dto.sysMenuId,
    SysMenuCode: dto.sysMenuCode,
    SysMenuName: dto.sysMenuName,
    ParentId: dto.parentId ?? null,
    AssignedRoles: dto.assignedRoles,
  };
}

/**
 * `Version` đi qua NGUYÊN VĂN — không cắt, không lowercase, không so sánh. Nó là chuỗi mờ do BE
 * băm từ dữ liệu; mọi "chuẩn hoá" ở đây đều biến nó thành token không bao giờ khớp lại.
 *
 * `?? ''` chỉ xử lý ca khoá VẮNG MẶT trên wire (BE cũ chưa trả `version`), không phải biến đổi giá
 * trị nhận được — cùng lý do với `assignedRoles ?? []` bên dưới: để `undefined` lọt vào một field
 * khai kiểu `string` là dạng nói dối về dữ liệu chạy thật mà TypeScript không bắt được. Chuỗi rỗng
 * KHÔNG BAO GIỜ khớp hàm băm hex 64 ký tự, nên ca đó hỏng to tiếng bằng 409 kèm lời mời tải lại,
 * chứ không âm thầm ghi đè lên trạng thái mình chưa đọc được.
 */
export function mapPermissionMatrixDtoToModel(dto: IPermissionMatrixDto): IPermissionMatrix {
  return { Roles: dto.roles, Rows: dto.rows.map(mapRowDtoToModel), Version: dto.version ?? '' };
}

// ===== PERM-2 — ma trận tài nguyên (RolePermission) =====

/**
 * `assignedRoles ?? []` KHÔNG thừa dù BE khai `IReadOnlyList<string>` không nullable: bộ
 * serializer bỏ qua giá trị null/mặc định ở một số cấu hình, và mảng vắng mặt sẽ tới FE thành
 * `undefined`. Nếu để lọt `undefined` vào `AssignedRoles` thì `row.AssignedRoles.includes(...)`
 * trong `ResourcePermissionMatrix` ném ngay lúc render — build vẫn xanh vì kiểu khai là `string[]`.
 * Đúng loại lỗi wire boundary mà fe-api-client.md mở đầu bằng cách cảnh báo.
 */
function mapResourceRowDtoToModel(dto: IResourcePermissionRowDto): IResourcePermissionRow {
  return {
    ResourceKey: dto.resourceKey,
    ResourceName: dto.resourceName,
    AssignedRoles: dto.assignedRoles ?? [],
  };
}

export function mapResourcePermissionMatrixDtoToModel(
  dto: IResourcePermissionMatrixDto,
): IResourcePermissionMatrix {
  return {
    Roles: dto.roles ?? [],
    Rows: (dto.rows ?? []).map(mapResourceRowDtoToModel),
    // Xem ghi chú `Version` ở `mapPermissionMatrixDtoToModel` — token của PERM-2 là token RIÊNG,
    // băm trên bảng `RolePermissions`.
    Version: dto.version ?? '',
  };
}
