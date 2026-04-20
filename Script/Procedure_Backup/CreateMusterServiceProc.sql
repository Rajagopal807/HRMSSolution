USE [HRMS_DB]
GO
-- =============================================
-- Author:		Rajagopal
-- Create date: 04-Apr-2026
-- Description:	Procedure to create Muster for employees
-- =============================================
CREATE OR ALTER  PROCEDURE [dbo].[CreateMusterServiceProc]
    @fromDate DATETIME, 
    @empid VARCHAR(11)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE 
        @MFDate VARCHAR(11),
        @CreateMusterFor BIT,
        @mEmpid VARCHAR(11);

    SET @MFDate = CONVERT(VARCHAR(10), @fromDate, 101);

    BEGIN TRY
        BEGIN TRAN;

        -- 🔥 CASE 1: All Employees
        IF @empid = ''
        BEGIN
            DECLARE emp_fetch CURSOR FOR 
            SELECT EmployeeID 
            FROM Tblempmast 
            WHERE IsActive = 1;

            OPEN emp_fetch;
            FETCH NEXT FROM emp_fetch INTO @mEmpid;

            WHILE @@FETCH_STATUS = 0
            BEGIN
                -- ✅ Delete existing data
                DELETE FROM TblMuster
                WHERE EmployeeId = @mEmpid
                  AND TDate >= @fromDate;

                -- ✅ Recreate
                EXEC CreateMusterFor @mEmpid, @MFDate, 1, @CreateMusterFor OUTPUT;

                FETCH NEXT FROM emp_fetch INTO @mEmpid;
            END

            CLOSE emp_fetch;
            DEALLOCATE emp_fetch;
        END
        ELSE
        BEGIN
            -- ✅ Delete existing data
            DELETE FROM TblMuster
            WHERE EmployeeId = @empid
              AND TDate >= @fromDate;

            -- ✅ Recreate
            EXEC CreateMusterFor @empid, @MFDate, 1, @CreateMusterFor OUTPUT;
        END

        COMMIT TRAN;
    END TRY

    BEGIN CATCH
        -- ❌ Rollback if error
        IF @@TRANCOUNT > 0
            ROLLBACK TRAN;

        -- 🔥 Error details
        DECLARE @ErrorMessage NVARCHAR(4000),
                @ErrorSeverity INT,
                @ErrorState INT;

        SELECT 
            @ErrorMessage = ERROR_MESSAGE(),
            @ErrorSeverity = ERROR_SEVERITY(),
            @ErrorState = ERROR_STATE();

        -- Optional: Log error
        PRINT 'Error: ' + @ErrorMessage;

        RAISERROR (@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END

--EXEC CreateMusterServiceProc '2026-03-01',''