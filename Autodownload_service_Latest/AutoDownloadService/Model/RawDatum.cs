using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutodownloadService.Model
{
    public class RawDatum
    {
        public String Userid { get; set; }
        public String DeviceId { get; set; }
        public String Direction { get; set; }
        public Int32 C4 { get; set; }
        public String LogDate { get; set; }
        public String TableName { get; set; }
    }
}
