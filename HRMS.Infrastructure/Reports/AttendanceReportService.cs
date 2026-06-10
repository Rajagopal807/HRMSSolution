using System;
using System.Collections.Generic;
using System.Linq;
using HRMS.Application.DTOs;
using HRMS.Application.Interfaces;
using HRMS.Domain.Entities;
using HRMS.Domain.Enums;
using HRMS.Domain.Interfaces;

namespace HRMS.Infrastructure.Reports
{
    /// <summary>
    /// Orchestrator: fetches data, builds AttendanceReportDto, delegates
    /// byte generation to the injected IReportGenerator implementations.
    /// OCP: to add a new format, inject a new IReportGenerator — this class
    /// never changes.
    /// </summary>
    public class AttendanceReportService : IAttendanceReportService
    {
        private readonly IUnitOfWork _uow;
        private readonly IReportGenerator<AttendanceReportDto> _pdfGenerator;
        private readonly IReportGenerator<AttendanceReportDto> _excelGenerator;
        private readonly IReportGenerator<GroupedAttendanceReportDto> _groupPdfGenerator;
        private readonly IReportGenerator<GroupedAttendanceReportDto> _groupExcelGenerator;

        public AttendanceReportService(IUnitOfWork uow, IReportGenerator<AttendanceReportDto> pdfGenerator,
            IReportGenerator<AttendanceReportDto> excelGenerator, IReportGenerator<GroupedAttendanceReportDto> groupPdfGenerator,
            IReportGenerator<GroupedAttendanceReportDto> groupExcelGenerator)
        {
            _uow = uow;
            _pdfGenerator = pdfGenerator;
            _excelGenerator = excelGenerator;
            _groupPdfGenerator = groupPdfGenerator;
            _groupExcelGenerator = groupExcelGenerator;
        }

        #region Employee Report
        public AttendanceReportDto GetReportData(DateTime from, DateTime to, List<string> selectedItems, string companyName = "Zeith Software Pvt Ltd.")
        {
            from = from.Date;
            to = to.Date;
            var logs = _uow.Attendace.GetByDateRange(from, to).ToList();
            var employeeIds = ResolveEmployeeIds(selectedItems);

            var dto = new AttendanceReportDto
            {
                CompanyName = companyName,
                FromDate = from,
                ToDate = to,
                PrintedOn = DateTime.Now,
                LeaveTypes = GetReportLeaveTypes()
            };

            foreach (string item in employeeIds)
            {
                var emp = _uow.Employees.GetByEmployeeID(item);
                if (emp == null) continue;
                dto.Rows.Add(BuildRow(emp, logs, from, to));
            }

            return dto;
        }
        #endregion

        #region Department grouped report
        public GroupedAttendanceReportDto GetDepartmentReportData(DateTime from, DateTime to, IList<string> selectedDepartments, string companyName = "Zeith Software Pvt Ltd.")
        {
            var logs = _uow.Attendace.GetByDateRange(from, to).ToList();
            var employees = _uow.Employees.GetActiveEmployees()
                .Where(e => selectedDepartments.Contains(e.Department.DepartmentId.ToString(), StringComparer.OrdinalIgnoreCase))
                .OrderBy(e => e.Department.ToString())
                .ThenBy(e => e.EmployeeId)
                .ToList();

            var dto = new GroupedAttendanceReportDto
            {
                CompanyName = companyName,
                FromDate = from,
                ToDate = to,
                PrintedOn = DateTime.Now,
                GroupingLabel = "Department",
                LeaveTypes = GetReportLeaveTypes()
            };

            // Group employees by department
            var byDept = employees
                .GroupBy(e => e.Department)
                .OrderBy(g => g.Key.DepartmentId);

            foreach (var deptGroup in byDept)
            {
                var group = new AttendanceGroupDto { GroupName = deptGroup.Where(e=> selectedDepartments.Contains(e.Department.DepartmentId.ToString(), StringComparer.OrdinalIgnoreCase))
                                                                .Select(d=> d.Department.DepartmentName)
                                                                .Distinct().FirstOrDefault() };

                foreach (var emp in deptGroup)
                    group.Rows.Add(BuildRow(emp, logs, from, to));

                dto.Groups.Add(group);
            }

            return dto;
        }
        #endregion

