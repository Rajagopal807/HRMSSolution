using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRMS.Domain.Entities
{
    /// <summary>
    /// Base entity — Open/Closed principle: extend by inheriting, don't modify this class.
    /// </summary>
    public abstract class BaseEntity
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}
