IF OBJECT_ID('dbo.TblHolidays', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TblHolidays
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TblHolidays PRIMARY KEY,
        Holiday DATE NOT NULL,
        HolidayName NVARCHAR(100) NOT NULL,
        Description NVARCHAR(250) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_TblHolidays_IsActive DEFAULT (1),
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_TblHolidays_CreatedAt DEFAULT (GETUTCDATE()),
        UpdatedAt DATETIME NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_TblHolidays_IsDeleted DEFAULT (0)
    );

    CREATE UNIQUE INDEX UX_TblHolidays_Holiday_Active
        ON dbo.TblHolidays(Holiday, IsDeleted);
END
ELSE
BEGIN
    IF COL_LENGTH('dbo.TblHolidays', 'Id') IS NULL
    BEGIN
        ALTER TABLE dbo.TblHolidays
            ADD Id INT IDENTITY(1,1) NOT NULL;
    END

    IF NOT EXISTS (
        SELECT 1
        FROM sys.key_constraints
        WHERE parent_object_id = OBJECT_ID('dbo.TblHolidays')
          AND [type] = 'PK'
    )
    BEGIN
        ALTER TABLE dbo.TblHolidays
            ADD CONSTRAINT PK_TblHolidays PRIMARY KEY (Id);
    END

    IF COL_LENGTH('dbo.TblHolidays', 'HolidayName') IS NULL
    BEGIN
        ALTER TABLE dbo.TblHolidays
            ADD HolidayName NVARCHAR(100) NULL;

        UPDATE dbo.TblHolidays
        SET HolidayName = 'Holiday'
        WHERE HolidayName IS NULL;

        ALTER TABLE dbo.TblHolidays
            ALTER COLUMN HolidayName NVARCHAR(100) NOT NULL;
    END

    IF COL_LENGTH('dbo.TblHolidays', 'Description') IS NULL
    BEGIN
        ALTER TABLE dbo.TblHolidays
            ADD Description NVARCHAR(250) NULL;
    END

    IF COL_LENGTH('dbo.TblHolidays', 'IsActive') IS NULL
    BEGIN
        ALTER TABLE dbo.TblHolidays
            ADD IsActive BIT NOT NULL CONSTRAINT DF_TblHolidays_IsActive DEFAULT (1);
    END

    IF COL_LENGTH('dbo.TblHolidays', 'CreatedAt') IS NULL
    BEGIN
        ALTER TABLE dbo.TblHolidays
            ADD CreatedAt DATETIME NOT NULL CONSTRAINT DF_TblHolidays_CreatedAt DEFAULT (GETUTCDATE());
    END

    IF COL_LENGTH('dbo.TblHolidays', 'UpdatedAt') IS NULL
    BEGIN
        ALTER TABLE dbo.TblHolidays
            ADD UpdatedAt DATETIME NULL;
    END

    IF COL_LENGTH('dbo.TblHolidays', 'IsDeleted') IS NULL
    BEGIN
        ALTER TABLE dbo.TblHolidays
            ADD IsDeleted BIT NOT NULL CONSTRAINT DF_TblHolidays_IsDeleted DEFAULT (0);
    END

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = 'UX_TblHolidays_Holiday_Active'
          AND object_id = OBJECT_ID('dbo.TblHolidays')
    )
    BEGIN
        CREATE UNIQUE INDEX UX_TblHolidays_Holiday_Active
            ON dbo.TblHolidays(Holiday, IsDeleted);
    END
END
