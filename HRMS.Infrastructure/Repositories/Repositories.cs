using HRMS.Domain.Entities;
using HRMS.Domain.Enums;
using HRMS.Domain.Interfaces;
using HRMS.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Web;

namespace HRMS.Infrastructure.Reports
{
    // ─── Generic Repository ───────────────────────────────────────────────────
    public class Repository<T> : IRepository<T> where T : BaseEntity
    {
        protected readonly ApplicationDbContext _ctx;
        protected readonly DbSet<T> _set;

        public Repository(ApplicationDbContext ctx)
        {
            _ctx = ctx;
            _set = ctx.Set<T>();
        }

        public T GetById(int id) => _set.FirstOrDefault(e => e.Id == id && !e.IsDeleted);

        public IEnumerable<T> GetAll() => _set.Where(e => !e.IsDeleted).ToList();

        public IEnumerable<T> Find(Expression<Func<T, bool>> predicate)
            => _set.Where(e => !e.IsDeleted).Where(predicate).ToList();

        public void Add(T entity) { _set.Add(entity); }

        public void Update(T entity)
        {
            _ctx.Entry(entity).State = EntityState.Modified;
        }

        public void Delete(int id)
        {
            var entity = GetById(id);
            if (entity != null)
            {
                entity.IsDeleted = true;
                entity.UpdatedAt = DateTime.UtcNow;
                Update(entity);
            }
        }

        public int Count(Expression<Func<T, bool>> predicate = null)
        {
            var q = _set.Where(e => !e.IsDeleted);
            return predicate == null ? q.Count() : q.Count(predicate);
        }
    }

    // ─── Employee Repository ──────────────────────────────────────────────────
    public class EmployeeRepository : Repository<Employee>, IEmployeeRepository
    {
        public EmployeeRepository(ApplicationDbContext ctx) : base(ctx) { }

        public IEnumerable<Employee> GetByDepartment(Department department)
        {

                return _set.Where(e => !e.IsDeleted && e.DepartmentId == department.DepartmentId).ToList();
            //return Enumerable.Empty<Employee>();
        }

        public IEnumerable<Employee> GetActiveEmployees()
            => _set.Where(e => !e.IsDeleted && e.Status == EmployeeStatus.Active).ToList();


        public IEnumerable<Employee> GetWithLeaves()
            => _set.Include(e => e.LeaveRequests).Where(e => !e.IsDeleted).ToList();

        public IDictionary<string, int> GetDepartmentHeadcount()
            => _set.Where(e => !e.IsDeleted && e.Status == EmployeeStatus.Active)
                   .GroupBy(e => e.DepartmentId)
                   .ToDictionary(g => g.Key.ToString(), g => g.Count());

        public Employee GetByEmployeeID(string employeeId) => _set.FirstOrDefault(e => e.EmployeeId == employeeId && !e.IsDeleted);

        public void DeleteEmployeeID(string employeeID)
        {
            var entity = GetByEmployeeID(employeeID);
            if (entity != null)
            {
                entity.IsDeleted = true;
                entity.UpdatedAt = DateTime.UtcNow;
                Update(entity);
            }
        }
    }

    // ─── Department Repository ──────────────────────────────────────────────────
    public class DepartmentRepository : Repository<Department>, IDepartmentRepository
    {
        public DepartmentRepository(ApplicationDbContext ctx) : base(ctx) { }

        public void DeleteDepartmentID(string departmentID)
        {
            var entity = GetByDepartmentID(departmentID);
            if (entity != null)
            {
                entity.IsDeleted = true;
                entity.UpdatedAt = DateTime.UtcNow;
                Update(entity);
            }
        }

        public IEnumerable<Department> GetActiveDepartments()
            => _set.Where(e => e.IsActive == true).ToList();

        public Department GetByDepartmentID(string departmentID) 
            => _set.FirstOrDefault(e => e.DepartmentId == departmentID && !e.IsDeleted);
    }

