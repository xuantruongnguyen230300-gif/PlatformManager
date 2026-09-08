import { Routes } from '@angular/router';

// Route PUBLIC — không có authGuard (đây chính là nơi user CHƯA đăng nhập đến). `noShell: true`
// để `App` ẩn sidebar/topbar, xem app.ts.
//
// `title` ở CẤP `Route` (API chuẩn Angular, xem doc/huong_dan/quy-uoc/fe-routing-guard.md §2 quy
// tắc 3) — `PageTitleStrategy` đọc nó để set cả `<title>` của tab lẫn tiêu đề topbar. `noShell`
// vẫn ở trong `data` vì đó là cờ RIÊNG của app này, router không hiểu nó.
//
// 🛑 Giá trị là KHOÁ DỊCH (`<màn>.routeTitle`), KHÔNG phải câu — `PageTitleStrategy` tra nó qua
// bảng dịch và tra LẠI mỗi lần đổi ngôn ngữ. Viết thẳng câu tiếng Việt vào đây thì tiêu đề tab và
// tiêu đề topbar là hai chỗ DUY NHẤT trên màn hình không đổi khi bấm sang tiếng Anh, và không có
// gì báo: cả hai đều là chữ đọc được, chỉ sai ngôn ngữ.
export const LOGIN_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./pages/login/login.page').then((m) => m.LoginPage),
    title: 'login.routeTitle',
    data: { noShell: true },
  },
];
