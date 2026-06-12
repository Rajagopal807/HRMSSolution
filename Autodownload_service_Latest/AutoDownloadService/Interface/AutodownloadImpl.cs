using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutodownloadService.Model;
using AutodownloadService.Interface.Unit;
using System.Configuration;

namespace AutodownloadService.Interface
{
    public class AutodownloadImpl : IAutodownload
    {
        public void Compute()
        {
            Computation computation = new Computation();
            computation.ComputeAttendance();
        }

        public void CreateAtndFile(List<RawDatum> rawData)
        {
            CreateAtndFileUnit atndFile = new CreateAtndFileUnit(rawData);
            atndFile.execute();
        }

        public List<RawDatum> DownloadFromEpush()
        {
            string downloadType = ConfigurationManager.ConnectionStrings["DownloadType"].ConnectionString;
            DownloadFromEpushUnit download = new DownloadFromEpushUnit(downloadType);
            if (downloadType.ToUpper() == "SQL")
            {
                return download.Execute();
            }
            else
            {
                return download.ExecuteDevice();
            }
        }

        public void PostDataToDB()
        {
            PostDataToDBUnit postDataToDBUnit = new PostDataToDBUnit();
            postDataToDBUnit.PostData();
        }
    }
}
