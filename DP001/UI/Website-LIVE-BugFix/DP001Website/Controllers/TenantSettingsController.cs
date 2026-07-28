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

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        [ValidateAntiForgeryToken]
        public JsonResult Update(TenantSettingsViewModel model)
        {
            model.Tenant.TenantID = GetTenant().TenantID;

            var saveReturn = model.Update(model.Tenant);
            if (saveReturn.IsSuccess)
            {                
                RefreshTenant(model.Tenant.TenantID);
                return Json(new
                {
                    IsSuccess = true,
                    Id = model.Tenant.TenantID,
                    Action = "Save",
                    Msg = ""
                }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new
                {
                    IsSuccess = false,
                    Id = model.Tenant.TenantID,
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