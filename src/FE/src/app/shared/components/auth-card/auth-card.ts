import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/**
 * Khung "card căn giữa viewport" dùng chung cho màn đăng nhập + đổi mật khẩu (2 feature —
 * `platform/login`, `platform/doi-mat-khau` — đủ điều kiện đặt ở `shared/`, xem
 * src/FE/.claude/docs/architecture.md). Port token/layout 1:1 từ `doc/Prototype/login.html`
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
  readonly title = input.required<string>();
  readonly subtitle = input<string>('');
}
