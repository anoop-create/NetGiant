using DP001BusinessLogic;
using DP001BusinessLogic.ViewModels;
using DP001DataAccess.Entities;
using DP001DataAccess.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DP001Website.Controllers
{
    [Authorize]
    public class TenantSettingsController : ApplicationController
    {
        public ActionResult Index()
        {
            var tenant = GetTenant();
            var model = new TenantSettingsViewModel(tenant);

            return View(model);
        }

        public ActionResult Main()
        {
            var tenant = GetTenant();
            var model = new TenantSettingsViewModel(tenant);
            int channelId = Int32.Parse(Session["CurrentChannel"].ToString());

            ViewBag.thisChannel = model.Tenant.Channels.Where(x => x.ChannelID == channelId).FirstOrDefault();
            ViewBag.tenantId = tenant.TenantID;
            ViewBag.channelId = channelId;

            var crudFtpSetting = new CrudFtpSetting();
            model.FtpSettingList = crudFtpSetting.Read(x => x.ChannelFK == channelId);
            var crudSchedule = new CrudSchedule();
            model.ScheduleList = crudSchedule.Read(x => x.ChannelFK == channelId);
            var crudCustomField = new CrudCustomField();
            model.CustomFieldList = crudCustomField.Read(x => x.ChannelFK == channelId).OrderBy(x => x.CustFieldTypeFK).ThenBy(x => x.UserFieldName).ToList();

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        [ValidateAntiForgeryToken]
        public JsonResult Update(TenantSettingsViewModel model)
        {
            var currentTenantId = GetTenant().TenantID;
            var crudTenant = new CrudTenant();
            var dbTenant = crudTenant.Read(x => x.TenantID == currentTenantId).First();

            dbTenant.ContactName = model.Tenant.ContactName;
            dbTenant.ContactEmail = model.Tenant.ContactEmail;
            dbTenant.SalesHistory = dbTenant.AllowSalesHistory && model.Tenant.SalesHistory;
            dbTenant.CloneChannel = dbTenant.AllowCloneChannel && model.Tenant.CloneChannel;
            dbTenant.PriceRuleBanding = dbTenant.AllowPriceRuleBanding && model.Tenant.PriceRuleBanding;
            dbTenant.MultipleSuppliers = dbTenant.AllowMultipleSuppliers && model.Tenant.MultipleSuppliers;
            dbTenant.MultipleCalculationMethods = dbTenant.AllowMultipleCalculationMethods && model.Tenant.MultipleCalculationMethods;
            dbTenant.ProviderBrandExclusion = dbTenant.AllowProviderBrandExclusion && model.Tenant.ProviderBrandExclusion;

            var saveReturn = model.Update(dbTenant);
            if (saveReturn.IsSuccess)
            {                
                RefreshTenant(dbTenant.TenantID);
                return Json(new
                {
                    IsSuccess = true,
                    Id = dbTenant.TenantID,
                    Action = "Save",
                    Msg = ""
                }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new
                {
                    IsSuccess = false,
                    Id = dbTenant.TenantID,
                    Action = "Save",
                    Msg = saveReturn.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        [ChildActionOnly]
        public void RefreshTenant(int tenantId)
        {
            var tenantSetting = new CrudTenant();
            Session["Tenant"] = tenantSetting.Read(tenantId);
        }
    }
}