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
                    .Where(l => l.EmployeeId == emp.Id)
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

                    bool hasIn = string.IsNullOrEmpty(first.FirstIn);
                    bool hasOut = string.IsNullOrEmpty(last.LastOut);

                    if (hasIn) workDays++;

                    row.Days.Add(new AttendanceDayDto
                    {
                        Day = day,
                        FirstIn = first.FirstIn,
                        Lastout = last.LastOut,
                        AttId = first.AttId,
                        ShiftId = first.ShiftId
                    });
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
}
