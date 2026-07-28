using System.Web.Mvc;
using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance;
using System.Collections.Generic;
using System;

namespace netGiant.Intranet.Controllers.PMS.Maintenance
{
    [Authorize]
    public class ProductStatusController : ApplicationController
    {
        public ActionResult ProductStatusIndex()
        {
            ProductStatusViewModel model = new ProductStatusViewModel();
            return View("~/Views/PMS/Maintenance/ProductStatus/ProductStatusIndex.cshtml", model.Get(1, "", "", ""));
        }

        public ActionResult ProductStatusData(List<string> optionsArray)
        {
            ProductStatusViewModel model = new ProductStatusViewModel();
            model.Get(Convert.ToInt32(optionsArray[3]), optionsArray[0].ToString(), optionsArray[1].ToString(), optionsArray[2]);

            return PartialView("~/Views/PMS/Maintenance/ProductStatus/ProductStatusData.cshtml", model.productStatuses);
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Create(int id)
        {
            return View("~/Views/PMS/Maintenance/ProductStatus/CreateProductStatus.cshtml", ProductStatusViewModel.Create(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Save(ProductStatusViewModel model)
        {
            if (ModelState.IsValid)
            {
                model.Save();
                TempData["InformationBoxFlag"] = "Product Status Saved";
                return RedirectToAction("ProductStatusIndex");
            }

            return View("~/Views/PMS/Maintenance/ProductStatus/CreateProductStatus.cshtml", model);
        }

        [HttpPost]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Delete(List<string> optionsArray)
        {
            ProductStatusViewModel model = new ProductStatusViewModel();
            model.Delete(Convert.ToInt32(optionsArray[0]));
            TempData["InformationBoxFlag"] = "Product Status Deleted";

            return PartialView("~/Views/PMS/Maintenance/ProductStatus/ProductStatusData.cshtml", model.Get(Convert.ToInt32(optionsArray[4]),
                optionsArray[1].ToString(), optionsArray[2], optionsArray[3]).productStatuses);
        }
    }
}
