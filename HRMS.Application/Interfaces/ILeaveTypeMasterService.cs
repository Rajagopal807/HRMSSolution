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
        LeaveApplicationDto GetById(string leavetypeID);

        /// <summary>Returns (id, errorMessage). errorMessage is null on success.</summary>
        (string Id, string Error) Apply(ApplyLeaveDto dto, string appliedByUserId);
        void Approve(string id, string reviewerId, string notes);
        void Reject(string id, string reviewerId, string notes);

        ApplyLeaveScreenDto GetApplyScreen();
    }
}