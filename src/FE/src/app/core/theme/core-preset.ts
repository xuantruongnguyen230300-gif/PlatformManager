import { definePreset } from '@primeng/themes';
import Aura from '@primeng/themes/aura';

/**
 * Bảng màu thương hiệu — **DỮ LIỆU của từng sản phẩm**, không phải của `core/`.
 *
 * Mỗi field dưới đây soi gương đúng một biến `:root` trong `src/styles.scss` — CSS thuần đọc
 * biến, còn component PrimeNG đọc preset dựng từ đây. Hai nguồn lệch nhau thì giao diện render
 * hai màu khác nhau mà KHÔNG có gì đỏ: không lỗi biên dịch, không test, không lint. Đó là lý do
 * phải khai tường minh thành một hợp đồng có tên, thay vì một nhúm hằng số nằm im trong file cơ
 * chế. Danh sách biến phải khớp đọc thẳng từ JSDoc của từng field bên dưới, đừng chép ra chỗ khác.
 *
 * Vì sao tách khỏi `core/`: `core/` là CoreBase dùng lại cho sản phẩm khác
 * (`doc/kien-truc-core-module.md` §"Core giữ CƠ CHẾ, dự án cung cấp DỮ LIỆU"). Trước 2026-09-02
 * các màu này là hằng số ngay trong file preset, nên dùng lại `core/` cho sản phẩm mang màu
 * khác là phải MỞ `core/` RA SỬA — đúng thứ mà định nghĩa "CoreBase xong" loại trừ.
 *
 * Chỉ nhận chuỗi hex 6 chữ số (`#rrggbb`): `mix()` bên dưới cắt chuỗi theo vị trí ký tự, nên
 * `rgb()`/`hsl()`/tên màu CSS sẽ ra `NaN` một cách im lặng — không ném lỗi, chỉ ra màu sai.
 */
export interface ICorePalette {
  /** `--brand` — màu chính, gốc của ramp `primary` 50→950. */
  readonly brand: string;
  /** `--good` — trạng thái thành công (ramp `green` cho p-message/p-tag). */
  readonly good: string;
  /** `--warn` — trạng thái cảnh báo (ramp `amber`). */
  readonly warn: string;
  /** `--bad` — trạng thái lỗi (ramp `red`). */
  readonly bad: string;
  /** `--bg` — nền trang, bậc `surface.50`. */
  readonly bg: string;
  /** `--card` — nền bề mặt nổi, bậc `surface.0`. */
  readonly card: string;
  /** `--text` — chữ chính, bậc `surface.900`. */
  readonly text: string;
  /** `--muted` — chữ phụ, bậc `surface.700`. */
  readonly muted: string;
  /** `--line` — viền trang trí, bậc `surface.300`. */
  readonly line: string;
  /** `--border-strong` — viền ô nhập (đậm hơn `line` một bậc), bậc `surface.500`. */
  readonly borderStrong: string;
  /**
   * `--on-primary` — mực chữ/icon nằm TRÊN nền `brand` (ô vuông thương hiệu, nút primary,
   * seg-btn đang chọn), ánh xạ sang `primary.contrastColor` của Aura.
   *
   * 🛑 Đây là DỮ LIỆU, không phải đầu mút của phép pha như `#ffffff` trong `mix()` ngay dưới —
   * khác biệt nằm ở chỗ giá trị này bị RÀNG BUỘC bởi `brand` mà sản phẩm chọn: brand tối thì
   * mực trắng, brand sáng (vàng, cyan, lime) thì mực phải TỐI, nếu không chữ trên nút chính
   * biến mất. Để nó là hằng số trong `core/` — tình trạng trước 2026-09-03 — là buộc sản phẩm
   * thứ hai mở nền tảng ra sửa, đúng thứ định nghĩa "CoreBase xong" loại trừ. Nó lọt qua lượt
   * tách hôm trước vì trông y hệt hàng chục chuỗi trắng/đen khác trong file này.
   *
   * Viết đủ 6 chữ số (`#ffffff`, không `#fff`) dù giá trị này không đi qua `mix()`: giữ đúng
   * một luật cho cả `ICorePalette` thì không ai phải nhớ field nào được miễn.
   */
  readonly onPrimary: string;
}

