using System;
using System.Linq;
using System.Collections.Generic;
using System.Web.Mvc;
using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Equipment;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using netGiant.Intranet.DataLayer;

namespace netGiant.Intranet.Controllers.PMS.Equipment
{
    [Authorize]
    public class EquipmentController : ApplicationController
    {
        public ActionResult FireError()
        {
            var trrr = Convert.ToInt32("String");
            return RedirectToAction("FamiliesIndex");
        }

        public ActionResult FamiliesIndex()
        {
            EquipmentViewModel eqVm = new EquipmentViewModel();
            EquipmentViewModel.ReturnType en = EquipmentViewModel.ReturnType.Families;

            return View("~/Views/PMS/Equipment/FamiliesIndex.cshtml", eqVm.Get(1, en, 0, null, null, null, null));
        }

        public ActionResult EquipmentIndex()
        {
            EquipmentViewModel eqVm = new EquipmentViewModel();
            EquipmentViewModel.ReturnType en = EquipmentViewModel.ReturnType.Equipment;

            return View("~/Views/PMS/Equipment/EquipmentIndex.cshtml", eqVm.Get(1, en, 0, null, null, null, null));
        }

        public ActionResult EquipmentMemIndex(int id)
        {
            EquipmentViewModel eqVm = new EquipmentViewModel();
            EquipmentViewModel.ReturnType en = EquipmentViewModel.ReturnType.ProductMemberships;

            ViewBag.EquipName = eqVm.GetEquipmentName(id);
            ViewBag.eqEquipmentID = id;

            return View("~/Views/PMS/Equipment/EquipmentMemIndex.cshtml", eqVm.Get(1, en, id, null, null, null, null).equipmentMemList);
        }

        public ActionResult EquipmentMemIndexData(List<string> optionsArray)
        {
            EquipmentViewModel eqVm = new EquipmentViewModel();
            EquipmentViewModel.ReturnType en = EquipmentViewModel.ReturnType.ProductMemberships;
            ViewBag.eqEquipmentID = Convert.ToInt32(optionsArray[0]);

            return PartialView("~/Views/PMS/Equipment/EquipmentMemData.cshtml", eqVm.Get(Convert.ToInt32(optionsArray[2]), en,
                                Convert.ToInt32(optionsArray[0]), null, null, null, optionsArray[1]).equipmentMemList);
        }

        public ActionResult FamiliesIndexData(List<string> optionsArray)
        {
            EquipmentViewModel eqVm = new EquipmentViewModel();
            EquipmentViewModel.ReturnType en = EquipmentViewModel.ReturnType.Families;

            return PartialView("~/Views/PMS/Equipment/FamilyData.cshtml", eqVm.Get(Convert.ToInt32(optionsArray[3]), en, 0,
                                optionsArray[0], Convert.ToInt32(optionsArray[1]), null, optionsArray[2]).familiesList);
        }

        public ActionResult EquipmentIndexData(List<string> optionsArray)
        {
            EquipmentViewModel eqVm = new EquipmentViewModel();
            EquipmentViewModel.ReturnType en = EquipmentViewModel.ReturnType.Equipment;

            return PartialView("~/Views/PMS/Equipment/EquipmentData.cshtml", eqVm.Get(Convert.ToInt32(optionsArray[4]), en, 0,
                                optionsArray[0], Convert.ToInt32(optionsArray[1]), Convert.ToInt32(optionsArray[2]), optionsArray[3]).equipmentList);
        }

        public ActionResult FamilyMemIndex(int id)
        {
            EquipmentViewModel eqVm = new EquipmentViewModel();
            EquipmentViewModel.ReturnType en = EquipmentViewModel.ReturnType.FamilyMemberships;
            ViewBag.FamilyName = eqVm.GetFamilyName(id);
            ViewBag.FamilyID = id;

            return View("~/Views/PMS/Equipment/FamilyMemIndex.cshtml", eqVm.Get(1, en, id, null, null, null, null).familyMembershipList);
        }

