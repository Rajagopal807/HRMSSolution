using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.Domain.Entities
{
    /// <summary>
    /// Stores a single punch-in / punch-out record for one employee on one day.
    /// One employee can have multiple logs per day (in/out pairs).
    /// </summary>
    [Table("TblMuster")]
    public class Muster : BaseEntity
    {
        [Key, Column(Order = 0)] // Primary Key
        [DatabaseGenerated(DatabaseGeneratedOption.None)] //Important (no auto increment)
        public string EmployeeId { get; set; }
        [Key, Column(Order = 1)] // Primary Key
        public DateTime TDate  { get; set; }

        [StringLength(2)]
        public string ShiftId { get; set; }

        [StringLength(2)]
        public String AttId { get; set; }

        [StringLength(2)]
        public string LeaveTypeId { get; set; }

        public string HrsWorked { get; set; }

        public string OutPasses { get; set; }

        public string ErrCodeId { get; set; }

        public string SingleOT { get; set; }

        public string DoubleOT { get; set; }

        public string LatePunch { get; set; }

        public string EarlyOut { get; set; }

        public string ExtraHours { get; set; }

        public string CompOff { get; set; }

        public string PersonlaHrs { get; set; }
        
        public string Availed { get; set; }

        public string Applied { get; set; }

        [StringLength(20)]
        public string FirstIn { get; set; }

        [StringLength(20)]
        public string LastOut { get; set; }

        public string ShortAdj { get; set; }

        // Navigation
        public virtual Employee Employee { get; set; }
    }
}
