import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { CriteriaGridTable } from './criteria-grid-table';
import { ICriteriaRow } from '../../models/danh-muc-dti.model';

function row(overrides: Partial<ICriteriaRow> = {}): ICriteriaRow {
  return {
    CriteriaId: 'c1',
    Code: 'DTI-01',
    Name: 'Chỉ tiêu mẫu',
    GroupId: 'g1',
    GroupCode: 'NHOM-1',
    GroupName: 'Nhóm 1',
    MaxScore: 10,
    AssessmentId: null,
    ProgressPercent: null,
    SelfScore: null,
    VerifiedScore: null,
    Diff: null,
    Status: null,
    OwnerId: null,
    OwnerName: null,
    Deadline: null,
    Note: null,
    AssessmentDate: null,
    Evidences: [],
    IsEditable: true,
    ...overrides,
  };
}

/**
 * RULE READ-ONLY — `spec/danh-muc-dti/ui-spec.md` §"vòng phản hồi #4 — ĐÃ CHỐT CHÍNH THỨC" mục 3:
 * grid chỉ cho sửa — liệt kê ĐÍCH DANH "✓/✗ inline, Import CSV, +Thêm chỉ tiêu/**Sửa/Xoá**" — khi
 * đang xem "Tất cả" của NĂM HIỆN TẠI. BE tính điều đó ra `row.IsEditable`.
 *
 * Lỗi đã sửa 2026-08-22: hai nút Sửa/Xoá render VÔ ĐIỀU KIỆN, trong khi ô Tiến độ/Ghi chú cùng
 * hàng và thanh công cụ (`danh-muc-dti.page.html`) đều đã tôn trọng luật. Luật được áp cho thanh
 * công cụ mà quên hàng ⇒ đang xem dữ liệu quá khứ vẫn xoá được chỉ tiêu.
 *
 * Cặp test đi đôi có chủ đích: chỉ có ca "ẩn" thì một bản sửa quá tay (ẩn nút ở MỌI trạng thái)
 * vẫn xanh — mà như vậy là không ai sửa được gì nữa.
 */
describe('CriteriaGridTable — RULE READ-ONLY cho nút Sửa/Xoá', () => {
  let fixture: ComponentFixture<CriteriaGridTable>;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideZonelessChangeDetection()] });
    fixture = TestBed.createComponent(CriteriaGridTable);
  });

  function renderWith(isEditable: boolean): HTMLElement {
    fixture.componentRef.setInput('rows', [row({ IsEditable: isEditable })]);
    fixture.componentRef.setInput('totalCount', 1);
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  function actionLabels(host: HTMLElement): string[] {
    return Array.from(host.querySelectorAll('.action-btn')).map((b) => (b.textContent ?? '').trim());
  }

  it('IsEditable = true (đang xem Live) → CÓ nút Sửa và Xoá', () => {
    const labels = actionLabels(renderWith(true));

    expect(labels)
      .withContext('ẩn nút ở mọi trạng thái thì không ai sửa được gì nữa — đó là sửa quá tay')
      .toContain('Sửa');
    expect(labels).toContain('Xoá');
  });

  it('IsEditable = false (đang xem lịch sử) → KHÔNG có nút nào, hiển thị "—"', () => {
    const host = renderWith(false);

    expect(actionLabels(host))
      .withContext('xem dữ liệu quá khứ mà vẫn xoá được chỉ tiêu — đúng thứ RULE READ-ONLY chặn')
      .toEqual([]);
    expect((host.textContent ?? '')).toContain('—');
  });
});
