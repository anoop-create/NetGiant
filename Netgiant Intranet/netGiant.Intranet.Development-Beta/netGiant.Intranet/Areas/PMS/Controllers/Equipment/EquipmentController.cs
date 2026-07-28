using System;
using System.Linq;
using System.Collections.Generic;
using System.Web.Mvc;
using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Equipment;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;
using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance;
using System.Web.Services.Description;

namespace netGiant.Intranet.Areas.PMS.Equipment
{
    [Authorize]
    public class EquipmentController : Controller
    {
        public ActionResult EquipmentIndex()
        {
            return View(new EquipmentViewModel());
        }

        public ActionResult FamiliesIndex()
        {
            return View(new EquipmentViewModel());
        }

        public ActionResult EquipmentNotesIndex()
        {
            return View(new EquipmentNotesViewModel());
        }

        public ActionResult Equipment_Read([DataSourceRequest]DataSourceRequest request)
        {
            var model = new EquipmentViewModel();
            model.GetEquipment();

            var result = model.EquipmentList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        public ActionResult Families_Read([DataSourceRequest]DataSourceRequest request)
        {
            var model = new EquipmentViewModel();
            model.GetFamilies();

            var result = model.FamilyList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        public ActionResult EquipmentNotes_Read([DataSourceRequest]DataSourceRequest request)
        {
            var model = new EquipmentNotesViewModel().Get();

            var result = model.EquipmentNotesList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult CreateEquipment(int id)
        {
            var model = new EquipmentViewModel();

            return View(model.GetEquipment(id));
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult CreateFamily(int id)
        {
            var model = new EquipmentViewModel();

            return View(model.GetFamily(id));
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult CreateFamilyMembership(int id)
        {
            var model = new EquipmentViewModel();
            model.FamilyID = id;
            ViewBag.FamilyName = model.GetFamilyName(id);

            return View(model.GetFamilyMembership(id));
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin, SEO")]
        public ActionResult CreateEquipmentNotes(int id)
        {
            var model = new EquipmentNotesViewModel();
            return View(model.Create(id));
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public JsonResult ChangeManufacturer(string manufacturerId = "0")
        {
            var model = new EquipmentNotesViewModel();
            List<SelectListItem> eq = new List<SelectListItem>();
            eq = model.GetEquipmentNames(int.Parse(manufacturerId)).ToList();
            return Json(new
            {
                equiplist = eq
            });
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult SaveEquipment(EquipmentViewModel model)
        {
            try
            {
                if (model.SaveEquipment())
                {
                    TempData["InformationBoxFlag"] = "Equipment Saved";
                }
                return RedirectToAction("EquipmentIndex");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View("CreateEquipment", model.GetEquipment(model.Equipment.eqEquipmentID));
            }
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult SaveFamily(EquipmentViewModel model)
        {
            try
            {
                if (model.SaveFamily())
                {
                    TempData["InformationBoxFlag"] = "Equipment Family Saved";
                }
                return RedirectToAction("FamiliesIndex");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View("CreateFamily", model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin, SEO")]
        public ActionResult SaveEquipmentNotes(EquipmentNotesViewModel model, string content)
        {
            try
            {
                model.EquipmentNotes.note = content;
                if (ModelState.IsValid)
                {
                    model.Save();
                    TempData["InformationBoxFlag"] = "Equipment Note Saved";
                }
                return RedirectToAction("EquipmentNotesIndex");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                model.SetupSelectLists(model.EquipmentNotes.eqEquipmentFK);
                return View("CreateEquipmentNotes", model);
            }
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult DeleteEquipment(int id)
        {
            return Json(new { saveReturn = new EquipmentViewModel().DeleteEquipment(id) });
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult DeleteFamily(int id)
        {
            return Json(new { saveReturn = new EquipmentViewModel().DeleteFamily(id) });
        }

        [HttpPost]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin, SEO")]
        public ActionResult DeleteEquipmentNotes(int id)
        {
            return Json(new { saveReturn = new EquipmentNotesViewModel().Delete(id) });
        }

        public ActionResult EquipmentMemIndex(int id)
        {
            var model = new EquipmentViewModel();
            ViewBag.eqEquipmentID = id;

            return View(model.GetEquipmentProductMemberships(id));
        }

        public ActionResult FamilyMemIndex(int id)
        {
            var model = new EquipmentViewModel();
            ViewBag.FamilyName = model.GetFamilyName(id);
            ViewBag.FamilyID = id;

            return View(model.GetEquipmentFamilyMemberships(id, false));
        }

        [HttpPost]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult DeleteMembership(List<string> optionsArray)
        {
            var model = new EquipmentViewModel();
            ViewBag.eqEquipmentID = optionsArray[1];

            if (model.DeleteMembership(Convert.ToInt32(optionsArray[0])))
            {
                TempData["InformationBoxFlag"] = "Product Membership Deleted";
            }

            return PartialView("_EquipmentMemData", model.GetEquipmentProductMemberships(Convert.ToInt32(optionsArray[1])).EquipmentProductMembershipList);
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult CreateEquipMembership(int id)
        {
            var model = new EquipmentViewModel();
            ViewBag.EquipName = model.GetEquipmentName(id);
            ViewBag.eqEquipmentID = id;

            return View(model.GetEquipmentProductMemberships(id));
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public JsonResult GetProductsForMembership(string text)
        {
            var products = SelectListViewModel.SearchProductsPartNoDesc(text);
            return Json(products, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public JsonResult SaveMembership(int equipmentId, int[] products)
        {
            var model = new EquipmentViewModel();
            bool success = model.SaveMembership(equipmentId, products);

            return Json(new
            {
                success = success
            });
        }

        public JsonResult GetProducts(string searchTerm)
        {
            return Json(SelectListViewModel.GetProductsArray(searchTerm, 0, true).ToList(), JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult SaveFamilyMembership(EquipmentViewModel model)
        {
            int familyID = model.FamilyID;
            bool success = model.SaveFamilyMembership(model);

            if (success == true)
            {
                TempData["InformationBoxFlag"] = "Family Membership Saved";
            }

            return RedirectToAction("FamilyMemIndex", new { id = familyID });
        }

        [HttpPost]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult DeleteFamilyMembership(List<string> optionsArray)
        {
            EquipmentViewModel model = new EquipmentViewModel();
            bool success = model.DeleteFamilyMembership(Convert.ToInt32(optionsArray[0]));

            if (success == true)
            {
                TempData["InformationBoxFlag"] = "Family Membership Deleted";
            }

            ViewBag.FamilyID = Convert.ToInt32(optionsArray[0]);
            ViewBag.ShowEquipment = false;

            return PartialView("_FamilyMemData", model.GetEquipmentFamilyMemberships(Convert.ToInt32(optionsArray[1]), true).EquipmentFamilyMembershipList);
        }

        public ActionResult GetNewFamilyRow(List<string> optionsArray)
        {
            return PartialView("_CreateFamilyMapping",
                EquipmentViewModel.CreateNewFamilyMapping(Convert.ToInt32(optionsArray[0]),
                Convert.ToInt32(optionsArray[1])));
        }
    }
}

