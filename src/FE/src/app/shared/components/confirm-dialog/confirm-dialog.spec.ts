import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { ConfirmDialog } from './confirm-dialog';
import { provideTranslateService } from '@ngx-translate/core';
import { useTranslationsInTest } from '../../../core/i18n/i18n.testing';

/**
 * Trọng tâm: `<dialog>` chỉ phát MỘT sự kiện `close` cho mọi đường đóng, nên phần đáng vỡ nhất là
 * ánh xạ "đóng bằng cách nào → phát sự kiện gì". Nhánh nguy hiểm là Escape: nó không đi qua nút
 * nào, dễ rơi vào im lặng hoặc bị hiểu nhầm thành đồng ý.
 */
describe('ConfirmDialog', () => {
  let fixture: ComponentFixture<ConfirmDialog>;
  let component: ConfirmDialog;
  let confirmed: number;
  let cancelled: number;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection(), provideTranslateService()],
    });
    await useTranslationsInTest();
    fixture = TestBed.createComponent(ConfirmDialog);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('title', 'Xoá chỉ tiêu 6.4?');
    fixture.detectChanges();

    confirmed = 0;
    cancelled = 0;
    component.confirmed.subscribe(() => confirmed++);
    component.cancelled.subscribe(() => cancelled++);
  });

  afterEach(() => fixture.destroy());

  function el(): HTMLDialogElement {
    return (fixture.nativeElement as HTMLElement).querySelector('dialog') as HTMLDialogElement;
  }

  function query<T extends HTMLElement>(selector: string): T {
    return el().querySelector(selector) as T;
  }

  /**
   * `<dialog>.close()` phát `close` ở một TASK sau, và task đó KHÔNG bảo đảm chạy trước
   * `setTimeout(0)` — chờ bằng bộ đếm thời gian là test lúc xanh lúc đỏ tuỳ trình duyệt xếp hàng
   * ra sao. Vì vậy phải đăng ký nghe chính sự kiện đó TRƯỚC khi kích hoạt hành động.
   * Listener của Angular đã gắn từ lúc render nên luôn chạy trước listener này.
   */
  function closeEvent(): Promise<void> {
    return new Promise((resolve) => el().addEventListener('close', () => resolve(), { once: true }));
  }

  /** Nhường một vòng event loop — dùng cho nhánh KHÔNG được phép đóng, để không chờ vô ích. */
  function nextTask(): Promise<void> {
    return new Promise((resolve) => setTimeout(resolve, 0));
  }

  it('open() mở ở chế độ modal, close() đóng lại', () => {
    expect(el().open).toBeFalse();

    component.open();
    expect(el().open).toBeTrue();

    component.close();
    expect(el().open).toBeFalse();
  });

  it('open() gọi hai lần không ném lỗi (native showModal ném khi dialog đang mở)', () => {
    component.open();
    expect(() => component.open()).not.toThrow();
    expect(el().open).toBeTrue();
  });

  it('mở dialog đưa focus về nút Huỷ, không phải nút ×', () => {
    component.open();
    expect(document.activeElement).toBe(query('.dialog-actions .btn:not(.primary)'));
  });

  it('bấm nút xác nhận → phát confirmed, KHÔNG phát cancelled', async () => {
    component.open();
    const closed = closeEvent();
    query<HTMLButtonElement>('.dialog-actions .btn.primary').click();
    await closed;

    expect(confirmed).toBe(1);
    expect(cancelled).toBe(0);
    expect(el().open).toBeFalse();
  });

  it('bấm Huỷ → phát cancelled', async () => {
    component.open();
    const closed = closeEvent();
    query<HTMLButtonElement>('.dialog-actions .btn:not(.primary):not(.danger)').click();
    await closed;

    expect(cancelled).toBe(1);
    expect(confirmed).toBe(0);
  });

  it('bấm × → phát cancelled', async () => {
    component.open();
    const closed = closeEvent();
    query<HTMLButtonElement>('.dialog-close').click();
    await closed;

    expect(cancelled).toBe(1);
    expect(confirmed).toBe(0);
  });

  it('Escape (trình duyệt tự đóng, không qua nút nào) → phát cancelled', async () => {
    component.open();
    const closed = closeEvent();
    // Escape của <dialog> gốc đóng thẳng phần tử rồi phát `close` — mô phỏng đúng đường đó.
    el().close();
    await closed;

    expect(cancelled).toBe(1);
    expect(confirmed).toBe(0);
  });

  it('close() do trang cha gọi thì KHÔNG phát gì — tránh vòng lặp với handler (cancelled)', async () => {
    component.open();
    const closed = closeEvent();
    component.close();
    await closed;

    expect(cancelled).toBe(0);
    expect(confirmed).toBe(0);
  });

  it('mở lại sau một lần xác nhận không mang theo kết quả cũ', async () => {
    component.open();
    let closed = closeEvent();
    query<HTMLButtonElement>('.dialog-actions .btn.primary').click();
    await closed;

    component.open();
    closed = closeEvent();
    el().close();
    await closed;

    expect(confirmed).toBe(1);
    expect(cancelled).toBe(1);
  });

  it('mỗi mức độ dùng đúng icon và đúng biến thể màu', () => {
    const expected: Record<string, string> = {
      ask: 'pi-question-circle',
      ok: 'pi-check-circle',
      warn: 'pi-exclamation-triangle',
      bad: 'pi-trash',
    };

    for (const [severity, iconClass] of Object.entries(expected)) {
      fixture.componentRef.setInput('severity', severity);
      fixture.detectChanges();

      const badge = query('.dialog-icon');
      expect(badge.classList).toContain(severity);
      expect((badge.querySelector('i') as HTMLElement).classList).toContain(iconClass);
    }
  });

  it('input icon ghi đè icon mặc định của mức độ', () => {
    fixture.componentRef.setInput('severity', 'bad');
    fixture.componentRef.setInput('icon', 'pi-ban');
    fixture.detectChanges();

    expect((query('.dialog-icon i') as HTMLElement).classList).toContain('pi-ban');
  });

  it('confirmDanger đổi nút xác nhận sang .danger thay vì .primary', () => {
    fixture.componentRef.setInput('confirmDanger', true);
    fixture.componentRef.setInput('confirmLabel', 'Xoá');
    fixture.detectChanges();

    const confirmButton = query<HTMLButtonElement>('.dialog-actions .btn.danger');
    expect(confirmButton.textContent?.trim()).toBe('Xoá');
    expect(confirmButton.classList).not.toContain('primary');
  });

  it('confirmDisabled chặn bấm thật, không chỉ đổi màu', async () => {
    fixture.componentRef.setInput('confirmDisabled', true);
    fixture.detectChanges();
    component.open();

    query<HTMLButtonElement>('.dialog-actions .btn.primary').click();
    await nextTask();

    expect(confirmed).toBe(0);
    expect(el().open).toBeTrue();
  });

  it('mô tả rỗng thì không render .dialog-desc lẫn aria-describedby', () => {
    expect(el().querySelector('.dialog-desc')).toBeNull();
    expect(el().getAttribute('aria-describedby')).toBeNull();

    fixture.componentRef.setInput('description', 'Số liệu đã nhập ở các kỳ trước sẽ không còn hiển thị.');
    fixture.detectChanges();

    expect(query('.dialog-desc').textContent?.trim()).toBe('Số liệu đã nhập ở các kỳ trước sẽ không còn hiển thị.');
    expect(el().getAttribute('aria-describedby')).toBe(query('.dialog-desc').id);
  });

  it('tiêu đề được nối vào dialog bằng aria-labelledby', () => {
    expect(el().getAttribute('aria-labelledby')).toBe(query('.dialog-title').id);
  });
});
