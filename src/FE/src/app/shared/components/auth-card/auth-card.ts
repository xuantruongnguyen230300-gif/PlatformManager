import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { CORE_BRANDING } from '../../../core/config/core-branding';

/**
 * Khung "card căn giữa viewport" dùng chung cho màn đăng nhập + đổi mật khẩu (2 feature —
 * `platform/login`, `platform/doi-mat-khau` — đủ điều kiện đặt ở `shared/`, xem
 * doc/huong_dan/quy-uoc/fe-architecture.md §"Tầng app"). Layout/token khớp
 * doc/Design/Frontend/PlatformManager/Components/AuthCard.md
 * (`.login-shell`/`.login-card`/`.login-brand`) — không phát minh giá trị mới.
 */
@Component({
  selector: 'app-auth-card',
  standalone: true,
  templateUrl: './auth-card.html',
  styleUrl: './auth-card.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AuthCard {
  /**
   * Chữ tắt thương hiệu trong ô vuông ở đầu card — đến từ `CORE_BRANDING`, không viết cứng.
   * Cùng lý do với `Sidebar`: `shared/` là phần dùng lại cho sản phẩm khác, nên nó giữ CHỖ ĐẶT
   * còn app cấp CHUỖI (`doc/kien-truc-core-module.md` §"Core giữ CƠ CHẾ, dự án cung cấp DỮ LIỆU").
   *
   * Chỗ này KHÔNG nằm trong danh sách ban đầu của đợt tách 2026-09-02 — nó chỉ lộ ra khi quét
   * chữ tắt thương hiệu trong template của `shared/`. Ghi lại vì đó là bài học lặp lần thứ hai:
   * dữ liệu dự án nấp trong `.html` dễ sót hơn hẳn trong `.ts`, quét mỗi `.ts` là chưa quét.
   */
  protected readonly branding = inject(CORE_BRANDING);

  readonly title = input.required<string>();
  readonly subtitle = input<string>('');
}
