import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { APP_I18N } from '../../../../app.config';
import { useTranslationsInTest } from '../../../../core/i18n/i18n.testing';
import { ICriteriaRow } from '../../models/danh-muc-dti.model';
import { CriteriaFormDialog, ICriteriaFormValue } from './criteria-form-dialog';

const ROW: ICriteriaRow = {
  CriteriaId: 'c1',
  Code: '4.22.11',
  Name: 'Mức độ ứng dụng AI',
  GroupId: 'g1',
  GroupCode: '4',
  GroupName: 'Chính quyền số',
  MaxScore: 10,
  AssessmentId: 'a1',
  AssessmentDate: '2026-08-16',
  ProgressPercent: 70,
  SelfScore: 7.04,
  VerifiedScore: 10,
  Diff: 2.96,
  Status: 'Đang thực hiện',
  OwnerId: 'u1',
  OwnerName: 'Nguyễn Văn A',
  Deadline: '2026-08-31',
  Note: 'CV 123',
  Version: 'v1',
  AssessmentPeriod: '2026-W33',
  AssessmentPeriodLabel: 'Tuần 33/2026 (10/08 – 16/08/2026)',
};

describe('CriteriaFormDialog (DM-3 + DM-4 — 10 trường, MỘT nút Lưu)', () => {
  let fixture: ComponentFixture<CriteriaFormDialog>;

  async function boot(editing: ICriteriaRow | null): Promise<void> {
    // `reset` để `boot()` gọi được nhiều lần trong MỘT `it` (xem ca Q65 duyệt bốn mã sai).
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      // `provideCoreI18n` KHÔNG có ở đây: nó đòi token `CORE_I18N` do `app.config.ts` bơm vào, và
      // spec của một component dumb không cấp thứ đó. `useTranslationsInTest()` nạp thẳng bảng
      // dịch thật vào `TranslateService` — đủ để kiểm CÂU, không cần cơ chế đổi ngôn ngữ.
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideTranslateService(),
      ],
    });
    // Truyền `resources` TƯỜNG MINH: không có `provideCoreI18n`, helper lùi về `CORE_ONLY_RESOURCES`
    // (chỉ `/i18n/`) và mọi khoá `danh-muc-dti.*` sẽ tra trượt — ngx-translate khi đó trả về chính
    // chuỗi khoá, nên test đo được một câu "đúng" mà người dùng không bao giờ đọc.
    await useTranslationsInTest('vi', { resources: APP_I18N.resources });
    fixture = TestBed.createComponent(CriteriaFormDialog);
    fixture.componentRef.setInput('open', true);
    fixture.componentRef.setInput('editing', editing);
    fixture.componentRef.setInput('groups', [{ Id: 'g1', Code: '4', Name: 'Chính quyền số', DisplayOrder: 4 }]);
    fixture.detectChanges();
  }

  function host(): HTMLElement {
    return fixture.nativeElement as HTMLElement;
  }

  function setValue(selector: string, value: string): void {
    const el = host().querySelector(selector) as HTMLInputElement | HTMLTextAreaElement;
    el.value = value;
    el.dispatchEvent(new Event('input'));
  }

  function save(): ICriteriaFormValue | null {
    let emitted: ICriteriaFormValue | null = null;
    const sub = fixture.componentInstance.saved.subscribe((value) => (emitted = value));
    (host().querySelectorAll('.dialog-actions button')[1] as HTMLButtonElement).click();
    fixture.detectChanges();
    sub.unsubscribe();
    return emitted;
  }

  function errorOf(id: string): string | null {
    return host().querySelector(`#${id}`)?.textContent?.trim() ?? null;
  }

  it('🛑 mã BA CẤP (`4.22.11`) là HỢP LỆ — dữ liệu thật có nó, đừng giả định khuôn `N.N`', async () => {
    await boot(ROW);
    expect(save()?.Code).toBe('4.22.11');
  });

  it('🛑 Q58 — một đoạn quá 4 chữ số bị CHẶN trước khi gửi', async () => {
    await boot(ROW);
    setValue('#cfCode', '4.12345');

    expect(save()).withContext('không được gửi gì').toBeNull();
    expect(errorOf('cfCodeError')).toContain('4 chữ số');
  });

  it('🛑 Q65 — mã có chữ cái / đoạn rỗng / dấu chấm ở biên đều bị CHẶN', async () => {
    for (const bad of ['1.a', '4..2', '.4.2', '4.2.']) {
      await boot(ROW);
      setValue('#cfCode', bad);
      expect(save()).withContext(bad).toBeNull();
      expect(errorOf('cfCodeError')).withContext(bad).not.toBeNull();
    }
  });

  it('gom HẾT lỗi trong một lần bấm, không dừng ở lỗi đầu tiên', async () => {
    await boot(null);
    // Mỗi ô có chỗ hiện lỗi riêng, nên báo một lỗi mỗi lần bấm là bắt người dùng sửa - bấm - sửa
    // nhiều vòng không cần thiết.
    expect(save()).toBeNull();
    expect(errorOf('cfCodeError')).not.toBeNull();
    expect(errorOf('cfNameError')).not.toBeNull();
    expect(errorOf('cfGroupIdError')).not.toBeNull();
    expect(errorOf('cfMaxScoreError')).not.toBeNull();
  });

  it('🛑 ô SỐ để trống ⇒ `null`, KHÔNG phải `0` — "chưa ai chấm" khác "được 0 điểm"', async () => {
    await boot(ROW);
    setValue('#cfSelfScore', '');
    setValue('#cfVerifiedScore', '');

    const value = save();
    expect(value?.SelfScore).toBeNull();
    expect(value?.VerifiedScore).toBeNull();
  });

  it('🛑 `Trạng thái` / `Phụ trách` để trống ⇒ `null`, KHÔNG gửi chuỗi rỗng', async () => {
    await boot(ROW);
    const status = host().querySelector('#cfStatus') as HTMLSelectElement;
    status.value = '';
    status.dispatchEvent(new Event('change'));
    const owner = host().querySelector('#cfOwner') as HTMLSelectElement;
    owner.value = '';
    owner.dispatchEvent(new Event('change'));

    const value = save();
    // `""` không thuộc 4 giá trị Q4 ⇒ `400 CRITERIA.STATUS_INVALID`; `ownerId: ""` không phải GUID.
    expect(value?.Status).toBeNull();
    expect(value?.OwnerId).toBeNull();
  });

  it('🛑 `Chênh lệch` và `Tiến độ %` KHÔNG có mặt trên form (Q25 + Q9)', async () => {
    await boot(ROW);
    // `Chênh lệch` là trường TÍNH; `Tiến độ %` sửa inline. Thêm một trong hai vào đây là dựng
    // nguồn nhập liệu thứ hai cho một con số đã có chủ.
    expect(host().querySelector('#cfDiff')).toBeNull();
    expect(host().querySelector('#cfProgress')).toBeNull();
  });

  it('số ra ô nhập là số THÔ, không định dạng locale — `Number()` phải đọc lại được', async () => {
    await boot(ROW);
    // `7,04` (dấu phẩy theo locale vi) sẽ được `Number()` đọc thành `NaN` lúc gửi.
    expect((host().querySelector('#cfSelfScore') as HTMLInputElement).value).toBe('7.04');
  });

  it('tải danh sách `Phụ trách` HỎNG ⇒ ô `disabled` + câu giải thích, KHÔNG chặn lưu', async () => {
    await boot(ROW);
    fixture.componentRef.setInput('ownersFailed', true);
    fixture.detectChanges();

    expect((host().querySelector('#cfOwner') as HTMLSelectElement).disabled).toBeTrue();
    expect(save()).withContext('bốn trường danh mục vẫn lưu được').not.toBeNull();
  });
});
