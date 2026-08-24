import { TestBed } from '@angular/core/testing';
import { PLATFORM_ID, provideZonelessChangeDetection } from '@angular/core';
import { SidebarStateService } from './sidebar-state.service';

const COLLAPSED_STORAGE_KEY = 'platform_manager_sidebar_collapsed_v1';

// `PLATFORM_ID` khai kiểu `InjectionToken<Object>` nhưng giá trị thật LUÔN là chuỗi
// ('browser'/'server' — `isPlatformBrowser()` so chuỗi), nên tham số nhận `string`.
function makeService(platformId = 'browser'): SidebarStateService {
  TestBed.resetTestingModule();
  TestBed.configureTestingModule({
    providers: [provideZonelessChangeDetection(), { provide: PLATFORM_ID, useValue: platformId }],
  });
  return TestBed.inject(SidebarStateService);
}

/**
 * Trọng tâm: state khởi tạo đọc từ `localStorage` NGAY trong field initializer, nên mọi test
 * phải dựng service SAU khi đã set sẵn storage — đó cũng chính là chỗ dễ vỡ nhất của service
 * này (đọc storage lúc construct = không thể sửa sau khi đã inject).
 */
describe('SidebarStateService', () => {
  afterEach(() => localStorage.removeItem(COLLAPSED_STORAGE_KEY));

  it('mặc định mở (không collapse) khi storage trống', () => {
    localStorage.removeItem(COLLAPSED_STORAGE_KEY);
    expect(makeService().collapsed()).toBeFalse();
  });

  it('khôi phục trạng thái collapse đã lưu từ phiên trước', () => {
    localStorage.setItem(COLLAPSED_STORAGE_KEY, '1');
    expect(makeService().collapsed()).toBeTrue();
  });

  it('coi mọi giá trị khác "1" là không collapse (chống dữ liệu rác trong storage)', () => {
    localStorage.setItem(COLLAPSED_STORAGE_KEY, 'true');
    expect(makeService().collapsed()).toBeFalse();
  });

  it('toggleCollapse() lật signal VÀ ghi xuống storage', () => {
    localStorage.removeItem(COLLAPSED_STORAGE_KEY);
    const service = makeService();

    service.toggleCollapse();
    expect(service.collapsed()).toBeTrue();
    expect(localStorage.getItem(COLLAPSED_STORAGE_KEY)).toBe('1');

    service.toggleCollapse();
    expect(service.collapsed()).toBeFalse();
    expect(localStorage.getItem(COLLAPSED_STORAGE_KEY)).toBe('0');
  });

  it('drawer mobile: open/close/toggle độc lập với collapse desktop', () => {
    const service = makeService();
    expect(service.mobileOpen()).toBeFalse();

    service.openDrawer();
    expect(service.mobileOpen()).toBeTrue();
    expect(service.collapsed()).toBeFalse(); // hai trạng thái không dính nhau

    service.closeDrawer();
    expect(service.mobileOpen()).toBeFalse();

    service.toggleDrawer();
    expect(service.mobileOpen()).toBeTrue();
  });

  it('không chạm localStorage khi không phải browser (SSR-safe)', () => {
    localStorage.setItem(COLLAPSED_STORAGE_KEY, '1');
    const service = makeService('server');

    // Có giá trị '1' trong storage nhưng platform là server → phải bỏ qua, trả mặc định.
    expect(service.collapsed()).toBeFalse();

    service.toggleCollapse();
    expect(service.collapsed()).toBeTrue();
    expect(localStorage.getItem(COLLAPSED_STORAGE_KEY)).toBe('1'); // vẫn là giá trị cũ, không bị ghi đè
  });

  it('storage ném lỗi (private mode) → không làm sập app, chỉ mất persist', () => {
    spyOn(Storage.prototype, 'getItem').and.throwError('SecurityError');
    spyOn(Storage.prototype, 'setItem').and.throwError('SecurityError');

    const service = makeService();
    expect(service.collapsed()).toBeFalse();

    expect(() => service.toggleCollapse()).not.toThrow();
    expect(service.collapsed()).toBeTrue();
  });
});
