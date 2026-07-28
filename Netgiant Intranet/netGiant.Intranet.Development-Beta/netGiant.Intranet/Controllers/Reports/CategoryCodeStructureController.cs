using netGiant.Intranet.BusinessLayer.Models;
using netGiant.Intranet.BusinessLayer.ViewModels.Reports;
using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace netGiant.Intranet.Controllers.Reports
{
    [Authorize(Roles = "IntranetAdmin, Reports")]
    public class CategoryCodeStructureController : ApplicationController
    {
        public ActionResult CategoryCodeIndex()
        {
            var model = new CategoryCodeStructureViewModel();
            model.BuildCategoryCodeStructure();
            return View("CategoryCodeStructure", model);
        }

        //public ActionResult CategoryCodeList(List<categoryCode> model)
        //{
        //    return PartialView("_CategoryCodeStructureData", model);
        //}

        public ActionResult CategoryCodeData(string[] optionsArray)
        {
            var model = new CategoryCodeStructureViewModel();
            model.BuildCategoryCodeStructure(Convert.ToInt32(optionsArray[0]));
            return CategoryCodeGetJson(model);
        }

        private ActionResult CategoryCodeGetJson(CategoryCodeStructureViewModel model)
        {
            var jsonModel = new JsonModel();
            jsonModel.HTMLString = RenderPartialViewToString("_CategoryCodeStructureData", model.CatList);
            jsonModel.InfoBoxMessage = TempData["InformationBoxFlag"] != null ? TempData["InformationBoxFlag"].ToString() : "";
            return Json(jsonModel);
        }

        public ActionResult ProductList(int id)
        {
            var model = new CategoryCodeStructureViewModel();
            return PartialView("_ProductList", model.GetCategoryProducts(id).ProductList);
        }
    }
}