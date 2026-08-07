using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using System;
using System.Web.Mvc;

namespace netGiant.Intranet.Controllers.Ecommerce
{
    // Manages dbo.WebsiteInfoCard - the basket-page sidebar widgets (sale banner,
    // "Free Next Day Delivery", "Trusted By 25,000+", "Exclusive Trade Pricing", etc).
    // Read by the Ecommerce site directly from the shared ngmd database
    // (BusinessLogic.EntityAccess.ReadWebsiteInfoCards), same pattern as CMS Entries.
    [Authorize(Roles = "IntranetAdmin, PMSReader, PMSAdmin, SEO")]
    public class WebsiteInfoCardController : Controller
    {
        public ActionResult Index()
        {
            return View(new WebsiteInfoCardViewModel());
        }

        public ActionResult InfoCard_Read([DataSourceRequest] DataSourceRequest request)
        {
            WebsiteInfoCardViewModel model = new WebsiteInfoCardViewModel();
            model.GetInfoCardList();

            var result = model.InfoCardList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin, SEO")]
        public ActionResult CreateEntry(int id)
        {
            return View(new WebsiteInfoCardViewModel().CreateEntry(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin, SEO")]
        public ActionResult SaveEntry(WebsiteInfoCardViewModel model)
        {
            try
            {
                if (model.SaveEntry(User.Identity.Name))
                {
                    TempData["InformationBoxFlag"] = "Info Card Saved";
                }
                return RedirectToAction("Index");
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.Message);
                model.SetupSelectLists();
                return View("CreateEntry", model);
            }
        }

        [HttpPost]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin, SEO")]
        public ActionResult DeleteEntry(int id)
        {
            WebsiteInfoCardViewModel model = new WebsiteInfoCardViewModel();

            SaveReturn sr = model.DeleteEntry(id);

            return Json(new { saveReturn = sr });
        }
    }
}
