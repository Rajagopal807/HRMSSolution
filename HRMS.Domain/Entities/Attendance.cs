using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HRMS.Domain.Entities
{
    public class Attendance : BaseEntity
    {
        public string EmployeeId { get; set; }
        public DateTime Date { get; set; }
        public string FirstIN { get; set; }
        public string LastOut { get; set; }
        public string TotalHrs { get; set; }
        public string AttendanceID { get; set; }
        public bool IsPresent { get; set; }
        // Navigation
        public virtual Employee Employee { get; set; }
    }
}
