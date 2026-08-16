// Dùng CHUNG cho Dashboard + Danh mục DTI (2 feature cùng cần "năm/kỳ nào có dữ liệu" — xem
// doc/contracts/dashboard.md CONTRACT DB-3 / doc/contracts/danh-muc-dti.md CONTRACT DM-8) — vì
// vậy đặt ở `shared/` thay vì trong 1 module cụ thể, đúng quy tắc
// src/FE/.claude/docs/architecture.md (chỉ đặt trong `modules/<feature>/` khi đúng 1 feature dùng).

export interface IPeriodOptionDto {
  value: string;
  date: string;
  overallProgress: number | null;
}

export interface IPeriodOptionsDto {
  years: number[];
  weeksInYear: IPeriodOptionDto[];
  monthsInYear: IPeriodOptionDto[];
}

export interface IPeriodOption {
  Value: string;
  Date: string;
  OverallProgress: number | null;
}

export interface IPeriodOptions {
  Years: number[];
  WeeksInYear: IPeriodOption[];
  MonthsInYear: IPeriodOption[];
}
