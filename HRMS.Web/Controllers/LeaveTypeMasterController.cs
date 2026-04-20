using System.Web.Mvc;
using HRMS.Application.DTOs;
using HRMS.Application.Interfaces;
using HRMS.Web.Filters;

namespace HRMS.Web.Controllers
{
    [Authorize]
    [RoleAuthorize("Admin", "HRManager")]
    public class LeaveTypeMasterController : Controller
    {
        private readonly ILeaveTypeMasterService _service;

        public LeaveTypeMasterController(ILeaveTypeMasterService service)
        {
            _service = service;
        }

        // GET: /LeaveTypeMaster
        public ActionResult Index()
            => View(_service.GetAll());

        // GET: /LeaveTypeMaster/Create
        public ActionResult Create()
            => View(new CreateLeaveTypeMasterDto { IsActive = true, AllowHalfDay = true });

        // POST: /LeaveTypeMaster/Create
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Create(CreateLeaveTypeMasterDto dto)
        {
            if (!ModelState.IsValid) return View(dto);

            try
            {
                _service.Create(dto);
                TempData["Success"] = $"Leave type '{dto.Name}' created successfully.";
                return RedirectToAction("Index");
            }
            catch (System.InvalidOperationException ex)
            {
                ModelState.AddModelError("Code", ex.Message);
                return View(dto);
            }
        }

        // GET: /LeaveTypeMaster/Edit/5
        public ActionResult Edit(string id)
        {
            var lt = _service.GetById(id);
            if (lt == null) return HttpNotFound();

            ViewBag.editId = id;
            return View(new CreateLeaveTypeMasterDto
            {
                Name           = lt.Name,
                Code           = lt.LeavetypeID,
                MaxDaysPerYear = lt.MaxDaysPerYear,
                AllowHalfDay   = lt.AllowHalfDay,
                IsCarryForward = lt.IsCarryForward,
                IsActive       = lt.IsActive,
                Description    = lt.Description
            });
        }

        // POST: /LeaveTypeMaster/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Edit(string id, CreateLeaveTypeMasterDto dto)
        {
            if (!ModelState.IsValid) return View(dto);

            try
            {
                _service.Update(id, dto);
                TempData["Success"] = "Leave type updated successfully.";
                return RedirectToAction("Index");
            }
            catch (System.InvalidOperationException ex)
            {
                ModelState.AddModelError("Code", ex.Message);
                return View(dto);
            }
        }

        // POST: /LeaveTypeMaster/Delete/5
        [HttpPost, ValidateAntiForgeryToken]
        [RoleAuthorize("Admin")]
        public ActionResult Delete(string id)
        {
            _service.Delete(id);
            TempData["Success"] = "Leave type removed.";
            return RedirectToAction("Index");
        }

        // GET: /LeaveTypeMaster/CheckCode?code=SL&excludeId=0  (AJAX)
        public JsonResult CheckCode(string code, string excludeId = "0")
        {
            bool exists = _service.CodeExists(code, excludeId);
            return Json(!exists, JsonRequestBehavior.AllowGet); // true = valid (not taken)
        }
    }
}
