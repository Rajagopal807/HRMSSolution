UPDATE TblDailyTransactions
SET TransTime = DATEADD(MONTH, 2, TransTime)
WHERE EmpId IN ('00000011255', '00000012134');

UPDATE TblDailyTransactions
SET PunchedTime = DATEADD(MONTH, 2, PunchedTime)
WHERE EmpId IN ('00000011255', '00000012134');

UPDATE TblDailyTransactions
SET AttendanceDate = DATEADD(MONTH, 2, AttendanceDate)
WHERE EmpId IN ('00000011255', '00000012134');

SELECT * FROM TblDailytransactions

UPDATE TblDailyTransactions set empid = '00000012134' WHERE empid='00000000024'

UPDATE TblDailyTransactions set punchedTime = transtime

UPDATE TblDailyTransactions Set isDeleted=0