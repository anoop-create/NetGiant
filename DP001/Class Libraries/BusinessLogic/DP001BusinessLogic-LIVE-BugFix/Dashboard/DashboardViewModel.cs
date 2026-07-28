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

namespace DP001BusinessLogic.ViewModels
{
    public class DashboardViewModel
    {
        public DashboardViewModel()
        {

        }

        public DashboardViewModel(int tenantId, int channelId)
        {
            DataSet ds = new DataSet("dashboards");
            DataTable dt = new DataTable();

            List<SqlParameter> parms = new List<SqlParameter>();
            SqlParameter parm1 = new SqlParameter("@ChannelID", SqlDbType.Int);
            parm1.Value = channelId;
            parms.Add(parm1);

            ds = SQL.ExecuteReadStoredProcedure("DP001", "GetDashboardData", parms, "dashboards");
            dt = ds.Tables[0];
            Counters = new Dash1();
            foreach (DataRow dr in dt.Rows)
            {
                Counters.ProductInventoryCount = Int32.Parse(dr["ProductInventoryCount"].ToString());
                Counters.SupplierInventoryCount = Int32.Parse(dr["SupplierInventoryCount"].ToString());
                Counters.CompetitorInventoryCount = Int32.Parse(dr["CompetitorInventoryCount"].ToString());
                Counters.PriceRuleCount = Int32.Parse(dr["PriceRuleCount"].ToString());
                Counters.ProductRuleCount = Int32.Parse(dr["ProductRuleCount"].ToString());
                Counters.SupplierExceptionsCount = Int32.Parse(dr["SupplierExceptionsCount"].ToString());
                Counters.CompetitorExceptionsCount = Int32.Parse(dr["CompetitorExceptionsCount"].ToString());
                Counters.ManualMappingsCount = Int32.Parse(dr["ManualMappingsCount"].ToString());

                int pricingAnalysisTotal = Int32.Parse(dr["PricingNudgeUpCount"].ToString()) + Int32.Parse(dr["PricingNudgeDownCount"].ToString()) + Int32.Parse(dr["PricingDesiredCount"].ToString()) + Int32.Parse(dr["PricingTooLowCount"].ToString()) + Int32.Parse(dr["PricingTooHighCount"].ToString());
                if (pricingAnalysisTotal == 0)
                {
                    pricingAnalysisTotal = 1;
                }
                PricingAnalysisList = new List<Tuple<string, string, int>>();
                PricingAnalysisList.Add(new Tuple<string, string, int>("Above desired margin", "Products where your prices are being calculated above the desired margin.", Int32.Parse(dr["PricingNudgeUpCount"].ToString())));
                PricingAnalysisList.Add(new Tuple<string, string, int>("Below desired margin", "Products where your prices are being calculated below the desired margin.", Int32.Parse(dr["PricingNudgeDownCount"].ToString())));
                PricingAnalysisList.Add(new Tuple<string, string, int>("At desired margin", "Products where your prices are being calculated at the desired margin.", Int32.Parse(dr["PricingDesiredCount"].ToString())));
                PricingAnalysisList.Add(new Tuple<string, string, int>("Margin Opportunity", "Products where your maximum margin percentage is reached.", Int32.Parse(dr["PricingTooLowCount"].ToString())));
                PricingAnalysisList.Add(new Tuple<string, string, int>("Sales Opportunity", "Products where your minimum margin percentage is reached.", Int32.Parse(dr["PricingTooHighCount"].ToString())));


                int pricingAnalysisTotalKey = Int32.Parse(dr["PricingNudgeUpCountKey"].ToString()) + Int32.Parse(dr["PricingNudgeDownCountKey"].ToString()) + Int32.Parse(dr["PricingDesiredCountKey"].ToString()) + Int32.Parse(dr["PricingTooLowCountKey"].ToString()) + Int32.Parse(dr["PricingTooHighCountKey"].ToString());
                if (pricingAnalysisTotalKey == 0)
                {
                    pricingAnalysisTotalKey = 1;
                }
                PricingAnalysisListKey = new List<Tuple<string, string, int>>();
                PricingAnalysisListKey.Add(new Tuple<string, string, int>("Above desired margin", "Products where your prices are being calculated above the desired margin.", Int32.Parse(dr["PricingNudgeUpCountKey"].ToString())));
                PricingAnalysisListKey.Add(new Tuple<string, string, int>("Below desired margin", "Products where your prices are being calculated below the desired margin.", Int32.Parse(dr["PricingNudgeDownCountKey"].ToString())));
                PricingAnalysisListKey.Add(new Tuple<string, string, int>("At desired margin", "Products where your prices are being calculated at the desired margin.", Int32.Parse(dr["PricingDesiredCountKey"].ToString())));
                PricingAnalysisListKey.Add(new Tuple<string, string, int>("Margin Opportunity", "Products where your maximum margin percentage is reached.", Int32.Parse(dr["PricingTooLowCountKey"].ToString())));
                PricingAnalysisListKey.Add(new Tuple<string, string, int>("Sales Opportunity", "Products where your minimum margin percentage is reached.", Int32.Parse(dr["PricingTooHighCountKey"].ToString())));


                //Counters.PricingInRangeCount = Int32.Parse(dr["PricingInRangeCount"].ToString());
                //Counters.PricingTooLowCount = Int32.Parse(dr["PricingTooLowCount"].ToString());
                //Counters.PricingTooHighCount = Int32.Parse(dr["PricingTooHighCount"].ToString());
            }

            dt = ds.Tables[1];
            MarginDistributionList = new List<Tuple<string, int, string>>();
            foreach (DataRow dr in dt.Rows)
            {
                if (dr["d_Start"].ToString() != "")
                {
                    MarginDistributionList.Add(new Tuple<string, int, string>(dr["d_Start"].ToString() + " - " + dr["d_End"].ToString(), Int32.Parse(dr["d_Count"].ToString()), "There are " + Int32.Parse(dr["d_Count"].ToString()).ToString("N0") + " products with a gross margin greater than or equal to " + dr["d_Start"].ToString() + "% and less than " + dr["d_End"].ToString() + "%, Click here to view these products."));
                }
            }

            //dt = ds.Tables[2];
            //CategoryRankList = new List<Tuple<string, int, int>>();
            //foreach (DataRow dr in dt.Rows)
            //{
            //    CategoryRankList.Add(new Tuple<string, int, int>(dr["CategoryName"].ToString(), Int32.Parse(dr["Percentage"].ToString()), Int32.Parse(dr["Count"].ToString())));
            //}

            //dt = ds.Tables[3];
            //BrandRankList = new List<Tuple<string, int, int>>();
            //foreach (DataRow dr in dt.Rows)
            //{
            //    BrandRankList.Add(new Tuple<string, int, int>(dr["BrandName"].ToString(), Int32.Parse(dr["Percentage"].ToString()), Int32.Parse(dr["Count"].ToString())));
            //}

        }

        public Dash1 Counters { get; set; }
        public List<Tuple<string, int, int>> CategoryRankList { get; set; }
        public List<Tuple<string, int, int>> BrandRankList { get; set; }
        public List<Tuple<string, int, string>> MarginDistributionList { get; set; }
        public List<Tuple<string, string, int>> PricingAnalysisList { get; set; }
        public List<Tuple<string, string, int>> PricingAnalysisListKey { get; set; }
        public string LastRunDate { get; set; }
        public string NextRunDate { get; set; }

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
            public int PricingInRangeCount { get; set; }
            public int PricingTooLowCount { get; set; }
            public int PricingTooHighCount { get; set; }
            public int SupplierExceptionsCount { get; set; }
            public int CompetitorExceptionsCount { get; set; }
            public int ManualMappingsCount { get; set; }
        }

        //public class Dash2
        //{
        //    public Dash2()
        //    {
        //        //CategoryRankList = new Dictionary<string, int>();
        //        CategoryRankList = new List<Tuple<string, int, int>>();
        //    }

        //    public List<Tuple<string, int, int>> CategoryRankList { get; set; }
        //}

        public class Dash3
        {
        }
    }
}
