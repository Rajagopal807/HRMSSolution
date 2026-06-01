INSERT INTO [HRMS_DB].[dbo].[TblDailyTransactions]
(
    [EmpId],
    [IOFlag],
    [ActualIOFlag],
    [OPFlag],
    [PunchedTime],
    [TransTime],
    [AttendanceDate],
    [BOutPassHrs],
    [Remarks],
    [BadgeReaderNo],
    [Deleted],
    [ReasonCode],
    [CreatedAt],
    [IsDeleted]
)
SELECT 
    [EmpId],
    [IOFlag],
    [ActualIOFlag],
    [OPFlag],
    DATEADD(MONTH, 3, DATEADD(YEAR, 5, [PunchedTime])),
    DATEADD(MONTH, 3, DATEADD(YEAR, 5, [TransTime])),
    DATEADD(MONTH, 3, DATEADD(YEAR, 5, [AttendanceDate])),
    [BOutPassHrs],
    [Remarks],
    [BadgeReaderNo],
    [Deleted],
    [ReasonCode],
    GETDATE(),
    0
FROM [stars].[dbo].[TblDailyTransactions]
WHERE EmpId IN ('00000000047', '00000000042')
  AND AttendanceDate >= '2020-12-01'
  AND AttendanceDate <  '2020-12-31';  

UPDATE [HRMS_DB].[dbo].TblDailyTransactions set empid = '00000012134' WHERE empid='00000000042'
UPDATE [HRMS_DB].[dbo].TblDailyTransactions set empid = '00000011255' WHERE empid='00000000047'

INSERT INTO TblShifts Values ('AG', 'General Shift');

INSERT INTO TblshiftDetails values ('AG', 1, '1899-12-30 09:00:00.000', '1899-12-30 20:00:00.000', '1899-12-30 13:00:00.000', '1899-12-30 14:00:00.000');
INSERT INTO TblshiftDetails values ('AG', 2, '1899-12-30 09:00:00.000', '1899-12-30 20:00:00.000', '1899-12-30 13:00:00.000', '1899-12-30 14:00:00.000');
INSERT INTO TblshiftDetails values ('AG', 3, '1899-12-30 09:00:00.000', '1899-12-30 20:00:00.000', '1899-12-30 13:00:00.000', '1899-12-30 14:00:00.000');
INSERT INTO TblshiftDetails values ('AG', 4, '1899-12-30 09:00:00.000', '1899-12-30 20:00:00.000', '1899-12-30 13:00:00.000', '1899-12-30 14:00:00.000');
INSERT INTO TblshiftDetails values ('AG', 5, '1899-12-30 09:00:00.000', '1899-12-30 20:00:00.000', '1899-12-30 13:00:00.000', '1899-12-30 14:00:00.000');
INSERT INTO TblshiftDetails values ('AG', 6, '1899-12-30 09:00:00.000', '1899-12-30 20:00:00.000', '1899-12-30 13:00:00.000', '1899-12-30 14:00:00.000');
INSERT INTO TblshiftDetails values ('AG', 7, '1899-12-30 09:00:00.000', '1899-12-30 20:00:00.000', '1899-12-30 13:00:00.000', '1899-12-30 14:00:00.000');

--EXEC CreateMusterServiceProc '2026-03-01',''

--EXEC ComputeAttendanceFor '00000011255','03/01/2026','03/31/2026','STAFF',0

---------------Delete Below---------------------------
SELECt * FROM TblDailyTransactions WHERE AttendanceDate is NULL;
---DELETE FROM TblDailyTransactions WHERE AttendanceDate is NULL;
SELECT * FROM TblMuster WHERE  TDate = '05/01/2026' and EmployeeId='00000BD1861' and Attid='AA';
DELETE FROM TblMuster WHERE  TDate >= '05/01/2026' and Tdate<='05/31/2026' and EmployeeId='00000BD1861'
SELECT * FROM tblErrorDisplay
--DELETE FROM TblMuster WHERE  TDate>='03/01/2026';
TRUNCATE TABLE TblLeaveApplications
    SELECT * FROM    TblLeaveApplications
    WHERE   EmployeeId  = RTRIM(LTRIM('00000011255'))
      AND   FromDate   = CONVERT(DATETIME, '04/27/2026', 101)
      AND   IsDeleted     = 0 ;
SELECT * FROM Tblshifts;
SELECT * FROM TblshiftDetails;

SELECT CAST('03/01/2026' as DATE);

SELECT
    M.*,
    H.Holiday
FROM TblMuster M
LEFT JOIN TblHolidays H
    ON M.TDate = H.Holiday
WHERE M.AttId = 'AA'
  AND M.TDate = '2026-05-01';

            SELECT * 
            FROM TblHolidays 
            WHERE Holiday BETWEEN '05/01/2026' AND '05/31/2026'
              AND IsActive = 1
              AND IsDeleted = 0;

SELECT
    M.TDate,
    H.Holiday,
    M.AttId
FROM TblMuster M
INNER JOIN TblHolidays H
    ON CAST(M.TDate AS DATE) = CAST(H.Holiday AS DATE)
WHERE CAST(H.Holiday AS DATE)
      BETWEEN CAST('05/01/2026' AS DATE)
          AND CAST('05/31/2026' AS DATE)
  AND H.IsActive = 1
  AND H.IsDeleted = 0
ORDER BY H.Holiday;

UPDATE M
SET AttId = 'HH'
FROM TblMuster M
INNER JOIN TblHolidays H 
    ON M.TDate = H.Holiday
WHERE H.Holiday BETWEEN '05/01/2026' AND '05/31/2026'
    AND H.IsActive = 1
    AND H.IsDeleted = 0;

SELECT *
FROM TblMuster M
INNER JOIN TblHolidays H 
    ON M.TDate = H.Holiday
WHERE H.Holiday BETWEEN '05/01/2026' AND '05/31/2026'
    AND H.IsActive = 1
    AND H.IsDeleted = 0;


SELECT TRY_CONVERT(DATE, '03/01/2026', 101)
select * FROM TblEMpmast WHERE EmployeeId='00000BD1861'

select * FROM TblDepartment
select * FROM TblDesignation

SELECT * FROM TblDailyTransactions;

DELETE FROM TBLEMPMAST;
TRUNCATE TABLE TBLDEPARTMENT;