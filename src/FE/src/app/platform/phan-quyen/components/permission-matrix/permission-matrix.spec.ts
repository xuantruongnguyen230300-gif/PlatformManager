import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { PermissionMatrix } from './permission-matrix';
import { IPermissionRow } from '../../models/phan-quyen.model';

const ROLES = ['SuperAdmin', 'Admin', 'User'];

const ROWS: IPermissionRow[] = [
  {
    SysMenuId: 'm1',
    SysMenuCode: 'phan-quyen',
    SysMenuName: 'Phân quyền',
    ParentId: null,
    AssignedRoles: ['SuperAdmin'],
  },
];

/**
 * CHỐT CHẶN CHỐNG NHẦM 2 MA TRẬN. PERM-1 ghi `SysMenuRole` (hiển thị menu) — KHÔNG dính break-glass
 * của `RequirePermissionFilter`, nên bỏ tick `SuperAdmin` ở đây VẪN có tác dụng thật và ô phải để
 * bấm được. Nếu ai đó "cho nhất quán" mà copy phần khoá cột từ `ResourcePermissionMatrix` sang đây,
 * test này đỏ ngay — đó là lỗi "UI nói sai về quyền" theo chiều ngược lại.
 */
describe('PermissionMatrix (PERM-1 — menu) — cột SuperAdmin KHÔNG bị khoá', () => {
  let fixture: ComponentFixture<PermissionMatrix>;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideZonelessChangeDetection()] });
    fixture = TestBed.createComponent(PermissionMatrix);
    fixture.componentRef.setInput('rows', ROWS);
    fixture.componentRef.setInput('roles', ROLES);
    fixture.detectChanges();
  });

  function cell(roleIndex: number): HTMLInputElement {
    return fixture.nativeElement.querySelectorAll('tbody tr input[type="checkbox"]')[
      roleIndex
    ] as HTMLInputElement;
  }

  it('ô SuperAdmin bấm được và bỏ tick vẫn phát permissionToggle', () => {
    const emitted: { SysMenuId: string; Role: string }[] = [];
    fixture.componentInstance.permissionToggle.subscribe((e) => emitted.push(e));

    expect(cell(0).checked).toBeTrue();
    expect(cell(0).disabled)
      .withContext('PERM-1 không có break-glass — không được khoá cột SuperAdmin')
      .toBeFalse();

    cell(0).dispatchEvent(new Event('change'));
    expect(emitted).toEqual([{ SysMenuId: 'm1', Role: 'SuperAdmin' }]);
  });

  it('không mượn chú thích break-glass của ma trận tài nguyên', () => {
    expect(fixture.nativeElement.querySelector('.always-note')).toBeNull();
    expect((fixture.nativeElement as HTMLElement).textContent).not.toContain('luôn có toàn quyền');
  });
});
