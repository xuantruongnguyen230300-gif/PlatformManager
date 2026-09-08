import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { DataGrid, IDataGridPageChange } from '../../../../shared/components/data-grid/data-grid';
import { IUser } from '../../models/quan-tri-nguoi-dung.model';


function initials(fullName: string): string {
  const parts = fullName.trim().split(/\s+/);
  const last = parts.at(-1)?.[0] ?? '';
  const secondLast = parts.length > 1 ? parts.at(-2)?.[0] ?? '' : '';
  return `${secondLast}${last}`.toUpperCase();
}

/**
 * Grid người dùng — `p-table` server-side pagination (`[lazy]`), khớp bố cục màn "Người dùng"
 * trong `doc/Design/Frontend/PlatformManager/Prototypes/index.html` (avatar chữ cái đầu, role
 * tag, badge trạng thái, cột Hành động ghim phải). Dumb — không tự gọi service, chỉ phát
 * output().
 */
@Component({
  selector: 'app-user-grid-table',
  standalone: true,
  imports: [DataGrid, DatePipe, TranslatePipe],
  templateUrl: './user-grid-table.html',
  styleUrl: './user-grid-table.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserGridTable {
  readonly rows = input.required<IUser[]>();
  readonly loading = input<boolean>(false);
  readonly totalCount = input<number>(0);
  readonly page = input<number>(1);
  readonly pageSize = input<number>(10);
  /** Id người đang đăng nhập — `null` khi chưa biết (không chặn dòng nào trong lúc đó). Dùng để
   * chặn UI trước cho `USER.SELF_LOCK_FORBIDDEN` (doc/contracts/users.md §"Bảo vệ tài khoản quản
   * trị" luật #4) — áp cho MỌI role, chỉ chặn "Khoá", không chặn "Mở khoá". */
  readonly currentUserId = input<string | null>(null);

  readonly editRow = output<IUser>();
  readonly toggleLock = output<IUser>();
  readonly pageChange = output<{ Page: number; PageSize: number }>();

  /**
   * `localeId` của ngôn ngữ đang chọn, truyền làm THAM SỐ CUỐI của `DatePipe`.
   *
   * Trước 2026-09-05 chỗ này là `toLocaleDateString('vi-VN')` khai cứng, nên ngày **luôn** hiện
   * theo định dạng Việt kể cả khi người dùng đã chuyển sang English — lỗi không bao giờ tự lộ ra,
   * vì cả hai định dạng đều là "một ngày trông hợp lý".
   *
   * **Là `input()` chứ KHÔNG `inject(LanguageService)`**: component này nằm trong
   * `components/`, tức dumb — LUẬT G4 cấm inject service dữ liệu ở đây. Bản đầu tôi inject thẳng
   * và nó làm **37 test đỏ**: `LanguageService` cần token `CORE_I18N`, mà spec của các màn nghiệp
   * vụ không cấp token đó nên `inject()` ném NG0201 và kéo đổ cả loạt.
   *
   * 🛑 Điều KHÔNG được đọc ra từ đoạn trên: *"cổng G4 đã bắt được lỗi này"*. Lúc đó cổng CHƯA
   * tồn tại — thứ làm 37 test đỏ là **bộ test**, không phải cổng.
   *
   * ✅ Cổng G4 nay đã có (bật 2026-09-08, section `G4` trong `scripts/fe-gate.sh`, xem
   * doc/huong_dan/wiki-core/fe/trien-khai/05-gate.md §G4). Nên lần vi phạm sau **sẽ** bị bắt kể
   * cả ở chỗ không có test tương ứng — đó chính là khoảng trống mà đoạn trên từng cảnh báo.
   *
   * Mặc định `'vi'` để spec nào không quan tâm định dạng ngày thì không phải khai gì.
   */
  readonly localeId = input<string>('vi');
  protected readonly initials = initials;

  /**
   * Chỉ đổi tên trường, KHÔNG tính lại trang: phép quy đổi `first` (0-based của PrimeNG) sang
   * trang 1-based nay nằm trong `DataGrid` — một chỗ duy nhất cho một phép tính lệch-một-đơn-vị,
   * thay vì mỗi màn hình tự nhớ.
   */
  protected onGridPageChange(event: IDataGridPageChange): void {
    this.pageChange.emit({ Page: event.page, PageSize: event.pageSize });
  }

  /** Chỉ đúng khi đang bấm "Khoá" (chưa khoá) trên chính dòng của người đang đăng nhập. */
  isSelfLock(row: IUser): boolean {
    return !row.IsLocked && this.currentUserId() !== null && row.Id === this.currentUserId();
  }

  /**
   * KHOÁ DỊCH cho `title` của nút khoá/mở khoá — template dịch bằng `| translate`.
   *
   * Trả khoá chứ không trả câu vì component này nằm trong `components/`, tức DUMB: LUẬT G4 cấm
   * inject service dữ liệu ở đây (doc/huong_dan/wiki-core/fe/trien-khai/05-gate.md — cổng tự
   * động chưa có, xem ghi chú ở `localeId`), nên nó không có
   * `TranslateService` để tự dịch. Dịch ở template thì `TranslatePipe` lo cả việc vẽ lại khi đổi
   * ngôn ngữ — xem thêm ghi chú cùng chủ đề ở `localeId` phía trên.
   */
  lockButtonTitleKey(row: IUser): string {
    if (this.isSelfLock(row)) return 'quan-tri-nguoi-dung.grid.selfLockTitle';
    return row.IsLocked ? 'quan-tri-nguoi-dung.grid.unlockTitle' : 'quan-tri-nguoi-dung.grid.lockTitle';
  }
}
