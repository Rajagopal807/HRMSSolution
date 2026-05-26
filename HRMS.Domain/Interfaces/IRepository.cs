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

    public interface ILeaveTypeMasterRepository : IRepository<LeaveTypeMaster>
    {
        IEnumerable<LeaveTypeMaster> GetActive();
        LeaveTypeMaster GetByCode(string code);
        bool CodeExists(string code, string excludeId = "0");
        LeaveTypeMaster GetByLeaveTypeID(string leaveTypeID);
        void DeleteLeaveTypeID(string leaveTypeID);

    }

    public interface ILeaveApplicationRepository : IRepository<LeaveApplication>
    {
        IEnumerable<LeaveApplication> GetAllLeaveApplication();
        IEnumerable<LeaveApplication> GetByEmployee(string employeeId);
        IEnumerable<LeaveApplication> GetPending();
        LeaveApplication GetByApplicationId(int applicationId);
        void DeleteLeaveApplicationID(int applicationId);
        void CallCreateMusterSP(string employeeId, DateTime fromDate);
    }

    public interface IAuditService
    {
        void Log(string action, string module, string details = null);
    }

    public interface IAttendanceRepository : IRepository<Muster>
    {
        /// <summary>
        /// Returns all punch records for every employee within the date range.
        /// Used to build the attendance register grid.
        /// </summary>
        IEnumerable<Muster> GetByDateRange(DateTime from, DateTime to);

        /// <summary>Returns punch records for one employee within the date range.</summary>
        IEnumerable<Muster> GetByEmployee(string employeeId, DateTime from, DateTime to);
        IEnumerable<DailyTransactions> GetDailyTransactions(string employeeId, DateTime from, DateTime to);
        IEnumerable<DailyTransactions> GetDailyTransactionsForDay(string employeeId, DateTime attendanceDate);
        DailyTransactions GetDailyTransaction(string employeeId, string ioFlag, DateTime transTime);
        void AddDailyTransaction(DailyTransactions transaction);
        void UpdateDailyTransaction(DailyTransactions transaction);
        void DeleteDailyTransaction(DailyTransactions transaction);
    }

    public interface ITempCardRepository : IRepository<TempCard>
    {
        IEnumerable<TempCard> GetAllTempCards();
        TempCard GetByTempCard(string Tempcardno);
        bool TempCardIdExists(string tempCardId, string excludeId = "0");
        bool EmployeeAlreadyHasCard(string employeeId, string excludeId = "0");
        void DeleteByTempcard(string id);

    }

    public interface IUnitOfWork : IDisposable
    {
        IEmployeeRepository Employees { get; }
        IDepartmentRepository Departments { get; }
        IDesignationRepository Designations { get; }
        ILeaveTypeMasterRepository LeaveTypesMasters { get; }
        ILeaveApplicationRepository LeaveApplications { get; }
        IAuditService Log { get; }
        IAttendanceRepository Attendace { get; }
        ITempCardRepository TempCard { get; }
        int SaveChanges();
    }
}
