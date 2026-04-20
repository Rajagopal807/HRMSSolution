USE [HRMS_DB]
GO
-- =============================================
-- Author:       Rajagopal
-- Create date:  04-Apr-2026
-- Modified:     16-Apr-2026
-- Description:  Procedure to create Muster and compute attendance for employees
-- Changes:
--   1. Skip muster creation if a record already exists for the employee + date
--   2. Call ComputeAttendanceFor after muster creation (or skip) per employee
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[CreateMusterServiceProc]
    @fromDate DATETIME,
    @empid    VARCHAR(11)
AS
BEGIN
    SET NOCOUNT OFF;

    DECLARE
        @MFDate               VARCHAR(11),
        @CreateMusterFor      BIT,
        @ComputeAttendanceFor SMALLINT,
        @mEmpid               VARCHAR(11);

    -- Convert to MM/DD/YYYY format expected by CreateMusterFor and ComputeAttendanceFor
    SET @MFDate = CONVERT(VARCHAR(10), @fromDate, 101);

    BEGIN TRY
        BEGIN TRAN;

        -- =============================================
        -- CASE 1: All active employees
        -- =============================================
        IF @empid = ''
        BEGIN
            DECLARE emp_fetch CURSOR LOCAL FAST_FORWARD FOR
                SELECT EmployeeID
                FROM Tblempmast
                WHERE IsActive = 1;

            OPEN emp_fetch;
            FETCH NEXT FROM emp_fetch INTO @mEmpid;

            WHILE @@FETCH_STATUS = 0
            BEGIN
                -- ✅ Create muster ONLY if it does not already exist for this employee + date
                IF NOT EXISTS (
                    SELECT 1
                    FROM TblMuster WITH (NOLOCK)
                    WHERE EmployeeId = @mEmpid
                      AND TDate     = @fromDate
                )
                BEGIN
                    EXEC CreateMusterFor
                        @EmpCode         = @mEmpid,
                        @TempDate9       = @MFDate,
                        @FileNum         = 1,
                        @CreateMusterFor = @CreateMusterFor OUTPUT;
                END

                -- ✅ Compute attendance for this employee for the given date
                --    (runs whether muster was just created or already existed)
                EXEC ComputeAttendanceFor
                    @EmpCode              = @mEmpid,
                    @TDf5                 = @MFDate,
                    @TDt5                 = @MFDate,
                    @location             = '',
                    @ComputeAttendanceFor = @ComputeAttendanceFor OUTPUT;

                FETCH NEXT FROM emp_fetch INTO @mEmpid;
            END

            CLOSE emp_fetch;
            DEALLOCATE emp_fetch;
        END

        -- =============================================
        -- CASE 2: Single specific employee
        -- =============================================
        ELSE
        BEGIN
            -- ✅ Create muster ONLY if it does not already exist for this employee + date
            IF NOT EXISTS (
                SELECT 1
                FROM TblMuster WITH (NOLOCK)
                WHERE EmployeeId = @empid
                  AND TDate     = @fromDate
            )
            BEGIN
                EXEC CreateMusterFor
                    @EmpCode         = @empid,
                    @TempDate9       = @MFDate,
                    @FileNum         = 1,
                    @CreateMusterFor = @CreateMusterFor OUTPUT;
            END

            -- ✅ Compute attendance for this employee for the given date
            --    (runs whether muster was just created or already existed)
            EXEC ComputeAttendanceFor
                @EmpCode              = @empid,
                @TDf5                 = @MFDate,
                @TDt5                 = @MFDate,
                @location             = '',
                @ComputeAttendanceFor = @ComputeAttendanceFor OUTPUT;
        END

        COMMIT TRAN;
    END TRY

    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRAN;

        DECLARE
            @ErrorMessage  NVARCHAR(4000),
            @ErrorSeverity INT,
            @ErrorState    INT;

        SELECT
            @ErrorMessage  = ERROR_MESSAGE(),
            @ErrorSeverity = ERROR_SEVERITY(),
            @ErrorState    = ERROR_STATE();

        PRINT 'Error: ' + @ErrorMessage;
        RAISERROR (@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END
GO

-- Test examples:
-- All employees for a date:
 --EXEC CreateMusterServiceProc '2026-04-02', ''
--
-- Single employee for a date:
-- EXEC CreateMusterServiceProc '2026-03-01', '00000011255'
