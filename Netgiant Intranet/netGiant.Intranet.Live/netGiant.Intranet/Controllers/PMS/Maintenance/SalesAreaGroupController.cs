using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using netGiant.Intranet.DataLayer;
using PagedList;
using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance;
using System.Collections.Generic;
using System;

namespace netGiant.Intranet.Controllers.PMS.Maintenance
{
    [Authorize]
    public class SalesAreaGroupController : ApplicationController
    {
        public ActionResult SalesAreaGroupIndex()
        {
            SalesAreaGroupViewModel model = new SalesAreaGroupViewModel();
            return View("~/Views/PMS/Maintenance/SalesAreaGroup/SalesAreaGroupIndex.cshtml", model.Get());
        }

        public ActionResult SalesAreaGroupData(List<string> optionsArray)
        {
            SalesAreaGroupViewModel model = new SalesAreaGroupViewModel();
            model.Get(Convert.ToInt32(optionsArray[3]), optionsArray[0].ToString(), optionsArray[1].ToString(), optionsArray[2].ToString());

            return PartialView("~/Views/PMS/Maintenance/SalesAreaGroup/SalesAreaGroupData.cshtml", model.salesAreaGroups);
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Create(int id)
        {
            return View("~/Views/PMS/Maintenance/SalesAreaGroup/CreateSalesAreaGroup.cshtml", SalesAreaGroupViewModel.Create(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Save(SalesAreaGroupViewModel model)
        {
            if (ModelState.IsValid)
            {
                model.Save();
                TempData["InformationBoxFlag"] = "Sales Area Group Saved";

                return RedirectToAction("SalesAreaGroupIndex");
            }

            return View("~/Views/PMS/Maintenance/SalesAreaGroup/CreateSalesAreaGroup.cshtml", model);
        }

        [HttpPost]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Delete(List<string> optionsArray)
        {
            SalesAreaGroupViewModel model = new SalesAreaGroupViewModel();
            model.Delete(Convert.ToInt32(optionsArray[0]));
            TempData["InformationBoxFlag"] = "Sales Area Group Deleted";

            return PartialView("~/Views/PMS/Maintenance/SalesAreaGroup/SalesAreaGroupData.cshtml", model.Get(Convert.ToInt32(optionsArray[4]),
                optionsArray[1].ToString(), optionsArray[2], optionsArray[3].ToString()).salesAreaGroups);
        }
    }
}
