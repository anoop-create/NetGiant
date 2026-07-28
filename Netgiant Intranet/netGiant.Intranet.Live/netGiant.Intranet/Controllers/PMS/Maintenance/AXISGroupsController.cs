using System;
using System.Linq;
using System.Web.Mvc;
using netGiant.Intranet.DataLayer;
using System.Collections.Generic;
using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance;

namespace netGiant.Intranet.Controllers.PMS.Maintenance
{
    [Authorize]
    public class AXISGroupsController : ApplicationController
    {
        public ActionResult Index()
        {
            AXISGroupsViewModel axgrpVm = new AXISGroupsViewModel();
            return View("~/Views/PMS/Maintenance/AXISGroups/AXISGroupsIndex.cshtml", axgrpVm.Get(1, null, null, null, null, null));
        }

        public ActionResult IndexData(List<string> optionsArray)
        {
            AXISGroupsViewModel axgrpVm = new AXISGroupsViewModel();

            return PartialView("~/Views/PMS/Maintenance/AXISGroups/AXISGroupsData.cshtml",
                                axgrpVm.Get(Convert.ToInt32(optionsArray[5]), optionsArray[0], optionsArray[1],
                                Convert.ToInt32(optionsArray[2]), Convert.ToInt32(optionsArray[3]), optionsArray[4]).axisGroups);
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Create(int id)
        {
            AXISGroupsViewModel axgpVm = new AXISGroupsViewModel();

            return View("~/Views/PMS/Maintenance/AXISGroups/CreateAXISGroup.cshtml", axgpVm.Create(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Save(AXISGroupsViewModel model)
        {
            if (ModelState.IsValid)
            {
                bool success = model.Save(model);
                if (success == true)
                {
                    TempData["InformationBoxFlag"] = "AXIS Group Saved";
                }
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Delete(List<string> optionsArray)
        {
            AXISGroupsViewModel axgrpVm = new AXISGroupsViewModel();
            bool deleteGroup = axgrpVm.Delete(Convert.ToInt32(optionsArray[0]));

            if (deleteGroup == true)
            {
                TempData["InformationBoxFlag"] = "Category Code Deleted";
            }

            return PartialView("~/Views/PMS/Maintenance/AXISGroups/AXISGroupsData.cshtml",
                                axgrpVm.Get(Convert.ToInt32(optionsArray[6]), optionsArray[2],
                                            optionsArray[1], Convert.ToInt32(optionsArray[3]),
                                            Convert.ToInt32(optionsArray[4]), optionsArray[5]).axisGroups);
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public JsonResult GetCategoryCodesByWebsite(int id)
        {
            if (id == 0)
            {
                throw new ArgumentNullException("id");
            }

            List<categoryCode> list = new List<categoryCode>();

            using (ngmdEntities dbContext = new ngmdEntities())
            {
                dbContext.Configuration.ProxyCreationEnabled = false;

                list = dbContext.categoryCode.Where(x => x.websiteFK == id).ToList();
            }

            return Json(list, JsonRequestBehavior.AllowGet);
        }
    }
}
