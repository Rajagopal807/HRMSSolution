using System.ComponentModel.DataAnnotations;

namespace HRMS.Domain.Enums
{
    public enum EmployeeStatus
    {
        Active = 1,
        Inactive = 2,
        OnLeave = 3,
        Terminated = 4
    }

    public enum UserRole
    {
        Admin = 1,
        HRManager = 2,
        Employee = 3,
        SuperAdmin = 4,
    }

    public enum LeaveType
    {
        Annual = 1,
        Sick = 2,
        Casual = 3,
        Maternity = 4,
        Unpaid = 5
    }

    public enum LeaveStatus
    {
        Pending = 1,
        Approved = 2,
        Rejected = 3
    }

    public enum ReportGrouping
    {
        EmployeeWise = 1,
        DepartmentWise = 2,
        CadreWise = 3
    }

    public enum ReportDuration
    {
        Daily = 1,
        Monthly = 2,
        Periodic = 3
    }

    public enum Gender
    {
        [Display(Name = "Male")]
        Male = 1,
        [Display(Name = "Female")]
        Female =0,
    }

    public enum MaritalStatus
    {
        [Display(Name = "Unmarried")]
        Unmarried = 0,
        [Display(Name = "Married")]
        Married = 1,
    }
}