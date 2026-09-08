import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { CurrentUserService } from '../../../../core/auth/current-user.service';
import { CORE_BRANDING } from '../../../../core/config/core-branding';
import { CORE_ROUTES } from '../../../../core/config/core-routes';

/**
 * SMART — route `/trang-chu`. Gate `authGuard` + `mustChangePasswordGuard`.
 *
 * Trang đích mặc định của app. Hiện chỉ chào người dùng, tóm tắt tài khoản và trỏ về menu: module
 * nghiệp vụ duy nhất (DTI tuần) đã được gỡ ngày 2026-08-29 để xây lại, nên chưa có màn nghiệp vụ
 * nào để dẫn tới. Khi module đầu tiên trở lại, trang này là chỗ đặt lối vào của nó — hoặc thay hẳn
 * bằng dashboard nếu nghiệp vụ cần.
 *
 * Giao diện dựng bằng ĐÚNG các class dùng chung của src/styles.scss (`.notice` · `.card` ·
 * `.title` · `.btn` · `.badge`); prototype đã duyệt chưa có màn này nên không có bản đối chiếu
 * pixel — mọi thứ ở đây phải là thứ đã tồn tại trong thư viện component, không phát minh thêm.
 */
@Component({
  selector: 'app-trang-chu-page',
  standalone: true,
  imports: [RouterLink, TranslatePipe],
  templateUrl: './trang-chu.page.html',
  styleUrl: './trang-chu.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TrangChuPage {
  private readonly currentUserService = inject(CurrentUserService);

  /**
   * Tên sản phẩm trong đoạn dẫn — lấy từ `CORE_BRANDING`, không viết thẳng vào template.
   * `platform/` là các màn hình Core, tức NẰM TRONG CoreBase (doc/kien-truc-core-module.md),
   * nên một chuỗi khai cứng ở đây đi theo nền tảng sang sản phẩm thứ hai và biến trang chủ của
   * nó thành lời quảng cáo cho sản phẩm trước.
   *
   * Vì sao nó thoát lượt tách 2026-09-02: chuỗi này nằm giữa một câu văn xuôi, không phải ở
   * thuộc tính hay hằng số — phép kiểm hôm đó lại chỉ quét `core/` và `shared/`.
   */
  protected readonly branding = inject(CORE_BRANDING);

  /**
   * Đường dẫn cho `routerLink` "Đổi mật khẩu" — cùng lý do với `branding` ngay trên, và cùng
   * seam mà `mustChangePasswordGuard` dùng.
   *
   * Vì sao chỗ này thoát cả lượt tách 2026-09-02 LẪN lượt rà 2026-09-03: nó nằm trong
   * **template**, không phải trong `.ts`. Danh sách bàn giao của lượt 09-03 chỉ liệt ba dòng
   * `.ts`, còn phép thử 3 ở `doc/kien-truc-core-module.md` §Nghiệm thu quét cả `--include=*.html`
   * — và chính nó bắt được dòng này. Đây là lần thứ ba cùng một lỗi nấp ở một loại file mà phép
   * kiểm lần trước chưa quét tới.
   */
  protected readonly routes = inject(CORE_ROUTES);

  /** Dùng lại `fullName` của service — không tự tính lại tên hiển thị ở từng màn. */
  protected readonly fullName = this.currentUserService.fullName;

  /**
   * `authGuard` đã chặn khách vãng lai nên trên thực tế signal này luôn có giá trị; template vẫn
   * bọc `@if` vì kiểu là `ICurrentUser | null` — ép `!` ở đây sẽ là lời hứa mà guard không thể
   * bảo đảm lúc biên dịch.
   */
  protected readonly user = this.currentUserService.currentUser;
}
