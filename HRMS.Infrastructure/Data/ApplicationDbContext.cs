using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using Microsoft.AspNet.Identity.EntityFramework;
using HRMS.Domain.Entities;
using HRMS.Domain.Enums;
using System;

namespace HRMS.Infrastructure.Data
{
    // ─── Identity User ────────────────────────────────────────────────────────
    public class ApplicationUser : IdentityUser
    {
        public string UserID { get; set; }
        public string EmployeeId { get; set; }
        public string FullName { get; set; }
        public UserRole Role { get; set; }
        public string ResetToken { get; set; }
        public DateTime? TokenExpiry { get; set; }
        public bool IsActive { get; set; } = true;
    }

    // ─── ApplicationRole ─────────────────────────────────────────────────────
    public class ApplicationRole : IdentityRole
    {
        public ApplicationRole() { }
        public ApplicationRole(string name) : base(name) { }
    }

    // ─── DbContext ────────────────────────────────────────────────────────────
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext()
            : base("DefaultConnection", throwIfV1Schema: false) { }

        public static ApplicationDbContext Create() => new ApplicationDbContext();

        public DbSet<Department> Departments { get; set; }
        public DbSet<Designation> Designations { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Muster> Muster { get; set; }
        public DbSet<DailyTransactions> DailyTransactions { get; set; }
        public DbSet<LeaveTypeMaster> LeaveTypeMasters { get; set; }
        public DbSet<LeaveApplication> LeaveApplications { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

            // UNIQUE CONSTRAINTS
            modelBuilder.Entity<Department>()
                .Property(d => d.DepartmentName)
                .IsRequired()
                .HasMaxLength(100);

            modelBuilder.Entity<Designation>()
                .Property(d => d.DesignationName)
                .IsRequired()
                .HasMaxLength(100);

            modelBuilder.Entity<Employee>()
                .Property(e => e.EmployeeId)
                .IsRequired()
                .HasMaxLength(11);

            modelBuilder.Entity<LeaveTypeMaster>()
                .Property(e => e.LeaveTypeID)
                .IsRequired()
                .HasMaxLength(2);

            modelBuilder.Entity<LeaveApplication>()
                .HasKey(a => a.ApplicationId)
                .Map(m => m.ToTable("TblLeaveApplications"));

            // ApplicationId  → IDENTITY (the real auto-increment PK)
            modelBuilder.Entity<LeaveApplication>()
                .Property(a => a.ApplicationId)
                .HasDatabaseGeneratedOption(
                    System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.Identity);

            // Id (from BaseEntity) → NOT identity for this table — avoids the
            // "Multiple identity columns" SQL Server error.
            modelBuilder.Entity<LeaveApplication>()
                .Property(a => a.Id)
                .HasDatabaseGeneratedOption(
                    System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.None);

            // Required fields
            modelBuilder.Entity<LeaveApplication>()
                .Property(a => a.EmployeeId)
                .IsRequired()
                .HasMaxLength(11);

            modelBuilder.Entity<LeaveApplication>()
                .Property(a => a.LeaveTypeMasterId)
                .IsRequired()
                .HasMaxLength(2);

            // ==============================
            // CORRECT UNIQUE INDEX
            // ==============================
            modelBuilder.Entity<LeaveApplication>()
                .HasIndex(a => new
                {
                    a.EmployeeId,
                    a.FromDate,
                    a.Session,
                    a.IsDeleted
                })
                .IsUnique();

            base.OnModelCreating(modelBuilder);
        }
    }
}
