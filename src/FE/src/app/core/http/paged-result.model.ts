// Wire shape của kết quả phân trang — BE trả `IApiResult<PagedResult<T>>` (PagedResult LỒNG
// bên trong `data`, không phải envelope riêng, xem src/BE/.claude/rules/api-controller.md
// §Envelope response "Quyết định có chủ đích: endpoint list/grid trả cùng envelope"). Camelcase
// theo đúng quy ước envelope mới — xem api-result.model.ts.
export interface IPagedResultDto<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

// Model app tương ứng — PascalCase + prefix I theo quy ước wire boundary
// (src/FE/.claude/docs/api-client.md). Dùng chung cho mọi grid server-side pagination.
export interface IPagedResult<T> {
  Items: T[];
  Page: number;
  PageSize: number;
  TotalCount: number;
  TotalPages: number;
}

export function mapPagedResultDto<TDto, TModel>(
  dto: IPagedResultDto<TDto>,
  mapItem: (item: TDto) => TModel,
): IPagedResult<TModel> {
  return {
    Items: dto.items.map(mapItem),
    Page: dto.page,
    PageSize: dto.pageSize,
    TotalCount: dto.totalCount,
    TotalPages: dto.totalPages,
  };
}
