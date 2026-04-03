using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using HRMS.Domain.Entities;

namespace HRMS.Domain.Interfaces
{
    /// <summary>
    /// Generic repository contract — Open/Closed: closed for modification, open for extension.
    /// </summary>
    public interface IRepository<T> where T : BaseEntity
    {
        T GetById(int id);
        IEnumerable<T> GetAll();
        IEnumerable<T> Find(Expression<Func<T, bool>> predicate);
        void Add(T entity);
        void Update(T entity);
        void Delete(int id);
        int Count(Expression<Func<T, bool>> predicate = null);
    }

    public interface IEmployeeRepository : IRepository<Employee>
    {
        Employee GetByEmployeeID(string employeeID);
        IEnumerable<Employee> GetByDepartment(Department department);
        IEnumerable<Employee> GetActiveEmployees();
        IEnumerable<Employee> GetWithLeaves();
        IDictionary<string, int> GetDepartmentHeadcount();
        void DeleteEmployeeID(string employeeID);

    }

    public interface IDepartmentRepository : IRepository<Department>
    {
        Department GetByDepartmentID(string departmentID);
        IEnumerable<Department> GetActiveDepartments();
        void DeleteDepartmentID(string departmentID);
    }

    public interface IDesignationRepository : IRepository<Designation>
    {
        Designation GetByDesignationID(string designationID);
        IEnumerable<Designation> GetActiveDesignations();
        void DeleteDesignationID(string designationID);
    }

    public interface ILeaveRepository : IRepository<LeaveRequest>
    {
        IEnumerable<LeaveRequest> GetByEmployee(string employeeId);
        IEnumerable<LeaveRequest> GetPending();
        IEnumerable<LeaveRequest> GetByDateRange(DateTime from, DateTime to);
        IDictionary<string, int> GetLeaveCountByType();
    }

    public interface IAuditService
    {
        void Log(string action, string module, string details = null);
    }

    public interface IUnitOfWork : IDisposable
    {
        IEmployeeRepository Employees { get; }
        IDepartmentRepository Departments { get; }
        IDesignationRepository Designations { get; }
        ILeaveRepository Leaves { get; }
        IAuditService Log { get; }
        int SaveChanges();
    }
}
