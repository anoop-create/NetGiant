using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace netGiant.Intranet.Controllers.PMS.Maintenance
{
    [Authorize]
    public class FieldSectionController : ApplicationController
    {
        public ActionResult FieldSection()
        {
            FieldSectionViewModel model = new FieldSectionViewModel();
            return View("~/Views/PMS/Maintenance/FieldSection/FieldSection.cshtml", model.Get());
        }

        public ActionResult FieldSectionData(List<string> optionsArray)
        {
            FieldSectionViewModel model = new FieldSectionViewModel();
            model.Get(Convert.ToInt32(optionsArray[3]), optionsArray[0].ToString(), optionsArray[1].ToString(), optionsArray[2]);

            return PartialView("~/Views/PMS/Maintenance/FieldSection/FieldSectionData.cshtml", model.fieldSections);
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Create(int id)
        {
            return View("~/Views/PMS/Maintenance/FieldSection/CreateFieldSection.cshtml", FieldSectionViewModel.Create(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Save(FieldSectionViewModel model)
        {
            if (ModelState.IsValid)
            {
                model.Save();
                TempData["InformationBoxFlag"] = "Field Section Saved";
                return RedirectToAction("FieldSection");
            }

            return View("~/Views/PMS/Maintenance/FieldSection/CreateFieldSection.cshtml", model);
        }

        [HttpPost]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Delete(List<string> optionsArray)
        {
            FieldSectionViewModel model = new FieldSectionViewModel();
            model.Delete(Convert.ToInt32(optionsArray[0]));
            TempData["InformationBoxFlag"] = "Field Section Deleted";

            return PartialView("~/Views/PMS/Maintenance/FieldSection/FieldSectionData.cshtml", model.Get(Convert.ToInt32(optionsArray[4]),
                optionsArray[1].ToString(), optionsArray[2], optionsArray[3]).fieldSections);
        }

        public ActionResult RelatedSubSections(int id)
        {
            return RedirectToAction("SubSectionBySection", "FieldSubSection", new { options = " ,name, ," + id.ToString() + ",1" });
        }
    }
}