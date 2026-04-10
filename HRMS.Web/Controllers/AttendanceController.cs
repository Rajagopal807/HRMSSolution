using System;
using System.Web.Mvc;
using HRMS.Application.Interfaces;
using HRMS.Web.Filters;

namespace HRMS.Web.Controllers
{
    [Authorize]
    public class AttendanceController : Controller
    {
        private readonly IAttendanceReportService _reportService;

        public AttendanceController(IAttendanceReportService reportService)
        {
            _reportService = reportService;
        }

        // GET: /Attendance
        public ActionResult Index()
        {
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
        public ActionResult DownloadPdf(DateTime from, DateTime to)
        {
            if (from > to)
            {
                TempData["Error"] = "From date cannot be after To date.";
                return RedirectToAction("Index");
            }

            //var bytes = _reportService.GenerateEmployeePdf(from, to);
            var bytes = new byte[0];
            var fileName = string.Format("AttendanceRegister_{0:MMMMyyyy}.pdf", from);
            return File(bytes, "application/pdf", fileName);
        }

        // GET: /Attendance/DownloadExcel?from=2026-03-01&to=2026-03-31
        [RoleAuthorize("Admin", "HRManager")]
        public ActionResult DownloadExcel(DateTime from, DateTime to)
        {
            if (from > to)
            {
                TempData["Error"] = "From date cannot be after To date.";
                return RedirectToAction("Index");
            }

            //var bytes = _reportService.GenerateExcel(from, to);
            var bytes = new byte[0];
            var fileName = string.Format("AttendanceRegister_{0:MMMMyyyy}.xlsx", from);
            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
    }
}
