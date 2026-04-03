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
}