using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using HRMS.Domain.Entities;

namespace HRMS.Application.DTOs
{
    // ── Leave Type Master ─────────────────────────────────────────────────────

    public class LeaveTypeMasterDto
    {
        public string LeavetypeID { get; set; }
        public string Name { get; set; }
        public int MaxDaysPerYear { get; set; }
        public bool AllowHalfDay { get; set; }
        public bool IsCarryForward { get; set; }
        public bool IsActive { get; set; }
        public string Description { get; set; }
    }

    public class CreateLeaveTypeMasterDto
    {
        [Required, StringLength(100)]
        public string Name { get; set; }

        [Required, StringLength(10)]
        public string Code { get; set; }

        [Range(0, 365)]
        public int MaxDaysPerYear { get; set; }

        public bool AllowHalfDay { get; set; } = true;
        public bool IsCarryForward { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public string Description { get; set; }
    }

    // ── Leave Application ─────────────────────────────────────────────────────

    public class LeaveApplicationDto
    {
        public int ApplicationId { get; set; }
        public string EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string Department { get; set; }
        public string LeaveTypeMasterId { get; set; }
        public string LeaveTypeName { get; set; }
        public string LeaveTypeCode { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string Session { get; set; }   // FullDay / FirstHalf / SecondHalf
        public decimal TotalDays { get; set; }
        public string Reason { get; set; }
        public string Status { get; set; }
        public string ReviewNotes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ApplyLeaveDto
    {
        public int ApplicationId { get; set; }

        [Required(ErrorMessage = "EmployeeId is required.")]
        public string EmployeeId { get; set; }

        [Required(ErrorMessage = "LeaveTypeMasterId is required.")]
        public string LeaveTypeMasterId { get; set; }

        [Required(ErrorMessage = "FromDate is required.")]
        public DateTime FromDate { get; set; }

        [Required(ErrorMessage = "ToDate is required.")]
        public DateTime ToDate { get; set; }

        [Required(ErrorMessage = "Session is required.")]
        public string Session { get; set; } = "FullDay";

        [Required(ErrorMessage = "Reason is required."), StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters.")]
        public string Reason { get; set; }
    }

    /// <summary>Everything the Apply Leave screen needs to render.</summary>
    public class ApplyLeaveScreenDto
    {
        public List<EmployeeDto> Employees { get; set; } = new List<EmployeeDto>();
        public List<LeaveTypeMasterDto> LeaveTypes { get; set; } = new List<LeaveTypeMasterDto>();
        public ApplyLeaveDto Form { get; set; } = new ApplyLeaveDto();
    }
}