// Preset PrimeNG map thẳng vào token đã có trong src/styles.scss — KHÔNG dùng theme Aura mặc
// định (màu sẽ lệch hoàn toàn khỏi doc/Design/Frontend/PlatformManager/Prototypes/index.html đã
// duyệt). Xem doc/huong_dan/wiki-core/fe/04-design-token-system.md §Theme PrimeNG khớp token
// hiện có.
//
// Aura yêu cầu 1 "ramp" 50→950 cho mỗi màu ngữ nghĩa (primary/surface/...) để tự suy ra hover/
// active/disabled của từng component PrimeNG. Token của app chỉ có 1 giá trị/màu (vd --brand),
// nên ramp bên dưới được PHÁI SINH bằng công thức pha trắng/đen từ đúng giá trị token gốc —
// không phải màu tự chọn ngoài palette.
//
// Sửa 2026-09-08: chỗ này từng viện dẫn một ui-spec sidebar-menu (nay đã xoá) làm tiền lệ cho
// "tint phái sinh có công thức rõ ràng". Ngoài việc nguồn biến mất, phép so sánh nay cũng sai:
// sidebar
// không phái sinh gì cả — nền mục đang chọn đọc thẳng token `--surface-nav-active`
// (src/FE/src/styles.scss § --surface-nav-active; sidebar.scss dùng `var()`), đúng như
// doc/Design/Frontend/PlatformManager/Components/Sidebar.md §States mô tả. Nguyên tắc phái
// sinh ở file NÀY vẫn đúng và tự đứng được, không cần tiền lệ nào chống lưng.

function hexToRgb(hex: string): { r: number; g: number; b: number } {
  const clean = hex.replace('#', '');
  return {
    r: parseInt(clean.substring(0, 2), 16),
    g: parseInt(clean.substring(2, 4), 16),
    b: parseInt(clean.substring(4, 6), 16),
  };
}

function rgbToHex(r: number, g: number, b: number): string {
  const c = (n: number) => Math.round(Math.min(255, Math.max(0, n))).toString(16).padStart(2, '0');
  return `#${c(r)}${c(g)}${c(b)}`;
}

/**
 * Pha `hex` với `target` theo tỉ trọng `weight` (0 = giữ nguyên hex, 1 = ra hẳn target).
 *
 * `#000000`/`#ffffff` truyền vào hàm này là ĐẦU MÚT của phép pha đậm/nhạt — cơ chế, không phải
 * màu thương hiệu. Đừng lôi chúng ra `ICorePalette`: sản phẩm nào cũng pha về đen/trắng.
 *
 * 🛑 Phép thử phân biệt, viết ra vì câu trên từng bị đọc thành "mọi chuỗi trắng/đen trong file
 * này đều là cơ chế" và che mất `primary.contrastColor` suốt một lượt tách: một hex là cơ chế
 * khi và chỉ khi nó là **đối số của `mix()`**. Hex đứng ngoài `mix()` là màu người dùng NHÌN
 * THẤY nguyên vẹn, tức dữ liệu của sản phẩm — nó thuộc `ICorePalette`. Phép thử này chạy được
 * bằng máy, xem lệnh grep ở doc/kien-truc-core-module.md §"Đã thi công — phần FE, bảng màu".
 */
function mix(hex: string, target: string, weight: number): string {
  const a = hexToRgb(hex);
  const b = hexToRgb(target);
  return rgbToHex(
    a.r + (b.r - a.r) * weight,
    a.g + (b.g - a.g) * weight,
    a.b + (b.b - a.b) * weight,
  );
}

