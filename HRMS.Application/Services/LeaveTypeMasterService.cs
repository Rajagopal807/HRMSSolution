using HRMS.Application.DTOs;
using HRMS.Application.Interfaces;
using HRMS.Domain.Entities;
using HRMS.Domain.Enums;
using HRMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;

namespace HRMS.Application.Services
{
    // ════════════════════════════════════════════════════════════════════════════
    // Leave Type Master Service
    // ════════════════════════════════════════════════════════════════════════════
    public class LeaveTypeMasterService : ILeaveTypeMasterService
    {
        private readonly IUnitOfWork _uow;
        public LeaveTypeMasterService(IUnitOfWork uow) { _uow = uow; }

        public IEnumerable<LeaveTypeMasterDto> GetAll()
            => _uow.LeaveTypesMasters.GetAll().Select(Map);

        public IEnumerable<LeaveTypeMasterDto> GetActive()
            => _uow.LeaveTypesMasters.GetActive().Select(Map);

        public LeaveTypeMasterDto GetById(string leavetypeID)
        {
            var lt = _uow.LeaveTypesMasters.GetByLeaveTypeID(leavetypeID);
            return lt == null ? null : Map(lt);
        }

        public string Create(CreateLeaveTypeMasterDto dto)
        {
            if (_uow.LeaveTypesMasters.CodeExists(dto.Code))
                throw new InvalidOperationException(
                    $"Leave type code '{dto.Code}' already exists.");

            var lt = new LeaveTypeMaster
            {
                Name = dto.Name.Trim(),
                LeaveTypeID = dto.Code.ToUpper().Trim(),
                MaxDaysPerYear = dto.MaxDaysPerYear,
                AllowHalfDay = dto.AllowHalfDay,
                IsCarryForward = dto.IsCarryForward,
                IsActive = dto.IsActive,
                Description = dto.Description,
                CreatedAt = DateTime.UtcNow
            };
            _uow.LeaveTypesMasters.Add(lt);
            _uow.SaveChanges();
            return lt.LeaveTypeID;
        }

        public void Update(string id, CreateLeaveTypeMasterDto dto)
        {
            var lt = _uow.LeaveTypesMasters.GetByLeaveTypeID(id);
            if (lt == null) throw new KeyNotFoundException($"Leave type {id} not found.");

            if (_uow.LeaveTypesMasters.CodeExists(dto.Code, excludeId: id))
                throw new InvalidOperationException(
                    $"Leave type code '{dto.Code}' is already used by another record.");

            lt.Name = dto.Name.Trim();
            lt.LeaveTypeID = dto.Code.ToUpper().Trim();
            lt.MaxDaysPerYear = dto.MaxDaysPerYear;
            lt.AllowHalfDay = dto.AllowHalfDay;
            lt.IsCarryForward = dto.IsCarryForward;
            lt.IsActive = dto.IsActive;
            lt.Description = dto.Description;
            lt.UpdatedAt = DateTime.UtcNow;
            _uow.LeaveTypesMasters.Update(lt);
            _uow.SaveChanges();
        }

        public void Delete(string id)
        {
            _uow.LeaveTypesMasters.DeleteLeaveTypeID(id);
            _uow.SaveChanges();
        }

        public bool CodeExists(string code, string excludeId = "0")
            => _uow.LeaveTypesMasters.CodeExists(code, excludeId);

        private static LeaveTypeMasterDto Map(LeaveTypeMaster lt) =>
            new LeaveTypeMasterDto
            {
                LeavetypeID = lt.LeaveTypeID,
                Name = lt.Name,
                MaxDaysPerYear = lt.MaxDaysPerYear,
                AllowHalfDay = lt.AllowHalfDay,
                IsCarryForward = lt.IsCarryForward,
                IsActive = lt.IsActive,
                Description = lt.Description
            };
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Leave Application Service
    // ════════════════════════════════════════════════════════════════════════════
    public class LeaveApplicationService : ILeaveApplicationService
    {
        private readonly IUnitOfWork _uow;
        public LeaveApplicationService(IUnitOfWork uow) { _uow = uow; }

        public IEnumerable<LeaveApplicationDto> GetAll()
            => _uow.LeaveApplications.GetAllLeaveApplication().Select(Map);

        public IEnumerable<LeaveApplicationDto> GetPending()
            => _uow.LeaveApplications.GetPending().Select(Map);

