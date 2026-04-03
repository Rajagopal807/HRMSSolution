using System;
using System.Collections.Generic;
using HRMS.Domain.Entities;
using HRMS.Domain.Enums;

namespace HRMS.Application.DTOs
{
    public class EmployeeDto
    {
        public string EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public DateTime DateOfBirth { get; set; }
        public DateTime JoiningDate { get; set; }
        public string DepartmentID { get; set; }
        public string DepartmentName { get; set; }
        public string DesignationID { get; set; }
        public string DesignationName { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateEmployeeDto
    {
        public string EmployeeID { get; set; }
        public string EmployeeName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public DateTime DateOfBirth { get; set; }
        public DateTime JoiningDate { get; set; }
        public string DepartmentID { get; set; }
        public string DesignationID { get; set; }
    }

    public class DepartmentDto
    {
        public string DepartmentID { get; set; }
        public string DepartmentName { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class DesignationDto
    {
        public string DesignationID { get; set; }
        public string DesignationName { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class LeaveRequestDto
    {
        public int Id { get; set; }
        public string EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string Department { get; set; }
        public string LeaveType { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int TotalDays { get; set; }
        public string Reason { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class DashboardDto
    {
        public int TotalEmployees { get; set; }
        public int ActiveEmployees { get; set; }
        public int PendingLeaves { get; set; }
        public int NewHiresThisMonth { get; set; }
        public decimal TotalPayroll { get; set; }
        public decimal AvgSalary { get; set; }
        public IDictionary<string, int> DepartmentHeadcount { get; set; }
        public IEnumerable<EmployeeDto> RecentHires { get; set; }
    }

    public class ReportDto
    {
        public Department Department { get; set; }
        public string EmployeeName { get; set; }
        public string EmployeeID { get; set; }
        public string FirstIN { get; set; }
        public string LastOut { get; set; }
        public string TotalHours { get; set; }
        public DateTime Date { get; set; }
        public bool IsPresent { get; set; }
    }
}