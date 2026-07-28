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
    public class CustomFieldController : ApplicationController
    {
        public ActionResult Index()
        {
            var tenant = GetTenant();
            int channelId = GetChannelId();
            var model = new CustomFieldViewModel(channelId);
            model.GetCustomFields();

            return View(model);
        }

        public ActionResult Edit(int id)
        {
            int channelId = GetChannelId();
            var model = new CustomFieldViewModel(channelId);
            model.Edit(id);

            if (model.CustomFieldEntry != null)
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
        public JsonResult Update(CustomFieldViewModel model)
        {
            model.CustomFieldEntry.ChannelFK = GetChannelId();
            model.Channel = GetChannel();
            model.Tenant = GetTenant();

            var saveReturn = model.Update(model.CustomFieldEntry);
            if (saveReturn.IsSuccess)
            {
                CommonModel cm = new CommonModel();
                cm.RefreshTenantSession();

                string pv = RenderPartialViewToString("~/Views/CustomField/IndexRow.cshtml", model);
                return Json(new { isSuccess = true, id = model.CustomFieldEntry.CustomFieldID, action = "Save", html = pv, msg = "" }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new { isSuccess = false, id = model.CustomFieldEntry.CustomFieldID, action = "Save", msg = saveReturn.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult New()
        {
            int channelId = GetChannelId();
            var model = new CustomFieldViewModel(channelId);
            model.New();

            return PartialView(model);
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        [ValidateAntiForgeryToken]
        [MultipleButton(Name = "action", Argument = "Save")]
        public JsonResult Create(CustomFieldViewModel model)
        {
            model.CustomFieldEntry.ChannelFK = GetChannelId();
            model.Channel = GetChannel();
            model.Tenant = GetTenant();

            var saveReturn = model.Create();
            if (saveReturn.IsSuccess)
            {
                CommonModel cm = new CommonModel();
                cm.RefreshTenantSession();

                int channelId = GetChannelId();
                var savedModel = new CustomFieldViewModel(channelId);
                model = savedModel.Edit(model.CustomFieldEntry.CustomFieldID);
                string pv = RenderPartialViewToString("~/Views/CustomField/IndexRow.cshtml", model);
                pv = "<tr class=\"list-form-row\" data-id=\"" + model.CustomFieldEntry.CustomFieldID + "\">" + pv + "</tr>";
                return Json(new { isSuccess = true, id = model.CustomFieldEntry.CustomFieldID, action = "Save", html = pv, msg = "" }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new { isSuccess = false, id = model.CustomFieldEntry.CustomFieldID, action = "Save", msg = saveReturn.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpDelete]
        [Authorize(Roles = "Administrator")]
        public JsonResult Delete(int id)
        {
            int channelId = GetChannelId();
            var model = new CustomFieldViewModel(channelId);
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