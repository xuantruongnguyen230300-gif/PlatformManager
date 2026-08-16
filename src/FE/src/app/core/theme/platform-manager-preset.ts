import { definePreset } from '@primeng/themes';
import Aura from '@primeng/themes/aura';

// Preset PrimeNG map thẳng vào token đã có trong src/styles.scss (--brand/--card/--text/--line/
// --good/--warn/--bad...) — KHÔNG dùng theme Aura mặc định (màu sẽ lệch hoàn toàn khỏi
// doc/Prototype/dashboard.html đã duyệt). Xem
// doc/huong_dan/wiki-core/fe/04-design-token-system.md §Theme PrimeNG khớp token hiện có.
//
// Aura yêu cầu 1 "ramp" 50→950 cho mỗi màu ngữ nghĩa (primary/surface/...) để tự suy ra hover/
// active/disabled của từng component PrimeNG. Token của app chỉ có 1 giá trị/màu (vd --brand),
// nên ramp bên dưới được PHÁI SINH bằng công thức pha trắng/đen từ đúng giá trị token gốc
// (cùng nguyên tắc "tint phái sinh có công thức rõ ràng" đã dùng ở sidebar — xem
// spec/sidebar-menu/ui-spec.md mục 5) — không phải màu tự chọn ngoài palette.

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

/** Pha `hex` với `target` theo tỉ trọng `weight` (0 = giữ nguyên hex, 1 = ra hẳn target). */
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

// Token gốc — khớp nguyên xi src/styles.scss :root (đừng đổi giá trị ở đây, đổi ở styles.scss
// trước rồi mirror lại theo doc/huong_dan/wiki-core/fe/04-design-token-system.md §2 nguồn phải
// luôn khớp nhau).
const BRAND = '#0f5bd7';
const GOOD = '#0e7050';
const WARN = '#a8690a';
const BAD = '#b83232';
const BG = '#eef2f8';
const CARD = '#ffffff';
const TEXT = '#152033';
const MUTED = '#57647a';
const LINE = '#dfe6ef';
const BORDER_STRONG = '#7e91b4';

export const PlatformManagerPreset = definePreset(Aura, {
  semantic: {
    primary: ramp(BRAND),
    colorScheme: {
      light: {
        surface: {
          0: CARD,
          50: BG,
          100: mix(BG, '#000000', 0.03),
          200: mix(BG, '#000000', 0.06),
          300: LINE,
          400: mix(LINE, '#000000', 0.12),
          500: BORDER_STRONG,
          600: mix(BORDER_STRONG, '#000000', 0.15),
          700: MUTED,
          800: mix(MUTED, '#000000', 0.3),
          900: TEXT,
          950: mix(TEXT, '#000000', 0.2),
        },
        primary: {
          color: BRAND,
          contrastColor: '#ffffff',
          hoverColor: mix(BRAND, '#000000', 0.12),
          activeColor: mix(BRAND, '#000000', 0.24),
        },
        text: {
          color: TEXT,
          hoverColor: TEXT,
          mutedColor: MUTED,
          hoverMutedColor: TEXT,
        },
        content: {
          background: CARD,
          hoverBackground: BG,
          borderColor: LINE,
          color: TEXT,
          hoverColor: TEXT,
        },
        formField: {
          background: CARD,
          disabledBackground: BG,
          filledBackground: BG,
          borderColor: BORDER_STRONG,
          hoverBorderColor: BRAND,
          focusBorderColor: BRAND,
          color: TEXT,
          placeholderColor: MUTED,
        },
        overlay: {
          select: { background: CARD, borderColor: LINE },
          popover: { background: CARD, borderColor: LINE },
          modal: { background: CARD, borderColor: LINE },
        },
        list: {
          option: {
            focusBackground: BG,
            selectedBackground: mix(BRAND, '#ffffff', 0.92),
            selectedFocusBackground: mix(BRAND, '#ffffff', 0.86),
            color: TEXT,
            selectedColor: BRAND,
          },
        },
        navigation: {
          item: {
            focusBackground: BG,
            activeBackground: mix(BRAND, '#ffffff', 0.92),
            color: TEXT,
            activeColor: BRAND,
          },
        },
      },
    },
  },
  // Ramp riêng cho message/tag semantics (success/warn/danger) — dùng `--good`/`--warn`/`--bad` đã
  // có, không phát minh màu mới. Chưa dùng nhiều ở F1 (Badge tự viết tay, xem
  // doc/huong_dan/wiki-core/fe/05-component-library.md) nhưng khai sẵn để p-message/p-tag (nếu
  // dùng ở F2+) không rơi về màu Aura mặc định.
  primitive: {
    green: ramp(GOOD),
    amber: ramp(WARN),
    red: ramp(BAD),
  },
});
