import { ICriteriaGridDto, ICriteriaRowDto } from '../models/danh-muc-dti.model';
import {
  mapCriteriaGridDto,
  mapCriteriaRowDto,
  mapCriteriaWriteAccess,
  mapDeleteCriteriaResultDto,
  mapOwnerOptionDto,
  toCriteriaWriteBody,
  toInlineAssessmentBody,
} from './danh-muc-dti.mapper';

/** Dòng ĐẦY ĐỦ — mọi khoá nullable đều có mặt. Đối chứng cho ca "vắng mặt" ngay dưới. */
const FULL_ROW: ICriteriaRowDto = {
  criteriaId: 'c1',
  code: '1.1',
  name: 'Tỷ lệ hồ sơ trực tuyến',
  groupId: 'g1',
  groupCode: '1',
  groupName: 'Hạ tầng và Nền tảng số',
  maxScore: 10,
  assessmentId: 'a1',
  assessmentDate: '2026-08-12',
  progressPercent: 70.4,
  selfScore: 7.04,
  verifiedScore: 10,
  diff: 2.96,
  status: 'Đang thực hiện',
  ownerId: 'u1',
  ownerName: 'Nguyễn Văn A',
  deadline: '2026-08-31',
  note: 'CV 123/UBND',
  version: 'v1',
  assessmentPeriod: '2026-W33',
  assessmentPeriodLabel: 'Tuần 33/2026 (10/08 – 16/08/2026)',
};

/**
 * Dòng của một chỉ tiêu **chưa có bản ghi đánh giá nào** — đúng hình dạng BE gửi sau khi import,
 * tức khoá nullable **biến mất khỏi JSON** chứ không mang `null` (§0 của card).
 *
 * Ép kiểu qua `unknown` là cố ý: TypeScript cho phép bỏ trường optional, nhưng viết ra dạng thô
 * này để người đọc thấy đúng thứ đi trên dây.
 */
const BARE_ROW = {
  criteriaId: 'c2',
  code: '1.2',
  name: 'Chỉ tiêu chưa có đánh giá',
  groupId: 'g1',
  groupCode: '1',
  groupName: 'Hạ tầng và Nền tảng số',
  maxScore: 5,
} as unknown as ICriteriaRowDto;

