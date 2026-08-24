import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { PermissionMatrix } from './permission-matrix';
import { IPermissionMatrixDto } from '../../models/phan-quyen.model';
import { mapPermissionMatrixDtoToModel } from '../../services/phan-quyen.mapper';

/**
 * PAYLOAD THẬT của `GET /api/admin/permissions`, giữ nguyên dạng chuỗi JSON và `JSON.parse` thay vì
 * khai object literal TypeScript — có chủ đích:
 *
 * BE bật `DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull`
 * (src/BE/PlatformManager.Api/Program.cs) nên `PermissionMatrixDto.ParentId` (`Guid?`) của menu GỐC
 * **không xuất hiện trong JSON**, chứ không phải `"parentId": null`. Object literal TS sẽ buộc phải
 * ghi `parentId: null` (type khai `string | null`) — tức là mô phỏng SAI đúng cái tình huống gây
 * lỗi, và test sẽ xanh giả. Đi qua `JSON.parse` mới tái hiện được `undefined` thật.
 */
const WIRE_JSON = `{
  "roles": ["SuperAdmin", "Admin", "User"],
  "rows": [
    { "sysMenuId": "m1", "sysMenuCode": "dashboard", "sysMenuName": "Dashboard", "assignedRoles": ["SuperAdmin", "Admin"] },
    { "sysMenuId": "m2", "sysMenuCode": "danh-muc", "sysMenuName": "Danh mục", "assignedRoles": ["SuperAdmin"] },
    { "sysMenuId": "m3", "sysMenuCode": "danh-muc-dti", "sysMenuName": "Danh mục DTI", "parentId": "m2", "assignedRoles": ["SuperAdmin"] },
    { "sysMenuId": "m4", "sysMenuCode": "quan-tri", "sysMenuName": "Quản trị", "assignedRoles": ["SuperAdmin"] },
    { "sysMenuId": "m5", "sysMenuCode": "quan-tri-nguoi-dung", "sysMenuName": "Người dùng", "parentId": "m4", "assignedRoles": ["SuperAdmin"] },
    { "sysMenuId": "m6", "sysMenuCode": "phan-quyen", "sysMenuName": "Phân quyền", "parentId": "m4", "assignedRoles": ["SuperAdmin"] }
  ]
}`;

function parseWirePayload(): IPermissionMatrixDto {
  return JSON.parse(WIRE_JSON) as IPermissionMatrixDto;
}

/**
 * Chốt chặn cho lỗi P0 đã xảy ra thật: ma trận PERM-1 render RỖNG HOÀN TOÀN.
 *
 * Chuỗi nhân quả: BE bỏ hẳn key `parentId` cho menu gốc → mapper gán thẳng `dto.parentId` nên
 * `ParentId` là `undefined` → `PermissionMatrix.toDisplayOrder()` gom `Map` theo key `undefined`
 * nhưng duyệt từ `visit(null, ...)` → không hàng gốc nào ra được, và vì hàng con chỉ được duyệt
 * QUA hàng cha nên cả bảng trống.
 *
 * Spec cũ (`permission-matrix.spec.ts`) nạp thẳng model `IPermissionRow` nên KHÔNG BAO GIỜ chạm
 * mapper — đó là đúng tầng test đang thiếu. Test này đi trọn đường wire → mapper → component.
 */
describe('PermissionMatrix (PERM-1) — payload BE thật, menu gốc KHÔNG có key `parentId`', () => {
  it('mapper chuẩn hoá `parentId` vắng mặt thành `null` (không để lọt `undefined`)', () => {
    const matrix = mapPermissionMatrixDtoToModel(parseWirePayload());

    const root = matrix.Rows.find((r) => r.SysMenuId === 'm1');
    expect(root?.ParentId).withContext('menu gốc phải là null, không phải undefined').toBeNull();
    // `toBeNull()` một mình KHÔNG đủ: `undefined == null` nhưng `undefined === null` là false, và
    // chính phép so `=== null`/`Map.get(null)` mới là chỗ đã gãy.
    expect(root?.ParentId === null).toBeTrue();

    const child = matrix.Rows.find((r) => r.SysMenuId === 'm3');
    expect(child?.ParentId).toBe('m2');
  });

  describe('ma trận dựng từ payload đó', () => {
    let fixture: ComponentFixture<PermissionMatrix>;

    beforeEach(() => {
      TestBed.configureTestingModule({ providers: [provideZonelessChangeDetection()] });
      const matrix = mapPermissionMatrixDtoToModel(parseWirePayload());

      fixture = TestBed.createComponent(PermissionMatrix);
      fixture.componentRef.setInput('rows', matrix.Rows);
      fixture.componentRef.setInput('roles', matrix.Roles);
      fixture.detectChanges();
    });

    function bodyRows(): HTMLTableRowElement[] {
      return Array.from(fixture.nativeElement.querySelectorAll('tbody tr'));
    }

    function firstCellText(row: HTMLTableRowElement): string {
      return (row.querySelector('td')?.textContent ?? '').replace('└', '').trim();
    }

    it('render ĐỦ 6 hàng — không rơi vào trạng thái rỗng', () => {
      expect(bodyRows().length).toBe(6);
      expect((fixture.nativeElement as HTMLElement).textContent).not.toContain('Chưa có mục menu nào.');
    });

    it('đúng thứ tự cha-trước-con, con nằm ngay sau cha và được thụt lề', () => {
      const rows = bodyRows();
      expect(rows.map(firstCellText)).toEqual([
        'Dashboard',
        'Danh mục',
        'Danh mục DTI',
        'Quản trị',
        'Người dùng',
        'Phân quyền',
      ]);

      const indents = rows.map((r) => r.querySelector('td')?.classList.contains('indent') ?? false);
      expect(indents).toEqual([false, false, true, false, true, true]);
    });

    it('checkbox vẫn bind đúng `AssignedRoles` của từng hàng sau khi qua mapper', () => {
      const dashboardRow = bodyRows()[0];
      const checks = Array.from(
        dashboardRow.querySelectorAll<HTMLInputElement>('input[type="checkbox"]'),
      ).map((c) => c.checked);
      // Cột theo `roles`: SuperAdmin, Admin, User
      expect(checks).toEqual([true, true, false]);
    });
  });
});
