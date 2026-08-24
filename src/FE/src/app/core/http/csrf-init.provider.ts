import { EnvironmentProviders, inject, provideAppInitializer } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { CsrfService } from './csrf.service';

/**
 * Mồi cookie CSRF (`XSRF-TOKEN`) đúng 1 LẦN lúc app khởi động — chạy song song với
 * `provideAuthInit()` (`core/auth/auth-init.provider.ts`, cả hai đều là `provideAppInitializer`
 * nên Angular tự đợi song song, không cần sắp thứ tự tay). Cookie phải tồn tại TRƯỚC request ghi
 * đầu tiên của app — kể cả `POST /api/auth/login` — nên phải mồi lúc khởi động, không đợi tới lúc
 * user bấm nút ghi đầu tiên. Xem doc/contracts/auth.md §"CSRF — GET /api/antiforgery/token".
 * `CsrfService.primeToken()` đã tự nuốt lỗi (không throw) nên `firstValueFrom` ở đây không bao
 * giờ reject.
 */
export function provideCsrfInit(): EnvironmentProviders {
  return provideAppInitializer(() => {
    const csrfService = inject(CsrfService);
    return firstValueFrom(csrfService.primeToken());
  });
}
