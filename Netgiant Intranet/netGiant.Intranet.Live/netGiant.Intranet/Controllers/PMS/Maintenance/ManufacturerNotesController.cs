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
    public class ManufacturerNotesController : ApplicationController
    {
        public ActionResult ManufacturerNotesIndex()
        {
            ManufacturerNotesViewModel model = new ManufacturerNotesViewModel();
            return View("~/Views/PMS/Maintenance/ManufacturerNotes/ManufacturerNotesIndex.cshtml", model.GetManufacturerNotes());
        }

        public ActionResult ManufacturerNotesList(List<manufacturerNotes> model)
        {
            return PartialView("~/Views/PMS/Maintenance/ManufacturerNotes/ManufacturerNotesData.cshtml", model);
        }

        public ActionResult ManufacturerNotesData(string[] optionsArray)
        {
            ManufacturerNotesViewModel model = new ManufacturerNotesViewModel();
            model.GetManufacturerNotes(optionsArray[0].ToString(), optionsArray[1].ToString(), optionsArray[2].ToString(), Convert.ToInt32(optionsArray[3]), 
                Convert.ToInt32(optionsArray[4]), Convert.ToInt32(optionsArray[5]), Convert.ToInt32(optionsArray[6]));
            return ManufacturerNotesGetJson(model);
        }

        private ActionResult ManufacturerNotesGetJson(ManufacturerNotesViewModel model)
        {
            JsonModel jsonModel = new JsonModel();
            jsonModel.NoMoreData = model.ManufacturerNotesList.Count < 50;
            jsonModel.Count = model.ManufacturerNotesListCount;
            jsonModel.HTMLString = RenderPartialViewToString("~/Views/PMS/Maintenance/ManufacturerNotes/ManufacturerNotesData.cshtml",
                model.ManufacturerNotesList);
            jsonModel.InfoBoxMessage = TempData["InformationBoxFlag"] != null ? TempData["InformationBoxFlag"].ToString() : "";
            return Json(jsonModel);
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin, SEO")]
        public ActionResult Create(int id)
        {
            ManufacturerNotesViewModel model = new ManufacturerNotesViewModel();
            return View("~/Views/PMS/Maintenance/ManufacturerNotes/CreateManufacturerNotes.cshtml",
                model.Create(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin, SEO")]
        public ActionResult Save(ManufacturerNotesViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    model.Save();
                    TempData["InformationBoxFlag"] = "Manufacturer Note Saved";
                }
                return RedirectToAction("ManufacturerNotesIndex");
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.Message);
                model.SetupSelectLists();
                return View("~/Views/PMS/Maintenance/ManufacturerNotes/CreateManufacturerNotes.cshtml", model);
            }
        }

        [HttpPost]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin, SEO")]
        public ActionResult Delete(List<string> optionsArray)
        {
            ManufacturerNotesViewModel model = new ManufacturerNotesViewModel();
            model.Delete(Convert.ToInt32(optionsArray[0]));
            model.GetManufacturerNotes(optionsArray[0].ToString(), optionsArray[1].ToString(), optionsArray[2].ToString(), Convert.ToInt32(optionsArray[3]),
                Convert.ToInt32(optionsArray[4]), Convert.ToInt32(optionsArray[5]), Convert.ToInt32(optionsArray[6]));
            TempData["InformationBoxFlag"] = "Manufacturer Note Deleted";

            return ManufacturerNotesGetJson(model);
        }
    }
}