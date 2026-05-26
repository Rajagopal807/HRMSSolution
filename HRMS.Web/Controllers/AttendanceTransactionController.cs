using HRMS.Application.DTOs;
using HRMS.Application.Interfaces;
using HRMS.Web.Filters;
using System;
using System.Globalization;
using System.Web.Mvc;

namespace HRMS.Web.Controllers
{
    [Authorize]
    [RoleAuthorize("Admin", "HRManager")]
    public class AttendanceTransactionController : Controller
    {
        private readonly IAttendanceTransactionService _service;

        public AttendanceTransactionController(IAttendanceTransactionService service)
        {
            _service = service;
        }

        public ActionResult Index(string employeeId = null, int? month = null, int? year = null)
        {
            return View(_service.GetScreen(employeeId, month, year));
        }

        public ActionResult EditPunches(string employeeId, DateTime attendanceDate)
        {
            if (string.IsNullOrWhiteSpace(employeeId))
                return RedirectToAction("Index");

            return View(_service.GetPunchScreen(employeeId, attendanceDate, TempData["PunchStatus"] as string));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult AddPunch(SaveManualPunchDto form)
        {
            var error = _service.AddPunch(form);
            if (!string.IsNullOrEmpty(error))
                TempData["Error"] = error;
            else
                TempData["PunchStatus"] = "Punch added successfully.";

            return RedirectToAction("EditPunches", new { employeeId = form.EmployeeId, attendanceDate = form.AttendanceDate.ToString("yyyy-MM-dd") });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult ModifyPunch(SaveManualPunchDto form)
        {
            var error = _service.UpdatePunch(form);
            if (!string.IsNullOrEmpty(error))
                TempData["Error"] = error;
            else
                TempData["PunchStatus"] = "Punch modified successfully.";

            return RedirectToAction("EditPunches", new { employeeId = form.EmployeeId, attendanceDate = form.AttendanceDate.ToString("yyyy-MM-dd") });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult DeletePunch(string employeeId, DateTime attendanceDate, string ioFlag, string transTime)
        {
            if (!DateTime.TryParse(transTime, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedTransTime))
            {
                TempData["Error"] = "Invalid selected punch key.";
                return RedirectToAction("EditPunches", new { employeeId, attendanceDate = attendanceDate.ToString("yyyy-MM-dd") });
            }

            var error = _service.DeletePunch(employeeId, ioFlag, parsedTransTime);
            if (!string.IsNullOrEmpty(error))
                TempData["Error"] = error;
            else
                TempData["PunchStatus"] = "Punch deleted successfully.";

            return RedirectToAction("EditPunches", new { employeeId, attendanceDate = attendanceDate.ToString("yyyy-MM-dd") });
        }
    }
}
