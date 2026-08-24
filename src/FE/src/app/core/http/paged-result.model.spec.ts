import { IPagedResultDto, mapPagedResultDto } from './paged-result.model';

interface IRowDto {
  id: string;
  name: string;
}

/**
 * PAYLOAD của `PagedList<T>` phía BE theo shape đã CHỐT
 * (doc/huong_dan/quy-uoc/be-cqrs-handler.md §"Shape phân trang" +
 * src/BE/Core/PlatformManager.Core.Application/Common/Models/PagedList.cs): ĐÚNG 4 field
 * `Items`/`TotalCount`/`Page`/`PageSize`, serialize camelCase, KHÔNG có `totalPages` (suy ra
 * được từ `totalCount`/`pageSize`, gửi kèm là tạo hai nguồn có thể lệch nhau). `JSON.parse` thay
 * vì object literal TS — để test đọc đúng những gì có TRÊN DÂY, không phải những gì type FE tự
 * khai.
 */
const WIRE_JSON = `{
  "items": [{ "id": "c1", "name": "Chỉ tiêu 1" }, { "id": "c2", "name": "Chỉ tiêu 2" }],
  "totalCount": 42,
  "page": 2,
  "pageSize": 20
}`;

/**
 * Chốt chặn cho lỗi P0 (2026-08-23): trước đó tồn tại BA tên cho cùng một khái niệm —
 * `PagedList` (`total`) ở `contracts/users.md` cũ, `IPagedResultDto` (`totalCount` +
 * `totalPages`) ở đây, `PagedResult` (`TotalPages` sentinel `-1`) ở wiki-core — BE gửi `total`,
 * FE đọc `totalCount` → `undefined`. Nay CHỐT một shape duy nhất; test này khoá lại đúng 4 field,
 * không cho tái sinh field đã bỏ (`total`/`totalPages`).
 */
describe('mapPagedResultDto — khớp `PagedList<T>` đã CHỐT của BE', () => {
  function parseWire(): IPagedResultDto<IRowDto> {
    return JSON.parse(WIRE_JSON) as IPagedResultDto<IRowDto>;
  }

  it('đọc `totalCount` và map từng item', () => {
    const result = mapPagedResultDto(parseWire(), (dto) => ({ Id: dto.id, Name: dto.name }));

    expect(result.TotalCount).toBe(42);
    expect(result.Page).toBe(2);
    expect(result.PageSize).toBe(20);
    expect(result.Items.map((i) => i.Id)).toEqual(['c1', 'c2']);
  });

  it('không sinh ra field `undefined` nào — model chỉ đúng 4 field BE thật sự trả', () => {
    const result = mapPagedResultDto(parseWire(), (dto) => dto.id);

    expect(Object.keys(result).sort()).toEqual(['Items', 'Page', 'PageSize', 'TotalCount']);
    expect(Object.values(result).every((v) => v !== undefined)).toBeTrue();
    // Field đã bỏ: khai lại là dựng lại đúng cái bẫy cũ (đọc ra `undefined`, build vẫn xanh).
    const asRecord = result as unknown as Record<string, unknown>;
    expect(asRecord['Total']).toBeUndefined();
    expect(asRecord['TotalPages']).toBeUndefined();
  });
});