    // ─── Deignation Repository ──────────────────────────────────────────────────
    public class DesignationRepository : Repository<Designation>, IDesignationRepository
    {
        public DesignationRepository(ApplicationDbContext ctx) : base(ctx) { }

        public void DeleteDesignationID(string designationID)
        {
            var entity = GetByDesignationID(designationID);
            if (entity != null)
            {
                entity.IsDeleted = true;
                entity.UpdatedAt = DateTime.UtcNow;
                Update(entity);
            }
        }

        public IEnumerable<Designation> GetActiveDesignations()
            => _set.Where(e => e.IsActive == true).ToList();

        public Designation GetByDesignationID(string designationID) 
            => _set.FirstOrDefault(e => e.DesignationID == designationID && !e.IsDeleted);
    }

    // ─── Attendance Report Repository ─────────────────────────────────────────────────────
    /// <summary>
    /// OCP: New repository added by extension — Repositories.cs untouched.
    /// </summary>
    public class AttendanceRepository : Repository<Muster>, IAttendanceRepository
    {
        public AttendanceRepository(ApplicationDbContext ctx) : base(ctx) { }

        public IEnumerable<Muster> GetByDateRange(DateTime from, DateTime to)
        {
            return _set
                .Include(a => a.Employee)
                .Where(a => !a.IsDeleted && a.TDate  >= from && a.TDate  <= to)
                .OrderBy(a => a.Employee.EmployeeId)
                .ThenBy(a => a.TDate )
                .ToList();
        }

        public IEnumerable<Muster> GetByEmployee(
            string employeeId, DateTime from, DateTime to)
        {
            return _set
                .Include(a => a.Employee)
                .Where(a => !a.IsDeleted
                         && a.EmployeeId == employeeId
                         && a.TDate  >= from
                         && a.TDate  <= to)
                .OrderBy(a => a.TDate )
                .ToList();
        }
    }


    // ─── Leave Repository ─────────────────────────────────────────────────────
    public class AuditRepository : Repository<AuditLog>, IAuditService
    {
        public AuditRepository(ApplicationDbContext ctx) : base(ctx) { }

        public void Log(string action, string module, string details = null)
        {
            var http = HttpContext.Current;

            var log = new AuditLog
            {
                UserId = http?.Session["UserId"]?.ToString(),
                UserName = http?.Session["UserName"]?.ToString(),
                Action = action,
                Module = module,
                Details = details,
                IpAddress = IpHelper.GetClientIp(http?.Request)
            };

            _ctx.AuditLogs.Add(log);
            _ctx.SaveChanges();
        }
    }

    // ── Leave Type Master Repository ──────────────────────────────────────────
    public class LeaveTypeMasterRepository : Repository<LeaveTypeMaster>, ILeaveTypeMasterRepository
    {
        public LeaveTypeMasterRepository(ApplicationDbContext ctx) : base(ctx) { }

        public IEnumerable<LeaveTypeMaster> GetActive()
            => _set.Where(l => !l.IsDeleted && l.IsActive)
                   .OrderBy(l => l.Name)
                   .ToList();

        public LeaveTypeMaster GetByCode(string code)
            => _set.FirstOrDefault(l => !l.IsDeleted
                                     && l.LeaveTypeID == code.ToUpper().Trim());

        public bool CodeExists(string code, string excludeId = "0")
            => _set.Any(l => !l.IsDeleted
                          && l.LeaveTypeID == code.ToUpper().Trim()
                          && l.LeaveTypeID != excludeId);

        public LeaveTypeMaster GetByLeaveTypeID(string leaveTypeID) => _set.FirstOrDefault(e => e.LeaveTypeID == leaveTypeID && !e.IsDeleted);

        public void DeleteLeaveTypeID(string leaveTypeID)
        {
            var entity = GetByLeaveTypeID(leaveTypeID);
            if (entity != null)
            {
                entity.IsDeleted = true;
                entity.UpdatedAt = DateTime.UtcNow;
                Update(entity);
            }
        }
    }

