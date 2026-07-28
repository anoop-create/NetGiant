using DP001BusinessLogic.ViewModels;
using DP001DataAccess.Utilities;
using Newtonsoft.Json;
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

            model.GetFilteredData(channelId, 0, 0, 0, 0, 0);

            return View(model);
        }

        [HttpPost]
        public JsonResult GetFilteredCharts(int brand, int category, int pricerule, int productgroup)
        {
            var tenant = GetTenant();
            int channelId = Int32.Parse(Session["CurrentChannel"].ToString());
            DashboardViewModel model = new DashboardViewModel(tenant.TenantID, channelId);

            model.GetFilteredData(channelId, brand, category, pricerule, productgroup, 1);

            return Json(new {
                brands = model.Brands,
                categories = model.Categories,
                pricerules = model.PriceRules,
                marginDistribution = model.MarginDistribution,
                pricingAnalysis = model.PricingAnalysis, 
                productCountMd = model.ProductCountMD.ToString("N0"),
                productCountPa = model.ProductCountPA.ToString("N0")
            }, JsonRequestBehavior.AllowGet);

        }
    }
}