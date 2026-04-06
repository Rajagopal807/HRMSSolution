USE [HRMS_DB]
GO
-- =============================================
-- Author:		Rajagopal
-- Create date: 04-Apr-2026
-- Description:	Procedure to create Muster for employees
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[p_DetermineShift]
(
    @EmpCode   VARCHAR(11),
    @TransDate DATETIME,
    @GroupCode VARCHAR(2),
    @Shift     VARCHAR(2) OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE 
        @Tdate        DATE,
        @FirstInTime  DATETIME,
        @LastOutTime  DATETIME,
        @DayCode      INT;

    BEGIN TRY
        -- ============================
        -- INITIAL SETUP
        -- ============================
        SET @Tdate = CAST(@TransDate AS DATE);
        SET @DayCode = DATEPART(WEEKDAY, @Tdate);
        SET @Shift = LTRIM(RTRIM(@GroupCode));

        -- ============================
        -- GET FIRST IN & LAST OUT
        -- ============================
        SELECT 
            @FirstInTime = MIN(TransTime),
            @LastOutTime = MAX(TransTime)
        FROM TblDailyTransactions WITH (NOLOCK)
        WHERE EmpId = @EmpCode
          AND AttendanceDate = @Tdate
          AND Deleted = 'F';

        -- If no records
        IF @FirstInTime IS NULL
            RETURN;

        -- ============================
        -- SHIFT MATCHING
        -- ============================
        ;WITH ShiftCTE AS
        (
            SELECT 
                ShiftId,
                StartTime,
                EndTime,
                DATEADD(HOUR, -1, DATEADD(DAY, DATEDIFF(DAY, 0, @Tdate), StartTime)) AS StTime,
                DATEADD(HOUR,  2, DATEADD(DAY, DATEDIFF(DAY, 0, @Tdate), StartTime)) AS EndTimeWindow,
                DATEADD(DAY, DATEDIFF(DAY, 0, @Tdate), StartTime) AS ActualStart,
                DATEADD(DAY, DATEDIFF(DAY, 0, @Tdate), EndTime)   AS ActualEnd
            FROM TblShiftDetails WITH (NOLOCK)
            WHERE LEFT(LTRIM(ShiftId),1) = LTRIM(@GroupCode)
              AND DayId = @DayCode
              AND RIGHT(RTRIM(ShiftId),1) <> 'G'
        )
        SELECT TOP 1 @Shift = ShiftId
        FROM ShiftCTE
        WHERE @FirstInTime BETWEEN StTime AND EndTimeWindow
        ORDER BY ShiftId;

    END TRY
    BEGIN CATCH
        -- fallback
        SET @Shift = LTRIM(RTRIM(@GroupCode));
    END CATCH
END
GO