import { IUserDto } from '../models/quan-tri-nguoi-dung.model';
import { mapUserDtoToModel } from './quan-tri-nguoi-dung.mapper';

/** DTO đầy đủ — mọi field nullable đều CÓ giá trị. Từng test tự khuyết đi phần nó quan tâm. */
const DAY_DU: IUserDto = {
  id: 'u1',
  userName: 'nguyenvana',
  email: 'a@example.com',
  fullName: 'Nguyễn Văn A',
  roles: ['Admin'],
  isLocked: false,
  mustChangePassword: true,
  dateCreate: '2026-08-29T03:00:00Z',
  version: 'stamp-1',
};

/**
 * Mapper này canh đúng MỘT lớp lỗi, và nó là lớp lỗi đã xảy ra thật ở repo này hơn một lần.
 *
 * `Program.cs` của BE bật `DefaultIgnoreCondition = WhenWritingNull`, nên một property C# bằng
 * `null` KHÔNG về tới FE dưới dạng `null` — nó **biến mất khỏi JSON**. Model app thì khai
 * `string | null`. Hai hình dạng, một nghĩa; và TypeScript bị xoá lúc chạy nên không ai báo.
 *
 * Vì sao không có test nào khác bắt được:
 *  · `DatePipe` nuốt cả `null` lẫn `undefined` thành ô trống ⇒ cột "Ngày tạo" không bao giờ tố cáo.
 *  · `?.`/truthy-check chạy đúng với cả hai ⇒ mọi đường hiện tại đều xanh.
 * Nó chỉ nổ vào ngày có người viết `=== null` — nhánh đó im lặng không bao giờ chạy. Đó chính là
 * lỗi thật đã xảy ra ở `platform/phan-quyen/services/phan-quyen.mapper.ts`, ghi lại ở
 * `core/menu/menu.service.ts`.
 */
describe('mapUserDtoToModel — chuẩn hoá field vắng mặt', () => {
  it('DTO đầy đủ: đổi camelCase sang PascalCase, giữ nguyên giá trị', () => {
    expect(mapUserDtoToModel(DAY_DU)).toEqual({
      Id: 'u1',
      UserName: 'nguyenvana',
      Email: 'a@example.com',
      FullName: 'Nguyễn Văn A',
      Roles: ['Admin'],
      IsLocked: false,
      MustChangePassword: true,
      DateCreate: '2026-08-29T03:00:00Z',
      Version: 'stamp-1',
    });
  });

  /**
   * `dateCreate` là `DateTimeOffset?` (`UserDto.cs:20`) và `version` là `string?` (`:21`) — hai
   * field DUY NHẤT của DTO này đi qua `WhenWritingNull`. Dựng ca test bằng cách `delete` khoá
   * chứ không gán `undefined`: đó mới là hình dạng JSON thật khi BE bỏ qua field.
   */
  for (const khoa of ['dateCreate', 'version'] as const) {
    it(`khoá \`${khoa}\` VẮNG MẶT trong JSON ⇒ model nhận null, không phải undefined`, () => {
      const thieu: IUserDto = { ...DAY_DU };
      delete thieu[khoa];

      const model = mapUserDtoToModel(thieu);
      const nhan = khoa === 'dateCreate' ? model.DateCreate : model.Version;

      // `toBeNull()` chứ KHÔNG `toBeFalsy()`: `undefined` cũng falsy, nên `toBeFalsy` sẽ xanh với
      // đúng cái bug này. Phân biệt null/undefined là toàn bộ nội dung của test.
      expect(nhan)
        .withContext(
          `BE bật WhenWritingNull nên \`${khoa}\` = null phía C# về tới đây là KEY VẮNG MẶT. ` +
            'Mapper phải chuẩn hoá bằng `?? null` để model đúng như type đã khai.',
        )
        .toBeNull();
      expect(nhan).not.toBeUndefined();
    });
  }

  it('`null` tường minh (BE không bật WhenWritingNull) cũng ra null — hai đường về một chỗ', () => {
    const model = mapUserDtoToModel({ ...DAY_DU, dateCreate: null, version: null });

    expect(model.DateCreate).toBeNull();
    expect(model.Version).toBeNull();
  });
});
