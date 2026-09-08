import {
  IPermissionMatrixDto,
  IResourcePermissionMatrixDto,
} from '../models/phan-quyen.model';
import {
  mapPermissionMatrixDtoToModel,
  mapResourcePermissionMatrixDtoToModel,
} from './phan-quyen.mapper';

/**
 * Ranh giới WIRE của màn Phân quyền — nơi dễ vỡ nhất theo fe-ui-conventions.md §Testing: TypeScript
 * bị xoá lúc chạy, nên đổi tên field DTO mà quên mapper là lỗi im lặng, build vẫn xanh.
 *
 * Payload đi qua `JSON.parse` chứ không khai object literal TS — cùng lý do đã ghi ở
 * `permission-matrix-wire.spec.ts`: BE bật `DefaultIgnoreCondition = WhenWritingNull` nên field
 * mang giá trị mặc định/`null` KHÔNG xuất hiện trong JSON, còn literal TS thì buộc phải ghi ra và
 * sẽ mô phỏng sai đúng tình huống gây lỗi.
 */
describe('phan-quyen.mapper — PERM-1 (ma trận menu)', () => {
  it('đổi camelCase DTO sang PascalCase model, giữ nguyên thứ tự hàng', () => {
    const dto = JSON.parse(`{
      "roles": ["SuperAdmin", "Admin"],
      "rows": [
        { "sysMenuId": "m1", "sysMenuCode": "quan-tri", "sysMenuName": "Quản trị", "assignedRoles": ["SuperAdmin"] },
        { "sysMenuId": "m2", "sysMenuCode": "phan-quyen", "sysMenuName": "Phân quyền", "parentId": "m1", "assignedRoles": [] }
      ],
      "version": "9f2c"
    }`) as IPermissionMatrixDto;

    const model = mapPermissionMatrixDtoToModel(dto);

    expect(model.Roles).toEqual(['SuperAdmin', 'Admin']);
    expect(model.Rows.map((r) => r.SysMenuId)).toEqual(['m1', 'm2']);
    expect(model.Rows[0]).toEqual({
      SysMenuId: 'm1',
      SysMenuCode: 'quan-tri',
      SysMenuName: 'Quản trị',
      ParentId: null,
      AssignedRoles: ['SuperAdmin'],
    });
    expect(model.Rows[1].ParentId).toBe('m1');
    expect(model.Version).withContext('token phiên bản phải đi qua nguyên văn').toBe('9f2c');
  });

  /**
   * `version` là chuỗi mờ dùng để SO KHỚP CHÍNH XÁC ở BE (`string.Equals(..., Ordinal)`), nên mọi
   * "dọn dẹp" — trim, lowercase, cắt bớt — đều biến nó thành token không bao giờ khớp lại, và triệu
   * chứng là 409 ở mọi lần lưu. Test này khoá đúng chỗ đó lại.
   */
  it('KHÔNG đụng vào nội dung `version`: hoa/thường và khoảng trắng giữ y nguyên', () => {
    const raw = '  A1B2c3  ';
    const dto = JSON.parse(`{ "roles": [], "rows": [], "version": "${raw}" }`) as IPermissionMatrixDto;

    expect(mapPermissionMatrixDtoToModel(dto).Version).toBe(raw);
  });

  it('`version` VẮNG MẶT (BE cũ) ra chuỗi rỗng, không phải undefined', () => {
    // Chuỗi rỗng không bao giờ khớp hàm băm hex 64 ký tự ⇒ `PUT` nhận 409 và người dùng được mời
    // tải lại. `undefined` lọt vào một field khai `string` thì lỗi đi xa hơn rồi mới nổ.
    const dto = JSON.parse(`{ "roles": ["SuperAdmin"], "rows": [] }`) as IPermissionMatrixDto;

    expect(mapPermissionMatrixDtoToModel(dto).Version).toBe('');
  });
});

describe('phan-quyen.mapper — PERM-2 (ma trận tài nguyên)', () => {
  /** Payload đúng như `GET /api/admin/permissions/resources` trả về (ResourcePermissionMatrixDto). */
  const WIRE_JSON = `{
    "roles": ["SuperAdmin", "Admin", "User"],
    "rows": [
      { "resourceKey": "import.manage", "resourceName": "Import CSV/Excel", "assignedRoles": ["Admin"] }
    ],
    "version": "7ac9"
  }`;

  it('đổi camelCase DTO sang PascalCase model — SAI tên field là hỏng lúc chạy, không phải lúc build', () => {
    const model = mapResourcePermissionMatrixDtoToModel(
      JSON.parse(WIRE_JSON) as IResourcePermissionMatrixDto,
    );

    expect(model.Roles).toEqual(['SuperAdmin', 'Admin', 'User']);
    expect(model.Rows).toEqual([
      {
        ResourceKey: 'import.manage',
        ResourceName: 'Import CSV/Excel',
        AssignedRoles: ['Admin'],
      },
    ]);
    // Token của PERM-2 là token RIÊNG (băm trên bảng `RolePermissions`) — gửi nhầm sang endpoint
    // của PERM-1 luôn ra 409.
    expect(model.Version).toBe('7ac9');
  });

  it('KHÔNG mượn shape của PERM-1: model không có SysMenuId/ParentId', () => {
    const model = mapResourcePermissionMatrixDtoToModel(
      JSON.parse(WIRE_JSON) as IResourcePermissionMatrixDto,
    );
    // Hai ma trận từng suýt bị "gom chung cho gọn"; PERM-2 là danh sách PHẲNG, không có cây.
    expect(Object.keys(model.Rows[0]).sort()).toEqual([
      'AssignedRoles',
      'ResourceKey',
      'ResourceName',
    ]);
  });

  it('`assignedRoles` VẮNG MẶT trong JSON vẫn ra mảng rỗng, không phải undefined', () => {
    // Đây là lý do mapper có `?? []`. Để lọt `undefined` thì `AssignedRoles.includes()` trong
    // `ResourcePermissionMatrix` ném ngay lúc render, trong khi kiểu khai là `string[]` nên
    // TypeScript không hề báo.
    const dto = JSON.parse(`{
      "roles": ["SuperAdmin"],
      "rows": [{ "resourceKey": "import.manage", "resourceName": "Import CSV/Excel" }]
    }`) as IResourcePermissionMatrixDto;

    const model = mapResourcePermissionMatrixDtoToModel(dto);

    expect(model.Rows[0].AssignedRoles).toEqual([]);
    expect(model.Rows[0].AssignedRoles).not.toBeUndefined();
  });

  it('`rows` vắng mặt hoàn toàn (chưa có ResourceKey nào) không làm vỡ màn hình', () => {
    const dto = JSON.parse(`{ "roles": ["SuperAdmin"], "version": "0000" }`) as IResourcePermissionMatrixDto;

    expect(mapResourcePermissionMatrixDtoToModel(dto)).toEqual({
      Roles: ['SuperAdmin'],
      Rows: [],
      Version: '0000',
    });
  });
});
