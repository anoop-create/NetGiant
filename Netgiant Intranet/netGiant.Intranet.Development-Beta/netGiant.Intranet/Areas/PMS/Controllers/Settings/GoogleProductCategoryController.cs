using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance;
using netGiant.Intranet.Controllers;
using System;
using System.Web.Mvc;

namespace netGiant.Intranet.Areas.PMS.Maintenance
{
    [Authorize(Roles = "IntranetAdmin, PMSReader, PMSAdmin")]
    public class GoogleProductCategoryController : ApplicationController
    {
        public ActionResult GoogleProductCategoryIndex()
        {
            return View(new GoogleProductCategoryViewModel());
        }

        public ActionResult GoogleProductCategory_Read([DataSourceRequest]DataSourceRequest request)
        {
            var model = new GoogleProductCategoryViewModel().Get();

            var result = model.GoogleProductCategoryList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Create(int id)
        {
            return View("CreateGoogleProductCategory", new GoogleProductCategoryViewModel().Create(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Save(GoogleProductCategoryViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    model.Save();
                    TempData["InformationBoxFlag"] = "Google Product Category Saved";
                }
                return RedirectToAction("GoogleProductCategoryIndex");
            }
            catch (Exception e)
            {
                ModelState.AddModelError(string.Empty, e.Message);
                return RedirectToAction("Create", new { id = model.GoogleProductCategory.googleProductCategoryID});
            }
        }

        [HttpPost]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Delete(int id)
        {
            return Json(new { saveReturn = new GoogleProductCategoryViewModel().Delete(id) });
        }
    }
}