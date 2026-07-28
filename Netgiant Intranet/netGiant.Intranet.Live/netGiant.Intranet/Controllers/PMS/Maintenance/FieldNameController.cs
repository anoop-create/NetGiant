using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace netGiant.Intranet.Controllers.PMS.Maintenance
{
    [Authorize]
    public class FieldNameController : ApplicationController
    {
        public ActionResult FieldName()
        {
            FieldNameViewModel model = new FieldNameViewModel();
            return View("~/Views/PMS/Maintenance/FieldName/FieldName.cshtml", model.Get());
        }

        public ActionResult FieldNameData(List<string> optionsArray)
        {
            FieldNameViewModel model = new FieldNameViewModel();
            model.selectedFieldSubSectionID = Convert.ToInt16(optionsArray[3]);
            model.selectedFieldTypeID = Convert.ToInt16(optionsArray[4]);
            model.Get(Convert.ToInt32(optionsArray[5]), optionsArray[0].ToString(), optionsArray[1].ToString(), optionsArray[2]);

            return PartialView("~/Views/PMS/Maintenance/FieldName/FieldNameData.cshtml", model.fieldNames);
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Create(int id)
        {
            return View("~/Views/PMS/Maintenance/FieldName/CreateFieldName.cshtml", FieldNameViewModel.Create(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Save(FieldNameViewModel model)
        {
            if (ModelState.IsValid)
            {
                model.Save();
                TempData["InformationBoxFlag"] = "Field Name Saved";
                return RedirectToAction("FieldName");
            }

            return View("~/Views/PMS/Maintenance/FieldName/CreateFieldName.cshtml", model);
        }

        [HttpPost]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Delete(List<string> optionsArray)
        {
            FieldNameViewModel model = new FieldNameViewModel();
            model.selectedFieldSubSectionID = Convert.ToInt16(optionsArray[4]);
            model.selectedFieldTypeID = Convert.ToInt16(optionsArray[5]);
            model.Delete(Convert.ToInt32(optionsArray[0]));
            TempData["InformationBoxFlag"] = "Field Name Deleted";

            return PartialView("~/Views/PMS/Maintenance/FieldName/FieldNameData.cshtml", model.Get(Convert.ToInt32(optionsArray[6]),
                optionsArray[1].ToString(), optionsArray[2], optionsArray[3]).fieldNames);
        }

        public ActionResult FieldNameBySubSection(string options)
        {
            string[] optionsArray = options.Split(',');
            
            FieldNameViewModel model = new FieldNameViewModel();
            model.selectedFieldSubSectionID = Convert.ToInt16(optionsArray[3]);
            model.selectedFieldTypeID = Convert.ToInt16(optionsArray[4]);

            return View("~/Views/PMS/Maintenance/FieldName/FieldName.cshtml", 
                model.Get(Convert.ToInt32(optionsArray[5]), optionsArray[0].ToString(), optionsArray[1].ToString(), optionsArray[2]));
        }
    }
}