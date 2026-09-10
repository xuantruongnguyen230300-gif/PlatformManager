import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { DashboardPage } from './dashboard.page';
import { APP_I18N } from '../../../../app.config';
import { CORE_I18N } from '../../../../core/i18n/core-i18n';
import { useTranslationsInTest } from '../../../../core/i18n/i18n.testing';

/**
 * `modules/dashboard/` chưa có route nào trỏ tới (chốt Q3: nó sẽ THAY `/trang-chu`, xem
 * `../../dashboard.routes.ts`). Vì vậy nó không nằm trên đường đi của `app.routes.spec.ts` lẫn
 * `app-i18n.spec.ts` — không phép kiểm nào chạm tới nó, và một khung hỏng sẽ nằm im tới tận lượt
 * hoán đổi route. Spec này là chỗ duy nhất giữ nó sống.
 *
 * Chuỗi tiêu đề đến từ nhóm khoá DỰ ÁN (`public/i18n-app/`), nên TestBed phải cấp `CORE_I18N` —
 * `useTranslationsInTest()` lấy danh sách nguồn từ đó. Xem `core/i18n/i18n.testing.ts`.
 */
describe('DashboardPage — khung màn Tổng quan DTI', () => {
  it('🛑 tiêu đề tra ra CÂU từ nhóm khoá dự án, không phải chuỗi khoá', async () => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideTranslateService(),
        { provide: CORE_I18N, useValue: APP_I18N },
      ],
    });
    await useTranslationsInTest();

    const fixture = TestBed.createComponent(DashboardPage);
    fixture.detectChanges();

    const heading = (fixture.nativeElement as HTMLElement).querySelector('h2') as HTMLElement;
    expect(heading).not.toBeNull();
    expect(heading.textContent?.trim()).toBe('Tổng quan DTI');
  });
});
