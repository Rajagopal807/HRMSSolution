using System;
using System.Collections.Generic;
using System.Linq;
using HRMS.Application.DTOs;
using HRMS.Application.Interfaces;
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

        public AttendanceReportService(
            IUnitOfWork uow,
            IReportGenerator<AttendanceReportDto> pdfGenerator,
            IReportGenerator<AttendanceReportDto> excelGenerator)
        {
            _uow = uow;
            _pdfGenerator = pdfGenerator;
            _excelGenerator = excelGenerator;
        }

        // ── Build DTO ─────────────────────────────────────────────────────────
        public AttendanceReportDto GetReportData(
            DateTime from, DateTime to, string companyName = "Zeith Software Pvt Ltd.")
        {
            var logs = _uow.Attendace.GetByDateRange(from, to).ToList();
            var employees = _uow.Employees.GetActiveEmployees().ToList();

            var dto = new AttendanceReportDto
            {
                CompanyName = companyName,
                FromDate = from,
                ToDate = to,
                PrintedOn = DateTime.Now
            };

            int daysInMonth = DateTime.DaysInMonth(from.Year, from.Month);

            foreach (var emp in employees.OrderBy(e => e.EmployeeId))
            {
                var empLogs = logs
                    .Where(l => l.EmployeeId == emp.EmployeeId)
                    .ToLookup(l => l.TDate .Day);

                var row = new AttendanceRowDto
                {
                    EmployeeCode = emp.EmployeeId,
                    EmployeeName = emp.EmployeeName
                };

                int workDays = 0;

                for (int day = 1; day <= 31; day++)
                {
                    if (day > daysInMonth)
                    {
                        // Pad empty cells for months shorter than 31 days
                        row.Days.Add(new AttendanceDayDto { Day = day, IsPadDay = true });
                        continue;
                    }

                    var dayLogs = empLogs[day].ToList();

                    if (!dayLogs.Any())
                    {
                        row.Days.Add(new AttendanceDayDto
                        {
                            Day = day,
                            AttId = "00"
                        });
                        continue;
                    }

                    var first = dayLogs.First();
                    var last = dayLogs.Last();

                    bool hasIn = !string.IsNullOrEmpty(first.FirstIn);
                    bool hasOut = !string.IsNullOrEmpty(last.LastOut);

                    if (hasIn) workDays++;

                    AttendanceDayDto attendanceDayDto = new AttendanceDayDto();
                    attendanceDayDto.Day = day;
                    attendanceDayDto.FirstIn = !string.IsNullOrEmpty(first.FirstIn) ? DateTime.Parse(first.FirstIn).ToString("HH:mm") : "";
                    attendanceDayDto.Lastout = !string.IsNullOrEmpty(last.LastOut) ? DateTime.Parse(last.LastOut).ToString("HH:mm") : "";
                    attendanceDayDto.AttId = first.AttId;
                    attendanceDayDto.ShiftId = first.ShiftId;

                    row.Days.Add(attendanceDayDto);
                }

                row.WorkDays = workDays;
                dto.Rows.Add(row);
            }

            return dto;
        }

        // ── Generate PDF ──────────────────────────────────────────────────────
        public byte[] GeneratePdf(DateTime from, DateTime to)
        {
            var data = GetReportData(from, to);
            return _pdfGenerator.Generate(data);
        }

        // ── Generate Excel ────────────────────────────────────────────────────
        public byte[] GenerateExcel(DateTime from, DateTime to)
        {
            var data = GetReportData(from, to);
            return _excelGenerator.Generate(data);
        }
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

        // ── OCP factory: map report type string → generator delegate ──────────
        private readonly Dictionary<string,
            Func<ReportFilterDto, (byte[], string, string)>> _generators;

        public ReportScreenService(
            IUnitOfWork uow,
            IAttendanceReportService attendanceService)
        {
            _uow = uow;
            _attendanceService = attendanceService;

            // Register each report type here.
            // To add a new report: add one entry — nothing else changes.
            _generators = new Dictionary<string,
                Func<ReportFilterDto, (byte[], string, string)>>(
                    StringComparer.OrdinalIgnoreCase)
            {
                ["Attendance Register"] = filter =>
                {
                    if (filter.ExportType == "EXCEL")
                    {
                        var bytes = _attendanceService.GenerateExcel(
                            filter.ResolvedFrom, filter.ResolvedTo);

                        var fileName = $"AttendanceRegister_{filter.ResolvedFrom:MMMMyyyy}.xlsx";

                        return (bytes,
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                            fileName);
                    }
                    else
                    {
                        var bytes = _attendanceService.GeneratePdf(
                            filter.ResolvedFrom, filter.ResolvedTo);

                        var fileName = $"AttendanceRegister_{filter.ResolvedFrom:MMMMyyyy}.pdf";

                        return (bytes, "application/pdf", fileName);
                    }
                },
                //["Attendance Register"] = filter =>
                //{
                //    var bytes = _attendanceService.GeneratePdf(
                //                       filter.ResolvedFrom, filter.ResolvedTo);
                //    var fileName = string.Format(
                //                       "AttendanceRegister_{0:MMMMyyyy}.pdf",
                //                       filter.ResolvedFrom);
                //    return (bytes, "application/pdf", fileName);
                //},

                //["Attendance Register Excel"] = filter =>
                //{
                //    var bytes = _attendanceService.GenerateExcel(
                //                       filter.ResolvedFrom, filter.ResolvedTo);
                //    var fileName = string.Format(
                //                       "AttendanceRegister_{0:MMMMyyyy}.xlsx",
                //                       filter.ResolvedFrom);
                //    return (bytes,
                //        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                //        fileName);
                //},

                // ── Placeholders — wire up when those generators are built ────
                //["Employee Directory"] = filter =>
                //    throw new NotImplementedException(
                //        "Employee Directory report generator not yet registered."),

                //["Leave Summary"] = filter =>
                //    throw new NotImplementedException(
                //        "Leave Summary report generator not yet registered."),

                //["Payroll Summary"] = filter =>
                //    throw new NotImplementedException(
                //        "Payroll Summary report generator not yet registered."),
            };
        }

        // ── Screen data ───────────────────────────────────────────────────────
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

            //var departments = _uow.Employees.GetDepartmentHeadcount()
            //    .Keys
            //    .OrderBy(d => d)
            //    .Select(d => new AvailableItemDto { Value = d, Display = d })
            //    .ToList();

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

            return new ReportScreenDto
            {
                AvailableEmployees = employees,
                AvailableDepartments = departments,
                AvailableCadres = cadres
            };
        }

        // ── Generate ──────────────────────────────────────────────────────────
        public (byte[] Bytes, string ContentType, string FileName)
            Generate(ReportFilterDto filter)
        {
            if (string.IsNullOrEmpty(filter.ReportType))
                throw new ArgumentException("Report type must be selected.");

            if (!_generators.TryGetValue(filter.ReportType, out var generator))
                throw new KeyNotFoundException(
                    $"No generator registered for report type '{filter.ReportType}'.");

            return generator(filter);
        }
    }
}
