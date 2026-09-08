import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { catchError, throwError } from 'rxjs';
import { CurrentUserService } from '../auth/current-user.service';
import { CORE_ROUTES } from '../config/core-routes';
import { ApiErrorMessageService } from '../i18n/api-error-message.service';
import { IApiResult } from '../http/api-result.model';
import { SKIP_ERROR_TOAST } from '../http/http-context-tokens';
import { ToastService } from '../toast/toast.service';

/**
 * Khoá dịch của câu thông báo khi phiên bị chấm dứt từ phía server. Cố ý KHÔNG dùng `toast.error`
 * (đỏ, tiêu đề "Lỗi hệ thống"/"Chưa đăng nhập"): doc/contracts/auth.md §"Vòng đời phiên" nói rõ
 * 401 giữa chừng là chuyện BÌNH THƯỜNG của vòng đời phiên (tài khoản bị khoá, bị đổi role, đổi mật
 * khẩu ở nơi khác, `SecurityStampValidator` chạy sau tối đa 30 phút) — "không coi đó là lỗi hệ
 * thống". Người dùng đang bị đưa êm về màn đăng nhập, nên một toast đỏ chỉ làm họ tưởng app hỏng;
 * `warn` + một câu giải thích vì sao vừa bị đá ra là đủ, phần còn lại để màn đăng nhập tự nói.
 *
 * Là KHOÁ chứ không còn là câu (đổi 2026-09-05): chuỗi viết trong `core/` không thuộc màn nào nên
 * đoạn giữa của khoá lấy từ TÊN DỊCH VỤ chứa nó (`http-error.interceptor.ts`), không phải từ chủ
 * đề của câu — doc/huong_dan/wiki-core/fe/08-i18n.md §Khuôn khoá dịch §6, dòng cuối bảng.
 */
const SESSION_ENDED_TITLE_KEY = 'shared.httpError.endedTitle';
const SESSION_ENDED_TEXT_KEY = 'shared.httpError.endedText';

/**
 * CẠM BẪY 3 — nhiều request 401 cùng lúc. Một trang thường bắn vài request song song; khi phiên
 * chết thì CẢ BA cùng nhận 401 trong cùng một tick. Không có cờ này thì user nhận 3 toast chồng
 * nhau và 3 lần `router.navigate()` liên tiếp (lần sau ghi đè `returnUrl` của lần trước bằng
 * `<màn đăng nhập>?returnUrl=...` đã đổi). Cờ ở tầng module vì interceptor là HÀM, không có
 * instance để giữ state; nó tự mở lại khi điều hướng kết thúc (thành công hay thất bại) nên không
 * kẹt vĩnh viễn — đăng nhập lại rồi hết phiên lần nữa vẫn được xử lý.
 */
let redirectingToLogin = false;

/**
 * Bỏ query/fragment để so sánh đường dẫn — `router.url` mang thêm `?returnUrl=...` khi đã ở màn
 * đăng nhập. `signInPath` truyền từ ngoài vào (`CORE_ROUTES.signIn`) chứ không khai cứng: đường
 * dẫn là thứ của app, xem core/config/core-routes.ts.
 */
