import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { QuanTriNguoiDungService } from '../../services/quan-tri-nguoi-dung.service';
import { ToastService } from '../../../../core/toast/toast.service';
import { CurrentUserService } from '../../../../core/auth/current-user.service';
import { IHttpErrorWithApiResult } from '../../../../core/http/api-result.model';
import { ICreateUserPayload, IUpdateUserPayload, IUser, IUserListParams } from '../../models/quan-tri-nguoi-dung.model';
import { UserGridTable } from '../../components/user-grid-table/user-grid-table';
import { UserFormDialog, IUserFormSaveEvent } from '../../components/user-form-dialog/user-form-dialog';

const DEBOUNCE_MS = 300;
const DEFAULT_PAGE_SIZE = 10;

/**
 * SMART — route `/quan-tri/nguoi-dung`. Gate `authGuard`+`adminGuard`. Điều phối
 * `user-grid-table` + `user-form-dialog`, gọi `QuanTriNguoiDungService`. Bộ lọc search debounce
 * 300ms, phân trang server-side — xem doc/contracts/users.md.
 */
@Component({
  selector: 'app-quan-tri-nguoi-dung-page',
  standalone: true,
  imports: [UserGridTable, UserFormDialog],
  templateUrl: './quan-tri-nguoi-dung.page.html',
  styleUrl: './quan-tri-nguoi-dung.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class QuanTriNguoiDungPage {
  private readonly service = inject(QuanTriNguoiDungService);
  private readonly toast = inject(ToastService);
  private readonly currentUser = inject(CurrentUserService);

  protected readonly currentUserId = computed(() => this.currentUser.currentUser()?.Id ?? null);

  protected readonly searchInput = signal('');
  private readonly searchSubject = new Subject<string>();
  private readonly debouncedSearch = toSignal(
    this.searchSubject.pipe(debounceTime(DEBOUNCE_MS), distinctUntilChanged()),
    { initialValue: '' },
  );

  protected readonly page = signal(1);
  protected readonly pageSize = signal(DEFAULT_PAGE_SIZE);

  protected readonly rows = signal<IUser[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly loading = signal(false);

  protected readonly formOpen = signal(false);
  protected readonly formEditing = signal<IUser | null>(null);
  protected readonly formServerError = signal<string | null>(null);

  private readonly requestParams = computed<IUserListParams>(() => ({
    Page: this.page(),
    PageSize: this.pageSize(),
    SearchText: this.debouncedSearch() || undefined,
  }));

  constructor() {
    // `effect()` gọi API theo đúng filter/phân trang ĐANG CHỌN — side-effect thật (gọi HTTP),
    // không derive state. Chạy ĐÚNG 1 LẦN mỗi khi `requestParams()` (phụ thuộc `debouncedSearch`,
    // `page`, `pageSize`) thực sự đổi giá trị — không phải mỗi phím gõ. Cùng mẫu với
    // danh-muc-dti.page.ts để nhất quán codebase.
    //
    // Regression đã có test chốt (quan-tri-nguoi-dung.page.spec.ts): bản cũ gọi thẳng `loadList()`
    // trong `onSearchInputEvent` mỗi phím gõ — vừa bắn 1 request/phím (sai
    // doc/huong_dan/wiki-core/fe/13-performance.md §6) vừa gửi SAI chuỗi tìm kiếm (giá trị TRƯỚC
    // debounce, vì `requestParams()` đọc `debouncedSearch()` chứ không phải `searchInput()`).
    effect(() => {
      const params = this.requestParams();
      this.loading.set(true);
      this.service.getList(params).subscribe({
        next: (result) => {
          this.rows.set(result.Items);
          this.totalCount.set(result.TotalCount);
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
      });
    });
  }

  /** Nạp lại NGOÀI effect — dùng sau CUD (tạo/sửa/khoá) khi `requestParams()` không đổi giá trị
   * nên effect không tự kích lại, nhưng dữ liệu server đã đổi. */
  private reload(): void {
    this.loading.set(true);
    this.service.getList(this.requestParams()).subscribe({
      next: (result) => {
        this.rows.set(result.Items);
        this.totalCount.set(result.TotalCount);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  onSearchInputEvent(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.searchInput.set(value);
    this.searchSubject.next(value);
    this.page.set(1);
  }

  onGridPageChange(event: { Page: number; PageSize: number }): void {
    this.page.set(event.Page);
    this.pageSize.set(event.PageSize);
  }

  openCreateForm(): void {
    this.formEditing.set(null);
    this.formServerError.set(null);
    this.formOpen.set(true);
  }

  openEditForm(user: IUser): void {
    this.formEditing.set(user);
    this.formServerError.set(null);
    this.formOpen.set(true);
  }

  onFormClosed(): void {
    this.formOpen.set(false);
  }

  onFormSaved(event: IUserFormSaveEvent): void {
    const editing = this.formEditing();
    if (event.IsEditing && editing) {
      this.submitUpdate(editing.Id, event.Update as IUpdateUserPayload);
    } else if (!event.IsEditing) {
      this.submitCreate(event.Create as ICreateUserPayload);
    }
  }

  private submitCreate(payload: ICreateUserPayload): void {
    this.service.create(payload).subscribe({
      next: () => {
        this.formOpen.set(false);
        this.reload();
        this.toast.success('Đã thêm người dùng.');
      },
      error: (err: IHttpErrorWithApiResult) => {
        this.formServerError.set(err.apiResult?.message ?? 'Không tạo được người dùng — thử lại sau.');
      },
    });
  }

  private submitUpdate(id: string, payload: IUpdateUserPayload): void {
    this.service.update(id, payload).subscribe({
      next: () => {
        this.formOpen.set(false);
        this.reload();
        this.toast.success('Đã cập nhật người dùng.');
      },
      error: (err: IHttpErrorWithApiResult) => {
        this.formServerError.set(err.apiResult?.message ?? 'Không cập nhật được người dùng — thử lại sau.');
      },
    });
  }

  onToggleLock(user: IUser): void {
    const request$ = user.IsLocked ? this.service.unlock(user.Id) : this.service.lock(user.Id);
    request$.subscribe({
      next: () => {
        this.reload();
        this.toast.success(user.IsLocked ? 'Đã mở khoá tài khoản.' : 'Đã khoá tài khoản.');
      },
      error: () => {
        // httpErrorInterceptor đã hiện toast.
      },
    });
  }
}
