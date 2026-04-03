using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using HRMS.Application.DTOs;
using HRMS.Application.Interfaces;
using HRMS.Web.ViewModels;

namespace HRMS.Web.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        // GET: /Dashboard
        public ActionResult Index()
        {
            var dto = _dashboardService.GetDashboard();


            // Map DashboardDto → DashboardViewModel (Web layer responsibility)
            var vm = new DashboardViewModel();

            vm.TotalEmployees = dto.TotalEmployees;
            vm.ActiveEmployees = dto.ActiveEmployees;
            vm.NewHiresThisMonth = dto.NewHiresThisMonth;
            vm.DepartmentHeadcount = dto.DepartmentHeadcount.ToDictionary(k => k.Key, v => v.Value);
            vm.RecentHires = dto.RecentHires.Select(e => new EmployeeViewModel
            {
                EmployeeId = e.EmployeeId,
                EmployeeName = e.EmployeeName,
                DepartmentID = e.DepartmentID,
                DepartmentName = e.DepartmentName,
                DesignationID = e.DesignationID,
                DesignationName = e.DesignationName,
                JoiningDate = e.JoiningDate,
                Status = e.Status
            }).ToList();
            

            return View(vm);
        }

        private static EmployeeViewModel MapToVm(EmployeeDto e) => new EmployeeViewModel
        {
            EmployeeId = e.EmployeeId,
            EmployeeName = e.EmployeeName,
            Email = e.Email,
            Phone = e.Phone,
            Address = e.Address,
            DateOfBirth = e.DateOfBirth,
            JoiningDate = e.JoiningDate,
            DepartmentID = e.DepartmentID,
            DepartmentName = e.DepartmentName,
            DesignationID = e.DesignationID,
            DesignationName = e.DesignationName,
            Status = e.Status
        };
    }
}