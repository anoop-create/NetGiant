using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance;
using netGiant.Intranet.DataLayer;
using netGiant.Intranet.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace netGiant.Intranet.Controllers.PMS.Maintenance
{
    [Authorize(Roles = "IntranetAdmin, PMSReader, PMSAdmin, SEO")]
    public class EquipmentNotesController : ApplicationController
    {
        public ActionResult EquipmentNotesIndex()
        {
            EquipmentNotesViewModel model = new EquipmentNotesViewModel();
            return View("~/Views/PMS/Maintenance/EquipmentNotes/EquipmentNotesIndex.cshtml", model.GetEquipmentNotes()); 
        }

        public ActionResult EquipmentNotesList(List<equipmentNotes> model)
        {
            return PartialView("~/Views/PMS/Maintenance/EquipmentNotes/EquipmentNotesData.cshtml", model);
        }

        public ActionResult EquipmentNotesData(string[] optionsArray)
        {
            EquipmentNotesViewModel model = new EquipmentNotesViewModel();
            model.GetEquipmentNotes(optionsArray[0].ToString(), optionsArray[1].ToString(), optionsArray[2].ToString(), 
                Convert.ToInt32(optionsArray[3]), Convert.ToInt32(optionsArray[4]), Convert.ToInt32(optionsArray[5]));
            return EquipmentNotesGetJson(model);
        }

        private ActionResult EquipmentNotesGetJson(EquipmentNotesViewModel model)
        {
            JsonModel jsonModel = new JsonModel();
            jsonModel.NoMoreData = model.EquipmentNotesList.Count < 50;
            jsonModel.Count = model.EquipmentNotesListCount;
            jsonModel.HTMLString = RenderPartialViewToString("~/Views/PMS/Maintenance/EquipmentNotes/EquipmentNotesData.cshtml",
                model.EquipmentNotesList);
            jsonModel.InfoBoxMessage = TempData["InformationBoxFlag"] != null ? TempData["InformationBoxFlag"].ToString() : "";
            return Json(jsonModel);
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin, SEO")]
        public ActionResult Create(int id)
        {
            EquipmentNotesViewModel model = new EquipmentNotesViewModel();
            return View("~/Views/PMS/Maintenance/EquipmentNotes/CreateEquipmentNotes.cshtml",
                model.Create(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin, SEO")]
        public ActionResult Save(EquipmentNotesViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    model.Save();
                    TempData["InformationBoxFlag"] = "Equipment Note Saved";
                }
                return RedirectToAction("EquipmentNotesIndex");
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.Message);
                model.SetupSelectLists();
                return View("~/Views/PMS/Maintenance/EquipmentNotes/CreateEquipmentNotes.cshtml", model);
            }
        }

        [HttpPost]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin, SEO")]
        public ActionResult Delete(List<string> optionsArray)
        {
            EquipmentNotesViewModel model = new EquipmentNotesViewModel();
            model.Delete(Convert.ToInt32(optionsArray[0]));
            model.GetEquipmentNotes(optionsArray[0].ToString(), optionsArray[1].ToString(), optionsArray[2].ToString(),
                Convert.ToInt32(optionsArray[3]), Convert.ToInt32(optionsArray[4]), Convert.ToInt32(optionsArray[5]));
            TempData["InformationBoxFlag"] = "Equipment Note Deleted";

            return EquipmentNotesGetJson(model);
        }
    }
}