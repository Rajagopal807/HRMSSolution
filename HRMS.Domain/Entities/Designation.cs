using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HRMS.Domain.Entities
{
    public class Designation : BaseEntity
    {
        [Key] // Primary Key
        [DatabaseGenerated(DatabaseGeneratedOption.None)] //Important (no auto increment)
        public string DesignationID { get; set; }
        public string DesignationName { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public virtual ICollection<Employee> Employees { get; set; }
    }
}
