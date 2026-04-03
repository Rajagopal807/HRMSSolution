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
            catch(Exception ex)
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
            catch(Exception ex)
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
}