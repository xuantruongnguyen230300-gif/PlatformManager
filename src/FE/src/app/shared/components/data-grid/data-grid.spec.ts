import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideTranslateService } from '@ngx-translate/core';
import { DataGrid, IDataGridFrozenColumns, IDataGridPageChange } from './data-grid';
import { useTranslationsInTest } from '../../../core/i18n/i18n.testing';

/**
 * Ghim cột mép trái/mép phải — input `frozenColumns`, hợp đồng ở
 * `doc/Design/Frontend/PlatformManager/Components/DataTable.md` § Frozen edge columns.
 *
 * ## Vì sao phép kiểm đọc `getComputedStyle` chứ không đọc tên class
 *
 * Ghim cột là một hành vi CSS thuần (xem JSDoc của `frozenColumns` trong `data-grid.ts`: directive
 * `pFrozenColumn` không chạm được tới ô do màn hình khai qua `TemplateRef`). Assert lên tên class
 * chỉ chứng minh template có gắn class — nó vẫn xanh khi khối SCSS bị xoá sạch, tức xanh vì không
 * kiểm gì cả. `getComputedStyle` là chỗ duy nhất phân biệt được "đã ghim" với "có class mà không
 * ghim", và Karma chạy Chrome thật nên nó đo được.
 *
 * `src/styles.scss` được `angular.json` nạp cho CẢ target `test`, nên nền dòng, `th` sticky-top và
 * mọi token trong phép đo dưới đây chính là thứ chạy lúc deploy.
 */

interface IRow {
  readonly Id: string;
  readonly Code: string;
  readonly Name: string;
}

const ROWS: IRow[] = [
  { Id: '1', Code: '1.01', Name: 'First' },
  { Id: '2', Code: '2.02', Name: 'Second' },
];

@Component({
  standalone: true,
  imports: [DataGrid],
  template: `
    <app-data-grid
      [rows]="rows()"
      [totalCount]="rows().length"
      [frozenColumns]="frozen()"
      [headerTemplate]="hdr"
      [bodyTemplate]="body"
      [emptyTemplate]="empty"
      (pageChange)="lastPageChange = $event"
    />

    <ng-template #hdr>
      <tr>
        <th style="min-width: 70px">Code</th>
        <th style="min-width: 900px">Name</th>
        <th style="min-width: 120px">Actions</th>
      </tr>
    </ng-template>

    <ng-template #body let-row>
      <tr>
        <td>{{ row.Code }}</td>
        <td>{{ row.Name }}</td>
        <td><button type="button">edit</button></td>
      </tr>
    </ng-template>

    <ng-template #empty>
      <tr>
        <td colspan="3">nothing</td>
      </tr>
    </ng-template>
  `,
})
class HostComponent {
  readonly rows = signal<IRow[]>(ROWS);
  readonly frozen = signal<IDataGridFrozenColumns | undefined>(undefined);
  lastPageChange: IDataGridPageChange | null = null;
}

