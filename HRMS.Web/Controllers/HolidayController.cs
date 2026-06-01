using HRMS.Application.DTOs;
using HRMS.Application.Interfaces;
using HRMS.Web.Filters;
using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace HRMS.Web.Controllers
{
    [Authorize]
    [RoleAuthorize("Admin", "HRManager")]
    public class HolidayController : Controller
    {
        private readonly IHolidayService _service;

        public HolidayController(IHolidayService service)
        {
            _service = service;
        }

        public ActionResult Index()
            => View(_service.GetAll());

        public ActionResult Create()
            => View(new SaveHolidayDto { HolidayDate = DateTime.Today, IsActive = true });

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Create(SaveHolidayDto dto)
        {
            if (!ModelState.IsValid) return View(dto);

            try
            {
                _service.Create(dto);
                TempData["Success"] = $"Holiday '{dto.HolidayName}' created successfully.";
                return RedirectToAction("Index");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("HolidayDate", ex.Message);
                return View(dto);
            }
        }

        public ActionResult Edit(int id)
        {
            var holiday = _service.GetById(id);
            if (holiday == null) return HttpNotFound();

            ViewBag.EditId = id;
            return View(new SaveHolidayDto
            {
                Id = holiday.Id,
                HolidayDate = holiday.HolidayDate,
                HolidayName = holiday.HolidayName,
                Description = holiday.Description,
                IsActive = holiday.IsActive
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Edit(int id, SaveHolidayDto dto)
        {
            if (!ModelState.IsValid) return View(dto);

            try
            {
                _service.Update(id, dto);
                TempData["Success"] = "Holiday updated successfully.";
                return RedirectToAction("Index");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("HolidayDate", ex.Message);
                return View(dto);
            }
            catch (KeyNotFoundException)
            {
                return HttpNotFound();
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        [RoleAuthorize("Admin")]
        public ActionResult Delete(int id)
        {
            _service.Delete(id);
            TempData["Success"] = "Holiday removed.";
            return RedirectToAction("Index");
        }

        public JsonResult CheckDate(DateTime holidayDate, int excludeId = 0)
        {
            bool exists = _service.DateExists(holidayDate, excludeId);
            return Json(!exists, JsonRequestBehavior.AllowGet);
        }
    }
}
