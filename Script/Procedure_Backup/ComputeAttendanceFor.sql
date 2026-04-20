USE [HRMS_DB]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- Procedure : [dbo].[ComputeAttendanceFor]
-- Description: Computes attendance for a given employee over a date range.
--              Handles shift detection, punch pairing, holiday/weekly-off logic,
--              normal/extra/outpass hours calculation, late-in and early-out.
-- Parameters:
--   @EmpCode  - Employee code
--   @TDf5     - Date range start (varchar, auto-cast to datetime)
--   @TDt5     - Date range end   (varchar, auto-cast to datetime)
--   @location - Location code
--   @ComputeAttendanceFor (OUTPUT) - 0 = success, 1 = error/skipped, -1 = null input
-- =============================================
ALTER PROCEDURE [dbo].[ComputeAttendanceFor]
    @EmpCode  VARCHAR(11),
    @TDf5     VARCHAR(11),
    @TDt5     VARCHAR(11),
    @location CHAR(6),
    @ComputeAttendanceFor SMALLINT OUTPUT
AS
BEGIN
    SET NOCOUNT OFF;

    -- -------------------------
    PRINT 'Input validation'
    -- Input validation
    -- -------------------------
    IF LTRIM(RTRIM(@EmpCode)) IS NULL
        OR LTRIM(RTRIM(@TDf5))   IS NULL
        OR LTRIM(RTRIM(@TDt5))   IS NULL
    BEGIN
        SET @ComputeAttendanceFor = -1;
        RETURN;
    END

    -- -------------------------
    PRINT 'Working variables'
    -- Working variables
    -- -------------------------
    DECLARE
        @Dtfrom         DATETIME,
        @Dtto           DATETIME,
        @TDT            DATETIME,
        @fromdate       VARCHAR(20),
        @IOFlag         CHAR(1),
        @TransTime      DATETIME,
        @BOutPassHrs    INT,
        @OPFlag         CHAR(1),
        @LastOut        DATETIME,
        @ShiftId        CHAR(2),
        @AttId          CHAR(2),
        @SingleOT       INT,
        @DoubleOT       INT,
        @CompOff        INT,
        @GraceInTime    INT,
        @GraceOutTime   INT,
        @GraceLunchIn   INT,
        @GraceLunchOut  INT,
        @GraceOverTime  INT,
        @GraceOT        INT,
        @CadreId        VARCHAR(6),
        @LateInFlag     CHAR(1),
        @EarlyOutFlag   CHAR(1),
        @ShiftStartTime DATETIME,
        @ShiftEndTime   DATETIME,
        @LunchStartTime DATETIME,
        @LunchEndTime   DATETIME,
        @cntCompany     INT,
        @cntShift       INT,
        @cntMRec        INT,
        @cntDp          INT,
        @cntLate        INT,
        @cntHoliDay1    INT,
        @cntMinTrans    INT,
        @ShiftCode      CHAR(2),
        @SCode          CHAR(2),
        @ShiftStart     DATETIME,
        @ShiftEnd       DATETIME,
        @LunchStart     DATETIME,
        @LunchEnd       DATETIME,
        @SftGraceIn     FLOAT,
        @SftGraceOut    FLOAT,
        @LunGraceIn     FLOAT,
        @LunGraceOut    FLOAT,
        @SftGraceStart  DATETIME,
        @SftGraceEnd    DATETIME,
        @LunGraceStart  DATETIME,
        @LunGraceEnd    DATETIME,
        @NormalHrs      INT,
        @ExtraHrs       INT,
        @OutPass        INT,
        @LateIn         INT,
        @EarlyOutHrs    INT,
        @LateHrs        INT,
        @BusLateBy      INT,
        @IsPairedRet    INT,
        @iActualOPHrs   INT,
        @iBOutPassHrs   INT,
        @TempTime       INT,
        @Time1          DATETIME,
        @Time2          DATETIME,
        @sOPFlag        CHAR(1),
        @EmpStatus      CHAR(1),
        @AttnId         CHAR(2),
        @StrHoliday     CHAR(2),
        @LocId          VARCHAR(6),
        @Pdate          VARCHAR(10),
        @CFinLOut       CHAR(1),
        @Fin            DATETIME,
        @temp_punch     INT,
        @I              INT,
        @cntPunch       BIT,
        @cunt           INT;

    -- -------------------------
    PRINT 'Initialise date range'
    -- Initialise date range
    -- -------------------------
    SET @Dtfrom = @TDf5; --TRY_CONVERT(DATETIME, @TDf5, 101);
    SET @Dtto   = @TDt5; --TRY_CONVERT(DATETIME, @TDt5, 101);

    -- Read FirstIn/LastOut calculation flag once (outside the loop)
    SELECT @CFinLOut = 'T';
    --SELECT TOP 1 @CFinLOut = FInLoutCal FROM TblCompany;

    -- Clear display errors once before the loop
    DELETE FROM tblErrorDisplay;

    -- =========================================================
    PRINT 'Main date loop'
    -- Main date loop
    -- =========================================================
    WHILE @Dtfrom <= @Dtto
    BEGIN
        -- Reset per-day state
        SET @TDT            = CONVERT(DATETIME, @Dtfrom, 101);
        SET @fromdate       = @TDT;
        SET @LateHrs        = 0;
        SET @IsPairedRet    = 0;
        SET @StrHoliday     = '';
        SET @NormalHrs      = 0;
        SET @ExtraHrs       = 0;
        SET @OutPass        = 0;
        SET @LateIn         = 0;
        SET @EarlyOutHrs    = 0;
        SET @AttnId         = 'AA';
        SET @cntHoliDay1    = 0;
        SET @ShiftStart     = NULL;
        SET @ShiftEnd       = NULL;
        SET @LunchStart     = NULL;
        SET @LunchEnd       = NULL;
        SET @SftGraceIn     = 0;
        SET @SftGraceOut    = 0;
        SET @LunGraceIn     = 0;
        SET @LunGraceOut    = 0;
        SET @GraceOT        = 0;
        SET @cunt           = 0;

        -- -------------------------
        PRINT '1. Punch pairing check'
        -- 1. Punch pairing check
        -- -------------------------
        IF @CFinLOut = 'F'
        BEGIN
            EXEC p_IsPaired @EmpCode, @TDT, @ispaired = @IsPairedRet OUTPUT;
        END
        ELSE
        BEGIN
            SELECT @temp_punch = COUNT(*)
            FROM TblDailyTransactions
            WHERE EmpId          = LTRIM(RTRIM(@EmpCode))
              AND AttendanceDate = CAST(@fromdate AS CHAR(11))
              AND Deleted        = 'F';

            IF @temp_punch = 1
            BEGIN
                SET @IsPairedRet = 1;

                SELECT TOP 1
                    @Fin    = TransTime,
                    @IOFlag = IOFlag
                FROM TblDailyTransactions
                WHERE AttendanceDate = CONVERT(DATETIME, @TDT, 101)
                  AND EmpId          = LTRIM(RTRIM(@EmpCode))
                  AND Deleted        = 'F';

                UPDATE tblMuster
                SET FirstIn = NULL, LastOut = NULL
                WHERE TDate = CONVERT(DATETIME, @TDT, 101)
                  AND EmployeeId = LTRIM(RTRIM(@EmpCode));

                IF @IOFlag = 'I'
                    UPDATE tblMuster
                    SET FirstIn = CONVERT(VARCHAR, @Fin, 108), AttId = 'AA'
                    WHERE TDate = CONVERT(DATETIME, @TDT, 101)
                      AND EmployeeId = LTRIM(RTRIM(@EmpCode));
                ELSE IF @IOFlag = 'O'
                    UPDATE tblMuster
                    SET LastOut = CONVERT(VARCHAR, @Fin, 108), AttId = 'AA'
                    WHERE TDate = CONVERT(DATETIME, @TDT, 101)
                      AND EmployeeId = LTRIM(RTRIM(@EmpCode));
            END
            ELSE IF @temp_punch = 0
            BEGIN
                UPDATE tblMuster
                SET AttId = 'AA'
                WHERE EmployeeId = LTRIM(RTRIM(@EmpCode))
                  AND TDate = @fromdate;
            END
            ELSE -- >= 2 punches
            BEGIN
                
                PRINT '>= 2 punches'

                SELECT
                    @Fin     = MIN(TransTime),
                    @LastOut = MAX(TransTime)
                FROM TblDailyTransactions
                WHERE AttendanceDate = CONVERT(DATETIME, @TDT, 101)
                  AND EmpId          = LTRIM(RTRIM(@EmpCode))
                  AND Deleted        = 'F';
                  PRINT '@Fin       : '+ CONVERT(VARCHAR, @Fin, 108);
                  PRINT '@LastOut   : '+ CONVERT(VARCHAR, @LastOut, 108);

                BEGIN TRANSACTION;
                UPDATE TblMuster
                SET FirstIn = CONVERT(VARCHAR, @Fin, 108),
                    LastOut = CONVERT(VARCHAR, @LastOut, 108)
                WHERE TDate = CONVERT(DATETIME,@TDT,101)
                  AND EmployeeId = LTRIM(RTRIM(@EmpCode));
                COMMIT TRANSACTION;
            END
        END

        -- -------------------------
           PRINT '2. Read muster record'
        -- 2. Read muster record
        -- -------------------------
        SELECT 
            --@cunt     = CAST(COUNT (*) as Varchar),
            @ShiftId  = ShiftId,
            @AttId    = AttId,
            @SingleOT = ISNULL(SingleOT, 0),
            @DoubleOT = ISNULL(DoubleOT, 0),
            @CompOff  = ISNULL(CompOff,  0)
        FROM tblMuster
        WHERE EmployeeId = LTRIM(RTRIM(@EmpCode))
          AND TDate = @fromdate;
        IF @@ROWCOUNT = 0
        BEGIN
        PRINT '@@ROWCOUNT  :' + CAST(@@ROWCOUNT as VARCHAR(10));
            SET @ComputeAttendanceFor = 1;
            SET @Dtfrom = DATEADD(DAY, 1, @Dtfrom);
            CONTINUE;
        END

        -- -------------------------
        PRINT '3. Holiday check'
        -- 3. Holiday check
        -- -------------------------
        --SET @LocId = (SELECT LocationCode FROM tblEmpMast WHERE EmpId = LTRIM(RTRIM(@EmpCode)));

        --IF EXISTS (SELECT 1 FROM TblHoliday WHERE Holiday = @TDT)
        --    SET @StrHoliday = 'HH';

        -- -------------------------
        -- 4. Resolve shift code
        -- -------------------------
        SET @ShiftCode = LTRIM(RTRIM(@ShiftId));

        IF @IsPairedRet = 1
        BEGIN
            EXEC p_DetermineShift @EmpCode, @TDT, @ShiftCode, @SCode OUTPUT;
            EXEC p_Late_In @EmpCode, @TDT, @ShiftCode, @par_late_in = @LateHrs OUTPUT;


            BEGIN TRANSACTION;
                UPDATE tblMuster
                SET ShiftId    = @SCode,
                    ErrCodeId  = 1,
                    AttId      = 'AA',
                    LatePunch  = @LateHrs,
                    HrsWorked  = 0,
                    OutPasses  = 0,
                    ExtraHours = 0,
                    EarlyOut   = 0,
                    SingleOT   = 0,
                    DoubleOT   = 0
                WHERE EmployeeId = LTRIM(RTRIM(@EmpCode))
                  AND TDate = CAST(@fromdate AS CHAR(11));

                INSERT INTO tblErrorDisplay
                VALUES (LTRIM(RTRIM(@EmpCode)), @TDT, 'C', 'Punches Are Not Paired');
            COMMIT TRANSACTION;

            SET @ComputeAttendanceFor = 1;
            SET @Dtfrom = DATEADD(DAY, 1, @Dtfrom);
            CONTINUE;
        END

        IF @ShiftCode IS NULL
        BEGIN
            BEGIN TRANSACTION;
                UPDATE tblMuster
                SET ErrCodeId  = 2,
                    HrsWorked  = 0,
                    OutPasses  = 0,
                    ExtraHours = 0,
                    EarlyOut   = 0
                WHERE EmployeeId = LTRIM(RTRIM(@EmpCode))
                  AND TDate = CAST(@fromdate AS CHAR(11));

                INSERT INTO tblErrorDisplay
                VALUES (LTRIM(RTRIM(@EmpCode)), @TDT, 'C',
                        'ShiftId Not Present in Shift Master Or Null in the Muster');
            COMMIT TRANSACTION;

            SET @ComputeAttendanceFor = 1;
            SET @Dtfrom = DATEADD(DAY, 1, @Dtfrom);
            CONTINUE;
        END

        -- -------------------------
        PRINT '5. Non-weekly-off: load shift timings'
        -- 5. Non-weekly-off: load shift timings
        -- -------------------------
        IF UPPER(@ShiftCode) <> 'WW'
        BEGIN
            -- Auto-shift detection (single-char shift code)
            IF LEN(LTRIM(RTRIM(@ShiftCode))) = 1
            BEGIN
                EXEC p_DetermineShift @EmpCode, @TDT, @ShiftCode, @SCode OUTPUT;
                SET @ShiftCode = @SCode;

                UPDATE tblMuster
                SET ShiftId = @ShiftCode
                WHERE EmployeeId = @EmpCode
                  AND CONVERT(DATETIME, TDate, 101) = @fromdate;
            END

            SELECT
                @ShiftStartTime = StartTime,
                @ShiftEndTime   = EndTime,
                @LunchStartTime = LunchStart,
                @LunchEndTime   = LunchEnd
            FROM TblShiftDetails
            WHERE ShiftId = LTRIM(RTRIM(@ShiftCode))
              AND DayId   = DATEPART(DW, @TDT);

            IF @@ROWCOUNT = 0
            BEGIN
                BEGIN TRANSACTION;
                    UPDATE tblMuster
                    SET AttId      = CASE WHEN @StrHoliday = 'HH' THEN 'HH' ELSE 'AA' END,
                        ErrCodeId  = 2,
                        HrsWorked  = 0,
                        OutPasses  = 0,
                        ExtraHours = 0,
                        EarlyOut   = 0
                    WHERE EmployeeId = LTRIM(RTRIM(@EmpCode))
                      AND TDate = CAST(@fromdate AS CHAR(11));

                    INSERT INTO tblErrorDisplay
                    VALUES (LTRIM(RTRIM(@EmpCode)), CONVERT(DATETIME, @TDT, 101), 'C',
                            'ShiftId Not Present in Shift Master Or Null in the Muster');
                COMMIT TRANSACTION;

                SET @ComputeAttendanceFor = 1;
                SET @Dtfrom = DATEADD(DAY, 1, @Dtfrom);
                CONTINUE;
            END

            IF @ShiftStartTime IS NULL OR @ShiftEndTime IS NULL
            BEGIN
                BEGIN TRANSACTION;
                    UPDATE tblMuster
                    SET ErrCodeId  = 3,
                        HrsWorked  = 0,
                        OutPasses  = 0,
                        ExtraHours = 0,
                        EarlyOut   = 0
                    WHERE EmployeeId = LTRIM(RTRIM(@EmpCode))
                      AND TDate = CAST(@fromdate AS CHAR(11));

                    INSERT INTO tblErrorDisplay
                    VALUES (LTRIM(RTRIM(@EmpCode)), @TDT, 'C',
                            'Timings Not Present in the Shift Master');
                COMMIT TRANSACTION;

                SET @ComputeAttendanceFor = 1;
                -- Note: original did not advance @Dtfrom here; preserving that behaviour.
                CONTINUE;
            END

            -- Build absolute shift datetime boundaries
            SET @ShiftStart = CONVERT(VARCHAR(10), @TDT, 101) + ' ' + CONVERT(VARCHAR(5), @ShiftStartTime, 108);
            SET @ShiftEnd   = CONVERT(VARCHAR(10), @TDT, 101) + ' ' + CONVERT(VARCHAR(5), @ShiftEndTime,   108);

            PRINT '@ShiftStart    : '+ CONVERT(VARCHAR(20), @ShiftStart, 25)
            PRINT '@ShiftEnd      : '+ CONVERT(VARCHAR(20), @ShiftEnd, 25)

            IF @ShiftEnd < @ShiftStart
                SET @ShiftEnd = @ShiftEnd + 1;  -- cross-midnight shift

            PRINT '@LunchStartTime    : '+ CONVERT(VARCHAR(20), @LunchStartTime, 25)
            PRINT '@LunchEndTime      : '+ CONVERT(VARCHAR(20), @LunchEndTime, 25)

            -- Lunch boundaries
            IF @LunchStartTime IS NULL OR @LunchEndTime IS NULL
            BEGIN
                SET @LunchStart = @ShiftStart + (DATEDIFF(MI, @ShiftStart, @ShiftEnd) / 2) / 1440.0;
                SET @LunchEnd   = @LunchStart;
            END
            ELSE
            BEGIN
                SET @LunchStart = CONVERT(VARCHAR(10), @TDT, 101) + ' ' + CONVERT(VARCHAR(5), @LunchStartTime, 108);
                SET @LunchEnd   = CONVERT(VARCHAR(10), @TDT, 101) + ' ' + CONVERT(VARCHAR(5), @LunchEndTime,   108);

                IF @LunchStart < @ShiftStart
                BEGIN
                    SET @LunchStart = @LunchStart + 1;
                    SET @LunchEnd   = @LunchEnd   + 1;
                END
                ELSE IF @LunchEnd < @LunchStart
                    SET @LunchEnd = @LunchEnd + 1;
            END

            -- -------------------------
            PRINT '6. Company grace settings'
            -- 6. Company grace settings
            -- -------------------------
            SELECT 
                @SftGraceIn  = 0,
                @SftGraceOut = 0,
                @LunGraceIn  = 0,
                @LunGraceOut = 0,
                @GraceOT     = 0;
            --SELECT TOP 1
            --    @SftGraceIn  = ISNULL(GraceInTime,  0),
            --    @SftGraceOut = ISNULL(GraceOutTime, 0),
            --    @LunGraceIn  = ISNULL(GraceLunchIn, 0),
            --    @LunGraceOut = ISNULL(GraceLunchOut,0),
            --    @GraceOT     = ISNULL(GraceOt,      0)
            --FROM tblCompany;

            IF @LunchStart = @LunchEnd
            BEGIN
                SET @LunGraceIn  = 0;
                SET @LunGraceOut = 0;
            END

            SET @SftGraceStart = @ShiftStart + ((@SftGraceIn)  / 60.0 / 24.0);
            SET @SftGraceEnd   = @ShiftEnd   - ((@SftGraceOut) / 60.0 / 24.0);
            SET @LunGraceStart = @LunchStart - ((@LunGraceOut) / 60.0 / 24.0);
            SET @LunGraceEnd   = @LunchEnd   + ((@LunGraceIn)  / 60.0 / 24.0);
        END -- END non-WW shift setup

        -- =========================================================
        PRINT '7. Open daily transactions cursor'
        -- 7. Open daily transactions cursor
        --    Guard: deallocate from any prior iteration before re-declaring.
        -- =========================================================
        IF CURSOR_STATUS('local', 'c_DailyTransactions') >= -1
        BEGIN
            IF CURSOR_STATUS('local', 'c_DailyTransactions') > -1
                CLOSE c_DailyTransactions;
            DEALLOCATE c_DailyTransactions;
        END

        DECLARE c_DailyTransactions CURSOR LOCAL SCROLL FOR
            SELECT IOFlag, TransTime, BOutPassHrs, OPFlag
            FROM TblDailyTransactions
            WHERE AttendanceDate = CONVERT(DATETIME, @TDT, 101)
              AND EmpId          = LTRIM(RTRIM(@EmpCode))
              AND Deleted        = 'F'
            ORDER BY TransTime;

        OPEN c_DailyTransactions;
        FETCH FIRST FROM c_DailyTransactions INTO @IOFlag, @TransTime, @BOutPassHrs, @OPFlag;
        SELECT @cntDp = @@CURSOR_ROWS;

        IF @cntDp = 0 AND @StrHoliday = 'HH' AND UPPER(@ShiftCode) <> 'WW'
            SET @AttId = 'HH';

        PRINT '@cntDp   : ' + CAST(@cntDp as VARCHAR);
        -- =========================================================
        PRINT '8. BRANCH A: Punches exist'
        -- 8. BRANCH A: Punches exist
        -- =========================================================
        IF @cntDp > 0
        BEGIN
            PRINT '@cntDp   : ' + CAST(@cntDp as VARCHAR);

            SET @NormalHrs = 0;
            SET @ExtraHrs  = 0;
            SET @OutPass   = 0;
            SET @I         = 1;

            -- ======================
            PRINT '8a. Normal (non-WW) shift'
            -- 8a. Normal (non-WW) shift
            -- ======================
            IF UPPER(@ShiftCode) <> 'WW'
            BEGIN
                -- -------------------------------------------------------
                PRINT 'Punch-pair loop'
                -- Punch-pair loop
                -- -------------------------------------------------------
                WHILE @I <= @cntDp
                BEGIN
                    SET @EmpStatus    = UPPER(@IOFlag);
                    SET @Time1        = @TransTime;
                    SET @iBOutPassHrs = @BOutPassHrs;
                    SET @sOPFlag      = UPPER(@OPFlag);

                    IF @CFinLOut = 'T'
                    BEGIN
                        SET @I = @I + 1;
                        FETCH LAST FROM c_DailyTransactions INTO @IOFlag, @TransTime, @BOutPassHrs, @OPFlag;
                        SET @Time2 = @TransTime;
                        SET @I     = @cntDp + 1;
                    END
                    ELSE
                    BEGIN
                        SET @I = @I + 1;
                        FETCH NEXT FROM c_DailyTransactions INTO @IOFlag, @TransTime, @BOutPassHrs, @OPFlag;
                    END

                    IF @@FETCH_STATUS <> 0 BREAK;

                    SET @Time2     = @TransTime;
                    SET @EmpStatus = 'I';

                    PRINT '@Time1       : ' + CONVERT(VARCHAR(20), @Time1, 25);
                    PRINT '@Time2       : ' + CONVERT(VARCHAR(20), @Time2, 25);
                    PRINT '--------------------------------------------------';
                    PRINT '@Condition : ' + 
                                    CASE 
                                        WHEN @Time1 >= @ShiftStart AND @Time2 <= @ShiftEnd
                                        THEN 'TRUE'
                                        ELSE 'FALSE'
                                    END;
                    PRINT '--------------------------------------------------';

                    IF @EmpStatus = 'I'
                    BEGIN
                        IF CONVERT(DATETIME, @Time2, 108) <= CONVERT(DATETIME, @ShiftStart, 108)
                            SET @ExtraHrs = @ExtraHrs + DATEDIFF(MI, @Time1, @Time2)
                        ELSE IF @Time1 >= @ShiftEnd
                            SET @ExtraHrs = @ExtraHrs + DATEDIFF(MI, @Time1, @Time2)
                        ELSE IF @Time1 <= @ShiftStart AND @Time2 >= @ShiftEnd
                        BEGIN
                            SET @NormalHrs = @NormalHrs + (DATEDIFF(MI, @ShiftStart, @ShiftEnd) - DATEDIFF(MI, @LunchStart, @LunchEnd));
                            SET @ExtraHrs  = @ExtraHrs  + (DATEDIFF(MI, @Time1, @ShiftStart) + DATEDIFF(MI, @ShiftEnd, @Time2));
                        END
                        ELSE IF @Time1 >= @ShiftStart AND @Time2 <= @ShiftEnd
                        BEGIN
                            IF @Time2 >= @LunGraceStart AND @Time2 <= @LunchStart SET @Time2 = @LunchStart;
                            IF @Time1 >= @LunchEnd      AND @Time1 <= @LunGraceEnd SET @Time1 = @LunchEnd;

                            IF CONVERT(DATETIME, @Time2, 108) <= CONVERT(DATETIME, @LunchStart, 108)
                                SET @NormalHrs = @NormalHrs + DATEDIFF(MI, @Time1, @Time2)
                            ELSE IF CONVERT(DATETIME, @Time1, 108) >= CONVERT(DATETIME, @LunchEnd, 108)
                                SET @NormalHrs = @NormalHrs + DATEDIFF(MI, @Time1, @Time2)
                            ELSE IF CONVERT(DATETIME, @Time1, 108) <= CONVERT(DATETIME, @LunchStart, 108)
                                 AND CONVERT(DATETIME, @Time2, 108) >= CONVERT(DATETIME, @LunchEnd,   108)
                                SET @NormalHrs = @NormalHrs + (DATEDIFF(MI, @Time1, @Time2) - DATEDIFF(MI, @LunchStart, @LunchEnd))
                            ELSE IF @Time1 <= @LunchStart AND @Time2 < @LunchEnd
                                SET @NormalHrs = @NormalHrs + DATEDIFF(MI, @Time1, @LunchStart)
                            ELSE IF @Time1 >= @LunchStart AND @Time2 > @LunchEnd
                                SET @NormalHrs = @NormalHrs + DATEDIFF(MI, @LunchEnd, @Time2);
                        END
                        ELSE IF @Time1 < @ShiftStart AND @Time2 <= @ShiftEnd
                        BEGIN
                            IF @Time2 >= @LunGraceStart AND @Time2 <= @LunchStart SET @Time2 = @LunchStart;
                            SET @ExtraHrs = @ExtraHrs + DATEDIFF(MI, @Time1, @ShiftStart);

                            IF @Time2 < @LunchStart
                                SET @NormalHrs = @NormalHrs + DATEDIFF(MI, @ShiftStart, @Time2)
                            ELSE IF @Time2 < @LunchEnd
                                SET @NormalHrs = @NormalHrs + DATEDIFF(MI, @ShiftStart, @LunchStart)
                            ELSE
                                SET @NormalHrs = @NormalHrs + (DATEDIFF(MI, @ShiftStart, @Time2) - DATEDIFF(MI, @LunchStart, @LunchEnd));
                        END
                        ELSE IF @Time1 >= @ShiftStart AND @Time2 > @ShiftEnd
                        BEGIN
                            IF @Time1 >= @LunchEnd AND @Time1 <= @LunGraceEnd SET @Time1 = @LunchEnd;
                            SET @ExtraHrs = @ExtraHrs + DATEDIFF(MI, @ShiftEnd, @Time2);

                            IF @Time1 <= @LunchStart
                                SET @NormalHrs = @NormalHrs + (DATEDIFF(MI, @Time1, @ShiftEnd) - DATEDIFF(MI, @LunchStart, @LunchEnd))
                            ELSE IF @Time1 <= @LunchEnd
                                SET @NormalHrs = @NormalHrs + DATEDIFF(MI, @LunchEnd, @ShiftEnd)
                            ELSE
                                SET @NormalHrs = @NormalHrs + DATEDIFF(MI, @Time1, @ShiftEnd);
                        END
                    END
                    ELSE  -- OUT punch pair (OutPass)
                    BEGIN
                        IF @Time2 <= @ShiftStart OR @Time1 >= @ShiftEnd
                        BEGIN
                            IF @sOPFlag = 'O'
                            BEGIN
                                SET @iActualOPHrs = DATEDIFF(MI, @Time1, @Time2);
                                IF @iBOutPassHrs = 0
                                BEGIN
                                    SET @iBOutPassHrs = @iActualOPHrs;
                                    UPDATE TblDailyTransactions SET BoutPassHrs = @iBOutPassHrs
                                    WHERE EmpId = @EmpCode AND TransTime = @TransTime AND IOFlag = @IOFlag;
                                END
                                SET @ExtraHrs = @ExtraHrs + CASE WHEN @iActualOPHrs > @iBOutPassHrs THEN @iBOutPassHrs ELSE @iActualOPHrs END;
                            END
                        END
                        ELSE IF @Time1 <= @ShiftStart AND @Time2 >= @ShiftEnd
                        BEGIN
                            IF @sOPFlag = 'O'
                            BEGIN
                                SET @iActualOPHrs = DATEDIFF(MI, @Time1, @Time2) - DATEDIFF(MI, @LunchStart, @LunchEnd);
                                IF @iBOutPassHrs = 0
                                BEGIN
                                    SET @iBOutPassHrs = @iActualOPHrs;
                                    UPDATE TblDailyTransactions SET BoutPassHrs = @iBOutPassHrs
                                    WHERE EmpId = @EmpCode AND TransTime = @TransTime AND IOFlag = @IOFlag;
                                END
                                IF @iActualOPHrs > @iBOutPassHrs
                                BEGIN
                                    SET @TempTime = DATEDIFF(MI, @ShiftStart, @ShiftEnd) - DATEDIFF(MI, @LunchStart, @LunchEnd);
                                    IF @TempTime > @iBOutPassHrs
                                        SET @NormalHrs = @NormalHrs + @iBOutPassHrs;
                                    ELSE
                                    BEGIN
                                        SET @NormalHrs = @NormalHrs + @TempTime;
                                        SET @ExtraHrs  = @ExtraHrs  + @iBOutPassHrs - @TempTime;
                                    END
                                END
                                ELSE
                                BEGIN
                                    SET @NormalHrs = @NormalHrs + (DATEDIFF(MI, @ShiftStart, @LunchStart) + DATEDIFF(MI, @LunchEnd, @ShiftEnd));
                                    SET @ExtraHrs  = @ExtraHrs  + (DATEDIFF(MI, @Time1, @ShiftStart)     + DATEDIFF(MI, @ShiftEnd, @Time2));
                                END
                            END
                            ELSE
                                SET @OutPass = @OutPass + (DATEDIFF(MI, @Time1, @Time2) - DATEDIFF(MI, @LunchStart, @LunchEnd));
                        END
                        ELSE IF @Time1 >= @ShiftStart AND @Time2 <= @ShiftEnd
                        BEGIN
                            IF @Time1 >= @LunGraceStart AND @Time1 <= @LunchStart SET @Time1 = @LunchStart;
                            IF @Time2 >= @LunchEnd      AND @Time2 <= @LunGraceEnd SET @Time2 = @LunchEnd;

                            IF @Time2 <= @LunchStart OR @Time1 >= @LunchEnd
                            BEGIN
                                SET @iActualOPHrs = DATEDIFF(MI, @Time1, @Time2);
                                IF @sOPFlag = 'O'
                                BEGIN
                                    IF @iBOutPassHrs = 0
                                    BEGIN
                                        SET @iBOutPassHrs = @iActualOPHrs;
                                        UPDATE TblDailyTransactions SET BoutPassHrs = @iBOutPassHrs
                                        WHERE EmpId = @EmpCode AND TransTime = @TransTime AND IOFlag = @IOFlag;
                                    END
                                    SET @NormalHrs = @NormalHrs + CASE WHEN @iActualOPHrs > @iBOutPassHrs THEN @iBOutPassHrs ELSE @iActualOPHrs END;
                                END
                                ELSE
                                    SET @OutPass = @OutPass + @iActualOPHrs;
                            END
                            ELSE IF @Time1 <= @LunchStart AND @Time2 >= @LunchEnd
                            BEGIN
                                SET @iActualOPHrs = DATEDIFF(MI, @Time1, @Time2) - DATEDIFF(MI, @LunchStart, @LunchEnd);
                                IF @sOPFlag = 'O'
                                BEGIN
                                    IF @iBOutPassHrs = 0
                                    BEGIN
                                        SET @iBOutPassHrs = @iActualOPHrs;
                                        UPDATE TblDailyTransactions SET BoutPassHrs = @iBOutPassHrs
                                        WHERE EmpId = @EmpCode AND TransTime = @TransTime AND IOFlag = @IOFlag;
                                    END
                                    SET @NormalHrs = @NormalHrs + CASE WHEN @iActualOPHrs > @iBOutPassHrs THEN @iBOutPassHrs ELSE @iActualOPHrs END;
                                END
                                ELSE
                                    SET @OutPass = @OutPass + @iActualOPHrs;
                            END
                            ELSE IF @Time1 <= @LunchStart AND @Time2 < @LunchEnd
                            BEGIN
                                SET @iActualOPHrs = DATEDIFF(MI, @Time1, @LunchStart);
                                IF @sOPFlag = 'O'
                                BEGIN
                                    IF @iBOutPassHrs = 0
                                    BEGIN
                                        SET @iBOutPassHrs = @iActualOPHrs;
                                        UPDATE TblDailyTransactions SET BoutPassHrs = @iBOutPassHrs
                                        WHERE EmpId = @EmpCode AND TransTime = @TransTime AND IOFlag = @IOFlag;
                                    END
                                    SET @NormalHrs = @NormalHrs + CASE WHEN @iActualOPHrs > @iBOutPassHrs THEN @iBOutPassHrs ELSE @iActualOPHrs END;
                                END
                                ELSE
                                    SET @OutPass = @OutPass + @iActualOPHrs;
                            END
                            ELSE IF @Time1 >= @LunchStart AND @Time2 > @LunchEnd
                            BEGIN
                                SET @iActualOPHrs = DATEDIFF(MI, @LunchEnd, @Time2);
                                IF @sOPFlag = 'O'
                                BEGIN
                                    IF @iBOutPassHrs = 0
                                    BEGIN
                                        SET @iBOutPassHrs = @iActualOPHrs;
                                        UPDATE TblDailyTransactions SET BoutPassHrs = @iBOutPassHrs
                                        WHERE EmpId = @EmpCode AND TransTime = @TransTime AND IOFlag = @IOFlag;
                                    END
                                    SET @NormalHrs = @NormalHrs + CASE WHEN @iActualOPHrs > @iBOutPassHrs THEN @iBOutPassHrs ELSE @iActualOPHrs END;
                                END
                                ELSE
                                    SET @OutPass = @OutPass + @iActualOPHrs;
                            END
                            ELSE IF @Time1 < @ShiftStart AND @Time2 <= @ShiftEnd
                            BEGIN
                                IF @Time2 >= @LunchEnd AND @Time2 <= @LunGraceEnd SET @Time2 = @LunchEnd;
                                SET @iActualOPHrs = DATEDIFF(MI, @Time1, @Time2);

                                IF @Time2 < @LunchStart
                                    SET @TempTime = DATEDIFF(MI, @ShiftStart, @Time2)
                                ELSE IF @Time2 < @LunchEnd
                                BEGIN
                                    SET @iActualOPHrs = @iActualOPHrs - DATEDIFF(MI, @LunchStart, @Time2);
                                    SET @TempTime     = DATEDIFF(MI, @ShiftStart, @LunchStart) / 1440.0;
                                END
                                ELSE
                                BEGIN
                                    SET @iActualOPHrs = @iActualOPHrs - DATEDIFF(MI, @LunchStart, @LunchEnd);
                                    SET @TempTime     = DATEDIFF(MI, @ShiftStart, @LunchStart) + DATEDIFF(MI, @LunchEnd, @Time2);
                                END

                                IF @sOPFlag = 'O'
                                BEGIN
                                    IF @iBOutPassHrs = 0
                                    BEGIN
                                        SET @iBOutPassHrs = @iActualOPHrs;
                                        UPDATE TblDailyTransactions SET BoutPassHrs = @iBOutPassHrs
                                        WHERE EmpId = @EmpCode AND TransTime = @TransTime AND IOFlag = @IOFlag;
                                    END
                                    IF @iActualOPHrs > @iBOutPassHrs
                                    BEGIN
                                        IF @TempTime > @iBOutPassHrs
                                            SET @NormalHrs = @NormalHrs + @iBOutPassHrs;
                                        ELSE
                                        BEGIN
                                            SET @NormalHrs = @NormalHrs + @TempTime;
                                            SET @ExtraHrs  = @ExtraHrs  + @iBOutPassHrs - @TempTime;
                                        END
                                    END
                                    ELSE
                                    BEGIN
                                        IF @TempTime > @iActualOPHrs
                                            SET @NormalHrs = @NormalHrs + @iActualOPHrs;
                                        ELSE
                                        BEGIN
                                            SET @NormalHrs = @NormalHrs + @TempTime;
                                            SET @ExtraHrs  = @ExtraHrs  + @iActualOPHrs - @TempTime;
                                        END
                                    END
                                END
                                ELSE
                                    SET @OutPass = @OutPass + @iActualOPHrs;
                            END
                            ELSE IF @Time1 >= @ShiftStart AND @Time2 > @ShiftEnd
                            BEGIN
                                IF @Time1 > @LunchEnd AND @Time1 <= @LunGraceEnd SET @Time1 = @LunchEnd;
                                SET @iActualOPHrs = DATEDIFF(MI, @Time1, @ShiftEnd);

                                IF @Time1 < @LunchStart
                                BEGIN
                                    SET @iActualOPHrs = @iActualOPHrs - DATEDIFF(MI, @LunchStart, @LunchEnd);
                                    SET @TempTime     = DATEDIFF(MI, @Time1, @LunchStart) + DATEDIFF(MI, @LunchEnd, @ShiftEnd);
                                END
                                ELSE IF @Time1 < @LunchEnd
                                BEGIN
                                    SET @iActualOPHrs = @iActualOPHrs - DATEDIFF(MI, @Time1, @LunchEnd);
                                    SET @TempTime     = DATEDIFF(MI, @LunchEnd, @ShiftEnd);
                                END
                                ELSE
                                    SET @TempTime = DATEDIFF(MI, @Time1, @ShiftEnd);

                                IF @sOPFlag = 'O'
                                BEGIN
                                    IF @iBOutPassHrs = 0
                                    BEGIN
                                        SET @iBOutPassHrs = @iActualOPHrs;
                                        UPDATE TblDailyTransactions SET BoutPassHrs = @iBOutPassHrs
                                        WHERE EmpId = @EmpCode AND TransTime = @TransTime AND IOFlag = @IOFlag;
                                    END
                                    IF @iActualOPHrs > @iBOutPassHrs
                                    BEGIN
                                        IF @TempTime > @iBOutPassHrs
                                            SET @NormalHrs = @NormalHrs + @iBOutPassHrs;
                                        ELSE
                                        BEGIN
                                            SET @NormalHrs = @NormalHrs + @TempTime;
                                            SET @ExtraHrs  = @ExtraHrs  + @iBOutPassHrs - @TempTime;
                                        END
                                    END
                                    ELSE
                                    BEGIN
                                        IF @TempTime > @iActualOPHrs
                                            SET @NormalHrs = @NormalHrs + @iActualOPHrs;
                                        ELSE
                                        BEGIN
                                            SET @NormalHrs = @NormalHrs + @TempTime;
                                            SET @ExtraHrs  = @ExtraHrs  + @iActualOPHrs - @TempTime;
                                        END
                                    END
                                END
                                ELSE
                                    SET @OutPass = @OutPass + @iActualOPHrs;
                            END
                        END -- @Time1 >= @ShiftStart AND @Time2 <= @ShiftEnd
                    END -- OUT punch pair
                END -- WHILE punch-pair loop

                -- -------------------------------------------------------
                PRINT '9. Late-In / Early-Out'
                -- 9. Late-In / Early-Out
                -- -------------------------------------------------------
                SET @LateIn      = 0;
                SET @EarlyOutHrs = 0;

                FETCH FIRST FROM c_DailyTransactions INTO @IOFlag, @TransTime, @BOutPassHrs, @OPFlag;
                SET @LateIn = DATEDIFF(MI, @ShiftStart, @TransTime);
                IF @LateIn < 0 SET @LateIn = 0;

                IF @OPFlag = 'O'
                BEGIN
                    SET @NormalHrs = @NormalHrs + @BOutPassHrs;
                    SET @LateIn    = @LateIn    - @BOutPassHrs;
                END

                FETCH LAST FROM c_DailyTransactions INTO @IOFlag, @TransTime, @BOutPassHrs, @OPFlag;
                SET @EarlyOutHrs = DATEDIFF(MI, @TransTime, @ShiftEnd);
                IF @EarlyOutHrs < 0 SET @EarlyOutHrs = 0;

                IF @OPFlag = 'O'
                BEGIN
                    SET @NormalHrs   = @NormalHrs   + @BOutPassHrs;
                    SET @EarlyOutHrs = @EarlyOutHrs - @BOutPassHrs;
                END

                IF @LateIn <= @SftGraceIn
                BEGIN
                    SET @NormalHrs = @NormalHrs + @LateIn;
                    SET @LateIn    = 0;
                END

                IF @EarlyOutHrs <= @SftGraceOut
                BEGIN
                    SET @NormalHrs   = @NormalHrs + @EarlyOutHrs;
                    SET @EarlyOutHrs = 0;
                END

                SET @AttnId = 'AA';
                SET @AttId  = 'AA';

                --SET @Pdate = CONVERT(VARCHAR(10), @TDT, 101);
                --EXEC ProcLateDeduction @EmpCode, @Pdate;

                -- -------------------------------------------------------
                PRINT '10. Holiday override'
                -- 10. Holiday override
                -- -------------------------------------------------------
                IF @StrHoliday = 'HH'
                BEGIN
                    SET @AttId  = 'HH';
                    SET @AttnId = 'HH';

                    BEGIN TRANSACTION;
                        UPDATE tblMuster
                        SET AttId = @StrHoliday
                        WHERE EmployeeId = LTRIM(RTRIM(@EmpCode))
                          AND TDate = CAST(@fromdate AS CHAR(11));
                    COMMIT TRANSACTION;

                    SET @ExtraHrs    = @ExtraHrs + @NormalHrs;
                    SET @NormalHrs   = 0;
                    SET @EarlyOutHrs = 0;
                    SET @LateIn      = 0;
                END
                ELSE
                BEGIN
                    -- -------------------------------------------------------
                    PRINT '11. Attendance ID derivation'
                    -- 11. Attendance ID derivation
                    -- -------------------------------------------------------
                    IF @LateIn >= DATEDIFF(MI, @ShiftStart, @ShiftEnd)
                    BEGIN
                        SET @AttnId = 'AA';
                        SET @LateIn = 0;
                    END
                    ELSE
                    BEGIN
                        IF @LateIn > 180
                        BEGIN
                            SET @AttnId = 'A' + SUBSTRING(@ShiftCode, 2, 1);
                            SET @LateIn = 0;
                        END
                        ELSE IF @LateIn >= DATEDIFF(MI, @ShiftStart, @LunchStart)
                        BEGIN
                            SET @AttnId = 'A' + SUBSTRING(@ShiftCode, 2, 1);
                            SET @LateIn = @LateIn - DATEDIFF(MI, @ShiftStart, @LunchEnd);
                        END
                        ELSE
                            SET @AttnId = RIGHT(@ShiftCode, 1) + RIGHT(@AttnId, 1);
                    END

                    IF @EarlyOutHrs >= DATEDIFF(MI, @ShiftStart, @ShiftEnd)
                    BEGIN
                        SET @AttnId      = 'AA';
                        SET @EarlyOutHrs = 0;
                    END
                    ELSE
                    BEGIN
                        IF @EarlyOutHrs >= 150
                        BEGIN
                            SET @AttnId      = LEFT(@AttnId, 1) + 'A';
                            SET @EarlyOutHrs = 0;
                        END
                        ELSE IF @EarlyOutHrs > DATEDIFF(MI, @LunchEnd, @ShiftEnd)
                        BEGIN
                            SET @AttnId      = SUBSTRING(@ShiftCode, 2, 1) + 'A';
                            SET @EarlyOutHrs = @EarlyOutHrs - DATEDIFF(MI, @LunchStart, @ShiftEnd);
                            IF @EarlyOutHrs < 0 SET @EarlyOutHrs = 0;
                        END
                        ELSE
                            SET @AttnId = LEFT(@AttnId, 1) + RIGHT(@ShiftCode, 1);
                    END
                END -- ELSE not holiday

                -- -------------------------------------------------------
                PRINT '12. Bus late deduction'
                -- 12. Bus late deduction
                -- -------------------------------------------------------
                SET @BusLateBy = 0;
                --SELECT TOP 1 @BusLateBy = ISNULL(LateBy, 0)
                --FROM TblBusLateEntries
                --INNER JOIN TblEmpMast ON TblBusLateEntries.BusRtId = TblEmpMast.BusRtId
                --WHERE TblEmpMast.EmpId              = LTRIM(RTRIM(@EmpCode))
                --  AND TDate                         = @TDT
                --  AND TblBusLateEntries.ShiftId      = @ShiftCode
                --  AND TblBusLateEntries.Locationcode = @location;

                IF @@ROWCOUNT = 0 SET @BusLateBy = 0;

                SET @LateIn = @LateIn - @BusLateBy;
                IF @LateIn < 0 SET @LateIn = 0;

                -- OT threshold checks
                IF @ExtraHrs < @GraceOT  SET @ExtraHrs = 0;
                IF @ExtraHrs < @SingleOT SET @SingleOT  = 0;
                IF @ExtraHrs < @DoubleOT SET @DoubleOT  = 0;
                IF @ExtraHrs < @CompOff  SET @CompOff   = 0;

                -- -------------------------------------------------------
                PRINT '13. Cadre overrides'
                -- 13. Cadre overrides
                -- -------------------------------------------------------
                    SET @CadreId      = 'STAFF';
                    SET @LateInFlag   = 'T';
                    SET @EarlyOutFlag = 'F';
                --SELECT TOP 1
                --    @CadreId      = CadreGroup,
                --    @LateInFlag   = LateInFlag,
                --    @EarlyOutFlag = EarlyOutFlag
                --FROM tblCadres
                --WHERE CadreId = (SELECT CadreId FROM tblEmpMast WHERE EmpId = LTRIM(RTRIM(@EmpCode)));

                IF @@ROWCOUNT > 0
                BEGIN
                    IF @LateInFlag   = '0' SET @LateIn      = 0;
                    IF @EarlyOutFlag = '0' SET @EarlyOutHrs = 0;

                    IF UPPER(LEFT(LTRIM(RTRIM(@CadreId)), 1)) = 'M'
                    BEGIN
                        IF @NormalHrs <= 120 AND @NormalHrs > 0 AND @AttnId <> '**'
                        BEGIN
                            SET @AttnId      = 'AA';
                            SET @LateIn      = 0;
                            SET @EarlyOutHrs = 0;
                        END
                    END
                    ELSE
                    BEGIN
                        IF @NormalHrs <= 30 AND @NormalHrs > 0 AND @AttnId <> '**'
                        BEGIN
                            SET @AttnId      = 'AA';
                            SET @LateIn      = 0;
                            SET @EarlyOutHrs = 0;
                        END
                    END
                END

                -- Write final muster record
                BEGIN TRANSACTION;
                    UPDATE tblMuster
                    SET AttId      = @AttnId,
                        HrsWorked  = @NormalHrs,
                        OutPasses  = @OutPass,
                        LatePunch  = @LateIn,
                        EarlyOut   = @EarlyOutHrs,
                        ExtraHours = @ExtraHrs,
                        SingleOT   = @SingleOT,
                        DoubleOT   = @DoubleOT,
                        CompOff    = @CompOff,
                        ErrCodeId  = 0
                    WHERE EmployeeId = LTRIM(RTRIM(@EmpCode))
                      AND TDate = CAST(@fromdate AS CHAR(11));
                COMMIT TRANSACTION;

                -- -------------------------------------------------------
                PRINT '14. OutPass adjustments for first/last punch'
                -- 14. OutPass adjustments for first/last punch
                --     Guard: deallocate from any prior iteration before re-declaring.
                -- -------------------------------------------------------
                IF CURSOR_STATUS('local', 'DailyTransactions') >= -1
                BEGIN
                    IF CURSOR_STATUS('local', 'DailyTransactions') > -1
                        CLOSE DailyTransactions;
                    DEALLOCATE DailyTransactions;
                END

                DECLARE DailyTransactions CURSOR LOCAL SCROLL FOR
                    SELECT IOFlag, TransTime, BOutPassHrs, OPFlag
                    FROM TblDailyTransactions
                    WHERE AttendanceDate = CONVERT(DATETIME, @TDT, 101)
                      AND EmpId          = LTRIM(RTRIM(@EmpCode))
                      AND Deleted        = 'F'
                    ORDER BY TransTime;

                OPEN DailyTransactions;

                FETCH FIRST FROM DailyTransactions INTO @IOFlag, @TransTime, @BOutPassHrs, @OPFlag;
                IF @@FETCH_STATUS = 0 AND UPPER(@OPFlag) = 'O' AND @LateIn > 0
                BEGIN
                    SET @iBOutPassHrs = @LateIn;
                    UPDATE TblDailyTransactions SET BoutPassHrs = @iBOutPassHrs
                    WHERE EmpId = @EmpCode AND TransTime = @TransTime AND IOFlag = @IOFlag;

                    UPDATE tblMuster SET LatePunch = 0
                    WHERE EmployeeId = LTRIM(RTRIM(@EmpCode))
                      AND TDate = CAST(@fromdate AS CHAR(11));
                END

                FETCH LAST FROM DailyTransactions INTO @IOFlag, @TransTime, @BOutPassHrs, @OPFlag;
                IF @@FETCH_STATUS = 0 AND UPPER(@OPFlag) = 'O' AND @EarlyOutHrs > 0
                BEGIN
                    SET @iBOutPassHrs = @EarlyOutHrs;
                    UPDATE TblDailyTransactions SET BoutPassHrs = @iBOutPassHrs
                    WHERE EmpId = @EmpCode AND TransTime = @TransTime AND IOFlag = @IOFlag;

                    UPDATE tblMuster SET EarlyOut = 0
                    WHERE EmployeeId = LTRIM(RTRIM(@EmpCode))
                      AND TDate = CAST(@fromdate AS CHAR(11));
                END

                CLOSE DailyTransactions;
                DEALLOCATE DailyTransactions;

                SET @ComputeAttendanceFor = 0;
            END -- IF UPPER(@ShiftCode) <> 'WW'  (8a)

            -- ======================
            -- 8b. Weekly-Off shift with punches
            -- ======================
            ELSE
                        PRINT '8b. Weekly-Off shift with punches';
            BEGIN
                SET @I        = 1;
                SET @cntPunch = 1;

                WHILE @cntPunch = 1
                BEGIN
                    SET @EmpStatus    = UPPER(@IOFlag);
                    SET @Time1        = @TransTime;
                    SET @iBOutPassHrs = @BOutPassHrs;
                    SET @sOPFlag      = UPPER(@OPFlag);

                    IF @CFinLOut = 'T'
                    BEGIN
                        SET @I = @I + 1;
                        FETCH LAST FROM c_DailyTransactions INTO @IOFlag, @TransTime, @BOutPassHrs, @OPFlag;
                        SET @Time2    = @TransTime;
                        SET @cntPunch = 0;
                    END
                    ELSE
                    BEGIN
                        SET @I = @I + 1;
                        FETCH NEXT FROM c_DailyTransactions INTO @IOFlag, @TransTime, @BOutPassHrs, @OPFlag;
                    END

                    IF @@FETCH_STATUS <> 0 BREAK;

                    SET @Time2 = @TransTime;

                    PRINT '@Time1       : ' + CONVERT(VARCHAR(20), @Time1, 25);
                    PRINT '@Time2       : ' + CONVERT(VARCHAR(20), @Time2, 25);
                    PRINT '@EmpStatus   : ' + @EmpStatus;

                    IF @EmpStatus = 'I'
                        SET @ExtraHrs = @ExtraHrs + DATEDIFF(MI, @Time1, @Time2)
                    ELSE
                    BEGIN
                        SET @TempTime = DATEDIFF(MI, @Time1, @Time2);
                        IF @sOPFlag = 'O'
                            SET @ExtraHrs = @ExtraHrs + CASE WHEN @iBOutPassHrs > @TempTime THEN @TempTime ELSE @iBOutPassHrs END;
                        ELSE
                            SET @OutPass = 0;
                    END
                END -- WHILE weekly-off punch loop

                IF @ExtraHrs < @GraceOT SET @ExtraHrs = 0;

                IF UPPER(LTRIM(RTRIM(@AttId))) <> 'HH'
                BEGIN
                    SET @AttnId    = 'WW';
                    SET @ExtraHrs  = @ExtraHrs + @NormalHrs;
                    SET @NormalHrs = 0;
                END

                SET @LateIn      = 0;
                SET @EarlyOutHrs = 0;
                SET @AttnId      = CASE WHEN @ShiftCode = 'WW' THEN 'WW' ELSE 'HH' END;

                PRINT '@LateIn     : ' + CAST(@LateIn as VARCHAR);
                PRINT '@EarlyOutHrs: ' + CAST(@EarlyOutHrs as VARCHAR);
                PRINT '@ExtraHrs   : ' + CAST(@ExtraHrs as VARCHAR);
                PRINT '@AttnId     : ' + CAST(@AttnId as VARCHAR);
                PRINT '@fromdate   : ' + CAST(@fromdate AS CHAR(11));
                PRINT '@EmpCode    : ' + @EmpCode;

                BEGIN TRANSACTION;
                    UPDATE tblMuster
                    SET AttId      = @AttnId,
                        HrsWorked  = @NormalHrs,
                        OutPasses  = @OutPass,
                        LatePunch  = @LateIn,
                        EarlyOut   = @EarlyOutHrs,
                        ExtraHours = @ExtraHrs,
                        SingleOT   = @SingleOT,
                        DoubleOT   = @DoubleOT,
                        CompOff    = @CompOff,
                        UpdatedAt  = SYSDATETIME(),
                        ErrCodeId  = 0
                    WHERE EmployeeId = LTRIM(RTRIM(@EmpCode))
                      AND TDate = @TDT;
                COMMIT TRANSACTION;

                SET @ComputeAttendanceFor = 0;
            END -- ELSE weekly-off with punches (8b)
        END -- IF @cntDp > 0  (Branch A)

        -- =========================================================
        -- BRANCH B: No punches — mark absent / WW / holiday
        -- =========================================================
        ELSE
                PRINT 'BRANCH B: No punches — mark absent / WW / holiday';
        BEGIN
            IF UPPER(@ShiftId) = 'WW'
                SET @AttnId = 'WW'
            ELSE IF UPPER(@AttId) <> 'HH'
                SET @AttnId = 'AA'
            ELSE
                SET @AttnId = 'HH';

            BEGIN TRANSACTION;
                UPDATE tblMuster
                SET AttId      = @AttnId,
                    FirstIn    = '',
                    LastOut    = '',
                    HrsWorked  = 0,
                    OutPasses  = 0,
                    LatePunch  = 0,
                    ExtraHours = 0,
                    SingleOT   = @SingleOT,
                    DoubleOT   = @DoubleOT,
                    CompOff    = 0,
                    EarlyOut   = 0,
                    ErrCodeId  = 0
                WHERE EmployeeId = LTRIM(RTRIM(@EmpCode))
                  AND TDate = CAST(@fromdate AS CHAR(11));

                IF @@ERROR <> 0
                BEGIN
                    ROLLBACK TRANSACTION;
                    CLOSE c_DailyTransactions;
                    DEALLOCATE c_DailyTransactions;
                    SET @ComputeAttendanceFor = 1;
                    SET @Dtfrom = DATEADD(DAY, 1, @Dtfrom);
                    CONTINUE;
                END
            COMMIT TRANSACTION;

            SET @ComputeAttendanceFor = 0;
        END -- ELSE no punches (Branch B)

        -- Cleanup both cursors for this day (safe to call even if already closed/deallocated)
        IF CURSOR_STATUS('local', 'c_DailyTransactions') >= -1
        BEGIN
            IF CURSOR_STATUS('local', 'c_DailyTransactions') > -1
                CLOSE c_DailyTransactions;
            DEALLOCATE c_DailyTransactions;
        END

        IF CURSOR_STATUS('local', 'DailyTransactions') >= -1
        BEGIN
            IF CURSOR_STATUS('local', 'DailyTransactions') > -1
                CLOSE DailyTransactions;
            DEALLOCATE DailyTransactions;
        END

        SET @Dtfrom = DATEADD(DAY, 1, @Dtfrom);
    END -- WHILE date loop

    SET @ComputeAttendanceFor = 0;
    RETURN @ComputeAttendanceFor;
END
GO

-- Usage:
 --DECLARE @result SMALLINT;
 --EXEC ComputeAttendanceFor '00000011255', '03/01/2026', '03/01/2026', 'STAFF', @result OUTPUT;
 --Print @result
 --SELECT @result AS ComputeResult;
 --exec ComputeAttendanceFor '00000011255','03/01/2026','03/31/2026','STAFF',0