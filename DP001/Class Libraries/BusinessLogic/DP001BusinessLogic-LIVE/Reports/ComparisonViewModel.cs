using DP001DataAccess.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
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
        public IQueryable<CrudReports.ProductInventoryDisplayModel> ProductsDM { get; set; }
        public bool UsesVariantOf { get; set; }

        private int _channelId;
        private DP001Entities _ctx;

        public ReportsViewModel Get(bool? hasRuleName = null)
        {
            var crud = new CrudReports();

            switch (hasRuleName)
            {
                case null:
                    ProductsDM = crud.ReadProductsQuery(
                    x => x.Pi.ChannelFK == _channelId &&
                    x.Pi.Lookup1.LookupName == "Active",
                    _ctx);
                    break;
                case true:
                    ProductsDM = crud.ReadProductsQuery(
                    x => x.Pi.ChannelFK == _channelId && x.Pi.PriceRuleFK > 0 &&
                    x.Pi.Lookup1.LookupName == "Active",
                    _ctx);
                    break;
                case false:
                    ProductsDM = crud.ReadProductsQuery(
                    x => x.Pi.ChannelFK == _channelId && x.Pi.PriceRuleFK == null &&
                    x.Pi.Lookup1.LookupName == "Active",
                    _ctx);
                    break;
            }

            return this;
        }

        public ReportsViewModel GetStagingPriceComparison()
        {
            var crud = new CrudReports();

            ProductsDM = crud.ReadProductsStagingPricesQuery(
                x => x.Pi.ChannelFK == _channelId &&
                    x.Pi.Lookup1.LookupName == "Active",
                _ctx);

            return this;
        }

        public Stream CreateExportFile()
        {
            var data = ProductsDM.Select(x => new
            {
                Part_Number = x.Pi.ManufacturerPartNo,
                Client_Product_ID = x.Pi.ClientProductID,
                Description = x.Pi.Description,
                Variant_Of = x.Pi.VariantOf,
                Category = x.Pi.ProductCategory.CategoryName,
                Brand = x.Pi.Brand.BrandName,
                Cheapest_Cost_Price = x.Pi.CheapestCostPrice.ToString(),
                Cheapest_Competitor_Price = x.Pi.CheapestCompetitorPrice.ToString(),
                Price = x.Pi.Price.ToString(),
                Competitor_Difference = x.Pi.CompetitorDifference.ToString(),
                Gross_Margin_Percent = x.Pi.GrossMarginPercent.ToString(),
                Gross_Margin_Value = x.Pi.GrossMarginValue.ToString(),
                Calculation_Outcome = x.Pi.Lookup.LookupName,
                Price_Rule = x.Pi.PriceRule.RuleName,
                Stock = x.Pi.StockQuantity.ToString()
            }).ToList();

            return ExportUtilities.ExportToSpreadsheet(data, true);
        }

        public Stream CreateExportFileTestRules()
        {
            var data = ProductsDM.Select(x => new
            {
                Part_Number = x.Pi.ManufacturerPartNo,
                Client_Product_ID = x.Pi.ClientProductID,
                Description = x.Pi.Description,
                Variant_Of = x.Pi.VariantOf,
                Category = x.Pi.ProductCategory.CategoryName,
                Brand = x.Pi.Brand.BrandName,
                Current_Price = x.Pi.Price.ToString(),
                New_Price = x.Pi.PriceStaging != null ? x.Pi.PriceStaging.Price.ToString() : "",
                Price_Difference = x.Pi.PriceStaging != null ? x.Pi.PriceStaging.CurrentPriceDifference.ToString() : "",
                Calculation_Outcome = x.Pi.PriceStaging != null ? x.Pi.PriceStaging.Lookup.LookupName : "",
                Price_Rule = x.Pi.PriceStaging != null ? x.Pi.PriceStaging.PriceRule.RuleName : ""
            }).ToList();

            return ExportUtilities.ExportToSpreadsheet(data, true);
        }

        public ReportsViewModel GetKeyLinesInventory()
        {
            var crud = new CrudReports();

            ProductsDM = crud.ReadProductsQuery(
                x => x.Pi.ChannelFK == _channelId && x.Pi.IsKeyLine == true && x.Pi.Lookup1.LookupName == "Active",
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
