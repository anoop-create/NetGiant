using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Product;
using netGiant.Intranet.DataLayer;
using netGiant.Intranet.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace netGiant.Intranet.Controllers.PMS.Product
{
    [Authorize(Roles = "IntranetAdmin, PMSReader, PMSAdmin")]
    public class CategoryCodeAssignmentController : ApplicationController
    {
        public ActionResult CategoryCodeAssignmentIndex()
        {
            CategoryCodeAssignmentViewModel model = new CategoryCodeAssignmentViewModel();
            return View("~/Views/PMS/Product/CategoryCodeAssignment/CategoryCodeAssignmentIndex.cshtml", model.GetCategoryCodeAssignment());
        }

        public ActionResult CategoryCodeAssignmentList(List<websiteInventory> model)
        {
            return PartialView("~/Views/PMS/Product/CategoryCodeAssignment/CategoryCodeAssignmentData.cshtml", model);
        }

        public ActionResult CategoryCodeAssignmentData(string[] optionsArray)
        {
            CategoryCodeAssignmentViewModel model = new CategoryCodeAssignmentViewModel();
            model.GetCategoryCodeAssignment(optionsArray[0].ToString(), optionsArray[1].ToString(), optionsArray[2].ToString(), Convert.ToInt32(optionsArray[3])
                , Convert.ToInt32(optionsArray[4]), Convert.ToInt32(optionsArray[5]), Convert.ToInt32(optionsArray[6]), Convert.ToBoolean(optionsArray[7]), Convert.ToInt32(optionsArray[8]));
            return CategoryCodeAssignmentGetJson(model);
        }

        private ActionResult CategoryCodeAssignmentGetJson(CategoryCodeAssignmentViewModel model)
        {
            JsonModel jsonModel = new JsonModel();
            
            jsonModel.NoMoreData = model.CategoryCodeAssignmentList.Count < 50;
            jsonModel.Count = model.CategoryCodeAssignmentListCount;
            jsonModel.HTMLString = RenderPartialViewToString("~/Views/PMS/Product/CategoryCodeAssignment/CategoryCodeAssignmentData.cshtml",
                model.CategoryCodeAssignmentList);
            jsonModel.InfoBoxMessage = TempData["InformationBoxFlag"] != null ? TempData["InformationBoxFlag"].ToString() : "";

            //Set max length for JSON Serializer to max int value
            var returnJson = Json(jsonModel);
            returnJson.MaxJsonLength = int.MaxValue;

            return returnJson;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Save(CategoryCodeAssignmentViewModel model)
        {    
            try
            {
                if (ModelState.IsValid)
                {
                    model.Save();
                    TempData["InformationBoxFlag"] = "Category Codes Have Been Assigned";
                }
            return RedirectToAction("CategoryCodeAssignmentIndex");
            }
            catch (Exception e)
            {
                ModelState.AddModelError(String.Empty, e.Message);
                return RedirectToAction("CategoryCodeAssignmentIndex");
            }
        }
    }
}