// @ts-check
const eslint = require("@eslint/js");
const { defineConfig } = require("eslint/config");
const tseslint = require("typescript-eslint");
const angular = require("angular-eslint");
const importPlugin = require("eslint-plugin-import");

// Gate G8 (doc/huong_dan/wiki-core/fe/trien-khai/05-gate.md) — 1 module nghiệp vụ trong
// `modules/<A>/` không được import trực tiếp nội bộ `modules/<B>/` (module nghiệp vụ khác); vẫn
// được import từ `core/`, `shared/`, `platform/`. Xem doc/kien-truc-core-module.md.
//
// Thêm module nghiệp vụ mới → chỉ cần thêm tên vào mảng này, KHÔNG cần viết tay 1 zone mới.
const BUSINESS_MODULES = ["dashboard", "danh-muc-dti"];

const moduleBoundaryZones = BUSINESS_MODULES.map((moduleName) => ({
  target: `./src/app/modules/${moduleName}`,
  from: "./src/app/modules",
  except: [`./${moduleName}`],
  message:
    `modules/${moduleName}/ không được import trực tiếp nội bộ 1 module nghiệp vụ khác trong ` +
    `modules/ — chỉ được import từ core/, shared/, platform/. Cần dùng chung logic thì đưa lên ` +
    `shared/ (nếu là dumb UI/service tái dùng) hoặc core/ (nếu là hạ tầng toàn app). Xem ` +
    `doc/kien-truc-core-module.md.`,
}));

module.exports = defineConfig([
  {
    files: ["**/*.ts"],
    extends: [
      eslint.configs.recommended,
      tseslint.configs.recommended,
      tseslint.configs.stylistic,
      angular.configs.tsRecommended,
    ],
    processor: angular.processInlineTemplates,
    rules: {
      "@angular-eslint/directive-selector": [
        "error",
        {
          type: "attribute",
          prefix: "app",
          style: "camelCase",
        },
      ],
      "@angular-eslint/component-selector": [
        "error",
        {
          type: "element",
          prefix: "app",
          style: "kebab-case",
        },
      ],
    },
  },
  {
    // Gate G8 — chỉ áp cho module NGHIỆP VỤ (src/app/modules/**), không áp cho platform/ (màn
    // Core không bị ràng buộc quy tắc này, xem doc/kien-truc-core-module.md).
    files: ["src/app/modules/**/*.ts"],
    plugins: { import: importPlugin },
    // `no-restricted-paths` cần resolver phân giải được import specifier tương đối (`.ts`
    // không có phần mở rộng) ra đường dẫn file thật — resolver "node" mặc định chỉ thử
    // `.js`/`.json`, phải khai thêm `.ts` mới nhận diện đúng import nội bộ TypeScript.
    settings: {
      "import/resolver": {
        node: { extensions: [".ts", ".js"] },
      },
    },
    rules: {
      "import/no-restricted-paths": ["error", { zones: moduleBoundaryZones }],
    },
  },
  {
    files: ["**/*.html"],
    extends: [
      angular.configs.templateRecommended,
      angular.configs.templateAccessibility,
    ],
    rules: {},
  }
]);
