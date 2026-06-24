using HRMS.Web.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Mvc;

namespace HRMS.Web.Filters
{
    /// <summary>
    /// Marker attribute for controllers that must stay logged in across long-lived
    /// operations (e.g. live SSE/streaming pages) and therefore should not be
    /// force-logged-out by SessionAuthorizeAttribute's idle timeout check.
    /// Authentication ([Authorize]/[RoleAuthorize]) is still fully enforced —
    /// only the idle-timeout-based session expiry is skipped.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ExemptFromIdleTimeoutAttribute : Attribute
    {
    }

    public class SessionAuthorizeAttribute : AuthorizeAttribute
    {
        private readonly int _timeoutMinutes = 20;

        // Cache controller-name -> exemption lookups so we don't reflect on every request.
        private static readonly Dictionary<string, bool> _idleTimeoutExemptCache =
            new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        private static readonly object _cacheLock = new object();

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            // Skip login page
            _ = httpContext.Request.Url.AbsolutePath.ToLower();
            var routeData = httpContext.Request.RequestContext.RouteData;
            var controller = routeData.Values["controller"]?.ToString().ToLower();
            var action = routeData.Values["action"]?.ToString().ToLower();

            if (controller == "account" && (action == "index" || action == "login" || action == "forgotpassword" || action == "resetpassword"))
                return true;

            //if (url.Contains("/account/login") || url.Contains("/account/forgotpassword") || url.Contains("/account/resetpassword"))
            //    return true;

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

            // Idle timeout check — skipped for controllers explicitly marked with
            // [ExemptFromIdleTimeout] (e.g. LiveAttendanceController), since those
            // pages keep a long-lived live connection open and should not be
            // force-logged-out just because the idle timer elapsed. Authentication
            // and role checks above/elsewhere are NOT affected.
            if (!IsIdleTimeoutExempt(controller) && session.LastActivity.HasValue)
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
            filterContext.HttpContext.GetOwinContext().Authentication.SignOut();
            filterContext.Result = new RedirectToRouteResult(
                new System.Web.Routing.RouteValueDictionary(
                    new
                    {
                        controller = "Account",
                        action = "Login",
                        sessionExpired = true
                    })
            );
        }

        private static bool IsIdleTimeoutExempt(string controllerName)
        {
            if (string.IsNullOrEmpty(controllerName))
                return false;

            lock (_cacheLock)
            {
                if (_idleTimeoutExemptCache.TryGetValue(controllerName, out var cached))
                    return cached;

                var typeName = controllerName + "Controller";
                var controllerType = typeof(SessionAuthorizeAttribute).Assembly
                    .GetTypes()
                    .FirstOrDefault(t => typeof(IController).IsAssignableFrom(t)
                        && string.Equals(t.Name, typeName, StringComparison.OrdinalIgnoreCase));

                var isExempt = controllerType != null
                    && controllerType.GetCustomAttribute<ExemptFromIdleTimeoutAttribute>() != null;

                _idleTimeoutExemptCache[controllerName] = isExempt;
                return isExempt;
            }
        }
    }
}
