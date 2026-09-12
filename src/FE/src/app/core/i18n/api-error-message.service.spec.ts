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

  it('🛑 `CommonPassword` có bản dịch ở CẢ hai ngôn ngữ, và câu nói rõ phải làm gì', async () => {
    // Mã này là thứ `AddTop10000PasswordValidator` phát ra và là mã Identity DUY NHẤT liên quan
    // tới độ mạnh mật khẩu mà chính sách hiện hành thật sự kích hoạt được — bốn mã
    // `PasswordRequires*` đang chết vì `options.Password.Require* = false`. Nó từng KHÔNG có
    // trong bảng dịch, và đó là nửa đầu của sự cố 2026-09-11.
    const fieldError = { code: 'CommonPassword', message: 'CommonPassword' };

    const vi = service.fieldMessage(fieldError);
    expect(vi).toContain('phổ biến');
    expect(vi)
      .withContext('câu phải nói LỐI RA (dùng cụm từ dài, ít gặp), không chỉ nói cái sai')
      .toContain('cụm từ dài');
    expect(vi)
      .withContext('hệ thống KHÔNG đòi luật thành phần — nói thế là dạy người dùng sai')
      .not.toContain('chữ hoa');

    await firstValueFrom(translate.use('en'));

    expect(service.fieldMessage(fieldError)).toContain('commonly used');
  });

  it('🛑 mã LẠ mà BE gửi `message` BẰNG CHÍNH mã → câu chung, KHÔNG phải chuỗi mã', async () => {
    // Nửa sau của sự cố 2026-09-11, và là phần đáng sửa hơn. `IdentityFieldErrors.Build` dựng
    // `new ApiFieldError(code, code)` — đúng chính sách i18n (BE gửi MÃ, FE dịch), nên đường lùi
    // `?? fieldError.message` không phải đường lùi: nó đẩy thẳng định danh nội bộ ra màn hình cho
    // MỌI mã Identity chưa dịch. `IdentityErrorDescriber` có hơn 20 mã; bảng dịch sẽ luôn thiếu
    // một cái nào đó, nên đây là lớp bảo vệ chứ không phải ca hiếm.
    const message = service.fieldMessage({ code: 'UserAlreadyHasPassword', message: 'UserAlreadyHasPassword' });

    expect(message).toBe('Giá trị này không được chấp nhận. Vui lòng kiểm tra lại.');
    expect(message).not.toContain('UserAlreadyHasPassword');

    await firstValueFrom(translate.use('en'));

    expect(service.fieldMessage({ code: 'UserAlreadyHasPassword', message: 'UserAlreadyHasPassword' })).toBe(
      'This value was not accepted. Please check it and try again.',
    );
  });

  it('🛑 mã lạ KHÔNG kèm câu nào → vẫn ra một câu, không bao giờ `undefined`/rỗng', () => {
    // `ApiFieldError.message` khai `string` ở FE, nhưng TypeScript bị xoá lúc chạy: một envelope
    // thiếu trường đó (bản BE khác, proxy cắt bớt, endpoint mới) cho ra `undefined`, và đường lùi
    // cũ trả thẳng `undefined` — template bind vào thì KHÔNG render gì cả, đúng dạng hỏng im lặng
    // mà người dùng báo: "form từ chối nhưng không có chữ nào".
    const missing = service.fieldMessage({ code: 'LạMã' } as never);
    expect(missing).toBe('Giá trị này không được chấp nhận. Vui lòng kiểm tra lại.');

    expect(service.fieldMessage({ code: 'InvalidToken', message: '   ' })).toBe(
      'Giá trị này không được chấp nhận. Vui lòng kiểm tra lại.',
    );
  });
});
