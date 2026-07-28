using System;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Net;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using DP001Website.Models;
using DP001DataAccess.Entities;
using System.Collections.Generic;
using Microsoft.AspNet.Identity.EntityFramework;
using DP001BusinessLogic.ViewModels;

namespace DP001Website.Controllers
{
    [Authorize]
    public class AccountController : ApplicationController
    {
        //private ApplicationSignInManager _signInManager;
        //private ApplicationUserManager _userManager;

        public AccountController()
        {
        }

        //public AccountController(ApplicationUserManager userManager, ApplicationSignInManager signInManager )
        //{
        //    UserManager = userManager;
        //    SignInManager = signInManager;
        //}

        //public ApplicationSignInManager SignInManager
        //{
        //    get
        //    {
        //        return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
        //    }
        //    private set
        //    {
        //        _signInManager = value;
        //    }
        //}

        //public ApplicationUserManager UserManager
        //{
        //    get
        //    {
        //        return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
        //    }
        //    private set
        //    {
        //        _userManager = value;
        //    }
        //}

        //
        // GET: /Account/Login
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        //
        // GET: /Account/LoginUnbranded
        [AllowAnonymous]
        public ActionResult LoginUnbranded(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        //
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction("Login");
            }

            // This doesn't count login failures towards account lockout
            // To enable password failures to trigger account lockout, change to shouldLockout: true
            var result = await SignInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, shouldLockout: false);
            switch (result)
            {
                case SignInStatus.Success:
                    if (!string.IsNullOrEmpty(returnUrl))
                    {
                        return RedirectToLocal(returnUrl);
                    }
                    else
                    {
                        return RedirectToAction("Index", "Dashboard");
                    }
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.RequiresVerification:
                    return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "Invalid login attempt.");
                    return View(model);
            }
        }

        //
        // GET: /Account/VerifyCode
        [AllowAnonymous]
        public async Task<ActionResult> VerifyCode(string provider, string returnUrl, bool rememberMe)
        {
            // Require that the user has already logged in via username/password or external login
            if (!await SignInManager.HasBeenVerifiedAsync())
            {
                return View("Error");
            }
            return View(new VerifyCodeViewModel { Provider = provider, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        //
        // POST: /Account/VerifyCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> VerifyCode(VerifyCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // The following code protects for brute force attacks against the two factor codes. 
            // If a user enters incorrect codes for a specified amount of time then the user account 
            // will be locked out for a specified amount of time. 
            // You can configure the account lockout settings in IdentityConfig
            var result = await SignInManager.TwoFactorSignInAsync(model.Provider, model.Code, isPersistent:  model.RememberMe, rememberBrowser: model.RememberBrowser);
            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(model.ReturnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "Invalid code.");
                    return View(model);
            }
        }

        //
        // GET: /Account/Register
        [AllowAnonymous]
        public ActionResult Register()
        {
            RegisterViewModel model = new RegisterViewModel();
            model.StageIndicator = 1;
            model.ContractType = "Freemium";
            return View(model);
        }

        //
        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel model)
        {
            
            ModelState.Remove("StageIndicator");
            if (ModelState.IsValid)
            {
                if (model.StageIndicator == 2)
                {
                    // Submission of final form
                    // Take Payment/ Set up Payment Details
                    // Create Tenant
                    // Create User

                    return RedirectToAction("Confirm", "Account");
                }
                model.StageIndicator += 1;
            }
            return View(model);
        }

        //
        // GET: /Account/Confirm
        [AllowAnonymous]
        public ActionResult Confirm()
        {
            RegisterViewModel model = new RegisterViewModel();
            model.StageIndicator = 4;
            return View(model);
        }

        //
        // POST: /Account/Register
        //[HttpPost]
        //[AllowAnonymous]
        //[ValidateAntiForgeryToken]
        //public async Task<ActionResult> Register(RegisterViewModel model)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
        //        var result = await UserManager.CreateAsync(user, model.Password);
        //        if (result.Succeeded)
        //        {
        //            await SignInManager.SignInAsync(user, isPersistent:false, rememberBrowser:false);

        //            // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=320771
        //            // Send an email with this link
        //            // string code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
        //            // var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
        //            // await UserManager.SendEmailAsync(user.Id, "Confirm your account", "Please confirm your account by clicking <a href=\"" + callbackUrl + "\">here</a>");

        //            return RedirectToAction("Index", "Home");
        //        }
        //        AddErrors(result);
        //    }

        //    // If we got this far, something failed, redisplay form
        //    return View(model);
        //}

        //
        // POST: /Account/Confirm
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Confirm(RegisterViewModel model)
        {
            return View(model);
        }

        //
        // GET: /Account/ConfirmEmail
        [AllowAnonymous]
        public async Task<ActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return View("Error");
            }
            var result = await UserManager.ConfirmEmailAsync(userId, code);
            return View(result.Succeeded ? "ConfirmEmail" : "Error");
        }

        //
        // GET: /Account/ForgotPassword
        [AllowAnonymous]
        public ActionResult ForgotPassword()
        {
            return View();
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
                    // Don't reveal that the user does not exist or is not confirmed
                    return View("ForgotPasswordConfirmation");
                }

                // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=320771
                // Send an email with this link
                string code = await UserManager.GeneratePasswordResetTokenAsync(user.Id);
                var callbackUrl = Url.Action("ResetPasswordExternal", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
                await UserManager.SendEmailAsync(user.Id, "Reset Password", "Please reset your password by clicking <a href=\"" + callbackUrl + "\">here</a>");
                return RedirectToAction("ForgotPasswordConfirmation", "Account");
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ForgotPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        //
        // GET: /Account/ResetPassword
        [Authorize]
        public ActionResult ResetPassword(string email)
        {
            return email == null ? View("Error") : View();
        }

        //
        // GET: /Account/ResetPassword
        [AllowAnonymous]
        public ActionResult ResetPasswordExternal(string code)
        {
            return code == null ? View("Error") : View();
        }

        // POST: /Account/ResetPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResetPasswordExternal(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await UserManager.FindByNameAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction("ResetPasswordConfirmation", "Account");
            }
            var result = await UserManager.ResetPasswordAsync(user.Id, model.Code, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction("ResetPasswordConfirmation", "Account");
            }
            AddErrors(result);
            return View();
        }

        [Authorize]
        public ActionResult ChangeMyPassword()
        {
            return View();
        }

        //
        // POST: /Account/ResetPassword
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await UserManager.FindByNameAsync(model.Email);
            model.Code = await UserManager.GeneratePasswordResetTokenAsync(user.Id);
            var result = await UserManager.ResetPasswordAsync(user.Id, model.Code, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction("ResetPasswordConfirmationInternal", "Account", new { email = model.Email });
            }
            AddErrors(result);
            return View();
        }
        
        // POST: /Account/ResetPassword
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ChangeMyPassword(ResetMyPasswordViewModel model)
        {
            var user = UserManager.Find(User.Identity.Name, model.CurrentPassword);

            if (user != null)
            {
                model.Email = user.Email;
                model.Code = await UserManager.GeneratePasswordResetTokenAsync(user.Id);
                var result = await UserManager.ResetPasswordAsync(user.Id, model.Code, model.Password);
                if (result.Succeeded)
                {
                    return RedirectToAction("ResetPasswordConfirmationInternal", "Account", new { email = model.Email });
                }

                AddErrors(result);
            }
            else
            {
                ModelState.AddModelError("", "Current Password Incorrect");
            }

            return View();
        }

        //
        // GET: /Account/ResetPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        [Authorize]
        public ActionResult ResetPasswordConfirmationInternal(string email)
        {
            ViewBag.Email = email;
            return View();
        }

        [Authorize]
        public ActionResult ChangeMyPasswordConfirmation(string email)
        {
            ViewBag.Email = email;
            return View();
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
        // GET: /Account/SendCode
        [AllowAnonymous]
        public async Task<ActionResult> SendCode(string returnUrl, bool rememberMe)
        {
            var userId = await SignInManager.GetVerifiedUserIdAsync();
            if (userId == null)
            {
                return View("Error");
            }
            var userFactors = await UserManager.GetValidTwoFactorProvidersAsync(userId);
            var factorOptions = userFactors.Select(purpose => new SelectListItem { Text = purpose, Value = purpose }).ToList();
            return View(new SendCodeViewModel { Providers = factorOptions, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        //
        // POST: /Account/SendCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SendCode(SendCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            // Generate the token and send it
            if (!await SignInManager.SendTwoFactorCodeAsync(model.SelectedProvider))
            {
                return View("Error");
            }
            return RedirectToAction("VerifyCode", new { Provider = model.SelectedProvider, ReturnUrl = model.ReturnUrl, RememberMe = model.RememberMe });
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
            var result = await SignInManager.ExternalSignInAsync(loginInfo, isPersistent: false);
            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(returnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.RequiresVerification:
                    return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = false });
                case SignInStatus.Failure:
                default:
                    // If the user does not have an account, then prompt the user to create an account
                    ViewBag.ReturnUrl = returnUrl;
                    ViewBag.LoginProvider = loginInfo.Login.LoginProvider;
                    return View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel { Email = loginInfo.Email });
            }
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
                return RedirectToAction("Index", "Manage");
            }

            if (ModelState.IsValid)
            {
                // Get the information about the user from the external login provider
                var info = await AuthenticationManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    return View("ExternalLoginFailure");
                }
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                var result = await UserManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    result = await UserManager.AddLoginAsync(user.Id, info.Login);
                    if (result.Succeeded)
                    {
                        await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
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
            TenantSetting tenant = GetTenant();
            AuthenticationManager.SignOut();
            //Clear key Session variables
            Session.Remove("Tenant");
            Session.Remove("CurrentChannel");

            if (tenant.Lookup.LookupName != "SaaS System")
            {
                return RedirectToAction("LoginUnbranded", "Account");
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        //
        // GET: /Account/ExternalLoginFailure
        [AllowAnonymous]
        public ActionResult ExternalLoginFailure()
        {
            return View();
        }

        //Users Controller Actions`
        [Authorize(Roles = "SuperAdmin")]
        public ActionResult ListUsers()
        {
            UserViewModel model = new UserViewModel();
            //List<ApplicationUser> allUsers = new List<ApplicationUser>();
            //using (ApplicationDbContext context = new ApplicationDbContext())
            //{
            //    allUsers = context.Users.ToList();

            //    foreach (ApplicationUser usr in allUsers)
            //    {
            //        string email = usr.Email;
            //        string Name = usr.UserName;
            //    }
            //}

            return View(model);
        }

        // GET: /Account/Register
        [Authorize(Roles = "SuperAdmin")]
        public ActionResult CreateUser()
        {
            var model = new RegisterViewModel();
            var tenantVm = new TenantSettingsViewModel();
            model.TenantList = tenantVm.GetTenantList().TenantList;

            return View(model);
        }

        // POST: /Account/Register
        [HttpPost]
        [Authorize(Roles = "SuperAdmin")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateUser(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser() { UserName = model.Email, Email = model.Email };
                IdentityResult result = await UserManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    //await SignInAsync(user, isPersistent: false);

                    // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=320771
                    // Send an email with this link
                    //string code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
                    // var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
                    //await UserManager.SendEmailAsync(user.Id, "Confirm your account", "Please confirm your account by clicking <a href=\"" + callbackUrl + "\">here</a>");

                    var newUser = UserManager.FindByEmail(model.Email);
                    newUser.TenantID = model.TenantID;
                    UserManager.Update(newUser);

                    return RedirectToAction("ListUsers", "Account");
                }
                else
                {
                    AddErrors(result);
                }
            }

            // If we got this far, something failed, redisplay form
            var tenantVm = new TenantSettingsViewModel();
            model.TenantList = tenantVm.GetTenantList().TenantList;
            return View(model);
        }

        [Authorize(Roles = "SuperAdmin")]
        public bool DeleteUser(string id)
        {
            if (ModelState.IsValid)
            {
                if (id == null)
                {
                    return false;
                }

                try
                {
                    var user = UserManager.FindById(id);
                    var logins = user.Logins;

                    foreach (var login in logins.ToList())
                    {
                        UserManager.RemoveLogin(login.UserId, new UserLoginInfo(login.LoginProvider, login.ProviderKey));
                    }

                    var rolesForUser = UserManager.GetRoles(id);

                    if (rolesForUser.Count() > 0)
                    {
                        foreach (var item in rolesForUser.ToList())
                        {
                            // item should be the name of the role
                            var result = UserManager.RemoveFromRole(user.Id, item);
                        }
                    }

                    UserManager.Delete(user);
                }
                catch (Exception)
                {
                    return false;
                }
            }

            return true;
        }

        //Roles Controler Actions
        [Authorize(Roles = "SuperAdmin")]
        public ActionResult ListRoles()
        {
            List<IdentityRole> allRoles = new List<IdentityRole>();
            allRoles = GetRoles();

            return View(allRoles);
        }

        [Authorize(Roles = "SuperAdmin")]
        public ActionResult CreateRole()
        {
            IdentityRole model = new IdentityRole();

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "SuperAdmin")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateRole(IdentityRole model)
        {
            if (ModelState.IsValid)
            {
                using (ApplicationDbContext context = new ApplicationDbContext())
                {
                    var roleStore = new RoleStore<IdentityRole>(context);
                    var roleManager = new RoleManager<IdentityRole>(roleStore);
                    if (!roleManager.RoleExists(model.Name))
                    {
                        var result = await roleManager.CreateAsync(new IdentityRole { Name = model.Name });
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

        [Authorize(Roles = "SuperAdmin")]
        //public async Task<ActionResult> DeleteRole(List<string> optionsArray)
        public async Task<ActionResult> DeleteRole(string id)
        {
            if (ModelState.IsValid)
            {
                using (ApplicationDbContext context = new ApplicationDbContext())
                {
                    var roleStore = new RoleStore<IdentityRole>(context);
                    var roleManager = new RoleManager<IdentityRole>(roleStore);
                    //if (roleManager.RoleExists(Convert.ToString(optionsArray[0])))
                    if (roleManager.RoleExists(id))
                        {
                        //var role = roleManager.FindByName(Convert.ToString(optionsArray[0]));
                        var role = roleManager.FindByName(id);
                        var result = await roleManager.DeleteAsync(role);
                        if (result.Succeeded)
                        {
                            var model = GetRoles();
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

        private static List<IdentityRole> GetRoles()
        {
            List<IdentityRole> allRoles = new List<IdentityRole>();

            using (ApplicationDbContext context = new ApplicationDbContext())
            {
                allRoles = context.Roles.OrderBy(x => x.Name).ToList();
            }

            return allRoles;
        }

        //UserRoles Controller Actions
        [Authorize(Roles = "SuperAdmin")]
        public ActionResult ListUserRoles(string email)
        {
            UserRoleViewModel model = new UserRoleViewModel(email);

            return View("~/Views/Account/ListUserRoles.cshtml", model);
        }

        [HttpPost]
        [Authorize(Roles = "SuperAdmin")]
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

                //model.GetAllRoles();

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

            return RedirectToAction("ListUsers", "Account", new { email = model.Email });
        }

        //
        // GET: /Account/DeleteUserRole
        [Authorize(Roles = "SuperAdmin")]
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
                            var model = GetRoles();
                            return PartialView("~/Views/Account/ListRoleData.cshtml", model);
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

        [Authorize(Roles = "SuperAdmin")]
        public ActionResult ModifyTenant()
        {
            if (TempData["ModifyTenantMessage"] != null)
            {
                ModelState.AddModelError("", TempData["ModifyTenantMessage"].ToString());
            }

            var model = new TenantSettingsViewModel();
            model.GetTenantList();
            model.SelectedTenantID = GetTenant().TenantID;

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "SuperAdmin")]
        public ActionResult UpdateTenant(TenantSettingsViewModel model)
        {
            try
            {
                var cm = new CommonModel();
                cm.ChangeUserTenant(model.SelectedTenantID);
                TempData["ModifyTenantMessage"] = "Successfuly updated the tenant for this user.";
            }
            catch (Exception e)
            {
                TempData["ModifyTenantMessage"] = "Failed to update tenant for this user. Error: " + e.Message;
            }

            return RedirectToAction("ModifyTenant");
        }

        [Authorize(Roles = "SuperAdmin")]
        public ActionResult ClientCounts()
        {
            var model = new TenantSettingsViewModel();
            model.GetClientCounts();

            return View(model);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_userManager != null)
                {
                    _userManager.Dispose();
                    _userManager = null;
                }

                if (_signInManager != null)
                {
                    _signInManager.Dispose();
                    _signInManager = null;
                }
            }

            base.Dispose(disposing);
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

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        internal class ChallengeResult : HttpUnauthorizedResult
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
                var properties = new AuthenticationProperties { RedirectUri = RedirectUri };
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