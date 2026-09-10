import { ChangeDetectionStrategy, Component } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

/**
 * SMART — route `/danh-muc/dti`. Gate `authGuard` + `mustChangePasswordGuard`.
 *
 * 🚧 **KHUNG, CHƯA PHẢI MÀN THẬT (dựng 2026-09-09).** Trang này hôm nay chỉ có hàng tiêu đề và một
 * câu nói rõ nó chưa xong. Màn đầy đủ — toolbar, lưới 14 cột ghim hai mép, bốn dialog, import chạy
 * nền — đặc tả ở `spec/danh-muc-dti/ui-spec.md` và
 * `doc/Design/Frontend/PlatformManager/Screens/02-danh-muc-dti.md`, thi công ở lượt sau.
 *
 * Vì sao dựng khung trước thay vì đợi làm một lượt: bốn thứ của hạ tầng chỉ kiểm được khi đã có
 * một module nghiệp vụ THẬT trên đĩa — cổng G8 (ranh giới `modules/<A>` ↔ `modules/<B>`, hằng
 * `BUSINESS_MODULES` ở `src/FE/eslint.config.js`), phạm vi quét của G11 trong `scripts/fe-gate.sh`,
 * nhóm khoá dịch thứ hai (`public/i18n-app/`), và đường `loadChildren` trong `app.routes.ts`. Để
 * cả bốn tới cùng lượt với màn thật là gộp hai loại rủi ro vào một lần kiểm.
 *
 * Chuỗi hiển thị đi qua i18n ngay từ khung — nhóm khoá DỰ ÁN (`public/i18n-app/`), không phải nhóm
 * Core. Ranh giới đó tách bằng FILE, xem doc/huong_dan/wiki-core/fe/08-i18n.md §Khuôn CoreBase ý 2.
 */
@Component({
  selector: 'app-danh-muc-dti-page',
  standalone: true,
  imports: [TranslatePipe],
  templateUrl: './danh-muc-dti.page.html',
  styleUrl: './danh-muc-dti.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DanhMucDtiPage {}
