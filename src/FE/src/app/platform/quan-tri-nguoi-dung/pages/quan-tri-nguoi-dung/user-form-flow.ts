import { Injectable, inject, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { QuanTriNguoiDungService } from '../../services/quan-tri-nguoi-dung.service';
import { ToastService } from '../../../../core/toast/toast.service';
import { ApiFieldError, IApiResult, IHttpErrorWithApiResult } from '../../../../core/http/api-result.model';
import { ICreateUserPayload, IUpdateUserPayload, IUser } from '../../models/quan-tri-nguoi-dung.model';
import { IUserFormSaveEvent } from '../../components/user-form-dialog/user-form-dialog';

/**
 * LUỒNG HỘP THOẠI TẠO/SỬA của màn Quản trị người dùng: trạng thái hộp thoại, gửi lệnh ghi, và
 * ÁNH XẠ LỖI envelope về hai chỗ hiển thị (câu chung + lỗi từng ô).
 *
 * Tách khỏi `QuanTriNguoiDungPage` (2026-09-10) theo đúng ranh giới nghiệp vụ, không theo số dòng:
 * lớp này không biết trang đang lọc gì, đang ở trang mấy, hay lưới đang ở trạng thái nào. Nó nhận
 * một sự kiện "người dùng bấm Lưu" và một callback "ghi xong thì nạp lại" — hết.
 *
 * Vì sao đáng tách: đây là khuôn mà mọi màn hình lưới + hộp thoại sau này chép lại. Để nguyên
 * trong trang thì màn có N hộp thoại sẽ có N bản sao của cùng chuỗi
 * `saving → gọi API → đóng/không đóng → toast/ánh xạ lỗi` nằm lẫn với logic lọc và phân trang.
 *
 * 🛑 `@Injectable()` KHÔNG `providedIn: 'root'` — khai trong `providers` của trang, nên vòng đời
 * trùng đúng vòng đời trang, y hệt các field `signal()` mà nó thay thế. Đưa lên `root` là để một
 * hộp thoại đang mở dở, kèm lỗi của lần trước, hiện lại ở lượt vào màn sau.
 */
@Injectable()
export class UserFormFlow {
  private readonly service = inject(QuanTriNguoiDungService);
  private readonly toast = inject(ToastService);
  private readonly translate = inject(TranslateService);

  readonly open = signal(false);
  /** Request tạo/sửa đang bay → khoá nút Lưu của hộp thoại (fe/09-forms-validation.md). */
  readonly saving = signal(false);
  readonly editing = signal<IUser | null>(null);
  readonly serverError = signal<string | null>(null);
  /**
   * `fieldErrors` của envelope lỗi form gần nhất — truyền thẳng xuống `UserFormDialog`.
   *
   * Đổi 2026-09-06 từ `fields`: lỗi nghiệp vụ (mã Identity) chỉ có ở `fieldErrors`, `fields` để
   * trống. Xem doc/huong_dan/wiki-core/fe/02-http-envelope.md §"Lỗi theo ô".
   */
  readonly serverFieldErrors = signal<Record<string, ApiFieldError[]> | null>(null);

  openCreate(): void {
    this.editing.set(null);
    this.clearErrors();
    this.saving.set(false);
    this.open.set(true);
  }

  openEdit(user: IUser): void {
    this.editing.set(user);
    this.clearErrors();
    this.saving.set(false);
    this.open.set(true);
  }

  close(): void {
    this.open.set(false);
  }

  /** `onSaved` chạy SAU khi envelope báo ghi thành công — trang dùng nó để nạp lại danh sách. */
  save(event: IUserFormSaveEvent, onSaved: () => void): void {
    const editing = this.editing();
    if (event.IsEditing && editing && event.Update) {
      // `Version` ghép Ở ĐÂY chứ không để form phát ra: token chống ghi đè thuộc về BẢN GHI đang
      // sửa (`editing`), không phải một ô nhập nào. Dùng đúng bản ghi đã mở form — KHÔNG tra
      // lại từ danh sách đang hiện — vì thứ cần so là trạng thái mà người này ĐÃ NHÌN THẤY lúc bấm
      // Sửa; tra lại danh sách hiện tại sẽ lấy nhầm bản vừa bị người khác ghi đè và làm 409 không
      // bao giờ xảy ra, tức vô hiệu hoá đúng lớp bảo vệ này.
      this.submitUpdate(editing.Id, { ...event.Update, Version: editing.Version }, onSaved);
    } else if (!event.IsEditing) {
      this.submitCreate(event.Create as ICreateUserPayload, onSaved);
    }
  }

  private clearErrors(): void {
    this.serverError.set(null);
    this.serverFieldErrors.set(null);
  }

  /**
   * Tách lỗi envelope thành 2 phần cho form: `fieldErrors` (bind vào từng ô) và `message` (câu chung).
   * Trước đây chỉ lấy `message`, nên mọi lỗi 400 hiện đúng một câu "Dữ liệu không hợp lệ." dù BE
   * đã nói rõ ô nào sai — trái quy tắc ở doc/huong_dan/quy-uoc/fe-api-client.md §Envelope.
   */
  private applyError(err: IHttpErrorWithApiResult, fallbackKey: string): void {
    const result: IApiResult<unknown> | null | undefined = err.apiResult;
    this.serverFieldErrors.set(result?.fieldErrors ?? null);
    // Câu dự phòng dịch NGAY tại đây: `serverError` cũng mang `message` của BE, tức đã là câu —
    // một trường, một loại giá trị. Nhánh này chỉ chạy khi envelope KHÔNG có `message` (lỗi mạng /
    // hạ tầng), lúc đó người dùng bấm Lưu lại chứ không đứng đổi ngôn ngữ.
    this.serverError.set(result?.message ?? (this.translate.instant(fallbackKey) as string));
  }

  private submitCreate(payload: ICreateUserPayload, onSaved: () => void): void {
    this.saving.set(true);
    this.service.create(payload).subscribe({
      next: () => {
        this.saving.set(false);
        this.open.set(false);
        onSaved();
        this.toast.success(this.translate.instant('quan-tri-nguoi-dung.toast.created') as string);
      },
      error: (err: IHttpErrorWithApiResult) => {
        this.saving.set(false);
        this.applyError(err, 'quan-tri-nguoi-dung.error.createFailed');
      },
    });
  }

  /**
   * **409 `USER.VERSION_CONFLICT` đi CHUNG đường với mọi lỗi khác của form** — không có nhánh
   * riêng, và đó là lựa chọn có chủ đích (2026-09-08).
   *
   * doc/contracts/users.md chốt đúng một hành vi cho ca tranh chấp ghi: *"lệch ⇒ 409, và handler
   * KHÔNG ghi gì"*. Nó KHÔNG mô tả màn hình phải làm gì thêm. Nên FE giữ nguyên đường đã có —
   * `applyError` hiện `message` của BE trong form, `httpErrorInterceptor` hiện cùng câu ấy trên
   * toast — y hệt 400/422. Tự thêm "đóng form", "tải lại rồi mở lại", hay "trộn dữ liệu mới vào ô
   * đang gõ" đều là phát minh hành vi ngoài hợp đồng, và cái cuối còn xoá mất thứ người dùng vừa gõ.
   *
   * ⚠️ Hệ quả đã biết, KHÔNG che giấu: form giữ nguyên `Version` cũ sau 409, nên bấm Lưu lại sẽ
   * 409 tiếp cho tới khi người dùng đóng form và mở lại. Câu của BE nói đúng lối ra đó ("Hãy tải
   * lại danh sách rồi thực hiện lại"). Rút ngắn vòng này là một quyết định UX cần chốt riêng.
   */
  private submitUpdate(id: string, payload: IUpdateUserPayload, onSaved: () => void): void {
    this.saving.set(true);
    this.service.update(id, payload).subscribe({
      next: (succeeded) => {
        this.saving.set(false);
        if (!succeeded) {
          // Envelope không báo thành công → KHÔNG đóng form, KHÔNG báo "đã cập nhật".
          this.serverError.set(this.translate.instant('quan-tri-nguoi-dung.error.updateNotSaved') as string);
          return;
        }
        this.open.set(false);
        onSaved();
        this.toast.success(this.translate.instant('quan-tri-nguoi-dung.toast.updated') as string);
      },
      error: (err: IHttpErrorWithApiResult) => {
        this.saving.set(false);
        this.applyError(err, 'quan-tri-nguoi-dung.error.updateFailed');
      },
    });
  }
}
