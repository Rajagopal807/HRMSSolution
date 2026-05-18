using HRMS.Domain.Entities;
using HRMS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HRMS.Application.DTOs
{
    public class EmployeeDto
    {
        public string EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public DateTime JoiningDate { get; set; }
        public string DepartmentID { get; set; }
        public string DepartmentName { get; set; }
        public string DesignationID { get; set; }
        public string DesignationName { get; set; }
        public Gender Gender { get; set; }
        public MaritalStatus MaritalStatus { get; set; }
        public string BloodGroup { get; set; }
        public int Weekoff1 { get; set; }
        public string Weekoff1Name { get; set; }
        public int Weekoff2 { get; set; }
        public string Weekoff2Name { get; set; }
        public DateTime? DateofLeft { get; set; }
        public string Status { get; set; }
        public bool IsInactive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateEmployeeDto
    {
        public string EmployeeID { get; set; }
        public string EmployeeName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public DateTime JoiningDate { get; set; }
        public string DepartmentID { get; set; }
        public string DesignationID { get; set; }
        public Gender Gender { get; set; }
        public MaritalStatus MaritalStatus { get; set; }
        public string BloodGroup { get; set; }
        public int Weekoff1 { get; set; }
        public int Weekoff2 { get; set; }
        public bool IsInactive { get; set; }
        public DateTime? DateOfLeft { get; set; }
        public string Status { get; set; }
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

    // ── Grouped report extensions ─────────────────────────────────────────────
    // OCP: existing AttendanceReportDto is untouched.
    // GroupedAttendanceReportDto wraps it with grouping metadata.

    /// <summary>
    /// One group (a Department or a Cadre) containing its rows and a summary.
    /// </summary>
    public class AttendanceGroupDto
    {
        /// <summary>Group label — dept name or cadre name shown as section header.</summary>
        public string GroupName { get; set; }

        /// <summary>All employee rows belonging to this group.</summary>
        public List<AttendanceRowDto> Rows { get; set; } = new List<AttendanceRowDto>();

        /// <summary>Total work-days across all employees in the group.</summary>
        public int TotalWorkDays
        {
            get
            {
                int total = 0;
                foreach (var r in Rows) total += r.WorkDays;
                return total;
            }
        }

        public int EmployeeCount { get { return Rows.Count; } }
    }

    /// <summary>
    /// Full grouped attendance report — passed to GroupedPdfReport / GroupedExcelReport.
    /// Extends the same base metadata as AttendanceReportDto.
    /// </summary>
    public class GroupedAttendanceReportDto
    {
        public string CompanyName { get; set; }
        public System.DateTime FromDate { get; set; }
        public System.DateTime ToDate { get; set; }
        public System.DateTime PrintedOn { get; set; }

        /// <summary>"Department" or "Cadre"</summary>
        public string GroupingLabel { get; set; }

        public List<AttendanceGroupDto> Groups { get; set; }= new List<AttendanceGroupDto>();

        public int DaysInMonth
        {
            get { return System.DateTime.DaysInMonth(FromDate.Year, FromDate.Month); }
        }

        public int TotalEmployees
        {
            get
            {
                int n = 0;
                foreach (var g in Groups) n += g.EmployeeCount;
                return n;
            }
        }
    }

    /// <summary>
    /// Carries every filter the user selected on the report screen.
    /// Passed straight to IReportGenerator.Generate().
    /// </summary>
    public class ReportFilterDto
    {
        // ── Grouping ──────────────────────────────────────────────────────────
        public ReportGrouping Grouping { get; set; } = ReportGrouping.EmployeeWise;

        // ── Selected items (employee codes / dept names / cadre names) ────────
        public List<string> SelectedItems { get; set; } = new List<string>();

        // ── Duration ──────────────────────────────────────────────────────────
        public ReportDuration Duration { get; set; } = ReportDuration.Monthly;

        /// <summary>Used when Duration = Daily.</summary>
        public DateTime? DailyDate { get; set; }

        /// <summary>Used when Duration = Monthly  (year + month only).</summary>
        public int? Month { get; set; }
        public int? Year { get; set; }

        /// <summary>Used when Duration = Periodic.</summary>
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        // ── Report type (dropdown value) ──────────────────────────────────────
        public string ReportType { get; set; }
        public string ExportType { get; set; }

        /// <summary>True when user selected Excel output.</summary>
        public bool IsExcel
        {
            get
            {
                return string.Equals(ExportType, "EXCEL",
                    System.StringComparison.OrdinalIgnoreCase);
            }
        }

        // ── Convenience: resolved date range ─────────────────────────────────
        public DateTime ResolvedFrom
        {
            get
            {
                switch (Duration)
                {
                    case ReportDuration.Daily:
                        return DailyDate ?? DateTime.Today;
                    case ReportDuration.Monthly:
                        return new DateTime(Year ?? DateTime.Now.Year,
                                            Month ?? DateTime.Now.Month, 1);
                    default:
                        return FromDate ?? DateTime.Today.AddMonths(-1);
                }
            }
        }

        public DateTime ResolvedTo
        {
            get
            {
                switch (Duration)
                {
                    case ReportDuration.Daily:
                        return DailyDate ?? DateTime.Today;
                    case ReportDuration.Monthly:
                        int y = Year ?? DateTime.Now.Year;
                        int m = Month ?? DateTime.Now.Month;
                        return new DateTime(y, m,
                                            DateTime.DaysInMonth(y, m));
                    default:
                        return ToDate ?? DateTime.Today;
                }
            }
        }
    }

    /// <summary>
    /// One item shown in the Available Items listbox.
    /// Value  = the code / key sent back on submit.
    /// Display = what the user sees.
    /// </summary>
    public class AvailableItemDto
    {
        public string Value { get; set; }
        public string Display { get; set; }
    }

    /// <summary>
    /// Everything the Report screen View needs to render.
    /// </summary>
    public class ReportScreenDto
    {
        public List<AvailableItemDto> AvailableEmployees { get; set; } = new List<AvailableItemDto>();
        public List<AvailableItemDto> AvailableDepartments { get; set; } = new List<AvailableItemDto>();
        public List<AvailableItemDto> AvailableCadres { get; set; } = new List<AvailableItemDto>();

        public List<string> ReportTypes { get; set; } = new List<string>
        {
            "Attendance Register",
            //"Employee Directory",
            //"Leave Summary",
            //"Payroll Summary"
        };
    }

    // ── Attendance Transaction (daily summary row in Image 1 grid) ─────────────

    /// <summary>
    /// One row in the Attendance Transactions grid — computed from ManualPunch records.
    /// </summary>
    public class AttendanceTransactionRowDto
    {
        public string EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public DateTime Date { get; set; }

        // Computed from punches
        public TimeSpan? FirstIn { get; set; }
        public TimeSpan? LastOut { get; set; }
        public string Shift { get; set; } = "GEN";  // General shift default
        public string AttId { get; set; } = "P";    // P=Present, A=Absent, WO=WeekOff, H=Holiday
        public TimeSpan TotalHrs { get; set; }
        public TimeSpan Worked { get; set; }
        public TimeSpan Extra { get; set; }            // overtime / extra time
        public TimeSpan Late { get; set; }            // late arrival duration
        public TimeSpan Early { get; set; }            // early departure duration
        public string OutPass { get; set; }            // out-pass if any
        public int PunchCount { get; set; }            // total punch count
        public TimeSpan OT { get; set; }            // official overtime

        // Display helpers
        public string FirstInDisplay => FirstIn.HasValue ? FirstIn.Value.ToString(@"hh\:mm") : "--:--";
        public string LastOutDisplay => LastOut.HasValue ? LastOut.Value.ToString(@"hh\:mm") : "--:--";
        public string TotalHrsDisplay => TotalHrs == TimeSpan.Zero ? "--:--" : TotalHrs.ToString(@"hh\:mm");
        public string WorkedDisplay => Worked == TimeSpan.Zero ? "--:--" : Worked.ToString(@"hh\:mm");
        public string ExtraDisplay => Extra == TimeSpan.Zero ? "00:00" : Extra.ToString(@"hh\:mm");
        public string LateDisplay => Late == TimeSpan.Zero ? "00:00" : Late.ToString(@"hh\:mm");
        public string EarlyDisplay => Early == TimeSpan.Zero ? "00:00" : Early.ToString(@"hh\:mm");
        public string OTDisplay => OT == TimeSpan.Zero ? "00:00" : OT.ToString(@"hh\:mm");
    }

    /// <summary>Everything the Attendance Transactions screen needs to render.</summary>
    public class AttendanceTransactionScreenDto
    {
        public List<EmployeeDto> Employees { get; set; } = new List<EmployeeDto>();
        public string SelectedEmpId { get; set; }
        public string SelectedEmpName { get; set; }
        public int SelectedMonth { get; set; } = DateTime.Now.Month;
        public int SelectedYear { get; set; } = DateTime.Now.Year;
        public List<AttendanceTransactionRowDto> Rows { get; set; } = new List<AttendanceTransactionRowDto>();
        public bool HasResult { get; set; } = false;
    }

    // ── Manual Punch (Image 2 — the edit punches detail screen) ───────────────

    public class ManualPunchDto
    {
        public int Id { get; set; }
        public string EmployeeId { get; set; }
        public DateTime AttendanceDate { get; set; }
        public string PunchedTimeDisplay => PunchedTime.ToString(@"hh\:mm");
        public TimeSpan PunchedTime { get; set; }
        public DateTime TransactionDate { get; set; }
        public string TransTimeDisplay => TransTime.HasValue ? TransTime.Value.ToString(@"hh\:mm") : "";
        public TimeSpan? TransTime { get; set; }
        public string IOFlag { get; set; }   // "In" or "Out"
        public bool IsDeleted { get; set; }
        public string Remarks { get; set; }
        public string BranchNo { get; set; }
    }

    public class SaveManualPunchDto
    {
        [Required]
        public string EmployeeId { get; set; }

        [Required]
        public DateTime AttendanceDate { get; set; }

        [Required]
        public string PunchedTimeStr { get; set; }   // "HH:mm" from UI

        public string TransDateStr { get; set; }   // optional, defaults to now
        public string IOFlag { get; set; } = "In";
        public bool IsDeleted { get; set; } = false;

        [StringLength(200)]
        public string Remarks { get; set; }

        [StringLength(10)]
        public string BranchNo { get; set; }
    }

    /// <summary>Everything the Edit Punches screen needs to render.</summary>
    public class EditPunchesScreenDto
    {
        public string EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public DateTime AttendanceDate { get; set; }
        public string StatusMessage { get; set; }
        public List<ManualPunchDto> Punches { get; set; } = new List<ManualPunchDto>();
        public SaveManualPunchDto Form { get; set; } = new SaveManualPunchDto();
    }

    public class TempCardDto
    {
        public string TempCardId { get; set; }
        public string EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string Department { get; set; }
    }

    public class SaveTempCardDto
    {
        [Required(ErrorMessage = "Temp Card ID is required.")]
        [StringLength(50, ErrorMessage = "Max 50 characters.")]
        public string TempCardId { get; set; }

        [Required(ErrorMessage = "Employee is required.")]
        public string EmployeeId { get; set; }
    }

    public class EmployeeDropdownDto
    {
        public string EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string DepartmentName { get; set; }
    }
}