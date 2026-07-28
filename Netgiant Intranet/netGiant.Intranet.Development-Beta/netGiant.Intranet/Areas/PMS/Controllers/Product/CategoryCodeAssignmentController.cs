using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Product;
using netGiant.Intranet.BusinessLayer.Models;
using System;
using System.Collections.Generic;
using System.Web.Mvc;
using netGiant.Intranet.Controllers;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;

namespace netGiant.Intranet.Areas.PMS.Product
{
    [Authorize(Roles = "IntranetAdmin, PMSReader, PMSAdmin")]
    public class CategoryCodeAssignmentController : ApplicationController
    {
        public ActionResult CategoryCodeAssignmentIndex()
        {
            //var model = new CategoryCodeAssignmentViewModel();
            //return View(model.GetCategoryCodeAssignment());
            return View("CategoryCodeAssignmentIndex", new CategoryCodeAssignmentViewModel());
        }

        public ActionResult CategoryCodeAssignment_Read([DataSourceRequest]DataSourceRequest request)
        {
            var model = new CategoryCodeAssignmentViewModel().Get();

            var result = model.CategoryCodeAssignmentList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        //public ActionResult CategoryCodeAssignmentList(List<websiteInventory> model)
        //{
        //    return PartialView("_CategoryCodeAssignmentData", model);
        //}

        //public ActionResult CategoryCodeAssignmentData(string[] optionsArray)
        //{
        //    var model = new CategoryCodeAssignmentViewModel();
        //    model.GetCategoryCodeAssignment(optionsArray[0].ToString(), optionsArray[1].ToString(), optionsArray[2].ToString(), Convert.ToInt32(optionsArray[3]), Convert.ToInt32(optionsArray[4]), 
        //        Convert.ToInt32(optionsArray[5]), Convert.ToInt32(optionsArray[6]), Convert.ToBoolean(optionsArray[7]), Convert.ToInt32(optionsArray[8]));
        //    return CategoryCodeAssignmentGetJson(model);
        //}

        //private ActionResult CategoryCodeAssignmentGetJson(CategoryCodeAssignmentViewModel model)
        //{
        //    var jsonModel = new JsonModel();
            
        //    jsonModel.NoMoreData = model.CategoryCodeAssignmentList.Count < 50;
        //    jsonModel.Count = model.CategoryCodeAssignmentListCount;
        //    jsonModel.HTMLString = RenderPartialViewToString("_CategoryCodeAssignmentData", model.CategoryCodeAssignmentList);
        //    jsonModel.InfoBoxMessage = TempData["InformationBoxFlag"] != null ? TempData["InformationBoxFlag"].ToString() : "";

        //    //Set max length for JSON Serializer to max int value
        //    var returnJson = Json(jsonModel);
        //    returnJson.MaxJsonLength = int.MaxValue;

        //    return returnJson;
        //}

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //[Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        //public ActionResult Save(CategoryCodeAssignmentViewModel model)
        //{    
        //    try
        //    {
        //        if (ModelState.IsValid)
        //        {
        //            model.Save();
        //            TempData["InformationBoxFlag"] = "Category Codes Have Been Assigned";
        //        }
        //        return RedirectToAction("CategoryCodeAssignmentIndex");
        //    }
        //    catch (Exception e)
        //    {
        //        ModelState.AddModelError(string.Empty, e.Message);
        //        return RedirectToAction("CategoryCodeAssignmentIndex");
        //    }
        //}
    }
}