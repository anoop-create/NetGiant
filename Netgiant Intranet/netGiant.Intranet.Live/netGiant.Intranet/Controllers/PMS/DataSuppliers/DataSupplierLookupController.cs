using netGiant.Intranet.BusinessLayer.ViewModels.PMS.DataSuppliers;
using netGiant.Intranet.DataLayer;
using netGiant.Intranet.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace netGiant.Intranet.Controllers.PMS.DataSuppliers
{
    [Authorize(Roles = "IntranetAdmin, PMSReader, PMSAdmin")]
    public class DataSupplierLookupController : ApplicationController
    {
        public ActionResult DataSupplierLookupIndex()
        {
            DataSupplierLookupViewModel model = new DataSupplierLookupViewModel();
            return View("~/Views/PMS/DataSuppliers/DataSupplierLookupIndex.cshtml", model.GetDataSupplierLookup());
        }

        public ActionResult DataSupplierLookupList(List<ds_productView> model)
        {
            return PartialView("~/Views/PMS/DataSuppliers/DataSupplierLookupData.cshtml", model);
        }

        public ActionResult DataSupplierLookupData(string[] optionsArray)
        {
            DataSupplierLookupViewModel model = new DataSupplierLookupViewModel();
            model.GetDataSupplierLookup(optionsArray[0].ToString(), optionsArray[1].ToString(), optionsArray[2].ToString(), Convert.ToInt32(optionsArray[3]));
            return DataSupplierLookupGetJson(model);
        }

        private ActionResult DataSupplierLookupGetJson(DataSupplierLookupViewModel model)
        {
            JsonModel jsonModel = new JsonModel();
            jsonModel.NoMoreData = model.DataSupplierLookupList.Count < 50;
            jsonModel.Count = model.DataSupplierLookupListCount;
            jsonModel.HTMLString = RenderPartialViewToString("~/Views/PMS/DataSuppliers/DataSupplierLookupData.cshtml",
                model.DataSupplierLookupList);
            jsonModel.InfoBoxMessage = TempData["InformationBoxFlag"] != null ? TempData["InformationBoxFlag"].ToString() : "";
            return Json(jsonModel);
        }

        public ActionResult DataSupplierLookupDetails(int id)
        {
            DataSupplierLookupViewModel model = new DataSupplierLookupViewModel();
            return View("~/Views/PMS/DataSuppliers/DataSupplierLookupDetails.cshtml", model.Details(id));
        }
    }
}