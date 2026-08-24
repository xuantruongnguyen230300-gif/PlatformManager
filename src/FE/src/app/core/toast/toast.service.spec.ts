import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { ToastService } from './toast.service';

const AUTO_DISMISS_MS = 5000;

/**
 * Trọng tâm: hàng đợi toast là signal chỉ-đọc ra ngoài, và mỗi toast tự biến mất sau đúng
 * `AUTO_DISMISS_MS`. Hai thứ dễ vỡ nhất: (1) `Id` phải duy nhất — trùng Id thì `dismiss()` xoá
 * nhầm toast của người khác, (2) timer auto-dismiss phải gắn với Id của CHÍNH nó, không phải
 * "xoá phần tử cuối".
 *
 * Dùng `jasmine.clock()` (chi phối được `setTimeout` thuần mà service này dùng) thay cho
 * `fakeAsync()` — dự án đã bỏ zone.js (zoneless), xem fe/13-performance.md §1.
 */
describe('ToastService', () => {
  let service: ToastService;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideZonelessChangeDetection()] });
    service = TestBed.inject(ToastService);
    jasmine.clock().install();
  });

  afterEach(() => jasmine.clock().uninstall());

  it('bắt đầu rỗng', () => {
    expect(service.toasts()).toEqual([]);
  });

  it('4 mức độ đẩy đúng severity tương ứng', () => {
    service.success('xong');
    service.error('hỏng');
    service.info('biết vậy');
    service.warn('cẩn thận');

    expect(service.toasts().map((t) => t.Severity)).toEqual(['success', 'error', 'info', 'warn']);
    expect(service.toasts().map((t) => t.Text)).toEqual(['xong', 'hỏng', 'biết vậy', 'cẩn thận']);
  });

  it('giữ thứ tự đẩy vào (mới nhất ở cuối) và cấp Id duy nhất tăng dần', () => {
    service.info('a');
    service.info('b');
    service.info('c');

    const ids = service.toasts().map((t) => t.Id);
    expect(new Set(ids).size).toBe(3);
    expect(ids).toEqual([...ids].sort((x, y) => x - y));
  });

  it('dismiss(id) chỉ xoá đúng toast đó', () => {
    service.info('a');
    service.info('b');
    service.info('c');
    const [, middle] = service.toasts();

    service.dismiss(middle.Id);

    expect(service.toasts().map((t) => t.Text)).toEqual(['a', 'c']);
  });

  it('dismiss(id) không tồn tại → không ném lỗi, không đụng hàng đợi', () => {
    service.info('a');
    const before = service.toasts();

    expect(() => service.dismiss(-99)).not.toThrow();
    expect(service.toasts()).toEqual(before);
  });

  it('tự biến mất sau AUTO_DISMISS_MS, không sớm hơn', () => {
    service.info('a');

    jasmine.clock().tick(AUTO_DISMISS_MS - 1);
    expect(service.toasts().length).toBe(1);

    jasmine.clock().tick(1);
    expect(service.toasts()).toEqual([]);
  });

  it('mỗi toast có timer riêng — toast đẩy sau không bị toast trước kéo đi cùng', () => {
    service.info('sớm');
    jasmine.clock().tick(3000);
    service.info('muộn');

    jasmine.clock().tick(2000); // 'sớm' đủ 5000ms, 'muộn' mới 2000ms
    expect(service.toasts().map((t) => t.Text)).toEqual(['muộn']);

    jasmine.clock().tick(3000);
    expect(service.toasts()).toEqual([]);
  });

  it('dismiss thủ công trước hạn → timer đến hạn không xoá nhầm toast khác', () => {
    service.info('a');
    const a = service.toasts()[0];
    service.dismiss(a.Id);

    jasmine.clock().tick(100);
    service.info('b'); // đẩy vào lúc t=100 → hạn riêng của nó là t=5100

    jasmine.clock().tick(AUTO_DISMISS_MS - 100); // t=5000: đúng lúc timer của 'a' nổ

    // 'b' vẫn còn: timer của 'a' lọc theo Id của 'a', không phải theo vị trí trong mảng.
    expect(service.toasts().map((t) => t.Text)).toEqual(['b']);
  });
});
