import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { IApiResult, unwrapData } from '../../../core/http/api-result.model';
import { IPagedResult, IPagedResultDto, mapPagedResultDto } from '../../../core/http/paged-result.model';
import {
  ICreateUserPayload,
  IUpdateUserPayload,
  IUpdateUserRequestDto,
  IUser,
  IUserDto,
  IUserListParams,
} from '../models/quan-tri-nguoi-dung.model';
import { mapUserDtoToModel } from './quan-tri-nguoi-dung.mapper';

/**
 * Envelope có BÁO thành công không.
 *
 * Không rút gọn thành "gọi xong không lỗi mạng ⇒ thành công" — đó đúng là con đường đã sinh ra
 * bug: bản BE trước 2026-08-29 trả **200 + `data: false`** khi tầng ghi Identity hỏng, FE map
 * `() => undefined` rồi hiện toast *"Đã khoá tài khoản."* cho một thao tác chưa khoá được gì, mà
 * đường `lock` còn để lại trạng thái NỬA VỜI (con dấu bảo mật đã đổi, lockout chưa set: người
 * dùng bị chấm dứt phiên nhưng vẫn đăng nhập lại được). Xem doc/contracts/users.md §"Thao tác
 * hỏng nay là LỖI".
 *
 * BE hiện tại trả `Fail` (422 `USER.LOCK_FAILED`/`USER.UNLOCK_FAILED`/`USER.UPDATE_FAILED`) nên
 * nhánh hỏng rơi vào `error` và `httpErrorInterceptor` đã hiện `message`. Kiểm ở đây là chốt chặn
 * còn lại cho FE chạy với một BE chưa cập nhật — thứ duy nhất ngăn lời nói dối im lặng đó quay
 * lại.
 */
function envelopeSucceeded(res: IApiResult<boolean>): boolean {
  return res.status === 'SUCCESS' && res.data === true;
}

/** Gọi API Quản trị người dùng — xem doc/contracts/users.md. Gate BE: `SuperAdmin,Admin`. */
@Injectable({ providedIn: 'root' })
export class QuanTriNguoiDungService {
  private readonly http = inject(HttpClient);

  getList(params: IUserListParams): Observable<IPagedResult<IUser>> {
    let httpParams = new HttpParams().set('page', params.Page).set('pageSize', params.PageSize);
    if (params.SearchText) httpParams = httpParams.set('searchText', params.SearchText);
    // `role`/`isLocked`: CONTRACT USER-6 đã **AGREED** và BE đã hiện thực hoá (2026-08-29,
    // `GetUsersListQuery` + `UserAdminService` lọc ở tầng truy vấn) — hai tham số này lọc THẬT,
    // không còn bị model binding bỏ qua như bản DRAFT. Tên tham số giữ nguyên nên FE không phải
    // sửa gì; `role` sai giá trị nay bị BE trả 400 `ValidationError` kèm `fields.Role` — key của
    // `fields` là **PascalCase** khớp tên property C# (`GetUsersListQuery.Role`), CỐ Ý khác casing
    // của payload; xem doc/huong_dan/quy-uoc/fe-api-client.md §Envelope. Sửa 2026-08-29: bản trước
    // ghi `fields.role`, đọc key đó sẽ luôn ra `undefined`. FE hiện chỉ gửi 3 giá trị hợp lệ từ
    // `<select>` nên đường này chưa chạy — nhưng đừng chép sai casing ra chỗ khác.
    if (params.Role) httpParams = httpParams.set('role', params.Role);
    if (params.IsLocked !== undefined) httpParams = httpParams.set('isLocked', params.IsLocked);
    // `unwrapData` chứ KHÔNG toán tử `??` kèm một lưới rỗng dựng sẵn (khuôn cũ, đổi 2026-09-08).
    // (Câu trên cố ý không viết ra khuôn cũ nguyên văn — lệnh grep ở §Service pattern quét cả file
    // .ts, một chú thích chứa nó sẽ làm cổng đỏ vì chính lời cảnh báo của mình.) Lưới rỗng
    // là một câu trả lời HỢP LỆ của server — "không có tài khoản nào khớp bộ lọc" — nên dựng nó ra
    // từ một envelope HỎNG là nói dối bằng đúng thứ giao diện người dùng tin nhất: một bảng trống
    // kèm dòng "Không có dữ liệu", không toast, không dấu vết. Envelope thiếu `data` nay ném lỗi,
    // rơi vào `catchError` của trang và hiện KHỐI LỖI (xem `fetchList`/`applyListOutcome` ở
    // pages/quan-tri-nguoi-dung/quan-tri-nguoi-dung.page.ts) — cùng đường mà mọi hỏng hóc khác của
    // lượt tải này đã đi. Luật: doc/huong_dan/quy-uoc/fe-api-client.md §Service pattern.
    return this.http
      .get<IApiResult<IPagedResultDto<IUserDto>>>('/users', { params: httpParams })
      .pipe(map((res) => mapPagedResultDto(unwrapData(res), mapUserDtoToModel)));
  }

  create(payload: ICreateUserPayload): Observable<string> {
    const body = {
      userName: payload.UserName,
      email: payload.Email,
      fullName: payload.FullName,
      tempPassword: payload.TempPassword,
      roles: payload.Roles,
    };
    return this.http.post<IApiResult<string>>('/users', body).pipe(map((res) => unwrapData(res)));
  }

  /**
   * `true` = envelope báo đã lưu. Xem `envelopeSucceeded` — nơi gọi PHẢI đọc giá trị này trước
   * khi báo thành công cho người dùng.
   *
   * **`version` bật lớp chống ghi đè** (nối 2026-09-08, hoàn tất quyết định 3 của
   * doc/contracts/users.md §"Quyết định người dùng 2026-08-30"). Trước ngày này FE chỉ gửi 3
   * trường `{ email, fullName, roles }`, mà BE lại chỉ kiểm khi client CÓ gửi token
   * (`UpdateUserCommand.cs:62`) — nên lớp bảo vệ 409 đã tồn tại đủ ở BE nhưng chưa bên nào bật
   * lên, và hai admin sửa cùng một người vẫn ghi đè nhau im lặng.
   *
   * Gửi `version: null` khi không có token (BE cũ) chứ không bỏ hẳn key: hai cách cho ra cùng
   * `cmd.Version is null` phía BE, nhưng gửi tường minh giữ hình dạng body ổn định — đọc log một
   * request là biết ngay FE có token hay không, thay vì phải đoán giữa "không có" và "quên gửi".
   */
  update(id: string, payload: IUpdateUserPayload): Observable<boolean> {
    const body: IUpdateUserRequestDto = {
      email: payload.Email,
      fullName: payload.FullName,
      roles: payload.Roles,
      version: payload.Version,
    };
    return this.http.put<IApiResult<boolean>>(`/users/${id}`, body).pipe(map(envelopeSucceeded));
  }

  lock(id: string): Observable<boolean> {
    return this.http.post<IApiResult<boolean>>(`/users/${id}/lock`, {}).pipe(map(envelopeSucceeded));
  }

  unlock(id: string): Observable<boolean> {
    return this.http.post<IApiResult<boolean>>(`/users/${id}/unlock`, {}).pipe(map(envelopeSucceeded));
  }
}
