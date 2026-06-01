using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Data.Entity;
using HRMS.Infrastructure.Data;
using HRMS.Web.App_Start;
using HRMS.Infrastructure.Identity;
using System.Web;
using System;
using System.IO;

namespace HRMS.Web
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            using (var ctx = new ApplicationDbContext())
            {
                if (!ctx.Database.Exists())
                {
                    DatabaseSetup.Initialize();
                    Database.SetInitializer<ApplicationDbContext>(new DatabaseSeeder());
                    ctx.Database.Initialize(true);
                }
                else
                {
                    Database.SetInitializer<ApplicationDbContext>(null);
                    DatabaseSetup.EnsureHolidayTable();
                }
            }

            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            AutofacConfig.Configure();
        }

        protected void Application_Error()
        {
            var context = HttpContext.Current;
            var ex = Server.GetLastError();

            if (context != null && ex != null)
            {
                try
                {
                    ErrorLogger.Log(ex);
                }
                catch
                {
                    // Avoid throwing inside error handler
                }
            }
        }

        protected void Application_End()
        {
            var context = HttpContext.Current;
            if (context != null)
            {
                try
                {
                    // 1. Clear Session
                    context.Session?.Clear();
                    context.Session?.Abandon();

                    // 2. Sign out OWIN
                    context.GetOwinContext().Authentication.SignOut();

                    // 3. Clear Cookies
                    foreach (string cookie in context.Request.Cookies.AllKeys)
                    {
                        var c = new HttpCookie(cookie)
                        {
                            Expires = DateTime.Now.AddDays(-1)
                        };
                        context.Response.Cookies.Add(c);
                    }
                }
                catch
                {
                    // Avoid throwing inside error handler
                }

                // 4. Redirect to Login
                context.Response.Clear();
                context.Response.Redirect("~/Account/Login");
            }
        }
    }

    public static class ErrorLogger
    {
        public static string Log(Exception ex)
        {
            string errorId = Guid.NewGuid().ToString();

            string logPath = HttpContext.Current.Server.MapPath("~/App_Data/ErrorLog.txt");

            File.AppendAllText(logPath,
                $"ErrorId: {errorId}\nTime: {DateTime.Now}\nMessage: {ex.Message}\nStackTrace: {ex.StackTrace}\n\n");

            return errorId;
        }
    }
}
