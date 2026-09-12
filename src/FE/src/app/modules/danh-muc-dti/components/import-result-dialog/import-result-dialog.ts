import { isPlatformBrowser } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  PLATFORM_ID,
  computed,
  effect,
  inject,
  input,
  output,
  viewChild,
} from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { IImportResult, IImportRowError } from '../../models/danh-muc-dti.model';

/** Một dòng lỗi đã dựng thành CÂU — `track` theo `Key` vì hai lỗi có thể cùng `RowNumber`. */
export interface IImportErrorLine {
  readonly Key: string;
  readonly Text: string;
}

/**
 * V12 — hộp thoại `Kết quả Import` (DM-7 bước 2, `status = "Succeeded"`).
 *
 * ## FE sở hữu câu chữ; BE chỉ gửi mã + tham số
 *
 * `result.errors[]` mang `{ rowNumber, code, messageParams }` và **không** có khoá `message` —
 * `doc/huong_dan/wiki-core/be/16-i18n-va-ma-loi.md` §3. BE ghép sẵn *"Điểm tự đánh giá (6) vượt
 * quá điểm tối đa (5)"* thì FE **không tách lại được** con số ra khỏi câu, nên không đổi được
 * ngôn ngữ và không định dạng lại được số theo locale.
 *
 * `businessCode` **chính là khoá dịch**, lồng hai cấp (`IMPORT` → `ROW_…`); `messageParams` khớp
 * thẳng cú pháp `{{Tên}}` của ngx-translate nên không phải quy đổi gì.
 *
 * ## `rowNumber` là một TRƯỜNG, không phải tham số — nên tiền tố do FE sở hữu
 *
 * Hai khuôn tiền tố, và cái thứ hai không phải để cho đẹp: `IMPORT.ROW_CODE_MISSING` **cố ý**
 * không mang `Code` — chính mã chỉ tiêu là thứ đang thiếu ở dòng đó. In `mã ""` cho ca ấy là nói
 * dối về nội dung file.
 *
 * ## 🛑 `ErrorCount` đếm số DÒNG hỏng, KHÔNG đếm số phần tử `errors[]`
 *
 * Đo được khi BE nghiệm thu trên dữ liệu thật (2026-09-11, `doc/contracts/danh-muc-dti.md`
 * §"Nghiệm thu — shape THẬT"): một dòng sai **cả hai** cột điểm cho ra **hai** phần tử `errors[]`
 * cùng `rowNumber` nhưng vẫn là **một** dòng hỏng. Vì vậy câu tổng hợp đọc `ErrorCount` của
 * server, còn `<ul>` bên dưới liệt kê từng phần tử — hai con số **được phép** khác nhau, và dùng
 * `errors.length` cho câu tổng hợp sẽ làm `59 + N ≠ 62` ngay ở ca đó.
 *
 * ## `IMPORT.ROW_*` là lỗi MỘT DÒNG; `IMPORT.FILE_*` là lỗi CẢ FILE — tiền tố mang nghĩa
 *
 * Năm mã của Q73 (duyệt tên 2026-09-11: `ROW_NAME_MISSING` · `ROW_MAX_SCORE_INVALID` ·
 * `ROW_SELF_SCORE_INVALID` · `ROW_VERIFIED_SCORE_INVALID` · `ROW_DEADLINE_INVALID`) đi vào
 * `result.errors[]` và job vẫn `Succeeded` — các dòng khác **vẫn được nạp**.
 *
 * 🛑 Đối lập với nó là `IMPORT.FILE_TOO_MANY_ROWS` (Q75): job **`Failed`**, `result` vắng mặt,
 * **không dòng nào** được ghi. Nó không bao giờ xuất hiện trong hộp thoại này — nó đi bằng toast
 * qua `errorCode` (xem `criteria-import-flow.ts`). Đó là lý do tiền tố khác nhau, và là lý do câu
 * dịch của nó phải nói rõ *"không dòng nào được nhập"*: một câu mơ hồ ở đây khiến người dùng tin
 * là file đã nạp được một phần.
 *
 * ## Job `Failed` KHÔNG mở hộp thoại này
 *
 * `Failed` là lỗi **hạ tầng** (file hỏng, job crash): `result` vắng mặt, nên không có gì để trình
 * bày. Nó đi bằng toast — ranh giới đó thuộc hợp đồng, không phải lựa chọn hiển thị.
 */
