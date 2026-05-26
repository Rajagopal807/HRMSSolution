using HRMS.Application.DTOs;
using HRMS.Application.Interfaces;
using HRMS.Domain.Entities;
using HRMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace HRMS.Application.Services
{
    public class AttendanceTransactionService : IAttendanceTransactionService
    {
        private readonly IUnitOfWork _uow;

        public AttendanceTransactionService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public AttendanceTransactionScreenDto GetScreen(string employeeId, int? month, int? year)
        {
            var selectedMonth = month.GetValueOrDefault(DateTime.Today.Month);
            var selectedYear = year.GetValueOrDefault(DateTime.Today.Year);

            var screen = new AttendanceTransactionScreenDto
            {
                Employees = _uow.Employees.GetActiveEmployees()
                    .OrderBy(e => e.EmployeeId)
                    .Select(e => new EmployeeDto
                    {
                        EmployeeId = e.EmployeeId,
                        EmployeeName = e.EmployeeName,
                        DepartmentName = e.Department != null ? e.Department.DepartmentName : ""
                    })
                    .ToList(),
                SelectedEmpId = employeeId,
                SelectedMonth = selectedMonth,
                SelectedYear = selectedYear
            };

            if (string.IsNullOrWhiteSpace(employeeId))
                return screen;

            var employee = _uow.Employees.GetByEmployeeID(employeeId);
            screen.SelectedEmpName = employee == null ? "" : employee.EmployeeName;

            var from = new DateTime(selectedYear, selectedMonth, 1);
            var to = new DateTime(selectedYear, selectedMonth, DateTime.DaysInMonth(selectedYear, selectedMonth));

            var musterRows = _uow.Attendace.GetByEmployee(employeeId, from, to).ToList();
            var punches = _uow.Attendace.GetDailyTransactions(employeeId, from, to).ToList();

            var days = musterRows.Select(m => m.TDate.Date)
                .Union(punches.Where(p => p.AttendanceDate.HasValue).Select(p => p.AttendanceDate.Value.Date))
                .OrderBy(d => d)
                .ToList();

            screen.Rows = days.Select(day =>
            {
                var muster = musterRows.FirstOrDefault(m => m.TDate.Date == day);
                var dayPunches = punches.Where(p => p.AttendanceDate.HasValue && p.AttendanceDate.Value.Date == day).ToList();
                return BuildRow(day, muster, dayPunches);
            }).ToList();

            screen.HasResult = true;
            return screen;
        }

        public EditPunchesScreenDto GetPunchScreen(string employeeId, DateTime attendanceDate, string statusMessage = null)
        {
            var employee = _uow.Employees.GetByEmployeeID(employeeId);
            var punches = _uow.Attendace.GetDailyTransactionsForDay(employeeId, attendanceDate)
                .Select(MapPunch)
                .ToList();

            return new EditPunchesScreenDto
            {
                EmployeeId = employeeId,
                EmployeeName = employee == null ? "" : employee.EmployeeName,
                AttendanceDate = attendanceDate.Date,
                StatusMessage = statusMessage,
                Punches = punches,
                Form = new SaveManualPunchDto
                {
                    EmployeeId = employeeId,
                    AttendanceDate = attendanceDate.Date,
                    TransDateStr = DateTime.Now.ToString("yyyy-MM-dd"),
                    IOFlag = "I",
                    IsDeleted = false
                }
            };
        }

        public string AddPunch(SaveManualPunchDto dto)
        {
            if (!TryBuildPunch(dto, out var punch, out var error))
                return error;

            if (_uow.Attendace.GetDailyTransaction(punch.EmpId, punch.IOFlag, punch.TransTime) != null)
                return "A punch with the same employee, IO flag and transaction time already exists.";

            _uow.Attendace.AddDailyTransaction(punch);
            _uow.SaveChanges();
            return null;
        }

        public string UpdatePunch(SaveManualPunchDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.OriginalIOFlag) || string.IsNullOrWhiteSpace(dto.OriginalTransTimeStr))
                return "Select an existing punch before modifying.";

            if (!DateTime.TryParse(dto.OriginalTransTimeStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var originalTransTime))
                return "Invalid selected punch key.";

            var existing = _uow.Attendace.GetDailyTransaction(dto.EmployeeId, dto.OriginalIOFlag, originalTransTime);
            if (existing == null)
                return "Selected punch was not found.";

            if (!TryBuildPunch(dto, out var updated, out var error))
                return error;

            var keyChanged = existing.IOFlag != updated.IOFlag || existing.TransTime != updated.TransTime;
            if (keyChanged && _uow.Attendace.GetDailyTransaction(updated.EmpId, updated.IOFlag, updated.TransTime) != null)
                return "Another punch already uses the new IO flag and transaction time.";

            if (keyChanged)
            {
                _uow.Attendace.DeleteDailyTransaction(existing);
                _uow.Attendace.AddDailyTransaction(updated);
            }
            else
            {
                existing.AttendanceDate = updated.AttendanceDate;
                existing.PunchedTime = updated.PunchedTime;
                existing.ActualIOFlag = updated.ActualIOFlag;
                existing.OPFlag = updated.OPFlag;
                existing.Remarks = updated.Remarks;
                existing.Deleted = updated.Deleted;
                existing.BadgeReaderNo = updated.BadgeReaderNo;
                existing.UpdatedAt = DateTime.UtcNow;
                _uow.Attendace.UpdateDailyTransaction(existing);
            }

            _uow.SaveChanges();
            return null;
        }

        public string DeletePunch(string employeeId, string ioFlag, DateTime transTime)
        {
            var punch = _uow.Attendace.GetDailyTransaction(employeeId, ioFlag, transTime);
            if (punch == null)
                return "Selected punch was not found.";

            _uow.Attendace.DeleteDailyTransaction(punch);
            _uow.SaveChanges();
            return null;
        }

        private static AttendanceTransactionRowDto BuildRow(DateTime day, Muster muster, IList<DailyTransactions> punches)
        {
            var firstIn = ParseTimeSpan(muster?.FirstIn) ?? punches.Select(p => p.PunchedTime).Where(t => t.HasValue).Select(t => t.Value.TimeOfDay).DefaultIfEmpty().Min();
            var lastOut = ParseTimeSpan(muster?.LastOut) ?? punches.Select(p => p.PunchedTime).Where(t => t.HasValue).Select(t => t.Value.TimeOfDay).DefaultIfEmpty().Max();

            return new AttendanceTransactionRowDto
            {
                Date = day,
                FirstIn = firstIn == TimeSpan.Zero ? (TimeSpan?)null : firstIn,
                LastOut = lastOut == TimeSpan.Zero ? (TimeSpan?)null : lastOut,
                Shift = string.IsNullOrWhiteSpace(muster?.ShiftId) ? "GEN" : muster.ShiftId,
                AttId = string.IsNullOrWhiteSpace(muster?.AttId) ? (punches.Any() ? "P" : "00") : muster.AttId,
                TotalHrs = MinutesToTimeSpan(muster?.HrsWorked),
                Worked = MinutesToTimeSpan(muster?.HrsWorked),
                Extra = MinutesToTimeSpan(muster?.ExtraHours),
                Late = MinutesToTimeSpan(muster?.LatePunch),
                Early = MinutesToTimeSpan(muster?.EarlyOut),
                OutPass = (muster?.OutPasses ?? 0).ToString(),
                PunchCount = punches.Count,
                OT = MinutesToTimeSpan((muster?.SingleOT ?? 0) + (muster?.DoubleOT ?? 0))
            };
        }

        private static ManualPunchDto MapPunch(DailyTransactions p)
        {
            return new ManualPunchDto
            {
                Key = BuildKey(p),
                EmployeeId = p.EmpId,
                AttendanceDate = (p.AttendanceDate ?? p.TransTime).Date,
                PunchedTime = (p.PunchedTime ?? p.TransTime).TimeOfDay,
                TransactionDate = p.TransTime.Date,
                TransTime = p.TransTime.TimeOfDay,
                IOFlag = p.IOFlag,
                IsDeleted = string.Equals(p.Deleted, "Y", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(p.Deleted, "1", StringComparison.OrdinalIgnoreCase),
                Remarks = p.Remarks,
                BranchNo = p.BadgeReaderNo.HasValue ? p.BadgeReaderNo.Value.ToString() : ""
            };
        }

        private static bool TryBuildPunch(SaveManualPunchDto dto, out DailyTransactions punch, out string error)
        {
            punch = null;
            error = null;

            if (dto == null)
            {
                error = "Punch details are required.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(dto.EmployeeId))
            {
                error = "Employee is required.";
                return false;
            }

            if (!TimeSpan.TryParse(dto.PunchedTimeStr, out var punchedTime))
            {
                error = "Punch time must be in HH:mm format.";
                return false;
            }

            DateTime transDate = DateTime.Today;
            if (!string.IsNullOrWhiteSpace(dto.TransDateStr) && !DateTime.TryParse(dto.TransDateStr, out transDate))
            {
                error = "Transaction date is invalid.";
                return false;
            }

            var transTime = transDate.Date.Add(punchedTime);
            var attendanceDate = dto.AttendanceDate.Date;

            punch = new DailyTransactions
            {
                EmpId = dto.EmployeeId,
                IOFlag = NormalizeIOFlag(dto.IOFlag),
                ActualIOFlag = NormalizeIOFlag(dto.IOFlag),
                OPFlag = "M",
                TransTime = transTime,
                AttendanceDate = attendanceDate,
                PunchedTime = attendanceDate.Add(punchedTime),
                Deleted = dto.IsDeleted ? "Y" : "N",
                Remarks = dto.Remarks,
                BadgeReaderNo = ParseNullableInt(dto.BranchNo),
                CreatedAt = DateTime.UtcNow
            };

            return true;
        }

        private static string NormalizeIOFlag(string ioFlag)
        {
            if (string.Equals(ioFlag, "O", StringComparison.OrdinalIgnoreCase)
                || string.Equals(ioFlag, "Out", StringComparison.OrdinalIgnoreCase))
                return "O";

            return "I";
        }

        private static int? ParseNullableInt(string value)
        {
            if (int.TryParse(value, out var number))
                return number;

            return null;
        }

        private static TimeSpan? ParseTimeSpan(string value)
        {
            if (TimeSpan.TryParse(value, out var time))
                return time;

            if (DateTime.TryParse(value, out var dateTime))
                return dateTime.TimeOfDay;

            return null;
        }

        private static TimeSpan MinutesToTimeSpan(int? minutes)
        {
            return TimeSpan.FromMinutes(minutes.GetValueOrDefault());
        }

        private static string BuildKey(DailyTransactions punch)
        {
            return punch.IOFlag + "|" + punch.TransTime.ToString("o", CultureInfo.InvariantCulture);
        }
    }
}
