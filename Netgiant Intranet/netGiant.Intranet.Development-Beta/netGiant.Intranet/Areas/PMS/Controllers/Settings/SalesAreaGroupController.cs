using System.Web.Mvc;
using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance;
using System.Collections.Generic;
using System;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;

namespace netGiant.Intranet.Areas.PMS.Maintenance
{
    [Authorize]
    public class SalesAreaGroupController : Controller
    {
        public ActionResult SalesAreaGroupIndex()
        {
            return View(new SalesAreaGroupViewModel());
        }

        public ActionResult SalesAreaGroup_Read([DataSourceRequest]DataSourceRequest request)
        {
            var model = new SalesAreaGroupViewModel().Get();

            var result = model.SalesAreaGroupList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Create(int id)
        {
            var model = new SalesAreaGroupViewModel();
            return View("CreateSalesAreaGroup", model.Create(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Save(SalesAreaGroupViewModel model)
        {
            if (ModelState.IsValid)
            {
                bool success = model.Save(model);
                if (success == true)
                {
                    TempData["InformationBoxFlag"] = "Sales Area Group Saved";
                }
            }
            return RedirectToAction("SalesAreaGroupIndex");
        }

        [HttpPost]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Delete(int id)
        {
            return Json(new { saveReturn = new SalesAreaGroupViewModel().Delete(id) });
        }
    }
}