        #region Cadre grouped report
        public GroupedAttendanceReportDto GetCadreReportData(DateTime from, DateTime to, IList<string> selectedCadres, string companyName = "Zeith Software Pvt Ltd.")
        {
            var logs = _uow.Attendace.GetByDateRange(from, to).ToList();
            var employees = _uow.Employees.GetActiveEmployees()
                .Where(e => selectedCadres.Contains(e.Designation.DesignationID, StringComparer.OrdinalIgnoreCase))
                .OrderBy(e => e.Designation.DesignationID)
                .ThenBy(e => e.EmployeeId)
                .ToList();

            var dto = new GroupedAttendanceReportDto
            {
                CompanyName = companyName,
                FromDate = from,
                ToDate = to,
                PrintedOn = DateTime.Now,
                GroupingLabel = "Cadre",
                LeaveTypes = GetReportLeaveTypes()
            };

            // Group employees by designation (cadre)
            var byCadre = employees
                .GroupBy(e => e.Designation)
                .OrderBy(g => g.Key.DesignationID);

            foreach (var cadreGroup in byCadre)
            {
                var group = new AttendanceGroupDto
                {
                    GroupName = cadreGroup.Where(e => selectedCadres.Contains(e.Designation.DesignationID.ToString(), StringComparer.OrdinalIgnoreCase))
                                                .Select(d => d.Designation.DesignationName)
                                                .Distinct().FirstOrDefault()
                };

                foreach (var emp in cadreGroup)
                    group.Rows.Add(BuildRow(emp, logs, from, to));

                dto.Groups.Add(group);
            }

            return dto;
        }
        #endregion

        #region Generate PDF 
        public byte[] GenerateEmployeePdf(DateTime from, DateTime to, List<string> selectedItems, out string fileName)
        {
            var data = GetReportData(from, to, selectedItems);
            fileName = _pdfGenerator.GetFileName(data);
            return _pdfGenerator.Generate(data);
        }

        public byte[] GenerateDepartmentPdf(DateTime from, DateTime to, IList<string> selectedDepartments, out string fileName)
        {
            var data = GetDepartmentReportData(from, to, selectedDepartments);
            fileName = _groupPdfGenerator.GetFileName(data);
            return _groupPdfGenerator.Generate(data);
        }

        public byte[] GenerateCadrePdf(DateTime from, DateTime to, IList<string> selectedCadres, out string fileName)
        {
            var data = GetCadreReportData(from, to, selectedCadres);
            fileName = _groupPdfGenerator.GetFileName(data);
            return _groupPdfGenerator.Generate(data);
        }
        #endregion

        #region Generate Excel
        public byte[] GenerateEmployeeExcel(DateTime from, DateTime to, List<string> selectedItems)
        {
            var data = GetReportData(from, to, selectedItems);
            return _excelGenerator.Generate(data);
        }

        public byte[] GenerateDepartmentExcel(DateTime from, DateTime to, IList<string> selectedDepartments)
        {
            var data = GetDepartmentReportData(from, to, selectedDepartments);
            return _groupExcelGenerator.Generate(data);
        }

        public byte[] GenerateCadreExcel(DateTime from, DateTime to, IList<string> selectedCadres)
        {
            var data = GetCadreReportData(from, to, selectedCadres);
            return _groupExcelGenerator.Generate(data);
        }
        #endregion

