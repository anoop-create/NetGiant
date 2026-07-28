using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance;
using netGiant.Intranet.DataLayer;
using netGiant.Intranet.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace netGiant.Intranet.Controllers.PMS.Maintenance
{
    [Authorize(Roles = "IntranetAdmin, PMSReader, PMSAdmin")]
    public class ProductItemTypeController : ApplicationController
    {
        public ActionResult ProductItemTypeIndex()
        {
            ProductItemTypeViewModel model = new ProductItemTypeViewModel();
            return View("~/Views/PMS/Maintenance/ProductItemType/ProductItemTypeIndex.cshtml", model.GetProductItemType()); 
        }

        public ActionResult ProductItemTypeList(List<productItemType> model)
        {
            return PartialView("~/Views/PMS/Maintenance/ProductItemType/ProductItemTypeData.cshtml", model);
        }

        public ActionResult ProductItemTypeData(string[] optionsArray)
        {
            ProductItemTypeViewModel model = new ProductItemTypeViewModel();
            model.GetProductItemType(optionsArray[0].ToString(), optionsArray[1].ToString(), optionsArray[2].ToString(), Convert.ToInt32(optionsArray[3]));
            return ProductItemTypeGetJson(model);
        }

        private ActionResult ProductItemTypeGetJson(ProductItemTypeViewModel model)
        {
            JsonModel jsonModel = new JsonModel();
            jsonModel.NoMoreData = model.ProductItemTypeList.Count < 50;
            jsonModel.Count = model.ProductItemTypeListCount;
            jsonModel.HTMLString = RenderPartialViewToString("~/Views/PMS/Maintenance/ProductItemType/ProductItemTypeData.cshtml",
                model.ProductItemTypeList);
            jsonModel.InfoBoxMessage = TempData["InformationBoxFlag"] != null ? TempData["InformationBoxFlag"].ToString() : "";
            return Json(jsonModel);
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Create(int id)
        {
            ProductItemTypeViewModel model = new ProductItemTypeViewModel();
            return View("~/Views/PMS/Maintenance/ProductItemType/CreateProductItemType.cshtml",
                model.Create(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Save(ProductItemTypeViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    model.Save();
                    TempData["InformationBoxFlag"] = "Product Item Type Saved";
                }
                return RedirectToAction("ProductItemTypeIndex");
            }
            catch (Exception e)
            {
                ModelState.AddModelError(String.Empty, e.Message);
                return RedirectToAction("Create", new { id = model.ProductItemType.productItemTypeID });
            }
        }

        [HttpPost]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Delete(List<string> optionsArray)
        {
            ProductItemTypeViewModel model = new ProductItemTypeViewModel();
            model.Delete(Convert.ToInt32(optionsArray[0]));
            model.GetProductItemType(optionsArray[0].ToString(), optionsArray[1].ToString(), optionsArray[2].ToString(), Convert.ToInt32(optionsArray[3]));
            TempData["InformationBoxFlag"] = "Product Item Type Deleted";

            return ProductItemTypeGetJson(model);
        }
    }
}