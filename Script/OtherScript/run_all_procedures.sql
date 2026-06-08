-- ============================================================
-- MASTER SCRIPT: Create All Stored Procedures
-- Run this on a fresh SQL Server database
-- ============================================================

SET NOCOUNT ON;
GO

PRINT '============================================================';
PRINT ' Starting Stored Procedure Deployment';
PRINT ' Time: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '============================================================';
GO

-- ------------------------------------------------------------
-- STEP 1: Verify we are connected to the correct database
-- Change 'YourDatabaseName' to your actual DB name
-- ------------------------------------------------------------
IF DB_NAME() = 'master'
BEGIN
    RAISERROR('WARNING: You are running this on [master]. Switch to your target database first!', 16, 1);
    -- Uncomment and set your DB name to auto-switch:
    -- USE [YourDatabaseName];
END
GO

-- ============================================================
-- HELPER: Drop procedure if it already exists before recreating
-- ============================================================

-- 1. ComputeAttendanceFor
PRINT 'Creating dbo.ComputeAttendanceFor...';
IF OBJECT_ID('dbo.ComputeAttendanceFor', 'P') IS NOT NULL
    DROP PROCEDURE dbo.ComputeAttendanceFor;
GO
:r .\dbo.ComputeAttendanceFor.sql
GO
PRINT '  [OK] dbo.ComputeAttendanceFor created.';
GO

-- 2. CreateMusterFor
PRINT 'Creating dbo.CreateMusterFor...';
IF OBJECT_ID('dbo.CreateMusterFor', 'P') IS NOT NULL
    DROP PROCEDURE dbo.CreateMusterFor;
GO
:r .\dbo.CreateMusterFor.sql
GO
PRINT '  [OK] dbo.CreateMusterFor created.';
GO

-- 3. CreateMusterServiceProc
PRINT 'Creating dbo.CreateMusterServiceProc...';
IF OBJECT_ID('dbo.CreateMusterServiceProc', 'P') IS NOT NULL
    DROP PROCEDURE dbo.CreateMusterServiceProc;
GO
:r .\dbo.CreateMusterServiceProc.sql
GO
PRINT '  [OK] dbo.CreateMusterServiceProc created.';
GO

-- 4. p_DeleteDuplicatePunches
PRINT 'Creating dbo.p_DeleteDuplicatePunches...';
IF OBJECT_ID('dbo.p_DeleteDuplicatePunches', 'P') IS NOT NULL
    DROP PROCEDURE dbo.p_DeleteDuplicatePunches;
GO
:r .\dbo.p_DeleteDuplicatePunches.sql
GO
PRINT '  [OK] dbo.p_DeleteDuplicatePunches created.';
GO

-- 5. p_DetermineAttendanceDate
PRINT 'Creating dbo.p_DetermineAttendanceDate...';
IF OBJECT_ID('dbo.p_DetermineAttendanceDate', 'P') IS NOT NULL
    DROP PROCEDURE dbo.p_DetermineAttendanceDate;
GO
:r .\dbo.p_DetermineAttendanceDate.sql
GO
PRINT '  [OK] dbo.p_DetermineAttendanceDate created.';
GO

-- 6. p_DetermineShift
PRINT 'Creating dbo.p_DetermineShift...';
IF OBJECT_ID('dbo.p_DetermineShift', 'P') IS NOT NULL
    DROP PROCEDURE dbo.p_DetermineShift;
GO
:r .\dbo.p_DetermineShift.sql
GO
PRINT '  [OK] dbo.p_DetermineShift created.';
GO

-- 7. p_InsertRecords
PRINT 'Creating dbo.p_InsertRecords...';
IF OBJECT_ID('dbo.p_InsertRecords', 'P') IS NOT NULL
    DROP PROCEDURE dbo.p_InsertRecords;
GO
:r .\dbo.p_InsertRecords.sql
GO
PRINT '  [OK] dbo.p_InsertRecords created.';
GO

-- 8. p_IsPaired
PRINT 'Creating dbo.p_IsPaired...';
IF OBJECT_ID('dbo.p_IsPaired', 'P') IS NOT NULL
    DROP PROCEDURE dbo.p_IsPaired;
GO
:r .\dbo.p_IsPaired.sql
GO
PRINT '  [OK] dbo.p_IsPaired created.';
GO

-- 9. p_Late_In
PRINT 'Creating dbo.p_Late_In...';
IF OBJECT_ID('dbo.p_Late_In', 'P') IS NOT NULL
    DROP PROCEDURE dbo.p_Late_In;
GO
:r .\dbo.p_Late_In.sql
GO
PRINT '  [OK] dbo.p_Late_In created.';
GO

-- 10. p_writetoDataBase
PRINT 'Creating dbo.p_writetoDataBase...';
IF OBJECT_ID('dbo.p_writetoDataBase', 'P') IS NOT NULL
    DROP PROCEDURE dbo.p_writetoDataBase;
GO
:r .\dbo.p_writetoDataBase.sql
GO
PRINT '  [OK] dbo.p_writetoDataBase created.';
GO

-- 11. WriteErrToFile
PRINT 'Creating dbo.WriteErrToFile...';
IF OBJECT_ID('dbo.WriteErrToFile', 'P') IS NOT NULL
    DROP PROCEDURE dbo.WriteErrToFile;
GO
:r .\dbo.WriteErrToFile.sql
GO
PRINT '  [OK] dbo.WriteErrToFile created.';
GO

-- ============================================================
-- FINAL VERIFICATION: List all deployed procedures
-- ============================================================
PRINT '';
PRINT '============================================================';
PRINT ' Deployment Summary';
PRINT '============================================================';

SELECT
    name            AS ProcedureName,
    create_date     AS CreatedOn,
    modify_date     AS LastModified
FROM sys.procedures
WHERE schema_id = SCHEMA_ID('dbo')
  AND name IN (
      'ComputeAttendanceFor',
      'CreateMusterFor',
      'CreateMusterServiceProc',
      'p_DeleteDuplicatePunches',
      'p_DetermineAttendanceDate',
      'p_DetermineShift',
      'p_InsertRecords',
      'p_IsPaired',
      'p_Late_In',
      'p_writetoDataBase',
      'WriteErrToFile'
  )
ORDER BY name;
GO

PRINT '============================================================';
PRINT ' All procedures deployed successfully!';
PRINT '============================================================';
GO
