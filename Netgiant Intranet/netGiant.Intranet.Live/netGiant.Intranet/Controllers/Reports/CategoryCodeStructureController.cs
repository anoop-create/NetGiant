using netGiant.Intranet.BusinessLayer.ViewModels.Reports;
using netGiant.Intranet.DataLayer;
using netGiant.Intranet.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace netGiant.Intranet.Controllers.Reports
{
    [Authorize(Roles = "IntranetAdmin, Reports")]
    public class CategoryCodeStructureController : ApplicationController
    {
        public ActionResult CategoryCodeIndex()
        {
            CategoryCodeStructureViewModel model = new CategoryCodeStructureViewModel();
            model.BuildCategoryCodeStructure();
            return View("~/Views/Reports/CategoryCodeStructure.cshtml", model);
        }

        public ActionResult CategoryCodeList(List<categoryCode> model)
        {
            return PartialView("~/Views/Reports/CategoryCodeStructureData.cshtml", model);
        }

        public ActionResult CategoryCodeData(string[] optionsArray)
        {
            CategoryCodeStructureViewModel model = new CategoryCodeStructureViewModel();
            model.BuildCategoryCodeStructure(Convert.ToInt32(optionsArray[0]));
            return CategoryCodeGetJson(model);
        }

        private ActionResult CategoryCodeGetJson(CategoryCodeStructureViewModel model)
        {
            JsonModel jsonModel = new JsonModel();
            jsonModel.HTMLString = RenderPartialViewToString("~/Views/Reports/CategoryCodeStructureData.cshtml",
                model.CatList);
            jsonModel.InfoBoxMessage = TempData["InformationBoxFlag"] != null ? TempData["InformationBoxFlag"].ToString() : "";
            return Json(jsonModel);
        }

        public ActionResult ProductList(int id)
        {
            CategoryCodeStructureViewModel model = new CategoryCodeStructureViewModel();
            return PartialView("~/Views/Reports/ProductList.cshtml",
                model.GetCategoryProducts(id).ProductList);
        }
    }
}