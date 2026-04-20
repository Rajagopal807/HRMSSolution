USE [HRMS_DB]
GO
-- =============================================
-- Author:		Rajagopal
-- Create date: 04-Apr-2026
-- Description:	Procedure to create Muster for employees
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[p_IsPaired]
(
    @empcode   VARCHAR(11),
    @transdate DATETIME,
    @ispaired  INT OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE 
        @punch       CHAR(1) = 'I',
        @temp_punch  CHAR(1);

    -- ============================
    -- VALIDATION
    -- ============================
    IF @empcode IS NULL OR LTRIM(RTRIM(@empcode)) = ''
       OR @transdate IS NULL
    BEGIN
        SET @ispaired = -1;
        RETURN;
    END

    -- ============================
    -- CURSOR (OPTIMIZED)
    -- ============================
    DECLARE ispaired_cur CURSOR LOCAL FAST_FORWARD FOR
    SELECT IOFlag
    FROM TblDailyTransactions
    WHERE Empid = LTRIM(RTRIM(@empcode))
      AND AttendanceDate = @transdate
      AND Deleted = 'F'
    ORDER BY Transtime;

    OPEN ispaired_cur;

    FETCH NEXT FROM ispaired_cur INTO @temp_punch;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF UPPER(@temp_punch) <> @punch
        BEGIN
            SET @ispaired = 1;

            CLOSE ispaired_cur;
            DEALLOCATE ispaired_cur;
            RETURN;
        END

        -- Toggle punch
        SET @punch = CASE 
                        WHEN @punch = 'I' THEN 'O'
                        ELSE 'I'
                     END;

        FETCH NEXT FROM ispaired_cur INTO @temp_punch;
    END

    CLOSE ispaired_cur;
    DEALLOCATE ispaired_cur;

    -- ============================
    -- FINAL RESULT
    -- ============================
    IF @punch = 'O'
        SET @ispaired = 1;
    ELSE
        SET @ispaired = 0;
END
GO