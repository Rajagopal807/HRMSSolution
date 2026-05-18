using HRMS.Web.Filters;
using System.Web;
using System.Web.Mvc;

namespace HRMS.Web.App_Start
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new PageExceptionFilterAttribute());
            filters.Add(new SessionAuthorizeAttribute());
        }
    }
}
