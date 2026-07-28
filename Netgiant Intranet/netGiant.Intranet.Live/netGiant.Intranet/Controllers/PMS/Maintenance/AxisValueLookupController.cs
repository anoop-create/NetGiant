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
    public class AxisValueLookupController : ApplicationController
    {
        public ActionResult AxisValueLookupIndex()
        {
            AxisValueLookupViewModel model = new AxisValueLookupViewModel();
            return View("~/Views/PMS/Maintenance/AxisValueLookup/AxisValueLookupIndex.cshtml", model.GetAxisValueLookup()); 
        }

        public ActionResult AxisValueLookupList(List<AxisValueLookup> model)
        {
            return PartialView("~/Views/PMS/Maintenance/AxisValueLookup/AxisValueLookupData.cshtml", model);
        }

        public ActionResult AxisValueLookupData(string[] optionsArray)
        {
            AxisValueLookupViewModel model = new AxisValueLookupViewModel();
            model.GetAxisValueLookup(optionsArray[0].ToString(), optionsArray[1].ToString(), optionsArray[2].ToString(), Convert.ToInt32(optionsArray[3]), Convert.ToInt32(optionsArray[4]));
            return AxisValueLookupGetJson(model);
        }

        private ActionResult AxisValueLookupGetJson(AxisValueLookupViewModel model)
        {
            JsonModel jsonModel = new JsonModel();
            jsonModel.NoMoreData = model.AxisValueLookupList.Count < 50;
            jsonModel.Count = model.AxisValueLookupListCount;
            jsonModel.HTMLString = RenderPartialViewToString("~/Views/PMS/Maintenance/AxisValueLookup/AxisValueLookupData.cshtml",
                model.AxisValueLookupList);
            jsonModel.InfoBoxMessage = TempData["InformationBoxFlag"] != null ? TempData["InformationBoxFlag"].ToString() : "";
            return Json(jsonModel);
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Create(int id)
        {
            AxisValueLookupViewModel model = new AxisValueLookupViewModel();
            return View("~/Views/PMS/Maintenance/AxisValueLookup/CreateAxisValueLookup.cshtml",
                model.Create(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Save(AxisValueLookupViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    model.Save();
                    TempData["InformationBoxFlag"] = "AxisValueLookup Record Saved";
                }
                return RedirectToAction("AxisValueLookupIndex");
            }
            catch (Exception e)
            {
                ModelState.AddModelError(String.Empty, e.Message);
                return RedirectToAction("Create", new { id = model.AxisValueLookup.axisValueLookupID });
            }
        }

        [HttpPost]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Delete(List<string> optionsArray)
        {
            AxisValueLookupViewModel model = new AxisValueLookupViewModel();
            model.Delete(Convert.ToInt32(optionsArray[0]));
            model.GetAxisValueLookup(optionsArray[0].ToString(), optionsArray[1].ToString(), optionsArray[2].ToString(), Convert.ToInt32(optionsArray[3]), Convert.ToInt32(optionsArray[4]));
            TempData["InformationBoxFlag"] = "AxisValueLookup Record Deleted";

            return AxisValueLookupGetJson(model);
        }
    }
}