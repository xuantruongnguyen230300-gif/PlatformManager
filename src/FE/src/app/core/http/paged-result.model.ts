// Wire shape của kết quả phân trang — BE trả `IApiResult<PagedResult<T>>` (PagedResult LỒNG
// bên trong `data`, không phải envelope riêng, xem src/BE/.claude/rules/api-controller.md
// §Envelope response "Quyết định có chủ đích: endpoint list/grid trả cùng envelope"). Camelcase
// theo đúng quy ước envelope mới — xem api-result.model.ts.
// Shape chuẩn duy nhất cho MỌI endpoint list — xem
// doc/huong_dan/quy-uoc/be-cqrs-handler.md §"Shape phân trang — CHỐT một bản duy nhất
// (2026-08-23)". KHÔNG có `totalPages`: suy ra được từ `totalCount`/`pageSize`, gửi kèm là tạo
// hai nguồn có thể lệch nhau. PrimeNG paginator chỉ cần `totalRecords` (= totalCount).
export interface IPagedResultDto<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

// Model app tương ứng — PascalCase + prefix I theo quy ước wire boundary
// (doc/huong_dan/quy-uoc/fe-api-client.md). Dùng chung cho mọi grid server-side pagination.
export interface IPagedResult<T> {
  Items: T[];
  Page: number;
  PageSize: number;
  TotalCount: number;
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
  };
}
