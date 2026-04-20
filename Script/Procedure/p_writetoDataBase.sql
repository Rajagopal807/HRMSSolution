USE [HRMS_DB]
GO
-- =============================================
-- Author:		Rajagopal
-- Create date: 04-Apr-2026
-- Description:	Procedure to create Muster for employees
-- =============================================
CREATE OR ALTER PROCEDURE dbo.p_writetoDataBase
(
    @strline VARCHAR(40),
    @Sversion VARCHAR(10),
    @returnWtDb SMALLINT OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE 
        @strdate VARCHAR(8),
        @strcode VARCHAR(11),
        @strtime VARCHAR(4),
        @strio CHAR(1),
        @strbr VARCHAR(2),
        @strSftFlg VARCHAR(2),
        @StrReason VARCHAR(2),
        @dtTransTime DATETIME,
        @iret SMALLINT,
        @iRet1 CHAR(1),
        @strRemarks VARCHAR(50) = '',
        @StrTreatment CHAR(1) = 'P';

    SET @returnWtDb = 0;

    -- =============================
    -- Parse Input Based on Version (UNCHANGED)
    -- =============================
    IF @sversion = '7'
    BEGIN
        SELECT 
            @strdate = SUBSTRING(@strline,1,8),
            @strCode = '0000' + SUBSTRING(@strline,9,7),
            @strTime = SUBSTRING(@strline,16,4),
            @strIO   = SUBSTRING(@strline,20,1),
            @strBr   = SUBSTRING(@strline,21,2);
    END
    ELSE IF @sversion IN ('8','10')
    BEGIN
        SELECT 
            @strSftFlg = SUBSTRING(@strline,3,2),
            @strCode = '0' + SUBSTRING(@strline,7,10),
            @strdate = SUBSTRING(@strline,18,8),
            @strTime = SUBSTRING(@strline,26,4),
            @strIO   = SUBSTRING(@strline,30,1),
            @strBr   = SUBSTRING(@strline,35,2);
    END
    ELSE IF @sversion = '10.22'
    BEGIN
        SELECT 
            @StrReason = SUBSTRING(@strline,1,2),
            @strSftFlg = SUBSTRING(@strline,3,2),
            @strCode = '0' + SUBSTRING(@strline,8,10),
            @strdate = SUBSTRING(@strline,19,8),
            @strTime = SUBSTRING(@strline,27,4),
            @strIO   = SUBSTRING(@strline,31,1),
            @strBr   = SUBSTRING(@strline,37,2);
    END

    -- =============================
    -- 1. Shift Flag Validation
    -- =============================
    IF @sversion IN ('8','10','10.22')
    BEGIN
        IF UPPER(@strSftFlg) = 'FF'
        BEGIN
            SET @returnWtDb = 1;
            RETURN;
        END
    END

    -- =============================
    -- 2. Code Validation
    -- =============================
    DECLARE @cnt INT = 0;

    SELECT @cnt = COUNT(1) FROM TblEmpMast WHERE EmployeeId = @strcode;

    IF @cnt > 0
        SET @iret = 1;
    ELSE
    BEGIN
        SELECT @cnt = COUNT(1) FROM TblTempCards WHERE TempCardNo = @strcode;

        IF @cnt > 0
            SET @iret = 2;
        ELSE
            SET @iret = 0;
    END

    IF @iret = 0
    BEGIN
        SET @returnWtDb = 2;
        RETURN;
    END

    -- =============================
    -- 3. Date Validation (INLINE p_IsValidDate)
    -- =============================
    DECLARE 
        @TDate VARCHAR(20),
        @temp VARCHAR(20),
        @i INT,
        @TDay INT,
        @TMonth INT,
        @Tempdate DATETIME;

    SET @TDate = 
        SUBSTRING(@strdate,7,2) + '/' +
        SUBSTRING(@strdate,5,2) + '/' +
        SUBSTRING(@strdate,1,4);

    SET @TDate = LTRIM(@TDate);

    IF @TDate = ''
    BEGIN
        SET @returnWtDb = 3;
        RETURN;
    END

    SET @i = 1;
    SET @temp = '';

    -- Extract Day
    WHILE SUBSTRING(@TDate, @i, 1) <> '/' AND @i <= LEN(@TDate)
    BEGIN
        SET @temp = @temp + SUBSTRING(@TDate, @i, 1);
        SET @i = @i + 1;
    END

    IF LEN(@temp) > 2
    BEGIN
        SET @returnWtDb = 3;
        RETURN;
    END

    SET @TDay = TRY_CAST(@temp AS INT);

    IF @TDay IS NULL OR @TDay < 1 OR @TDay > 31
    BEGIN
        SET @returnWtDb = 3;
        RETURN;
    END

    SET @i = @i + 1;
    SET @temp = '';

    -- Extract Month
    WHILE SUBSTRING(@TDate, @i, 1) <> '/' AND @i <= LEN(@TDate)
    BEGIN
        SET @temp = @temp + SUBSTRING(@TDate, @i, 1);
        SET @i = @i + 1;
    END

    IF LEN(@temp) > 2
    BEGIN
        SET @returnWtDb = 3;
        RETURN;
    END

    SET @TMonth = TRY_CAST(@temp AS INT);

    IF @TMonth IS NULL OR @TMonth < 1 OR @TMonth > 12
    BEGIN
        SET @returnWtDb = 3;
        RETURN;
    END

    SET @i = @i + 1;
    SET @temp = '';

    -- Extract Year
    WHILE @i <= LEN(@TDate)
    BEGIN
        IF SUBSTRING(@TDate, @i, 1) = '/'
        BEGIN
            SET @returnWtDb = 3;
            RETURN;
        END
        SET @temp = @temp + SUBSTRING(@TDate, @i, 1);
        SET @i = @i + 1;
    END

    IF LEN(@temp) > 4 OR TRY_CAST(@temp AS INT) < 100
    BEGIN
        SET @returnWtDb = 3;
        RETURN;
    END

    -- Validate actual date (month-end logic)
    SET @Tempdate = CONVERT(DATETIME, '1/' + CAST(@TMonth AS VARCHAR) + '/' + @temp, 103);
    SET @Tempdate = DATEADD(MONTH, 1, @Tempdate) - 1;

    IF @TDay > DAY(@Tempdate)
    BEGIN
        SET @returnWtDb = 3;
        RETURN;
    END

    -- =============================
    -- 4. Time Validation
    -- =============================
    DECLARE @Hr INT, @Min INT;

    SET @Hr  = TRY_CAST(SUBSTRING(@strtime,1,2) AS INT);
    SET @Min = TRY_CAST(SUBSTRING(@strtime,3,2) AS INT);

    IF @Hr IS NULL OR @Min IS NULL OR @Hr NOT BETWEEN 0 AND 23 OR @Min NOT BETWEEN 0 AND 59
    BEGIN
        SET @returnWtDb = 4;
        RETURN;
    END

    -- =============================
    -- 5. IO Validation
    -- =============================
    IF @strIO IN ('0','I') SET @strIO = 'I';
    IF @strIO IN ('O','5') SET @strIO = 'O';

    IF UPPER(@strIO) IN ('I','O')
        SET @iRet1 = UPPER(@strIO);
    ELSE
        SET @iRet1 = '0';

    IF @iRet1 = '0'
    BEGIN
        SET @returnWtDb = 5;
        RETURN;
    END

    SET @strIO = @iRet1;

    -- =============================
    -- Build DATETIME
    -- =============================
    SET @dtTransTime = 
        CONVERT(DATETIME, 
            STUFF(STUFF(@strdate,5,0,'-'),8,0,'-') + ' ' +
            STUFF(@strtime,3,0,':')
        );

    -- =============================
    -- Duplicate Check
    -- =============================
    IF EXISTS
    (
        SELECT 1 
        FROM TblDailyTransactions WITH (NOLOCK)
        WHERE EmpId = @strcode
          AND TransTime = @dtTransTime
    )
    BEGIN
        RETURN;
    END

    -- =============================
    -- Insert
    -- =============================
    INSERT INTO TblDailyTransactions
    (
        EmpId, IOFlag, ActualIOFlag, OPFlag,
        PunchedTime, TransTime, AttendanceDate,
        BOutPassHrs, Remarks, BadgeReaderNo,
        Deleted, ReasonCode, CreatedAt, IsDeleted
    )
    VALUES
    (
        @strcode, @strIO, @strIO, @StrTreatment,
        @dtTransTime, @dtTransTime, NULL,
        0, @strRemarks, @strBr,
        'F', @StrReason, SYSDATETIME(), 0
    );

END