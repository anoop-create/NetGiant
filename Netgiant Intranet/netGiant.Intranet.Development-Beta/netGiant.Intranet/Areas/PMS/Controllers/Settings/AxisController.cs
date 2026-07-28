using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance;
using System;
using System.Web.Mvc;

namespace netGiant.Intranet.Areas.PMS.Controllers.Settings
{
    [Authorize(Roles = "IntranetAdmin, PMSReader, PMSAdmin")]
    public class AxisController : Controller
    {
        public ActionResult AxisValueLookupIndex()
        {
            return View(new AxisValueLookupViewModel());
        }

        public ActionResult AxisEbusinessIndex()
        {
            return View(new AxisEBusinessViewModel());
        }

        public ActionResult AxisValueLookup_Read([DataSourceRequest]DataSourceRequest request)
        {
            var model = new AxisValueLookupViewModel().Get();

            var result = model.AxisValueLookupList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        public ActionResult AxisEBusiness_Read([DataSourceRequest]DataSourceRequest request)
        {
            var model = new AxisEBusinessViewModel().Get();

            var result = model.EbusinessGroupList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult CreateAxisValueLookup(int id)
        {
            return View(new AxisValueLookupViewModel().Create(id));
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult CreateAxisEBusiness(int id)
        {
            return View(new AxisEBusinessViewModel().Create(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult SaveAxisValueLookup(AxisValueLookupViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    model.Save();
                    TempData["InformationBoxFlag"] = "AxisValueLookup Record Saved";
                }
                return RedirectToAction("AxisValueLookupIndex");
            }
            catch (Exception e)
            {
                ModelState.AddModelError(string.Empty, e.Message);
                return RedirectToAction("CreateAxisValueLookup", new { id = model.AxisValueLookup.axisValueLookupID });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult SaveAxisEBusiness(AxisEBusinessViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    model.Save();
                    TempData["InformationBoxFlag"] = "Configuration Setting Saved";
                }
                return RedirectToAction("AxisEbusinessIndex");
            }
            catch (Exception e)
            {
                ModelState.AddModelError(string.Empty, e.Message);
                return RedirectToAction("CreateAxisEBusinessGroup", new { id = model.AxisEBusinessGroup.AxisEbusinessID });
            }
        }

        [HttpPost]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult DeleteAxisValueLookup(int id)
        {
            return Json(new { saveReturn = new AxisValueLookupViewModel().Delete(id) });
        }

        [HttpPost]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult DeleteAxisEBusiness(int id)
        {
            return Json(new { saveReturn = new AxisEBusinessViewModel().Delete(id) });
        }
    }
}