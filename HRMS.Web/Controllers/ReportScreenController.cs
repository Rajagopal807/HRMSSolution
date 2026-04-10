using System;
using System.Web.Mvc;
using HRMS.Application.DTOs;
using HRMS.Application.Interfaces;
using HRMS.Domain.Enums;
using HRMS.Web.Filters;

namespace HRMS.Web.Controllers
{
    [Authorize]
    [RoleAuthorize("Admin", "HRManager")]
    public class ReportScreenController : Controller
    {
        private readonly IReportScreenService _reportScreenService;

        public ReportScreenController(IReportScreenService reportScreenService)
        {
            _reportScreenService = reportScreenService;
        }

        // ── GET: /ReportScreen ────────────────────────────────────────────────
        public ActionResult Index()
        {
            var screenData = _reportScreenService.GetScreenData();
            return View(screenData);
        }

        // ── POST: /ReportScreen/Generate ──────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Generate(ReportFilterDto filter)
        {
            // ── Validation ────────────────────────────────────────────────────
            if (string.IsNullOrWhiteSpace(filter.ReportType))
            {
                TempData["Error"] = "Please select a report type.";
                return RedirectToAction("Index");
            }

            // Employee Wise allows empty selection (generates for all employees).
            // Department Wise and Cadre Wise REQUIRE at least one selected item.
            if (filter.Grouping != ReportGrouping.EmployeeWise
                && (filter.SelectedItems == null || filter.SelectedItems.Count == 0))
            {
                TempData["Error"] = filter.Grouping == ReportGrouping.DepartmentWise
                    ? "Please select at least one Department from the list."
                    : "Please select at least one Cadre from the list.";
                return RedirectToAction("Index");
            }

            // ── Build effective ReportType key ────────────────────────────────
            // ReportScreenService._generators uses keys like:
            //   "Attendance Register"       → PDF
            //   "Attendance Register Excel" → Excel
            // We append " Excel" when the user chose the Excel radio button.
            string effectiveReportType = filter.IsExcel
                ? filter.ReportType + " Excel"
                : filter.ReportType;

            // Store effective type back so the service sees it
            filter.ReportType = effectiveReportType;

            // ── Generate ──────────────────────────────────────────────────────
            try
            {
                var (bytes, contentType, fileName) =
                    _reportScreenService.Generate(filter);

                return File(bytes, contentType, fileName);
            }
            catch (NotImplementedException ex)
            {
                TempData["Error"] = "This report type is not yet available: " + ex.Message;
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Report generation failed: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // ── GET: /ReportScreen/GetItems?grouping=1 ────────────────────────────
        // Called via AJAX when the Grouping radio is switched.
        public JsonResult GetItems(int grouping)
        {
            var screenData = _reportScreenService.GetScreenData();

            switch ((ReportGrouping)grouping)
            {
                case ReportGrouping.DepartmentWise:
                    return Json(screenData.AvailableDepartments,
                                JsonRequestBehavior.AllowGet);
                case ReportGrouping.CadreWise:
                    return Json(screenData.AvailableCadres,
                                JsonRequestBehavior.AllowGet);
                default: // EmployeeWise
                    return Json(screenData.AvailableEmployees,
                                JsonRequestBehavior.AllowGet);
            }
        }
    }
}
