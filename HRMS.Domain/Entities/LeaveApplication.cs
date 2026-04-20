using HRMS.Domain.Entities;
using HRMS.Domain.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.Domain.Entities
{
    public enum LeaveSession
    {
        FullDay = 1,
        FirstHalf = 2,
        SecondHalf = 3
    }

    [Table("TblLeaveApplications")]
    /// <summary>
    /// A leave application submitted by or on behalf of an employee.
    /// Uses LeaveTypeMaster (DB-driven) instead of the hard-coded LeaveType enum.
    /// Supports FullDay / FirstHalf / SecondHalf selection.
    /// </summary>
    public class LeaveApplication : BaseEntity
    {
        [Key, Column(Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)] //Important (no auto increment)
        [StringLength(11)]
        public string EmployeeId { get; set; }
        public string LeaveTypeMasterId { get; set; }

        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public LeaveSession Session { get; set; } = LeaveSession.FullDay;

        /// <summary>
        /// Calculated days, accounting for half-day sessions.
        /// FirstHalf / SecondHalf on a single day = 0.5 days.
        /// </summary>
        public decimal TotalDays
        {
            get
            {
                int calDays = (int)(ToDate - FromDate).TotalDays + 1;
                if (calDays == 1 && Session != LeaveSession.FullDay)
                    return 0.5m;
                return calDays;
            }
        }

        public string Reason { get; set; }
        public LeaveStatus Status { get; set; } = LeaveStatus.Pending;
        public string AppliedBy { get; set; }   // UserId of submitter
        public string ReviewedBy { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string ReviewNotes { get; set; }

        // Navigation
        public virtual Employee Employee { get; set; }
        public virtual LeaveTypeMaster LeaveTypeMaster { get; set; }
    }
}