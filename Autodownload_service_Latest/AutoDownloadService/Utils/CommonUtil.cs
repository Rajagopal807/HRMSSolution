using AutodownloadService.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AutodownloadService.Utils
{
    public class CommonUtil
    {
        public static String AddZero(String value)
        {
            String strValue = value.Trim();
            for (int i = strValue.Length; i <= 10; i++)
            {
                strValue = "0" + strValue;
            }
            return strValue.Trim();
        }

        public static String AddTenZero(String value)
        {
            String strValue = value.Trim();
            for (int i = strValue.Length; i <= 9; i++)
            {
                strValue = "0" + strValue;
            }
            return strValue.Trim();
        }

        public static String SapAddZero(String value)
        {
            String strValue = value.Trim();
            for (int i = strValue.Length; i < 8; i++)
            {
                strValue = "0" + strValue;
            }
            return strValue.Trim();
        }


        public static String RemoveZero(String value)
        {
            String strValue = value.Trim();
            for (int i = 0; i <= 10; i++)
            {
                if (!strValue.Substring(i, 1).Equals("0"))
                {
                    strValue = strValue.Substring(i, strValue.Length - i);
                    break;
                }
            }
            return strValue.Trim();
        }

        public static Boolean checkFileExist(String strPath, String mode)
        {
            if (!File.Exists(strPath))
            {
                if (mode.ToUpper().Equals("DOWNLOAD") || mode.ToUpper().Equals("ERROR") || mode.ToUpper().Equals("POSTING"))
                {
                    createFile(strPath);
                }
                else
                {
                    return false;
                }
            }


            return true;
        }

        public static void createFile(String strPath)
        {
            File.Create(strPath).Close();
        }

        public static void appendToFile(String strPath, List<String> attlogs, String mode)
        {
            if (checkFileExist(strPath, mode))
            {
                using (StreamWriter writer = new StreamWriter(strPath, true))
                {
                    foreach (String attlog in attlogs)
                    {
                        writer.WriteLine(attlog);
                    }
                    writer.Close();
                }
            }
        }

        public static void appendActivityFile(String strPath, String logString, String mode)
        {
            if (checkFileExist(strPath, mode))
            {
                using (StreamWriter writer = new StreamWriter(strPath, true))
                {

                    writer.WriteLine(logString);
                    writer.Close();
                }
            }
        }

        public static List<String> readFile(String strpath, String mode)
        {
            List<String> logs = new List<string>();
            if (checkFileExist(strpath, mode))
            {
                var fileStream = new FileStream(strpath, FileMode.Open, FileAccess.Read);
                using (StreamReader reader = new StreamReader(strpath))
                {
                    String log;
                    while ((log = reader.ReadLine()) != null)
                    {
                        logs.Add(log);
                    }
                    reader.Close();
                    fileStream.Close();
                }
            }
            return logs;
        }

        public static DeviceConfig readDeviceConfig()
        {
            try
            {
                string jsonFilePah = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DeviceConfig.json");
                using (StreamReader reader = new StreamReader(jsonFilePah))
                {
                    string jsonContent = reader.ReadToEnd();
                    DeviceConfig deviceConfig = Newtonsoft.Json.JsonConvert.DeserializeObject<DeviceConfig>(jsonContent);
                    return deviceConfig;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error reading device config: " + ex.Message);
                return null;
            }
        }
    }
}
