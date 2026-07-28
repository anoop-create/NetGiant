using System.Web.Mvc;
using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance;
using System.Collections.Generic;
using System;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;

namespace netGiant.Intranet.Areas.PMS.Maintenance
{
    [Authorize]
    public class WebsitesController : Controller
    {
        public ActionResult WebsiteIndex()
        {
            return View(new WebsiteViewModel());
        }

        public ActionResult Website_Read([DataSourceRequest]DataSourceRequest request)
        {
            var model = new WebsiteViewModel().Get();

            var result = model.WebsiteList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Create(int id)
        {
            var model = new WebsiteViewModel();
            return View("CreateWebsite", model.Create(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Save(WebsiteViewModel model)
        {
            if (ModelState.IsValid)
            {
                bool success = model.Save(model);
                if (success == true)
                {
                    TempData["InformationBoxFlag"] = "Sales Area Group Saved";
                }
            }
            return RedirectToAction("WebsiteIndex");
        }

        [HttpPost]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Delete(int id)
        {
            return Json(new { saveReturn = new WebsiteViewModel().Delete(id) });
        }
    }
}
