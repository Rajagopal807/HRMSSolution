using System.Reflection;
using System.Web.Mvc;
using Autofac;
using Autofac.Integration.Mvc;
using HRMS.Application.DTOs;
using HRMS.Application.Interfaces;
using HRMS.Application.Services;
using HRMS.Domain.Interfaces;
using HRMS.Infrastructure.Data;
using HRMS.Infrastructure.Identity;
using HRMS.Infrastructure.Reports;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace HRMS.Web.App_Start
{
    public static class AutofacConfig
    {
        public static void Configure()
        {
            var builder = new ContainerBuilder();

            // Register MVC controllers
            builder.RegisterControllers(Assembly.GetExecutingAssembly());

            // Infrastructure — DbContext (per-request lifetime)
            builder.RegisterType<ApplicationDbContext>().AsSelf().InstancePerRequest();

            // Unit of Work + Repositories
            builder.RegisterType<UnitOfWork>().As<IUnitOfWork>().InstancePerRequest();

            // Application Services
            builder.RegisterType<EmployeeService>().As<IEmployeeService>().InstancePerRequest();
            builder.RegisterType<DepartmentService>().As<IDepartmentService>().InstancePerRequest();
            builder.RegisterType<DesignationService>().As<IDesignationService>().InstancePerRequest();
            builder.RegisterType<LeaveTypeMasterService>().As<ILeaveTypeMasterService>().InstancePerRequest();
            builder.RegisterType<LeaveApplicationService>().As<ILeaveApplicationService>().InstancePerRequest();
            builder.RegisterType<HolidayService>().As<IHolidayService>().InstancePerRequest();
            builder.RegisterType<DashboardService>().As<IDashboardService>().InstancePerRequest();
            builder.RegisterType<AuditRepository>().As<IAuditService>().InstancePerRequest();
            builder.RegisterType<ApplicationDbContext>().AsSelf().InstancePerRequest();
            builder.RegisterType<UserStore<ApplicationUser>>().As<IUserStore<ApplicationUser>>().InstancePerRequest();
            builder.RegisterType<UserManager<ApplicationUser>>().AsSelf().InstancePerRequest();
            builder.RegisterType<IdentityService>().As<IIdentityService>().InstancePerRequest();
            builder.RegisterType<SecureTokenService>().As<IPasswordResetService>().InstancePerRequest();
            builder.RegisterType<AttendancePdfReport>().Named<IReportGenerator<AttendanceReportDto>>("pdf").InstancePerRequest();
            builder.RegisterType<AttendanceExcelReport>().Named<IReportGenerator<AttendanceReportDto>>("excel").InstancePerRequest();
            builder.RegisterType<GroupedAttendancePdfReport>().Named<IReportGenerator<GroupedAttendanceReportDto>>("grouped-pdf").InstancePerRequest();
            builder.RegisterType<GroupedAttendanceExcelReport>().Named<IReportGenerator<GroupedAttendanceReportDto>>("grouped-excel").InstancePerRequest();
            builder.Register(c => new AttendanceReportService(c.Resolve<IUnitOfWork>(),
                    c.ResolveNamed<IReportGenerator<AttendanceReportDto>>("pdf"),
                    c.ResolveNamed<IReportGenerator<AttendanceReportDto>>("excel"),
                     c.ResolveNamed<IReportGenerator<GroupedAttendanceReportDto>>("grouped-pdf"),
                     c.ResolveNamed<IReportGenerator<GroupedAttendanceReportDto>>("grouped-excel")))
                   .As<IAttendanceReportService>()
                   .InstancePerRequest();
            builder.RegisterType<ReportScreenService>().As<IReportScreenService>().InstancePerRequest();
            builder.RegisterType<TempcardService>().As<ITempCardService>().InstancePerRequest();
            builder.RegisterType<AttendanceTransactionService>().As<IAttendanceTransactionService>().InstancePerRequest();

            var container = builder.Build();
            DependencyResolver.SetResolver(new AutofacDependencyResolver(container));
        }
    }
}
