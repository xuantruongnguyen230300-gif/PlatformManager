import { IUser, IUserDto } from '../models/quan-tri-nguoi-dung.model';

/**
 * `?? null` cho MỌI field nullable phía BE — cùng khuôn đã áp ở `core/menu/menu.service.ts`, và
 * cùng lý do: `Program.cs` bật `DefaultIgnoreCondition = WhenWritingNull`, nên một property C#
 * bằng `null` (`UserDto.DateCreate` là `DateTimeOffset?`, `UserDto.Version` là `string?`) về tới
 * đây là **key vắng mặt** ⇒ `undefined`, không phải `null`.
 *
 * Vì sao phải chuẩn hoá chứ không gán thẳng: TypeScript bị xoá lúc chạy, nên một model khai
 * `string | null` mà thực tế mang `undefined` không sai ở đâu cả cho tới lúc có người viết
 * `=== null` — rồi nhánh đó im lặng không bao giờ chạy. Đó là lỗi THẬT đã xảy ra ở
 * `platform/phan-quyen/services/phan-quyen.mapper.ts`, ghi lại ở `core/menu/menu.service.ts`.
 *
 * `DateCreate` còn một đường hỏng thứ hai, đắt hơn: nó đang hiển thị thật ở
 * `components/user-grid-table/user-grid-table.html` qua `DatePipe`. `DatePipe` nuốt cả `null` lẫn
 * `undefined` thành ô trống, nên cột này KHÔNG bao giờ tự tố cáo hình dạng sai.
 */
export function mapUserDtoToModel(dto: IUserDto): IUser {
  return {
    Id: dto.id,
    UserName: dto.userName,
    Email: dto.email,
    FullName: dto.fullName,
    Roles: dto.roles,
    IsLocked: dto.isLocked,
    MustChangePassword: dto.mustChangePassword,
    DateCreate: dto.dateCreate ?? null,
    Version: dto.version ?? null,
  };
}
