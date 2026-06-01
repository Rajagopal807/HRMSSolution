using HRMS.Application.DTOs;
using HRMS.Application.Interfaces;
using HRMS.Web.Filters;
using System.Security.Claims;
using System.Web.Mvc;

namespace HRMS.Web.Controllers
{
    [Authorize]
    public class LeaveApplicationController : Controller
    {
        private readonly ILeaveApplicationService  _appService;
        private readonly ILeaveTypeMasterService   _typeService;

        public LeaveApplicationController(
            ILeaveApplicationService appService,
            ILeaveTypeMasterService  typeService)
        {
            _appService  = appService;
            _typeService = typeService;
        }

        // GET: /LeaveApplication
        public ActionResult Index()
        {
            var list = User.IsInRole("Admin") || User.IsInRole("HRManager")
                ? _appService.GetAll()
                : _appService.GetByEmployee(GetCurrentEmployeeId());
            return View(list);
        }

        // GET: /LeaveApplication/Apply
        public ActionResult Apply()
        {
            var screen = _appService.GetApplyScreen();
            return View(screen);
        }

        // POST: /LeaveApplication/Apply
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Apply([Bind(Prefix = "Form")] ApplyLeaveDto dto)
        {
            NormalizeEmployeeId(dto);

            if (!ModelState.IsValid)
            {
                var screen = _appService.GetApplyScreen();
                screen.Form = dto;
                return View(screen);
            }

            var (id, error) = _appService.Apply(dto, User.Identity.Name);
            if (!string.IsNullOrEmpty(error))
            {
                ModelState.AddModelError("", error);
                var screen = _appService.GetApplyScreen();
                screen.Form = dto;
                return View(screen);
            }

            

            TempData["Success"] = "Leave application submitted successfully.";
            return RedirectToAction("Index");
        }

        // GET: /LeaveApplication/Pending
        [RoleAuthorize("Admin", "HRManager")]
        public ActionResult Pending()
        {
            var pending = _appService.GetPending();
            return View(pending);
        }

        // POST: /LeaveApplication/Approve/5
        [HttpPost, ValidateAntiForgeryToken]
        [RoleAuthorize("Admin", "HRManager")]
        public ActionResult Approve(int id, string reviewNotes)
        {
            _appService.Approve(id, User.Identity.Name, reviewNotes ?? "Approved");
            TempData["Success"] = "Leave application approved.";
            return RedirectToAction("Pending");
        }

        // POST: /LeaveApplication/Reject/5
        [HttpPost, ValidateAntiForgeryToken]
        [RoleAuthorize("Admin", "HRManager")]
        public ActionResult Reject(int id, string reviewNotes)
        {
            _appService.Reject(id, User.Identity.Name, reviewNotes ?? "Rejected");
            TempData["Info"] = "Leave application rejected.";
            return RedirectToAction("Pending");
        }

        public ActionResult Edit(int id)
        {
            var data = _appService.GetByApplicationId(id); // you need this method in service

            if (data == null)
                return HttpNotFound();

            var screen = _appService.GetApplyScreen();
            ApplyLeaveDto applyLeaveDto = new ApplyLeaveDto();
            applyLeaveDto.ApplicationId = data.ApplicationId;
            applyLeaveDto.EmployeeId = data.EmployeeId;
            applyLeaveDto.LeaveTypeMasterId = data.LeaveTypeMasterId;
            applyLeaveDto.FromDate = data.FromDate;
            applyLeaveDto.ToDate = data.ToDate;
            applyLeaveDto.Session = data.Session;
            applyLeaveDto.Reason = data.Reason;
            screen.Form = applyLeaveDto;

            return View("Apply", screen); // reuse Apply screen
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Prefix = "Form")] ApplyLeaveDto dto)
        {
            NormalizeEmployeeId(dto);

            if (!ModelState.IsValid)
            {
                var screen = _appService.GetApplyScreen();
                screen.Form = dto;
                return View("Apply", screen);
            }

            var error = _appService.Update(dto, User.Identity.Name);

            if (!string.IsNullOrEmpty(error))
            {
                ModelState.AddModelError("", error);
                var screen = _appService.GetApplyScreen();
                screen.Form = dto;
                return View("Apply", screen);
            }

            TempData["Success"] = "Leave updated successfully.";
            return RedirectToAction("Index");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            _appService.Delete(id, User.Identity.Name);

            TempData["Success"] = "Leave deleted successfully.";
            return RedirectToAction("Index");
        }

        // GET: /LeaveApplication/GetLeaveTypeInfo?id=1  (AJAX — returns AllowHalfDay)
        public JsonResult GetLeaveTypeInfo(string id)
        {
            var lt = _typeService.GetById(id);
            if (lt == null) return Json(new { }, JsonRequestBehavior.AllowGet);
            return Json(new
            {
                allowHalfDay   = lt.AllowHalfDay,
                maxDaysPerYear = lt.MaxDaysPerYear,
                name           = lt.Name
            }, JsonRequestBehavior.AllowGet);
        }

        // Helper — returns 0 if no employee record linked to current user
        private string GetCurrentEmployeeId()
        {
            var identity = User.Identity as ClaimsIdentity;
            return identity?.FindFirst("EmployeeId")?.Value ?? "0";
        }

        private static void NormalizeEmployeeId(ApplyLeaveDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.EmployeeId)) return;

            var employeeId = dto.EmployeeId.Trim();
            if (employeeId.Length < 11 && long.TryParse(employeeId, out _))
            {
                employeeId = employeeId.PadLeft(11, '0');
            }

            dto.EmployeeId = employeeId;
        }
    }
}
