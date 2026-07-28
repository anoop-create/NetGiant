using DuoUniversal;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using netGiant.Intranet.BusinessLayer.Models;
using netGiant.Intranet.ViewModels;
using System.Linq;
using Microsoft.AspNet.Identity.EntityFramework;
using System.Collections.Generic;
using System;
using System.Net;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using System.Configuration;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;
using netGiant.Intranet.BusinessLayer.Utilities;
using System.IO;
using System.Web.Http.Filters;

namespace netGiant.Intranet.Controllers
{
    [Authorize]
    public class AccountController : ApplicationController
    {
        public AccountController()
        {
        }

        private ApplicationUserManager _userManager;

        public AccountController(ApplicationUserManager userManager)
        {
            UserManager = userManager;
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        //
        // GET: /Account/Login
        [AllowAnonymous]
        [OutputCache(NoStore = true, Duration = 0)]
        public ActionResult Login(string returnUrl)
        {
            var model = new LoginViewModel();
            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        [AllowAnonymous]
        public ActionResult DefaultAction()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectRoleBased(true);
            }
            else
            {
                return RedirectToAction("Login");
            }
        }

        //
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                Session["LoginModel"] = model;
                Session["returnUrl"] = returnUrl ?? "";
                var appUser = UserManager.FindByEmail(model.Email);

