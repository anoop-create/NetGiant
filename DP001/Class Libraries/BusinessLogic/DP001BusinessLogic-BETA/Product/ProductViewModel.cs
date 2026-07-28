using DP001BusinessLogic.Shared;
using DP001DataAccess.Entities;
using DP001DataAccess.Utilities;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Mvc;

namespace DP001BusinessLogic.ViewModels
{
    public class ProductViewModel
    {
        public ProductViewModel(int channelId)
        {
            _channelId = channelId;
            _ctx = new DP001Entities();
        }

        public IQueryable<Telerik> InventoryList { get; set; }
        public List<ProductInventory> SearchResults { get; set; }
        public List<PriceHistory> PriceHistory { get; set; }
        public List<CustomField> CustomFieldList { get; set; }
        public List<CustomField> CustomAdjustmentList { get; set; }
        public Channel Channel { get; set; }
        public ProductInventory ProductEntry { get; set; }
        public int SuppplierSuggestions { get; set; }
        public int CompetitorSuggestions { get; set; }
        public ProductInventory VariantOf { get; set; }
        public ReportConfiguration ReportConfiguration { get; set; }
        public List<SelectListItem> ReportSecurityList { get; set; }
        public List<SharedViewModel.SelectListItemExtended> ReportConfigList { get; set; }
        public int? RequestedReportTenantId { get; set; }
        public bool UserCanModify { get; set; }

        private int _channelId;
        private DP001Entities _ctx;

        public ProductViewModel InitializeReport(int? reportConfigId, string userId, int? tenantFk)
        {
            ReportSecurityList = SharedViewModel.GetLookupList("ReportSecurity");
            ReportConfigList = SharedViewModel.GetReportConfigSelectList(tenantFk, userId, "Product");

            if (reportConfigId != null)
            {
                var config = CrudReportConfiguration.Read(x => x.ReportConfigurationId == reportConfigId).FirstOrDefault();

                if (config != null)
                {
                    RequestedReportTenantId = config.TenantFk;

                    if (config.Lookup.LookupName == "Private")
                    {
                        if (config.UserId != userId)
                            return this;
                    }

                    if (config.Lookup.LookupName == "Shared")
                    {
                        if (config.TenantFk != tenantFk)
                            return this;
                    }

                    ReportConfiguration = config;

                    if (ReportConfiguration.UserId == userId)
                        UserCanModify = true;
                }
            }

            return this;
        }

        public ProductViewModel GetInventory()
        {
            var crud = new CrudProductInventory();
            InventoryList = crud.ReadProductsQuery(x => x.ChannelFK == _channelId && x.Lookup1.LookupName == "Active", _ctx).AsTelerikViewModel();

            return this;
        }

        public ProductViewModel Search(string term, int brandFK)
        {
            var crud = new CrudProductInventory();
            if (brandFK > 0)
            {
                SearchResults = crud.Read(x =>
                    x.ChannelFK == _channelId &&
                    (x.ManufacturerPartNo.Contains(term) ||
                    x.Description.Contains(term)) &&
                    x.BrandFK == brandFK &&
                    x.Lookup1.LookupName == "Active", 20);
            }
            else
            {
                SearchResults = crud.Read(x =>
                    x.ChannelFK == _channelId &&
                    (x.ManufacturerPartNo.Contains(term) ||
                    x.Description.Contains(term)) &&
                    x.Lookup1.LookupName == "Active", 20);
            }

            return this;
        }

        public int ProductInventoryCount()
        {
            var crud = new CrudProductInventory();
            return crud.ReadCount(_channelId);
        }

        public ProductViewModel GetPriceHistory(int id, bool addCurrentPrice = false)
        {
            var prodCrud = new CrudProductInventory();
            var prod = prodCrud.Read(x => x.ProductInventoryID == id && x.ChannelFK == _channelId).FirstOrDefault();

            if (prod != null)
            {

                var clientProductID = prod.ClientProductID;

                if (!string.IsNullOrEmpty(clientProductID))
                {
                    var priceHistoryCrud = new CrudPriceHistory();
                    PriceHistory = priceHistoryCrud.Read(x => x.ChannelFK == _channelId && x.ClientProductID == clientProductID);
                }
                else
                {
                    PriceHistory = new List<PriceHistory>();
                }

                if (addCurrentPrice)
                {
                    PriceHistory.Add(new PriceHistory()
                    {
                        Price = prod.Price,
                        ChannelFK = prod.ChannelFK,
                        ClientProductID = clientProductID,
                        Date = prod.DatePriceChanged ?? new DateTime()
                    });
                }
            }
            else
            {
                throw new ApplicationException("Product not found or you do not have permission to view it's prices");
            }

            return this;
        }

        public ProductViewModel GetProductDetail(int productInventoryID)
        {
            var crudProduct = new CrudProductInventory();
            ProductEntry = crudProduct.Read(x => x.ProductInventoryID == productInventoryID && x.ChannelFK == _channelId).FirstOrDefault();

            if (ProductEntry != null)
            {
                GetPriceHistory(productInventoryID, true);

                if (ProductEntry.VariantOf != null)
                {
                    VariantOf = crudProduct.Read(x => x.ClientProductID == ProductEntry.VariantOf && x.ChannelFK == _channelId).FirstOrDefault();
                }

                var crudSkuMap = new CrudSkuMapping();

                if (ProductEntry.SupplierInventories.Count == 0)
                    SuppplierSuggestions = crudSkuMap.GetSuggestedSupplierMappings(ProductEntry).Count;

                if (ProductEntry.CompetitorInventories.Count == 0)
                    CompetitorSuggestions = crudSkuMap.GetSuggestedCompetitorMappings(ProductEntry).Count;

                ProductEntry.SupplierInventories = ProductEntry.SupplierInventories.OrderBy(x => x.Price).ToList();
                ProductEntry.CompetitorInventories = ProductEntry.CompetitorInventories.OrderBy(x => x.Price).ToList();
            }
            else
            {
                throw new ApplicationException("Product not found or you do not have permission to view it");
            }

            return this;
        }

