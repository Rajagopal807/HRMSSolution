using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.Domain.Entities
{

    [Table("TblDailyTransactions")]
    public class DailyTransactions : BaseEntity
    {
        [Key, Column(Order = 0)]
        [StringLength(11)]
        public string EmpId { get; set; }

        [Key, Column(Order = 1)]
        [StringLength(1)]
        public string IOFlag { get; set; }

        [Key, Column(Order = 2)]
        public DateTime TransTime { get; set; }

        [StringLength(1)]
        public string ActualIOFlag { get; set; }

        [Required]
        [StringLength(1)]
        public string OPFlag { get; set; }

        public DateTime? PunchedTime { get; set; }

        public DateTime? AttendanceDate { get; set; }

        public int? BOutPassHrs { get; set; }

        [StringLength(25)]
        public string Remarks { get; set; }

        public int? BadgeReaderNo { get; set; }

        [StringLength(1)]
        public string Deleted { get; set; }

        [StringLength(2)]
        public string ReasonCode { get; set; }
    }
}
