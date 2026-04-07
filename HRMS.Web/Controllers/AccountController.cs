using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using HRMS.Infrastructure.Data;
using HRMS.Web.ViewModels;
using HRMS.Common;
using HRMS.Web.Session;
using HRMS.Domain.Interfaces;
using HRMS.Application.Interfaces;
using System;

namespace HRMS.Web.Controllers
{
    public class AccountController : Controller
    {

        private readonly IAuditService _audit;
        private readonly IPasswordResetService _resetService;

        public AccountController(IAuditService audit, IPasswordResetService resetService)
        {
            _audit = audit;
            _resetService = resetService;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private UserManager<ApplicationUser> CreateUserManager()
        {
            return new UserManager<ApplicationUser>(
                new UserStore<ApplicationUser>(ApplicationDbContext.Create()));
        }

        private IAuthenticationManager AuthManager
        {
            get { return HttpContext.GetOwinContext().Authentication; }
        }

        // ── GET: /Account/Login ───────────────────────────────────────────────

        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            // Redirect already-authenticated users straight to Dashboard
            if (User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Dashboard");

            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginViewModel());
        }

        // ── POST: /Account/Login ──────────────────────────────────────────────

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
            {

                return View(model);
            }

            var userManager = CreateUserManager();
            var user = await userManager.FindAsync(model.Userid.ToUpper(), model.Password);

            if (user == null)
            {
                ModelState.AddModelError("", "Invalid UserID or password.");
                return View(model);
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError("", "Your account has been deactivated. Please contact the administrator.");
                return View(model);
            }

            // OWIN Identity
            var identity = await userManager.CreateIdentityAsync(
                user, DefaultAuthenticationTypes.ApplicationCookie);

            AuthManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            AuthManager.SignIn(
                new AuthenticationProperties { IsPersistent = model.RememberMe },
                identity);

            // SESSION SYNC (IMPORTANT)
            var session = new HttpUserSession(Session);
            session.Create(user.UserID, user.UserName, user.Role.ToString());

            // Redirect to returnUrl if local, otherwise Dashboard
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) && returnUrl != "/\"")
                return Redirect(returnUrl);

            _audit.Log("Login", "Account", $"User {user.UserName} logged in");

            return RedirectToAction("Index", "Dashboard");
        }

        // ── POST: /Account/Logout ─────────────────────────────────────────────

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            AuthManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);

            var session = new HttpUserSession(Session);
            session.Clear();

            return RedirectToAction("Login", "Account");
        }

        // ── Get: /Account/ForgotPassword and ResetPassword ─────────────────────────────────────────────
        [HttpGet]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        [HttpGet]
        public ActionResult ResetPassword(string token)
        {
            return View(new ResetPasswordViewModel { Token = token });
        }

        // ── POST: /Account/ForgotPassword ─────────────────────────────────────────────
        [HttpPost]
        public async Task<ActionResult> ForgotPassword(string userId)
        {
            string baseUrl = Request.Url.GetLeftPart(UriPartial.Authority);

            try
            {
                var userManager = CreateUserManager();
                var user = await userManager.FindByNameAsync(userId.ToUpper());
                if(user == null)
                {
                    throw new Exception("Invalid user");
                }
                var link = await _resetService.GenerateResetLinkAsync(userId, baseUrl);

                ViewBag.ResetLink = link; // No email → show directly
                return View("ShowResetLink");
            }
            catch
            {
                ModelState.AddModelError("", "Invalid user");
                return View();
            }
        }

        [HttpPost]
        public async Task<ActionResult> ResetPassword(string token, string password)
        {
            var result = await _resetService.ResetPasswordAsync(token, password);

            if (!result)
            {
                ModelState.AddModelError("", "Invalid or expired token");
                return View();
            }

            return RedirectToAction("Login");
        }

        // ── GET: Session Check ────────────────────────────────────────
        [HttpGet]
        [AllowAnonymous]
        public JsonResult CheckSession()
        {
            var session = new HttpUserSession(Session);

            return Json(new
            {
                isActive = session.IsAuthenticated
            }, JsonRequestBehavior.AllowGet);
        }

        // ── GET: /Account/AccessDenied ────────────────────────────────────────

        [AllowAnonymous]
        public ActionResult AccessDenied()
        {
            return View("~/Views/Shared/AccessDenied.cshtml");
        }
    }
}