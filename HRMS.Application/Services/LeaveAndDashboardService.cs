using System;
using System.Collections.Generic;
using System.Linq;
using HRMS.Application.DTOs;
using HRMS.Application.Interfaces;
using HRMS.Domain.Entities;
using HRMS.Domain.Enums;
using HRMS.Domain.Interfaces;

namespace HRMS.Application.Services
{
    // ─── Leave Service ────────────────────────────────────────────────────────
    //public class LeaveService : ILeaveService
    //{
    //    private readonly IUnitOfWork _uow;
    //    public LeaveService(IUnitOfWork uow) { _uow = uow; }

    //    public IEnumerable<LeaveRequestDto> GetAll()
    //        => _uow.Leaves.GetAll().Select(Map);

    //    public IEnumerable<LeaveRequestDto> GetPending()
    //        => _uow.Leaves.GetPending().Select(Map);

    //    public IEnumerable<LeaveRequestDto> GetByEmployee(string employeeId)
    //        => _uow.Leaves.GetByEmployee(employeeId).Select(Map);

    //    public LeaveRequestDto GetById(int id)
    //    {
    //        var l = _uow.Leaves.GetById(id);
    //        return l == null ? null : Map(l);
    //    }

    //    public string Create(string employeeId, string leaveType, DateTime from, DateTime to, string reason)
    //    {
    //        //var leave = new LeaveRequest
    //        //{
    //        //    EmployeeId = employeeId,
    //        //    LeaveType  = (LeaveType)Enum.Parse(typeof(LeaveType), leaveType),
    //        //    FromDate   = from,
    //        //    ToDate     = to,
    //        //    Reason     = reason,
    //        //    Status     = LeaveStatus.Pending,
    //        //    CreatedAt  = DateTime.UtcNow
    //        //};
    //        //_uow.Leaves.Add(leave);
    //        //_uow.SaveChanges();
    //        //return leave.Id;
    //        return string.Empty;
    //    }

    //    public void Approve(int id, string reviewerId, string comments)
    //        => UpdateStatus(id, LeaveStatus.Approved, reviewerId, comments);

    //    public void Reject(int id, string reviewerId, string comments)
    //        => UpdateStatus(id, LeaveStatus.Rejected, reviewerId, comments);

    //    private void UpdateStatus(int id, LeaveStatus status, string reviewerId, string comments)
    //    {
    //        var leave = _uow.Leaves.GetById(id);
    //        if (leave == null) throw new KeyNotFoundException($"Leave {id} not found.");
    //        leave.Status         = status;
    //        leave.ReviewedBy     = reviewerId;
    //        leave.ReviewedAt     = DateTime.UtcNow;
    //        leave.ReviewComments = comments;
    //        leave.UpdatedAt      = DateTime.UtcNow;
    //        _uow.Leaves.Update(leave);
    //        _uow.SaveChanges();
    //    }

    //    private static LeaveRequestDto Map(LeaveRequest l) => new LeaveRequestDto
    //    {
    //        EmployeeId   = l.EmployeeId,
    //        EmployeeName = l.Employee?.EmployeeName ?? "Unknown",
    //        Department   = l.Employee?.DepartmentId.ToString() ?? "",
    //        LeaveType    = l.LeaveType.ToString(),
    //        FromDate     = l.FromDate,
    //        ToDate       = l.ToDate,
    //        TotalDays    = l.TotalDays,
    //        Reason       = l.Reason,
    //        Status       = l.Status.ToString(),
    //        CreatedAt    = l.CreatedAt
    //    };
    //}

    // ─── Dashboard Service ────────────────────────────────────────────────────
    public class DashboardService : IDashboardService
    {
        private readonly IUnitOfWork _uow;
        public DashboardService(IUnitOfWork uow) { _uow = uow; }

        public DashboardDto GetDashboard()
        {
            var employees = _uow.Employees.GetAll().ToList();
            //var leaves    = _uow.Leaves.GetAll().ToList();
            var now       = DateTime.UtcNow;

            var dashboardDto = new DashboardDto();

            dashboardDto.TotalEmployees = employees.Count;
            dashboardDto.ActiveEmployees = employees.Count(e => e.Status == EmployeeStatus.Active);
            dashboardDto.NewHiresThisMonth = employees.Count(e => e.DateOfJoining.Month == now.Month && e.DateOfJoining.Year == now.Year);
            dashboardDto.DepartmentHeadcount = employees.GroupBy(e => e.DepartmentId.ToString()).ToDictionary(g => g.Key, g => g.Count());
            dashboardDto.RecentHires = employees
                    .OrderByDescending(e => e.DateOfJoining)
                    .Take(5)
                    .Select(e => new EmployeeDto
                    {
                        EmployeeId = e.EmployeeId,
                        EmployeeName = e.EmployeeName,
                        Email = e.Email,
                        Phone = e.Phone,
                        JoiningDate = e.DateOfJoining,
                        DepartmentID = e.DepartmentId,
                        DepartmentName = e.Department.DepartmentName,
                        DesignationID = e.DesignationId,
                        DesignationName = e.Designation.DesignationName,
                        Status = e.Status.ToString()
                    });

            return dashboardDto;
        }
    }
}
