import { isPlatformBrowser } from '@angular/common';
import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { PLATFORM_ID, inject } from '@angular/core';
import { Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { Observable, Subject, catchError, mergeMap, race, retry, throwError, timer } from 'rxjs';
import { CurrentUserService } from '../auth/current-user.service';
import { CORE_ROUTES } from '../config/core-routes';
import { ApiErrorMessageService } from '../i18n/api-error-message.service';
import { IApiResult } from '../http/api-result.model';
import { SKIP_ERROR_TOAST } from '../http/http-context-tokens';
import { TOAST_AUTO_DISMISS_MS, ToastService } from '../toast/toast.service';

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
 * Khoá dịch của toast "không kết nối được" — nhóm khoá CORE (`public/i18n/`), vì hạ tầng HTTP đi
 * theo CoreBase sang sản phẩm thứ hai chứ không ở lại với dự án này.
 *
 * ⚠️ **`status === 0` KHÔNG đồng nghĩa với "người dùng mất mạng".** Trình duyệt trả `0` cho MỌI
 * ca request không bao giờ nhận được một response hợp lệ: mất mạng thật, DNS trượt, CORS chặn
 * preflight, chứng chỉ TLS hỏng, server đóng kết nối giữa chừng, request bị huỷ (`AbortError`,
 * đổi route lúc đang tải). FE không phân biệt được — trình duyệt cố tình không nói, vì nói ra là
 * rò rỉ thông tin xuyên nguồn. Nên câu chữ nói theo hướng **"không kết nối được tới máy chủ"** —
 * điều duy nhất quan sát được — thay vì quả quyết "bạn đã mất mạng", một câu sai hẳn khi nguyên
 * nhân thật là CORS hoặc máy chủ sập.
 */
const OFFLINE_TITLE_KEY = 'shared.httpError.offlineTitle';
const OFFLINE_TEXT_KEY = 'shared.httpError.offline';

/**
 * Câu MẠNH hơn, chỉ dùng khi `navigator.onLine === false`.
 *
 * ⚠️ Giới hạn của `navigator.onLine`, phải biết trước khi tin nó: nó chỉ trả lời *"máy có đang
 * gắn vào MỘT mạng nào đó không"* — card mạng có link, Wi-Fi đã kết nối. Nó **không** kiểm tra
 * có ra được Internet hay không. Hệ quả hai chiều:
 *
 *  · `true` mà request vẫn hỏng là chuyện thường: captive portal ở khách sạn, VPN rớt, DNS hỏng,
 *    hoặc chính máy chủ sập. Vì vậy `true` KHÔNG được dùng để suy ra "mạng ổn, chắc lỗi khác";
 *    nó chỉ khiến ta rơi về câu trung tính `OFFLINE_TEXT_KEY`.
 *  · `false` thì đáng tin theo ĐÚNG MỘT CHIỀU: hệ điều hành báo không có mạng nào cả, nên nói
 *    thẳng "thiết bị đang không có kết nối" là đúng — và đó là lúc duy nhất câu mạnh được dùng.
 *
 * Tức là chỉ khai thác chiều `false`, chiều mà giá trị này thực sự nói lên điều gì đó.
 */
const OFFLINE_DEVICE_TEXT_KEY = 'shared.httpError.offlineDevice';

/** Nhãn nút hành động trên toast mất kết nối. */
const RETRY_ACTION_KEY = 'shared.action.retry';

interface IOfflineGateDeps {
  toast: ToastService;
  translate: TranslateService;
  isBrowser: boolean;
}

/**
 * Cổng THỬ LẠI cho ca `status === 0` — đặt ở `retry({ delay })`, tức TRƯỚC `catchError`.
 *
 * ## Vì sao phải là `retry` chứ không phải một callback "gọi lại request" đặt trong `catchError`
 *
 * Yêu cầu là *"nút gọi lại đúng request vừa hỏng"*, và chữ nặng nhất là **đúng request**: kết quả
 * của lần gọi lại phải chảy về **đúng nơi đã đặt hàng** (component đang chờ danh sách, form đang
 * chờ kết quả lưu). Nếu bấm nút mà interceptor tự `next(req).subscribe()` thêm một lần nữa thì
 * request có bay đi thật, nhưng response rơi vào hư không — lưới vẫn trống, form vẫn treo, và
 * người dùng thấy nút "Thử lại" như không làm gì cả. `retry` thì **resubscribe chính nguồn**, nên
 * người đặt hàng ban đầu nhận kết quả như thể lần gọi đầu đã thành công.
 *
 * ## Cái giá phải trả, nói thẳng: lỗi bị HOÃN
 *
 * Muốn người dùng bấm được nút thì observable phải còn sống lúc họ bấm ⇒ lỗi `status 0` không
 * phát ra ngay nữa mà chờ hết **cửa sổ thử lại**. Trong khoảng đó spinner của màn hình vẫn quay.
 * Đánh đổi có chủ đích: một thao tác "đang chờ thử lại" vài giây dễ chịu hơn hẳn một thông báo
 * lỗi mà người dùng không làm gì được với nó.
 *
 * Cửa sổ ấy dài bằng ĐÚNG tuổi thọ của toast (`TOAST_AUTO_DISMISS_MS`), và đó là bất biến chứ
 * không phải trùng hợp: nút rời khỏi màn hình lúc nào thì cơ hội thử lại tắt lúc đó. Lấy hằng số
 * từ `toast.service.ts` thay vì chép một con số vào đây chính là để hai thứ không thể lệch nhau.
 *
 * 🛑 Notifier trả về **không bao giờ được complete rỗng**: `retry` hiểu "notifier complete" là
 * *thôi không thử nữa* và cho observable kết quả complete **không giá trị, không lỗi** — nơi gọi
 * sẽ không chạy nhánh `error`, còn `firstValueFrom` thì ném `EmptyError` rất khó truy. Vì vậy
 * nhánh hết giờ là `throwError(lỗi gốc)`, không phải `EMPTY`.
 *
 * ## Nhiều request hỏng cùng lúc → nhiều toast, CÓ CHỦ ĐÍCH
 *
 * Khác hẳn cách xử lý 401 bên dưới (một cờ chặn, vì ở đó ba request chỉ dẫn tới MỘT việc: điều
 * hướng về màn đăng nhập — làm ba lần là thừa ba lần). Ở đây mỗi toast **sở hữu một request
 * riêng**: gộp còn một nghĩa là những request kia mất luôn cơ hội thử lại và hỏng im lặng. Đổi
 * vài dòng toast chồng nhau lấy dữ liệu không bao giờ về là một cuộc đổi tồi.
 *
 * ## Không tự động thử lại
 *
 * Cổng chỉ mở khi CON NGƯỜI bấm. Điều này quan trọng với request không idempotent: `status 0`
 * không nói được server đã nhận request hay chưa (response mất trên đường về cũng ra `0`), nên
 * một `POST` thử lại có thể tạo hai bản ghi. Để người dùng quyết giữ đúng mức rủi ro của việc họ
 * tự bấm "Lưu" lần nữa — không hơn.
 */
function offlineRetryGate(err: unknown, deps: IOfflineGateDeps): Observable<unknown> {
  // Mọi thứ KHÔNG phải lỗi mạng đi thẳng xuống `catchError` như trước, không đổi hành vi gì. Kiểm
  // `instanceof` chứ không chỉ đọc `.status`: interceptor khác có thể ném lỗi thường xuống đây.
  if (!(err instanceof HttpErrorResponse) || err.status !== 0) {
    return throwError(() => err);
  }

  const deviceOffline = deps.isBrowser && navigator.onLine === false;
  const retryClicked = new Subject<void>();

  deps.toast.error(
    deps.translate.instant(deviceOffline ? OFFLINE_DEVICE_TEXT_KEY : OFFLINE_TEXT_KEY) as string,
    deps.translate.instant(OFFLINE_TITLE_KEY) as string,
    { Label: deps.translate.instant(RETRY_ACTION_KEY) as string, Run: () => retryClicked.next() },
  );

  // Ai emit trước thì thắng: bấm nút → `retry` gọi lại nguồn; hết giờ → ném lại CHÍNH lỗi ban đầu
  // để `catchError` phía dưới và nơi gọi nhìn thấy đúng thứ đã xảy ra.
  return race(retryClicked, timer(TOAST_AUTO_DISMISS_MS).pipe(mergeMap(() => throwError(() => err))));
}

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
  // `navigator` chỉ tồn tại trên trình duyệt. Dự án hôm nay không chạy SSR, nhưng quy ước SSR-safe
  // của repo (doc/huong_dan/quy-uoc/fe-ui-conventions.md) áp cho mọi truy cập API trình duyệt ở
  // `core/` — và `inject()` thì bắt buộc phải gọi ở ĐÂY, như `router` phía trên.
  const isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

  return next(req).pipe(
    // Nhánh MẤT KẾT NỐI, tách hẳn khỏi nhánh lỗi HTTP thường bên dưới. Đặt trước `catchError` vì
    // nó phải nhìn thấy lỗi TRƯỚC — `catchError` nuốt lỗi thì không còn gì để thử lại nữa.
    //
    // Request probe (`SKIP_ERROR_TOAST`, vd `GET /auth/me` lúc khởi động) đi thẳng: nó hỏng là
    // chuyện bình thường, và giữ nó sống thêm vài giây để chờ một cú bấm sẽ treo luôn màn hình
    // khởi động — đúng lúc mạng hỏng là lúc app cần trả lời "chưa đăng nhập" nhanh nhất.
    retry({
      delay: (err: unknown) =>
        isProbeRequest ? throwError(() => err) : offlineRetryGate(err, { toast, translate, isBrowser }),
    }),
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
      } else if (!isProbeRequest && err.status !== 0) {
        // `err.status !== 0`: ca mất kết nối đã được `offlineRetryGate` phía trên lo trọn (toast
        // riêng + nút thử lại), và tới được đây nghĩa là cửa sổ thử lại đã đóng. Không loại trừ
        // thì người dùng nhận HAI toast cho cùng một sự cố, cái sau còn xoá mất cái mang nút bấm.
        //
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
