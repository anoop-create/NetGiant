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
    [Authorize(Roles = "IntranetAdmin, PMSReader, PMSAdmin")]
    public class GoogleProductCategoryController : ApplicationController
    {
        public ActionResult GoogleProductCategoryIndex()
        {
            GoogleProductCategoryViewModel model = new GoogleProductCategoryViewModel();
            return View("~/Views/PMS/Maintenance/GoogleProductCategory/GoogleProductCategoryIndex.cshtml", model.GetGoogleProductCategory());
        }

        public ActionResult GoogleProductCategoryList(List<googleProductCategory> model)
        {
            return PartialView("~/Views/PMS/Maintenance/GoogleProductCategory/GoogleProductCategoryData.cshtml", model);
        }

        public ActionResult GoogleProductCategoryData(string[] optionsArray)
        {
            GoogleProductCategoryViewModel model = new GoogleProductCategoryViewModel();
            model.GetGoogleProductCategory(optionsArray[0].ToString(), optionsArray[1].ToString(), optionsArray[2].ToString(), Convert.ToInt32(optionsArray[3]));
            return GoogleProductCategoryGetJson(model);
        }

        private ActionResult GoogleProductCategoryGetJson(GoogleProductCategoryViewModel model)
        {
            JsonModel jsonModel = new JsonModel();
            jsonModel.NoMoreData = model.GoogleProductCategoryList.Count < 50;
            jsonModel.Count = model.GoogleProductCategoryListCount;
            jsonModel.HTMLString = RenderPartialViewToString("~/Views/PMS/Maintenance/GoogleProductCategory/GoogleProductCategoryData.cshtml",
                model.GoogleProductCategoryList);
            jsonModel.InfoBoxMessage = TempData["InformationBoxFlag"] != null ? TempData["InformationBoxFlag"].ToString() : "";
            return Json(jsonModel);
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Create(int id)
        {
            GoogleProductCategoryViewModel model = new GoogleProductCategoryViewModel();
            return View("~/Views/PMS/Maintenance/GoogleProductCategory/CreateGoogleProductCategory.cshtml",
                model.Create(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Save(GoogleProductCategoryViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    model.Save();
                    TempData["InformationBoxFlag"] = "Google Product Category Saved";
                }
                return RedirectToAction("GoogleProductCategoryIndex");
            }
            catch (Exception e)
            {
                ModelState.AddModelError(String.Empty, e.Message);
                return RedirectToAction("Create", new { id = model.GoogleProductCategory.googleProductCategoryID});
            }
        }

        [HttpPost]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Delete(List<string> optionsArray)
        {
            GoogleProductCategoryViewModel model = new GoogleProductCategoryViewModel();
            model.Delete(Convert.ToInt32(optionsArray[0]));
            model.GetGoogleProductCategory(optionsArray[0].ToString(), optionsArray[1].ToString(), optionsArray[2].ToString(), Convert.ToInt32(optionsArray[3]));
            TempData["InformationBoxFlag"] = "Google Product Category Deleted";

            return GoogleProductCategoryGetJson(model);
        }
    }
}