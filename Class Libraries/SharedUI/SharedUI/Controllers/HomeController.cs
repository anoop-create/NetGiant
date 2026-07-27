using System.Configuration;
using BusinessLogic;
using BusinessLogic.ViewModels;
using System.Globalization;
using System.Web.Mvc;
using System;

namespace SharedUI.Controllers
{
    [SiteOfflineCheck]
    public class HomeController : WizardController
    {
        private HomeViewModel model;
        public ActionResult Index()
        {
            model = new HomeViewModel();

            model.GetWizardLists();
            model.BlogFeed = null;
            if (int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString()) != 3)
            {
                model.GetPopularPrinters();
                model.GetBlogFeed("https://" + Utilities.GetItemFromDict(model.CommonData, "LiveDomainName") + "blog/feed/");
            }
            else
            {
                model.GetBestSellers();
            }
            ViewBag.CartridgeType = "";
            ViewBag.ManufacturerId = 0;
            ViewBag.ManufacturerName = "none";

            if (Convert.ToBoolean(Session["U_IsPortalUser"]) && Request.Cookies["__csuser"] != null)
            {

                Session["U_CSUser"] = Convert.ToString(Request.Cookies["__csuser"].Value);
            }

            return View(model);
        }

        public ActionResult Index2()
        {
            ViewBag.Message = "Test page for MailChimp.";

            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        public ActionResult CustomerReviews(string siteName)
        {
            model = new HomeViewModel();
            model.GetReviews();
            ViewBag.Title = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(siteName.ToLower());

            ViewBag.BreadcrumbJson = model.BuildBreadcrumbJson();

            return View(model);
        }
    }
}