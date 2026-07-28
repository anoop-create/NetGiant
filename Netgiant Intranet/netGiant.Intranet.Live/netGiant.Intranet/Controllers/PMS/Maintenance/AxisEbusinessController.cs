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
    public class AxisEbusinessController : ApplicationController
    {
        public ActionResult eBusinessIndex()
        {
            AxisEBusinessViewModel model = new AxisEBusinessViewModel();
            return View("~/Views/PMS/Maintenance/AxisEbusiness/AxisEbusinessIndex.cshtml", model.GetEbusinessGroups()); 
        }

        public ActionResult eBusinessList(List<AxisEbusiness> model)
        {
            return PartialView("~/Views/PMS/Maintenance/AxisEbusiness/AxisEbusinessData.cshtml", model);
        }

        public ActionResult eBusinessIndexData(string[] optionsArray)
        {
            AxisEBusinessViewModel model = new AxisEBusinessViewModel();
            model.GetEbusinessGroups(optionsArray[0].ToString(), optionsArray[1].ToString(), optionsArray[2].ToString(), Convert.ToInt32(optionsArray[3]));
            return eBusinessGetJson(model);
        }

        private ActionResult eBusinessGetJson(AxisEBusinessViewModel model)
        {
            JsonModel jsonModel = new JsonModel();
            jsonModel.NoMoreData = model.EbusinessGroupList.Count < 50;
            jsonModel.Count = model.EbusinessGroupListCount;
            jsonModel.HTMLString = RenderPartialViewToString("~/Views/PMS/Maintenance/AxisEbusiness/AxisEbusinessData.cshtml",
                model.EbusinessGroupList);
            jsonModel.InfoBoxMessage = TempData["InformationBoxFlag"] != null ? TempData["InformationBoxFlag"].ToString() : "";
            return Json(jsonModel);
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Create(int id)
        {
            AxisEBusinessViewModel model = new AxisEBusinessViewModel();
            return View("~/Views/PMS/Maintenance/AxisEbusiness/CreateAxisEBusinessGroup.cshtml",
                model.Create(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Save(AxisEBusinessViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    model.Save();
                    TempData["InformationBoxFlag"] = "Configuration Setting Saved";
                } 
                return RedirectToAction("eBusinessIndex");
            }
            catch (Exception e)
            {
                ModelState.AddModelError(String.Empty, e.Message);
                return RedirectToAction("Create", new { id = model.AxisEBusinessGroup.AxisEbusinessID });
            }
        }

        [HttpPost]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
        public ActionResult Delete(List<string> optionsArray)
        {
            AxisEBusinessViewModel model = new AxisEBusinessViewModel();
            model.Delete(Convert.ToInt32(optionsArray[0]));
            model.GetEbusinessGroups(optionsArray[0].ToString(), optionsArray[1].ToString(), optionsArray[2].ToString(), Convert.ToInt32(optionsArray[3]));
            TempData["InformationBoxFlag"] = "EBusiness Group Deleted";

            return eBusinessGetJson(model);
        }
    }
}