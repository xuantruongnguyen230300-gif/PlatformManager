import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { registerLocaleData } from '@angular/common';
import localeVi from '@angular/common/locales/vi';
import { By } from '@angular/platform-browser';
import { UserGridTable } from './user-grid-table';
import { IUser } from '../../models/quan-tri-nguoi-dung.model';
import { provideTranslateService } from '@ngx-translate/core';
import { useTranslationsInTest } from '../../../../core/i18n/i18n.testing';

/**
 * 🛑 SPEC NÀY PHẢI TỰ ĐỨNG — sửa flake 2026-09-08.
 *
 * `UserGridTable` render cột "Ngày tạo" bằng `DatePipe` với locale `'vi'` (mặc định của input
 * `localeId`). Angular chỉ biên dịch sẵn `en-US`; mọi locale khác phải qua `registerLocaleData`,
 * và chỗ DUY NHẤT gọi hàm đó trong app là `LanguageService.init()`.
 *
 * Nhưng `registerLocaleData` ghi vào một **registry toàn cục của tiến trình**, còn Karma chạy mọi
 * spec trong CÙNG một context trình duyệt theo **thứ tự ngẫu nhiên**. Nên trước đây spec này xanh
 * hay đỏ tuỳ vào việc `language.service.spec.ts` / `app.config.spec.ts` có tình cờ chạy trước hay
 * không: chạy trước ⇒ `vi` đã có trong registry ⇒ xanh; chạy sau ⇒ `DatePipe` ném **NG0701** và
 * 5 test ở đây đỏ. Tần suất quan sát được ~1/5 lượt.
 *
 * Đăng ký ngay tại đây làm spec không còn phụ thuộc vào tác dụng phụ toàn cục của spec khác. Gọi
 * ở cấp module (không phải trong `beforeEach`) vì registry là toàn cục — gọi lại nhiều lần chỉ
 * ghi đè cùng một giá trị, không có gì để dọn.
 *
 * ⚠️ Đây KHÔNG phải chỗ để kiểm "app có khai `localeData` không" — phép thử đó phải kiểm HÌNH
 * DẠNG của `APP_I18N`, và nó đã nằm ở `app.config.spec.ts` với lý do đầy đủ tại chỗ.
 */
registerLocaleData(localeVi);

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
    Version: 'stamp-1',
    ...overrides,
  };
}

/**
 * `USER.SELF_LOCK_FORBIDDEN` (403, doc/contracts/users.md §"Bảo vệ tài khoản quản trị"): bấm
 * "Khoá" trên chính dòng mình chắc chắn lỗi → chặn sẵn ở UI. Ranh giới cố ý: chỉ chặn đúng ca này,
 * KHÔNG chặn "Mở khoá" và KHÔNG chặn khoá người khác (kể cả SuperAdmin) — quy ước UI chung ở
 * `doc/huong_dan/quy-uoc/fe-ui-conventions.md`, luật nghiệp vụ ở `doc/contracts/users.md`.
 */
describe('UserGridTable — nút Khoá/Mở khoá', () => {
  let fixture: ComponentFixture<UserGridTable>;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection(), provideTranslateService()],
    });
    await useTranslationsInTest();
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

/**
 * FE-9 — vai trò dùng biến thể `.badge.outline` TOÀN CỤC, không phải lớp `.role-tag` riêng của
 * lưới này. Chốt 2026-08-29: tên vai trò là ĐỊNH DANH nên không mang màu ngữ nghĩa như
 * `.ok/.warn/.bad`, nhưng vẫn giữ viền — đúng lập luận của Components/RoleTag.md, chỉ khác là
 * nó nằm dưới dạng một biến thể của MỘT component thay vì một lớp riêng.
 *
 * `.role-tag` là bản thứ hai của cùng một khái niệm (cùng nền `--surface-table-header`) kèm 3 giá
 * trị px trần ngoài thang token — đúng cơ chế đã làm bảng người dùng trôi khỏi các màn khác một
 * lần (hai hệ tên badge `.active`/`.locked` vs `.ok`/`.bad`).
 */
describe('UserGridTable — nhãn vai trò', () => {
  let fixture: ComponentFixture<UserGridTable>;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection(), provideTranslateService()],
    });
    await useTranslationsInTest();
    fixture = TestBed.createComponent(UserGridTable);
  });

  it('mỗi vai trò là một `.badge.outline`, và không còn `.role-tag` nào', () => {
    fixture.componentRef.setInput('rows', [aUser(OTHER, { Roles: ['SuperAdmin', 'Admin'] })]);
    fixture.componentRef.setInput('totalCount', 1);
    fixture.detectChanges();

    const badges = fixture.debugElement.queryAll(By.css('.role-cell .badge.outline'));
    expect(badges.length).toBe(2);
    expect(badges.map((node) => (node.nativeElement as HTMLElement).textContent?.trim())).toEqual([
      'SuperAdmin',
      'Admin',
    ]);
    expect(fixture.debugElement.queryAll(By.css('.role-tag')).length).toBe(0);
  });
});
