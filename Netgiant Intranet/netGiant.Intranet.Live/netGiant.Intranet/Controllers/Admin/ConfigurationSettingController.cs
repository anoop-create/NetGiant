using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using netGiant.Intranet.BusinessLayer.ViewModels.Admin;
using netGiant.Intranet.DataLayer;
using netGiant.Intranet.Models;
using System.IO;

namespace netGiant.Intranet.Controllers.Admin
{
    [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
    public class ConfigurationSettingController : ApplicationController
    {
        // GET: ConfigurationSettings
        public ActionResult Index()
        {
            ConfigurationSettingViewModel model = new ConfigurationSettingViewModel();
            return View("~/Views/Admin/ConfigurationSetting/ConfigurationSettingIndex.cshtml",
                model.Get());
        }

        [ChildActionOnly]
        public ActionResult ConfigurationSettingList(List<configurationSetting> model)
        {
            return PartialView("~/Views/Admin/ConfigurationSetting/ConfigurationSettingData.cshtml", model);
        }

        [HttpPost]
        public ActionResult IndexData(string[] optionsArray)
        {
            ConfigurationSettingViewModel model = new ConfigurationSettingViewModel();
            model.Get(optionsArray[0], optionsArray[1].ToString(), optionsArray[2].ToString(), 
                Convert.ToInt32(optionsArray[3]), optionsArray[4].ToString(), Convert.ToInt32(optionsArray[5]));
            return GetJson(model);
        }

        public ActionResult Create(int id)
        {
            ConfigurationSettingViewModel model = new ConfigurationSettingViewModel();
            return View("~/Views/Admin/ConfigurationSetting/CreateConfigurationSetting.cshtml",
                model.Create(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Save(ConfigurationSettingViewModel model)
        {
            if (ModelState.IsValid)
            {
                model.Save();
                TempData["InformationBoxFlag"] = "Configuration Setting Saved";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult Delete(List<string> optionsArray)
        {
            ConfigurationSettingViewModel model = new ConfigurationSettingViewModel();
            model.Delete(Convert.ToInt32(optionsArray[0]));
            model.Get(optionsArray[0], optionsArray[1].ToString(), optionsArray[2].ToString(), 
                Convert.ToInt32(optionsArray[3]), optionsArray[4].ToString(), Convert.ToInt32(optionsArray[5]));
            TempData["InformationBoxFlag"] = "Configuration Setting Deleted";

            return GetJson(model);
        }

        private ActionResult GetJson(ConfigurationSettingViewModel model)
        {
            JsonModel jsonModel = new JsonModel();
            jsonModel.NoMoreData = model.SettingList.Count < 50;
            jsonModel.Count = model.ConfigurationSettingsCount;
            jsonModel.HTMLString = base.RenderPartialViewToString("~/Views/Admin/ConfigurationSetting/ConfigurationSettingData.cshtml",
                model.SettingList);
            jsonModel.InfoBoxMessage = TempData["InformationBoxFlag"] != null ? TempData["InformationBoxFlag"].ToString() : "";
            return Json(jsonModel);
        }
    }
}