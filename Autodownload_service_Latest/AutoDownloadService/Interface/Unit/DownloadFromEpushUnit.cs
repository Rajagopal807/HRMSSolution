using AutodownloadService.Model;
using AutodownloadService.Utils;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using zkemkeeper;
using static AutodownloadService.Model.DeviceConfig;

namespace AutodownloadService.Interface.Unit
{
    public class DownloadFromEpushUnit
    {
        private IMessageLogger _logger;
        private EPushConnectionClass _eConn;
        private CZKEM _zKEM;
        public DownloadFromEpushUnit(string downloadType)
        {
            if(downloadType.ToUpper() == "DEVICE")
            {
                _zKEM = new CZKEM();
            }
            else
            {
                _eConn = new EPushConnectionClass();
            }
            _logger = new Filelogger("DownloadFromEpush");
        }

        public List<RawDatum> ExecuteDevice()
        {
            List<RawDatum> rawData = new List<RawDatum>();
            try
            {
                DeviceConfig deviceConfig = CommonUtil.readDeviceConfig();
                foreach(DeviceConfig.Device device in deviceConfig.ZKDevice.Devices)
                {
                    if(ConnectToDevice(device))
                    {
                        // Code to download data from device and add to rawData list
                        rawData = DownloadDataFromDevice(device);
                    }
                }
            }
            catch (Exception exception)
            {
                _logger.Error("Error while executing the DownloadFromEpushUnit ", exception);
            }
            return rawData;
        }

        private List<RawDatum> DownloadDataFromDevice(DeviceConfig.Device device)
        {
            List<RawDatum> rawData = new List<RawDatum>();
            try
            {
                // Code to download data from device and add to rawData list
                // Read all logs into device buffer first
                _zKEM.ReadAllGLogData(device.Id);

                // SSR_GetGeneralLogData iterates through buffered records
                string enrollNumber = "";
                int verifyMode = 0, inOutMode = 0, year = 0, month = 0, day = 0,
                    hour = 0, minute = 0, second = 0, workCode = 0;

                while (_zKEM.SSR_GetGeneralLogData(device.Id, out enrollNumber, out verifyMode, out inOutMode, out year, out month, out day, out hour, out minute, out second, ref workCode))
                {
                    RawDatum rawDatum = new RawDatum();
                    rawDatum.Userid = CommonUtil.AddZero(enrollNumber);
                    rawDatum.LogDate = new DateTime(year, month, day, hour, minute, second).ToString("yyyy-MM-dd HH:mm");
                    rawDatum.Direction = inOutMode == 0 ? "IN" : "OUT";
                    rawDatum.DeviceId = device.Id.ToString();
                    rawDatum.C4 = workCode;
                    rawData.Add(rawDatum);
                }

                if(device.ClearDeviceAfterSync)
                {
                    ClearDeviceLog(device);
                }

                _logger.Log($"Downloaded {rawData.Count} records from device {device.Name} at {device.IpAddress}:{device.Port}");
            }
            catch (Exception exception)
            {
                _logger.Error($"Error while downloading data from device {device.Name} at {device.IpAddress}:{device.Port} ", exception);
            }
            finally
            {
                _zKEM.Disconnect();
                _logger.Log($"Disconnected from device {device.Name} at {device.IpAddress}:{device.Port}");
            }
            return rawData;
        }

        private void ClearDeviceLog(DeviceConfig.Device device)
        {
            if (!ConnectToDevice(device)) return;
            try
            {
                _zKEM.ClearGLog(device.Id); // Assuming device ID 1; adjust as needed
                _logger.Log($"Cleared logs from device {device.Name} at {device.IpAddress}:{device.Port} after sync.");
            }
            catch (Exception ex)
            {
                _logger.Log($"[{device.Name}] ClearDeviceLog failed: {ex.Message}");
            }
        }

        private bool ConnectToDevice(DeviceConfig.Device device)
        {
            try
            {
                if (_zKEM.Connect_Net(device.IpAddress, device.Port))
                {
                    _logger.Log($"Connected to device {device.Name} at {device.IpAddress}:{device.Port}");
                    return true;
                }
                else
                {
                    _logger.WriteError($"Failed to connect to device {device.Name} at {device.IpAddress}:{device.Port}");
                    return false;
                }
            }
            catch (Exception exception)
            {
                _logger.Error($"Error while connecting to device {device.Name} at {device.IpAddress}:{device.Port} ", exception);
                return false;
            }
        }