    // ── Leave Application Repository ──────────────────────────────────────────
    public class LeaveApplicationRepository : Repository<LeaveApplication>, ILeaveApplicationRepository
    {
        public LeaveApplicationRepository(ApplicationDbContext ctx) : base(ctx) { }

        public IEnumerable<LeaveApplication> GetAll()
            => _ctx.LeaveApplications
                   .Include(a => a.Employee)
                   .Include(a => a.LeaveTypeMaster)
                   .Where(a => a.IsDeleted == false)
                   .OrderByDescending(a => a.ApplicationId)
                   .ToList();

        public IEnumerable<LeaveApplication> GetPending()
            => _ctx.LeaveApplications
                   .Include(a => a.Employee)
                   .Include(a => a.LeaveTypeMaster)
                   .Where(a => a.Status == LeaveStatus.Pending)
                   .OrderBy(a => a.ApplicationId)
                   .ToList();

        public IEnumerable<LeaveApplication> GetByEmployee(string employeeId)
            => _ctx.LeaveApplications
                   .Include(a => a.Employee)
                   .Include(a => a.LeaveTypeMaster)
                   .Where(a => a.EmployeeId == employeeId)
                   .OrderByDescending(a => a.ApplicationId)
                   .ToList();

        public LeaveApplication GetByApplicationId(int applicationId)
            => _ctx.LeaveApplications
                   .Include(a => a.Employee)
                   .Include(a => a.LeaveTypeMaster)
                   .FirstOrDefault(a => a.ApplicationId == applicationId);

        public void DeleteLeaveApplicationID(int applicationId)
        {
            var entity = GetByApplicationId(applicationId);
            if (entity != null)
            {
                entity.IsDeleted = true;
                entity.UpdatedAt = DateTime.UtcNow;
                Update(entity);
            }
        }

        public void CallCreateMusterSP(string employeeId, DateTime fromDate)
        {
            var empParam = new SqlParameter("@empid", employeeId ?? "");
            var dateParam = new SqlParameter("@fromDate", fromDate);

            _ctx.Database.ExecuteSqlCommand(
                "EXEC CreateMusterServiceProc @fromDate, @empid",
                dateParam,
                empParam
            );
        }
    }

    // ─── Unit of Work ─────────────────────────────────────────────────────────
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _ctx;
        private EmployeeRepository _employees;
        private LeaveTypeMasterRepository _leaveTypeMasters;
        private LeaveApplicationRepository _leaveApplications;
        private DepartmentRepository _departments;
        private DesignationRepository _designations;
        private AuditRepository _auditRepository;
        private AttendanceRepository _attendanceRepository;

        public UnitOfWork(ApplicationDbContext ctx) { _ctx = ctx; }

        public IEmployeeRepository Employees
        {
            get { return _employees ?? (_employees = new EmployeeRepository(_ctx)); }
        }

        public ILeaveTypeMasterRepository LeaveTypesMasters
        {
            get { return _leaveTypeMasters ?? (_leaveTypeMasters = new LeaveTypeMasterRepository(_ctx)); }
        }

        public ILeaveApplicationRepository LeaveApplications
        {
            get { return _leaveApplications ?? (_leaveApplications = new LeaveApplicationRepository(_ctx)); }
        }

        public IDepartmentRepository Departments
        {
            get { return _departments ?? (_departments = new DepartmentRepository(_ctx)); }
        }

        public IDesignationRepository Designations
        {
            get { return _designations ?? (_designations = new DesignationRepository(_ctx)); }
        }

        public IAuditService Log
        {
            get { return _auditRepository ?? (_auditRepository = new AuditRepository(_ctx)); }
        }

        public IAttendanceRepository Attendace
        {
            get { return _attendanceRepository ?? (_attendanceRepository = new AttendanceRepository(_ctx)); }
        }

        public int SaveChanges() => _ctx.SaveChanges();

        public void Dispose() => _ctx.Dispose();
    }
}
