USE [HRMS_DB]
GO
-- =============================================
-- Author:		Rajagopal
-- Create date: 04-Apr-2026
-- Description:	Procedure to create Muster for employees
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[p_Late_In]
(
    @par_EmpId    VARCHAR(11),
    @par_TDate    DATETIME,
    @par_ShiftId  VARCHAR(2),
    @par_late_in  INT OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE 
        @ShiftStart   DATETIME,
        @StartTime    TIME,
        @Time1        DATETIME,
        @SftGraceIn   INT = 0,
        @LateBy       INT = 0;

    BEGIN TRY
        SET @par_late_in = 0;

        -- ============================
        -- SHIFT RESOLUTION
        -- ============================
        IF UPPER(@par_ShiftId) <> 'WW'
        BEGIN
            IF LEN(LTRIM(RTRIM(@par_ShiftId))) = 1
            BEGIN
                EXEC dbo.p_DetermineShift 
                    @par_EmpId, @par_TDate, @par_ShiftId, @par_ShiftId OUTPUT;
            END

            -- ============================
            -- GET SHIFT START TIME
            -- ============================
            SELECT @StartTime = StartTime
            FROM tblShiftDetails WITH (NOLOCK)
            WHERE ShiftId = @par_ShiftId
              AND DayId = DATEPART(WEEKDAY, @par_TDate);

            IF @StartTime IS NULL RETURN;

            -- Combine date + time properly
            SET @ShiftStart = DATEADD(DAY, DATEDIFF(DAY, 0, @par_TDate), CAST(@StartTime AS DATETIME));

            -- Midnight fix
            IF DATEPART(HOUR, @ShiftStart) = 0 AND DATEPART(MINUTE, @ShiftStart) = 0
                SET @ShiftStart = DATEADD(DAY, 1, @ShiftStart);

            -- ============================
            -- GET GRACE TIME
            -- ============================
            SELECT @SftGraceIn = 0;

            -- ============================
            -- GET FIRST IN TIME (OPTIMIZED)
            -- ============================
            SELECT @Time1 = MIN(TransTime)
            FROM tblDailyTransactions WITH (NOLOCK)
            WHERE EmpId = @par_EmpId
              AND AttendanceDate = CAST(@par_TDate AS DATE)
              AND Deleted = 'F';

            IF @Time1 IS NULL RETURN;

            -- ============================
            -- CALCULATE LATE
            -- ============================
            SET @par_late_in = DATEDIFF(MINUTE, @ShiftStart, @Time1);

            IF @par_late_in < 0 OR @par_late_in < @SftGraceIn
                SET @par_late_in = 0;

            -- ============================
            -- BUS LATE ADJUSTMENT
            -- ============================
            --SELECT TOP 1 @LateBy = LateBy
            --FROM tblBusLateEntries B
            --INNER JOIN tblEmpMast E ON B.BusRtId = E.BusRtId
            --WHERE E.EmpId = @par_EmpId
            --  AND B.ShiftId = @par_ShiftId
            --  AND B.TDate = CAST(@par_TDate AS DATE);

            IF @LateBy IS NOT NULL
                SET @par_late_in = @par_late_in - @LateBy;

            IF @par_late_in < @SftGraceIn OR @par_late_in < 0
                SET @par_late_in = 0;
        END
    END TRY
    BEGIN CATCH
        SET @par_late_in = 0;
    END CATCH
END
GO