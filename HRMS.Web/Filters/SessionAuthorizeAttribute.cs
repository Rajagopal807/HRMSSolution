using HRMS.Web.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace HRMS.Web.Filters
{
    public class SessionAuthorizeAttribute : AuthorizeAttribute
    {
        private readonly int _timeoutMinutes = 20;

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            // Skip login page
            var url = httpContext.Request.Url.AbsolutePath.ToLower();
            if (url.Contains("/account/login") || url.Contains("/account/forgotpassword") || url.Contains("/account/resetpassword"))
                return true;

            // Identity check first
            if (!httpContext.User.Identity.IsAuthenticated)
                return false;

            var session = new HttpUserSession(httpContext.Session);

            // Session missing → recreate OR logout
            if (!session.IsAuthenticated)
            {
                session.Clear();
                return false;
            }

            // Idle timeout check
            if (session.LastActivity.HasValue)
            {
                var idle = DateTime.UtcNow - session.LastActivity.Value;

                if (idle.TotalMinutes > _timeoutMinutes)
                {
                    session.Clear();
                    httpContext.GetOwinContext().Authentication.SignOut();
                    return false;
                }
            }

            // Update activity
            session.UpdateActivity();

            return true;
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            filterContext.Result = new RedirectResult("~/Account/Login?sessionExpired=true");
        }
    }
}