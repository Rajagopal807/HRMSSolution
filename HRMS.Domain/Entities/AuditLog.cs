using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.Domain.Entities
{
    [Table("TblAuditLog")]
    public class AuditLog : BaseEntity
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Action { get; set; }     // Login, Create, Edit, Delete
        public string Module { get; set; }     // Employee, Department
        public string Details { get; set; }    // JSON / Description
        public string IpAddress { get; set; }
    }
}
