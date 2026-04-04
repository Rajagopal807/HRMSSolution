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

    /// <summary>
    /// One cell in the attendance grid — represents a single day for one employee.
    /// InTime / OutTime may be null (absent, holiday, week-off).
    /// </summary>
    public class AttendanceDayDto
    {
        public int Day { get; set; }   // 1–31
        public string FirstIn { get; set; }
        public string Lastout { get; set; }

        /// <summary>
        /// Display value for the cell:
        ///   "00"        – absent, no punch
        ///   "AA"        – authorised absence
        ///   "HH"        – holiday
        ///   "WO"        – week off
        ///   "HH:mm\nHH:mm" – in/out times
        /// </summary>
        public string AttId { get; set; }
        public string ShiftId { get; set; }

        /// <summary>True when the day falls outside the selected month (pad day).</summary>
        public bool IsPadDay { get; set; }

        public string CellDisplay
        {
            get
            {
                if (IsPadDay) return string.Empty;
                if (!string.IsNullOrEmpty(AttId) && AttId != "00")
                    return AttId;                    // AA / HH / WO
                if (!string.IsNullOrEmpty(FirstIn) && !string.IsNullOrEmpty(Lastout))
                    return string.Format("{0:hh\\:mm}\n{1:hh\\:mm}",
                                         FirstIn, Lastout);
                if (!string.IsNullOrEmpty(FirstIn))
                    return string.Format("{0:hh\\:mm}\n--:--", FirstIn);
                return "00";
            }
        }
    }

    /// <summary>One row in the attendance register — one employee, 31 day columns.</summary>
    public class AttendanceRowDto
    {
        public string EmployeeCode { get; set; }
        public string EmployeeName { get; set; }

        /// <summary>Exactly 31 elements (index 0 = Day 1 … index 30 = Day 31).</summary>
        public List<AttendanceDayDto> Days { get; set; } = new List<AttendanceDayDto>();

        /// <summary>Count of days where employee actually punched in.</summary>
        public int WorkDays { get; set; }
    }

    /// <summary>Full data set passed to any report generator.</summary>
    public class AttendanceReportDto
    {
        public string CompanyName { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public DateTime PrintedOn { get; set; }
        public int PageNo { get; set; } = 1;

        public List<AttendanceRowDto> Rows { get; set; } = new List<AttendanceRowDto>();

        /// <summary>Number of calendar days in the selected month (28–31).</summary>
        public int DaysInMonth
        {
            get { return DateTime.DaysInMonth(FromDate.Year, FromDate.Month); }
        }
    }
}