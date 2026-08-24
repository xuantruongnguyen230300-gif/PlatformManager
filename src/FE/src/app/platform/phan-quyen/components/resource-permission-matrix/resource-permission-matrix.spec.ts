import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { ResourcePermissionMatrix } from './resource-permission-matrix';
import { IResourcePermissionRow } from '../../models/phan-quyen.model';

const ROLES = ['SuperAdmin', 'Admin', 'User'];

const ROWS: IResourcePermissionRow[] = [
  // CÓ CHỦ ĐÍCH: `SuperAdmin` KHÔNG nằm trong `AssignedRoles` — đúng trạng thái hợp lệ mà BE có
  // thể trả về (permissions.md dòng 128: `SuperAdmin` không cần dòng `RolePermission` nào).
  { ResourceKey: 'criteria.manage', ResourceName: 'Quản lý chỉ tiêu', AssignedRoles: ['Admin'] },
  { ResourceKey: 'import.manage', ResourceName: 'Import CSV/Excel', AssignedRoles: [] },
];

/**
 * Ma trận PERM-2 (tài nguyên) — BE có break-glass cho `SuperAdmin` (`RequirePermissionFilter`, từ
 * 2026-08-19), nên bỏ tick cột đó KHÔNG thu hồi được quyền. UI phải nói đúng điều đó: ô tick sẵn +
 * khoá + chú thích. Cặp đôi bắt buộc của spec này là `permission-matrix.spec.ts` (PERM-1 — cột
 * `SuperAdmin` VẪN phải bấm được); sửa 1 bên mà quên bên kia sẽ làm bên kia đỏ.
 */
describe('ResourcePermissionMatrix — cột SuperAdmin (break-glass)', () => {
  let fixture: ComponentFixture<ResourcePermissionMatrix>;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideZonelessChangeDetection()] });
    fixture = TestBed.createComponent(ResourcePermissionMatrix);
    fixture.componentRef.setInput('rows', ROWS);
    fixture.componentRef.setInput('roles', ROLES);
    fixture.detectChanges();
  });

  /** Ô tick của hàng `rowIndex` ở cột `roleIndex` (0 = SuperAdmin theo `ROLES`). */
  function cell(rowIndex: number, roleIndex: number): HTMLInputElement {
    const row = fixture.nativeElement.querySelectorAll('tbody tr')[rowIndex] as HTMLElement;
    return row.querySelectorAll('input[type="checkbox"]')[roleIndex] as HTMLInputElement;
  }

  it('mọi ô cột SuperAdmin đều đã tick + disabled, kể cả hàng BE không gán role đó', () => {
    for (const rowIndex of [0, 1]) {
      expect(cell(rowIndex, 0).checked)
        .withContext(`hàng ${rowIndex} cột SuperAdmin phải hiển thị đã tick`)
        .toBeTrue();
      expect(cell(rowIndex, 0).disabled)
        .withContext(`hàng ${rowIndex} cột SuperAdmin phải bị khoá`)
        .toBeTrue();
    }
  });

  it('các cột role khác vẫn bấm được và vẫn phát permissionToggle', () => {
    const emitted: { ResourceKey: string; Role: string }[] = [];
    fixture.componentInstance.permissionToggle.subscribe((e) => emitted.push(e));

    // Admin (cột 1) — đang tick, còn User (cột 2) — chưa tick: cả hai đều phải mở.
    expect(cell(0, 1).disabled).toBeFalse();
    expect(cell(0, 1).checked).toBeTrue();
    expect(cell(0, 2).disabled).toBeFalse();
    expect(cell(0, 2).checked).toBeFalse();

    cell(0, 2).dispatchEvent(new Event('change'));
    expect(emitted).toEqual([{ ResourceKey: 'criteria.manage', Role: 'User' }]);
  });

  it('có chú thích cho người quản trị: gỡ role ở màn Người dùng mới là cách thu quyền', () => {
    const note = fixture.nativeElement.querySelector('.always-note') as HTMLElement | null;
    expect(note).withContext('thiếu chú thích giải thích vì sao cột bị khoá').not.toBeNull();
    expect(note!.textContent).toContain('SuperAdmin');
    expect(note!.textContent).toContain('Người dùng');
  });

  it('BE không trả role SuperAdmin → không khoá cột nào, không hiện chú thích', () => {
    fixture.componentRef.setInput('roles', ['Admin', 'User']);
    fixture.detectChanges();

    expect(cell(0, 0).disabled).toBeFalse();
    expect(fixture.nativeElement.querySelector('.always-note')).toBeNull();
  });

  it('loading = true khoá hết mọi ô (hành vi cũ không bị phá)', () => {
    fixture.componentRef.setInput('loading', true);
    fixture.detectChanges();

    expect(cell(0, 1).disabled).toBeTrue();
    expect(cell(0, 2).disabled).toBeTrue();
  });
});
