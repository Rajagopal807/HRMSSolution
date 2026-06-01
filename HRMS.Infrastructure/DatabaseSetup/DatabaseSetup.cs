using System.Data.SqlClient;
using System.Configuration;
using System;

public class DatabaseSetup
{
    public static void Initialize()
    {
        bool canContinue = true;
        string sql = string.Empty;
        SqlCommand cmd = null;
        string masterConn = ConfigurationManager.ConnectionStrings["MasterConnection"].ConnectionString;

        using (SqlConnection con = new SqlConnection(masterConn))
        {
            con.Open();

            try
            {
                sql = @"
            -- Create Database
            IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'HRMS_DB')
            BEGIN
                CREATE DATABASE HRMS_DB;
            END
            ";
                cmd = new SqlCommand(sql, con);
                cmd.ExecuteNonQuery();
            }
            catch
            {
                canContinue = false;
            }


            try
            {
                if(canContinue)
                {
                            sql = @"
			        -- Create Login
                    IF NOT EXISTS (SELECT * FROM sys.sql_logins WHERE name = 'hrms')
                    BEGIN
                        CREATE LOGIN hrms WITH PASSWORD = 'St!rs8rms@!@#';
                    END
                    ";
                            cmd = new SqlCommand(sql, con);
                            cmd.ExecuteNonQuery();
                        }
            }
            catch
            {
                sql = @"
            -- Drop Database
            IF EXISTS (SELECT name FROM sys.databases WHERE name = 'HRMS_DB')
            BEGIN
                DROP DATABASE HRMS_DB;
            END
            ";
                cmd = new SqlCommand(sql, con);
                cmd.ExecuteNonQuery();
                canContinue = false;
            }

            if(canContinue)
            {
                    sql = @"
                    USE HRMS_DB;

                    -- Create User
                    IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = 'hrms')
                    BEGIN
                        CREATE USER hrms FOR LOGIN hrms;
                        ALTER ROLE db_accessadmin ADD MEMBER hrms;
                        ALTER ROLE db_backupoperator ADD MEMBER hrms;
                        ALTER ROLE db_ddladmin ADD MEMBER hrms;
                        ALTER ROLE db_securityadmin ADD MEMBER hrms;
                        ALTER ROLE db_owner ADD MEMBER hrms;
                        ALTER ROLE db_datareader ADD MEMBER hrms;
                        ALTER ROLE db_datawriter ADD MEMBER hrms;
                    END
                ";
                    cmd = new SqlCommand(sql, con);
                    cmd.ExecuteNonQuery();
            }

        }
    }

    public static void EnsureHolidayTable()
    {
        string sql = @"
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
END
ELSE
BEGIN
    IF COL_LENGTH('dbo.TblHolidays', 'Id') IS NULL
        ALTER TABLE dbo.TblHolidays ADD Id INT IDENTITY(1,1) NOT NULL;

    IF NOT EXISTS (
        SELECT 1 FROM sys.key_constraints
        WHERE parent_object_id = OBJECT_ID('dbo.TblHolidays')
          AND [type] = 'PK'
    )
        ALTER TABLE dbo.TblHolidays ADD CONSTRAINT PK_TblHolidays PRIMARY KEY (Id);

    IF COL_LENGTH('dbo.TblHolidays', 'HolidayName') IS NULL
    BEGIN
        ALTER TABLE dbo.TblHolidays ADD HolidayName NVARCHAR(100) NULL;
        UPDATE dbo.TblHolidays SET HolidayName = 'Holiday' WHERE HolidayName IS NULL;
        ALTER TABLE dbo.TblHolidays ALTER COLUMN HolidayName NVARCHAR(100) NOT NULL;
    END

    IF COL_LENGTH('dbo.TblHolidays', 'Description') IS NULL
        ALTER TABLE dbo.TblHolidays ADD Description NVARCHAR(250) NULL;

    IF COL_LENGTH('dbo.TblHolidays', 'IsActive') IS NULL
        ALTER TABLE dbo.TblHolidays ADD IsActive BIT NOT NULL CONSTRAINT DF_TblHolidays_IsActive DEFAULT (1);

    IF COL_LENGTH('dbo.TblHolidays', 'CreatedAt') IS NULL
        ALTER TABLE dbo.TblHolidays ADD CreatedAt DATETIME NOT NULL CONSTRAINT DF_TblHolidays_CreatedAt DEFAULT (GETUTCDATE());

    IF COL_LENGTH('dbo.TblHolidays', 'UpdatedAt') IS NULL
        ALTER TABLE dbo.TblHolidays ADD UpdatedAt DATETIME NULL;

    IF COL_LENGTH('dbo.TblHolidays', 'IsDeleted') IS NULL
        ALTER TABLE dbo.TblHolidays ADD IsDeleted BIT NOT NULL CONSTRAINT DF_TblHolidays_IsDeleted DEFAULT (0);
END

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UX_TblHolidays_Holiday_Active'
      AND object_id = OBJECT_ID('dbo.TblHolidays')
)
    CREATE UNIQUE INDEX UX_TblHolidays_Holiday_Active
        ON dbo.TblHolidays(Holiday, IsDeleted);
";

        string conn = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        using (SqlConnection con = new SqlConnection(conn))
        using (SqlCommand cmd = new SqlCommand(sql, con))
        {
            con.Open();
            cmd.ExecuteNonQuery();
        }
    }
}
