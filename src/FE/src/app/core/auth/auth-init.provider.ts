import { EnvironmentProviders, inject, provideAppInitializer } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { CurrentUserService } from './current-user.service';

/**
 * Tải phiên đăng nhập hiện tại (nếu có) đúng 1 LẦN lúc app khởi động — không load lại mỗi lần
 * đổi route (guard chỉ đọc `CurrentUserService.isAuthenticated()` đã có sẵn trong bộ nhớ). Wire
 * vào `app.config.ts`. `CurrentUserService.load()` đã tự nuốt lỗi 401 (trả `null`, không throw)
 * nên `firstValueFrom` ở đây không bao giờ reject vì lý do "chưa đăng nhập".
 */
export function provideAuthInit(): EnvironmentProviders {
  return provideAppInitializer(() => {
    const currentUserService = inject(CurrentUserService);
    return firstValueFrom(currentUserService.load());
  });
}
