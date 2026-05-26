using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HRMS.Application.DTOs;
using HRMS.Domain.Entities;

namespace HRMS.Application.Interfaces
{
    /// <summary>
    /// Service contracts — Open/Closed: new features add new methods or new services,
    /// existing contracts remain unchanged.
    /// </summary>
    public interface IEmployeeService
    {
        IEnumerable<EmployeeDto> GetAll();
        IEnumerable<EmployeeDto> GetActive();
        IEnumerable<EmployeeDto> GetByDepartment(string department);
        EmployeeDto GetById(string id);
        string Create(CreateEmployeeDto dto);
        void Update(string id, CreateEmployeeDto dto);
        void Delete(string id);
        IEnumerable<EmployeeDto> Search(string query, string department, string status);
    }

    public interface IDepartmentService
    {
        IEnumerable<DepartmentDto> GetAll();
        IEnumerable<DepartmentDto> GetActive();
        DepartmentDto GetById(string id);
        string Create(DepartmentDto dto);
        void Update(string id, DepartmentDto dto);
        void Delete(string id);
        IEnumerable<DepartmentDto> Search(string query, string department, string status);
    }

    public interface IDesignationService
    {
        IEnumerable<DesignationDto> GetAll();
        IEnumerable<DesignationDto> GetActive();
        DesignationDto GetById(string id);
        string Create(DesignationDto dto);
        void Update(string id, DesignationDto dto);
        void Delete(string id);
        IEnumerable<DesignationDto> Search(string query, string designation, string status);
    }

    public interface ILeaveService
    {
        IEnumerable<LeaveRequestDto> GetAll();
        IEnumerable<LeaveRequestDto> GetPending();
        IEnumerable<LeaveRequestDto> GetByEmployee(string employeeId);
        LeaveRequestDto GetById(int id);
        string Create(string employeeId, string leaveType, System.DateTime from, System.DateTime to, string reason);
        void Approve(int id, string reviewerId, string comments);
        void Reject(int id, string reviewerId, string comments);
    }

    public interface IDashboardService
    {
        DashboardDto GetDashboard();
    }

    public interface IReportService
    {
        /// <summary>
        /// OCP: New report types can be added by implementing this interface
        /// without changing existing report logic.
        /// </summary>
        List<IGrouping<string, ReportDto>> GetAttendanceReport(DateTime? from, DateTime? to);
        byte[] ExportPdf(DateTime? from, DateTime? to);
        byte[] ExportExcel(DateTime? from, DateTime? to);
    }

   public interface IUserSession
    {
        string UserId { get; }
        string UserName { get; }
        string Role { get; }
        bool IsAuthenticated { get; }
        DateTime? LastActivity { get; }
        void Create(string userId, string userName, string role);
        void UpdateActivity();
        void Clear();
    }

    public interface IPasswordResetService
    {
        Task<string> GenerateResetLinkAsync(string userId, string baseUrl);
        Task<bool> ResetPasswordAsync(string token, string newPassword);
    }

    public interface IIdentityService
    {
        Task<bool> UpdateResetTokenAsync(string userId, string token, DateTime expiry);
        Task<string> GetUserIdByTokenAsync(string token);
        Task<bool> ResetPasswordAsync(string userId, string newPassword);
        Task<bool> IsTokenValidAsync(string token);
        Task InvalidateTokenAsync(string userId);
    }

    public interface IReportScreenService
    {
        /// <summary>Returns all data needed to render the report filter screen.</summary>
        ReportScreenDto GetScreenData();

        /// <summary>
        /// Generates the report bytes for the given filter.
        /// Returns (bytes, contentType, fileName).
        /// </summary>
        (byte[] Bytes, string ContentType, string FileName) Generate(ReportFilterDto filter);
    }

    public interface ITempCardService
    {
        IEnumerable<TempCardDto> GetAll();
        TempCardDto GetById(string id);

        /// <summary>Returns (id, errorMessage). error is null on success.</summary>
        (string Id, string Error) Create(SaveTempCardDto dto);

        /// <summary>Returns error message or null on success.</summary>
        string Update(string id, SaveTempCardDto dto);

        void Delete(string id);

        bool TempCardIdExists(string tempCardId, string excludeId = "0");
        bool EmployeeAlreadyHasCard(string employeeId, string excludeId = "0");

        // Employee list for the dropdown
        IEnumerable<EmployeeDropdownDto> GetEmployees();
    }

    public interface IAttendanceTransactionService
    {
        AttendanceTransactionScreenDto GetScreen(string employeeId, int? month, int? year);
        EditPunchesScreenDto GetPunchScreen(string employeeId, DateTime attendanceDate, string statusMessage = null);
        string AddPunch(SaveManualPunchDto dto);
        string UpdatePunch(SaveManualPunchDto dto);
        string DeletePunch(string employeeId, string ioFlag, DateTime transTime);
    }


}
