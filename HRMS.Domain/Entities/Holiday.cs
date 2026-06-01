using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.Domain.Entities
{
    [Table("TblHolidays")]
    public class Holiday : BaseEntity
    {
        [Required]
        [Column("Holiday", TypeName = "date")]
        public DateTime HolidayDate { get; set; }

        [Required]
        [StringLength(100)]
        public string HolidayName { get; set; }

        [StringLength(250)]
        public string Description { get; set; }

        public bool IsActive { get; set; } = true;

    }
}
