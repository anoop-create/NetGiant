using BusinessLogic;
using BusinessLogic.ViewModels;
using System;
using System.Web;
using System.Web.Mvc;

namespace SharedUI.Controllers
{
    [AuthorizeIPAddress]
    public class DebugController : ApplicationController
    {
        private CommonViewModel model;
        // GET: Debug
        public ActionResult Index()
        {
            model = new CommonViewModel();
            return View(model);
        }

        public ActionResult AddCookie()
        {
            model = new CommonViewModel();
            return View(model);
        }

        public ActionResult CreateCookie(string name, string value)
        {
            HttpCookie aCookie = new HttpCookie(name);
            aCookie.Value = value;
            aCookie.Expires = DateTime.Now.AddDays(1);
            Response.Cookies.Add(aCookie);

            model = new CommonViewModel();
            return RedirectToAction("Index", model);
        }

        public JsonResult DeleteCache(string cacheKey = null)
        {
            DataCache.DeleteCache(cacheKey);
            model = new CommonViewModel();

            return Json(new { issuccess = true }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult LoadAppVariables()
        {
            Utilities.LoadApplicationVariables();
            model = new CommonViewModel();

            return Json(new { issuccess = true }, JsonRequestBehavior.AllowGet);
        }
    }
}