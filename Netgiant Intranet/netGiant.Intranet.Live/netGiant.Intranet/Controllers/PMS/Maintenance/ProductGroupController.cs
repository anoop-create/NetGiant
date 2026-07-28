using System.Web.Mvc;
using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance;
using System.Collections.Generic;
using System;

namespace netGiant.Intranet.Controllers.PMS.Maintenance
{
    [Authorize]
    public class ProductGroupController : ApplicationController
    {
        public ActionResult Index()
        {
            ProductGroupViewModel pgVm = new ProductGroupViewModel();
            return View("~/Views/PMS/Maintenance/ProductGroup/ProductGroupIndex.cshtml", pgVm.Get(1, null, null, null, null));
        }

        public ActionResult IndexData(List<string> optionsArray)
        {
            ProductGroupViewModel pgVm = new ProductGroupViewModel();

            return PartialView("~/Views/PMS/Maintenance/ProductGroup/ProductGroupData.cshtml",
                                pgVm.Get(Convert.ToInt32(optionsArray[4]), optionsArray[0], optionsArray[1],
                                            Convert.ToInt32(optionsArray[2]), optionsArray[3]).productGroupList);
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Create(int id)
        {
            ProductGroupViewModel pgVm = new ProductGroupViewModel();

            return View("~/Views/PMS/Maintenance/ProductGroup/CreateProductGroup.cshtml", pgVm.Create(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Save(ProductGroupViewModel model)
        {
            if (ModelState.IsValid)
            {
                bool success = model.Save(model);
                if (success == true)
                {
                    TempData["InformationBoxFlag"] = "Product Group Saved";
                }
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Delete(List<string> optionsArray)
        {
            ProductGroupViewModel pgVm = new ProductGroupViewModel();
            bool success = pgVm.Delete(Convert.ToInt32(optionsArray[0]));

            if (success == true)
            {
                TempData["InformationBoxFlag"] = "Product Group Deleted";
            }

            return PartialView("~/Views/PMS/Maintenance/ProductGroup/ProductGroupData.cshtml",
                                pgVm.Get(Convert.ToInt32(optionsArray[3]), optionsArray[2], optionsArray[1], null, null).productGroupList);
        }
    }
}
