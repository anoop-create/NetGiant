using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace netGiant.Intranet.Controllers.PMS.Maintenance
{
    [Authorize]
    public class FieldSubSectionController : ApplicationController
    {
        public ActionResult FieldSubSection()
        {
            FieldSubSectionViewModel model = new FieldSubSectionViewModel();
            return View("~/Views/PMS/Maintenance/FieldSubSection/FieldSubSection.cshtml", model.Get());
        }

        public ActionResult FieldSubSectionData(List<string> optionsArray)
        {
            FieldSubSectionViewModel model = new FieldSubSectionViewModel();
            model.SelectedFieldSectionID = Convert.ToInt32(optionsArray[3]);
            model.Get(Convert.ToInt32(optionsArray[4]), optionsArray[0].ToString(), optionsArray[1].ToString(), optionsArray[2]);

            return PartialView("~/Views/PMS/Maintenance/FieldSubSection/FieldSubSectionData.cshtml", model.fieldSubSections);
        }

        public ActionResult SubSectionBySection(string options)
        {
            string[] optionsArray = options.Split(',');
            
            FieldSubSectionViewModel model = new FieldSubSectionViewModel();
            model.SelectedFieldSectionID = Convert.ToInt32(optionsArray[3]);

            return View("~/Views/PMS/Maintenance/FieldSubSection/FieldSubSection.cshtml", 
                model.Get(Convert.ToInt32(optionsArray[4]), optionsArray[0].ToString(), optionsArray[1].ToString(), optionsArray[2]));
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Create(int id)
        {
            return View("~/Views/PMS/Maintenance/FieldSubSection/CreateFieldSubSection.cshtml", FieldSubSectionViewModel.Create(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Save(FieldSubSectionViewModel model)
        {
            if (ModelState.IsValid)
            {
                model.Save();
                TempData["InformationBoxFlag"] = "Field Sub Section Saved";
                return RedirectToAction("FieldSubSection");
            }

            return View("~/Views/PMS/Maintenance/FieldSubSection/CreateFieldSubSection.cshtml", model);
        }

        [HttpPost]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Delete(List<string> optionsArray)
        {
            FieldSubSectionViewModel model = new FieldSubSectionViewModel();
            model.SelectedFieldSectionID = Convert.ToInt32(optionsArray[4]);
            model.Delete(Convert.ToInt32(optionsArray[0]));
            TempData["InformationBoxFlag"] = "Field Sub Section Deleted";

            return PartialView("~/Views/PMS/Maintenance/FieldSubSection/FieldSubSectionData.cshtml", model.Get(Convert.ToInt32(optionsArray[5]),
                optionsArray[1].ToString(), optionsArray[2], optionsArray[3]).fieldSubSections);
        }

        public ActionResult RelatedFieldNames(int id)
        {
            return RedirectToAction("FieldNameBySubSection", "FieldName", new { options = string.Format(" ,name, ,{0},0,1", id.ToString()) });
        }
    }
}