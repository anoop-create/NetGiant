using DP001BusinessLogic.ViewModels;
using DP001DataAccess.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
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
            if (schModel.Channel == null)
            {
                schModel.Channel = tenant.Channels.Where(x => x.TenantFK == tenant.TenantID && x.IsActive).FirstOrDefault();
            }
            schModel.GetSchedules();
            schModel.FindNextRun();
            model.NextRunDate = schModel.NextRunDate;

            LogViewModel logModel = new LogViewModel();
            model.LastRunDate = logModel.GetLastRunDate(channelId);

            model.GetFilteredData(channelId, 0, 0, 0, 0, 0);

            return View(model);
        }

        public ActionResult TelerikChartData(int brand, int category, int pricerule, int productgroup, string chart)
        {
            var dt = new DataTable();

            var tenant = GetTenant();
            int channelId = int.Parse(Session["CurrentChannel"].ToString());
            DashboardViewModel model = new DashboardViewModel(tenant.TenantID, channelId);
            model.GetFilteredData(channelId, brand, category, pricerule, productgroup, 1);

            if (chart == "Margin")
            {
                //model.GetFilteredData(channelId, brand, category, pricerule, productgroup, 1);
                //Session["CompetitivenessDt"] = model.PriceCompetitivenessDt;

                dt = model.MarginDistributionDt;
                var dataList = new List<MarginDistribution>();

                foreach (DataRow dr in dt.Rows)
                {
                    dataList.Add(new MarginDistribution
                    {
                        Count = (int)dr["d_Count"],
                        Decile = (int)dr["d_Decile"],
                        Start = (decimal)dr["d_Start"],
                        End = (decimal)dr["d_End"],
                        CategoryLabel = dr["d_Start"] + " - " + dr["d_End"],
                        FormattedCount = ((int)dr["d_Count"]).ToString("N0")
                    });
                }

                return Json(dataList);
            }
            else
            {
                //dt = (DataTable)Session["CompetitivenessDt"];
                dt = model.PriceCompetitivenessDt;
                var dataList = new List<PriceCompetitiveness>();

                foreach (DataRow dr in dt.Rows)
                {
                    dataList.Add(new PriceCompetitiveness
                    {
                        Description = dr["Reason"].ToString(),
                        LongDescription = dr["LongDesc"].ToString(),
                        Total = (int)dr["Total"]
                    });
                }

                return Json(dataList);
            }
        }
    }

    public class PriceCompetitiveness
    {
        public string Description { get; set; }
        public string LongDescription { get; set; }
        public int Total { get; set; }
    }

    public class MarginDistribution
    {
        public int Count { get; set; }
        public int Decile { get; set; }
        public decimal Start { get; set; }
        public decimal End { get; set; }
        public string CategoryLabel { get; set; }
        public string FormattedCount { get; set; }
    }
}

