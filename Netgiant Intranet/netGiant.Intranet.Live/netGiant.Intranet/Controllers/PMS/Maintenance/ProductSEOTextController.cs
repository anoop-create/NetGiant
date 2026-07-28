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
    [Authorize(Roles = "IntranetAdmin, PMSReader, PMSAdmin, SEO")]
    public class ProductSEOTextController : ApplicationController
    {
        public ActionResult ProductSEOTextIndex()
        {
            ProductSEOTextViewModel model = new ProductSEOTextViewModel();
            return View("~/Views/PMS/Maintenance/ProductSEOText/ProductSEOTextIndex.cshtml", model.GetProductSEOText());
        }

        public ActionResult ProductSEOTextList(List<productSEOText> model)
        {
            return PartialView("~/Views/PMS/Maintenance/ProductSEOText/ProductSEOTextData.cshtml", model);
        }

        public ActionResult ProductSEOTextData(string[] optionsArray)
        {
            ProductSEOTextViewModel model = new ProductSEOTextViewModel();
            int? itemType = null;
            int? ownBrand = null;
            //int? maintenance = null;

            if (optionsArray[5] != "")
            {
                ownBrand = Convert.ToInt32(optionsArray[5]);
            }

            if (optionsArray[6] != "")
            {
                itemType = Convert.ToInt32(optionsArray[6]);
            }

            //if (optionsArray[7] != "")
            //{
            //    maintenance = Convert.ToInt32(optionsArray[7]);
            //}

            model.GetProductSEOText(optionsArray[0].ToString(), optionsArray[1].ToString(), optionsArray[2].ToString(), Convert.ToInt32(optionsArray[3]),
                Convert.ToInt32(optionsArray[4]), ownBrand, itemType, Convert.ToBoolean(optionsArray[7]), Convert.ToInt32(optionsArray[8]));
            return ProductSEOTextGetJson(model);
        }

        private ActionResult ProductSEOTextGetJson(ProductSEOTextViewModel model)
        {
            JsonModel jsonModel = new JsonModel();
            jsonModel.NoMoreData = model.ProductSEOTextList.Count < 50;
            jsonModel.Count = model.ProductSEOTextListCount;
            jsonModel.HTMLString = RenderPartialViewToString("~/Views/PMS/Maintenance/ProductSEOText/ProductSEOTextData.cshtml",
                model.ProductSEOTextList);
            jsonModel.InfoBoxMessage = TempData["InformationBoxFlag"] != null ? TempData["InformationBoxFlag"].ToString() : "";
            return Json(jsonModel);
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin, SEO")]
        public ActionResult Create(int id)
        {
            ProductSEOTextViewModel model = new ProductSEOTextViewModel();
            return View("~/Views/PMS/Maintenance/ProductSEOText/CreateProductSEOText.cshtml",
                model.Create(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin, SEO")]
        public ActionResult Save(ProductSEOTextViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    model.Save();
                    TempData["InformationBoxFlag"] = "Product SEO Text Saved";
                }
                return RedirectToAction("ProductSEOTextIndex");
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.Message);
                model.SetupSelectLists();
                return View("~/Views/PMS/Maintenance/ProductSEOText/CreateProductSEOText.cshtml", model);
            }
        }

        [HttpPost]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin, SEO")]
        public ActionResult Delete(List<string> optionsArray)
        {
            ProductSEOTextViewModel model = new ProductSEOTextViewModel();
            int? itemType = null;
            int? ownBrand = null;
            //int? maintenace = null;

            if (optionsArray[5] != "")
            {
                ownBrand = Convert.ToInt32(optionsArray[5]);
            }

            if (optionsArray[6] != "")
            {
                itemType = Convert.ToInt32(optionsArray[6]);
            }

            //if (optionsArray[7] != "")
            //{
            //    maintenace = Convert.ToInt32(optionsArray[7]);
            //}

            model.Delete(Convert.ToInt32(optionsArray[0]));
            model.GetProductSEOText(optionsArray[0].ToString(), optionsArray[1].ToString(), optionsArray[2].ToString(), Convert.ToInt32(optionsArray[3]),
                Convert.ToInt32(optionsArray[4]), ownBrand, itemType, Convert.ToBoolean(optionsArray[7]), Convert.ToInt32(optionsArray[8]));
            TempData["InformationBoxFlag"] = "Product SEO Text Deleted";

            return ProductSEOTextGetJson(model);
        }
    }
}