using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Data.Entity;
using HRMS.Infrastructure.Data;
using HRMS.Web.App_Start;
using HRMS.Infrastructure.Identity;

namespace HRMS.Web
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {

            DatabaseSetup.Initialize();
            
            // EF: Use DatabaseSeeder to create + seed tables on first run
             Database.SetInitializer(new DatabaseSeeder());

            using (var ctx = new ApplicationDbContext())
            {
                ctx.Database.Initialize(true);
            }

            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            AutofacConfig.Configure();
        }
    }
}
