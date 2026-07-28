using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance;
using System;
using System.Web.Mvc;

namespace netGiant.Intranet.Areas.PMS.SEO
{
    [Authorize(Roles = "IntranetAdmin, PMSReader, PMSAdmin, SEO")]
    public class ObsoleteItemsController : Controller
    {
        public ActionResult ObsoleteItemsIndex()
        {
            return View(new ObsoleteItemsViewModel());
        }

        public ActionResult ObsoleteItem_Read([DataSourceRequest]DataSourceRequest request)
        {
            var model = new ObsoleteItemsViewModel().GetObsoleteItemList();

            var result = model.ObsoleteItemList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        public ActionResult CreateObsoleteItem(int id)
        {
            return View(new ObsoleteItemsViewModel().Create(id));
        }

        public ActionResult SaveObsoleteItem(ObsoleteItemsViewModel model)
        {
            try
            {
                if (model.Save())
                {
                    TempData["InformationBoxFlag"] = "Obsolete Item Saved";
                }
                return RedirectToAction("ObsoleteItemsIndex");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                model.GetSelectListData();
                return View("CreateObsoleteItem", model);
            }
        }

        public ActionResult DeleteObsoleteItem(int id)
        {
            return Json(new { saveReturn = new ObsoleteItemsViewModel().Delete(id) });
        }
    }
}