        public void Dispose()
        {
            _ctx.Dispose();
        }

        public class Telerik
        {
            public long? ProductInventoryID { get; set; }
            public string ManufacturerPartNo { get; set; }
            public string ClientProductID { get; set; }
            public string Description { get; set; }
            public string CategoryName { get; set; }
            public string BrandName { get; set; }
            public int? StockQuantity { get; set; }
            public int? SupplierCount { get; set; }
            public int? CompetitorCount { get; set; }
            public decimal? CheapestCostPrice { get; set; }
            public decimal? PriceToBeat { get; set; }
            public decimal? TargetMarginPercent { get; set; }
            public decimal? Price { get; set; }
            public decimal? GrossMarginPercent { get; set; }
            public decimal? CompetitorDifference { get; set; }
            public string CalculationOutcome { get; set; }
            public string RuleName { get; set; }
            public int? PriceRuleId { get; set; }
            public decimal? AltPrice1 { get; set; }
            public decimal? AltPrice2 { get; set; }
            public decimal? AltPrice3 { get; set; }
            public decimal? AltPrice4 { get; set; }
            public decimal? AltPrice5 { get; set; }
            public decimal? AltPrice6 { get; set; }
            public decimal? AltPrice7 { get; set; }
            public decimal? AltPrice8 { get; set; }
            public decimal? AltPrice9 { get; set; }
            public decimal? AltPrice10 { get; set; }
            public decimal? CustomField1 { get; set; }
            public decimal? CustomField2 { get; set; }
            public decimal? CustomField3 { get; set; }
            public decimal? CustomField4 { get; set; }
            public decimal? CustomField5 { get; set; }
            public decimal? CustomField6 { get; set; }
            public decimal? CustomField7 { get; set; }
            public decimal? CustomField8 { get; set; }
            public decimal? CustomField9 { get; set; }
            public decimal? CustomField10 { get; set; }
            public string BandName { get; set; }
            public int? CompetitivePosition { get; set; }

            private DateTime? _dateLastUpdated;
            public DateTime? DateLastUpdated {
                get
                {
                    return _dateLastUpdated;
                }
                set
                {
                    if (value.HasValue)
                        _dateLastUpdated = CommonDataFunctions.GetGmtTime(value.Value).LocalDateTime;
                }
            }

            public string RuleNameAndBand { get; set; }
        }
    }

    public static class ProductExtensions
    {
        public static IQueryable<ProductViewModel.Telerik> AsTelerikViewModel(this IQueryable<ProductInventory> productQuery)
        {
            return productQuery.Select(o => new ProductViewModel.Telerik
            {
                ProductInventoryID = o.ProductInventoryID,
                ManufacturerPartNo = o.ManufacturerPartNo,
                ClientProductID = o.ClientProductID,
                Description = o.Description,
                CategoryName = o.ProductCategory.CategoryName,
                BrandName = o.Brand.BrandName,
                StockQuantity = o.StockQuantity,
                SupplierCount = o.SupplierInventories.Count,
                CompetitorCount = o.CompetitorInventories.Count,
                CheapestCostPrice = o.CheapestCostPrice,
                PriceToBeat = o.BeatenCompetitorPrice,
                TargetMarginPercent = o.TargetMarginPercent != null ? o.TargetMarginPercent : 0,
                Price = o.Price,
                GrossMarginPercent = o.GrossMarginPercent,
                CompetitorDifference = o.CompetitorDifference,
                CalculationOutcome = o.Lookup.LookupName,
                RuleName = o.PriceRule.RuleName,
                AltPrice1 = o.AltPrice1,
                AltPrice2 = o.AltPrice2,
                AltPrice3 = o.AltPrice3,
                AltPrice4 = o.AltPrice4,
                AltPrice5 = o.AltPrice5,
                AltPrice6 = o.AltPrice6,
                AltPrice7 = o.AltPrice7,
                AltPrice8 = o.AltPrice8,
                AltPrice9 = o.AltPrice9,
                AltPrice10 = o.AltPrice10,
                PriceRuleId = o.PriceRule.PriceRuleID,
                CustomField1 = o.CustomProductField1,
                CustomField2 = o.CustomProductField2,
                CustomField3 = o.CustomProductField3,
                CustomField4 = o.CustomProductField4,
                CustomField5 = o.CustomProductField5,
                CustomField6 = o.CustomProductField6,
                CustomField7 = o.CustomProductField7,
                CustomField8 = o.CustomProductField8,
                CustomField9 = o.CustomProductField9,
                CustomField10 = o.CustomProductField10,
                BandName = o.PriceRule.BandName,
                DateLastUpdated = o.DateLastUpdated,
                RuleNameAndBand = o.PriceRule != null ? o.PriceRule.RuleName != null ? o.PriceRule.RuleName + (!string.IsNullOrEmpty(o.PriceRule.BandName) ? " - " + o.PriceRule.BandName : "") : "" : "None",
                CompetitivePosition = o.CompetitivePosition
            });
        }
    }
}
