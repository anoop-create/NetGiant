using DP001DataAccess.Entities;
using System;
using System.Globalization;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity.Owin;
using System.IO;
using System.Reflection;
using System.Threading;
using DP001Website.Models;
using DP001BusinessLogic.Shared;

namespace DP001Website.Controllers
{
    [Authorize]
    public class ApplicationController : Controller
    {
        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set
            {
                _signInManager = value;
            }
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

        protected ApplicationSignInManager _signInManager;
        protected ApplicationUserManager _userManager;

        [ChildActionOnly]
        public TenantSetting GetTenant()
        {
            var cm = new CommonModel();
            return cm.GetTenant();
        }

        [ChildActionOnly]
        public Channel GetChannel()
        {
            var cm = new CommonModel();
            return cm.GetChannel();
        }

        [ChildActionOnly]
        public int GetChannelId()
        {
            var cm = new CommonModel();
            return cm.GetChannelId();
        }

        [ChildActionOnly]
        public string RenderPartialViewToString(string viewName, object model)
        {
            ViewData.Model = model;
            using (var sw = new StringWriter())
            {
                var viewResult = ViewEngines.Engines.FindPartialView(ControllerContext, viewName);
                var viewContext = new ViewContext(ControllerContext, viewResult.View, ViewData, TempData, sw);
                viewResult.View.Render(viewContext, sw);
                viewResult.ViewEngine.ReleaseView(ControllerContext, viewResult.View);
                return sw.GetStringBuilder().ToString();
            }
        }

        public ActionResult Error()
        {
            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                NoLockInterceptor.ApplyNoLock = false;
            }
            base.Dispose(disposing);
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class MultipleButtonAttribute : ActionNameSelectorAttribute
    {
        public string Name { get; set; }
        public string Argument { get; set; }

        public override bool IsValidName(ControllerContext controllerContext, string actionName, MethodInfo methodInfo)
        {
            var isValidName = false;
            var keyValue = string.Format("{0}:{1}", Name, Argument);
            var value = controllerContext.Controller.ValueProvider.GetValue(keyValue);

            if (value != null)
            {
                controllerContext.Controller.ControllerContext.RouteData.Values[Name] = Argument;
                isValidName = true;
            }

            return isValidName;
        }
    }

    public class DeleteFileAttribute : ActionFilterAttribute
    {
        public override void OnResultExecuted(ResultExecutedContext filterContext)
        {
            filterContext.HttpContext.Response.Flush();
            Type fcType = filterContext.Result.GetType();

            if (fcType.Name == "FilePathResult")
            {
                string filePath = (filterContext.Result as FilePathResult).FileName;
                CommonFunctions.DeleteFile(filePath);
            }
        }
    }

    public class SetCulture : ActionFilterAttribute, IActionFilter
    {
        void IActionFilter.OnActionExecuting(ActionExecutingContext filterContext)
        {
            var cm = new CommonModel();
            Thread.CurrentThread.CurrentCulture = new CultureInfo(cm.GetChannel().CultureName ?? "en-GB");
            OnActionExecuting(filterContext);
        }
    }
}