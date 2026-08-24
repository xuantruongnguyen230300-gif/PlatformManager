import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { UserFormDialog, IUserFormSaveEvent } from './user-form-dialog';
import { CurrentUserService } from '../../../../core/auth/current-user.service';
import { IUser } from '../../models/quan-tri-nguoi-dung.model';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';

function aUser(roles: string[]): IUser {
  return {
    Id: 'u1',
    UserName: 'nguyen.van.a',
    Email: 'a@congty.vn',
    FullName: 'Nguyễn Văn A',
    Roles: roles,
    IsLocked: false,
    MustChangePassword: false,
    DateCreate: '2026-08-19T00:00:00Z',
  };
}

/**
 * `PUT /api/users/{id}` nhận `roles` TRỌN GÓI và BE gỡ mọi role không có trong payload
 * (doc/contracts/users.md §"Luật cấp/gỡ role SuperAdmin", BE enforce từ 2026-08-19). Form này chỉ
 * có ô tick cho `ASSIGNABLE_ROLES` (`Admin`/`User`) nên phải tự gửi lại role hệ thống của user
 * đích, nếu không: người gọi Admin → 403, người gọi SuperAdmin → **hạ quyền âm thầm**.
 */
describe('UserFormDialog — giữ nguyên role ngoài ASSIGNABLE_ROLES', () => {
  let fixture: ComponentFixture<UserFormDialog>;
  let dialog: UserFormDialog;
  let currentUser: CurrentUserService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection(), provideHttpClient(), provideHttpClientTesting()],
    });
    currentUser = TestBed.inject(CurrentUserService);
    fixture = TestBed.createComponent(UserFormDialog);
    dialog = fixture.componentInstance;
  });

  /** Mở dialog ở chế độ SỬA cho `user` và trả về payload mà form phát ra khi bấm Lưu. */
  function openEditAndSubmit(user: IUser, mutate?: () => void): IUserFormSaveEvent {
    fixture.componentRef.setInput('editing', user);
    fixture.componentRef.setInput('open', true);
    fixture.detectChanges();

    mutate?.();

    let emitted: IUserFormSaveEvent | undefined;
    dialog.saved.subscribe((event) => (emitted = event));
    dialog.onSubmit();

    expect(emitted).toBeDefined();
    return emitted as IUserFormSaveEvent;
  }

  function setCaller(roles: string[]): void {
    currentUser.setUser({
      Id: 'caller',
      UserName: 'caller',
      Email: null,
      FullName: 'Người đang đăng nhập',
      Roles: roles,
      MustChangePassword: false,
    });
  }

  it('sửa user có SuperAdmin → payload VẪN gồm SuperAdmin (người gọi là Admin: nếu thiếu sẽ 403)', () => {
    setCaller(['Admin']);
    const event = openEditAndSubmit(aUser(['SuperAdmin', 'Admin']));

    expect(event.IsEditing).toBeTrue();
    expect(event.Update?.Roles).toContain('SuperAdmin');
    expect(event.Update?.Roles).toContain('Admin');
    expect(event.Update?.Roles.length).toBe(2);
  });

  it('người gọi CHÍNH LÀ SuperAdmin sửa một SuperAdmin khác → vẫn giữ SuperAdmin (ca BE cho qua, chỉ FE chặn được hạ quyền âm thầm)', () => {
    setCaller(['SuperAdmin']);
    const event = openEditAndSubmit(aUser(['SuperAdmin', 'User']));

    // Không có điều kiện nào theo role người đăng nhập: giữ role là VÔ ĐIỀU KIỆN.
    expect(event.Update?.Roles).toContain('SuperAdmin');
    expect(event.Update?.Roles).toContain('User');
  });

  it('không đăng nhập / không rõ người gọi → vẫn giữ nguyên role hệ thống', () => {
    currentUser.clear();
    const event = openEditAndSubmit(aUser(['SuperAdmin']));
    expect(event.Update?.Roles).toEqual(['SuperAdmin']);
  });

  it('bỏ tick hết ô vai trò vẫn không làm mất role hệ thống (payload còn đúng SuperAdmin)', () => {
    setCaller(['SuperAdmin']);
    const event = openEditAndSubmit(aUser(['SuperAdmin', 'Admin']), () => dialog.toggleRole('Admin'));

    expect(event.Update?.Roles).toEqual(['SuperAdmin']);
  });

  it('tick thêm vai trò → gộp cả role giữ nguyên lẫn role vừa chọn', () => {
    setCaller(['SuperAdmin']);
    const event = openEditAndSubmit(aUser(['SuperAdmin']), () => dialog.toggleRole('User'));

    expect(event.Update?.Roles).toContain('SuperAdmin');
    expect(event.Update?.Roles).toContain('User');
  });

  it('user thường (không có role hệ thống) → payload KHÔNG mọc thêm role lạ', () => {
    setCaller(['Admin']);
    const event = openEditAndSubmit(aUser(['User']));
    expect(event.Update?.Roles).toEqual(['User']);
  });

  it('so khớp tên role CHÍNH XÁC hoa/thường — "superadmin" (casing lạ) được coi là role hệ thống và giữ NGUYÊN CHUỖI, không tự sửa casing', () => {
    // BE so ordinal và không chuẩn hoá; validator chặn casing sai bằng 400. FE không được "sửa hộ"
    // — gửi lại đúng chuỗi server trả về để lỗi lộ ra ở đúng chỗ thay vì bị FE che mất.
    setCaller(['SuperAdmin']);
    const event = openEditAndSubmit(aUser(['superadmin', 'Admin']));

    expect(event.Update?.Roles).toContain('superadmin');
    expect(event.Update?.Roles).not.toContain('SuperAdmin');
  });

  it('TẠO MỚI: không có user đích → không thêm role giữ nguyên nào', () => {
    setCaller(['SuperAdmin']);
    fixture.componentRef.setInput('editing', null);
    fixture.componentRef.setInput('open', true);
    fixture.detectChanges();

    dialog.onUserNameInput({ target: { value: 'nguyen.van.b' } } as unknown as Event);
    dialog.onEmailInput({ target: { value: 'b@congty.vn' } } as unknown as Event);
    dialog.onFullNameInput({ target: { value: 'Nguyễn Văn B' } } as unknown as Event);
    dialog.onTempPasswordInput({ target: { value: 'TempPass@123' } } as unknown as Event);
    dialog.toggleRole('User');

    let emitted: IUserFormSaveEvent | undefined;
    dialog.saved.subscribe((event) => (emitted = event));
    dialog.onSubmit();

    expect(emitted?.IsEditing).toBeFalse();
    expect(emitted?.Create?.Roles).toEqual(['User']);
  });
});
