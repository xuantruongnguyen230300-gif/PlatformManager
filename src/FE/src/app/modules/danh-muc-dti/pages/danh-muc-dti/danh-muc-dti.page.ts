import { ChangeDetectionStrategy, Component, computed, effect, inject, viewChild } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { LanguageService } from '../../../../core/i18n/language.service';
import { ConfirmDialog } from '../../../../shared/components/confirm-dialog/confirm-dialog';
import { Toolbar } from '../../../../shared/components/toolbar/toolbar';
import { ICriteriaRow, IWritePeriod } from '../../models/danh-muc-dti.model';
import { PERIOD_ALL, buildWritePeriod } from '../../services/write-period';
import { CriteriaFormDialog, ICriteriaFormValue } from '../../components/criteria-form-dialog/criteria-form-dialog';
import { CriteriaGridTable, IInlineSaveEvent } from '../../components/criteria-grid-table/criteria-grid-table';
import { ImportDialog } from '../../components/import-dialog/import-dialog';
import { ImportResultDialog } from '../../components/import-result-dialog/import-result-dialog';
import { CriteriaDeleteFlow } from './criteria-delete-flow';
import { CriteriaFilters } from './criteria-filters';
import { CriteriaFormFlow } from './criteria-form-flow';
import { CriteriaImportFlow } from './criteria-import-flow';
import { CriteriaLabels } from './criteria-labels';
import { CriteriaInlineEditFlow } from './criteria-inline-edit-flow';
import { CriteriaListFeed } from './criteria-list-feed';

/**
 * SMART — route `/danh-muc/dti`. Gate `authGuard` + `mustChangePasswordGuard`, **không** guard
 * quyền (Q39): thiếu quyền ghi vẫn vào được, chỉ khác là không có gì để bấm.
 *
 * Trang này là NHẠC TRƯỞNG, không phải nơi chứa logic: bộ lọc ở `CriteriaFilters`, lưới ở
 * `CriteriaListFeed`, luồng nạp file ở `CriteriaImportFlow`. Phần còn lại ở đây là thứ **chỉ** màn
 * hình biết — câu chữ của chip/nhãn kỳ, và luật chọn dải băng nào hiện.
 *
 * ## VÒNG 2 — đường GHI đã mở đủ (2026-09-11)
 *
 * Bốn luồng, mỗi luồng một lớp trong cùng thư mục: import (DM-7) · sửa inline (DM-6) · hộp thoại
 * Thêm/Sửa (DM-3 + DM-4) · xoá (DM-5). Import đi trước vì nó là **cánh cửa duy nhất để dữ liệu
 * vào hệ thống** — một màn hình chỉ có nút `+ Thêm chỉ tiêu` trên một danh mục 62 dòng là một ngõ
 * cụt có nút bấm.
 *
 * ## Sau khi ghi: THAY DÒNG hay TẢI LẠI — hai đường, đừng làm giống nhau (Q49)
 *
 * | Đường | Response | FE |
 * | --- | --- | --- |
 * | sửa (DM-4) · sửa inline (DM-6) | một **dòng lưới** đầy đủ | thay dòng **tại chỗ** |
 * | thêm (DM-3) · xoá (DM-5) · import (DM-7) | sáu trường danh mục / `{hardDeleted}` / kết quả job | **tải lại lưới** |
 *
 * Ba đường dưới đổi `totalCount`, đổi phân trang, và vị trí dòng thì do sắp xếp **phía server**
 * quyết — FE tự chèn là FE tự sắp, tức dựng nguồn sự thật thứ hai cho thứ tự dòng.
 *
 * ## Dải băng V3 — TỐI ĐA MỘT băng, và thứ tự ưu tiên là một quyết định
 *
 * | Ưu tiên | Băng | Vì sao trước |
 * | ---: | --- | --- |
 * | 1 | danh mục rỗng (T9) | chưa có gì để sửa thì mọi lời nhắc về kỳ đều vô nghĩa |
 * | 2 | chỉ đọc vì bộ lọc (Q37 · T15) | nó nói **không ghi được**, nên phải chặn trước lời nhắc *"sẽ ghi vào đâu"* |
 * | 3 | nhắc kỳ đích (Q72) | chỉ có nghĩa khi lời ghi thật sự đi được |
 *
 * `NO_WRITE_PERMISSION` **không** sinh băng nào (Q51): hai mã lọc là lời mời *"đổi bộ lọc rồi sẽ
 * ghi được"*, nói câu đó với người vĩnh viễn không ghi được là dắt họ vào ngõ cụt.
 */
