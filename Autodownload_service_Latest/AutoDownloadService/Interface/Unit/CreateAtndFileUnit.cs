using AutodownloadService.Model;
using AutodownloadService.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace AutodownloadService.Interface.Unit
{
    public class CreateAtndFileUnit
    {
        private IMessageLogger _logger;
        private List<RawDatum> _rawData;
        public CreateAtndFileUnit(List<RawDatum> rawData)
        {
            _logger = new Filelogger("CreateAtndFileUnit");
            _rawData = rawData;
        }

        public void execute()
        {
            int i = 1;
            List<String> logData = new List<string>();
            try
            {
                foreach (RawDatum rawDatum in _rawData)
                {
                    try
                    {
                        String empid = rawDatum.Userid;
                        String lDateTime = DateTime.Parse(rawDatum.LogDate).ToString("yyyyMMddHHmm");
                        String actualDateTime = DateTime.Parse(rawDatum.LogDate).ToString("yyyy-MM-dd HH:mm:ss");
                        String badgeReaderNo = String.Format("{0:000}", Convert.ToInt32(rawDatum.DeviceId));
                        String ioFlag = "I";
                        empid = CommonUtil.RemoveZero(empid);
                        if (!String.IsNullOrEmpty(rawDatum.Direction))
                        {
                            int value = rawDatum.C4;
                            ioFlag = (value % 2 == 0) ? "I" : "O";
                        }

                        //"0000010" & Empid & " " & Format(Rstemp.Fields("LogDate"), "yyyyMMddHHmm") & Pdirection & Format(i, "0000") & Format(Type1, "000")
                        String strLine = $"0000010{CommonUtil.AddTenZero(empid)} {lDateTime}{ioFlag}{String.Format("{0:0000}", i)}{String.Format("{0:000}", Convert.ToInt32(badgeReaderNo))}";

                        logData.Add(strLine);
                        i++;
                    }
                    catch (Exception exception)
                    {
                        String jSonString = JsonConvert.SerializeObject(rawDatum);
                        _logger.WriteError($"Error Converting RawDatum object to String : {jSonString} || {exception.Message}");
                    }
                }

                if (logData.Count == 0)
                {
                    return;
                }


                String fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ATND.DAT");

                CommonUtil.appendToFile(fileName, logData, "download");

                _logger.Log($"Atnd file created with {logData.Count} data");

            }
            catch (Exception exception)
            {
                _logger.Error("Error While Writting raw data to file",exception);
            }
        }
    }
}