describe('danh-muc-dti.mapper', () => {
  it('đổi casing camelCase → PascalCase cho MỌI trường của một dòng đầy đủ', () => {
    const row = mapCriteriaRowDto(FULL_ROW);

    expect(row.CriteriaId).toBe('c1');
    expect(row.GroupCode).toBe('1');
    expect(row.ProgressPercent).toBe(70.4);
    expect(row.Diff).toBe(2.96);
    expect(row.AssessmentPeriodLabel).toBe('Tuần 33/2026 (10/08 – 16/08/2026)');
    expect(row.Version).toBe('v1');
  });

  /**
   * 🛑 Đây là test quan trọng nhất của file. Luật `WhenWritingNull` không làm đỏ thứ gì lúc biên
   * dịch, và một model nói `number | null` mà thực tế mang `undefined` chỉ lộ ra khi có người viết
   * `=== null` — rồi nhánh đó im lặng không chạy.
   */
  it('🛑 khoá VẮNG MẶT đọc y như `null` — mọi trường nullable phải là `null`, không `undefined`', () => {
    const row = mapCriteriaRowDto(BARE_ROW);

    const nullable = [
      row.AssessmentId,
      row.AssessmentDate,
      row.ProgressPercent,
      row.SelfScore,
      row.VerifiedScore,
      row.Diff,
      row.Status,
      row.OwnerId,
      row.OwnerName,
      row.Deadline,
      row.Note,
      row.Version,
      row.AssessmentPeriod,
      row.AssessmentPeriodLabel,
    ];

    for (const value of nullable) {
      expect(value).withContext('còn `undefined` ⇒ mapper chưa chuẩn hoá khoá vắng mặt').toBeNull();
    }
    // Trường không nullable vẫn phải đi qua nguyên vẹn.
    expect(row.MaxScore).toBe(5);
  });

  it('`null` tường minh và khoá vắng mặt cho ra CÙNG một kết quả', () => {
    const explicitNull = mapCriteriaRowDto({ ...BARE_ROW, selfScore: null, diff: null });

    expect(explicitNull.SelfScore).toBeNull();
    expect(explicitNull.Diff).toBeNull();
  });

  it('`diff` giữ NGUYÊN DẤU — âm là thông tin, không lấy trị tuyệt đối, không đảo chiều (Q25)', () => {
    expect(mapCriteriaRowDto({ ...FULL_ROW, selfScore: 5, verifiedScore: 0, diff: -5 }).Diff).toBe(-5);
  });

  /**
   * Hai ca của bảng "Khoá nào VẮNG MẶT khỏi JSON" (`doc/contracts/danh-muc-dti.md`, thêm
   * 2026-09-10). Cả hai chống đúng một thói quen: **suy sự tồn tại của khoá này từ khoá kia**.
   */
  it('🛑 CÓ bản ghi đánh giá nhưng thiếu một điểm ⇒ `diff` vắng, các khoá khác vẫn có', () => {
    const row = mapCriteriaRowDto({ ...FULL_ROW, verifiedScore: undefined, diff: undefined });

    expect(row.SelfScore).toBe(7.04);
    expect(row.VerifiedScore).toBeNull();
    expect(row.Diff).withContext('có `selfScore` KHÔNG suy ra được `diff` có mặt').toBeNull();
    expect(row.Status).toBe('Đang thực hiện');
  });

  it('🛑 có `ownerId` mà `ownerName` vắng là ca HỢP LỆ — không tra được `AppUser.FullName`', () => {
    const row = mapCriteriaRowDto({ ...FULL_ROW, ownerName: undefined });

    expect(row.OwnerId).toBe('u1');
    expect(row.OwnerName).toBeNull();
  });

  it('`diff = 0` KHÁC `diff` vắng mặt', () => {
    expect(mapCriteriaRowDto({ ...BARE_ROW, diff: 0 }).Diff).toBe(0);
    expect(mapCriteriaRowDto(BARE_ROW).Diff).toBeNull();
  });

  describe('khối quyền ghi cấp màn (§7.1a)', () => {
    const base: ICriteriaGridDto = {
      items: [FULL_ROW],
      page: 1,
      pageSize: 10,
      totalCount: 62,
      canWrite: true,
      isEditable: true,
      editBlockedBy: [],
      isCurrentYear: true,
      currentPeriod: '2026-W33',
      currentPeriodLabel: 'Tuần 33/2026 (10/08 – 16/08/2026)',
    };

    it('ghi được: mảng rỗng, không phải null', () => {
      const access = mapCriteriaWriteAccess(base);

      expect(access.CanWrite).toBeTrue();
      expect(access.IsEditable).toBeTrue();
      expect(access.EditBlockedBy).toEqual([]);
    });

    it('🛑 BE trả `editBlockedBy: null` vẫn phải ra MẢNG — nếu không, mọi `.includes()` ném lỗi', () => {
      const access = mapCriteriaWriteAccess({
        ...base,
        editBlockedBy: null as unknown as string[],
      });

      expect(access.EditBlockedBy).toEqual([]);
    });

    it('thiếu quyền: đúng MỘT mã, và mã đó KHÔNG sinh dòng băng nào (Q51)', () => {
      const access = mapCriteriaWriteAccess({
        ...base,
        canWrite: false,
        isEditable: false,
        editBlockedBy: ['NO_WRITE_PERMISSION'],
      });

      expect(access.CanWrite).toBeFalse();
      expect(access.EditBlockedBy).toEqual(['NO_WRITE_PERMISSION']);
    });

    it('cờ boolean thiếu ⇒ falsy, tức ẩn nút — an toàn mặc định, không đoán ngược lại', () => {
      const access = mapCriteriaWriteAccess({
        items: [],
        page: 1,
        pageSize: 10,
        totalCount: 0,
      } as unknown as ICriteriaGridDto);

      expect(access.CanWrite).toBeFalse();
      expect(access.IsEditable).toBeFalse();
      expect(access.IsCurrentYear).toBeFalse();
    });

    /**
     * Q72 — hai trường này mô tả **lịch**, không mô tả quyền, nên chúng có mặt ở CẢ hai ca mà một
     * cờ quyền sẽ vắng: lưới rỗng, và tài khoản không có quyền ghi. Đó chính là nghiệm thu ghi ở
     * card, viết lại thành hai `it` để máy giữ.
     */
    it('🛑 Q72 — `currentPeriod`/`currentPeriodLabel` có mặt kể cả khi lưới RỖNG', () => {
      const grid = mapCriteriaGridDto({ ...base, items: [], totalCount: 0 });

      expect(grid.Access.CurrentPeriod).toBe('2026-W33');
      expect(grid.Access.CurrentPeriodLabel).toBe('Tuần 33/2026 (10/08 – 16/08/2026)');
    });

    it('🛑 Q72 — vẫn có mặt khi `canWrite = false` (chúng mô tả lịch, không mô tả quyền)', () => {
      const access = mapCriteriaWriteAccess({
        ...base,
        canWrite: false,
        isEditable: false,
        editBlockedBy: ['NO_WRITE_PERMISSION'],
      });

      expect(access.CurrentPeriod).toBe('2026-W33');
      expect(access.CurrentPeriodLabel).toBe('Tuần 33/2026 (10/08 – 16/08/2026)');
    });

    it('lưới RỖNG vẫn mang đủ khối quyền — đó chính là lý do nó ở cấp màn (ca T9)', () => {
      const grid = mapCriteriaGridDto({ ...base, items: [], totalCount: 0 });

      expect(grid.Items).toEqual([]);
      expect(grid.Access.CanWrite).withContext('nút Import phải quyết được ẩn/hiện khi lưới rỗng').toBeTrue();
    });
  });
});

