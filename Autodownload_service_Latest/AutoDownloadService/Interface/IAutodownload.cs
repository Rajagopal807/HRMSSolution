using AutodownloadService.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutodownloadService.Interface
{
   public interface IAutodownload
    {
        List<RawDatum> DownloadFromEpush();
        void CreateAtndFile(List<RawDatum> rawData);
        void PostDataToDB();
        void Compute();
    }
}
