import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TranslateService, provideTranslateService } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';
import { ApiErrorMessageService } from './api-error-message.service';
import { useTranslationsInTest } from './i18n.testing';
import { IApiResult } from '../http/api-result.model';

/**
 * `ApiErrorMessageService` là **nơi duy nhất** của FE biến một lỗi HTTP thành câu người dùng đọc
 * được, nên nó cũng là nơi duy nhất "hướng A" (BE trả MÃ, FE dịch —
 * doc/huong_dan/wiki-core/fe/08-i18n.md §"Chuỗi BE trả về") có thể hỏng.
 *
 * Bảng dịch nạp là bảng THẬT (`public/i18n/*.json`), không phải stub: một stub sẽ làm test xanh kể
 * cả khi mã lỗi không hề có mặt trong bảng dịch được deploy.
 */
describe('ApiErrorMessageService', () => {
  let service: ApiErrorMessageService;
  let translate: TranslateService;

  function envelope(overrides: Partial<IApiResult<null>>): IApiResult<null> {
    return {
      data: null,
      message: null,
      status: 'BUSINESS_ERROR',
      code: 'BusinessRuleError',
      businessCode: null,
      traceId: 'trace-test',
      retryable: false,
      fields: null,
      ...overrides,
    };
  }

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection(), provideTranslateService()],
    });
    translate = await useTranslationsInTest('vi');
    service = TestBed.inject(ApiErrorMessageService);
  });

  it('🛑 businessCode → câu đã dịch, đổi theo ngôn ngữ đang chọn', async () => {
    const result = envelope({
      businessCode: 'AUTH.INVALID_CREDENTIALS',
      // Câu dev-facing tiếng Việt của BE — CỐ Ý để ở đây: nếu service đọc `message` thay vì tra mã
      // thì vế tiếng Anh dưới đây đỏ, tức test phân biệt được hai cách cài đặt.
      message: 'Tên đăng nhập hoặc mật khẩu không đúng.',
    });

    expect(service.messageFor(result, 422)).toBe('Tên đăng nhập hoặc mật khẩu không đúng.');

    await firstValueFrom(translate.use('en'));

    expect(service.messageFor(result, 422)).toBe('The user name or password is incorrect.');
  });

  it('🛑 messageParams được ráp vào câu — cùng cơ chế ở cả hai ngôn ngữ', async () => {
    const result = envelope({
      businessCode: 'USER.DUPLICATE_USERNAME',
      message: "Tên đăng nhập 'nguyen.van.a' đã tồn tại.",
      messageParams: { UserName: 'nguyen.van.a' },
    });

    expect(service.messageFor(result, 409)).toBe("Tên đăng nhập 'nguyen.van.a' đã tồn tại.");

    await firstValueFrom(translate.use('en'));

    expect(service.messageFor(result, 409)).toBe("The user name 'nguyen.van.a' already exists.");
  });

  it('🛑 mã CHƯA có bản dịch → lùi về `message` của BE, KHÔNG hiện chuỗi khoá', () => {
    // Cái giá đã chấp nhận và ghi rõ ở doc/huong_dan/wiki-core/fe/08-i18n.md §"Chuỗi BE trả về":
    // BE thêm mã mới mà FE quên thêm bản dịch thì build vẫn xanh. Đường lùi này quyết định hậu quả
    // là "một câu tiếng Việt giữa giao diện tiếng Anh" (xấu, vẫn hiểu được) thay vì
    // "CRITERIA.DUPLICATE_CODE" (vô nghĩa với người dùng).
    const result = envelope({
      businessCode: 'CRITERIA.DUPLICATE_CODE',
      message: 'Mã chỉ tiêu đã tồn tại.',
    });

    expect(service.messageFor(result, 409)).toBe('Mã chỉ tiêu đã tồn tại.');
  });

  it('🛑 ca đối chứng — ngx-translate trả về CHÍNH CHUỖI KHOÁ khi tra trượt', () => {
    // Đây là lý do `translateCode` phải so kết quả với chuỗi khoá thay vì kiểm null: thư viện
    // không ném, không trả null, không cảnh báo. Không có phép so đó thì nhánh lùi ở `it` trên
    // KHÔNG BAO GIỜ chạy, và màn hình hiện "CRITERIA.DUPLICATE_CODE" giữa giao diện.
    expect(translate.instant('CRITERIA.DUPLICATE_CODE')).toBe('CRITERIA.DUPLICATE_CODE');
    expect(service.translateCode('CRITERIA.DUPLICATE_CODE')).toBeNull();
  });

  it('🛑 không có envelope → câu dự phòng theo mã HTTP, kèm tiêu đề', () => {
    expect(service.messageFor(null, 0)).toBe('Không thể kết nối tới máy chủ. Kiểm tra kết nối mạng.');
    expect(service.titleFor(null, 0)).toBe('Mất kết nối');

    expect(service.messageFor(null, 403)).toBe('Bạn không có quyền thực hiện thao tác này.');
    expect(service.titleFor(null, 403)).toBe('Không đủ quyền');

    // Mã không có trong bảng → câu chung, KHÔNG phải chuỗi rỗng.
    expect(service.messageFor(null, 500)).toBe('Đã có lỗi xảy ra. Vui lòng thử lại.');
    expect(service.titleFor(null, 500)).toBe('Lỗi hệ thống');
  });

  it('envelope có câu thật → KHÔNG có tiêu đề (giữ nguyên quyết định cũ của interceptor)', () => {
    const result = envelope({ businessCode: 'AUTH.LOCKED_OUT', message: 'x' });

    expect(service.titleFor(result, 422))
      .withContext(
        'bịa một tiêu đề chung chung ("Lỗi") cho câu do BE soạn riêng cho nghiệp vụ đang lỗi chỉ ' +
          'chiếm chỗ mà không thêm thông tin',
      )
      .toBeUndefined();
  });

  it('🛑 fieldErrors — cùng MỘT cơ chế, chỉ khác chỗ đặt: code + messageParams', async () => {
    const fieldError = {
      code: 'NotEmptyValidator',
      message: "'Password' không được rỗng.",
      messageParams: { PropertyName: 'Password' },
    };

    expect(service.fieldMessage(fieldError)).toBe('Vui lòng nhập Password.');
    // Màn hình ghi đè bằng nhãn ô nhập của chính nó, nên câu tiếng Việt không lẫn chữ tiếng Anh
    // do FluentValidation sinh ra từ tên property C#.
    expect(service.fieldMessage(fieldError, { PropertyName: 'Mật khẩu' })).toBe(
      'Vui lòng nhập Mật khẩu.',
    );

    await firstValueFrom(translate.use('en'));

    expect(service.fieldMessage(fieldError)).toBe('Please enter Password.');
  });

  it('fieldErrors — validator CHƯA có bản dịch thì lùi về `message` của BE', () => {
    expect(
      service.fieldMessage({ code: 'GreaterThanValidator', message: "'Điểm' phải lớn hơn 0." }),
    ).toBe("'Điểm' phải lớn hơn 0.");
  });
});