        public List<RawDatum> Execute()
        {
            List<RawDatum> rawData = new List<RawDatum>();
            String query = "";
            try
            {
                List<String> tableNames = getDeviceData();

                foreach (String tableName in tableNames)
                {
                    int totalRec = 0;
                    Boolean isDataAvaialble = false;
                    try
                    {
                        query = $"SELECT UserId, LogDate, Direction, DeviceId, C4 FROM {tableName} WHERE (WorkCode='0' or WorkCode='' or WorkCode is null)";
                        using (SqlDataReader dataReader = _eConn.DataReader(query))
                        {
                            if (dataReader.HasRows)
                            {
                                while (dataReader.Read())
                                {
                                    isDataAvaialble = true;
                                    RawDatum rawDatum = new RawDatum();
                                    rawDatum.Userid = CommonUtil.AddZero(dataReader.GetString(0));
                                    rawDatum.LogDate = dataReader.GetDateTime(1).ToString("yyyy-MM-dd HH:mm:ss");
                                    rawDatum.Direction = dataReader.GetString(2);
                                    rawDatum.DeviceId = dataReader.GetInt32(3).ToString();
                                    rawDatum.C4 = Convert.ToInt32(dataReader.GetString(4));
                                    rawDatum.TableName = tableName;
                                    rawData.Add(rawDatum);
                                    totalRec++;
                                }
                            }
                        }
                        if (isDataAvaialble)
                        {
                            _logger.Log($"In Table {tableName} {totalRec} data is downloaded.");
                        }
                        else
                        {
                            _logger.Log($"In Table {tableName} no data is there to downloaded.");
                        }
                    }
                    catch(Exception exception)
                    {
                        _logger.WriteError($"Error While fetching the record from table {tableName} {exception.Message}");
                    }
                }


            }
            catch (Exception exception)
            {
                _logger.Error("Error while fetching data from table ", exception);
            }
            return rawData;
        }

        private List<String> getDeviceData()
        {
            Int32 monthTOCheck = 0;
            String query = "";
            List<String> tableNames = new List<string>();
            List<String> finalTableNames = new List<string>();
            try
            {
                //query = $"SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE SUBSTRING(TABLE_NAME, Len(LTRIM(RTRIM(TABLE_NAME))) - 3, 4) =  cast(YEAR(GETDATE()) as varchar(4))";
                query = $"SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE SUBSTRING(TABLE_NAME, Len(LTRIM(RTRIM(TABLE_NAME))) - 3, 4) =  cast(YEAR(GETDATE()) as varchar(4)) " +
                    $"OR SUBSTRING(TABLE_NAME, Len(LTRIM(RTRIM(TABLE_NAME))) - 3, 4) =  cast(YEAR(GETDATE())-1 as varchar(4))";
                using (SqlDataReader dataReader = _eConn.DataReader(query))
                {
                    if (dataReader.HasRows)
                    {
                        while (dataReader.Read())
                        {
                            tableNames.Add(dataReader.GetString(0));
                        }
                    }
                }

                if(!int.TryParse(ConfigurationManager.AppSettings["service.tablemonthcheck"], out monthTOCheck))
                {
                    monthTOCheck = 1;
                }

                int month = DateTime.Now.Month;
                int Year = DateTime.Now.Year;
                List<string> months = new List<string>();
                for (int i=0;i<=monthTOCheck;i++)
                {
                    string finalMonthYear = month + "-" + Year;
                    months.Add(finalMonthYear);
                    month = month - 1;
                    if(month == 0)
                    {
                        month = DateTime.Now.AddMonths(-1).Month;
                        Year = DateTime.Now.AddYears(-1).Year;
                    }

                }

                foreach(String tableName in tableNames)
                {
                    String[] splitTableName = tableName.Split('_');
                    foreach(string monthYear in months)
                    {
                        string[] splitMonthYear = monthYear.Split('-');
                        if (splitMonthYear[0].Equals(splitTableName[1]) && splitMonthYear[1].Equals(splitTableName[2]))
                        {
                            finalTableNames.Add(tableName);
                        }
                    }
                }

            }
            catch (Exception exception)
            {
                if (_eConn.cnn.State == ConnectionState.Open)
                    _eConn.cnn.Close();
                throw new Exception($"Eror While fetching list of Tables. {exception}");

            }
            finally
            {
                if (_eConn.cnn.State == ConnectionState.Open)
                    _eConn.cnn.Close();
            }

            return finalTableNames;
        }
    }
}
