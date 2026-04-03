using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HRMS.Domain.Entities
{
    public class DailyTransactions : BaseEntity
    {
        public string EmpId { get; set; }
        public string IOFlag { get; set; }
        public string PunchedTime { get; set; }
        public string AttendanceDate { get; set; }
        public string BadgeReaderNo { get; set; }
    }
}
