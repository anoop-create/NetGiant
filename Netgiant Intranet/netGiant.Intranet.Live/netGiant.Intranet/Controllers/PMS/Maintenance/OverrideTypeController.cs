using System.Web.Mvc;
using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance;
using System.Collections.Generic;
using System;

namespace netGiant.Intranet.Controllers.PMS.Maintenance
{
    [Authorize]
    public class OverrideTypeController : ApplicationController
    {
        public ActionResult Index()
        {
            OverrideTypeViewModel otVm = new OverrideTypeViewModel();
            return View("~/Views/PMS/Maintenance/OverrideType/OverrideTypeIndex.cshtml", otVm.Get(1, null, null, null).overrideTypesList);
        }

        public ActionResult IndexData(List<string> optionsArray)
        {
            OverrideTypeViewModel otVm = new OverrideTypeViewModel();

            return PartialView("~/Views/PMS/Maintenance/OverrideType/OverrideTypeData.cshtml",
                                otVm.Get(Convert.ToInt32(optionsArray[3]), optionsArray[0], optionsArray[1],
                                                optionsArray[2]).overrideTypesList);
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Create(int id)
        {
            OverrideTypeViewModel otVm = new OverrideTypeViewModel();
            return View("~/Views/PMS/Maintenance/OverrideType/CreateOverrideType.cshtml", otVm.Create(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Save(OverrideTypeViewModel model)
        {
            if (ModelState.IsValid)
            {
                bool success = model.Save(model);
                if (success == true)
                {
                    TempData["InformationBoxFlag"] = "Override Type Saved";
                }
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Delete(List<string> optionsArray)
        {
            OverrideTypeViewModel otVm = new OverrideTypeViewModel();
            bool success = otVm.Delete(Convert.ToInt32(optionsArray[0]));

            if (success == true)
            {
                TempData["InformationBoxFlag"] = "Override Type Deleted";
            }

            return PartialView("~/Views/PMS/Maintenance/OverrideType/OverrideTypeData.cshtml",
                                otVm.Get(Convert.ToInt32(optionsArray[4]), optionsArray[2], optionsArray[1],
                                            optionsArray[3]).overrideTypesList);
        }
    }
}
