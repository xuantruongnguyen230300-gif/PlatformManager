import { ApiFieldError } from './api-result.model';
import { RECORD_FIELD_KEY, groupServerFieldErrors } from './server-field-errors';

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

    expect(Object.keys(result.byField)).toEqual(['Roles']);
    expect(result.byField['Roles']).toBe('Vai trò không hợp lệ. Vai trò trùng.');
  });

  it('chỉ cắt chỉ số Ở CUỐI khoá, giữ nguyên khoá không có chỉ số', () => {
    const result = groupServerFieldErrors(
      {
        UserName: [fieldError('NotEmptyValidator', 'Bắt buộc.')],
        'Items[2].Code': [fieldError('NotEmptyValidator', 'Thiếu mã.')],
      },
      passthrough,
    );

    expect(result.byField['UserName']).toBe('Bắt buộc.');
    // Chỉ số nằm giữa khoá KHÔNG bị cắt — nó là đường dẫn tới một ô khác, không phải cùng ô.
    expect(result.byField['Items[2].Code']).toBe('Thiếu mã.');
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

    expect(result.byField['TempPassword']).toBe('Tối thiểu 12 ký tự. Phải có chữ hoa.');
  });

  it('dịch TỪNG lỗi qua callback — phần tử là mã + tham số, không phải câu dựng sẵn', () => {
    const translate = (error: ApiFieldError): string => `[${error.code}] ${error.messageParams?.['MinLength'] ?? ''}`;

    const result = groupServerFieldErrors(
      { Email: [{ code: 'MinimumLengthValidator', message: 'bỏ qua', messageParams: { MinLength: '12' } }] },
      translate,
    );

    expect(result.byField['Email']).toBe('[MinimumLengthValidator] 12');
  });

  it('trả object rỗng khi `fieldErrors` là null', () => {
    expect(groupServerFieldErrors(null, passthrough)).toEqual({ byField: {}, record: null });
  });

  it('trả object rỗng khi `fieldErrors` là object rỗng — không ném', () => {
    expect(groupServerFieldErrors({}, passthrough)).toEqual({ byField: {}, record: null });
  });

  it('🛑 `$record` KHÔNG lẫn vào lỗi theo ô — nó ra nhánh `record` riêng', () => {
    // Ca đã hỏng thật 2026-09-11: `CommonPassword` rơi về `$record` (BE chưa ánh xạ mã đó về ô
    // nào), và khi khoá này còn nằm chung với lỗi theo ô thì nó hỏng HAI tầng cùng lúc — không
    // template nào bind `$record` nên câu biến mất, mà sự có mặt của nó lại làm
    // `generalError`/`errorMessage` của form trả `null`. Form từ chối, không một chữ nào giải thích.
    //
    // `CommonPassword` nay đã được BE ánh xạ về ô mật khẩu (`IdentityFieldErrors.SlotByCode`), nên
    // test này dùng `ConcurrencyFailure` — một mã CỐ Ý không có trong bảng ánh xạ đó vì nó nói về
    // BẢN GHI chứ không về một ô. Bảng kia là allowlist: mã Identity mới mặc định rơi về `$record`,
    // nên nhánh này là đường sống, không phải ca lịch sử.
    const result = groupServerFieldErrors(
      {
        [RECORD_FIELD_KEY]: [fieldError('ConcurrencyFailure', 'ConcurrencyFailure')],
        Email: [fieldError('InvalidEmail', 'Email sai định dạng.')],
      },
      passthrough,
    );

    expect(Object.keys(result.byField))
      .withContext('`$record` lọt vào `fields` = lỗ đen: không ô nào hiện nó, mà nó lại che câu chung')
      .toEqual(['Email']);
    expect(result.record).toBe('ConcurrencyFailure');
  });

  it('gộp nhiều lỗi mức bản ghi thành MỘT câu, y như gộp theo ô', () => {
    const result = groupServerFieldErrors(
      {
        [RECORD_FIELD_KEY]: [
          fieldError('ConcurrencyFailure', 'Bản ghi vừa đổi.'),
          fieldError('DefaultError', 'Không rõ nguyên nhân.'),
        ],
      },
      passthrough,
    );

    expect(result.byField).toEqual({});
    expect(result.record).toBe('Bản ghi vừa đổi. Không rõ nguyên nhân.');
  });

  it('`record` là null khi envelope không có lỗi mức bản ghi — không phải chuỗi rỗng', () => {
    // Chuỗi rỗng là falsy nhưng KHÁC null ở chỗ nó đi qua `??`; nơi gọi dùng
    // `record ?? <câu chung>` nên một chuỗi rỗng sẽ CHIẾM CHỖ câu chung và hiện ra một khối lỗi
    // trống. Khoá hành vi này lại.
    expect(groupServerFieldErrors({ Email: [fieldError('InvalidEmail', 'x')] }, passthrough).record).toBeNull();
  });
});
