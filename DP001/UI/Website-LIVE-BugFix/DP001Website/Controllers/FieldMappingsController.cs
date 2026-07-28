using DP001BusinessLogic.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DP001Website.Controllers
{
    [Authorize]
    public class FieldMappingsController : ApplicationController
    {
        public ActionResult Index(int id)
        {
            int channelId = GetChannelId();
            var model = new FieldMappingsViewModel(channelId);
            model.GetFieldMappings(id);

            return View(model);
        }

        public ActionResult Edit(int id)
        {
            int channelId = GetChannelId();
            var model = new FieldMappingsViewModel(channelId);
            model.GetFieldMappings(id);

            return View(model);
        }


        [HttpPost]
        [Authorize(Roles = "Administrator")]
        [ValidateAntiForgeryToken]
        public ActionResult Update(FieldMappingsViewModel model)
        {
            model.FtpSetting.ChannelFK = GetChannelId();
            model.Update();
            return RedirectToAction("Index", new { id = model.FtpSetting.FTPSettingsID });
        }
    }
}