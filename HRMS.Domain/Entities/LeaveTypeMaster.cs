using HRMS.Domain.Entities;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.Domain.Entities
{
    [Table("TblLeaveTypes")]
    /// <summary>
    /// Configurable leave type defined by Admin / HR Manager.
    /// Replaces the hard-coded LeaveType enum for the Apply Leave screen.
    /// The enum still exists for seeded/legacy data; new records use this entity.
    /// </summary>
    public class LeaveTypeMaster : BaseEntity
    {
        [Key, Column(Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)] //Important (no auto increment)
        [StringLength(2)]
        /// <summary>Short code shown in dropdowns. e.g. "AL", "SL", "ML"</summary>
        public string LeaveTypeID { get; set; }

        /// <summary>e.g. "Annual Leave", "Sick Leave", "Maternity Leave"</summary>
        public string Name { get; set; }

        /// <summary>Maximum days allowed per year. 0 = unlimited.</summary>
        public int MaxDaysPerYear { get; set; }

        /// <summary>Whether half-day application is allowed for this type.</summary>
        public bool AllowHalfDay { get; set; } = true;

        /// <summary>Carry-forward to next year allowed?</summary>
        public bool IsCarryForward { get; set; } = false;

        /// <summary>Whether this leave type is currently active in the system.</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>Optional description shown to employees.</summary>
        public string Description { get; set; }

        // Navigation
        public virtual ICollection<LeaveApplication> LeaveApplications { get; set; }
            = new List<LeaveApplication>();
    }
}