@Component({
  selector: 'app-import-result-dialog',
  standalone: true,
  imports: [TranslatePipe],
  templateUrl: './import-result-dialog.html',
  styleUrl: './import-result-dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ImportResultDialog {
  private readonly platformId = inject(PLATFORM_ID);

  /**
   * Chỉ dùng để dựng câu lỗi từng dòng từ `code` + `messageParams`.
   *
   * G4 cấm component inject `HttpClient` hoặc service **dữ liệu**; `TranslateService` nằm trong
   * `UI_INFRA_SERVICES` của `scripts/fe-gate.sh` — miễn trừ theo TÊN service, không phải theo file.
   */
  private readonly translate = inject(TranslateService);

  readonly open = input.required<boolean>();
  readonly result = input<IImportResult | null>(null);

  readonly closed = output<void>();

  protected readonly summaryParams = computed(() => {
    const result = this.result();
    return {
      TotalRows: `${result?.TotalRows ?? 0}`,
      SuccessCount: `${result?.SuccessCount ?? 0}`,
      ErrorCount: `${result?.ErrorCount ?? 0}`,
      CriteriaCreatedCount: `${result?.CriteriaCreatedCount ?? 0}`,
    };
  });

  /**
   * Dòng lỗi đã dịch.
   *
   * `this.translate.currentLang()` được đọc để phụ thuộc signal tường minh tại chỗ — cùng lý do và
   * cùng cảnh báo như ở `user-form-dialog.ts`: `instant()` của v18 tự đăng ký phụ thuộc, nhưng đó
   * là chi tiết bên trong thư viện và không nên buộc màn hình vào nó.
   */
  protected readonly errorLines = computed<IImportErrorLine[]>(() => {
    this.translate.currentLang();
    return (this.result()?.Errors ?? []).map((error, index) => ({
      Key: `${error.RowNumber}-${error.Code}-${index}`,
      Text: this.lineOf(error),
    }));
  });

  private readonly dialogEl = viewChild.required<ElementRef<HTMLDialogElement>>('dialogEl');

  constructor() {
    effect(() => {
      if (!isPlatformBrowser(this.platformId)) return;
      const el = this.dialogEl().nativeElement;
      if (this.open() && !el.open) el.showModal();
      if (!this.open() && el.open) el.close();
    });
  }

  protected onNativeClose(): void {
    this.closed.emit();
  }

  /**
   * Tiền tố + thân câu. Mã LẠ (chưa có trong bảng dịch) vẫn phải in ra được **cùng `rowNumber`**:
   * một dòng lỗi biến mất khỏi danh sách là người dùng sửa file xong vẫn thấy `errorCount` không
   * khớp số dòng in ra. `translate.instant` tra trượt thì trả về chính chuỗi khoá — xấu, nhưng nó
   * vẫn trỏ đúng dòng và vẫn tra được trong hợp đồng.
   */
  private lineOf(error: IImportRowError): string {
    const prefixKey = error.MessageParams['Code']
      ? 'danh-muc-dti.import.rowPrefix'
      : 'danh-muc-dti.import.rowPrefixNoCode';
    const prefix = this.translate.instant(prefixKey, {
      RowNumber: `${error.RowNumber}`,
      Code: error.MessageParams['Code'] ?? '',
    }) as string;
    const body = this.translate.instant(error.Code, error.MessageParams) as string;
    return `${prefix}${body}`;
  }
}
