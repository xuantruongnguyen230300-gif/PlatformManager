import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { By } from '@angular/platform-browser';
import { UserGridTable } from './user-grid-table';
import { IUser } from '../../models/quan-tri-nguoi-dung.model';

const ME = 'me-id';
const OTHER = 'other-id';

function aUser(id: string, overrides: Partial<IUser> = {}): IUser {
  return {
    Id: id,
    UserName: `user-${id}`,
    Email: null,
    FullName: `User ${id}`,
    Roles: ['User'],
    IsLocked: false,
    MustChangePassword: false,
    DateCreate: '2026-08-19T00:00:00Z',
    ...overrides,
  };
}

/**
 * `USER.SELF_LOCK_FORBIDDEN` (403, doc/contracts/users.md §"Bảo vệ tài khoản quản trị"): bấm
 * "Khoá" trên chính dòng mình chắc chắn lỗi → chặn sẵn ở UI. Ranh giới cố ý: chỉ chặn đúng ca này,
 * KHÔNG chặn "Mở khoá" và KHÔNG chặn khoá người khác (kể cả SuperAdmin) — xem
 * `src/FE/.claude/docs/ui-conventions.md` §"Chặn trước ở UI".
 */
describe('UserGridTable — nút Khoá/Mở khoá', () => {
  let fixture: ComponentFixture<UserGridTable>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection()],
    });
    fixture = TestBed.createComponent(UserGridTable);
  });

  /** Render grid với danh sách `rows` và người đăng nhập `currentUserId`. */
  function render(rows: IUser[], currentUserId: string | null = ME): void {
    fixture.componentRef.setInput('rows', rows);
    fixture.componentRef.setInput('totalCount', rows.length);
    fixture.componentRef.setInput('currentUserId', currentUserId);
    fixture.detectChanges();
  }

  /** Nút khoá/mở khoá của dòng thứ `index` (nút thứ 2 trong `.row-actions`). */
  function lockButton(index: number): HTMLButtonElement {
    const rows = fixture.debugElement.queryAll(By.css('.row-actions'));
    const buttons = rows[index].queryAll(By.css('button'));
    return buttons[1].nativeElement as HTMLButtonElement;
  }

  it('dòng CHÍNH MÌNH (đang hoạt động) → nút Khoá bị disable, title nói rõ lối ra', () => {
    render([aUser(ME)]);

    const button = lockButton(0);
    expect(button.disabled).toBeTrue();
    expect(button.title).toBe('Không thể tự khoá tài khoản của chính mình — dùng Đăng xuất');
  });

  it('dòng NGƯỜI KHÁC → nút Khoá vẫn bật, kể cả khi người đó là SuperAdmin', () => {
    render([aUser(OTHER, { Roles: ['SuperAdmin'] })]);

    const button = lockButton(0);
    // Luật "chỉ SuperAdmin mới khoá được SuperAdmin" là luật phân quyền của BE — FE KHÔNG chép
    // lại, để 403 + message của BE làm nguồn sự thật duy nhất.
    expect(button.disabled).toBeFalse();
    expect(button.title).toBe('Khoá tài khoản');
  });

  it('nút MỞ KHOÁ không bị ảnh hưởng — cả trên dòng mình lẫn dòng người khác', () => {
    render([aUser(ME, { IsLocked: true }), aUser(OTHER, { IsLocked: true })]);

    // BE cố ý KHÔNG chặn unlock (chỉ có USER.NOT_FOUND) — không suy diễn "khoá bị chặn thì mở
    // khoá cũng vậy".
    expect(lockButton(0).disabled).toBeFalse();
    expect(lockButton(0).title).toBe('Mở khoá tài khoản');
    expect(lockButton(1).disabled).toBeFalse();
  });

  it('chưa biết người đăng nhập (currentUserId = null) → không chặn dòng nào', () => {
    render([aUser(ME), aUser(OTHER)], null);

    expect(lockButton(0).disabled).toBeFalse();
    expect(lockButton(1).disabled).toBeFalse();
  });

  it('nút Khoá còn bật vẫn phát output toggleLock như cũ', () => {
    render([aUser(OTHER)]);

    let emitted: IUser | undefined;
    fixture.componentInstance.toggleLock.subscribe((user) => (emitted = user));
    lockButton(0).click();

    expect(emitted?.Id).toBe(OTHER);
  });
});
