USE [HRMS_DB]
GO
-- =============================================
-- Author:		Rajagopal
-- Create date: 04-Apr-2026
-- Description:	Procedure to create Muster for employees
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[UpdateHolidays]
    @StrDate DATETIME,
    @UpdateHolidays BIT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        DECLARE 
            @DtFrom DATE,
            @DtTo DATE;

        --------------------------------------------------
        -- Get Month Start & End
        --------------------------------------------------
        SET @DtFrom = DATEFROMPARTS(YEAR(@StrDate), MONTH(@StrDate), 1);
        SET @DtTo   = EOMONTH(@StrDate);

        --------------------------------------------------
        -- Check if Holidays Exist
        --------------------------------------------------
        IF NOT EXISTS (
            SELECT 1 
            FROM TblHolidays 
            WHERE Holiday BETWEEN @DtFrom AND @DtTo
              AND IsActive = 1
              AND IsDeleted = 0
        )
        BEGIN
            SET @UpdateHolidays = 0;
            RETURN;
        END

        --------------------------------------------------
        -- Update Muster for Holidays
        --------------------------------------------------
        UPDATE M
        SET AttId = 'HH'
        FROM TblMuster M
        INNER JOIN TblHolidays H 
            ON M.TDate = H.Holiday
        WHERE H.Holiday BETWEEN @DtFrom AND @DtTo
          AND H.IsActive = 1
          AND H.IsDeleted = 0;

        --------------------------------------------------
        -- Success
        --------------------------------------------------
        SET @UpdateHolidays = 1;
    END TRY

    BEGIN CATCH
        SET @UpdateHolidays = 0;

        DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
        PRINT 'Error in UpdateHolidays: ' + @ErrMsg;
    END CATCH
END
