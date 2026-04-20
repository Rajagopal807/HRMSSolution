USE [HRMS_DB]
GO
-- =============================================
-- Author:		Rajagopal
-- Create date: 15-Apr-2026
-- Description:	Procedure to create Muster for employees
-- =============================================
CREATE OR ALTER PROCEDURE  [dbo].[p_DetermineAttendanceDate]
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH BaseData AS
    (
        SELECT 
            EmpId,
            TransTime,
            IOFlag,
            PunchedTime,
            CAST(TransTime AS DATE) AS TAttnDate,
            DATEADD(HOUR, 5, DATEADD(MINUTE, 30, CAST(CAST(TransTime AS DATE) AS DATETIME))) AS CutOffTime
        FROM TblDailyTransactions
        WHERE AttendanceDate IS NULL
    ),

    LastAttendance AS
    (
        SELECT 
            EmpId,
            MAX(AttendanceDate) AS LastAttendDate
        FROM TblDailyTransactions
        WHERE AttendanceDate IS NOT NULL
        GROUP BY EmpId
    ),

    MinMaxTime AS
    (
        SELECT 
            t.EmpId,
            t.AttendanceDate,
            MIN(t.TransTime) AS MinTime,
            MAX(t.TransTime) AS MaxTime
        FROM TblDailyTransactions t
        WHERE t.AttendanceDate IS NOT NULL
        GROUP BY t.EmpId, t.AttendanceDate
    ),

    FinalCalc AS
    (
        SELECT 
            b.*,
            la.LastAttendDate,
            mm.MinTime,
            mm.MaxTime,

            CASE 
                -- Rule 1: Before cutoff → previous day
                WHEN b.TransTime <= b.CutOffTime 
                    THEN DATEADD(DAY, -1, b.TAttnDate)

                -- Rule 2: IOFlag = 'O' special logic
                WHEN b.IOFlag = 'O'
                     AND la.LastAttendDate IS NOT NULL
                     AND DATEDIFF(HOUR, mm.MinTime, b.TransTime) BETWEEN 0 AND 16
                     AND DATEDIFF(DAY, mm.MinTime, b.TransTime) = 1
                    THEN DATEADD(DAY, -1, b.TAttnDate)

                -- Rule 3: Default
                ELSE b.TAttnDate
            END AS DetAttnDate
        FROM BaseData b
        LEFT JOIN LastAttendance la 
            ON b.EmpId = la.EmpId
        LEFT JOIN MinMaxTime mm 
            ON b.EmpId = mm.EmpId 
           AND mm.AttendanceDate = la.LastAttendDate
    )

    UPDATE t
    SET AttendanceDate = f.DetAttnDate
    FROM TblDailyTransactions t
    INNER JOIN FinalCalc f
        ON t.EmpId = f.EmpId
       AND t.TransTime = f.TransTime
       AND t.IOFlag = f.IOFlag
       AND t.AttendanceDate IS NULL;

END