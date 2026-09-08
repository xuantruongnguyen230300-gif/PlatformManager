import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { UserFormDialog, IUserFormSaveEvent } from './user-form-dialog';
import { CurrentUserService } from '../../../../core/auth/current-user.service';
import { IUser } from '../../models/quan-tri-nguoi-dung.model';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { useTranslationsInTest } from '../../../../core/i18n/i18n.testing';
import { ApiFieldError } from '../../../../core/http/api-result.model';

/**
 * Một `ApiFieldError` như BE trả: **mã + câu**.
 *
 * `code` để `'TestValidator'` — một mã KHÔNG có trong bảng dịch, nên `fieldMessage` lùi về
 * `message`. Nhờ đó mọi assert dưới đây vẫn kiểm đúng câu tiếng Việt như trước 2026-09-06, chứ
 * không phải kiểm một khoá dịch.
 */
function fieldError(message: string): ApiFieldError {
  return { code: 'TestValidator', message };
}

function aUser(roles: string[]): IUser {
  return {
    // `Version` có mặt ở đây nhưng hộp thoại CỐ Ý không đọc tới: token chống ghi đè thuộc về bản
    // ghi, không phải ô nhập nào, nên trang cha mới là nơi ghép nó vào payload (xem
    // `IUserFormUpdate` và `submitUpdate`). Giữ giá trị thật để fixture đúng hình dạng `IUser`.
    Version: 'stamp-1',
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

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideTranslateService(),
      ],
    });
    await useTranslationsInTest();
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

  /**
   * Bộ test còn lại trong describe này phủ vế DỮ LIỆU (payload vẫn gửi kèm role hệ thống). Hai
   * test dưới đây phủ vế HIỂN THỊ — thứ chưa ai canh trước 2026-09-06: ô tick chỉ có
   * `Admin`/`User`, nên tài khoản SuperAdmin mở ra thấy cả hai ô đều trống. Không có dòng thông
   * báo thì người sửa tưởng tài khoản không có vai trò nào, tích thêm rồi gặp 403 không hiểu.
   */
  it('🛑 HIỂN THỊ: có role ngoài ASSIGNABLE_ROLES → hiện dòng "được giữ nguyên", kèm đúng tên role', async () => {
    fixture.componentRef.setInput('editing', aUser(['SuperAdmin', 'Admin']));
    fixture.componentRef.setInput('open', true);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const line = fixture.nativeElement.querySelector('.role-preserved') as HTMLElement | null;
    expect(line)
      .withContext('thiếu dòng này = người sửa tưởng tài khoản không có vai trò nào')
      .not.toBeNull();
    expect(line?.textContent).toContain('SuperAdmin');
    expect(line?.textContent)
      .withContext('Admin nằm trong ô tick rồi, không được lặp lại ở dòng giữ nguyên')
      .not.toContain('Admin,');
  });

  it('HIỂN THỊ: user thường → KHÔNG có dòng thừa', async () => {
    fixture.componentRef.setInput('editing', aUser(['Admin']));
    fixture.componentRef.setInput('open', true);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.role-preserved')).toBeNull();
  });

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

/**
 * FE-2 + FE-6 — `fields` của envelope lỗi bind vào ĐÚNG ô, kèm dấu hiệu đọc được bằng trình đọc
 * màn hình.
 *
 * Trước bản này không nơi nào đọc `fields`, nên mọi lỗi 400 hiện đúng một câu "Dữ liệu không hợp
 * lệ." ở cuối form — BE đã nói rõ ô nào sai mà người dùng vẫn phải tự đoán. Quy tắc:
 * doc/huong_dan/quy-uoc/fe-api-client.md §Envelope.
 */
