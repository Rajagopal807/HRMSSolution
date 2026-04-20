USE [HRMS_DB]
GO
-- =============================================
-- Author:		Rajagopal
-- Create date: 04-Apr-2026
-- Description:	Procedure to create Muster for employees
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[WriteErrToFile]
    @Filenum SMALLINT,
    @Empcode VARCHAR(11),
    @Tdate DATETIME,
    @message VARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        INSERT INTO ErrorLog (FileNum, EmpCode, LogDate, Message)
        VALUES (@Filenum, @Empcode, @Tdate, @message);
    END TRY
    BEGIN CATCH
        -- fallback (at least print if insert fails)
        PRINT 'Logging Failed: ' + ERROR_MESSAGE();
    END CATCH
END