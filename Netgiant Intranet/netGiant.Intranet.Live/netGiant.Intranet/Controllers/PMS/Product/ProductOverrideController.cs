using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using netGiant.Intranet.DataLayer;
using PagedList;
using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Product;
using System.Collections.Generic;
using System;

namespace netGiant.Intranet.Controllers.PMS.Product
{
    [Authorize]
    public class ProductOverrideController : ApplicationController
    {
        public ActionResult ProductOverride()
        {
            ProductOverrideViewModel model = new ProductOverrideViewModel();
            return View("~/Views/PMS/Product/ProductOverride/ProductOverride.cshtml", model.Get());
        }

        public ActionResult ProductOverrideData(List<string> optionsArray)
        {
            ProductOverrideViewModel model = new ProductOverrideViewModel();
            model.Get(Convert.ToInt32(optionsArray[3]), optionsArray[0].ToString(), optionsArray[1].ToString(), optionsArray[2].ToString());

            return PartialView("~/Views/PMS/Product/ProductOverride/ProductOverrideData.cshtml", model.ProductOverrides);
        }

        public ActionResult Create(int id)
        {
            return View("~/Views/PMS/Product/ProductOverride/CreateProductOverride.cshtml", ProductOverrideViewModel.Create(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Save(ProductOverrideViewModel model)
        {
           if (ModelState.IsValid)
           {
               model.Save();
               TempData["InformationBoxFlag"] = "Product Override Saved";
               return RedirectToAction("ProductOverride");
           }

           return View("~/Views/PMS/Product/ProductOverride/CreateProductOverride.cshtml", model);
        }

        [HttpPost]
        public ActionResult Delete(List<string> optionsArray)
        {
            ProductOverrideViewModel model = new ProductOverrideViewModel();
            model.Delete(Convert.ToInt32(optionsArray[0]));
            TempData["InformationBoxFlag"] = "Product Override Deleted";

            return PartialView("~/Views/PMS/Product/ProductOverride/ProductOverrideData.cshtml", model.Get(Convert.ToInt32(optionsArray[4]),
                optionsArray[1].ToString(), optionsArray[2], optionsArray[3].ToString()).ProductOverrides);
        }
    }
}
