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
using System.Linq.Expressions;
using Kendo.Mvc;

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
        public IQueryable<ProductViewModel.Telerik> TelerikProducts { get; set; }
        public List<CustomField> CustomProductFields { get; set; }
        public List<CustomField> CustomAdjustmentFields { get; set; }
        public IList<IFilterDescriptor> InitialFilters { get; set; }
        public ReportConfiguration ReportConfiguration { get; set; }
        public List<SelectListItem> ReportSecurityList { get; set; }
        public List<SharedViewModel.SelectListItemExtended> ReportConfigList { get; set; }
        public int? RequestedReportTenantId { get; set; }
        public bool UserCanModify { get; set; }

        private int _channelId;
        private DP001Entities _ctx;

        //public ReportsViewModel Get(bool? hasRuleName = null)
        //{
        //    var crud = new CrudReports();

        //    switch (hasRuleName)
        //    {
        //        case null:
        //            ProductsDM = crud.ReadProductsQuery(
        //            x => x.Pi.ChannelFK == _channelId &&
        //            x.Pi.Lookup1.LookupName == "Active",
        //            _ctx);
        //            break;
        //        case true:
        //            ProductsDM = crud.ReadProductsQuery(
        //            x => x.Pi.ChannelFK == _channelId && x.Pi.PriceRuleFK > 0 &&
        //            x.Pi.Lookup1.LookupName == "Active",
        //            _ctx);
        //            break;
        //        case false:
        //            ProductsDM = crud.ReadProductsQuery(
        //            x => x.Pi.ChannelFK == _channelId && x.Pi.PriceRuleFK == null &&
        //            x.Pi.Lookup1.LookupName == "Active",
        //            _ctx);
        //            break;
        //    }

        //    return this;
        //}

        public ReportsViewModel InitializeReport(int? reportConfigId, string userId, int? tenantFk)
        {
            var crud = new CrudCustomField();
            CustomProductFields = crud.Read(x => x.ChannelFK == _channelId && x.Lookup.LookupName == "Product Inventory Field");
            CustomAdjustmentFields = crud.Read(x => x.ChannelFK == _channelId && x.Lookup.LookupName == "Price Adjustment Field");
            ReportSecurityList = SharedViewModel.GetLookupList("ReportSecurity");
            ReportConfigList = SharedViewModel.GetReportConfigSelectList(tenantFk, userId, "Comparison");

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

        public ReportsViewModel GetComparison()
        {
            var crud = new CrudProductInventory();
            TelerikProducts = crud.ReadProductsQuery(x => x.ChannelFK == _channelId && x.Lookup1.LookupName == "Active", _ctx).AsTelerikViewModel();

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

        public ReportsViewModel GetKeyLinesInventory()
        {
            var crud = new CrudProductInventory();

            TelerikProducts = crud.ReadProductsQuery(
                x => x.ChannelFK == _channelId && x.IsKeyLine == true && x.Lookup1.LookupName == "Active",
                _ctx).AsTelerikViewModel();

            return this;
        }

        public void Dispose()
        {
            _ctx.Dispose();
        }
    }
}
