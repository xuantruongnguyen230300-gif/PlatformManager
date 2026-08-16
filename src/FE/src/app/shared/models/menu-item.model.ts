// Hợp đồng menu động — BE trả danh sách PHẲNG (mỗi item có `parentId`), FE tự dựng cây 1 cấp
// (item cha có `children`, không có `route`; item không `children` là điểm đến thật). Xem
// doc/ke-hoach-xay-lai-corebase.md §"Phân quyền màn hình" (bảng SysMenu) và
// spec/sidebar-menu/ui-spec.md §1 (IA cây menu, chỉ hỗ trợ đúng 1 cấp lồng).
//
// BE đã lọc `SysMenuRole` theo role hiện tại trước khi trả về — FE KHÔNG tự lọc lại theo quyền
// (xem doc/ke-hoach-xay-lai-corebase.md §"Điểm bàn giao"). Casing camelCase theo đúng quy ước
// envelope mới (core/http/api-result.model.ts).
export interface IMenuItemDto {
  id: string;
  parentId: string | null;
  code: string;
  label: string;
  icon: string | null;
  route: string | null;
  displayOrder: number;
}

// Model app — PascalCase + prefix I (src/FE/.claude/docs/api-client.md). `Children` là field
// dựng thêm ở FE (không có trên wire) — mảng rỗng cho item lá.
export interface IMenuItem {
  Id: string;
  ParentId: string | null;
  Code: string;
  Label: string;
  Icon: string | null;
  Route: string | null;
  DisplayOrder: number;
  Children: IMenuItem[];
}
