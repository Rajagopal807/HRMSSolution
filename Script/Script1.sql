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
    DATEADD(YEAR, 5, [PunchedTime]),
    DATEADD(YEAR, 5, [TransTime]),
    DATEADD(YEAR, 5, [AttendanceDate]),
    [BOutPassHrs],
    [Remarks],
    [BadgeReaderNo],
    [Deleted],
    [ReasonCode],
    GETDATE(),
    0
FROM [stars].[dbo].[TblDailyTransactions]
WHERE EmpId IN ('00000000017', '00000000024')
  AND AttendanceDate >= '2021-01-01'
  AND AttendanceDate <  '2022-01-01';

UPDATE [HRMS_DB].[dbo].TblDailyTransactions set empid = '00000012134' WHERE empid='00000000024'
UPDATE [HRMS_DB].[dbo].TblDailyTransactions set empid = '00000011255' WHERE empid='00000000017'

UPDATE [HRMS_DB].[dbo].TblDailyTransactions
SET TransTime = DATEADD(MONTH, 2, TransTime)
WHERE EmpId IN ('00000011255', '00000012134');

UPDATE [HRMS_DB].[dbo].TblDailyTransactions
SET PunchedTime = DATEADD(MONTH, 2, PunchedTime)
WHERE EmpId IN ('00000011255', '00000012134');

UPDATE [HRMS_DB].[dbo].TblDailyTransactions
SET AttendanceDate = DATEADD(MONTH, 2, AttendanceDate)
WHERE EmpId IN ('00000011255', '00000012134');

---------------Delete Below---------------------------
SELECt * FROM TblDailyTransactions
SELECT * FROM TblMuster