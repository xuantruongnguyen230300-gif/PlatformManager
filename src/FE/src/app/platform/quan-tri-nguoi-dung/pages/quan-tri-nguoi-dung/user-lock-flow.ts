import { Injectable, computed, inject, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { QuanTriNguoiDungService } from '../../services/quan-tri-nguoi-dung.service';
import { ToastService } from '../../../../core/toast/toast.service';
import { LanguageService } from '../../../../core/i18n/language.service';
import { IUser } from '../../models/quan-tri-nguoi-dung.model';

/**
 * LUỒNG KHOÁ/MỞ KHOÁ của màn Quản trị người dùng: giữ người đang chờ xác nhận, dựng câu hỏi lại,
 * gọi API và báo kết quả.
 *
 * Tách khỏi `QuanTriNguoiDungPage` (2026-09-10) vì nó là một luồng ĐỘC LẬP với hộp thoại tạo/sửa
 * và với bộ lọc — hai bên không chia sẻ một mẩu trạng thái nào. Trang chỉ giữ lại đúng phần không
 * tách được: `viewChild` trỏ tới `<app-confirm-dialog>` (một tham chiếu DOM thì phải thuộc về
 * component).
 *
 * 🛑 `@Injectable()` KHÔNG `providedIn: 'root'` — khai trong `providers` của trang, nên vòng đời
 * trùng đúng vòng đời trang, y hệt các field `signal()` mà nó thay thế.
 */
@Injectable()
export class UserLockFlow {
  private readonly service = inject(QuanTriNguoiDungService);
  private readonly toast = inject(ToastService);
  private readonly translate = inject(TranslateService);
  private readonly language = inject(LanguageService);

  /**
   * Người dùng đang chờ xác nhận KHOÁ. `null` = không có câu hỏi nào đang mở. Giữ ở đây (không đọc
   * lại từ grid) vì dữ liệu có thể được nạp lại trong lúc hộp thoại đang mở.
   */
  private readonly pending = signal<IUser | null>(null);

  /**
   * Câu mô tả trong hộp thoại xác nhận khoá. Nó nói ĐÚNG độ trễ đã ghi ở doc/contracts/users.md
   * §"Khoá KHÔNG có hiệu lực tức thì": hệ thống dùng cookie session, phiên đang chạy còn sống tối
   * đa ~30 phút. Hứa "đã đăng xuất ngay" là để quản trị viên tin đã chặn xong trong khi người bị
   * khoá vẫn đang thao tác.
   *
   * 🛑 HAI KHOÁ TRỌN CÂU, không phải một câu ghép với một mảnh chủ ngữ thay được. Ghép mảnh buộc
   * mọi bản dịch phải nhận đúng trật tự từ của tiếng Việt; hai câu đủ thì mỗi ngôn ngữ tự đặt chủ
   * ngữ vào chỗ của nó. Cùng khuôn với `trang-chu.greeting` / `trang-chu.greetingNamed` đã có sẵn
   * trong bảng dịch.
   */
  readonly confirmDescription = computed(() => {
    this.language.current();
    const user = this.pending();
    return user
      ? (this.translate.instant('quan-tri-nguoi-dung.dialog.lockDescriptionNamed', {
          name: user.FullName,
        }) as string)
      : (this.translate.instant('quan-tri-nguoi-dung.dialog.lockDescription') as string);
  });

  /** Ghi nhận người sắp bị khoá; trang mở hộp thoại hỏi lại ngay sau lời gọi này. */
  arm(user: IUser): void {
    this.pending.set(user);
  }

  /** Người dùng bấm Huỷ — bỏ câu hỏi, không gọi gì. */
  disarm(): void {
    this.pending.set(null);
  }

  /**
   * Toast thành công CHỈ khi envelope báo thành công — không suy từ "gọi xong không lỗi mạng"
   * (doc/contracts/users.md §"Thao tác hỏng nay là LỖI", finding BE-4). Với BE hiện tại, thao tác
   * hỏng là **422** nên rơi vào nhánh `error` và `httpErrorInterceptor` hiện thẳng `message` của
   * `USER.LOCK_FAILED` — câu đó cố ý nhắc kiểm lại trạng thái tài khoản, vì `lock` đổi con dấu bảo
   * mật TRƯỚC khi đặt lockout nên hỏng giữa chừng để lại trạng thái nửa vời (phiên bị chấm dứt
   * nhưng vẫn đăng nhập lại được).
   */
  confirm(onDone: () => void): void {
    const user = this.pending();
    this.pending.set(null);
    if (!user) return;
    this.service.lock(user.Id).subscribe({
      next: (succeeded) => {
        onDone();
        if (succeeded) {
          this.toast.success(this.translate.instant('quan-tri-nguoi-dung.toast.locked') as string);
        } else {
          this.toast.error(this.translate.instant('quan-tri-nguoi-dung.toast.lockFailed') as string);
        }
      },
      error: () => {
        // 422/403/404: httpErrorInterceptor đã hiện `message` của envelope.
      },
    });
  }

  /** MỞ KHOÁ không đi qua `arm`/`confirm` — xem `QuanTriNguoiDungPage.onToggleLock`. */
  unlock(user: IUser, onDone: () => void): void {
    this.service.unlock(user.Id).subscribe({
      next: (succeeded) => {
        onDone();
        if (succeeded) {
          this.toast.success(this.translate.instant('quan-tri-nguoi-dung.toast.unlocked') as string);
        } else {
          this.toast.error(this.translate.instant('quan-tri-nguoi-dung.toast.unlockFailed') as string);
        }
      },
      error: () => {
        // 422/404: httpErrorInterceptor đã hiện `message` của envelope.
      },
    });
  }
}
