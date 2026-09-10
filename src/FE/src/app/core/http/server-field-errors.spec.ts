import { ApiFieldError } from './api-result.model';
import { groupServerFieldErrors } from './server-field-errors';

/** Dịch giả lập — trả thẳng `message` để test đo phần GOM, không đo phần dịch. */
const passthrough = (error: ApiFieldError): string => error.message;

function fieldError(code: string, message: string): ApiFieldError {
  return { code, message };
}

/**
 * Ba hành vi khoá ở đây đều là thứ hỏng IM LẶNG khi mất: form từ chối, không ô nào đỏ, không lỗi
 * nào ném ra. Không test nào khác trong bộ FE bắt được — dialog người dùng chỉ khẳng định câu
 * hiện ra đúng cho khoá không có chỉ số.
 *
 * Luật: doc/huong_dan/wiki-core/fe/09-forms-validation.md §"Bind lỗi từ `fieldErrors` vào form".
 */
describe('groupServerFieldErrors', () => {
  it('cắt hậu tố chỉ số của FluentValidation: `Roles[0]`/`Roles[1]` gộp về `Roles`', () => {
    // `RuleForEach(x => x.Roles)` phát PropertyName kèm chỉ số; BE giữ nguyên chuỗi đó.
    const result = groupServerFieldErrors(
      {
        'Roles[0]': [fieldError('PredicateValidator', 'Vai trò không hợp lệ.')],
        'Roles[1]': [fieldError('PredicateValidator', 'Vai trò trùng.')],
      },
      passthrough,
    );

    expect(Object.keys(result)).toEqual(['Roles']);
    expect(result['Roles']).toBe('Vai trò không hợp lệ. Vai trò trùng.');
  });

  it('chỉ cắt chỉ số Ở CUỐI khoá, giữ nguyên khoá không có chỉ số', () => {
    const result = groupServerFieldErrors(
      {
        UserName: [fieldError('NotEmptyValidator', 'Bắt buộc.')],
        'Items[2].Code': [fieldError('NotEmptyValidator', 'Thiếu mã.')],
      },
      passthrough,
    );

    expect(result['UserName']).toBe('Bắt buộc.');
    // Chỉ số nằm giữa khoá KHÔNG bị cắt — nó là đường dẫn tới một ô khác, không phải cùng ô.
    expect(result['Items[2].Code']).toBe('Thiếu mã.');
  });

  it('gộp nhiều thông điệp của cùng một ô thành MỘT chuỗi', () => {
    const result = groupServerFieldErrors(
      {
        TempPassword: [
          fieldError('MinimumLengthValidator', 'Tối thiểu 12 ký tự.'),
          fieldError('PredicateValidator', 'Phải có chữ hoa.'),
        ],
      },
      passthrough,
    );

    expect(result['TempPassword']).toBe('Tối thiểu 12 ký tự. Phải có chữ hoa.');
  });

  it('dịch TỪNG lỗi qua callback — phần tử là mã + tham số, không phải câu dựng sẵn', () => {
    const translate = (error: ApiFieldError): string => `[${error.code}] ${error.messageParams?.['MinLength'] ?? ''}`;

    const result = groupServerFieldErrors(
      { Email: [{ code: 'MinimumLengthValidator', message: 'bỏ qua', messageParams: { MinLength: '12' } }] },
      translate,
    );

    expect(result['Email']).toBe('[MinimumLengthValidator] 12');
  });

  it('trả object rỗng khi `fields` là null', () => {
    expect(groupServerFieldErrors(null, passthrough)).toEqual({});
  });

  it('trả object rỗng khi `fields` là object rỗng — không ném', () => {
    expect(groupServerFieldErrors({}, passthrough)).toEqual({});
  });
});
