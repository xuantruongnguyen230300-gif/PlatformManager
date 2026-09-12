import { Injectable, computed, inject } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { LanguageService } from '../../../../core/i18n/language.service';
import { IToolbarChip } from '../../../../shared/components/toolbar/toolbar';
import { IPeriodOption } from '../../../../shared/models/dti-period.model';
import { periodLabelParams } from '../../../../shared/services/dti-period.format';
import { DashboardFeed } from './dashboard-feed';
import { DashboardFilters } from './dashboard-filters';

/**
 * CÂU CHỮ và ĐỊNH DẠNG SỐ của màn Dashboard: chip bộ lọc, nhãn nhóm, nhãn kỳ, phần trăm và số đếm
 * theo locale.
 *
 * Tách khỏi `DashboardPage` (vòng 2, 2026-09-11) cùng lý do với `criteria-labels.ts` ở màn Danh
 * mục: đây là một trục riêng — không phải trạng thái, mà là phép biến trạng thái thành chuỗi
 * người đọc.
 *
 * 🛑 `@Injectable()` KHÔNG `providedIn: 'root'` — nó đọc hai lớp có vòng đời bằng vòng đời trang.
 */
@Injectable()
export class DashboardLabels {
  private readonly language = inject(LanguageService);
  private readonly translate = inject(TranslateService);
  private readonly filters = inject(DashboardFilters);
  private readonly feed = inject(DashboardFeed);

  private readonly localeId = this.language.localeId;

  /**
   * Danh sách nhóm cho ô lọc, lấy từ CHÍNH `groups[]` của DB-1 — không gọi thêm DM-1.
   *
   * Hai lý do: `groups[]` luôn tính trên toàn bộ chỉ tiêu của kỳ (luật 1) nên nó không bị bộ lọc
   * làm hẹp lại, và DM-1 là hợp đồng của module Danh mục — gọi nó từ đây là buộc màn này phụ thuộc
   * một endpoint nó không cần.
   */
  readonly groupChoices = computed(() => this.feed.data()?.Groups ?? []);

  readonly filterChips = computed<IToolbarChip[]>(() => {
    this.language.current();
    const chips: IToolbarChip[] = [];
    const groupId = this.filters.groupFilter();
    if (groupId) {
      chips.push({
        Key: 'groupId',
        Label: this.translate.instant('dashboard.filter.chipGroup', { Value: this.groupLabelOf(groupId) }) as string,
      });
    }
    const status = this.filters.statusFilter();
    if (status) {
      chips.push({
        Key: 'status',
        Label: this.translate.instant('dashboard.filter.chipStatus', { Value: status }) as string,
      });
    }
    return chips;
  });

  /**
   * `—` là *"không tính được"*; `0` là *"tính được, kết quả bằng không"*.
   *
   * 🛑 Đừng gộp hai ca đó: ngay sau import, mọi `Tiến độ %` đều **rỗng** (Q24) — hiện `0%` ở đó là
   * nói dối, và người dùng sẽ đi tìm lỗi import.
   */
  percent(value: number | null | undefined): string | null {
    if (value === null || value === undefined || !Number.isFinite(value)) return null;
    return new Intl.NumberFormat(this.localeId(), {
      style: 'percent',
      minimumFractionDigits: 1,
      maximumFractionDigits: 1,
    }).format(value / 100);
  }

  count(value: number | null | undefined): string | null {
    if (value === null || value === undefined || !Number.isFinite(value)) return null;
    return new Intl.NumberFormat(this.localeId()).format(value);
  }

  /** `1. Hạ tầng và Nền tảng số` — Q42, cùng khuôn với thanh nhóm ở V3. */
  groupOptionLabel(code: string, name: string): string {
    this.language.current();
    return this.translate.instant('dashboard.grid.groupLabel', { Code: code, Name: name }) as string;
  }

  /** `Tuần 33 · 10/08 – 16/08 · 82,1%` — khuôn ba đoạn của màn này (dấu `·`, khác màn Danh mục). */
  periodOptionLabel(option: IPeriodOption): string {
    this.language.current();
    const params = periodLabelParams(option, this.localeId());
    const progress = this.percent(option.OverallProgress);
    // Kỳ CHƯA có số liệu dùng khuôn hai đoạn: một `· ` treo lơ lửng ở cuối option trông như chuỗi
    // bị cắt cụt, và `0%` thì nói dối (RỖNG ≠ 0).
    const key =
      progress === null
        ? option.Unit === 'week'
          ? 'dashboard.period.weekOptionNoData'
          : 'dashboard.period.monthOptionNoData'
        : option.Unit === 'week'
          ? 'dashboard.period.weekOption'
          : 'dashboard.period.monthOption';
    return this.translate.instant(key, { ...params, Progress: progress ?? '' }) as string;
  }

  /**
   * Nhãn kỳ ĐẦY ĐỦ (`Tuần 33/2026 (10/08 – 16/08/2026)`) cho `title` của nút `Xuất báo cáo`.
   *
   * Khác khuôn của option trong ô chọn: ở đây câu là một câu văn xuôi, nên nó dùng dạng đầy đủ có
   * năm — người đọc `title` không nhìn thấy ô `Năm` lúc đó.
   */
  readonly exportPeriodLabel = computed<string>(() => {
    const option = this.filters.periodChoices().find((item) => item.Value === this.filters.period());
    if (!option) return '';
    const key = option.Unit === 'week' ? 'dashboard.period.weekFull' : 'dashboard.period.monthFull';
    return this.translate.instant(key, periodLabelParams(option, this.localeId())) as string;
  });

  historyLabel(option: IPeriodOption): { From: Date; To: Date } {
    return { From: option.StartDate, To: option.EndDate };
  }

  private groupLabelOf(groupId: string): string {
    const group = this.groupChoices().find((item) => item.GroupId === groupId);
    return group ? this.groupOptionLabel(group.GroupCode, group.GroupName) : groupId;
  }
}
