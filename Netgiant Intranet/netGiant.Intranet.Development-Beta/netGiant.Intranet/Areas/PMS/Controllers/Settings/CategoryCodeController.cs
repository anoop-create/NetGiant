using System.Web.Mvc;
using System.Collections.Generic;
using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance;
using System;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using System.Linq;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;

namespace netGiant.Intranet.Areas.PMS.Maintenance
{
    [Authorize]
    public class CategoryCodeController : Controller
    {
        public ActionResult Index()
        {
            return View("CategoryCodeIndex", new CategoryCodeViewModel());
        }

        public ActionResult CategoryCode_Read([DataSourceRequest]DataSourceRequest request)
        {
            var model = new CategoryCodeViewModel();
            model.GetCategoryCodes();

            var result = model.CategoryCodeList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Create(int id)
        {
            return View("CreateCategoryCode", new CategoryCodeViewModel().Create(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Save(CategoryCodeViewModel model, string content)
        {
            if (ModelState.IsValid)
            {
                model.catCode.description = content;
                model.Save(model);
                TempData["InformationBoxFlag"] = "Category Code Saved";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Delete(int id)
        {
            return Json(new { saveReturn = new CategoryCodeViewModel().Delete(id) });
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult CategoryAttributesIndex(int id)
        {
            var model = new CategoryCodeViewModel();
            ViewBag.CategoryCodeName = model.GetCategoryCodeName(id);
            ViewBag.CategoryCodeID = id;

            return View(model.GetCategoryAttributes(id, 1, null));
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult CategoryAttributesData(List<string> optionsArray)
        {
            var model = new CategoryCodeViewModel();
            int catCodeID = Convert.ToInt32(optionsArray[0]);
            ViewBag.CategoryCodeID = catCodeID;

            return PartialView("_CategoryAttributesData", model.GetCategoryAttributes(catCodeID, Convert.ToInt32(optionsArray[2]), optionsArray[1]).CategoryAttributesList);
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult CreateCategoryAttributes(int id)
        {
            var model = new CategoryCodeViewModel();
            ViewBag.CategoryCodeName = model.GetCategoryCodeName(id);
            ViewBag.CategoryCodeID = id;
            return View(model.CreateCategoryAttributes(id));
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
            var model = new CategoryCodeViewModel();
            bool success = model.DeleteCategoryAttribute(Convert.ToInt32(optionsArray[0]));

            if (success == true)
            {
                TempData["InformationBoxFlag"] = "Category Attribute Deleted";
            }

            int catCodeID = Convert.ToInt32(optionsArray[1]);
            ViewBag.CategoryCodeID = catCodeID;

            return PartialView("_CategoryAttributesData", model.GetCategoryAttributes(catCodeID, Convert.ToInt32(optionsArray[3]), "attributeNameAsc").CategoryAttributesList);
        }

        public JsonResult GetCategoryCodes(int id)
        {
            return Json(SelectListViewModel.GetAllCategoryCodes(id, false).ToList(), JsonRequestBehavior.AllowGet);
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult OpenRangeAttributes(int id)
        {
            ViewBag.CategoryId = id;
            var model = new CategoryCodeViewModel();
            return View("OpenRangeIndex", model);
        }

        //[Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        //public ActionResult OpenRangeAttributes_Read([DataSourceRequest]DataSourceRequest request, int id)
        //{
        //    var model = new CategoryCodeViewModel();
        //    model.GetOpenRangeRemovables(id);

        //    var result = model.OpenRangeAttributeList.ToDataSourceResult(request);
        //    var jsonResult = Json(result);
        //    jsonResult.MaxJsonLength = int.MaxValue;
        //    return jsonResult;
        //}

        //public JsonResult GetOpenRangeAttributes(int id)
        //{
        //    var model = new CategoryCodeViewModel();
        //    var sr = model.GetOpenRangeAttributes(id);

        //    return Json(new
        //    {
        //        saveReturn = sr
        //    });
        //}

        //[Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        //public JsonResult CreateOpenRangeRemovable(int nameId, int categoryId)
        //{
        //    var model = new CategoryCodeViewModel();
        //    var sr = model.CreateOpenRangeRemovable(nameId, categoryId);

        //    return Json(new
        //    {
        //        saveReturn = sr
        //    });
        //}

        //[Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        //public JsonResult DeleteOpenRangeRemovable(int id)
        //{
        //    var model = new CategoryCodeViewModel();
        //    var sr = model.DeleteOpenRangeRemovable(id);

        //    return Json(new
        //    {
        //        saveReturn = sr
        //    });
        //}
    }
}
