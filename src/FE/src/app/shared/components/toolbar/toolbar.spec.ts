import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { IToolbarChip, Toolbar } from './toolbar';
import { provideTranslateService } from '@ngx-translate/core';
import { useTranslationsInTest } from '../../../core/i18n/i18n.testing';

/**
 * Chỉ test phần CÓ NHÁNH của thanh công cụ: đóng/mở panel lọc (thứ `<details>` gốc không tự làm)
 * và các sự kiện phát ra ngoài. Phần hình dạng thuộc `styles.scss`, không test ở đây.
 */
describe('Toolbar', () => {
  let fixture: ComponentFixture<Toolbar>;
  let toolbar: Toolbar;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection(), provideTranslateService()],
    });
    await useTranslationsInTest();
    fixture = TestBed.createComponent(Toolbar);
    toolbar = fixture.componentInstance;
  });

  function host(): HTMLElement {
    return fixture.nativeElement as HTMLElement;
  }

  /** Bật bộ lọc, render, trả về chính thẻ `<details>` để thao tác trực tiếp. */
  function withFilter(): HTMLDetailsElement {
    fixture.componentRef.setInput('hasFilter', true);
    fixture.detectChanges();
    return host().querySelector('details.filter') as HTMLDetailsElement;
  }

  it('gõ vào ô tìm kiếm cập nhật model searchValue', () => {
    fixture.detectChanges();
    const input = host().querySelector('.search input') as HTMLInputElement;

    input.value = 'hồ sơ trực tuyến';
    input.dispatchEvent(new Event('input'));

    expect(toolbar.searchValue()).toBe('hồ sơ trực tuyến');
  });

  it('ô tìm kiếm bị khoá thì thật sự disabled, không chỉ đổi màu', () => {
    fixture.componentRef.setInput('searchDisabled', true);
    fixture.detectChanges();

    expect((host().querySelector('.search input') as HTMLInputElement).disabled).toBeTrue();
  });

  it('không có bộ lọc thì không mọc nút Lọc rỗng', () => {
    fixture.detectChanges();
    expect(host().querySelector('details.filter')).toBeNull();
  });

  it('filterCount = 0 thì ẩn huy hiệu số, > 0 thì hiện', () => {
    withFilter();
    expect(host().querySelector('.filter-count')).toBeNull();

    fixture.componentRef.setInput('filterCount', 3);
    fixture.detectChanges();
    expect(host().querySelector('.filter-count')?.textContent?.trim()).toBe('3');
  });

  it('bấm Áp dụng phát filterApply và đóng panel', () => {
    const details = withFilter();
    details.open = true;
    let applied = 0;
    toolbar.filterApply.subscribe(() => applied++);

    (host().querySelector('.filter-foot .btn.primary') as HTMLButtonElement).click();

    expect(applied).toBe(1);
    expect(details.open).toBeFalse();
  });

  it('bấm Xoá lọc phát filterClear và đóng panel', () => {
    const details = withFilter();
    details.open = true;
    let cleared = 0;
    toolbar.filterClear.subscribe(() => cleared++);

    (host().querySelector('.filter-foot .btn:not(.primary)') as HTMLButtonElement).click();

    expect(cleared).toBe(1);
    expect(details.open).toBeFalse();
  });

  it('bấm ra ngoài đóng panel, bấm bên trong thì không', () => {
    const details = withFilter();
    details.open = true;

    // Bấm bên trong panel: sự kiện vẫn nổi lên document, nhưng target nằm trong <details>.
    (host().querySelector('.filter-panel') as HTMLElement).dispatchEvent(new MouseEvent('click', { bubbles: true }));
    expect(details.open).toBeTrue();

    document.body.dispatchEvent(new MouseEvent('click', { bubbles: true }));
    expect(details.open).toBeFalse();
  });

  it('Escape đóng panel (hành vi <details> gốc không có)', () => {
    const details = withFilter();
    details.open = true;

    details.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));

    expect(details.open).toBeFalse();
  });

  it('gỡ chip phát đúng Key của chip đó', () => {
    const chips: IToolbarChip[] = [
      { Key: 'group', Label: 'Chính quyền số' },
      { Key: 'period', Label: 'Quý III/2026' },
    ];
    fixture.componentRef.setInput('chips', chips);
    fixture.detectChanges();

    const removed: string[] = [];
    toolbar.chipRemove.subscribe((key) => removed.push(key));

    const buttons = host().querySelectorAll<HTMLButtonElement>('.filter-chip .icon-btn');
    expect(buttons.length).toBe(2);
    buttons[1].click();

    expect(removed).toEqual(['period']);
  });

  it('nút gỡ chip có aria-label nói rõ đang gỡ điều kiện nào', () => {
    fixture.componentRef.setInput('chips', [{ Key: 'group', Label: 'Chính quyền số' }]);
    fixture.detectChanges();

    const button = host().querySelector('.filter-chip .icon-btn') as HTMLButtonElement;
    expect(button.getAttribute('aria-label')).toBe('Bỏ lọc Chính quyền số');
  });
});