        public ActionResult FamilyMemIndexData(List<string> optionsArray)
        {
            EquipmentViewModel eqVm = new EquipmentViewModel();
            EquipmentViewModel.ReturnType en = EquipmentViewModel.ReturnType.FamilyMemberships;
            ViewBag.FamilyID = Convert.ToInt32(optionsArray[0]);

            return PartialView("~/Views/PMS/Equipment/FamilyMemData.cshtml", eqVm.Get(Convert.ToInt32(optionsArray[2]), en,
                                Convert.ToInt32(optionsArray[0]), null, null, null, optionsArray[1]).familyMembershipList);
        }

        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult CreateFamily(int id)
        {
            EquipmentViewModel eqVm = new EquipmentViewModel();
            EquipmentViewModel.ReturnType en = EquipmentViewModel.ReturnType.SingleFamily;
            eqVm = eqVm.Get(1, en, id, null, null, null, null);

            return View("~/Views/PMS/Equipment/CreateFamily.cshtml", eqVm);
        }

        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult CreateEquipment(int id)
        {
            EquipmentViewModel eqVm = new EquipmentViewModel();
            EquipmentViewModel.ReturnType en = EquipmentViewModel.ReturnType.SingleEquipment;
            eqVm = eqVm.Get(1, en, id, null, null, null, null);

            return View("~/Views/PMS/Equipment/CreateEquipment.cshtml", eqVm);
        }

        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult CreateEquipMembership(int id)
        {
            EquipmentViewModel eqVm = new EquipmentViewModel(); 
            EquipmentViewModel.ReturnType en = EquipmentViewModel.ReturnType.SingleProductMembership;
            eqVm = eqVm.Get(0, en, id, null, null, null, null);

            ViewBag.EquipName = eqVm.GetEquipmentName(id);
            ViewBag.eqEquipmentID = id;

            return View("~/Views/PMS/Equipment/CreateEquipMembership.cshtml", eqVm);
        }

        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult CreateFamilyMembership(int id)
        {
            EquipmentViewModel eqVm = new EquipmentViewModel();
            EquipmentViewModel.ReturnType en = EquipmentViewModel.ReturnType.SingleFamilyMembership;
            eqVm = eqVm.Get(0, en, id, null, null, null, null);

            ViewBag.FamilyName = eqVm.GetFamilyName(id);
            eqVm.familyID = id;

            return View("~/Views/PMS/Equipment/CreateFamilyMembership.cshtml", eqVm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult SaveFamily(EquipmentViewModel model)
        {
            if (ModelState.IsValid)
            {
                bool updated = false;
                updated = model.SaveFamily(model);

                if (updated == true)
                {
                    TempData["InformationBoxFlag"] = "Family Saved";
                }

                return RedirectToAction("FamiliesIndex");
            }

            EquipmentViewModel.ReturnType en = EquipmentViewModel.ReturnType.SingleFamily;
            model = model.Get(1, en, model.family.eqFamilyID, null, null, null, null);

            return View("~/Views/PMS/Equipment/CreateFamily.cshtml", model);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult SaveEquipment(EquipmentViewModel model)
        {
            if (ModelState.IsValid)
            {
                bool updated = false;
                updated = model.SaveEquipment(model);

                if (updated == true)
                {
                    TempData["InformationBoxFlag"] = "Equipment Saved";
                }

                return RedirectToAction("EquipmentIndex");
            }

            EquipmentViewModel.ReturnType en = EquipmentViewModel.ReturnType.SingleEquipment;
            model = model.Get(1, en, model.equipment.eqEquipmentID, null, null, null, null);

            return View("~/Views/PMS/Equipment/CreateEquipment.cshtml", model);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult SaveMembership(EquipmentViewModel model)
        {
            int equipID = model.equipment.eqEquipmentID;
            bool success = model.SaveMembership(model);

            if (success == true)
            {
                TempData["InformationBoxFlag"] = "Product Membership Saved";
            }

            return RedirectToAction("EquipmentMemIndex", new { id = equipID });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult SaveFamilyMembership(EquipmentViewModel model)
        {
            int familyID = model.familyID;
            bool success = model.SaveFamilyMembership(model);

            if (success == true)
            {
                TempData["InformationBoxFlag"] = "Family Membership Saved";
            }

            return RedirectToAction("FamilyMemIndex", new { id = familyID });
        }

        [HttpPost]
        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult DeleteFamily(List<string> optionsArray)
        {
            EquipmentViewModel eqVm = new EquipmentViewModel();
            bool success = eqVm.DeleteFamily(Convert.ToInt32(optionsArray[0]));

            if (success == true)
            {
                TempData["InformationBoxFlag"] = "Family Deleted";
            }

            EquipmentViewModel.ReturnType en = EquipmentViewModel.ReturnType.Families;

            return PartialView("~/Views/PMS/Equipment/FamilyData.cshtml", eqVm.Get(Convert.ToInt32(optionsArray[4]), en, 0,
                                optionsArray[1], Convert.ToInt32(optionsArray[2]), null, optionsArray[3]).familiesList);
        }

        [HttpPost]
        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult DeleteEquipment(List<string> optionsArray)
        {
            EquipmentViewModel eqVm = new EquipmentViewModel();
            bool success = eqVm.DeleteEquipment(Convert.ToInt32(optionsArray[0]));

            if (success == true)
            {
                TempData["InformationBoxFlag"] = "Equipment Deleted";
            }

            EquipmentViewModel.ReturnType en = EquipmentViewModel.ReturnType.Equipment;

            return PartialView("~/Views/PMS/Equipment/EquipmentData.cshtml", eqVm.Get(Convert.ToInt32(optionsArray[4]), en, 0,
                                optionsArray[1], Convert.ToInt32(optionsArray[2]), null, optionsArray[3]).equipmentList);
        }

        [HttpPost]
        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult DeleteMembership(List<string> optionsArray)
        {
            EquipmentViewModel eqVm = new EquipmentViewModel();
            bool success = eqVm.DeleteMembership(Convert.ToInt32(optionsArray[0]));

            if (success == true)
            {
                TempData["InformationBoxFlag"] = "Product Membership Deleted";
            }

            EquipmentViewModel.ReturnType en = EquipmentViewModel.ReturnType.ProductMemberships;

            return PartialView("~/Views/PMS/Equipment/EquipmentMemData.cshtml", eqVm.Get(1, en,
                                Convert.ToInt32(optionsArray[1]), null, null, null, null).equipmentMemList);
        }

        [HttpPost]
        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult DeleteFamilyMembership(List<string> optionsArray)
        {
            EquipmentViewModel eqVm = new EquipmentViewModel();
            bool success = eqVm.DeleteFamilyMembership(Convert.ToInt32(optionsArray[0]));

            if (success == true)
            {
                TempData["InformationBoxFlag"] = "Family Membership Deleted";
            }

            EquipmentViewModel.ReturnType en = EquipmentViewModel.ReturnType.FamilyMemberships;
            ViewBag.FamilyID = Convert.ToInt32(optionsArray[0]);

            return PartialView("~/Views/PMS/Equipment/FamilyMemData.cshtml", eqVm.Get(Convert.ToInt32(optionsArray[2]), en,
                                Convert.ToInt32(optionsArray[1]), null, null, null, null).familyMembershipList);
        }

        public JsonResult GetProducts(string searchTerm)
        {
            return Json(SelectListViewModel.GetProductsArray(searchTerm).ToList(), JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetNewFamilyRow(List<string> optionsArray)
        {
            return PartialView("~/Views/PMS/Equipment/CreateFamilyMapping.cshtml", 
                EquipmentViewModel.CreateNewFamilyMapping(Convert.ToInt32(optionsArray[0]), 
                Convert.ToInt32(optionsArray[1])));
        }
    }
}

