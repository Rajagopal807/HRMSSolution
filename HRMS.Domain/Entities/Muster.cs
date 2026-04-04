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
        [Key] // Primary Key
        [DatabaseGenerated(DatabaseGeneratedOption.None)] //Important (no auto increment)
        public int EmployeeId { get; set; }
        public DateTime TDate  { get; set; }

        [StringLength(2)]
        public string ShiftId { get; set; }

        [StringLength(2)]
        public string AttId { get; set; }

        [StringLength(2)]
        public string LeaveTypeId { get; set; }

        public int HrsWorked { get; set; }

        public int OutPasses { get; set; }

        public int ErrCodeId { get; set; }

        public int SingleOT { get; set; }

        public int DoubleOT { get; set; }

        public int LatePunch { get; set; }

        public int EarlyOut { get; set; }

        public int ExtraHours { get; set; }

        public int CompOff { get; set; }

        public int PersonlaHrs { get; set; }

        public DateTime Availed { get; set; }

        public DateTime Applied { get; set; }

        [StringLength(20)]
        public string FirstIn { get; set; }

        [StringLength(20)]
        public string LastOut { get; set; }

        public int? ShortAdj { get; set; }

        // Navigation
        public virtual Employee Employee { get; set; }
    }
}
