using BusinessLogic;
using BusinessLogic.ViewModels;
using DataAccess.Utilities;
using System;
using System.Configuration;
using System.Globalization;
using System.Web.Mvc;

namespace CommonUI.Controllers
{
    [SiteOfflineCheck]
    //[AuthenticateForBeta]
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
                model.GetPopularPrinters(0, 0, 24);
                // Site audit (May 2026) item 16: homepage had no "Best Sellers" row with
                // Add to Basket - Product/BestSellers.cshtml already exists (built for the
                // WebsiteId==3 site below) and needs no changes, it just wasn't being
                // populated for this site. GetBestSellers() reads via the same cached
                // ngmd.GetBestSellers stored-proc call already used elsewhere - no new
                // backend work, just calling the existing method here too.
                model.GetBestSellers();
                if (Convert.ToBoolean(model.HomeData["ShowBlog"]) && ConfigurationManager.AppSettings["Environment"] != "Local")
                {
                    model.GetBlogFeed("https://blog." + Utilities.GetItemFromDict(model.CommonData, "SiteName").ToLower() + "/feed/");
                }
            }
            else
            {
                model.GetBestSellers();
            }
            ViewBag.CartridgeType = "";
            ViewBag.ManufacturerId = 0;
            ViewBag.ManufacturerName = "none";
            ViewBag.BreadcrumbJson = model.BuildBreadcrumbJson(true);

            return View(model);
        }

        public ActionResult Health()
        {
            // Get todays date from SQL
            try
            {
                string sql = @"SELECT GETDATE()";

                SQL.ExecuteInlineProcedure("netgiantmasterdata", sql);
                return new HttpStatusCodeResult(200);
            }
            catch
            {
                return new HttpStatusCodeResult(500);
            }
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
            model.GetMeta();
            model.GetReviews();
            ViewBag.Title = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(siteName.ToLower());

            ViewBag.BreadcrumbJson = model.BuildBreadcrumbJson();

            return View(model);
        }
    }
}