        public IEnumerable<LeaveApplicationDto> GetByEmployee(string employeeId)
            => _uow.LeaveApplications.GetByEmployee(employeeId).Select(Map);

        public LeaveApplicationDto GetByApplicationId(int applicationId)
        {
            var app = _uow.LeaveApplications.GetByApplicationId(applicationId);
            return app == null ? null : Map(app);
        }

        public ApplyLeaveDto GetById(int applicationId)
        {
            var app = _uow.LeaveApplications.GetByApplicationId(applicationId);
            return app == null ? null : MapApply(app);
        }

        public string Update(ApplyLeaveDto dto, string userName)
        {
            var app = _uow.LeaveApplications.GetByApplicationId(dto.ApplicationId);

            if (app == null)
                return "Leave application not found.";

            // 🚫 Allow only pending edits
            //if (app.Status != LeaveStatus.Pending)
            //    return "Only pending leave applications can be edited.";

            // ── Validate dates ─────────────────────────────
            if (dto.FromDate > dto.ToDate)
                return "From Date cannot be after To Date.";

            // ── Validate leave type ────────────────────────
            var lt = _uow.LeaveTypesMasters.GetByLeaveTypeID(dto.LeaveTypeMasterId);
            if (lt == null || !lt.IsActive)
                return "Selected leave type is not available.";

            // ── Half-day validation ────────────────────────
            bool isHalfDay = dto.Session == "FirstHalf" || dto.Session == "SecondHalf";
            if (isHalfDay)
            {
                if (!lt.AllowHalfDay)
                    return $"'{lt.Name}' does not allow half-day applications.";

                if (dto.FromDate != dto.ToDate)
                    return "Half-day leave must be for a single day.";
            }

            // ── Parse enum ─────────────────────────────────
            if (!Enum.TryParse(dto.Session, out LeaveSession session))
                return "Invalid session.";


            // ── Update values ──────────────────────────────
            app.EmployeeId = dto.EmployeeId;
            app.LeaveTypeMasterId = dto.LeaveTypeMasterId;
            app.FromDate = dto.FromDate;
            app.ToDate = dto.ToDate;
            app.Session = session;
            app.Reason = dto.Reason;
            app.UpdatedAt = DateTime.UtcNow;

            _uow.LeaveApplications.Update(app);
            _uow.SaveChanges();
            _uow.LeaveApplications.CallCreateMusterSP(app.EmployeeId, app.FromDate);
            return string.Empty;
        }

        public void Delete(int id, string userName)
        {
            var app = _uow.LeaveApplications.GetByApplicationId(id);

            if (app == null)
                throw new KeyNotFoundException($"Leave application {id} not found.");

            // 🚫 Restrict delete
            //if (app.Status != LeaveStatus.Pending)
            //    throw new InvalidOperationException("Only pending leave can be deleted.");

            _uow.LeaveApplications.DeleteLeaveApplicationID(id); // or Remove(app)
            _uow.SaveChanges();
            _uow.LeaveApplications.CallCreateMusterSP(app.EmployeeId, app.FromDate);
        }


        public (int applicationId, string Error) Apply(ApplyLeaveDto dto, string appliedByUserId)
        {
            // ── Validate dates ────────────────────────────────────────────────
            if (dto.FromDate > dto.ToDate)
                return (0, "From Date cannot be after To Date.");

            // ── Validate leave type exists and is active ───────────────────────
            var lt = _uow.LeaveTypesMasters.GetByLeaveTypeID(dto.LeaveTypeMasterId);
            if (lt == null || !lt.IsActive)
                return (0, "Selected leave type is not available.");

            // ── Validate half-day rule ─────────────────────────────────────────
            bool isHalfDay = dto.Session == "FirstHalf" || dto.Session == "SecondHalf";
            if (isHalfDay)
            {
                if (!lt.AllowHalfDay)
                    return (0, $"'{lt.Name}' does not allow half-day applications.");
                if (dto.FromDate != dto.ToDate)
                    return (0, "Half-day leave can only be applied for a single day.");
            }

            // ── Parse session enum ────────────────────────────────────────────
            if (!Enum.TryParse(dto.Session, out LeaveSession session))
                return (0, "Invalid session selected.");

            for (var date = dto.FromDate.Date; date <= dto.ToDate.Date; date = date.AddDays(1))
            {
                bool exists = _uow.LeaveApplications
                    .Find(x =>
                        x.EmployeeId == dto.EmployeeId &&
                        DbFunctions.TruncateTime(x.FromDate) == date &&
                        x.Session == session &&
                        !x.IsDeleted)
                    .Any();

                if (exists)
                {
                    return (0, $"Leave already applied for {date:dd-MMM-yyyy}");
                }
            }


            var app = new LeaveApplication
            {
                EmployeeId = dto.EmployeeId,
                LeaveTypeMasterId = dto.LeaveTypeMasterId,
                FromDate = dto.FromDate,
                ToDate = dto.ToDate,
                Session = session,
                Reason = dto.Reason,
                AppliedBy = appliedByUserId,
                Status = LeaveStatus.Approved,
                CreatedAt = DateTime.UtcNow
            };

            _uow.LeaveApplications.Add(app);
            _uow.SaveChanges();
            _uow.LeaveApplications.CallCreateMusterSP(app.EmployeeId, app.FromDate);
            return (app.ApplicationId, string.Empty);
        }

