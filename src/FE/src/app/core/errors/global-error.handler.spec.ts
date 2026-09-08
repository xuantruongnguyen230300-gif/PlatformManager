import { TestBed } from '@angular/core/testing';
import { ErrorHandler, provideZonelessChangeDetection } from '@angular/core';
import { GlobalErrorHandler } from './global-error.handler';
import { appConfig } from '../../app.config';

describe('GlobalErrorHandler', () => {
  let handler: GlobalErrorHandler;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideZonelessChangeDetection()] });
    handler = TestBed.inject(GlobalErrorHandler);
    // Vẫn log như `ErrorHandler` mặc định — chỉ chặn để output test không bị rác.
    spyOn(console, 'error');
  });

  it('mặc định KHÔNG có phiên bản mới nào', () => {
    expect(handler.newVersionAvailable()).toBeFalse();
  });

  it('lỗi tải chunk → bật cờ mời tải lại, và vẫn log lỗi gốc', () => {
    const error = new TypeError('Failed to fetch dynamically imported module: /chunk-1.js');
    handler.handleError(error);

    expect(handler.newVersionAvailable()).toBeTrue();
    expect(console.error).toHaveBeenCalledWith(error);
  });

  it('🛑 lỗi JavaScript khác KHÔNG bật cờ — không hiện "có phiên bản mới" cho bug thường', () => {
    handler.handleError(new TypeError("Cannot read properties of undefined (reading 'Id')"));

    expect(handler.newVersionAvailable()).toBeFalse();
    expect(console.error).toHaveBeenCalled();
  });

  it('🛑 KHÔNG tự tải lại — người dùng phải tự bấm (dữ liệu đang nhập là của họ)', () => {
    const reload = spyOn(handler, 'reloadApp');
    handler.handleError(new TypeError('Failed to fetch dynamically imported module: /chunk-1.js'));

    expect(reload).not.toHaveBeenCalled();
  });
});

/**
 * `useExisting` chứ không `useClass`: `App` inject THẲNG `GlobalErrorHandler` để đọc signal, nên
 * Angular và `App` phải nhìn vào CÙNG một thể hiện. Viết nhầm thành `useClass` không làm hỏng
 * build và cũng không làm đỏ test nào khác — chỉ khiến dải thông báo không bao giờ hiện ra.
 */
describe('appConfig — ErrorHandler toàn cục dùng chung thể hiện với App', () => {
  it('ErrorHandler của app CHÍNH LÀ thể hiện GlobalErrorHandler mà App đọc signal', () => {
    TestBed.configureTestingModule({ providers: [provideZonelessChangeDetection(), ...appConfig.providers] });

    expect(TestBed.inject(ErrorHandler)).toBe(TestBed.inject(GlobalErrorHandler));
  });
});
