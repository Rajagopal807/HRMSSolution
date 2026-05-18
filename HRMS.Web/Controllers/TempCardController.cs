using HRMS.Application.DTOs;
using HRMS.Application.Interfaces;
using HRMS.Application.Services;
using HRMS.Domain.Entities;
using HRMS.Infrastructure.Data;
using System.Linq;
using System.Web.Mvc;

namespace HRMS.Web.Controllers
{
    public class TempCardController : Controller
    {
        private readonly ITempCardService _service;

        public TempCardController(ITempCardService service)
        {
            _service = service;
        }

        // ── GET: /TempCard ────────────────────────────────────────────────────
        public ActionResult Index()
            => View(_service.GetAll());

        // ── GET: /TempCard/Create ─────────────────────────────────────────────
        public ActionResult Create()
        {
            ViewBag.Employees = _service.GetEmployees();
            return View(new SaveTempCardDto());
        }

        // ── POST: /TempCard/Create ────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Create(SaveTempCardDto dto)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Employees = _service.GetEmployees();
                return View(dto);
            }

            var (id, error) = _service.Create(dto);
            if (!string.IsNullOrEmpty(error))
            {
                ModelState.AddModelError("", error);
                ViewBag.Employees = _service.GetEmployees();
                return View(dto);
            }

            TempData["Success"] = $"Temp Card '{dto.TempCardId}' created successfully.";
            return RedirectToAction("Index");
        }

        // ── GET: /TempCard/Edit/5 ─────────────────────────────────────────────
        public ActionResult Edit(string id)
        {
            var card = _service.GetById(id);
            if (card == null) return HttpNotFound();

            ViewBag.EditId = id;
            ViewBag.Employees = _service.GetEmployees();

            return View(new SaveTempCardDto
            {
                TempCardId = card.TempCardId,
                EmployeeId = card.EmployeeId
            });
        }

        // ── POST: /TempCard/Edit/5 ────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Edit(string id, SaveTempCardDto dto)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.EditId = id;
                ViewBag.Employees = _service.GetEmployees();
                return View(dto);
            }

            var error = _service.Update(id, dto);
            if (!string.IsNullOrEmpty(error))
            {
                ModelState.AddModelError("", error);
                ViewBag.EditId = id;
                ViewBag.Employees = _service.GetEmployees();
                return View(dto);
            }

            TempData["Success"] = "Temp Card updated successfully.";
            return RedirectToAction("Index");
        }

        // ── POST: /TempCard/Delete/5 ──────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Delete(string id)
        {
            _service.Delete(id);
            TempData["Success"] = "Temp Card removed.";
            return RedirectToAction("Index");
        }

        // ── GET: /TempCard/CheckCardId?cardId=T001&excludeId=0  (AJAX) ────────
        public JsonResult CheckCardId(string cardId, string excludeId = "0")
        {
            bool taken = _service.TempCardIdExists(cardId, excludeId);
            return Json(!taken, JsonRequestBehavior.AllowGet); // true = available
        }

        // ── GET: /TempCard/CheckEmployee?employeeId=EMP-0001&excludeId=0 ──────
        public JsonResult CheckEmployee(string employeeId, string excludeId = "0")
        {
            bool taken = _service.EmployeeAlreadyHasCard(employeeId, excludeId);
            return Json(!taken, JsonRequestBehavior.AllowGet); // true = available
        }
    }
}