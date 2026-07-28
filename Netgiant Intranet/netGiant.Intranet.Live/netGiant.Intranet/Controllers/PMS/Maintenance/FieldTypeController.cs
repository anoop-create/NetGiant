using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace netGiant.Intranet.Controllers.PMS.Maintenance
{
    [Authorize]
    public class FieldTypeController : ApplicationController
    {
        public ActionResult FieldType()
        {
            FieldTypeViewModel model = new FieldTypeViewModel();
            return View("~/Views/PMS/Maintenance/FieldType/FieldType.cshtml", model.Get());
        }

        public ActionResult FieldTypeData(List<string> optionsArray)
        {
            FieldTypeViewModel model = new FieldTypeViewModel();
            model.Get(Convert.ToInt32(optionsArray[3]), optionsArray[0].ToString(), optionsArray[1].ToString(), optionsArray[2]);

            return PartialView("~/Views/PMS/Maintenance/FieldType/FieldTypeData.cshtml", model.fieldTypes);
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Create(int id)
        {
            return View("~/Views/PMS/Maintenance/FieldType/CreateFieldType.cshtml", FieldTypeViewModel.Create(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Save(FieldTypeViewModel model)
        {
            if (ModelState.IsValid)
            {
                model.Save();
                TempData["InformationBoxFlag"] = "Field Type Saved";
                return RedirectToAction("FieldType");
            }

            return View("~/Views/PMS/Maintenance/FieldType/CreateFieldType.cshtml", model);
        }

        [HttpPost]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Delete(List<string> optionsArray)
        {
            FieldTypeViewModel model = new FieldTypeViewModel();
            model.Delete(Convert.ToInt32(optionsArray[0]));
            TempData["InformationBoxFlag"] = "Field Type Deleted";

            return PartialView("~/Views/PMS/Maintenance/FieldType/FieldTypeData.cshtml", model.Get(Convert.ToInt32(optionsArray[4]),
                optionsArray[1].ToString(), optionsArray[2], optionsArray[3]).fieldTypes);
        }
    }
}