        #region Shared: build one AttendanceRowDto for an employee
        private static AttendanceRowDto BuildRow(
            Employee emp,
            IList<Muster> allLogs,
            DateTime from, DateTime to)
        {
            from = from.Date;
            to = to.Date;

            var empLogs = allLogs
                .Where(l => l.EmployeeId == emp.EmployeeId)
                .OrderBy(l => l.TDate)
                .ToLookup(l => l.TDate.Date);

            var row = new AttendanceRowDto
            {
                EmployeeCode = emp.EmployeeId,
                EmployeeName = emp.EmployeeName
            };

            int workDays = 0;

            for (var date = from; date <= to; date = date.AddDays(1))
            {
                var dayLogs = empLogs[date].ToList();

                if (!dayLogs.Any())
                {
                    row.Days.Add(new AttendanceDayDto
                    {
                        Day = date.Day,
                        Date = date,
                        AttId = "00"
                    });
                    continue;
                }

                var first = dayLogs.First();
                var last = dayLogs.Last();

                if (!string.IsNullOrEmpty(first.FirstIn)) workDays++;

                row.Days.Add(new AttendanceDayDto
                {
                    Day = date.Day,
                    Date = date,
                    FirstIn = FormatTime(first.FirstIn),
                    Lastout = FormatTime(last.LastOut),
                    AttId = first.AttId,
                    ShiftId = first.ShiftId
                });
            }

            row.WorkDays = workDays;
            return row;
        }

        private List<string> ResolveEmployeeIds(IList<string> selectedItems)
        {
            if (selectedItems != null && selectedItems.Any())
            {
                return selectedItems
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            return _uow.Employees.GetActiveEmployees()
                .OrderBy(e => e.EmployeeId)
                .Select(e => e.EmployeeId)
                .ToList();
        }

        private static string FormatTime(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";

            DateTime parsedDate;
            if (DateTime.TryParse(value, out parsedDate))
                return parsedDate.ToString("HH:mm");

            TimeSpan parsedTime;
            if (TimeSpan.TryParse(value, out parsedTime))
                return parsedTime.ToString(@"hh\:mm");

            return value;
        }

        private List<ReportLeaveTypeDto> GetReportLeaveTypes()
        {
            return _uow.LeaveTypesMasters.GetActive()
                .OrderBy(l => l.LeaveTypeID)
                .Select(l => new ReportLeaveTypeDto
                {
                    LeaveTypeID = l.LeaveTypeID,
                    Description = string.IsNullOrWhiteSpace(l.Description) ? l.Name : l.Description
                })
                .ToList();
        }
        #endregion
    }

    /// <summary>
    /// OCP: orchestrates the report screen.
    /// Adding a new report type = register a new IReportGenerator in the factory
    /// dictionary below. This class body never changes.
    /// </summary>
    public class ReportScreenService : IReportScreenService
    {
        private readonly IUnitOfWork _uow;
        private readonly IAttendanceReportService _attendanceService;
        private readonly IReportGenerator<HolidayReportDto> _holidayPdfGenerator;
        private readonly IReportGenerator<HolidayReportDto> _holidayExcelGenerator;
        private readonly IReportGenerator<SinglePunchReportDto> _singlePunchPdfGenerator;
        private readonly IReportGenerator<SinglePunchReportDto> _singlePunchExcelGenerator;

        // ── OCP factory: map report type string → generator delegate ──────────
        private readonly Dictionary<string,
            Func<ReportFilterDto, (byte[], string, string)>> _generators;

