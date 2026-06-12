using AutodownloadService.Utils;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutodownloadService.Interface.Unit
{
    public class PostDataToDBUnit
    {
        private IMessageLogger _logger;
        private AMSConnectionClass _amsConn = new AMSConnectionClass();
        private readonly String _atndFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Atnd.Dat");
        private String _errorLog = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"PostingError{DateTime.Now.ToString("yyyyMMdd")}.txt");
        public PostDataToDBUnit() 
        {
            _logger = new Filelogger("PostDataToDB");
        }

        public void PostData()
        {
            Int64 insRec = 0;
            Int64 errRec = 0;
            int result = 0;
            try
            {
                List<string> logdata = CommonUtil.readFile(_atndFilePath, "POSTING");
                if(logdata.Count == 0)
                {
                    _logger.Log("No data to post.");
                    return;
                }
                else
                {
                    string brVersion = "10.22";

                    foreach(string log in logdata)
                    {
                        _amsConn.ClearParameters();
                        _amsConn.AddParameter("@strline", log, SqlDbType.VarChar, ParameterDirection.Input);
                        _amsConn.AddParameter("@Sversion", brVersion, SqlDbType.VarChar, ParameterDirection.Input);
                        _amsConn.AddParameter("@returnWtDb", DBNull.Value, SqlDbType.SmallInt, ParameterDirection.Output);
                        result = _amsConn.ExecuteStoreProcedure("p_writetoDataBase");
                        if(result >0)
                        {
                            string error = errorMessage(result);
                            CommonUtil.appendActivityFile(_errorLog, $"{DateTime.Now.ToString("dd-MMM-yyyy HH:mm")} : {error} for Data {log}", "POSTING");
                            errRec++;
                        }
                        else
                        {
                            insRec++;
                        }

                    }
                }

                //DeleteDuplicatePunch
                _amsConn.ClearParameters();
                result = _amsConn.ExecuteStoreProcedure("p_DeleteDuplicatePunches");

                //Determine Attendancedate
                _amsConn.ClearParameters();
                result = _amsConn.ExecuteStoreProcedure("p_DetermineAttendanceDate");

                rename(_atndFilePath);

                _logger.Log($"Total records need to insert to LMS {logdata.Count}, Inserted record {insRec}, Error records {errRec}");
            }
            catch(Exception ex)
            {
                errRec++;
                _logger.Log($"Exception while posting data to database: {ex.Message}");
            }
        }

        private void rename(String strPath)
        {
            try
            {
                String fileName = $"{DateTime.Now.ToString("yyyyMMddHHmm")}.dat";
                String filePath = $"{AppDomain.CurrentDomain.BaseDirectory}";
                String newFilePath = Path.Combine(filePath, fileName);
                File.Move(strPath, newFilePath);
            }
            catch (Exception exception)
            {
                _logger.Error($"Error while renaming the ATND.DAT File  {exception.Message}", exception);
            }

        }

        private String errorMessage(int resultvalue)
        {
            String strMessage = String.Empty;
            switch (resultvalue)
            {
                case 1:
                    strMessage = "Invalied Shift";
                    break;
                case 2:
                    strMessage = "Invalied EmpNo";
                    break;
                case 3:
                    strMessage = "Invalied Date";
                    break;
                case 4:
                    strMessage = "Invalied Time";
                    break;
                case 5:
                    strMessage = "Invalied I/O Flag";
                    break;
                case 6:
                    strMessage = "Duplicate Punch";
                    break;
                default:
                    strMessage = "";
                    break;
            }
            return strMessage;
        }
    }
}