        public void Approve(int applicationId, string reviewerId, string notes)
            => UpdateStatus(applicationId, LeaveStatus.Approved, reviewerId, notes);

        public void Reject(int applicationId, string reviewerId, string notes)
            => UpdateStatus(applicationId, LeaveStatus.Rejected, reviewerId, notes);
        public ApplyLeaveScreenDto GetApplyScreen()
        {
            var employees = _uow.Employees.GetActiveEmployees()
                .OrderBy(e => e.EmployeeId)
                .Select(e => new EmployeeDto
                {
                    EmployeeId = e.EmployeeId,
                    EmployeeName = e.EmployeeName,
                    DepartmentName = e.Department.DepartmentName
                }).ToList();

            var leaveTypes = _uow.LeaveTypesMasters.GetActive()
                .Select(lt => new LeaveTypeMasterDto
                {
                    LeavetypeID = lt.LeaveTypeID,
                    Name = lt.Name,
                    AllowHalfDay = lt.AllowHalfDay,
                    MaxDaysPerYear = lt.MaxDaysPerYear
                }).ToList();

            return new ApplyLeaveScreenDto
            {
                Employees = employees,
                LeaveTypes = leaveTypes
            };
        }

        private void UpdateStatus(int applicationId, LeaveStatus status,
            string reviewerId, string notes)
        {
            var app = _uow.LeaveApplications.GetByApplicationId(applicationId);
            if (app == null) throw new KeyNotFoundException($"Leave application {applicationId} not found.");
            app.Status = status;
            app.ReviewedBy = reviewerId;
            app.ReviewedAt = DateTime.UtcNow;
            app.ReviewNotes = notes;
            app.UpdatedAt = DateTime.UtcNow;
            _uow.LeaveApplications.Update(app);
            _uow.SaveChanges();
        }

        private static LeaveApplicationDto Map(LeaveApplication a)
        {
            LeaveApplicationDto applicationDto = new LeaveApplicationDto();
            applicationDto.ApplicationId = a.ApplicationId;
            applicationDto.EmployeeId = a.EmployeeId;
            applicationDto.EmployeeName = a.Employee?.EmployeeName ?? a.EmployeeId;
            applicationDto.Department = a.Employee?.Department.DepartmentName ?? "";
            applicationDto.LeaveTypeMasterId = a.LeaveTypeMasterId;
            applicationDto.LeaveTypeName = a.LeaveTypeMaster?.Name ?? a.LeaveTypeMasterId;
            applicationDto.FromDate = a.FromDate;
            applicationDto.ToDate = a.ToDate;
            applicationDto.Session = a.Session.ToString();
            applicationDto.TotalDays = a.TotalDays;
            applicationDto.Reason = a.Reason;
            applicationDto.Status = a.Status.ToString();
            applicationDto.ReviewNotes = a.ReviewNotes;
            applicationDto.CreatedAt = a.CreatedAt;

            return applicationDto;
        }

        private static ApplyLeaveDto MapApply(LeaveApplication a)
        {
            ApplyLeaveDto applyLeaveDto = new ApplyLeaveDto();

            //applyLeaveDto.ApplicationId = a.ApplicationId;
            applyLeaveDto.EmployeeId = a.EmployeeId;
            applyLeaveDto.LeaveTypeMasterId = a.LeaveTypeMasterId;
            applyLeaveDto.FromDate = a.FromDate;
            applyLeaveDto.ToDate = a.ToDate;
            applyLeaveDto.Session = a.Session.ToString();
            applyLeaveDto.Reason = a.Reason;

            return applyLeaveDto;
        }
    }
}