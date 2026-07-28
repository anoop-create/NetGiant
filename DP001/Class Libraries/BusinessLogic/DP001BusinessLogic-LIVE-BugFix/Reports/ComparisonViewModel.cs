using DP001DataAccess.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using MoreLinq;
using DP001BusinessLogic.Shared;
using System.IO;

namespace DP001BusinessLogic.ViewModels
{
    public class ReportsViewModel
    {
        public ReportsViewModel(int channelId)
        {
            _channelId = channelId;
            _ctx = new DP001Entities();
        }

        public ReportsViewModel()
        {
        }
        
        public IQueryable<ProductInventory> Products { get; set; }
        private int _channelId;
        private DP001Entities _ctx;

        public ReportsViewModel Get(bool? hasRuleName = null)
        {
            var crud = new CrudReports();

            switch (hasRuleName)
            {
                case null:
                    Products = crud.ReadProductsQuery(
                    x => x.ChannelFK == _channelId,
                    _ctx);
                    break;
                case true:
                    Products = crud.ReadProductsQuery(
                    x => x.ChannelFK == _channelId && x.PriceRuleFK > 0,
                    _ctx);
                    break;
                case false:
                    Products = crud.ReadProductsQuery(
                    x => x.ChannelFK == _channelId && x.PriceRuleFK == null,
                    _ctx);
                    break;
            }

            return this;
        }

        public ReportsViewModel GetStagingPriceComparison()
        {
            var crud = new CrudReports();

            Products = crud.ReadProductsStagingPricesQuery(
                x => x.ChannelFK == _channelId,
                _ctx);

            return this;
        }

        public Stream CreateExportFile()
        {
            var data = Products.Select(x => new
            {
                Part_Number = x.ManufacturerPartNo,
                Description = x.Description,
                Brand = x.Brand.BrandName,
                Client_Product_ID = x.ClientProductID,
                Category = x.ProductCategory.CategoryName,
                Price = x.Price.ToString(),
                Price_Rule = x.PriceRule.RuleName,
                Calculation_Outcome = x.Lookup.LookupName,
                Stock = x.StockQuantity.ToString(),
                Cheapest_Cost_Price = x.CheapestCostPrice.ToString(),
                Cheapest_Competitor_Price = x.CheapestCompetitorPrice.ToString(),
                Gross_Margin_Percent = x.GrossMarginPercent.ToString(),
                Gross_Margin_Value = x.GrossMarginValue.ToString(),
                Competitor_Difference = x.CompetitorDifference.ToString()
            }).ToList();

            return ExportUtilities.ExportToSpreadsheet(data, true);
        }

        public Stream CreateExportFileTestRules()
        {
            var data = Products.Select(x => new
            {
                Part_Number = x.ManufacturerPartNo,
                Description = x.Description,
                Brand = x.Brand.BrandName,
                Client_Product_ID = x.ClientProductID,
                Category = x.ProductCategory.CategoryName,
                Current_Price = x.Price.ToString(),
                New_Price = x.PriceStaging != null ? x.PriceStaging.Price.ToString() : "",
                Price_Difference = x.PriceStaging != null ? x.PriceStaging.CurrentPriceDifference.ToString() : "",
                Calculation_Outcome = x.PriceStaging != null ? x.PriceStaging.Lookup.LookupName : "",
                Price_Rule = x.PriceStaging != null ? x.PriceStaging.PriceRule.RuleName : ""
            }).ToList();

            return ExportUtilities.ExportToSpreadsheet(data, true);
        }

        public ReportsViewModel GetKeyLinesInventory()
        {
            var crud = new CrudReports();

            Products = crud.ReadProductsQuery(
                x => x.ChannelFK == _channelId && x.IsKeyLine == true,
                _ctx);

            return this;
        }

        public void Dispose()
        {
            _ctx.Dispose();
        }

        public static implicit operator ReportsViewModel(ProductViewModel v)
        {
            throw new NotImplementedException();
        }
    }
}