// ===== Chiều NGƯỢC: model app → dây. Casing đổi ở ĐÚNG file này, không ở service, không ở trang. =====

describe('toCriteriaWriteBody (DM-3 + DM-4)', () => {
  const BASE = { Code: '1.1', Name: 'A', GroupId: 'g1', MaxScore: 10 } as const;

  it('4 trường danh mục ra dây dạng camelCase', () => {
    expect(toCriteriaWriteBody(BASE)).toEqual({ code: '1.1', name: 'A', groupId: 'g1', maxScore: 10 });
  });

  it('🛑 `Assessment` VẮNG MẶT ⇒ body KHÔNG có khoá `assessment` — khác hẳn "assessment rỗng"', () => {
    // Vắng mặt = "không đụng tới dữ liệu đánh giá"; có mặt với mọi trường `null` = "xoá trắng".
    // Dựng sẵn một object rỗng cho gọn là chọn hộ vế thứ hai cho mọi lần lưu.
    expect(Object.keys(toCriteriaWriteBody(BASE))).not.toContain('assessment');
  });

  it('🛑 trường `null` của `assessment` VẪN ra dây — `PUT` ghi đè trọn gói', () => {
    const body = toCriteriaWriteBody({
      ...BASE,
      Assessment: {
        Period: '2026-W33',
        SelfScore: null,
        VerifiedScore: null,
        Status: null,
        OwnerId: null,
        Deadline: null,
        Note: null,
        Version: null,
      },
    });
    const assessment = body['assessment'] as Record<string, unknown>;

    // Lọc `null` ra khỏi payload "cho gọn" sẽ biến "xoá trắng ô này" thành "giữ nguyên" — hộp
    // thoại là bộ soạn TRỌN GÓI, người dùng nhìn thấy cả sáu ô.
    // `version` KHÔNG có mặt ở đây vì nó `null`: Q74 làm `null` khác hẳn vắng mặt, và card chốt
    // "vắng mặt = không kiểm concurrency".
    expect(Object.keys(assessment).sort()).toEqual([
      'deadline',
      'note',
      'ownerId',
      'period',
      'selfScore',
      'status',
      'verifiedScore',
    ]);
    expect(assessment['selfScore']).toBeNull();
  });

  it('`year` chỉ ra dây khi CÓ — nó vô nghĩa với một kỳ tuần cụ thể', () => {
    const withYear = toCriteriaWriteBody({
      ...BASE,
      Assessment: {
        Period: 'all',
        Year: 2026,
        SelfScore: 1,
        VerifiedScore: null,
        Status: null,
        OwnerId: null,
        Deadline: null,
        Note: null,
        Version: null,
      },
    });
    expect((withYear['assessment'] as Record<string, unknown>)['year']).toBe(2026);
  });
});

