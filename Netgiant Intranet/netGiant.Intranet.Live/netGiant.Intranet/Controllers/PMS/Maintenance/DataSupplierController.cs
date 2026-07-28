using System;
using System.Web.Mvc;
using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance;
using System.Collections.Generic;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;

namespace netGiant.Intranet.Controllers.PMS.Maintenance
{
    [Authorize]
    public class DataSupplierController : ApplicationController
    {
        public ActionResult Index()
        {
            DataSupplierViewModel dsVm = new DataSupplierViewModel();
            return View("~/Views/PMS/Maintenance/DataSupplier/DataSupplierIndex.cshtml", dsVm.Get(1, null, null, null).dataSupplierList);
        }

        [HttpPost]
        public ActionResult IndexData(List<string> optionsArray)
        {
            
            DataSupplierViewModel dsVm = new DataSupplierViewModel();
            return PartialView("~/Views/PMS/Maintenance/DataSupplier/DataSupplierData.cshtml",
                                dsVm.Get(Convert.ToInt32(optionsArray[3]), optionsArray[0], optionsArray[1],
                                optionsArray[2]).dataSupplierList);
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Create(int id)
        {
            DataSupplierViewModel dsVm = new DataSupplierViewModel();
            return View("~/Views/PMS/Maintenance/DataSupplier/CreateDataSupplier.cshtml", dsVm.Create(id));
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

            if (success == true)
            {
                TempData["InformationBoxFlag"] = "Data Supplier Deleted";
            }

            return PartialView("~/Views/PMS/Maintenance/DataSupplier/DataSupplierData.cshtml",
                                dsVm.Get(Convert.ToInt32(optionsArray[4]), optionsArray[2], optionsArray[1],
                                optionsArray[3]).dataSupplierList);
        }

        public ActionResult Overrides()
        {
            var dsVm = new DataSupplierViewModel();


            return View("~/Views/PMS/Maintenance/DataSupplier/Overrides.cshtml");
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
            DataSupplierViewModel dsVm = new DataSupplierViewModel();
            return View("~/Views/PMS/Maintenance/DataSupplier/CreateOverride.cshtml", dsVm.CreateOverride(id));
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
                return View("~/Views/PMS/Maintenance/DataSupplier/CreateOverride.cshtml", model);
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
