import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { DanhMucDtiPage } from './danh-muc-dti.page';
import { APP_I18N } from '../../../../app.config';
import { CORE_I18N } from '../../../../core/i18n/core-i18n';
import { useTranslationsInTest } from '../../../../core/i18n/i18n.testing';

/**
 * Màn nghiệp vụ đầu tiên lấy chuỗi từ nhóm khoá DỰ ÁN (`public/i18n-app/`), không phải nhóm Core
 * (`public/i18n/`). Đây là phép kiểm ĐẦU-CUỐI của ranh giới đó, và nó bắt một lỗi mà không gate
 * nào bắt được: khoá nằm nhầm file — hoặc `APP_I18N.resources` thiếu tiền tố thứ hai — thì
 * ngx-translate trả về chính chuỗi khoá, nên màn hình hiện `danh-muc-dti.title` giữa giao diện.
 * Build xanh, lint xanh, G12 xanh (template CÓ bọc `| translate`), và không ai biết.
 *
 * 🛑 `{ provide: CORE_I18N, useValue: APP_I18N }` là dòng LÀM NÊN phép kiểm, không phải thủ tục
 * dựng TestBed: `useTranslationsInTest()` nạp theo `resources` của cấu hình i18n mà nó tìm thấy
 * trong TestBed. Gỡ dòng đó ⇒ helper lùi về nguồn Core một mình ⇒ khoá dự án tra không trúng ⇒
 * `it` đầu tiên ĐỎ. Đó chính là ca đối chứng của chính spec này.
 */
describe('DanhMucDtiPage — khung màn Danh mục DTI', () => {
  async function render(): Promise<HTMLElement> {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideTranslateService(),
        { provide: CORE_I18N, useValue: APP_I18N },
      ],
    });
    await useTranslationsInTest();

    const fixture = TestBed.createComponent(DanhMucDtiPage);
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  it('🛑 tiêu đề tra ra CÂU từ nhóm khoá dự án, không phải chuỗi khoá', async () => {
    const heading = (await render()).querySelector('h2') as HTMLElement;

    expect(heading).withContext('trang phải có hàng tiêu đề .title > h2').not.toBeNull();
    expect(heading.textContent?.trim())
      .withContext(
        'tra không trúng thì ngx-translate trả về chính chuỗi khoá — dạng hỏng hiện ' +
          '"danh-muc-dti.title" giữa giao diện mà không lỗi, không cảnh báo',
      )
      .toBe('Danh mục DTI');
  });

  it('trang dùng chuỗi chiều cao .page-fill — nửa đầu của hợp đồng lưới cuộn trong', async () => {
    // Nửa sau (`:host { display: contents }`) nằm ở .scss; thiếu nửa nào lưới cũng âm thầm về
    // chiều cao nội dung (spec/danh-muc-dti/ui-spec.md §3.2). Khoá nửa đầu ngay từ khung để lượt
    // dựng lưới thật không phải phát hiện lại.
    expect((await render()).querySelector('.page-fill > .card')).not.toBeNull();
  });
});
