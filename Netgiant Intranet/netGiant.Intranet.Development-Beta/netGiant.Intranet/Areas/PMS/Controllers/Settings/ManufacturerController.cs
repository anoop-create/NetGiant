using System;
using System.Web.Mvc;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance;

namespace netGiant.Intranet.Areas.PMS.Maintenance
{
    [Authorize]
    public class ManufacturerController : Controller
    {
        public ActionResult Index()
        {
            return View(new ManufacturersViewModel());
        }

        public ActionResult Manufacturer_Read([DataSourceRequest]DataSourceRequest request)
        {
            var model = new ManufacturersViewModel().Get();

            var result = model.ManufacturerList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Create(int id)
        {
            return View("CreateManufacturer", new ManufacturersViewModel().Create(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Save(ManufacturersViewModel model)
        {
            if (ModelState.IsValid)
            {
                bool success = model.Save(model);
                if (success == true)
                {
                    TempData["InformationBoxFlag"] = "Manufacturer Saved";
                }
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Delete(int id)
        {
            return Json(new { saveReturn = new ManufacturersViewModel().Delete(id) });
        }

        public ActionResult ManufacturerNotesIndex()
        {
            return View(new ManufacturerNotesViewModel());
        }

        public ActionResult ManufacturerNotes_Read([DataSourceRequest]DataSourceRequest request)
        {
            var model = new ManufacturerNotesViewModel().Get();

            var result = model.ManufacturerNotesList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin, SEO")]
        public ActionResult CreateManufacturerNotes(int id)
        {
            var model = new ManufacturerNotesViewModel();
            return View(model.Create(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin, SEO")]
        public ActionResult SaveManufacturerNotes(ManufacturerNotesViewModel model, string note, string priorityNote, string secondaryNote)
        {
            try
            {
                model.ManufacturerNotes.note = note;
                model.ManufacturerNotes.priorityNote = priorityNote;
                model.ManufacturerNotes.secondaryNote = secondaryNote;
                if (ModelState.IsValid)
                {
                    model.Save();
                    TempData["InformationBoxFlag"] = "Manufacturer Note Saved";
                }
                return RedirectToAction("ManufacturerNotesIndex");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                model.SetupSelectLists();
                return View("CreateManufacturerNotes", model);
            }
        }

        [HttpPost]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin, SEO")]
        public ActionResult DeleteManufacturerNotes(int id)
        {
            return Json(new { saveReturn = new ManufacturerNotesViewModel().Delete(id) });
        }
    }
}
