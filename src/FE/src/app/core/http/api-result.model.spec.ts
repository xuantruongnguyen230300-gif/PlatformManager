import { IApiResult, unwrapData } from './api-result.model';

function envelope<T>(data: T | null | undefined): IApiResult<T> {
  // Cast có chủ đích: mô phỏng key `data` VẮNG MẶT trên dây (`undefined`) dù type khai `T | null`
  // — đúng trạng thái thật `unwrapData` phải bắt được, không phải lỗi kiểu cần sửa.
  return {
    data,
    message: null,
    status: 'SUCCESS',
    code: 'Success',
    businessCode: null,
    traceId: 'trace-unwrap',
    retryable: null,
    fields: null,
  } as IApiResult<T>;
}

/**
 * `unwrapData` thay cho `res.data as T` / `res.data!` ở mọi service gọi HTTP (core/auth,
 * shared/services, platform/*, modules/*) — những cách đó **tắt hẳn** kiểm tra kiểu, nên `data`
 * vắng mặt sẽ đi thẳng vào mapper rồi nổ ở tận trong ruột với thông điệp vô nghĩa ("cannot read
 * property of undefined"), không lần ra được endpoint nào sai.
 *
 * Ca falsy bên dưới mới là ca dễ làm hỏng nhất khi ai đó "đơn giản hoá" hàm này thành `if
 * (!res.data) throw`: `data: false` (BE trả cho logout/lock/unlock) và `data: 0`/`''` đều là giá
 * trị HỢP LỆ. Viết `!res.data` là biến mọi lượt logout thành lỗi hệ thống.
 */
describe('unwrapData', () => {
  it('trả nguyên `data` khi có', () => {
    expect(unwrapData(envelope({ id: 'x' }))).toEqual({ id: 'x' });
  });

  it('KHÔNG chặn giá trị falsy hợp lệ — `false`, `0`, chuỗi rỗng', () => {
    expect(unwrapData(envelope(false)))
      .withContext('BE trả `data: true/false` cho logout, lock, unlock — `!res.data` là sai')
      .toBeFalse();
    expect(unwrapData(envelope(0))).toBe(0);
    expect(unwrapData(envelope(''))).toBe('');
  });

  it('ném lỗi khi `data` vắng mặt (key không có trên dây)', () => {
    expect(() => unwrapData(envelope(undefined))).toThrowError(/thiếu `data`/);
  });

  it('ném lỗi khi `data` là null', () => {
    expect(() => unwrapData(envelope(null))).toThrowError(/thiếu `data`/);
  });

  it('thông điệp lỗi kèm traceId để tra thẳng log BE', () => {
    // Không có traceId trong thông điệp thì lỗi này vô dụng khi hỗ trợ người dùng: biết "có lỗi"
    // nhưng không tra được request nào phía server.
    expect(() => unwrapData(envelope(null))).toThrowError(/trace-unwrap/);
  });
});
