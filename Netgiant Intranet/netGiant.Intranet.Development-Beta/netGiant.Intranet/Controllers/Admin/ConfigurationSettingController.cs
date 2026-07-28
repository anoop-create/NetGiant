using System;
using System.Web.Mvc;
using netGiant.Intranet.BusinessLayer.ViewModels.Admin;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;

namespace netGiant.Intranet.Controllers.Admin
{
    [Authorize(Roles = "IntranetAdmin, PMSAdmin")]
    public class ConfigurationSettingController : ApplicationController
    {
        // GET: ConfigurationSettings
        public ActionResult Index()
        {
            var model = new ConfigurationSettingViewModel();
            return View("ConfigurationSettingIndex", model);
        }

        public ActionResult ConfigurationSetting_Read([DataSourceRequest]DataSourceRequest request)
        {
            ConfigurationSettingViewModel model = new ConfigurationSettingViewModel();
            model.GetConfigurationSettingList();

            var result = model.ConfigurationSettingList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        [Authorize(Roles = "IntranetAdmin, PMSAdmin, SEO")]
        public ActionResult Create(int id)
        {
            var model = new ConfigurationSettingViewModel();
            return View("CreateConfigurationSetting", model.CreateConfigurationSetting(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin, SEO")]
        public ActionResult Save(ConfigurationSettingViewModel model)
        {
            try
            {
                if (model.Save())
                {
                    TempData["InformationBoxFlag"] = "Congiuration Setting Saved";
                }
                return RedirectToAction("Index");
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.Message);
                return View("CreateConfigurationSetting", model);
            }
        }

        [HttpPost]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin, SEO")]
        public ActionResult Delete(int id)
        {
            ConfigurationSettingViewModel model = new ConfigurationSettingViewModel();

            SaveReturn sr = model.DeleteConfigurationSetting(id);

            return Json(new { saveReturn = sr });
        }

        [HttpPost]
        [Authorize(Roles = "IntranetAdmin, PMSAdmin, SEO")]
        public ActionResult SetSelectionToTrue(string settingName)
        {
            ConfigurationSettingViewModel model = new ConfigurationSettingViewModel();

            SaveReturn sr = model.SetSelectionToTrue(x => x.settingName == settingName);

            return Json(new { saveReturn = sr });
        }
    }
}