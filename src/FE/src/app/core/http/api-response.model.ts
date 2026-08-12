/**
 * Envelope chuẩn của mọi response API `src/BE` — xác nhận trong
 * `doc/contracts/danh-muc-dti.md` (IMPLEMENTED, backend-expert). Dùng chung
 * cho mọi feature, đặt ở `core/` vì là quy ước tầng HTTP cross-cutting, không
 * riêng của 1 feature.
 */
export interface ApiResponse<T> {
  Success: boolean;
  Data: T;
  ErrorCode: string | null;
  ErrorMessage: string | null;
  TraceId: string | null;
}

/**
 * Kết quả phân trang chuẩn — bọc trong `ApiResponse<PagedResult<T>>`.
 */
export interface PagedResult<T> {
  Items: T[];
  Page: number;
  PageSize: number;
  TotalCount: number;
  TotalPages: number;
}