describe('UserFormDialog — lỗi theo từng ô', () => {
  let fixture: ComponentFixture<UserFormDialog>;
  let dialog: UserFormDialog;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideTranslateService(),
      ],
    });
    await useTranslationsInTest();
    fixture = TestBed.createComponent(UserFormDialog);
    dialog = fixture.componentInstance;
  });

  /** Mở dialog ở chế độ TẠO MỚI (đủ 5 ô) và render. */
  function openCreate(): void {
    fixture.componentRef.setInput('editing', null);
    fixture.componentRef.setInput('open', true);
    fixture.detectChanges();
  }

  function el<T extends HTMLElement>(selector: string): T {
    return fixture.nativeElement.querySelector(selector) as T;
  }

  it('`fields` từ BE hiện dưới đúng ô, kèm aria-invalid + aria-describedby trỏ đúng thẻ lỗi', () => {
    openCreate();
    fixture.componentRef.setInput('serverFieldErrors', { Email: [fieldError('Email đã tồn tại.')] });
    fixture.detectChanges();

    const input = el<HTMLInputElement>('#ufEmail');
    expect(input.getAttribute('aria-invalid')).toBe('true');
    expect(input.classList).toContain('invalid');

    const describedBy = input.getAttribute('aria-describedby');
    expect(describedBy).toBeTruthy();
    const message = el<HTMLElement>(`#${describedBy}`);
    expect(message).not.toBeNull();
    expect(message.textContent?.trim()).toBe('Email đã tồn tại.');

    // Ô KHÔNG lỗi phải sạch hoàn toàn: `aria-describedby` trỏ tới thẻ không tồn tại là lỗi a11y
    // thật, và `aria-invalid="false"` chỉ thêm nhiễu.
    const fullName = el<HTMLInputElement>('#ufFullName');
    expect(fullName.hasAttribute('aria-invalid')).toBeFalse();
    expect(fullName.hasAttribute('aria-describedby')).toBeFalse();
  });

  it('key có chỉ số của FluentValidation (`Roles[0]`) vẫn về đúng ô Vai trò', () => {
    openCreate();
    // `RuleForEach(x => x.Roles)` phát PropertyName dạng `Roles[0]`; tra thẳng `fields['Roles']`
    // sẽ trượt và lỗi biến mất im lặng.
    fixture.componentRef.setInput('serverFieldErrors', {
      'Roles[0]': [fieldError('Role không hợp lệ — chỉ nhận SuperAdmin/Admin/User.')],
    });
    fixture.detectChanges();

    const group = el<HTMLElement>('.role-checkboxes');
    expect(group.getAttribute('aria-invalid')).toBe('true');
    expect(el<HTMLElement>(`#${group.getAttribute('aria-describedby')}`).textContent).toContain('Role không hợp lệ');
  });

  it('có `fields` thì KHÔNG hiện thêm câu chung "Dữ liệu không hợp lệ." bên dưới', () => {
    openCreate();
    fixture.componentRef.setInput('serverError', 'Dữ liệu không hợp lệ.');
    fixture.componentRef.setInput('serverFieldErrors', { Email: [fieldError('Email không đúng định dạng.')] });
    fixture.detectChanges();

    const messages = Array.from(fixture.nativeElement.querySelectorAll('.form-error')).map((node) =>
      (node as HTMLElement).textContent?.trim(),
    );
    expect(messages).toEqual(['Email không đúng định dạng.']);
  });

  it('lỗi KHÔNG gắn ô nào (vd 409 trùng tên đăng nhập) vẫn hiện ở cuối form', () => {
    openCreate();
    fixture.componentRef.setInput('serverError', 'Tên đăng nhập đã tồn tại.');
    fixture.detectChanges();

    expect(el<HTMLElement>('.form-error').textContent?.trim()).toBe('Tên đăng nhập đã tồn tại.');
  });

  it('kiểm tại chỗ báo HẾT lỗi trong một lần bấm Lưu, và không phát `saved`', () => {
    openCreate();

    let emitted = 0;
    dialog.saved.subscribe(() => emitted++);
    dialog.onSubmit();
    fixture.detectChanges();

    expect(emitted).toBe(0);
    // 5 ô đều trống → 5 lỗi cùng lúc, thay vì bắt người dùng sửa - bấm - sửa 5 vòng.
    const messages = Array.from(fixture.nativeElement.querySelectorAll('.form-error')).map((node) =>
      (node as HTMLElement).textContent?.trim(),
    );
    expect(messages.length).toBe(5);
    expect(el<HTMLInputElement>('#ufUserName').getAttribute('aria-invalid')).toBe('true');
    expect(el<HTMLInputElement>('#ufTempPassword').getAttribute('aria-invalid')).toBe('true');
  });

  it('lỗi kiểm tại chỗ ĐÈ lỗi server của cùng một ô', () => {
    openCreate();
    fixture.componentRef.setInput('serverFieldErrors', { Email: [fieldError('Email đã tồn tại.')] });
    fixture.detectChanges();

    dialog.onSubmit();
    fixture.detectChanges();

    expect(el<HTMLElement>('#ufEmailError').textContent?.trim()).toBe('Email bắt buộc.');
  });
});

