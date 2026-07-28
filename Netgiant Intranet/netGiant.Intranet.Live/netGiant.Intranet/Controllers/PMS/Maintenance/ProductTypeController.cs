using System.Web.Mvc;
using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance;
using System.Collections.Generic;
using System;

namespace netGiant.Intranet.Controllers.PMS.Maintenance
{
    [Authorize]
    public class ProductTypeController : ApplicationController
    {
        public ActionResult ProductTypeIndex()
        {
            ProductTypeViewModel model = new ProductTypeViewModel();
            return View("~/Views/PMS/Maintenance/ProductType/ProductTypeIndex.cshtml", model.Get());
        }

        public ActionResult ProductTypeData(List<string> optionsArray)
        {
            ProductTypeViewModel model = new ProductTypeViewModel();
            model.Get(Convert.ToInt32(optionsArray[3]), optionsArray[0].ToString(), optionsArray[1].ToString(), optionsArray[2]);

            return PartialView("~/Views/PMS/Maintenance/ProductType/ProductTypeData.cshtml", model.productTypes);
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Create(int id)
        {
            return View("~/Views/PMS/Maintenance/ProductType/CreateProductType.cshtml", ProductTypeViewModel.Create(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Save(ProductTypeViewModel model)
        {
            if (ModelState.IsValid)
            {
                model.Save();
                TempData["InformationBoxFlag"] = "Product Type Saved";
                return RedirectToAction("ProductTypeIndex");
            }

            return View("~/Views/PMS/Maintenance/ProductType/CreateProductType.cshtml", model);
        }

        [HttpPost]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Delete(List<string> optionsArray)
        {
            ProductTypeViewModel model = new ProductTypeViewModel();
            model.Delete(Convert.ToInt32(optionsArray[0]));
            TempData["InformationBoxFlag"] = "Product Type Deleted";

            return PartialView("~/Views/PMS/Maintenance/ProductType/ProductTypeData.cshtml", model.Get(Convert.ToInt32(optionsArray[4]),
                optionsArray[1].ToString(), optionsArray[2], optionsArray[3]).productTypes);
        }
    }
}
