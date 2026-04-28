using System.Collections.Generic;
using HRMS.Application.DTOs;

namespace HRMS.Application.Interfaces
{
    /// <summary>OCP: new service contracts added by extension.</summary>

    public interface ILeaveTypeMasterService
    {
        IEnumerable<LeaveTypeMasterDto> GetAll();
        IEnumerable<LeaveTypeMasterDto> GetActive();
        LeaveTypeMasterDto GetById(string leaveTypeID);
        string Create(CreateLeaveTypeMasterDto dto);
        void Update(string leaveTypeID, CreateLeaveTypeMasterDto dto);
        void Delete(string leaveTypeID);
        bool CodeExists(string code, string excludeId = "0");
    }

    public interface ILeaveApplicationService
    {
        IEnumerable<LeaveApplicationDto> GetAll();
        IEnumerable<LeaveApplicationDto> GetPending();
        IEnumerable<LeaveApplicationDto> GetByEmployee(string employeeId);
        LeaveApplicationDto GetByApplicationId(int applicationId);
        ApplyLeaveDto GetById(int applicationId);
        string Update(ApplyLeaveDto dto, string userName);
        void Delete(int id, string userName);
        /// <summary>Returns (applicationId, errorMessage). errorMessage is null on success.</summary>
        (int applicationId, string Error) Apply(ApplyLeaveDto dto, string appliedByUserId);
        void Approve(int applicationId, string reviewerId, string notes);
        void Reject(int applicationId, string reviewerId, string notes);

        ApplyLeaveScreenDto GetApplyScreen();
    }
}