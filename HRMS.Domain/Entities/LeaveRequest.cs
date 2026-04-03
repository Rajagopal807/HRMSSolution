using System;
using HRMS.Domain.Enums;

namespace HRMS.Domain.Entities
{
    public class LeaveRequest : BaseEntity
    {
        public string EmployeeId { get; set; }
        public LeaveType LeaveType { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int TotalDays => (int)(ToDate - FromDate).TotalDays + 1;
        public string Reason { get; set; }
        public LeaveStatus Status { get; set; } = LeaveStatus.Pending;
        public string ReviewedBy { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string ReviewComments { get; set; }

        // Navigation
        public virtual Employee Employee { get; set; }
    }
}