/**
 * FE — khoá form trong lúc đang gửi (fe/09-forms-validation.md §"Khoá form trong lúc đang gửi",
 * chốt 2026-08-31). Nghiệm thu của quy ước: bấm nút gửi hai lần thật nhanh ⇒ ĐÚNG MỘT request rời
 * khỏi client.
 *
 * Thiệt hại của việc thiếu nó không lớn nhưng rất khó hiểu với người dùng: tên đăng nhập là duy
 * nhất nên lần thứ hai bị BE từ chối, và họ đọc được câu "tên đăng nhập đã tồn tại" ngay sau khi
 * vừa tạo thành công chính tài khoản đó.
 */
describe('UserFormDialog — khoá nút Lưu trong lúc request đang bay', () => {
  let fixture: ComponentFixture<UserFormDialog>;
  let dialog: UserFormDialog;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideTranslateService(),
      ],
    });
    await useTranslationsInTest();
    fixture = TestBed.createComponent(UserFormDialog);
    dialog = fixture.componentInstance;
    fixture.componentRef.setInput('editing', aUser(['User']));
    fixture.componentRef.setInput('open', true);
    fixture.detectChanges();
  });

  function saveButton(): HTMLButtonElement {
    return fixture.nativeElement.querySelector('.dialog-actions .btn.primary') as HTMLButtonElement;
  }

  it('chưa gửi → nút Lưu bật, nhãn "Lưu"', () => {
    expect(saveButton().disabled).toBeFalse();
    expect(saveButton().textContent?.trim()).toBe('Lưu');
  });

  it('đang gửi → nút Lưu TẮT và nói rõ vì sao', () => {
    fixture.componentRef.setInput('saving', true);
    fixture.detectChanges();

    expect(saveButton().disabled).toBeTrue();
    expect(saveButton().textContent?.trim()).toBe('Đang lưu…');
  });

  it('đang gửi → gọi onSubmit() lần nữa KHÔNG phát thêm sự kiện lưu', () => {
    let emitted = 0;
    dialog.saved.subscribe(() => emitted++);

    dialog.onSubmit();
    expect(emitted).toBe(1);

    // `[disabled]` là giao diện, không phải bất biến của luồng — Enter/kịch bản khác vẫn gọi tới.
    fixture.componentRef.setInput('saving', true);
    fixture.detectChanges();
    dialog.onSubmit();

    expect(emitted).withContext('đúng MỘT request rời khỏi client').toBe(1);
  });

  it('nút Huỷ KHÔNG bị khoá khi đang gửi — đóng hộp thoại là lối thoát, không phải thao tác ghi', () => {
    fixture.componentRef.setInput('saving', true);
    fixture.detectChanges();

    const cancel = fixture.nativeElement.querySelector('.dialog-actions .btn:not(.primary)') as HTMLButtonElement;
    expect(cancel.disabled).toBeFalse();
  });
});