@Component({
  selector: 'app-danh-muc-dti-page',
  standalone: true,
  imports: [
    Toolbar,
    ConfirmDialog,
    CriteriaGridTable,
    CriteriaFormDialog,
    ImportDialog,
    ImportResultDialog,
    TranslatePipe,
  ],
  templateUrl: './danh-muc-dti.page.html',
  styleUrl: './danh-muc-dti.page.scss',
  // `page-fill` đặt trên CHÍNH thẻ host, không phải một <div> bọc thêm: host vốn đã là con trực
  // tiếp của <main> (đã là flex column, xem app.scss), nên `.card` bên trong vẫn là con trực tiếp
  // của `.page-fill` — khớp đúng selector `.page-fill > .card` ở styles.scss. Cùng khuôn với
  // `platform/quan-tri-nguoi-dung/`.
  host: { class: 'page-fill' },
  providers: [
    CriteriaFilters,
    CriteriaLabels,
    CriteriaListFeed,
    CriteriaImportFlow,
    CriteriaFormFlow,
    CriteriaInlineEditFlow,
    CriteriaDeleteFlow,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DanhMucDtiPage {
  // `WRITE_FLOWS_READY` đã GỠ (2026-09-11). Nó là cờ của vòng 1 — thời điểm mọi affordance ghi
  // `disabled` bất kể quyền, vì phía sau chưa có gì. Nay cả bốn luồng ghi đã nối, nên cờ đó không
  // còn nghĩa gì và giữ lại chỉ là một mức chặn thứ hai âm thầm phủ lên `isEditable`.

  private readonly language = inject(LanguageService);
  private readonly translate = inject(TranslateService);

  protected readonly filters = inject(CriteriaFilters);
  protected readonly labels = inject(CriteriaLabels);
  protected readonly feed = inject(CriteriaListFeed);
  protected readonly importFlow = inject(CriteriaImportFlow);
  protected readonly formFlow = inject(CriteriaFormFlow);
  protected readonly inlineFlow = inject(CriteriaInlineEditFlow);
  protected readonly deleteFlow = inject(CriteriaDeleteFlow);
  protected readonly localeId = this.language.localeId;

  /** Q31 luật (a) — cột `Kỳ của số liệu` chỉ render ở chế độ `Tất cả`. */
  protected readonly showPeriodColumn = computed(() => this.filters.periodFilter() === PERIOD_ALL);

  protected readonly access = this.feed.access;

  /** Q39 — `canWrite` là trường DUY NHẤT dùng để ẨN. Đừng hoán đổi với `isEditable`. */
  protected readonly canWrite = computed(() => this.access().CanWrite);

  /**
   * Mọi affordance ghi bật/tắt theo **một** trường: `isEditable` (§5.5.1) — nút ở nguyên chỗ, chỉ
   * xám đi, vì người này CÓ quyền và sẽ ghi được ngay sau khi đổi ô lọc.
   *
   * 🛑 Đừng hoán đổi với `canWrite`: `canWrite` quyết **ẩn hẳn** (*"không phải của bạn"*), và ẩn
   * rồi hiện lại theo ô lọc sẽ làm toolbar nhảy và làm người ta tưởng mình vừa mất quyền.
   */
  protected readonly writeEnabled = computed(() => this.access().IsEditable);

  // ===== Dải băng V3 =====

  /**
   * Ca T9 — "chưa import lần nào": không điều kiện nào khác mặc định, không từ khoá, tổng = 0.
   *
   * 🛑 KHÁC hẳn ca "lọc không khớp" (dòng `.muted` trong bảng): chỗ nào rỗng vì **chưa có việc gì
   * xảy ra** thì dùng băng có lối đi tiếp; chỗ nào rỗng vì **bộ lọc hẹp** thì dùng thông điệp
   * trong bảng. Gộp hai ca là mất đúng thông tin người dùng cần.
   */
  protected readonly showEmptyCatalogueNotice = computed(
    () =>
      !this.feed.loading() &&
      !this.feed.loadError() &&
      this.filters.activeFilterCount() === 0 &&
      !this.filters.searchText() &&
      this.feed.totalCount() === 0,
  );

  /**
   * Mã lý do đang chặn ghi, đã LỌC bỏ `NO_WRITE_PERMISSION` (Q51 — xem JSDoc của class).
   *
   * Mã LẠ vẫn được giữ lại trong danh sách (rơi vào câu chung ở template): `isEditable` đã `false`
   * nên bỏ qua nó là đi ngược chiều an toàn.
   */
  protected readonly blockNotices = computed<readonly string[]>(() => {
    if (this.showEmptyCatalogueNotice()) return [];
    return this.access().EditBlockedBy.filter((code) => code !== 'NO_WRITE_PERMISSION');
  });

  /**
   * Nhãn của kỳ mà **một lời ghi sẽ rơi vào** — không phải kỳ đang hiển thị.
   *
   * Ở chế độ `Tất cả` hai thứ đó KHÁC NHAU, và đó là toàn bộ lý do Q72 tồn tại: lưới hiện số của
   * tuần 29, tuần 31, tuần 33… (mỗi dòng một kỳ), còn mọi lời ghi đều rơi vào **tuần hiện tại**.
   *
   * 🛑 `CurrentPeriodLabel` đến từ **server** (khối quyền cấp màn của DM-2). Không tự tính: lịch
   * ISO chỉ có một bản cài, và một máy khách lệch múi giờ vào đêm Chủ nhật sẽ nói sai tuần.
   */
  protected readonly writeTargetLabel = computed<string>(() => {
    if (this.filters.periodFilter() === PERIOD_ALL) return this.access().CurrentPeriodLabel;
    return this.labels.selectedPeriodLabel() ?? '';
  });

  /**
   * Băng "nhắc kỳ đích" (§5.5) — khoá dịch cần dùng, hoặc `null` khi không hiện băng nào.
   *
   * **Hai ca hiện băng**, và ca thứ hai mới là ca nguy hiểm:
   *
   * | `Kỳ trong năm` | Lưới hiện số của | Lời ghi rơi vào | Băng |
   * | --- | --- | --- | --- |
   * | tuần **hiện tại** | tuần hiện tại | tuần hiện tại | **không** — hai thứ trùng nhau |
   * | một tuần **khác** | chính tuần đó | chính tuần đó | có — nhắc rằng không phải tuần này |
   * | **`Tất cả`** | **mỗi dòng một kỳ** | **tuần hiện tại** | có |
   *
   * Ca `Tất cả` **hợp lệ theo thiết kế** nên **không mã lỗi nào chặn** — dải băng là chỗ **duy
   * nhất** báo trước. Hai thứ khác cũng nói về kỳ đích đều **muộn hơn**: tiêu đề hộp thoại chỉ
   * hiện khi đã mở hộp thoại (không cứu được sửa inline), còn cột `Kỳ của số liệu` chỉ đổi **sau
   * khi** đã lưu.
   */
  protected readonly targetPeriodNoticeKey = computed<string | null>(() => {
    // Ghi không được thì không có lời ghi nào để nhắc — và hai băng ưu tiên cao hơn đã nói lý do.
    if (!this.access().IsEditable) return null;
    if (this.showEmptyCatalogueNotice() || this.blockNotices().length > 0) return null;
    if (!this.writeTargetLabel()) return null;

    const period = this.filters.periodFilter();
    if (period === PERIOD_ALL) return 'danh-muc-dti.notice.targetPeriodCurrent';
    // So với `CurrentPeriod` của SERVER, không với đồng hồ máy khách (§5.5 mục 2, cùng lý do Q40).
    if (period === this.access().CurrentPeriod) return null;
    return 'danh-muc-dti.notice.targetPeriodPast';
  });

  protected readonly currentYearText = `${this.filters.currentYear}`;

  // ===== Lưới =====

  // ===== Import (DM-7) =====

  /** Câu "nhập cho kỳ X" hiện TRONG hộp thoại — băng V3 bị chính hộp thoại modal che mất. */
  protected readonly importTargetText = computed<string>(() => {
    this.language.current();
    const label = this.writeTargetLabel();
    if (!label) return '';
    return this.translate.instant('danh-muc-dti.import.targetPeriod', { Period: label }) as string;
  });

  // ===== Hộp thoại Thêm/Sửa (DM-3 + DM-4) và Xoá (DM-5) =====

  /**
   * Câu kỳ đích cho hộp thoại V9 — dùng lại ĐÚNG hai câu băng V3 đã duyệt, theo cùng luật chọn
   * biến thể. Một câu thứ ba nói cùng một chuyện là một chỗ nữa để hai câu lệch nhau.
   *
   * Ở chế độ `Tất cả`, câu này nêu **tuần hiện tại** chứ không phải kỳ của dòng đang mở — §5.5
   * mục 2, và đó là đúng chỗ nhầm lẫn đắt nhất của màn: người dùng mở một dòng đang hiện số tuần
   * 29 và số họ gõ đi vào tuần 33.
   */
  protected readonly formTargetText = computed<string>(() => {
    this.language.current();
    const label = this.writeTargetLabel();
    if (!label) return '';
    const period = this.filters.periodFilter();
    const key =
      period === PERIOD_ALL || period === this.access().CurrentPeriod
        ? 'danh-muc-dti.notice.targetPeriodCurrent'
        : 'danh-muc-dti.notice.targetPeriodPast';
    return this.translate.instant(key, { Period: label }) as string;
  });

  /** Câu hỏi xác nhận xoá — MỘT chuỗi có hai tham số, không nối mã + tên bằng `+` (§8). */
  protected readonly deleteMessage = computed<string>(() => {
    this.language.current();
    const row = this.deleteFlow.target();
    if (!row) return '';
    return this.translate.instant('danh-muc-dti.dialog.deleteMessage', { Code: row.Code, Name: row.Name }) as string;
  });

  /**
   * `ConfirmDialog` mở bằng LỆNH (`open()`), không bằng một input `[open]` — hợp đồng của nó là
   * `<dialog>` gốc và `showModal()` phải được gọi từ một nơi biết đúng thời điểm. Trang vì vậy
   * cầm tham chiếu và đồng bộ nó với `deleteFlow.target()` bằng một `effect`.
   */
  private readonly deleteDialog = viewChild.required<ConfirmDialog>('deleteDialog');

  constructor() {
    // 🛑 Q62: URL bẩn ⇒ viết lại URL và KHÔNG gọi API ở nhịp này. Lần điều hướng đó làm
    // `queryParamMap` phát lại, effect chạy lần hai, và lúc đó `isClean()` đã đúng.
    effect(() => {
      if (!this.filters.isClean()) {
        this.filters.rewriteCleanUrl();
        return;
      }
      this.feed.load(this.filters.requestParams());
    });

    // Một nguồn sự thật: `deleteFlow.target()`. `open()`/`close()` của hộp thoại đều vô hại khi
    // gọi lúc trạng thái đã đúng (chúng tự kiểm `el.open`), nên không cần cờ "đang mở" thứ hai —
    // mà một cờ thứ hai chính là chỗ hộp thoại kẹt mở sau khi luồng đã xoá xong.
    effect(() => {
      if (this.deleteFlow.target()) this.deleteDialog().open();
      else this.deleteDialog().close();
    });
  }

  /** Handler của nút "Thử lại" ở khối lỗi. */
  protected reload(): void {
    this.feed.load(this.filters.requestParams());
  }

  protected onGridPageChange(event: { Page: number; PageSize: number }): void {
    this.filters.changePage(event.Page, event.PageSize);
  }

  /**
   * Cặp `{ period, year }` của MỌI lời ghi trên màn này — một hàm, một chỗ gọi cho cả bốn đường
   * (ui-spec §7.4b ràng buộc 1). Bốn bản sao là bốn cơ hội để một đường quên `year` khi `"all"`.
   */
  private writePeriod(): IWritePeriod | null {
    return buildWritePeriod(this.filters.periodFilter(), this.filters.yearFilter());
  }

  /**
   * Tra dòng theo id trong lưới ĐANG hiện.
   *
   * Component lưới phát `criteriaId` chứ không phát cả `ICriteriaRow`: dòng là dữ liệu của trang,
   * và một component dumb gửi ngược object dữ liệu lên là mời gọi việc nó giữ một bản sao đã cũ.
   */
  private rowOf(criteriaId: string): ICriteriaRow | null {
    return this.feed.rows().find((row) => row.CriteriaId === criteriaId) ?? null;
  }

  protected onInlineSave(event: IInlineSaveEvent): void {
    const source = this.rowOf(event.CriteriaId);
    if (!source) return;
    this.inlineFlow.save(event, this.writePeriod(), source, (row) => this.feed.replaceRow(row));
  }

  protected onEditRow(criteriaId: string): void {
    const row = this.rowOf(criteriaId);
    if (row) this.formFlow.openEdit(row);
  }

  protected onDeleteRow(criteriaId: string): void {
    const row = this.rowOf(criteriaId);
    if (row) this.deleteFlow.ask(row);
  }

  /**
   * Thêm ⇒ **tải lại lưới** (Q49); sửa ⇒ **thay dòng tại chỗ**. Hai callback, không phải một —
   * xem bảng ở JSDoc của class.
   */
  /** Xoá xong ⇒ **tải lại lưới**: nó đổi `totalCount` và có thể làm trang hiện tại rỗng. */
  protected onDeleteConfirmed(): void {
    this.deleteFlow.confirm(() => this.reload());
  }

  protected onFormSave(value: ICriteriaFormValue): void {
    this.formFlow.save(
      value,
      this.writePeriod(),
      () => this.reload(),
      (row) => this.feed.replaceRow(row),
    );
  }

  /**
   * Gửi file. Cặp `{ period, year }` dựng bằng `buildWritePeriod` — **cùng hàm** mà dialog sửa và
   * sửa inline sẽ gọi, để không đường nào quên `year` khi `"all"` (ui-spec §7.4b ràng buộc 1).
   */
  protected onImportSubmit(file: File): void {
    const period = buildWritePeriod(this.filters.periodFilter(), this.filters.yearFilter());
    this.importFlow.submit(file, period, () => this.reload());
  }
}
