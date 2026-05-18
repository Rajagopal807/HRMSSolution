using HRMS.Domain.Entities;
using HRMS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace HRMS.Web.ViewModels
{
    // ─────────────────────────────────────────────────────────────────────────
    // Account
    // ─────────────────────────────────────────────────────────────────────────

    public class LoginViewModel
    {
        [Required(ErrorMessage = "User ID is required.")]
        [Display(Name = "User ID")]
        public string Userid { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Employee — Read
    // ─────────────────────────────────────────────────────────────────────────

    public class EmployeeViewModel
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
        public List<SelectListItem> DepartmentOptions { get; set; }
        public List<SelectListItem> DesignationOptions { get; set; }
        public List<SelectListItem> WeekOffOptions { get; set; }
        public int WeekOff1 { get; set; }   
        public string WeekOff1Name { get; set; }
        public int WeekOff2 { get; set; }   
        public string WeekOff2Name { get; set; }
        public DateTime? DateOfLeft { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Employee — Create / Edit
    // ─────────────────────────────────────────────────────────────────────────

    public class CreateEmployeeViewModel
    {
        [Required(ErrorMessage = "Employee ID is required.")]
        [Display(Name = "Employee ID")]
        public string EmployeeID { get; set; }

        [Required(ErrorMessage = "Employee Name is required.")]
        [Display(Name = "Employee Name")]
        public string EmployeeName { get; set; }

        [Display(Name = "Email Address")]
        public string Email { get; set; }

        [Display(Name = "Phone Number")]
        public string Phone { get; set; }

        [Display(Name = "Address")]
        public string Address { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime? DateOfBirth { get; set; }

        [Required(ErrorMessage = "Joining date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Joining Date")]
        public DateTime JoiningDate { get; set; }

        [Required(ErrorMessage = "Department is required.")]
        [Display(Name = "Department")]
        public string Department { get; set; }

        [Required(ErrorMessage = "Designation is required.")]
        [StringLength(100, ErrorMessage = "Max 100 characters.")]
        [Display(Name = "Designation")]
        public string Designation { get; set; }

        [Display(Name = "Gender")]
        public Gender Gender { get; set; }

        [Display(Name = "MaritalStatus")]
        public MaritalStatus MaritalStatus { get; set; }

        [Display(Name = "BloodGroup")]
        public string BloodGroup { get; set; }

        [Required(ErrorMessage = "Week Off 1 is required.")]
        [Display(Name = "Week Off 1")]
        public int WeekOff1 { get; set; }

        [Required(ErrorMessage = "Week Off 2 is required.")]
        [Display(Name = "Week Off 2")]
        public int WeekOff2 { get; set; }

        [Display(Name = "Mark as Inactive")]
        public bool IsInactive { get; set; } = false;

        [DataType(DataType.Date)]
        [Display(Name = "Date of Left")]
        public DateTime? DateOfLeft { get; set; }

        public List<SelectListItem> DepartmentOptions { get; set; }

        public List<SelectListItem> DesignationOptions { get; set; }

        public List<SelectListItem> WeekOffOptions { get; set; }

    }

    // ─────────────────────────────────────────────────────────────────────────
    // Leave — Read
    // ─────────────────────────────────────────────────────────────────────────

    public class LeaveRequestViewModel
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
        public string ReviewedBy { get; set; }
        public string ReviewComments { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Leave — Apply
    // ─────────────────────────────────────────────────────────────────────────

    public class CreateLeaveViewModel
    {
        [Required(ErrorMessage = "Leave type is required.")]
        [Display(Name = "Leave Type")]
        public string LeaveType { get; set; }

        [Required(ErrorMessage = "From date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "From Date")]
        public DateTime FromDate { get; set; }

        [Required(ErrorMessage = "To date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "To Date")]
        public DateTime ToDate { get; set; }

        [Required(ErrorMessage = "Reason is required.")]
        [StringLength(500, ErrorMessage = "Max 500 characters.")]
        [Display(Name = "Reason")]
        public string Reason { get; set; }

        public List<string> LeaveTypeOptions
        {
            get
            {
                return new List<string>
                {
                    "Annual", "Sick", "Casual", "Maternity", "Unpaid"
                };
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Leave — Review (Approve / Reject)
    // ─────────────────────────────────────────────────────────────────────────

    public class ReviewLeaveViewModel
    {
        [Required]
        public int Id { get; set; }

        [StringLength(300, ErrorMessage = "Max 300 characters.")]
        [Display(Name = "Comments")]
        public string Comments { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Dashboard
    // ─────────────────────────────────────────────────────────────────────────

    public class DashboardViewModel
    {
        public int TotalEmployees { get; set; }
        public int ActiveEmployees { get; set; }
        public int PendingLeaves { get; set; }
        public int NewHiresThisMonth { get; set; }
        public decimal TotalPayroll { get; set; }
        public decimal AvgSalary { get; set; }

        public Dictionary<string, int> DepartmentHeadcount { get; set; }
        public Dictionary<string, int> LeaveByType { get; set; }
        public Dictionary<string, decimal> SalaryByDepartment { get; set; }

        public IEnumerable<EmployeeViewModel> RecentHires { get; set; }
        public IEnumerable<LeaveRequestViewModel> PendingLeaveRequests { get; set; }

        public DashboardViewModel()
        {
            DepartmentHeadcount = new Dictionary<string, int>();
            LeaveByType = new Dictionary<string, int>();
            SalaryByDepartment = new Dictionary<string, decimal>();
            RecentHires = new List<EmployeeViewModel>();
            PendingLeaveRequests = new List<LeaveRequestViewModel>();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Forgot Password
    // ─────────────────────────────────────────────────────────────────────────
    public class ForgotPasswordViewModel
    {
        public string UserId { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Reset Password
    // ─────────────────────────────────────────────────────────────────────────
    public class ResetPasswordViewModel
    {
        public string Token { get; set; }

        [Required]
        public string Password { get; set; }

        [System.ComponentModel.DataAnnotations.Compare("Password")]
        public string ConfirmPassword { get; set; }
    }


    // ─────────────────────────────────────────────────────────────────────────
    // Report Filters
    // ─────────────────────────────────────────────────────────────────────────

    public class EmployeeReportFilterViewModel
    {
        [Display(Name = "Department")]
        public string Department { get; set; }

        [Display(Name = "Status")]
        public string Status { get; set; }
    }

    public class LeaveReportFilterViewModel
    {
        [DataType(DataType.Date)]
        [Display(Name = "From Date")]
        public DateTime? FromDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "To Date")]
        public DateTime? ToDate { get; set; }
    }

    public class PayrollReportFilterViewModel
    {
        [Required]
        [Range(1, 12, ErrorMessage = "Enter a valid month (1-12).")]
        [Display(Name = "Month")]
        public int Month { get; set; }

        [Required]
        [Range(2000, 2100, ErrorMessage = "Enter a valid year.")]
        [Display(Name = "Year")]
        public int Year { get; set; }

        public PayrollReportFilterViewModel()
        {
            Month = DateTime.Now.Month;
            Year = DateTime.Now.Year;
        }
    }

    public class TempcardViewModel
    {
        [Required]
        [Display(Name = "TempCardNo")]
        public string TempCardNo { get; set; }

        [Required]
        [Display(Name = "Employee ID")]
        [StringLength(11)]
        public string EmpId { get; set; }

        public Employee Employee { get; set; }
    }

}