function isOnSignInPage(router: Router, signInPath: string): boolean {
  return router.url.split(/[?#]/)[0] === signInPath;
}

/**
 * Hai bảng câu dự phòng theo mã HTTP (`fallbackMessageForStatus` / `fallbackTitleForStatus`) ĐÃ
 * CHUYỂN sang `core/i18n/api-error-message.service.ts` ngày 2026-09-05 — cùng lượt bọc i18n.
 *
 * Vì sao chuyển chứ không dịch tại chỗ: màn đăng nhập hiển thị lỗi ở HAI kênh (toast của
 * interceptor + khối `.login-error` inline,
 * doc/Design/Frontend/PlatformManager/Screens/05-auth.md §States) và trước
 * đợt này mỗi kênh có một bộ câu dự phòng riêng — toast thì "Đã có lỗi xảy ra. Vui lòng thử lại."
 * còn form thì "Đăng nhập thất bại — thử lại sau.". Hai bộ câu cho cùng một sự kiện là hai bộ sẽ
 * lệch nhau; gom về một service giữ chúng nói cùng một điều.
 */

/**
 * 1 chỗ duy nhất dịch lỗi HTTP sang toast — đọc đúng `message` (KHÔNG phải `Message`/
 * `ErrorMessage` của envelope cũ đã bỏ, xem doc/huong_dan/wiki-core/fe/02-http-envelope.md
 * §Vấn đề gốc). Giữ nguyên `body` gắn vào `apiResult` trên error rethrow để nơi gọi (thường là
 * component form) tự đọc `fields`/`businessCode` — interceptor chỉ lo phần chung (toast).
 *
 * Cũng là 1 chỗ duy nhất xử lý **phiên chết giữa chừng** (401): xoá state phía client rồi điều
 * hướng êm về màn đăng nhập kèm `returnUrl=<đang ở đâu>` — cùng hình dạng `authGuard` dùng, để màn
 * đăng nhập trả user về đúng chỗ. Không đặt việc này ở guard được: guard chỉ chạy lúc ĐỔI route,
 * còn 401 xuất hiện giữa lúc user đang gõ form. Xem doc/contracts/auth.md §"Vòng đời phiên".
 */
export const httpErrorInterceptor: HttpInterceptorFn = (req, next) => {
  const toast = inject(ToastService);
  // `inject()` phải gọi Ở ĐÂY (thân interceptor còn trong injection context), KHÔNG gọi bên trong
  // `catchError` — callback đó chạy sau, ngoài context, sẽ ném NG0203.
  const router = inject(Router);
  const currentUser = inject(CurrentUserService);
  // Đường dẫn màn đăng nhập do app cung cấp (core/config/core-routes.ts) — `core/` không biết
  // đường dẫn riêng của sản phẩm nào. Cùng lý do phải `inject()` ở đây như `router` ở trên.
  const coreRoutes = inject(CORE_ROUTES);
  // Nơi DUY NHẤT của FE biến envelope lỗi thành câu người dùng đọc được — tra `businessCode` ra
  // khoá dịch, ráp `messageParams`, lùi về `message` của BE rồi mới tới câu dự phòng theo mã HTTP.
  // Màn đăng nhập gọi CÙNG service này cho khối lỗi inline, nên toast và khối inline không thể
  // nói hai câu khác nhau về cùng một lỗi. Cùng lý do phải `inject()` ở đây như `router` ở trên.
  const errorMessages = inject(ApiErrorMessageService);
  // `TranslateService` cũng phải lấy ở đây, KHÔNG lấy trong `catchError`.
  const translate = inject(TranslateService);
  // Đọc cờ probe ngay từ request, dùng cho CẢ hai quyết định bên dưới (toast + điều hướng).
  const isProbeRequest = req.context.get(SKIP_ERROR_TOAST);

  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      const body = (err.error ?? null) as IApiResult<unknown> | null;

      // ─── 401: phiên đã chết ở phía server ───────────────────────────────────────────────
      // Hai ca 401 KHÔNG được điều hướng, vì điều hướng ở đó làm hỏng đúng thứ nó định cứu:
      //  · CẠM BẪY 1 — request probe (`SKIP_ERROR_TOAST`, vd `GET /auth/me` lúc khởi động):
      //    401 nghĩa là "khách vãng lai chưa đăng nhập", chuyện thường; `CurrentUserService.load()`
      //    tự `catchError → null`. Điều hướng ở đây sẽ đá user ra khỏi trang công khai họ vừa mở.
      //  · CẠM BẪY 2 — đang Ở màn đăng nhập: `POST /auth/login` trả 401 nghĩa là SAI MẬT KHẨU, chứ
      //    không phải hết phiên. Điều hướng lại về chính màn đăng nhập vừa vô nghĩa vừa ghi đè
      //    `returnUrl` người dùng đang giữ, làm mất chỗ họ định quay về. Để form login hiện lỗi.
      const isSessionExpired =
        err.status === 401 && !isProbeRequest && !isOnSignInPage(router, coreRoutes.signIn);

      if (isSessionExpired) {
        // CẠM BẪY 3: chỉ request 401 ĐẦU TIÊN được xử lý; những request song song còn lại im lặng
        // (không toast, không điều hướng) — vẫn rethrow bình thường ở cuối để nơi gọi biết hỏng.
        if (!redirectingToLogin) {
          redirectingToLogin = true;
          currentUser.clear();
          toast.warn(
            translate.instant(SESSION_ENDED_TEXT_KEY) as string,
            translate.instant(SESSION_ENDED_TITLE_KEY) as string,
          );
          // Chụp `router.url` TRƯỚC khi navigate — sau đó nó đã là màn đăng nhập.
          const returnUrl = router.url;
          const releaseFlag = (): void => {
            redirectingToLogin = false;
          };
          // `.then(ok, ko)` chứ không `.finally()`: `navigate()` reject (guard ném lỗi) qua
          // `finally` sẽ thành unhandled rejection, còn cờ thì phải mở lại ở CẢ hai nhánh.
          router.navigate([coreRoutes.signIn], { queryParams: { returnUrl } }).then(releaseFlag, releaseFlag);
        }
      } else if (!isProbeRequest) {
        // Request "probe" tự đánh dấu bỏ qua toast — lỗi ở đó là tình huống bình thường, không
        // phải lỗi cần làm phiền user. Vẫn rethrow để nơi gọi tự xử lý.
        // `titleFor` trả `undefined` khi envelope đã có câu thật — giữ nguyên hành vi cũ
        // "toast không tiêu đề cho lỗi nghiệp vụ", chỉ khác là câu nay đến từ bảng dịch.
        //
        // Gọi tách làm hai nhánh chứ không truyền thẳng `undefined`: `toast.error(text)` và
        // `toast.error(text, undefined)` chạy giống nhau nhưng KHÔNG giống nhau dưới con mắt của
        // một spy — và hai test đang khoá hành vi này (`http-error.interceptor.spec.ts`) so khớp
        // đúng danh sách đối số. Giữ nguyên hình dạng lời gọi để chúng vẫn đo được "hành vi KHÔNG
        // đổi sau đợt bọc i18n", thay vì phải sửa assert cho khớp code mới.
        const text = errorMessages.messageFor(body, err.status);
        const title = errorMessages.titleFor(body, err.status);
        if (title) {
          toast.error(text, title);
        } else {
          toast.error(text);
        }
      }

      // Luôn rethrow — nơi gọi có thể cần biết request đã hỏng (huỷ spinner, giữ dữ liệu form...).
      return throwError(() => Object.assign(err, { apiResult: body }));
    }),
  );
};
