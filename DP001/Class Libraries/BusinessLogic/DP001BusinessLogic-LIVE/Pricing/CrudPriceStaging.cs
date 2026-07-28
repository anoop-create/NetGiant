using DP001DataAccess.Entities;
using DP001DataAccess.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DP001BusinessLogic
{
    public class CrudPriceStaging
    {
        public void Create(List<PriceStaging> stagingPrices)
        {
            var dt = new DataTable("PriceStaging");

            using (var db = new DP001Entities())
            {
                dt.Columns.Add(new DataColumn("ProductInventoryFK", typeof(long)));
                dt.Columns.Add(new DataColumn("ChannelFK", typeof(int)));
                dt.Columns.Add(new DataColumn("Price", typeof(decimal)));
                dt.Columns.Add(new DataColumn("AltPrice1", typeof(decimal)));
                dt.Columns.Add(new DataColumn("AltPrice2", typeof(decimal)));
                dt.Columns.Add(new DataColumn("AltPrice3", typeof(decimal)));
                dt.Columns.Add(new DataColumn("AltPrice4", typeof(decimal)));
                dt.Columns.Add(new DataColumn("AltPrice5", typeof(decimal)));
                dt.Columns.Add(new DataColumn("AltPrice6", typeof(decimal)));
                dt.Columns.Add(new DataColumn("AltPrice7", typeof(decimal)));
                dt.Columns.Add(new DataColumn("AltPrice8", typeof(decimal)));
                dt.Columns.Add(new DataColumn("AltPrice9", typeof(decimal)));
                dt.Columns.Add(new DataColumn("AltPrice10", typeof(decimal)));
                dt.Columns.Add(new DataColumn("PriceRuleFK", typeof(int)));
                dt.Columns.Add(new DataColumn("CalculationOutcome", typeof(int)));
                dt.Columns.Add(new DataColumn("BeatRateNumber", typeof(int)));
                dt.Columns.Add(new DataColumn("StockQuantity", typeof(int)));
                dt.Columns.Add(new DataColumn("CheapestCostPrice", typeof(decimal)));
                dt.Columns.Add(new DataColumn("CheapestCompetitorPrice", typeof(decimal)));
                dt.Columns.Add(new DataColumn("GrossMarginPercent", typeof(decimal)));
                dt.Columns.Add(new DataColumn("GrossMarginValue", typeof(decimal)));
                dt.Columns.Add(new DataColumn("CompetitorDifference", typeof(decimal)));
                dt.Columns.Add(new DataColumn("CurrentPriceDifference", typeof(decimal)));
                dt.Columns.Add(new DataColumn("BeatenCompetitorPrice", typeof(decimal)));

                foreach (var sp in stagingPrices)
                {
                    dt.Rows.Add(sp.ProductInventoryFK, sp.ChannelFK, sp.Price, sp.AltPrice1, sp.AltPrice2, sp.AltPrice3, sp.AltPrice4, sp.AltPrice5,
                         sp.AltPrice6, sp.AltPrice7, sp.AltPrice8, sp.AltPrice9, sp.AltPrice10, sp.PriceRuleFK,
                         sp.CalculationOutcome, sp.BeatRateNumber, sp.StockQuantity, sp.CheapestCostPrice,
                         sp.CheapestCompetitorPrice, sp.GrossMarginPercent, sp.GrossMarginValue, sp.CompetitorDifference,
                         sp.CurrentPriceDifference, sp.BeatenCompetitorPrice);
                }

                SQL.SQLBulkInsert(dt, "DP001");
            }
        }
    }
}
