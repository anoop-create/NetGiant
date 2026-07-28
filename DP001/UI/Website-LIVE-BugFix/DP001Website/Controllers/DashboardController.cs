using DP001BusinessLogic.ViewModels;
using DP001DataAccess.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DP001Website.Controllers
{
    [Authorize]
    public class DashboardController : ApplicationController
    {
        public ActionResult Index()
        {
            var tenant = GetTenant();
            int channelId = Int32.Parse(Session["CurrentChannel"].ToString());
            DashboardViewModel model = new DashboardViewModel(tenant.TenantID, channelId);


            ScheduleViewModel schModel = new ScheduleViewModel(channelId);
            schModel.Channel = tenant.Channels.Where(x => x.ChannelID == tenant.LastUsedChannelId).FirstOrDefault();
            schModel.GetSchedules();
            schModel.FindNextRun();
            model.NextRunDate = schModel.NextRunDate;

            LogViewModel logModel = new LogViewModel();
            model.LastRunDate = logModel.GetLastRunDate(channelId);

            return View(model);
        }
    }
}