        public ReportScreenService(
            IUnitOfWork uow,
            IAttendanceReportService attendanceService,
            IReportGenerator<HolidayReportDto> holidayPdfGenerator,
            IReportGenerator<HolidayReportDto> holidayExcelGenerator,
            IReportGenerator<SinglePunchReportDto> singlePunchPdfGenerator,
            IReportGenerator<SinglePunchReportDto> singlePunchExcelGenerator)
        {
            _uow = uow;
            _attendanceService = attendanceService;
            _holidayPdfGenerator = holidayPdfGenerator;
            _holidayExcelGenerator = holidayExcelGenerator;
            _singlePunchPdfGenerator = singlePunchPdfGenerator;
            _singlePunchExcelGenerator = singlePunchExcelGenerator;

            byte[] bytes = new byte[0];
            string fileName = string.Empty;

            // Register each report type here.
            // To add a new report: add one entry — nothing else changes.
            _generators = new Dictionary<string,
                Func<ReportFilterDto, (byte[], string, string)>>(
                    StringComparer.OrdinalIgnoreCase)
            {

                ["Attendance Register"] = filter =>
                {
                    if(filter.Grouping == ReportGrouping.EmployeeWise)
                    {
                         bytes = _attendanceService.GenerateEmployeePdf(filter.ResolvedFrom, filter.ResolvedTo, filter.SelectedItems, out fileName);
                    }
                    else if(filter.Grouping == ReportGrouping.DepartmentWise)
                    {
                        bytes = _attendanceService.GenerateDepartmentPdf(filter.ResolvedFrom, filter.ResolvedTo, filter.SelectedItems, out fileName);
                    }
                    else
                    {
                        bytes = _attendanceService.GenerateCadrePdf(filter.ResolvedFrom, filter.ResolvedTo, filter.SelectedItems, out fileName);
                    }
                    return (bytes, "application/pdf", fileName);
                },

                ["Attendance Register Excel"] = filter =>
                {
                    if (filter.Grouping == ReportGrouping.EmployeeWise)
                    {
                        bytes = _attendanceService.GenerateEmployeeExcel(filter.ResolvedFrom, filter.ResolvedTo, filter.SelectedItems);
                        fileName = string.Format("AttendanceRegister_{0:ddMMMyyyy}_to_{1:ddMMMyyyy}.xlsx",
                            filter.ResolvedFrom, filter.ResolvedTo);
                    }
                    else if (filter.Grouping == ReportGrouping.DepartmentWise)
                    {
                        bytes = _attendanceService.GenerateDepartmentExcel(filter.ResolvedFrom, filter.ResolvedTo, filter.SelectedItems);
                        fileName = string.Format("AttendanceRegister_DepartmentWise_{0:ddMMMyyyy}_to_{1:ddMMMyyyy}.xlsx",
                            filter.ResolvedFrom, filter.ResolvedTo);
                    }
                    else
                    {
                        bytes = _attendanceService.GenerateCadreExcel(filter.ResolvedFrom, filter.ResolvedTo, filter.SelectedItems);
                        fileName = string.Format("AttendanceRegister_DesignationWise_{0:ddMMMyyyy}_to_{1:ddMMMyyyy}.xlsx",
                            filter.ResolvedFrom, filter.ResolvedTo);
                    }
                    return (bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                },

                ["Holiday Register"] = filter =>
                {
                    var data = GetHolidayReportData(filter.ResolvedFrom, filter.ResolvedTo);
                    fileName = _holidayPdfGenerator.GetFileName(data);
                    bytes = _holidayPdfGenerator.Generate(data);
                    return (bytes, _holidayPdfGenerator.ContentType, fileName);
                },

                ["Holiday Register Excel"] = filter =>
                {
                    var data = GetHolidayReportData(filter.ResolvedFrom, filter.ResolvedTo);
                    fileName = _holidayExcelGenerator.GetFileName(data);
                    bytes = _holidayExcelGenerator.Generate(data);
                    return (bytes, _holidayExcelGenerator.ContentType, fileName);
                },

                ["Single Punch Report"] = filter =>
                {
                    var data = GetSinglePunchReportData(filter.ResolvedFrom, filter.ResolvedTo);
                    fileName = _singlePunchPdfGenerator.GetFileName(data);
                    bytes = _singlePunchPdfGenerator.Generate(data);
                    return (bytes, _singlePunchPdfGenerator.ContentType, fileName);
                },

                ["Single Punch Report Excel"] = filter =>
                {
                    var data = GetSinglePunchReportData(filter.ResolvedFrom, filter.ResolvedTo);
                    fileName = _singlePunchExcelGenerator.GetFileName(data);
                    bytes = _singlePunchExcelGenerator.Generate(data);
                    return (bytes, _singlePunchExcelGenerator.ContentType, fileName);
                },

            };
        }

        #region ── Screen data ───────────────────────────────────────────────────────
        public ReportScreenDto GetScreenData()
        {
            var employees = _uow.Employees.GetActiveEmployees()
                .OrderBy(e => e.EmployeeId)
                .Select(e => new AvailableItemDto
                {
                    Value = e.EmployeeId,
                    Display = e.EmployeeId + "  " + e.EmployeeName
                })
                .ToList();

            var departments = _uow.Employees.GetActiveEmployees()
                .Where(e => e.Department != null)
                .Select(e => new
                {
                    e.Department.DepartmentId,
                    e.Department.DepartmentName
                })
                .Distinct()
                .OrderBy(d => d.DepartmentName)
                .Select(d => new AvailableItemDto
                {
                    Value = d.DepartmentId,
                    Display = d.DepartmentName
                })
                .ToList();

            // Cadre = distinct Designations in this implementation
            var cadres = _uow.Employees.GetActiveEmployees()
                .Where(e => e.Designation != null)
                .Select(e => new
                {
                    e.Designation.DesignationID,
                    e.Designation.DesignationName
                })
                .Distinct()
                .OrderBy(d => d.DesignationName)
                .Select(d => new AvailableItemDto
                {
                    Value = d.DesignationID,
                    Display = d.DesignationName
                })
                .ToList();

            var screen = new ReportScreenDto
            {
                AvailableEmployees = employees,
                AvailableDepartments = departments,
                AvailableCadres = cadres
            };

            if (!screen.ReportTypes.Contains("Holiday Register"))
                screen.ReportTypes.Add("Holiday Register");

            if (!screen.ReportTypes.Contains("Single Punch Report"))
                screen.ReportTypes.Add("Single Punch Report");

            return screen;
        }
        #endregion

        #region ── Generate ──────────────────────────────────────────────────────────
        public (byte[] Bytes, string ContentType, string FileName) Generate(ReportFilterDto filter)
        {
            if (string.IsNullOrEmpty(filter.ReportType))
                throw new ArgumentException("Report type must be selected.");

            if (!_generators.TryGetValue(filter.ReportType, out var generator))
                throw new KeyNotFoundException(
                    $"No generator registered for report type '{filter.ReportType}'.");

            return generator(filter);
        }
        #endregion

        private HolidayReportDto GetHolidayReportData(DateTime from, DateTime to, string companyName = "Zeith Software Pvt Ltd.")
        {
            var holidays = _uow.Holidays.GetByDateRange(from, to)
                .Select(h => new HolidayReportRowDto
                {
                    HolidayDate = h.HolidayDate,
                    HolidayName = h.HolidayName,
                    Description = h.Description,
                    IsActive = h.IsActive
                })
                .ToList();

            return new HolidayReportDto
            {
                CompanyName = companyName,
                FromDate = from,
                ToDate = to,
                PrintedOn = DateTime.Now,
                Rows = holidays
            };
        }

        private SinglePunchReportDto GetSinglePunchReportData(DateTime from, DateTime to, string companyName = "Zeith Software Pvt Ltd.")
        {
            var punches = _uow.Attendace.GetDailyTransactionsByDateRange(from, to);

            var rows = punches
                .GroupBy(p => new
                {
                    p.EmpId,
                    AttendanceDate = (p.AttendanceDate ?? p.TransTime).Date
                })
                .Where(g => g.Count() == 1)
                .Select(g =>
                {
                    var punch = g.First();
                    var employee = punch.Employee ?? _uow.Employees.GetByEmployeeID(punch.EmpId);
                    var punchTime = punch.PunchedTime ?? punch.TransTime;

                    return new SinglePunchReportRowDto
                    {
                        EmployeeId = punch.EmpId,
                        EmployeeName = employee == null ? "" : employee.EmployeeName,
                        DepartmentName = employee != null && employee.Department != null ? employee.Department.DepartmentName : "",
                        AttendanceDate = g.Key.AttendanceDate,
                        PunchTime = punchTime,
                        IOFlag = punch.IOFlag,
                        Remarks = punch.Remarks,
                        BadgeReaderNo = punch.BadgeReaderNo.HasValue ? punch.BadgeReaderNo.Value.ToString() : ""
                    };
                })
                .OrderBy(r => r.AttendanceDate)
                .ThenBy(r => r.EmployeeId)
                .ToList();

            return new SinglePunchReportDto
            {
                CompanyName = companyName,
                FromDate = from,
                ToDate = to,
                PrintedOn = DateTime.Now,
                Rows = rows
            };
        }
    }
}
