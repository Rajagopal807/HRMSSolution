USE [HRMS_DB]
GO
-- =============================================
-- Author:		Rajagopal
-- Create date: 15-Apr-2026
-- Description:	Procedure to create Muster for employees
-- =============================================

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[p_DeleteDuplicatePunches]
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE 
        @DuplicatePeriod INT,
        @PunchedTime DATETIME,
        @IOFlag CHAR(1),
        @Time1 DATETIME,
        @TransTime DATETIME,
        @PunchedTime1 DATETIME,
        @TempEmpId VARCHAR(11),
        @EmpId VARCHAR(11),
        @IOFlag1 CHAR(1);

    -- Get duplicate period
    SELECT @DuplicatePeriod = 10;

    PRINT 'Duplicate Period: ' + CAST(@DuplicatePeriod AS VARCHAR);

    IF (@DuplicatePeriod > 0)
    BEGIN
        DECLARE DuplicatePeriod_Cur CURSOR FOR
        SELECT EmpId, TransTime, PunchedTime, IOFlag
        FROM TblDailyTransactions
        WHERE AttendanceDate IS NULL
        ORDER BY EmpId, TransTime;

        OPEN DuplicatePeriod_Cur;

        FETCH NEXT FROM DuplicatePeriod_Cur 
        INTO @EmpId, @TransTime, @PunchedTime, @IOFlag;

        IF @@FETCH_STATUS = 0
        BEGIN
            SET @TempEmpId   = @EmpId;
            SET @Time1       = DATEADD(MINUTE, @DuplicatePeriod, @TransTime);
            SET @IOFlag1     = @IOFlag;
            SET @PunchedTime1 = @PunchedTime;

            FETCH NEXT FROM DuplicatePeriod_Cur 
            INTO @EmpId, @TransTime, @PunchedTime, @IOFlag;

            WHILE @@FETCH_STATUS = 0
            BEGIN
                IF (
                    @TransTime <= @Time1
                    AND @IOFlag1 = @IOFlag
                    AND @EmpId = @TempEmpId
                )
                BEGIN
                    UPDATE TblDailyTransactions
                    SET 
                        Deleted = 'T',
                        AttendanceDate = CAST(@PunchedTime1 AS DATE)
                    WHERE 
                        EmpId = @TempEmpId
                        AND CAST(PunchedTime AS DATE) = CAST(@PunchedTime1 AS DATE)
                        AND CAST(TransTime AS TIME) = CAST(@PunchedTime1 AS TIME)
                        AND IOFlag = @IOFlag1
                        AND AttendanceDate IS NULL;

                    PRINT 'Duplicate marked for EmpId: ' + @TempEmpId;
                END

                -- Reset comparison values
                SET @TempEmpId    = @EmpId;
                SET @Time1        = DATEADD(MINUTE, @DuplicatePeriod, @TransTime);
                SET @IOFlag1      = @IOFlag;
                SET @PunchedTime1 = @PunchedTime;

                FETCH NEXT FROM DuplicatePeriod_Cur 
                INTO @EmpId, @TransTime, @PunchedTime, @IOFlag;
            END
        END

        CLOSE DuplicatePeriod_Cur;
        DEALLOCATE DuplicatePeriod_Cur;
    END
END
GO