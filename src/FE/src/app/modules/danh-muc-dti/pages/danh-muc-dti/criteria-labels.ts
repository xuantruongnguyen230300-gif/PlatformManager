import { Injectable, computed, inject } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { LanguageService } from '../../../../core/i18n/language.service';
import { IToolbarChip } from '../../../../shared/components/toolbar/toolbar';
import { IPeriodOption } from '../../../../shared/models/dti-period.model';
import { periodLabelParams, shortRangeParams } from '../../../../shared/services/dti-period.format';
import { ICriteriaGroup } from '../../models/danh-muc-dti.model';
import { PERIOD_ALL } from '../../services/write-period';
import { CriteriaFilters } from './criteria-filters';

/**
 * CÂU CHỮ của màn Danh mục DTI: chip bộ lọc, nhãn nhóm, nhãn kỳ.
 *
 * Tách khỏi `DanhMucDtiPage` (vòng 2, 2026-09-11) vì nó là một **trục** riêng: không phải trạng
 * thái (bộ lọc, lưới, hộp thoại) mà là phép biến trạng thái đó thành chuỗi người đọc. Trang giữ
 * lại đúng phần *quyết định hiện cái gì*; lớp này trả lời *nó đọc ra sao*.
 *
 * 🛑 Mọi chuỗi ở đây ráp bằng **khoá + tham số**, không nối bằng `+`. Nối chuỗi khoá cứng trật tự
 * từ theo tiếng Việt, và bản `en` sẽ đọc như máy dịch (`spec/danh-muc-dti/ui-spec.md` §8).
 *
 * 🛑 `@Injectable()` KHÔNG `providedIn: 'root'` — nó đọc `CriteriaFilters`, một lớp có vòng đời
 * bằng vòng đời trang.
 *
 * ## `this.language.current()` trong mỗi `computed` — có chủ đích
 *
 * Nó làm phụ thuộc signal trở nên **tường minh tại chỗ đọc**. `translate.instant()` của v18 tự
 * đăng ký phụ thuộc (đo 2026-09-06 bằng canary), nên bỏ dòng này mọi thứ vẫn chạy — nhưng lúc đó
 * cả màn hình bị buộc vào một chi tiết bên trong thư viện. Xem
 * `doc/huong_dan/wiki-core/fe/08-i18n.md` §Cạm bẫy 2.
 */
@Injectable()
export class CriteriaLabels {
  private readonly language = inject(LanguageService);
  private readonly translate = inject(TranslateService);
  private readonly filters = inject(CriteriaFilters);

  private readonly localeId = this.language.localeId;

  /**
   * Chip dựng từ điều kiện ĐANG ÁP (không phải bản nháp).
   *
   * `Label` là CÂU đã dịch: `IToolbarChip` đi thẳng vào `<app-toolbar>`, một component dumb chỉ
   * biết vẽ chữ nó nhận được.
   */
  readonly filterChips = computed<IToolbarChip[]>(() => {
    this.language.current();
    const chips: IToolbarChip[] = [];

    const groupId = this.filters.groupFilter();
    if (groupId) {
      chips.push({ Key: 'groupId', Label: this.chipLabel('chipGroup', this.groupLabelOf(groupId)) });
    }
    const status = this.filters.statusFilter();
    if (status) chips.push({ Key: 'status', Label: this.chipLabel('chipStatus', status) });

    const year = this.filters.yearFilter();
    if (year !== this.filters.currentYear) chips.push({ Key: 'year', Label: this.chipLabel('chipYear', `${year}`) });

    const period = this.filters.periodFilter();
    if (period !== PERIOD_ALL) {
      chips.push({ Key: 'period', Label: this.chipLabel('chipPeriod', this.periodShortLabelOf(period)) });
    }
    return chips;
  });

  /**
   * Bảng tra `mã kỳ → khoảng ngày` cho cột `Kỳ của số liệu`.
   *
   * Đây là một phép TRA trên dữ liệu BE đã trả (`weeksInYear`), **không** phải quy đổi lịch — quy
   * `"2026-W33"` ra khoảng ngày là việc của BE (`business-rules` §5.1). Tra trượt thì component
   * lùi về `assessmentPeriodLabel` mà chính DM-2 gửi kèm dòng, nên ô không bao giờ trống vì lý do
   * kỹ thuật.
   */
  readonly periodRangeLabels = computed<Record<string, string>>(() => {
    this.language.current();
    const locale = this.localeId();
    const table: Record<string, string> = {};
    for (const option of this.filters.periodOptions()?.WeeksInYear ?? []) {
      table[option.Value] = this.translate.instant(
        'danh-muc-dti.period.shortRange',
        shortRangeParams(option, locale),
      ) as string;
    }
    return table;
  });

  /**
   * Nhãn kỳ ĐẦY ĐỦ của kỳ đang chọn (`Tháng 8/2026 (01/08 – 31/08/2026)`) — cắm vào câu băng
   * `PERIOD_NOT_WEEKLY`. `null` khi chưa tra được: câu băng khi đó lùi về biến thể không nêu kỳ.
   */
  readonly selectedPeriodLabel = computed<string | null>(() => {
    const option = this.filters.periodChoices().find((item) => item.Value === this.filters.periodFilter());
    if (!option) return null;
    const key = option.Unit === 'week' ? 'danh-muc-dti.period.weekFull' : 'danh-muc-dti.period.monthFull';
    this.language.current();
    return this.translate.instant(key, periodLabelParams(option, this.localeId())) as string;
  });

  /** Nhãn một option nhóm — `1. Hạ tầng và Nền tảng số` (Q42), ráp qua khoá dịch. */
  groupOptionLabel(group: ICriteriaGroup): string {
    this.language.current();
    return this.translate.instant('danh-muc-dti.grid.groupLabel', { Code: group.Code, Name: group.Name }) as string;
  }

  /** `Tuần 33: 10/08 – 16/08/2026` — khuôn hai đoạn của màn này (dấu hai chấm, không phải `·`). */
  periodOptionLabel(option: IPeriodOption): string {
    this.language.current();
    const key = option.Unit === 'week' ? 'danh-muc-dti.period.weekOption' : 'danh-muc-dti.period.monthOption';
    return this.translate.instant(key, periodLabelParams(option, this.localeId())) as string;
  }

  private chipLabel(key: string, value: string): string {
    return this.translate.instant(`danh-muc-dti.filter.${key}`, { Value: value }) as string;
  }

  /** `{code}. {name}` — Q42. Không tìm thấy thì trả lại chính id, để chip không bao giờ trống. */
  private groupLabelOf(groupId: string): string {
    const group = this.filters.groups().find((item) => item.Id === groupId);
    return group ? this.groupOptionLabel(group) : groupId;
  }

  private periodShortLabelOf(period: string): string {
    const option = this.filters.periodChoices().find((item) => item.Value === period);
    return option ? this.periodOptionLabel(option) : period;
  }
}