                if (appUser != null)
                {
                    var userName = appUser.UserName;
                    var user = await UserManager.FindAsync(userName, model.Password);

                    var ip = OtherUtilities.GetClientIPAddress(Request);
                    if (ip.StartsWith("10.0.0") || ip.StartsWith("10.101.1") || ip.StartsWith("172.21.224"))
                    {
                        ip = "::1";
                    }

                    if (user != null)
                    {
                        if (!OtherUtilities.IpAddressIsAllowed(ip))
                        {
                            // Duo 2FA
                            if (user.TwoFactorEnabled)
                            {
                                switch (ConfigurationManager.AppSettings["Environment"])
                                {
                                    case "Live":
                                    case "Dev":
                                        {
                                            returnUrl = "https://" + Request.ServerVariables["SERVER_NAME"] + "/netGiant.Intranet/Account/LoginMember";
                                            break;
                                        }
                                    case "Local":
                                        {
                                            returnUrl = "http://localhost:" + Request.ServerVariables["SERVER_PORT"] + "/Account/LoginMember";
                                            break;
                                        }
                                }
                                Client duoClient = new ClientBuilder("DIDKECJ9BO3N01GDM661", "8TL4ifBbHBUFPh7PJJXJ1ffGtcbs7UgrUoGcqshx", "api-544ee06f.duosecurity.com", returnUrl).Build();
                                var isDuoHealthy = await duoClient.DoHealthCheck();
                                string state = DuoUniversal.Client.GenerateState();
                                string promptUri = duoClient.GenerateAuthUri(model.Email, state);
                                return Redirect(promptUri);
                            }
                        }
                        else
                        {
                            return RedirectToAction("LoginMember");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("", "Invalid username or password.");
                        return View(model);
                    }
                }
                else
                {
                    ModelState.AddModelError("", "Invalid username or password.");
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        [AllowAnonymous]
        public async Task<ActionResult> LoginMember()
        {
            LoginViewModel model = (LoginViewModel)Session["LoginModel"];
            if (model == null)
            {
                // If we got here, something went wrong, redisplay login form
                return RedirectToAction("Login");
            }
            string returnUrl = Session["returnUrl"].ToString();
            var appUser = UserManager.FindByEmail(model.Email);
            if (appUser != null)
            {
                var userName = appUser.UserName;
                var user = await UserManager.FindAsync(userName, model.Password);

                await SignInAsync(user, model.RememberMe);
            }
            else
            {
                // If we got here, something went wrong, redisplay login form
                return RedirectToAction("Login");
            }

            Session.Remove("LoginModel");
            Session.Remove("returnUrl");

            if (!string.IsNullOrEmpty(returnUrl))
            {
                return RedirectToLocal(returnUrl);
            }
            else
            {
                return RedirectRoleBased(false);
            }
        }

        //[HttpPost]
        //[AllowAnonymous]
        //[ValidateAntiForgeryToken]
        //public async Task<ActionResult> Login1(LoginViewModel model, string returnUrl)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        var appUser = UserManager.FindByEmail(model.Email);

        //        if (appUser != null)
        //        {
        //            var userName = appUser.UserName;
        //            var user = await UserManager.FindAsync(userName, model.Password);                    

        //            if (user != null)
        //            {
        //                await SignInAsync(user, model.RememberMe);

        //                var ip = OtherUtilities.GetClientIPAddress(Request);
        //                //var ip = (Request.Headers["X-Forwarded-For"] ?? Request.ServerVariables["REMOTE_ADDR"] ?? Request.UserHostAddress).Split(',')[0];
        //                if (ip.StartsWith("10.101.1") && ip.StartsWith("172.21.224"))
        //                {
        //                    ip = "::1";
        //                }

        //                if (!OtherUtilities.IpAddressIsAllowed(ip))
        //                {
        //                    // Duo 2FA
        //                    if (user.TwoFactorEnabled)
        //                    {
        //                        var config = new DuoAuthConfig("api-544ee06f.duosecurity.com", "DIDKECJ9BO3N01GDM661", "8TL4ifBbHBUFPh7PJJXJ1ffGtcbs7UgrUoGcqshx");
        //                        var client = new DuoAuthClient(config);

        //                        var result = await client.AuthPushByUsernameAsync(user.UserName);
        //                        if (result.Result.Result == DuoSecurity.Auth.Http.Results.AuthState.Deny)
        //                        {
        //                            ModelState.AddModelError("", "Access denied.");
        //                            return View(model);
        //                        }
        //                    }
        //                }
        //                if (!string.IsNullOrEmpty(returnUrl))
        //                {
        //                    return RedirectToLocal(returnUrl);
        //                }
        //                else
        //                {
        //                    return RedirectRoleBased(false);
        //                }
        //            }
        //            else
        //            {
        //                ModelState.AddModelError("", "Invalid username or password.");
        //                return View(model);
        //            }                    

        //        }
        //        else
        //        {
        //            ModelState.AddModelError("", "Invalid username or password.");
        //        }
        //    }

        //    // If we got this far, something failed, redisplay form
        //    return View(model);
        //}

        [Authorize(Roles = "IntranetAdmin")]
        public ActionResult RegisterUser()
        {
            RegisterViewModel model = new RegisterViewModel();

            return View(model);
        }

        // GET: /Account/Register
        [Authorize(Roles = "IntranetAdmin")]
        public ActionResult Register()
        {
            RegisterViewModel model = new RegisterViewModel();

            return View(model);
        }

        // POST: /Account/Register
        [HttpPost]
        [Authorize(Roles = "IntranetAdmin")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser() { UserName = model.Email, Email = model.Email, TwoFactorEnabled = true };
                IdentityResult result = await UserManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    //await SignInAsync(user, isPersistent: false);

                    // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=320771
                    // Send an email with this link
                    //string code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
                    // var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
                    //await UserManager.SendEmailAsync(user.Id, "Confirm your account", "Please confirm your account by clicking <a href=\"" + callbackUrl + "\">here</a>");

                    return RedirectToAction("ListUsers", "Account");
                }
                else
                {
                    AddErrors(result);
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "IntranetAdmin")]
        public async Task<ActionResult> SaveNewUser(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser() { UserName = model.Email, Email = model.Email };
                IdentityResult result = await UserManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    //await SignInAsync(user, isPersistent: false);

                    //return RedirectToAction("Index", "Admin");
                    return PartialView("RegisterUser", new RegisterViewModel() { IsNew = true });
                }
                else
                {
                    AddErrors(result);
                }
            }

            model.IsNew = false;
            // If we got this far, something failed, redisplay form
            return PartialView("RegisterUser", model);
        }

        //
        // GET: /Account/ConfirmEmail
        [AllowAnonymous]
        public async Task<ActionResult> ConfirmEmail(string userId, string code)
        {
            var model = new CommonViewModel();

            if (userId == null || code == null)
            {
                return View("Error", model);
            }

            IdentityResult result = await UserManager.ConfirmEmailAsync(userId, code);
            if (result.Succeeded)
            {
                return View("ConfirmEmail", model);
            }
            else
            {
                AddErrors(result);
                return View(model);
            }
        }

        //
        // GET: /Account/ForgotPassword
        [AllowAnonymous]
        public ActionResult ForgotPassword()
        {
            var model = new ForgotPasswordViewModel();
            return View(model);
        }

