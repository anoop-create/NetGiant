using DP001DataAccess.Entities;
using DP001DataAccess.Utilities;
using DP001BusinessLogic.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Data;
using System.Data.SqlClient;
using go = Google.DataTable.Net.Wrapper;

namespace DP001BusinessLogic.ViewModels
{
    public class DashboardViewModel
    {
        public DashboardViewModel()
        {

        }

        public List<SelectListItem> Categories { get; set; }
        public List<SelectListItem> Brands { get; set; }
        public List<SelectListItem> PriceRules { get; set; }
        public List<SelectListItem> ProductGroups { get; set; }
        public int ProductCountMD { get; set; }
        public int ProductCountPA { get; set; }
        public int ProductCountCA { get; set; }
        public Dash1 Counters { get; set; }
        public string PricingAnalysis { get; set; }
        public string CompetitorAnalysis { get; set; }
        public string MarginDistribution { get; set; }
        public string LastRunDate { get; set; }
        public string NextRunDate { get; set; }
        public DataTable PriceCompetitivenessDt { get; set; }
        public DataTable MarginDistributionDt { get; set; }

        public DashboardViewModel(int tenantId, int channelId)
        {
            Brands = SharedViewModel.GetBrandList(channelId);
            PriceRules = SharedViewModel.GetPriceRuleList(channelId, false, false);
            ProductGroups = new List<SelectListItem>()
            {
                new SelectListItem() {Text="Key Lines", Value="1" }
            };
        }

        public DashboardViewModel GetFilteredData(int channelId, int brand, int category, int pricerule, int productgroup, int getchartdata)
        {
            Categories = SharedViewModel.GetCategoryList(channelId, brand, true);
            Categories[0].Text = "All Categories";

            DataSet ds = new DataSet("dashboards");
            DataTable dt = new DataTable();

            List<SqlParameter> parms = new List<SqlParameter>();
            SqlParameter parm1 = new SqlParameter("@ChannelID", SqlDbType.Int);
            parm1.Value = channelId;
            parms.Add(parm1);
            SqlParameter parm2 = new SqlParameter("@BrandID", SqlDbType.Int);
            parm2.Value = brand;
            parms.Add(parm2);
            SqlParameter parm3 = new SqlParameter("@ProductCategoryID", SqlDbType.Int);
            parm3.Value = category;
            parms.Add(parm3);
            SqlParameter parm4 = new SqlParameter("@PriceRuleID", SqlDbType.Int);
            parm4.Value = pricerule;
            parms.Add(parm4);
            SqlParameter parm5 = new SqlParameter("@IsKeyLine", SqlDbType.Bit);
            parm5.Value = productgroup;
            parms.Add(parm5);
            SqlParameter parm6 = new SqlParameter("@GetChartData", SqlDbType.Bit);
            parm6.Value = getchartdata;
            parms.Add(parm6);

            ds = SQL.ExecuteReadStoredProcedure("DP001", "GetDashboardData", parms, "dashboards");

            if (getchartdata == 0)
            {
                dt = ds.Tables[0];
                Counters = new Dash1();
                foreach (DataRow dr in dt.Rows)
                {
                    Counters.ProductInventoryCount = int.Parse(dr["ProductInventoryCount"].ToString());
                    Counters.SupplierInventoryCount = int.Parse(dr["SupplierInventoryCount"].ToString());
                    Counters.CompetitorInventoryCount = int.Parse(dr["CompetitorInventoryCount"].ToString());
                    Counters.PriceRuleCount = int.Parse(dr["PriceRuleCount"].ToString());
                    Counters.ProductRuleCount = int.Parse(dr["ProductRuleCount"].ToString());
                    Counters.SupplierExceptionsCount = int.Parse(dr["SupplierExceptionsCount"].ToString());
                    Counters.CompetitorExceptionsCount = int.Parse(dr["CompetitorExceptionsCount"].ToString());
                    Counters.ManualMappingsCount = int.Parse(dr["ManualMappingsCount"].ToString());
                }
            }
            else
            {
                MarginDistributionDt = ds.Tables[0];
                PriceCompetitivenessDt = ds.Tables[1];
            }

            return this;
        }
        
        public DashboardViewModel GetDashboardData()
        {
            return this;
        }

        public class Dash1
        {
            public int ProductInventoryCount { get; set; }
            public int SupplierInventoryCount { get; set; }
            public int CompetitorInventoryCount { get; set; }
            public int PriceRuleCount { get; set; }
            public int ProductRuleCount { get; set; }
            public int SupplierExceptionsCount { get; set; }
            public int CompetitorExceptionsCount { get; set; }
            public int ManualMappingsCount { get; set; }
        }

    }
}
