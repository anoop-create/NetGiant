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

        public DashboardViewModel(int tenantId, int channelId)
        {
            Brands = SharedViewModel.GetBrandList(channelId);
            PriceRules = SharedViewModel.GetPriceRuleList(channelId, false, false);
            ProductGroups = new List<SelectListItem>()
            {
                new SelectListItem() {Text="Key Lines", Value="1" }
            };

            //GetFilteredData(channelId, 0, 0, 0, 0);
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
                    Counters.ProductInventoryCount = Int32.Parse(dr["ProductInventoryCount"].ToString());
                    Counters.SupplierInventoryCount = Int32.Parse(dr["SupplierInventoryCount"].ToString());
                    Counters.CompetitorInventoryCount = Int32.Parse(dr["CompetitorInventoryCount"].ToString());
                    Counters.PriceRuleCount = Int32.Parse(dr["PriceRuleCount"].ToString());
                    Counters.ProductRuleCount = Int32.Parse(dr["ProductRuleCount"].ToString());
                    Counters.SupplierExceptionsCount = Int32.Parse(dr["SupplierExceptionsCount"].ToString());
                    Counters.CompetitorExceptionsCount = Int32.Parse(dr["CompetitorExceptionsCount"].ToString());
                    Counters.ManualMappingsCount = Int32.Parse(dr["ManualMappingsCount"].ToString());
                }
            }
            else
            {
                dt = ds.Tables[0];

                var gdt = new go.DataTable();
                gdt.AddColumn(new go.Column(go.ColumnType.String, "Range", "Range"));
                gdt.AddColumn(new go.Column(go.ColumnType.Number, "Count", "Count"));
                var gcol = new go.Column(go.ColumnType.String);
                gcol.Role = go.ColumnRole.Tooltip;
                gcol.Id = "Tooltip";
                gdt.AddColumn(gcol);
                gcol = new go.Column(go.ColumnType.String);
                gcol.Role = go.ColumnRole.Annotation;
                gcol.Id = "Annotation";
                gdt.AddColumn(gcol);
                ProductCountMD = 0;
                foreach (DataRow dr in dt.Rows)
                {
                    if (dr["d_Start"].ToString() != "")
                    {
                        go.Row gr = gdt.NewRow();
                        gr.AddCellRange(new go.Cell[]
                        {
                    new go.Cell(dr["d_Start"].ToString() + " - " + dr["d_End"].ToString()),
                    new go.Cell(Int32.Parse(dr["d_Count"].ToString())),
                    new go.Cell("There are " + Int32.Parse(dr["d_Count"].ToString()).ToString("N0") + " products with a gross margin greater than or equal to " + dr["d_Start"].ToString() + "% and less than " + dr["d_End"].ToString() + "%, Click here to view these products."),
                    new go.Cell(dr["d_Count"].ToString())
                        });
                        gdt.AddRow(gr);

                        ProductCountMD += Int32.Parse(dr["d_Count"].ToString());
                    }
                }
                MarginDistribution = gdt.GetJson();

                dt = ds.Tables[1];
                int pricingAnalysisTotal = 0;

                gdt = new go.DataTable();
                gdt.AddColumn(new go.Column(go.ColumnType.String, "Inventory", "Inventory"));
                gdt.AddColumn(new go.Column(go.ColumnType.Number, "Count", "Count"));
                gcol = new go.Column(go.ColumnType.String);
                gcol.Role = go.ColumnRole.Tooltip;
                gcol.Id = "Tooltip";
                gdt.AddColumn(gcol);
                ProductCountPA = 0;

                foreach (DataRow dr in dt.Rows)
                {
                    go.Row gr = gdt.NewRow();
                    gr.AddCellRange(new go.Cell[]
                    {
                    new go.Cell(dr["Reason"].ToString()),
                    new go.Cell(Int32.Parse(dr["Total"].ToString())),
                    new go.Cell(dr["LongDesc"].ToString())
                    });
                    gdt.AddRow(gr);

                    ProductCountPA += Int32.Parse(dr["Total"].ToString());
                    pricingAnalysisTotal += Int32.Parse(dr["Total"].ToString());
                }
                PricingAnalysis = gdt.GetJson();

                if (pricingAnalysisTotal == 0)
                {
                    pricingAnalysisTotal = 1;
                }
            }

            return this;
        }

        public Dash1 Counters { get; set; }
        public string PricingAnalysis { get; set; }
        public string MarginDistribution { get; set; }
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
            public int SupplierExceptionsCount { get; set; }
            public int CompetitorExceptionsCount { get; set; }
            public int ManualMappingsCount { get; set; }
        }

    }
}
