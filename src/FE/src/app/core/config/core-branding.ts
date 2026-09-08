import { EnvironmentProviders, InjectionToken, makeEnvironmentProviders } from '@angular/core';

/**
 * Tên sản phẩm — hai dạng, dùng ở hai chỗ khác nhau, cùng là DỮ LIỆU của dự án chứ không phải
 * của nền tảng.
 *
 * Vì sao phải tách khỏi `core/`/`shared/`: cả hai tầng này là CoreBase dùng lại cho sản phẩm
 * khác (`doc/kien-truc-core-module.md` §"Core giữ CƠ CHẾ, dự án cung cấp DỮ LIỆU"). Trước
 * 2026-09-02 tên sản phẩm khai cứng ở `core/title/page-title.strategy.ts` và ở template của
 * `shared/components/{sidebar,auth-card}/`, nên dựng sản phẩm thứ hai là phải mở ba file của
 * nền tảng ra sửa — thứ mà định nghĩa "CoreBase xong" loại trừ.
 *
 * Cùng khuôn với `CORE_ROUTES` (file bên cạnh): `core/` giữ CƠ CHẾ (ghép hậu tố cho `<title>`,
 * đổ chữ vào ô thương hiệu ở sidebar và ở card đăng nhập), app cấp CHUỖI.
 */
export interface ICoreBranding {
  /**
   * Tên đầy đủ. Hai nơi tiêu thụ: hậu tố `<title>` của tab (và giá trị dự phòng khi route không
   * khai `title`) ở `core/title/page-title.strategy.ts`, và dòng chữ thương hiệu ở sidebar.
   *
   * 🛑 Phải khớp `<title>` tĩnh trong `src/index.html` — chuỗi đó là thứ hiện ra trong lúc app
   * chưa bootstrap xong, lệch nhau thì tên tab nhấp nháy đổi khi trang tải xong.
   */
  readonly name: string;

  /**
   * Chữ tắt trong ô vuông thương hiệu `.brand-mark` — hai nơi dùng: đầu sidebar và card đăng
   * nhập/đổi mật khẩu (`shared/components/auth-card/`). Tồn tại tách khỏi `name` vì ở trạng thái
   * sidebar thu gọn (và trên màn hình hẹp) `.brand-text` bị `display: none` — ô vuông này là thứ
   * DUY NHẤT còn lại để nhận ra đang ở sản phẩm nào.
   *
   * Ô vuông rộng 26px (`sidebar.scss`): giữ 2 ký tự. Dài hơn sẽ tràn, không có gì báo.
   */
  readonly shortName: string;
}

/**
 * CỐ Ý không có `factory` mặc định, cùng lý do với `CORE_ROUTES`: một tên mặc định sẽ biến "app
 * quên khai" thành một giao diện mang tên sản phẩm KHÁC — sai ở chỗ dễ thấy nhất (tiêu đề tab,
 * góc trên bên trái) nhưng không lỗi nào, không test nào bắt. Thiếu provider thì Angular ném
 * `NG0201` ngay lần dựng đầu tiên: ồn ào, đúng chỗ, sửa một dòng là xong.
 */
export const CORE_BRANDING = new InjectionToken<ICoreBranding>('CORE_BRANDING');

/** Wire ở `app.config.ts` — nơi duy nhất biết tên sản phẩm này. */
export function provideCoreBranding(branding: ICoreBranding): EnvironmentProviders {
  return makeEnvironmentProviders([{ provide: CORE_BRANDING, useValue: branding }]);
}
