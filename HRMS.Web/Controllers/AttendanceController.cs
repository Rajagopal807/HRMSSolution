using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using HRMS.Application.Interfaces;
using HRMS.Web.Filters;

namespace HRMS.Web.Controllers
{
    [Authorize]
    public class AttendanceController : Controller
    {
        private readonly IAttendanceReportService _reportService;
        private readonly IEmployeeService _employeeService;

        public AttendanceController(IAttendanceReportService reportService, IEmployeeService employeeService)
        {
            _reportService = reportService;
            _employeeService = employeeService;
        }

        // GET: /Attendance
        public ActionResult Index()
        {
            LoadEmployees();

            // Default to current month
            ViewBag.FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1)
                                   .ToString("yyyy-MM-dd");
            ViewBag.ToDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month,
                                   DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month))
                                   .ToString("yyyy-MM-dd");
            return View();
        }

        // GET: /Attendance/DownloadPdf?from=2026-03-01&to=2026-03-31
        [RoleAuthorize("Admin", "HRManager")]
        public ActionResult DownloadPdf(DateTime from, DateTime to, string employeeId = "")
        {
            if (!ValidateRange(from, to)) return RedirectToAction("Index");

            string fileName;
            var bytes = _reportService.GenerateEmployeePdf(from, to, ResolveEmployeeSelection(employeeId), out fileName);
            return File(bytes, "application/pdf", fileName);
        }

        // GET: /Attendance/DownloadExcel?from=2026-03-01&to=2026-03-31
        [RoleAuthorize("Admin", "HRManager")]
        public ActionResult DownloadExcel(DateTime from, DateTime to, string employeeId = "")
        {
            if (!ValidateRange(from, to)) return RedirectToAction("Index");

            var bytes = _reportService.GenerateEmployeeExcel(from, to, ResolveEmployeeSelection(employeeId));
            var fileName = string.Format("AttendanceRegister_{0:ddMMMyyyy}_to_{1:ddMMMyyyy}.xlsx", from, to);
            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        private void LoadEmployees()
        {
            ViewBag.Employees = _employeeService.GetActive()
                .OrderBy(e => e.EmployeeId)
                .Select(e => new SelectListItem
                {
                    Value = e.EmployeeId,
                    Text = e.EmployeeId + " - " + e.EmployeeName
                })
                .ToList();
        }

        private List<string> ResolveEmployeeSelection(string employeeId)
        {
            if (!string.IsNullOrWhiteSpace(employeeId))
                return new List<string> { employeeId.Trim() };

            return new List<string>();
        }

        private bool ValidateRange(DateTime from, DateTime to)
        {
            if (from.Date > to.Date)
            {
                TempData["Error"] = "From date cannot be after To date.";
                return false;
            }

            return true;
        }
    }
}
