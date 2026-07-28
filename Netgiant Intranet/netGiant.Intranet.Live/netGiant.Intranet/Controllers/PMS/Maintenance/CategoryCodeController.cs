using System.Web.Mvc;
using System.Collections.Generic;
using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance;
using System;
using netGiant.Intranet.DataLayer;
using netGiant.Intranet.Models;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using System.Linq;

namespace netGiant.Intranet.Controllers.PMS.Maintenance
{
    [Authorize]
    public class CategoryCodeController : ApplicationController
    {
        public ActionResult Index()
        {
            CategoryCodeViewModel cCodeVm = new CategoryCodeViewModel();

            return View("~/Views/PMS/Maintenance/CategoryCode/CategoryCodeIndex.cshtml",
                                cCodeVm.Get(1, null, null, null, null, null));
        }

        public ActionResult IndexData(List<string> optionsArray)
        {
            CategoryCodeViewModel model = new CategoryCodeViewModel();
            model = model.Get(Convert.ToInt32(optionsArray[5]), optionsArray[0], optionsArray[1],
                                Convert.ToInt32(optionsArray[2]), Convert.ToInt32(optionsArray[3]), optionsArray[4]);

            JsonModel jsonModel = new JsonModel();
            jsonModel.NoMoreData = model.categoryCodesList.Count < 50;
            jsonModel.Count = model.categoryCodeCount;
            jsonModel.HTMLString = RenderPartialViewToString("~/Views/PMS/Maintenance/CategoryCode/CategoryCodeData.cshtml", model.categoryCodesList);
            jsonModel.InfoBoxMessage = TempData["InformationBoxFlag"] != null ? TempData["InformationBoxFlag"].ToString() : "";
            return Json(jsonModel);
        }

        [ChildActionOnly]
        public ActionResult IndexList(List<categoryCode> Model)
        {
            return PartialView("~/Views/PMS/Maintenance/CategoryCode/CategoryCodeData.cshtml", Model);
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Create(int id)
        {

            CategoryCodeViewModel cCodeVm = new CategoryCodeViewModel();

            return View("~/Views/PMS/Maintenance/CategoryCode/CreateCategoryCode.cshtml", cCodeVm.Create(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Save(CategoryCodeViewModel model)
        {
            if (ModelState.IsValid)
            {
                model.Save(model);
                TempData["InformationBoxFlag"] = "Category Code Saved";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Delete(List<string> optionsArray)
        {
            CategoryCodeViewModel cCodeVm = new CategoryCodeViewModel();
            bool success = cCodeVm.Delete(Convert.ToInt32(optionsArray[0]));

            if (success == true)
            {
                TempData["InformationBoxFlag"] = "Category Code Deleted";
            }

            return PartialView("~/Views/PMS/Maintenance/CategoryCode/CategoryCodeData.cshtml",
                                cCodeVm.Get(Convert.ToInt32(optionsArray[6]), optionsArray[2], optionsArray[1],
                                Convert.ToInt32(optionsArray[3]), Convert.ToInt32(optionsArray[4]), 
                                optionsArray[5]).categoryCodesList);
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult CategoryAttributesIndex(int id)
        {
            CategoryCodeViewModel cCodeVm = new CategoryCodeViewModel();
            ViewBag.CategoryCodeName = cCodeVm.GetCategoryCodeName(id);
            ViewBag.CategoryCodeID = id;

            return View("~/Views/PMS/Maintenance/CategoryAttributes/CategoryAttributesIndex.cshtml",
                        cCodeVm.GetCategoryAttributes(id, 1, null).categoryAttributesList);
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult CategoryAttributesData(List<string> optionsArray)
        {
            CategoryCodeViewModel cCodeVm = new CategoryCodeViewModel();
            int catCodeID = Convert.ToInt32(optionsArray[0]);
            ViewBag.CategoryCodeID = catCodeID;

            return PartialView("~/Views/PMS/Maintenance/CategoryAttributes/CategoryAttributesData.cshtml",
                                cCodeVm.GetCategoryAttributes(catCodeID, Convert.ToInt32(optionsArray[2]),
                                optionsArray[1]).categoryAttributesList);
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult CreateCategoryAttributes(int id)
        {
            CategoryCodeViewModel cCodeVm = new CategoryCodeViewModel();
            ViewBag.CategoryCodeName = cCodeVm.GetCategoryCodeName(id);
            ViewBag.CategoryCodeID = id;
            return View("~/Views/PMS/Maintenance/CategoryAttributes/CreateCategoryAttributes.cshtml", cCodeVm.CreateCategoryAttributes(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult SaveCategoryAttributes(CategoryCodeViewModel model)
        {
            if (ModelState.IsValid)
            {
                bool success = model.SaveCategoryAttribute(model);

                if (success == true)
                {
                    TempData["InformationBoxFlag"] = "Category Attributes Saved";
                }
            }

            return RedirectToAction("CategoryAttributesIndex", new { id = model.categoryCodeID });
        }

        [HttpPost]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult DeleteCategoryAttribute(List<string> optionsArray)
        {
            CategoryCodeViewModel cCodeVm = new CategoryCodeViewModel();
            bool success = cCodeVm.DeleteCategoryAttribute(Convert.ToInt32(optionsArray[0]));

            if (success == true)
            {
                TempData["InformationBoxFlag"] = "Category Attribute Deleted";
            }

            int catCodeID = Convert.ToInt32(optionsArray[1]);
            ViewBag.CategoryCodeID = catCodeID;

            return PartialView("~/Views/PMS/Maintenance/CategoryAttributes/CategoryAttributesData.cshtml",
                                cCodeVm.GetCategoryAttributes(catCodeID, Convert.ToInt32(optionsArray[0]),
                                optionsArray[1]).categoryAttributesList);
        }

        public JsonResult GetCategoryCodes(int id)
        {
            return Json(SelectListViewModel.AllCategoryCodes(id, false).ToList(), JsonRequestBehavior.AllowGet);
        }
    }
}
