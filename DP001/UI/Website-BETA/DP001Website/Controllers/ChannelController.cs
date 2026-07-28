using DP001BusinessLogic;
using DP001BusinessLogic.Shared;
using DP001BusinessLogic.ViewModels;
using DP001DataAccess.Entities;
using DP001DataAccess.Utilities;
using DP001Website.Models;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DP001Website.Controllers
{
    [Authorize]
    public class ChannelController : ApplicationController
    {
        // GET: Channel
        public ActionResult Index()
        {
            return View();
        }

        //[Authorize(Roles = "SuperAdmin")]
        public void ChangeChannel(int newChannel)
        {
            var tenant = GetTenant();

            if (tenant.Channels.Any(x => x.ChannelID == newChannel))
            {
                try
                {
                    Session["CurrentChannel"] = newChannel;

                    var crud = new CrudTenant();
                    tenant.LastUsedChannelId = newChannel;
                    crud.Update(tenant);

                    CommonModel cm = new CommonModel();
                    cm.RefreshLogSummaryCount(tenant.LastUsedChannelId);
                }
                catch (Exception e)
                {
                    CommonDataFunctions.CreateLogEntry(tenant.TenantID, newChannel, "Error changing channel: " +
                        e.Message + " Stack: " + e.StackTrace, "Error");
                }
            }
        }

        public ActionResult New()
        {
            var tenant = GetTenant();
            var model = new TenantSettingsViewModel(tenant);
            var crudLookup = new CrudLookup();
            List<Lookup> v = crudLookup.Read(x => x.LookupType.LookupTypeName == "SkuudleLiteActiveType" &&
                x.LookupName == "SL None");
            model.ChannelEntry = new Channel();
            model.ChannelEntry.SLActiveTypeFK = crudLookup.Read(x => x.LookupType.LookupTypeName == "SkuudleLiteActiveType" &&
                x.LookupName == "SL None").FirstOrDefault().LookupID;
            model.ChannelEntry.UseClientProductId = true;
            model.RoundingGroups = SharedViewModel.GetLookupList("RoundingGroup");

            return PartialView(model);
        }

        public ActionResult Edit(int Id)
        {
            var tenant = GetTenant();
            var model = new TenantSettingsViewModel(tenant);
            model.ChannelEntry = model.Tenant.Channels.Where(x => x.ChannelID == Id).FirstOrDefault();
            model.RoundingGroups = SharedViewModel.GetLookupList("RoundingGroup");

            if (model.ChannelEntry == null)
            {
                return RedirectToAction("Main", "TenantSettings");
            }

            return PartialView(model);
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        [ValidateAntiForgeryToken]
        public JsonResult Create(TenantSettingsViewModel model)
        {
            model.Tenant = GetTenant();
            model.ChannelEntry.TenantFK = model.Tenant.TenantID;

            var saveReturn = model.CreateChannel();
            if (saveReturn.IsSuccess)
            {
                RefreshTenant(model.Tenant.TenantID);
                string pv = RenderPartialViewToString("~/Views/Channel/IndexRow.cshtml", model);
                pv = "<tr class=\"list-form-row\" data-id=\"" + model.ChannelEntry.ChannelID + "\">" + pv + "</tr>";
                return Json(new { isSuccess = true, id = model.ChannelEntry.ChannelID, action = "Save", html = pv, msg = "" }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new { isSuccess = false, id = model.ChannelEntry.ChannelID, action = "Save", msg = saveReturn.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        [ValidateAntiForgeryToken]
        public JsonResult Update(TenantSettingsViewModel model)
        {
            model.Tenant = GetTenant();
            model.ChannelEntry.TenantFK = model.Tenant.TenantID;

            var hasPermission = model.Tenant.Channels.Any(x => x.ChannelID == model.ChannelEntry.ChannelID);
            if (hasPermission)
            {

                var saveReturn = model.UpdateChannel(model.ChannelEntry);
                if (saveReturn.IsSuccess)
                {
                    RefreshTenant(model.Tenant.TenantID);
                    model.CurrentChannelID = Int32.Parse(Session["CurrentChannel"].ToString());
                    string pv = RenderPartialViewToString("~/Views/Channel/IndexRow.cshtml", model);
                    return Json(new { isSuccess = true, id = model.ChannelEntry.ChannelID, action = "Save", html = pv, msg = "" }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(new { isSuccess = false, id = model.ChannelEntry.ChannelID, action = "Save", msg = saveReturn.Message }, JsonRequestBehavior.AllowGet);
                }
            }
            else
            {
                return Json(new { isSuccess = false, id = model.ChannelEntry.ChannelID, action = "Save", msg = "Channel does not exist or you do not have permission to change it" }, JsonRequestBehavior.AllowGet);
            }
        }

        [ChildActionOnly]
        public void RefreshTenant(int tenantId)
        {
            var tenantSetting = new CrudTenant();
            Session["Tenant"] = tenantSetting.Read(tenantId);
        }

        [HttpPost]
        [CheckUserPermission(FieldName = "CloneChannel", Check = TenantPermissonCheck.IsFeatureOn)]
        public JsonResult Clone(int channelId)
        {
            var currentUserTenantId = UserManager.FindById(User.Identity.GetUserId()).TenantID;
            var crudTenant = new CrudTenant();
            var userHasPermission = crudTenant.Read(currentUserTenantId).Channels.Any(x => x.ChannelID == channelId);
            var saveReturn = new SaveReturn();

            if (userHasPermission)
            {
                var model = new ChannelViewModel(GetChannelId());
                saveReturn = model.Clone(channelId, currentUserTenantId);

                var cm = new CommonModel();
                cm.RefreshTenantSession();
            }
            else
            {
                saveReturn.IsSuccess = false;
                saveReturn.Message = "You do not have permission to Clone this Channel.";
            }

            return Json(saveReturn, JsonRequestBehavior.AllowGet);
        }
    }
}

