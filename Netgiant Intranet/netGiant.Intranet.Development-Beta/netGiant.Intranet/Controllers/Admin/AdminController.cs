using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using netGiant.Intranet.BusinessLayer.ViewModels.Admin;
using System.Collections.Generic;
using System.Web.Mvc;
using System;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using netGiant.Intranet.BusinessLayer;
using System.Configuration;
using System.Net;
using netGiant.Intranet.BusinessLayer.Utilities;
using Antlr.Runtime.Misc;
using RestSharp;
using RestSharp.Authenticators;
using System.Security.Policy;

namespace netGiant.Intranet.Controllers
{
    [Authorize(Roles="IntranetAdmin")]
    public class AdminController : ApplicationController
    {
        public ActionResult Index()
        {
            var model = new AdminViewModel();           
            return View(model.Get());
        }

        [Authorize]
        public ActionResult ListMenuItems()
        {
            var model = new MenuViewModel();
            return View(model);
        }
        public ActionResult MenuEntry_Read([DataSourceRequest]DataSourceRequest request)
        {
            MenuViewModel model = new MenuViewModel();
            model.GetMenuList();

            var result = model.MenuList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        public JsonResult GetParentMenuItems(int id)
        {
            var model = new MenuViewModel();

            model.GetParentMenuItems(id - 1);
            return Json(model.ParentMenuItems, JsonRequestBehavior.AllowGet);
        }

        [Authorize]
        public ActionResult CreateMenuItem()
        {
            var model = new MenuViewModel();
            model.GetMenuDetails(0);
            ViewBag.Title = "New Menu Item";
            ViewBag.SubTitle = "Create a new menu item";

            return View(model);
        }

        [Authorize]
        [HttpPost]
        public ActionResult CreateMenuItem(MenuViewModel model)
        {
            return updateMenu(model, true);
        }

        [Authorize]
        public ActionResult UpdateMenuItem(int id)
        {
            var model = new MenuViewModel();
            model.GetMenuDetails(id);
            ViewBag.Title = "Update Menu Item";
            ViewBag.SubTitle = "Make changes to a menu item";

            return View("CreateMenuItem", model);
        }

        [Authorize]
        [HttpPost]
        public ActionResult UpdateMenuItem(MenuViewModel model)
        {
            return updateMenu(model, true);
        }

        private ActionResult updateMenu(MenuViewModel model, bool update)
        {
            if (ModelState.IsValid)
            {
                bool updated = false;
                updated = model.SaveMenuItem(model);

                if (updated == true)
                {
                    TempData["InformationBoxFlag"] = "Menu Saved";
                }

                return RedirectToAction("ListMenuItems");
            }

            if (update == true)
            {
                model.GetMenuDetails(model.ActionLink.actionLinkID);
                ViewBag.Title = "Update Menu Item";
                ViewBag.SubTitle = "Make changes to a menu item";
            }
            else
            {
                model.GetMenuDetails(0);
                ViewBag.Title = "New Menu Item";
                ViewBag.SubTitle = "Create a new menu item";
            }

            return View("CreateMenuItem", model);
        }

        [HttpPost]
        [Authorize(Roles = "IntranetAdmin")]
        public ActionResult DeleteMenuItem(int id)
        {
            MenuViewModel model = new MenuViewModel();

            SaveReturn sr = model.DeleteMenuItem(id);

            return Json(new { saveReturn = sr });
        }

        public ActionResult Debug()
        {
            ViewBag.ClientIP = OtherUtilities.GetClientIPAddress(Request);
            return View("Debug", new CommonViewModel());
        }

        [AllowAnonymous]
        public JsonResult DeleteCache(string cacheKey = null, int exec = 0)
        {
            var sr = new SaveReturn
            {
                IsSuccess = true
            };

            DataCache.DeleteCache(cacheKey);
            if (ConfigurationManager.AppSettings["Environment"] == "Live" && exec == 0)
            {
                string extras = "";
                if (!string.IsNullOrEmpty(cacheKey))
                {
                    extras += "cacheKey=" + cacheKey + "&exec=1";
                }
                sr = DeleteLiveCache("DeleteCache?" + extras, "delete cache");
            }

            return Json(new { saveReturn = sr }, JsonRequestBehavior.AllowGet);
        }

        private SaveReturn DeleteLiveCache(string parameters, string func)
        {
            var sr = new SaveReturn();
            sr.IsSuccess = true;

            string ip = "10.0.0.5";
            if (Request.Params["LOCAL_ADDR"] == "10.0.0.5")
            {
                ip = "10.0.0.10";
            }

            try
            {
                var client = new RestClient("http://" + ip);
                var request = new RestRequest("/netGiant.Intranet/Admin/" + parameters, RestSharp.Method.Get)
                {
                    Authenticator = new HttpBasicAuthenticator("webadmin", "Innovation2020")
                }
                .AddParameter("grant_type", "client_credentials")
                .AddHeader("Host", "intranet.netgiant.com")
                .AddHeader("X-FORWARDED-PROTO", "https");

                var response = client.Execute(request, RestSharp.Method.Get);
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    sr.Message = "Unable to make portal request for server: " + ip + ": " + func;
                    sr.IsSuccess = false;
                }
            }
            catch (Exception e)
            {
                sr.Message = "Unable to make portal request for server: " + ip + ": " + func;
                sr.IsSuccess = false;
            }

            return sr;







            //string ip1 = "109.108.157.113";
            //string ip2 = "109.108.157.114";

            //ip1 = "172.21.224.140";
            //ip2 = "172.21.224.141";

            //try
            //{
            //    DeleteCacheWebRequest(ip1, parameters);
            //    DeleteCacheWebRequest(ip2, parameters);
            //    sr.IsSuccess = true;
            //}
            //catch (Exception ex)
            //{
            //    sr.IsSuccess = false;
            //    sr.Message = ex.Message;
            //}
            //return sr;
        }

        private void DeleteCacheWebRequest(string ip, string parameters)
        {
            HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create("http://" + ip + "/netGiant.Intranet/Admin/" + parameters);
            myHttpWebRequest.Host = ConfigurationManager.AppSettings["DomainName_Live"];
            myHttpWebRequest.Headers.Add("X-FORWARDED-PROTO", "http");

            HttpWebResponse response = (HttpWebResponse)myHttpWebRequest.GetResponse();
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new WebException(response.StatusDescription);
            }
        }

        public JsonResult ClearSession()
        {
            Session.Clear();
            return Json(new { issuccess = true }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Error()
        {
            // Server.GetLastError() only returns something if ASP.NET's own pipeline captured an
            // unhandled error IN THIS SAME request - but CustomErrorHandling.OnException always redirects
            // here as a brand new request, so GetLastError() is normally null. TempData["LastError"] is
            // set by CustomErrorHandling.OnException right before it redirects here, and survives exactly
            // this one redirect, so check that first for the real exception.
            Exception ex = (TempData["LastError"] as Exception) ?? Server.GetLastError() ?? new Exception();
            var model = new ErrorViewModel(ex);

            return View("~/Views/Shared/Error.cshtml", model);
        }
    }
}