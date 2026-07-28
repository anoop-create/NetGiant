using System;
using System.Web.Mvc;
using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance;
using System.Collections.Generic;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;

namespace netGiant.Intranet.Areas.PMS.Maintenance
{
    [Authorize]
    public class DataSupplierController : Controller
    {
        public ActionResult Index()
        {
            DataSupplierViewModel model = new DataSupplierViewModel();
            return View("DataSupplierIndex", model);
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult DataSupplierAjax([DataSourceRequest] DataSourceRequest request)
        {
            DataSupplierViewModel model = new DataSupplierViewModel();
            model.GetDataSuppliers();

            var result = model.DataSupplierList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Create(int id)
        {
            var model = new DataSupplierViewModel();
            return View("CreateDataSupplier", model.Create(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Save(DataSupplierViewModel model)
        {
            bool success = model.Save(model);

            if (success == true)
            {
                TempData["InformationBoxFlag"] = "Data Supplier Saved";
            }

            return RedirectToAction("Index");
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Delete(List<string> optionsArray)
        {
            DataSupplierViewModel dsVm = new DataSupplierViewModel();
            bool success = dsVm.Delete(Convert.ToInt32(optionsArray[0]));

            return RedirectToAction("Index");
        }

        public ActionResult Overrides()
        {
            var model = new DataSupplierViewModel();
            return View(model);
        }

        public ActionResult Overrides_Read([DataSourceRequest]DataSourceRequest request)
        {
            var model = new DataSupplierViewModel();
            model.GetOverrides();

            var result = model.DataSupplierOverrides.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult CreateOverride(int id)
        {
            var model = new DataSupplierViewModel();
            return View(model.CreateOverride(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin, SEO")]
        public ActionResult SaveOverride(DataSupplierViewModel model, string content)
        {
            try
            {
                if (model.SaveOverrideEntry())
                {
                    TempData["InformationBoxFlag"] = "Override Saved";
                }
                return RedirectToAction("Overrides");
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.Message);
                return View("CreateOverride", model);
            }
        }

        public ActionResult SetDeletedFlag(int id)
        {
            var model = new DataSupplierViewModel();
            var saveReturn = model.SetDeletedFlag(id, true);

            return Json(saveReturn, JsonRequestBehavior.AllowGet);
        }
    }
}
