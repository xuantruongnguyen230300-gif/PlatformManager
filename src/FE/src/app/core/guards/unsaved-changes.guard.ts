import { CanDeactivateFn } from '@angular/router';
import { Observable } from 'rxjs';

/**
 * Hợp đồng của MỌI màn hình có thể mất dữ liệu chưa lưu — xem
 * doc/huong_dan/wiki-core/fe/09-forms-validation.md §"Form dirty + điều hướng đi".
 *
 * Guard chỉ hỏi "có được rời đi không"; CÁCH HỎI (hộp thoại nào, câu chữ gì) do chính component
 * quyết định — nó đã có sẵn `confirm-dialog` trong template của mình, không cần dựng thêm một
 * service overlay toàn cục chỉ để đặt một câu hỏi.
 */
export interface IHasUnsavedChanges {
  /** `true` = đi được ngay. Trả `Observable` khi cần hỏi người dùng trước. */
  canDeactivate(): Observable<boolean> | boolean;
}

/**
 * Lớp 1 trong 2 (lớp 2 là `@HostListener('window:beforeunload')` đặt tại chính component) —
 * chặn điều hướng TRONG Angular Router: bấm sidebar, nút Back của SPA, gõ route khác trong app.
 * `beforeunload` không chạm tới những đường này, còn guard này không chạm tới đóng tab/F5; thiếu
 * lớp nào thì mất dữ liệu ở đúng nhóm thao tác lớp đó phụ trách.
 *
 * ⚠️ Component PHẢI trả `true` khi không có gì để mất. Một guard hỏi cả lúc người dùng không sửa
 * gì sẽ bị bấm qua theo phản xạ, và lúc đó nó ngừng bảo vệ được bất cứ thứ gì — đây là phép thử
 * số 4 trong bảng nghiệm thu của quy ước, quan trọng ngang ba phép thử kia.
 */
export const unsavedChangesGuard: CanDeactivateFn<IHasUnsavedChanges> = (component) =>
  component.canDeactivate();
