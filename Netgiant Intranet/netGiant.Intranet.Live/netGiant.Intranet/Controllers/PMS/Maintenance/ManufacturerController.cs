using System;
using System.Collections.Generic;
using System.Web.Mvc;
using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance;

namespace netGiant.Intranet.Controllers.PMS.Maintenance
{
    [Authorize]
    public class ManufacturerController : ApplicationController
    {
        public ActionResult Index()
        {
            ManufacturersViewModel manVm = new ManufacturersViewModel();
            return View("~/Views/PMS/Maintenance/Manufacturer/ManufacturerIndex.cshtml", manVm.Get(1, null, null, null).manufacturersList);
        }

        public ActionResult IndexData(List<string> optionsArray)
        {
            ManufacturersViewModel manVm = new ManufacturersViewModel();
            return PartialView("~/Views/PMS/Maintenance/Manufacturer/ManufacturerData.cshtml",
                                manVm.Get(Convert.ToInt32(optionsArray[3]), optionsArray[0], optionsArray[1],
                                            optionsArray[2]).manufacturersList);
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Create(int id)
        {
            ManufacturersViewModel manVm = new ManufacturersViewModel();
            return View("~/Views/PMS/Maintenance/Manufacturer/CreateManufacturer.cshtml", manVm.Create(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Save(ManufacturersViewModel model)
        {
            if (ModelState.IsValid)
            {
                bool success = model.Save(model);
                if (success == true)
                {
                    TempData["InformationBoxFlag"] = "Manufacturer Saved";
                }
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Delete(List<string> optionsArray)
        {
            ManufacturersViewModel manVm = new ManufacturersViewModel();
            bool success = manVm.Delete(Convert.ToInt32(optionsArray[0]));

            if (success == true)
            {
                TempData["InformationBoxFlag"] = "Manufacturer Deleted";
            }

            return PartialView("~/Views/PMS/Maintenance/Manufacturer/ManufacturerData.cshtml",
                                manVm.Get(Convert.ToInt32(optionsArray[4]), optionsArray[2], optionsArray[1],
                                            optionsArray[3]).manufacturersList);
        }
    }
}
