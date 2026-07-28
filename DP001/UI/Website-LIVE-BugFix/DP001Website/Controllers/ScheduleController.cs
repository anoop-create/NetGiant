using DP001BusinessLogic.ViewModels;
using DP001DataAccess.Utilities;
using DP001Website.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DP001Website.Controllers
{
    [Authorize]
    public class ScheduleController : ApplicationController
    {
        public ActionResult Index()
        {
            var tenant = GetTenant();
            int channelId = GetChannelId();
            var model = new ScheduleViewModel(channelId);
            model.GetSchedules();
            var channelModel = new ChannelViewModel(tenant.TenantID);
            model.Channel = channelModel.GetChannel(channelId);
            model.Channel = GetChannel();
            //if (!channel.JobInProgress.HasValue)
            //{
            //    channel.JobInProgress = false;
            //}           
            model.FindNextRun();

            return View(model);
        }

        public ActionResult Edit(int id)
        {
            int channelId = GetChannelId();
            var model = new ScheduleViewModel(channelId);
            model.Edit(id);

            if (model.ScheduleEntry != null)
            {
                return PartialView(model);
            }
            else
            {
                return RedirectToAction("Main", "TenantSettings");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        [ValidateAntiForgeryToken]
        [MultipleButton(Name = "action", Argument = "Update")]
        public JsonResult Update(ScheduleViewModel model)
        {
            model.ScheduleEntry.ChannelFK = GetChannelId();
            model.Channel = GetChannel();
            model.Tenant = GetTenant();

            var saveReturn = model.Update(model.ScheduleEntry);
            if (saveReturn.IsSuccess)
            {
                CommonModel cm = new CommonModel();
                cm.RefreshTenantSession();

                string pv = RenderPartialViewToString("~/Views/Schedule/IndexRow.cshtml", model);
                return Json(new { isSuccess = true, id = model.ScheduleEntry.ScheduleID, action = "Save", html = pv, msg = "" }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new { isSuccess = false, id = model.ScheduleEntry.ScheduleID, action = "Save", msg = saveReturn.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult New()
        {
            int channelId = GetChannelId();
            var model = new ScheduleViewModel(channelId);
            model.New();

            return PartialView(model);
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        [ValidateAntiForgeryToken]
        [MultipleButton(Name = "action", Argument = "Save")]
        public JsonResult Create(ScheduleViewModel model)
        {
            model.ScheduleEntry.ChannelFK = GetChannelId();
            model.Channel = GetChannel();
            model.Tenant = GetTenant();

            var saveReturn = model.Create();
            if (saveReturn.IsSuccess)
            {
                CommonModel cm = new CommonModel();
                cm.RefreshTenantSession();

                int channelId = GetChannelId();
                var savedModel = new ScheduleViewModel(channelId);
                model = savedModel.Edit(model.ScheduleEntry.ScheduleID);
                string pv = RenderPartialViewToString("~/Views/Schedule/IndexRow.cshtml", model);
                pv = "<tr class=\"list-form-row\" data-id=\"" + model.ScheduleEntry.ScheduleID + "\">" + pv + "</tr>";
                return Json(new { isSuccess = true, id = model.ScheduleEntry.ScheduleID, action = "Save", html = pv, msg = "" }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new { isSuccess = false, id = model.ScheduleEntry.ScheduleID, action = "Save", msg = saveReturn.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpDelete]
        [Authorize(Roles = "Administrator")]
        public JsonResult Delete(int id)
        {
            int channelId = GetChannelId();
            var model = new ScheduleViewModel(channelId);
            var sr = model.Delete(id);

            CommonModel cm = new CommonModel();
            cm.RefreshTenantSession();

            return Json(new
            {
                IsSuccess = sr.IsSuccess,
                Msg = sr.Message,
                Id = id

            }
            , JsonRequestBehavior.AllowGet);
        }
    }
}