        //
        // POST: /Account/ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await UserManager.FindByNameAsync(model.Email);
                if (user == null || !(await UserManager.IsEmailConfirmedAsync(user.Id)))
                {
                    ModelState.AddModelError("", "The user either does not exist or is not confirmed.");
                    return View(model);
                }

                // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=320771
                // Send an email with this link
                // string code = await UserManager.GeneratePasswordResetTokenAsync(user.Id);
                // var callbackUrl = Url.Action("ResetPassword", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);		
                // await UserManager.SendEmailAsync(user.Id, "Reset Password", "Please reset your password by clicking <a href=\"" + callbackUrl + "\">here</a>");
                // return RedirectToAction("ForgotPasswordConfirmation", "Account");
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ForgotPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ForgotPasswordConfirmation()
        {
            var model = new CommonViewModel();
            return View(model);
        }

        //
        // GET: /Account/ResetPassword
        [Authorize]
        public ActionResult ResetPassword(string Email)
        {
            ResetPasswordViewModel model = new ResetPasswordViewModel();
            return View(model);
        }

        //
        // POST: /Account/ResetPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await UserManager.FindByNameAsync(model.Email);
                if (user == null)
                {
                    ModelState.AddModelError("", "No user found.");
                    return View(model);
                }
                string code = UserManager.GeneratePasswordResetToken(user.Id);
                IdentityResult result = await UserManager.ResetPasswordAsync(user.Id, code, model.Password);
                if (result.Succeeded)
                {
                    return RedirectToAction("ResetPasswordConfirmation", "Account", model);
                }
                else
                {
                    AddErrors(result);
                    return View(model);
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ResetPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ResetPasswordConfirmation()
        {
            var model = new ResetPasswordViewModel();
            return View(model);
        }















        //Account/ListRoles
        [Authorize(Roles = "IntranetAdmin")]
        public ActionResult ListRoles()
        {
            var model = new ApplicationUserViewModel();
            model.GetRoles();

            return View(model);
        }

        //Account/ListRoleDataAjax - ajax call from above
        [Authorize(Roles = "IntranetAdmin")]
        public ActionResult ListRoleDataAjax([DataSourceRequest] DataSourceRequest request)
        {
            ApplicationUserViewModel model = new ApplicationUserViewModel();
            model.GetRoles();

            var result = model.RoleList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }















        // GET: /Account/CreateRole
        [Authorize(Roles = "IntranetAdmin")]
        public ActionResult CreateRole()
        {
            var model = new ApplicationUserViewModel();

            return View(model);
        }

        // POST: /Account/CreateRole
        [HttpPost]
        [Authorize(Roles = "IntranetAdmin")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateRole(ApplicationUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                using (ApplicationDbContext context = new ApplicationDbContext())
                {
                    var roleStore = new RoleStore<IdentityRole>(context);
                    var roleManager = new RoleManager<IdentityRole>(roleStore);
                    if (!roleManager.RoleExists(model.Role.Name))
                    {
                        var result = await roleManager.CreateAsync(new IdentityRole { Name = model.Role.Name });
                        if (result.Succeeded)
                        {
                            return RedirectToAction("ListRoles", "Account");
                        }
                        else
                        {
                            AddErrors(result);
                        }
                    }
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/DeleteRole
        [Authorize(Roles = "IntranetAdmin")]
        public async Task<ActionResult> DeleteRole(List<string> optionsArray)
        {
            if (ModelState.IsValid)
            {
                using (ApplicationDbContext context = new ApplicationDbContext())
                {
                    var roleStore = new RoleStore<IdentityRole>(context);
                    var roleManager = new RoleManager<IdentityRole>(roleStore);
                    if (roleManager.RoleExists(Convert.ToString(optionsArray[0])))
                    {
                        var role = roleManager.FindByName(Convert.ToString(optionsArray[0]));
                        var result = await roleManager.DeleteAsync(role);
                        if (result.Succeeded)
                        {
                            return RedirectToAction("ListRoles", "Account");
                        }
                        else
                        {
                            AddErrors(result);
                        }
                    }
                }
            }

            // If we got this far, something failed, redisplay form
            return RedirectToAction("ListRoles", "Account");
        }

        [Authorize(Roles = "IntranetAdmin")]
        public ActionResult ListUserRoles(string email)
        {
            return View(new UserRoleViewModel(email));
        }

        [HttpPost]
        [Authorize(Roles = "IntranetAdmin")]
        public async Task<ActionResult> CreateUserRoles(UserRoleViewModel model)
        {
            using (ApplicationDbContext context = new ApplicationDbContext())
            {
                var roleStore = new RoleStore<IdentityRole>(context);
                var roleManager = new RoleManager<IdentityRole>(roleStore);
                var userStore = new UserStore<ApplicationUser>(context);
                var userManager = new UserManager<ApplicationUser>(userStore);
                var user = new ApplicationUser();
                string roleName = "";

                foreach (IdentityRole role in model.AllRoles)
                {
                    if (model.PostedRoles == null)
                    { roleName = null; }
                    else
                    { roleName = Array.Find(model.PostedRoles, x => x.Equals(role.Name)); }
                    if (roleName == null)
                    {
                        user = await userManager.FindByEmailAsync(model.Email);
                        await userManager.RemoveFromRoleAsync(user.Id, role.Name);
                    }
                    else
                    {
                        user = await userManager.FindByEmailAsync(model.Email);
                        var result = await userManager.AddToRoleAsync(user.Id, role.Name);
                    }
                }
            }

            return RedirectToAction("ListUserRoles", "Account", new { email = model.Email });
        }

        //
        // GET: /Account/DeleteUserRole
        [Authorize(Roles = "IntranetAdmin")]
        public async Task<ActionResult> DeleteUserRole(List<string> optionsArray)
        {
            if (ModelState.IsValid)
            {
                using (ApplicationDbContext context = new ApplicationDbContext())
                {
                    var roleStore = new RoleStore<IdentityRole>(context);
                    var roleManager = new RoleManager<IdentityRole>(roleStore);
                    if (roleManager.RoleExists(Convert.ToString(optionsArray[0])))
                    {
                        var role = roleManager.FindByName(Convert.ToString(optionsArray[0]));
                        var result = await roleManager.DeleteAsync(role);
                        if (result.Succeeded)
                        {
                            var model = new ApplicationUserViewModel();
                            model.GetRoles();
                            return PartialView("_ListRoleData", model);
                        }
                        else
                        {
                            AddErrors(result);
                        }
                    }
                }
            }

            // If we got this far, something failed, redisplay form
            return RedirectToAction("ListRoles", "Account");
        }


        //
        // POST: /Account/Disassociate
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "IntranetAdmin")]
        public async Task<ActionResult> Disassociate(string loginProvider, string providerKey)
        {
            ManageMessageId? message = null;
            IdentityResult result = await UserManager.RemoveLoginAsync(User.Identity.GetUserId(), new UserLoginInfo(loginProvider, providerKey));
            if (result.Succeeded)
            {
                var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                await SignInAsync(user, isPersistent: false);
                message = ManageMessageId.RemoveLoginSuccess;
            }
            else
            {
                message = ManageMessageId.Error;
            }
            return RedirectToAction("Manage", new { Message = message });
        }

        //
        // GET: /Account/Manage
        [Authorize(Roles = "IntranetAdmin")]
        public ActionResult Manage(ManageMessageId? message)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.ChangePasswordSuccess ? "Your password has been changed."
                : message == ManageMessageId.SetPasswordSuccess ? "Your password has been set."
                : message == ManageMessageId.RemoveLoginSuccess ? "The external login was removed."
                : message == ManageMessageId.Error ? "An error has occurred."
                : "";
            ViewBag.HasLocalPassword = HasPassword();
            ViewBag.ReturnUrl = Url.Action("Manage");

            var model = new CommonViewModel();
            return View(model);
        }

        //
        // POST: /Account/Manage
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "IntranetAdmin")]
        public async Task<ActionResult> Manage(ManageUserViewModel model)
        {
            bool hasPassword = HasPassword();
            ViewBag.HasLocalPassword = hasPassword;
            ViewBag.ReturnUrl = Url.Action("Manage");
            if (hasPassword)
            {
                if (ModelState.IsValid)
                {
                    IdentityResult result = await UserManager.ChangePasswordAsync(User.Identity.GetUserId(), model.OldPassword, model.NewPassword);
                    if (result.Succeeded)
                    {
                        var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                        await SignInAsync(user, isPersistent: false);
                        return RedirectToAction("Manage", new { Message = ManageMessageId.ChangePasswordSuccess });
                    }
                    else
                    {
                        AddErrors(result);
                    }
                }
            }
            else
            {
                // User does not have a password so remove any validation errors caused by a missing OldPassword field
                ModelState state = ModelState["OldPassword"];
                if (state != null)
                {
                    state.Errors.Clear();
                }

                if (ModelState.IsValid)
                {
                    IdentityResult result = await UserManager.AddPasswordAsync(User.Identity.GetUserId(), model.NewPassword);
                    if (result.Succeeded)
                    {
                        return RedirectToAction("Manage", new { Message = ManageMessageId.SetPasswordSuccess });
                    }
                    else
                    {
                        AddErrors(result);
                    }
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // POST: /Account/ExternalLogin
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ExternalLogin(string provider, string returnUrl)
        {
            // Request a redirect to the external login provider
            return new ChallengeResult(provider, Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl }));
        }

        //
        // GET: /Account/ExternalLoginCallback
        [AllowAnonymous]
        public async Task<ActionResult> ExternalLoginCallback(string returnUrl)
        {
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync();
            if (loginInfo == null)
            {
                return RedirectToAction("Login");
            }

            // Sign in the user with this external login provider if the user already has a login
            var user = await UserManager.FindAsync(loginInfo.Login);
            if (user != null)
            {
                await SignInAsync(user, isPersistent: false);
                return RedirectToLocal(returnUrl);
            }
            else
            {
                // If the user does not have an account, then prompt the user to create an account
                ViewBag.ReturnUrl = returnUrl;
                ViewBag.LoginProvider = loginInfo.Login.LoginProvider;
                return View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel { Email = loginInfo.Email });
            }
        }

        //
        // POST: /Account/LinkLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LinkLogin(string provider)
        {
            // Request a redirect to the external login provider to link a login for the current user
            return new ChallengeResult(provider, Url.Action("LinkLoginCallback", "Account"), User.Identity.GetUserId());
        }

        //
        // GET: /Account/LinkLoginCallback
        public async Task<ActionResult> LinkLoginCallback()
        {
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync(XsrfKey, User.Identity.GetUserId());
            if (loginInfo == null)
            {
                return RedirectToAction("Manage", new { Message = ManageMessageId.Error });
            }
            IdentityResult result = await UserManager.AddLoginAsync(User.Identity.GetUserId(), loginInfo.Login);
            if (result.Succeeded)
            {
                return RedirectToAction("Manage");
            }
            return RedirectToAction("Manage", new { Message = ManageMessageId.Error });
        }

        //
        // POST: /Account/ExternalLoginConfirmation
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string returnUrl)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Manage");
            }

            if (ModelState.IsValid)
            {
                // Get the information about the user from the external login provider
                var info = await AuthenticationManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    return View("ExternalLoginFailure", model);
                }
                var user = new ApplicationUser() { UserName = model.Email, Email = model.Email };
                IdentityResult result = await UserManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    result = await UserManager.AddLoginAsync(user.Id, info.Login);
                    if (result.Succeeded)
                    {
                        await SignInAsync(user, isPersistent: false);

                        // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=320771
                        // Send an email with this link
                        // string code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
                        // var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
                        // SendEmail(user.Email, callbackUrl, "Confirm your account", "Please confirm your account by clicking this link");

                        return RedirectToLocal(returnUrl);
                    }
                }
                AddErrors(result);
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        //
        // POST: /Account/LogOff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            AuthenticationManager.SignOut();
            return RedirectToAction("Login");
        }

        //
        // GET: /Account/ExternalLoginFailure
        [AllowAnonymous]
        public ActionResult ExternalLoginFailure()
        {
            var model = new CommonViewModel();
            return View(model);
        }

        [ChildActionOnly]
        public ActionResult RemoveAccountList()
        {
            var linkedAccounts = UserManager.GetLogins(User.Identity.GetUserId());
            ViewBag.ShowRemoveButton = HasPassword() || linkedAccounts.Count > 1;
            return PartialView("_RemoveAccountPartial", linkedAccounts);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && UserManager != null)
            {
                UserManager.Dispose();
                UserManager = null;
            }
            base.Dispose(disposing);
        }










        //Account/ListUsers
        [Authorize(Roles = "IntranetAdmin")]
        public ActionResult ListUsers()
        {
            ApplicationUserViewModel model = new ApplicationUserViewModel();
            return View(model);
        }

        //Account/ListUserDataAjax
        [Authorize(Roles = "IntranetAdmin")]
        public ActionResult ListUserDataAjax([DataSourceRequest] DataSourceRequest request)
        {
            ApplicationUserViewModel model = new ApplicationUserViewModel();
            model.GetUsers();

            DataSourceResult result = model.UserList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

















        [Authorize(Roles = "IntranetAdmin")]
        public async Task<ActionResult> DeleteUser(List<string> optionsArray)
        {
            if (ModelState.IsValid)
            {
                string id = optionsArray[0].ToString();

                if (id == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }

                var user = await UserManager.FindByIdAsync(id);
                var logins = user.Logins;

                foreach (var login in logins.ToList())
                {
                    await UserManager.RemoveLoginAsync(login.UserId, new UserLoginInfo(login.LoginProvider, login.ProviderKey));
                }

                var rolesForUser = await UserManager.GetRolesAsync(id);

                if (rolesForUser.Count() > 0)
                {
                    foreach (var item in rolesForUser.ToList())
                    {
                        // item should be the name of the role
                        var result = await UserManager.RemoveFromRoleAsync(user.Id, item);
                    }
                }

                await UserManager.DeleteAsync(user);
            }

            return RedirectToAction("ListUsers");
        }

        private ActionResult RedirectRoleBased(bool defaultAction)
        {
            bool isPMS = User.IsInRole("PMSAdmin") || User.IsInRole("PMSReader");
            bool isQA = User.IsInRole("QAAdmin") || User.IsInRole("QAReader");
            bool isFullAdmin = User.IsInRole("IntranetAdmin");
            bool isSEO = User.IsInRole("SEO");
            bool isReports = User.IsInRole("Reports");

            if (isFullAdmin)
            {
                return RedirectToAction("ProductIndex", "Product", new { area = "PMS" });
            }
            else if (isSEO)
            {
                return RedirectToAction("ManufacturerNotesIndex", "ManufacturerNotes", new { area = "PMS" });
            }
            else if (isPMS && isQA == false)
            {
                return RedirectToAction("ProductIndex", "Product", new { area = "PMS" });
            }
            else if (isQA && isPMS == false)
            {
                return RedirectToAction("Index", "QA");
            }
            else if (isQA && isPMS)
            {
                return RedirectToAction("ProductIndex", "Product", new { area = "PMS" });
            }
            else if (isReports)
            {
                return RedirectToAction("Kpi", "Reports");
            }
            else
            {
                if (defaultAction)
                {
                    return RedirectToAction("NoUserRoles");
                }
                else
                {
                    return RedirectToAction("DefaultAction");
                }
            }
        }

        [Authorize]
        public ActionResult NoUserRoles()
        {
            return View(new CommonViewModel());
        }

        #region Helpers
        // Used for XSRF protection when adding external logins
        private const string XsrfKey = "XsrfId";

        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        private async Task SignInAsync(ApplicationUser user, bool isPersistent)
        {
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ExternalCookie);
            AuthenticationManager.SignIn(new AuthenticationProperties() { IsPersistent = isPersistent }, await user.GenerateUserIdentityAsync(UserManager));
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private bool HasPassword()
        {
            var user = UserManager.FindById(User.Identity.GetUserId());
            if (user != null)
            {
                return user.PasswordHash != null;
            }
            return false;
        }

        private void SendEmail(string email, string callbackUrl, string subject, string message)
        {
            // For information on sending mail, please visit http://go.microsoft.com/fwlink/?LinkID=320771
        }

        public enum ManageMessageId
        {
            ChangePasswordSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
            Error
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        private class ChallengeResult : HttpUnauthorizedResult
        {
            public ChallengeResult(string provider, string redirectUri)
                : this(provider, redirectUri, null)
            {
            }

            public ChallengeResult(string provider, string redirectUri, string userId)
            {
                LoginProvider = provider;
                RedirectUri = redirectUri;
                UserId = userId;
            }

            public string LoginProvider { get; set; }
            public string RedirectUri { get; set; }
            public string UserId { get; set; }

            public override void ExecuteResult(ControllerContext context)
            {
                var properties = new AuthenticationProperties() { RedirectUri = RedirectUri };
                if (UserId != null)
                {
                    properties.Dictionary[XsrfKey] = UserId;
                }
                context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
            }
        }
        #endregion
    }
}