describe('DataGrid — input frozenColumns (DataTable.md § Frozen edge columns)', () => {
  let fixture: ComponentFixture<HostComponent>;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection(), provideTranslateService()],
    });
    await useTranslationsInTest();
    fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
    await fixture.whenStable();
  });

  async function withFrozen(frozen: IDataGridFrozenColumns | undefined): Promise<void> {
    fixture.componentInstance.frozen.set(frozen);
    fixture.detectChanges();
    await fixture.whenStable();
  }

  function wrap(): HTMLElement {
    return fixture.nativeElement.querySelector('.tablewrap') as HTMLElement;
  }

  function headerCells(): HTMLElement[] {
    return Array.from(fixture.nativeElement.querySelectorAll('thead th')) as HTMLElement[];
  }

  function firstBodyCells(): HTMLElement[] {
    const row = fixture.nativeElement.querySelector('tbody tr') as HTMLElement;
    return Array.from(row.querySelectorAll('td')) as HTMLElement[];
  }

  it('mặc định KHÔNG ghim gì — lưới đang chạy hôm nay không được đổi hành vi', async () => {
    expect(wrap().classList.contains('frozen-left')).toBeFalse();
    expect(wrap().classList.contains('frozen-right')).toBeFalse();

    const cells = firstBodyCells();
    expect(getComputedStyle(cells[0]).position)
      .withContext('không truyền frozenColumns mà ô vẫn sticky = lưới Người dùng bị đổi hành vi')
      .toBe('static');
    expect(getComputedStyle(cells.at(-1) as HTMLElement).position).toBe('static');
  });

  it('🛑 { left: true } ghim CỘT ĐẦU vào mép trái — đo bằng style thật, không đo tên class', async () => {
    await withFrozen({ left: true });

    const th = headerCells()[0];
    const td = firstBodyCells()[0];

    // `th` toàn cục vốn đã sticky theo trục dọc (styles.scss §7); thứ input này thêm vào là `left`.
    expect(getComputedStyle(th).position).toBe('sticky');
    expect(getComputedStyle(th).left).toBe('0px');
    expect(getComputedStyle(td).position).toBe('sticky');
    expect(getComputedStyle(td).left).toBe('0px');
  });

  it('{ left: true } KHÔNG đụng tới cột cuối', async () => {
    await withFrozen({ left: true });

    expect(getComputedStyle(headerCells().at(-1) as HTMLElement).right).not.toBe('0px');
    expect(getComputedStyle(firstBodyCells().at(-1) as HTMLElement).position).toBe('static');
  });

  it('🛑 { right: true } ghim CỘT CUỐI vào mép phải (alignFrozen="right" của hợp đồng)', async () => {
    await withFrozen({ right: true });

    const th = headerCells().at(-1) as HTMLElement;
    const td = firstBodyCells().at(-1) as HTMLElement;

    expect(getComputedStyle(th).right).toBe('0px');
    expect(getComputedStyle(td).position).toBe('sticky');
    expect(getComputedStyle(td).right).toBe('0px');
  });

  it('hai mép ghim cùng lúc — hình dạng mà lưới Danh mục DTI yêu cầu (Q30)', async () => {
    await withFrozen({ left: true, right: true });

    expect(getComputedStyle(firstBodyCells()[0]).left).toBe('0px');
    expect(getComputedStyle(firstBodyCells().at(-1) as HTMLElement).right).toBe('0px');
  });

  it('ô được ghim có nền ĐỤC — không có nền thì các ô cuộn qua hiện xuyên qua nó', async () => {
    await withFrozen({ left: true, right: true });

    for (const cell of [firstBodyCells()[0], firstBodyCells().at(-1) as HTMLElement]) {
      const background = getComputedStyle(cell).backgroundColor;
      expect(background).withContext('nền ô ghim không được trong suốt').not.toBe('transparent');
      expect(background).not.toBe('rgba(0, 0, 0, 0)');
    }
  });

  it('🛑 dòng "không có dữ liệu" KHÔNG bị ghim — nó chỉ có MỘT ô, vừa đầu vừa cuối', async () => {
    fixture.componentInstance.rows.set([]);
    await withFrozen({ left: true, right: true });

    const cell = fixture.nativeElement.querySelector('tbody td') as HTMLElement;
    expect(cell).withContext('lưới rỗng phải render dòng câu rỗng').not.toBeNull();
    expect(getComputedStyle(cell).position)
      .withContext('ghim ô duy nhất của dòng rỗng = câu rỗng dính mép trong khi dòng trôi qua')
      .toBe('static');
  });

  it('ca đối chứng — phép đo trên PHÂN BIỆT được ghim với không ghim', async () => {
    // Không có `it` này thì mọi phép kiểm trên vẫn xanh nếu `getComputedStyle` trả `'0px'` cho mọi
    // thứ vì một lý do nào đó. So hai trạng thái của CÙNG một ô mới là phép đo có nghĩa.
    await withFrozen(undefined);
    const before = getComputedStyle(firstBodyCells()[0]).position;

    await withFrozen({ left: true });
    const after = getComputedStyle(firstBodyCells()[0]).position;

    expect(before).toBe('static');
    expect(after).toBe('sticky');
  });
});

/**
 * Phép quy đổi `first` 0-based của PrimeNG ↔ `page` 1-based của API. Nó là lý do `data-grid` tồn
 * tại (JSDoc `onLazyLoad`), và trước hôm nay không test nào chạm tới nó.
 */
describe('DataGrid — quy đổi trang 1-based', () => {
  it('🛑 phát pageChange với trang 1-based, không phải chỉ số dòng đầu', async () => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection(), provideTranslateService()],
    });
    await useTranslationsInTest();

    const fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
    await fixture.whenStable();

    // `onLazyLoad` là `protected` — nó là hợp đồng với `p-table`, không phải API công khai. Gọi
    // qua chỉ số chuỗi chứ không nới lỏng thành `public`: test không được đổi hình dạng của thứ
    // nó đo.
    const grid = fixture.debugElement.query(By.directive(DataGrid)).componentInstance as Record<
      string,
      (event: { first: number; rows: number }) => void
    >;
    grid['onLazyLoad']({ first: 40, rows: 20 });

    expect(fixture.componentInstance.lastPageChange).toEqual({ page: 3, pageSize: 20 });
  });
});
