using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HRMS.Domain.Enums;

namespace HRMS.Domain.Entities
{
    [Table("TblEmpMast")]
    public class Employee : BaseEntity
    {
        [Key] // Primary Key
        [DatabaseGenerated(DatabaseGeneratedOption.None)] //Important (no auto increment)
        public string EmployeeId { get; set; } 

        public string EmployeeName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string DepartmentId { get; set; }
        public string DesignationId { get; set; }
        public DateTime DateOfJoining { get; set; }
        public DateTime DateOfBirth { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;

        // Navigation
        public virtual Department Department { get; set; }
        public virtual Designation Designation { get; set; }
        public virtual ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
    }
}
