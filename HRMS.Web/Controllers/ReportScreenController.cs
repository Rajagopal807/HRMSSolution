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

        // GET: /ReportScreen
        public ActionResult Index()
        {
            var screenData = _reportScreenService.GetScreenData();
            return View(screenData);
        }

        // POST: /ReportScreen/Generate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Generate(ReportFilterDto filter)
        {
            if (string.IsNullOrEmpty(filter.ReportType))
            {
                TempData["Error"] = "Please select a report type.";
                return RedirectToAction("Index");
            }

            if (filter.SelectedItems == null || filter.SelectedItems.Count == 0)
            {
                TempData["Error"] = "Please select at least one item from the list.";
                return RedirectToAction("Index");
            }

            try
            {
                var (bytes, contentType, fileName) =
                    _reportScreenService.Generate(filter);

                return File(bytes, contentType, fileName);
            }
            catch (NotImplementedException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Report generation failed: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // GET: /ReportScreen/GetItems?grouping=1
        // Called via AJAX when the user switches the Grouping radio
        public JsonResult GetItems(int grouping)
        {
            var screenData = _reportScreenService.GetScreenData();

            switch ((ReportGrouping)grouping)
            {
                case ReportGrouping.DepartmentWise:
                    return Json(screenData.AvailableDepartments, JsonRequestBehavior.AllowGet);
                case ReportGrouping.CadreWise:
                    return Json(screenData.AvailableCadres, JsonRequestBehavior.AllowGet);
                default:
                    return Json(screenData.AvailableEmployees, JsonRequestBehavior.AllowGet);
            }
        }
    }
}
