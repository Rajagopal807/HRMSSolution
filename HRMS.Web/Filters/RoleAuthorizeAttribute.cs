using System;
using System.Web.Mvc;

namespace HRMS.Web.Filters
{
    /// <summary>
    /// OCP: Extend role checking by subclassing — don't modify this filter.
    /// Usage: [RoleAuthorize("Admin","HRManager")]
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class RoleAuthorizeAttribute : AuthorizeAttribute
    {
        public RoleAuthorizeAttribute(params string[] roles)
        {
            Roles = string.Join(",", roles);
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            if (!filterContext.HttpContext.User.Identity.IsAuthenticated)
            {
                base.HandleUnauthorizedRequest(filterContext);
            }
            else
            {
                // Authenticated but wrong role → show Access Denied
                filterContext.Result = new ViewResult { ViewName = "~/Views/Shared/AccessDenied.cshtml" };
            }
        }
    }
}
