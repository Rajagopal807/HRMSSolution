using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using HRMS.Infrastructure.Data;
using HRMS.Web.Filters;

namespace HRMS.Web.Controllers
{
    [Authorize]
    [RoleAuthorize("Admin", "HRManager")]
    public class LiveAttendanceController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.RealtimeUrl = ConfigurationManager.AppSettings["AttendSyncRealtimeUrl"]
                ?? "http://localhost:65147/api/attendance/realtime";

            return View();
        }

        public JsonResult ResolveEmployee(string punchId)
        {
            if (string.IsNullOrWhiteSpace(punchId))
            {
                return Json(new
                {
                    found = false,
                    punchId = "",
                    source = "",
                    employeeId = "",
                    employeeName = ""
                }, JsonRequestBehavior.AllowGet);
            }

            var lookupId = punchId.Trim().Length <11 ? punchId.Trim().PadLeft(11,'0') : punchId.Trim();

            using (var db = new ApplicationDbContext())
            {
                var employee = db.Employees
                    .AsNoTracking()
                    .Where(e => !e.IsDeleted && e.EmployeeId == lookupId)
                    .Select(e => new
                    {
                        e.EmployeeId,
                        e.EmployeeName
                    })
                    .FirstOrDefault();

                if (employee != null)
                {
                    return Json(new
                    {
                        found = true,
                        punchId = lookupId,
                        source = "employee",
                        employeeId = employee.EmployeeId,
                        employeeName = employee.EmployeeName
                    }, JsonRequestBehavior.AllowGet);
                }

                var card = db.TempCards
                    .AsNoTracking()
                    .Include(t => t.Employee)
                    .Where(t => !t.IsDeleted && t.TempCardNo == lookupId)
                    .Select(t => new
                    {
                        t.TempCardNo,
                        EmployeeId = t.EmpId,
                        EmployeeName = t.Employee.EmployeeName
                    })
                    .FirstOrDefault();

                return Json(new
                {
                    found = card != null,
                    punchId = lookupId,
                    source = card == null ? "" : "tempCard",
                    employeeId = card?.EmployeeId ?? "",
                    employeeName = card?.EmployeeName ?? ""
                }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}