/** Ramp 50(nhạt nhất)→950(đậm nhất) quanh `base`, 500 LUÔN đúng bằng `base` (token gốc). */
function ramp(base: string): Record<string, string> {
  return {
    50: mix(base, '#ffffff', 0.94),
    100: mix(base, '#ffffff', 0.86),
    200: mix(base, '#ffffff', 0.7),
    300: mix(base, '#ffffff', 0.52),
    400: mix(base, '#ffffff', 0.28),
    500: base,
    600: mix(base, '#000000', 0.12),
    700: mix(base, '#000000', 0.24),
    800: mix(base, '#000000', 0.36),
    900: mix(base, '#000000', 0.48),
    950: mix(base, '#000000', 0.6),
  };
}

/**
 * Dựng preset PrimeNG từ bảng màu của sản phẩm. `core/` giữ CÁCH dẫn xuất thang màu và cách ánh
 * xạ sang PrimeNG; sản phẩm chỉ đưa vào các màu gốc của `ICorePalette`.
 *
 * 🛑 Vì sao là THAM SỐ HÀM chứ không phải `InjectionToken` (khác hẳn `CORE_ROUTES`/`CORE_BRANDING`
 * ngay bên cạnh): preset được dựng trong `providePrimeNG({ theme: { preset } })` ở
 * `app.config.ts`, tức lúc **dựng đối tượng cấu hình** — trước khi có bất kỳ injector nào tồn
 * tại. `inject()` ở đó ném `NG0203`. Đừng "cho thống nhất" bằng cách đổi seam này sang token.
 *
 * Hệ quả kèm theo: seam này KHÔNG cần mẹo "cố ý không có giá trị mặc định" như hai token kia —
 * quên truyền bảng màu là lỗi BIÊN DỊCH, sớm hơn cả lỗi lúc chạy.
 */
export function createCorePreset(palette: ICorePalette) {
  const { brand, good, warn, bad, bg, card, text, muted, line, borderStrong, onPrimary } = palette;

  return definePreset(Aura, {
    semantic: {
      primary: ramp(brand),
      colorScheme: {
        light: {
          surface: {
            0: card,
            50: bg,
            100: mix(bg, '#000000', 0.03),
            200: mix(bg, '#000000', 0.06),
            300: line,
            400: mix(line, '#000000', 0.12),
            500: borderStrong,
            600: mix(borderStrong, '#000000', 0.15),
            700: muted,
            800: mix(muted, '#000000', 0.3),
            900: text,
            950: mix(text, '#000000', 0.2),
          },
          primary: {
            color: brand,
            contrastColor: onPrimary,
            hoverColor: mix(brand, '#000000', 0.12),
            activeColor: mix(brand, '#000000', 0.24),
          },
          text: {
            color: text,
            hoverColor: text,
            mutedColor: muted,
            hoverMutedColor: text,
          },
          content: {
            background: card,
            hoverBackground: bg,
            borderColor: line,
            color: text,
            hoverColor: text,
          },
          formField: {
            background: card,
            disabledBackground: bg,
            filledBackground: bg,
            borderColor: borderStrong,
            hoverBorderColor: brand,
            focusBorderColor: brand,
            color: text,
            placeholderColor: muted,
          },
          overlay: {
            select: { background: card, borderColor: line },
            popover: { background: card, borderColor: line },
            modal: { background: card, borderColor: line },
          },
          list: {
            option: {
              focusBackground: bg,
              selectedBackground: mix(brand, '#ffffff', 0.92),
              selectedFocusBackground: mix(brand, '#ffffff', 0.86),
              color: text,
              selectedColor: brand,
            },
          },
          navigation: {
            item: {
              focusBackground: bg,
              activeBackground: mix(brand, '#ffffff', 0.92),
              color: text,
              activeColor: brand,
            },
          },
        },
      },
    },
    // Ramp riêng cho message/tag semantics (success/warn/danger) — dùng `--good`/`--warn`/`--bad`
    // đã có, không phát minh màu mới. Chưa dùng nhiều ở F1 (Badge tự viết tay, xem
    // doc/huong_dan/wiki-core/fe/05-component-library.md) nhưng khai sẵn để p-message/p-tag (nếu
    // dùng ở F2+) không rơi về màu Aura mặc định.
    primitive: {
      green: ramp(good),
      amber: ramp(warn),
      red: ramp(bad),
    },
  });
}
