using System;
using System.Web.Mvc;

namespace HRMS.Web.Filters
{
    public class PageExceptionFilterAttribute : FilterAttribute, IExceptionFilter
    {
        public void OnException(ExceptionContext filterContext)
        {
            if (filterContext == null || filterContext.ExceptionHandled)
                return;

            var exception = filterContext.Exception;
            var request = filterContext.HttpContext.Request;

            if (request.IsAjaxRequest())
            {
                filterContext.Result = new JsonResult
                {
                    Data = new
                    {
                        success = false,
                        error = BuildMessage(exception)
                    },
                    JsonRequestBehavior = JsonRequestBehavior.AllowGet
                };
            }
            else if (string.Equals(request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
            {
                filterContext.Controller.ViewData.ModelState.AddModelError("", BuildMessage(exception));
                filterContext.Controller.ViewData["UnhandledExceptionMessage"] = BuildMessage(exception);

                var action = Convert.ToString(filterContext.RouteData.Values["action"]);
                filterContext.Result = new ViewResult
                {
                    ViewName = action,
                    ViewData = filterContext.Controller.ViewData,
                    TempData = filterContext.Controller.TempData
                };
            }
            else
            {
                filterContext.Controller.TempData["Error"] = BuildMessage(exception);

                var referrer = request.UrlReferrer;
                filterContext.Result = referrer != null
                    ? (ActionResult)new RedirectResult(referrer.ToString())
                    : new RedirectToRouteResult(filterContext.RouteData.Values);
            }

            filterContext.ExceptionHandled = true;
            filterContext.HttpContext.Response.StatusCode = 500;
            filterContext.HttpContext.Response.TrySkipIisCustomErrors = true;
        }

        private static string BuildMessage(Exception exception)
        {
            return "Unexpected error: " + GetInnermostException(exception).Message;
        }

        private static Exception GetInnermostException(Exception exception)
        {
            while (exception.InnerException != null)
                exception = exception.InnerException;

            return exception;
        }
    }
}
