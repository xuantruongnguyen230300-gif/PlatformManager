import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { IApiResult } from '../../../core/http/api-result.model';
import { IImportJobStatus, IImportJobStatusDto } from '../models/danh-muc-dti.model';
import { DanhMucDtiImportService } from './danh-muc-dti-import.service';

function ok<T>(data: T): IApiResult<T> {
  return {
    data,
    message: null,
    status: 'SUCCESS',
    code: 'Success',
    businessCode: null,
    traceId: 'trace-import',
    retryable: null,
    fields: null,
  };
}

function csv(name = 'dti.csv'): File {
  return new File(['Mã,Tên\n1.1,A\n'], name, { type: 'text/csv' });
}

describe('DanhMucDtiImportService (CONTRACT DM-7)', () => {
  let service: DanhMucDtiImportService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection(), provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(DanhMucDtiImportService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('startImport(): POST /import dạng multipart, field tên `file` + `period`', () => {
    let jobId: string | undefined;
    service.startImport(csv(), { Period: '2026-W33' }).subscribe((value) => (jobId = value));

    const req = httpMock.expectOne('/import');
    expect(req.request.method).toBe('POST');

    const body = req.request.body as FormData;
    expect(body instanceof FormData).withContext('phải là multipart, không phải JSON').toBeTrue();
    expect((body.get('file') as File).name).toBe('dti.csv');
    expect(body.get('period')).toBe('2026-W33');

    req.flush(ok({ jobId: 'job-1' }));
    expect(jobId).toBe('job-1');
  });

  it('🛑 KHÔNG tự đặt Content-Type — `boundary` do trình duyệt sinh, đặt tay là mất nó', () => {
    service.startImport(csv(), { Period: '2026-W33' }).subscribe();
    const req = httpMock.expectOne('/import');
    expect(req.request.headers.get('Content-Type')).toBeNull();
    req.flush(ok({ jobId: 'job-1' }));
  });

  it('`period = "all"` đi NGUYÊN VĂN ra dây kèm `year` (Q40 + T15) — FE không quy đổi', () => {
    service.startImport(csv(), { Period: 'all', Year: 2026 }).subscribe();

    const req = httpMock.expectOne('/import');
    const body = req.request.body as FormData;
    expect(body.get('period')).withContext('server quy "all" về tuần hiện tại, không phải FE').toBe('all');
    expect(body.get('year')).toBe('2026');
    req.flush(ok({ jobId: 'job-1' }));
  });

  it('kỳ là một TUẦN ⇒ KHÔNG gửi `year` (năm đã nằm trong chuỗi tuần)', () => {
    service.startImport(csv(), { Period: '2026-W33' }).subscribe();

    const req = httpMock.expectOne('/import');
    expect((req.request.body as FormData).get('year')).toBeNull();
    req.flush(ok({ jobId: 'job-1' }));
  });

  it('🛑 KHÔNG đính kèm danh tính người nạp — danh tính lấy từ phiên ở SERVER (Q35)', () => {
    service.startImport(csv(), { Period: '2026-W33' }).subscribe();

    const req = httpMock.expectOne('/import');
    expect([...(req.request.body as FormData).keys()].sort()).toEqual(['file', 'period']);
    req.flush(ok({ jobId: 'job-1' }));
  });

  it('getJobStatus(): GET /import/{id}, `result` map sang PascalCase', () => {
    let status: IImportJobStatus | undefined;
    service.getJobStatus('job-1').subscribe((value) => (status = value));

    const dto: IImportJobStatusDto = {
      status: 'Succeeded',
      result: {
        totalRows: 62,
        successCount: 59,
        errorCount: 3,
        criteriaCreatedCount: 2,
        errors: [{ rowNumber: 17, code: 'IMPORT.ROW_SELF_SCORE_EXCEEDS_MAX', messageParams: { Code: '4.2' } }],
      },
    };
    httpMock.expectOne('/import/job-1').flush(ok(dto));

    expect(status?.Status).toBe('Succeeded');
    expect(status?.Result?.TotalRows).toBe(62);
    expect(status?.Result?.Errors[0].RowNumber).withContext('số dòng TRONG FILE, dòng 1 = header').toBe(17);
    expect(status?.Result?.Errors[0].MessageParams['Code']).toBe('4.2');
  });

  it('🛑 `Pending` KHÔNG mang `result`, và `errorMessage` vắng mặt chứ không `null`', () => {
    let status: IImportJobStatus | undefined;
    service.getJobStatus('job-1').subscribe((value) => (status = value));
    httpMock.expectOne('/import/job-1').flush(ok({ status: 'Pending' }));

    expect(status?.Result).toBeNull();
    expect(status?.ErrorMessage).toBeNull();
  });

  it('🛑 `IMPORT.ROW_CODE_MISSING` VẮNG HẲN khoá `messageParams` — mapper phải chịu được', () => {
    // Đo được ở lần nghiệm thu BE 2026-09-11: không phải `{}`, không phải `null`, mà KHÔNG CÓ
    // KHOÁ. Để `undefined` đi tiếp thì chỗ dựng câu ném lỗi, và cả hộp thoại kết quả trắng.
    let status: IImportJobStatus | undefined;
    service.getJobStatus('job-1').subscribe((value) => (status = value));
    httpMock.expectOne('/import/job-1').flush(
      ok({
        status: 'Succeeded',
        result: {
          totalRows: 8,
          successCount: 2,
          errorCount: 6,
          criteriaCreatedCount: 0,
          errors: [{ rowNumber: 6, code: 'IMPORT.ROW_CODE_MISSING' }],
        },
      }),
    );

    expect(status?.Result?.Errors[0].MessageParams).toEqual({});
  });

  it('🛑 `errorCount` đếm DÒNG, giữ NGUYÊN — không suy từ `errors.length`', () => {
    // Một dòng sai cả hai cột điểm cho ra HAI phần tử cùng `rowNumber` nhưng vẫn là MỘT dòng.
    let status: IImportJobStatus | undefined;
    service.getJobStatus('job-1').subscribe((value) => (status = value));
    httpMock.expectOne('/import/job-1').flush(
      ok({
        status: 'Succeeded',
        result: {
          totalRows: 62,
          successCount: 61,
          errorCount: 1,
          criteriaCreatedCount: 0,
          errors: [
            { rowNumber: 17, code: 'IMPORT.ROW_SELF_SCORE_EXCEEDS_MAX', messageParams: { Code: '4.2' } },
            { rowNumber: 17, code: 'IMPORT.ROW_VERIFIED_SCORE_EXCEEDS_MAX', messageParams: { Code: '4.2' } },
          ],
        },
      }),
    );

    expect(status?.Result?.ErrorCount).withContext('1 DÒNG hỏng, 2 phần tử errors[]').toBe(1);
    expect(status?.Result?.Errors.length).toBe(2);
  });

  it('`Failed` CÓ `errorCode` ⇒ map ra để nơi gọi tra bảng dịch (Q75)', () => {
    let status: IImportJobStatus | undefined;
    service.getJobStatus('job-1').subscribe((value) => (status = value));
    httpMock.expectOne('/import/job-1').flush(
      ok({ status: 'Failed', errorCode: 'IMPORT.FILE_TOO_MANY_ROWS', errorMessage: 'Row limit 20000 exceeded' }),
    );

    expect(status?.ErrorCode).toBe('IMPORT.FILE_TOO_MANY_ROWS');
    expect(status?.Result).withContext('`Failed` ⇒ không dòng nào được ghi, không có kết quả').toBeNull();
  });

  it('`Failed` KHÔNG có `errorCode` vẫn là đường đi hợp lệ — job crash thì không có mã nghiệp vụ', () => {
    let status: IImportJobStatus | undefined;
    service.getJobStatus('job-1').subscribe((value) => (status = value));
    httpMock.expectOne('/import/job-1').flush(ok({ status: 'Failed', errorMessage: 'NullReference' }));

    expect(status?.ErrorCode).toBeNull();
  });

  it('🛑 trạng thái LẠ quy về `Failed`, KHÔNG quy về `Running` — nếu không thì poll vô hạn', () => {
    let status: IImportJobStatus | undefined;
    service.getJobStatus('job-1').subscribe((value) => (status = value));
    httpMock.expectOne('/import/job-1').flush(ok({ status: 'Queued' }));

    expect(status?.Status).toBe('Failed');
  });
});
