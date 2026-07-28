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
    public class ObsoleteItemsController : ApplicationController
    {
        public ActionResult ObsoleteItemsIndex()
        {
            ObsoleteItemsViewModel model = new ObsoleteItemsViewModel();
            return View("~/Views/PMS/Maintenance/ObsoleteItems/ObsoleteItemsIndex.cshtml", model.GetObsoleteItems());

        }

        public ActionResult ObsoleteItemsList(List<obsoleteItem> model)
        {
            return PartialView("~/Views/PMS/Maintenance/ObsoleteItems/ObsoleteItemsData.cshtml", model);
        }

        public ActionResult ObsoleteItemsData(string[] optionsArray)
        {
            ObsoleteItemsViewModel model = new ObsoleteItemsViewModel();

            model.GetObsoleteItems(optionsArray[0].ToString(), optionsArray[1].ToString(), optionsArray[2].ToString(), 
                Convert.ToInt32(optionsArray[3]), Convert.ToInt32(optionsArray[4]));
            return ObsoleteItemsGetJson(model);
        }

        private ActionResult ObsoleteItemsGetJson(ObsoleteItemsViewModel model)
        {
            JsonModel jsonModel = new JsonModel();
            jsonModel.NoMoreData = model.ObsoleteItemsList.Count < 50;
            jsonModel.Count = model.ObsoleteItemsListCount;
            jsonModel.HTMLString = RenderPartialViewToString("~/Views/PMS/Maintenance/ObsoleteItems/ObsoleteItemsData.cshtml",
                model.ObsoleteItemsList);
            jsonModel.InfoBoxMessage = TempData["InformationBoxFlag"] != null ? TempData["InformationBoxFlag"].ToString() : "";
            return Json(jsonModel);
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin, SEO")]
        public ActionResult Create(int id)
        {
            ObsoleteItemsViewModel model = new ObsoleteItemsViewModel();
            return View("~/Views/PMS/Maintenance/ObsoleteItems/CreateObsoleteItem.cshtml",
                model.Create(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin, SEO")]
        public ActionResult Save(ObsoleteItemsViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    model.Save();
                    TempData["InformationBoxFlag"] = "Obsolete Item Saved";
                }
                return RedirectToAction("ObsoleteItemsIndex");
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.Message);
                model.SetupSelectLists();
                return View("~/Views/PMS/Maintenance/ObsoleteItems/CreateObsoleteItem.cshtml", model);
            }
        }

        [HttpPost]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin, SEO")]
        public ActionResult Delete(List<string> optionsArray)
        {
            ObsoleteItemsViewModel model = new ObsoleteItemsViewModel();

            model.Delete(Convert.ToInt32(optionsArray[0]));
            model.GetObsoleteItems(optionsArray[0].ToString(), optionsArray[1].ToString(), optionsArray[2].ToString(),
                Convert.ToInt32(optionsArray[3]), Convert.ToInt32(optionsArray[4]));
            TempData["InformationBoxFlag"] = "Obsolete Item Deleted";

            return ObsoleteItemsGetJson(model);
        }
    }
}