USE [HRMS_DB]
GO
-- =============================================
-- Author:       Rajagopal
-- Create date:  04-Apr-2026
-- Modified:     16-Apr-2026
-- Description:  Procedure to create Muster and compute attendance for employees
-- Changes:
--   1. Skip muster creation if a record already exists for the employee + date
--   2. If muster is newly created  → ComputeAttendanceFor runs for the ENTIRE month
--   3. If muster already existed   → ComputeAttendanceFor runs for the given date only
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[CreateMusterServiceProc]
    @fromDate DATETIME,
    @empid    VARCHAR(11)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE
        @MFDate               VARCHAR(11),
        @MonthStart           VARCHAR(11),
        @MonthEnd             VARCHAR(11),
        @CreateMusterFor      BIT,
        @ComputeAttendanceFor SMALLINT,
        @mEmpid               VARCHAR(11),
        @MusterCreated        BIT;

    -- Date for CreateMusterFor  (MM/DD/YYYY)
    SET @MFDate     = CONVERT(VARCHAR(10), @fromDate, 101);

    -- First day of the month of @fromDate
    SET @MonthStart = CONVERT(VARCHAR(10),
                        DATEFROMPARTS(YEAR(@fromDate), MONTH(@fromDate), 1),
                        101);

    -- Last day of the month of @fromDate
    SET @MonthEnd   = CONVERT(VARCHAR(10),
                        EOMONTH(@fromDate),
                        101);

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
                SET @MusterCreated = 0;

                -- Create muster ONLY if it does not already exist for this employee + date
                IF NOT EXISTS (
                    SELECT 1
                    FROM TblMuster WITH (NOLOCK)
                    WHERE EmployeeId = @mEmpid
                      AND TDate      = @fromDate
                )
                BEGIN
                    EXEC CreateMusterFor
                        @EmpCode         = @mEmpid,
                        @TempDate9       = @MFDate,
                        @FileNum         = 1,
                        @CreateMusterFor = @CreateMusterFor OUTPUT;

                    SET @MusterCreated = ISNULL(@CreateMusterFor, 0);
                END

                -- If muster was newly created → compute for the entire month
                -- If muster already existed   → compute for the given date only
                IF @MusterCreated = 1
                BEGIN
                    EXEC ComputeAttendanceFor
                        @EmpCode              = @mEmpid,
                        @TDf5                 = @MonthStart,
                        @TDt5                 = @MonthEnd,
                        @location             = '',
                        @ComputeAttendanceFor = @ComputeAttendanceFor OUTPUT;
                END
                ELSE
                BEGIN
                    EXEC ComputeAttendanceFor
                        @EmpCode              = @mEmpid,
                        @TDf5                 = @MFDate,
                        @TDt5                 = @MFDate,
                        @location             = '',
                        @ComputeAttendanceFor = @ComputeAttendanceFor OUTPUT;
                END

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
            SET @MusterCreated = 0;

            -- Create muster ONLY if it does not already exist for this employee + date
            IF NOT EXISTS (
                SELECT 1
                FROM TblMuster WITH (NOLOCK)
                WHERE EmployeeId = @empid
                  AND TDate      = @fromDate
            )
            BEGIN
                EXEC CreateMusterFor
                    @EmpCode         = @empid,
                    @TempDate9       = @MFDate,
                    @FileNum         = 1,
                    @CreateMusterFor = @CreateMusterFor OUTPUT;

                SET @MusterCreated = ISNULL(@CreateMusterFor, 0);
            END

            -- If muster was newly created → compute for the entire month
            -- If muster already existed   → compute for the given date only
            IF @MusterCreated = 1
            BEGIN
                EXEC ComputeAttendanceFor
                    @EmpCode              = @empid,
                    @TDf5                 = @MonthStart,
                    @TDt5                 = @MonthEnd,
                    @location             = '',
                    @ComputeAttendanceFor = @ComputeAttendanceFor OUTPUT;
            END
            ELSE
            BEGIN
                EXEC ComputeAttendanceFor
                    @EmpCode              = @empid,
                    @TDf5                 = @MFDate,
                    @TDt5                 = @MFDate,
                    @location             = '',
                    @ComputeAttendanceFor = @ComputeAttendanceFor OUTPUT;
            END
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
-- All employees (muster created → computes full month; already exists → computes that date only):
-- EXEC CreateMusterServiceProc '2026-04-01', ''
--
-- Single employee:
-- EXEC CreateMusterServiceProc '2026-03-15', '00000011255'