/**
 * Q74 (2026-09-11) — BE phân biệt **khoá vắng** với **khoá mang `null`** bằng `Assigned<T>`.
 *
 * 🛑 Đây là loại ràng buộc KHÔNG có gì bắt được lúc biên dịch: `{ note: undefined }` và
 * `{ }` là hai object khác nhau với `JSON.stringify`, mà TypeScript coi như một. Nên phép kiểm
 * phải soi **danh sách khoá thật sự ra dây**, không soi giá trị.
 */
describe('toInlineAssessmentBody (DM-6 + Q74)', () => {
  it('🛑 chỉ sửa `Tiến độ %` ⇒ body KHÔNG có khoá `note` (giữ nguyên ghi chú)', () => {
    const body = toInlineAssessmentBody({ Period: '2026-W33', ProgressPercent: 70, Version: 'v1' });

    // Gửi kèm `note` cũ là ghi đè mù thứ người khác vừa đổi trong lúc ô sửa đang mở.
    expect(Object.keys(body).sort()).toEqual(['period', 'progressPercent', 'version']);
  });

  it('🛑 chỉ sửa `Ghi chú` ⇒ body KHÔNG có khoá `progressPercent`', () => {
    const body = toInlineAssessmentBody({ Period: '2026-W33', Note: 'CV 123', Version: 'v1' });
    expect(Object.keys(body).sort()).toEqual(['note', 'period', 'version']);
  });

  it('🛑 `null` VẪN ra dây — đó là lệnh XOÁ TRẮNG, khác hẳn vắng mặt', () => {
    const body = toInlineAssessmentBody({ Period: '2026-W33', Note: null, Version: 'v1' });

    expect('note' in body).withContext('xoá trắng phải NÓI RA, không phải im lặng').toBeTrue();
    expect(body['note']).toBeNull();
  });

  it('🛑 `Version` vắng/`null` ⇒ KHÔNG gửi khoá — "vắng mặt = không kiểm concurrency"', () => {
    // Một chỉ tiêu chưa có bản ghi đánh giá không mang `Version` nào; gửi `null` cho nó là nói
    // một câu về concurrency mà ta không có ý nói.
    expect('version' in toInlineAssessmentBody({ Period: '2026-W33', ProgressPercent: 0, Version: null })).toBeFalse();
    expect('version' in toInlineAssessmentBody({ Period: '2026-W33', ProgressPercent: 0 })).toBeFalse();
  });
});

describe('mapDeleteCriteriaResultDto (DM-5)', () => {
  it('đọc `hardDeleted` của BE — FE KHÔNG tự đoán cứng hay mềm', () => {
    expect(mapDeleteCriteriaResultDto({ hardDeleted: true }).HardDeleted).toBeTrue();
    expect(mapDeleteCriteriaResultDto({ hardDeleted: false }).HardDeleted).toBeFalse();
  });
});

describe('mapOwnerOptionDto (ô `Phụ trách`)', () => {
  it('🛑 option KHÔNG BAO GIỜ rỗng — thiếu `fullName` thì lùi về `userName`, rồi tới `id`', () => {
    // Một dòng trắng trong `<select>` vẫn chọn được, và người chọn không biết mình gán việc cho ai.
    expect(mapOwnerOptionDto({ id: 'u1', fullName: 'Nguyễn Văn A' }).Name).toBe('Nguyễn Văn A');
    expect(mapOwnerOptionDto({ id: 'u1', userName: 'a.nguyen' }).Name).toBe('a.nguyen');
    expect(mapOwnerOptionDto({ id: 'u1' }).Name).toBe('u1');
  });
});
