using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HRMS.Domain.Entities
{
    [Table("TblTempCards")]
    public class TempCard : BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [StringLength(11)]
        public string TempCardNo { get; set; }

        [StringLength(11)]
        public string EmpId { get; set; }

        // Navigation Property
        [ForeignKey("EmpId")]
        public virtual Employee Employee { get; set; }
    }
}
