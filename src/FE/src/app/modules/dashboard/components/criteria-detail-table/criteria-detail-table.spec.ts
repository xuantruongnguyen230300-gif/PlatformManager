import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { APP_I18N } from '../../../../app.config';
import { CORE_I18N } from '../../../../core/i18n/core-i18n';
import { useTranslationsInTest } from '../../../../core/i18n/i18n.testing';
import { IDashboardTableRow } from '../../models/dashboard.model';
import { CriteriaDetailTable } from './criteria-detail-table';

function row(patch: Partial<IDashboardTableRow> = {}): IDashboardTableRow {
  return {
    CriteriaId: 'c1',
    Code: '1.4',
    Name: 'Mức độ ứng dụng AI',
    GroupId: 'g1',
    GroupCode: '1',
    GroupName: 'Hạ tầng và Nền tảng số',
    MaxScore: 5,
    SelfScore: 5,
    VerifiedScore: 0,
    Diff: -5,
    Status: 'Cần bổ sung minh chứng',
    Note: null,
    ...patch,
  };
}

describe('CriteriaDetailTable — 9 cột, chỉ đọc (T4)', () => {
  let fixture: ComponentFixture<CriteriaDetailTable>;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideTranslateService(),
        { provide: CORE_I18N, useValue: APP_I18N },
      ],
    });
    await useTranslationsInTest();
    fixture = TestBed.createComponent(CriteriaDetailTable);
  });

  function render(rows: IDashboardTableRow[]): HTMLElement {
    fixture.componentRef.setInput('rows', rows);
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  it('đúng 9 cột, và bảng nằm trong `.tablewrap.scroll` — KHÔNG paginator, không p-table', () => {
    const host = render([row()]);

    expect(host.querySelectorAll('thead th').length).toBe(9);
    expect(host.querySelector('.tablewrap.scroll')).not.toBeNull();
    expect(host.querySelector('p-table')).withContext('T4: bảng thuần, không DataTable').toBeNull();
    expect(host.querySelector('p-paginator')).toBeNull();
  });

  it('header đúng thứ tự đã duyệt', () => {
    const headers = [...render([row()]).querySelectorAll('thead th')].map((th) => th.textContent?.trim());

    expect(headers).toEqual([
      'Mã',
      'Chỉ tiêu',
      'Nhóm',
      'Điểm tối đa',
      'Tự đánh giá',
      'Thẩm định',
      'Chênh lệch',
      'Trạng thái',
      'Minh chứng/Ghi chú',
    ]);
  });

  it('nhóm hiện dạng `Code. Name` (Q42)', () => {
    const cells = render([row()]).querySelectorAll('tbody td');

    expect(cells[2].textContent?.trim()).toBe('1. Hạ tầng và Nền tảng số');
  });

  /**
   * 🛑 Ca 1.4 là lý do Q25 tồn tại: tự chấm 5 rồi bị thẩm định bác trắng là ca XẤU NHẤT của bộ dữ
   * liệu, mà công thức cũ tô nó XANH. Dấu phải sống sót qua tầng hiển thị.
   */
  it('🛑 `Diff` âm hiện màu ĐỎ và giữ nguyên dấu — không lấy trị tuyệt đối, không đảo dấu', () => {
    const host = render([row({ Diff: -5 })]);
    const delta = host.querySelector('.delta') as HTMLElement;

    expect(delta.classList).toContain('down');
    // Dấu do `Intl.NumberFormat(signDisplay)` sinh — locale `vi` dùng dấu trừ ASCII. Viết đúng
    // thứ trình duyệt trả về, KHÔNG chép ký tự `−` (U+2212) từ bản dựng: một test khẳng định ký
    // tự mà runtime không bao giờ sinh ra là test luôn đỏ vì lý do sai.
    expect(delta.textContent?.trim()).toBe('-5,00');
  });

  it('`Diff` dương hiện màu XANH kèm dấu +', () => {
    const delta = render([row({ Diff: 2.96 })]).querySelector('.delta') as HTMLElement;

    expect(delta.classList).toContain('up');
    expect(delta.textContent?.trim()).toBe('+2,96');
  });

  it('`Diff` vắng mặt hiện `—` màu xám, không phải `0,00`', () => {
    const delta = render([row({ Diff: null })]).querySelector('.delta') as HTMLElement;

    expect(delta.classList).toContain('flat');
    expect(delta.textContent?.trim()).toBe('—');
  });

  it('badge trạng thái theo bảng ánh xạ duy nhất (Q10)', () => {
    expect(render([row({ Status: 'Hoàn thành' })]).querySelector('.badge')?.classList).toContain('ok');
    expect(render([row({ Status: 'Đang thực hiện' })]).querySelector('.badge')?.classList).toContain('warn');
    expect(render([row({ Status: 'Cần bổ sung minh chứng' })]).querySelector('.badge')?.classList).toContain('bad');
    expect(render([row({ Status: 'Chưa thực hiện' })]).querySelector('.badge')?.classList).toContain('neutral');
  });

  it('trạng thái vắng mặt: KHÔNG render badge nào', () => {
    const host = render([row({ Status: null })]);

    expect(host.querySelector('tbody .badge')).toBeNull();
    expect(host.querySelectorAll('tbody td')[7].textContent?.trim()).toBe('—');
  });

  it('ô ghi chú rỗng hiện `—` màu xám', () => {
    const cells = render([row({ Note: null })]).querySelectorAll('tbody td');

    expect(cells[8].textContent?.trim()).toBe('—');
  });

  it('lọc không khớp dòng nào: một câu trong bảng, colspan phủ hết 9 cột', () => {
    const host = render([]);
    const cell = host.querySelector('tbody td') as HTMLTableCellElement;

    expect(cell.getAttribute('colspan')).toBe('9');
    expect(cell.textContent?.trim()).toBe('Không có chỉ tiêu nào khớp bộ lọc.');
  });
});
