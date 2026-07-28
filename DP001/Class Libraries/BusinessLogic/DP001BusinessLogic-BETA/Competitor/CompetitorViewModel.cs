using DP001BusinessLogic.Shared;
using DP001DataAccess.Entities;
using DP001DataAccess.Utilities;
using PagedList;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using MoreLinq;

namespace DP001BusinessLogic.ViewModels
{
    public class CompetitorViewModel
    {
        public CompetitorViewModel(int channelId)
        {
            _channelId = channelId;
            _ctx = new DP001Entities();
        }

        public IQueryable<TelerikCompetitor> CompetitorList { get; set; }
        public List<CompetitorInventory> CompetitorsList { get; set; }
        public IQueryable<Telerik> InventoryList { get; set; }
        public Competitor CompetitorEntry { get; set; }
        public List<CompetitorInventory> SearchResults { get; set; }
        public ReportConfiguration ReportConfiguration { get; set; }
        public List<SelectListItem> ReportSecurityList { get; set; }
        public List<SharedViewModel.SelectListItemExtended> ReportConfigList { get; set; }
        public bool UserCanModify { get; set; }
        public int? RequestedReportTenantId { get; set; }
        private int _channelId;
        private DP001Entities _ctx;

        public CompetitorViewModel InitializeReport(int? reportConfigId, string userId, int? tenantFk)
        {
            ReportSecurityList = SharedViewModel.GetLookupList("ReportSecurity");
            ReportConfigList = SharedViewModel.GetReportConfigSelectList(tenantFk, userId, "Competitor");

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

        public CompetitorViewModel GetInventory()
        {
            var crud = new CrudCompetitorInventory();
            InventoryList = crud.ReadCompetitorInventoryQuery(x => x.ChannelFK == _channelId && x.Lookup.LookupName == "Active", _ctx).AsTelerikViewModel();

            return this;
        }

        public Competitor GetCompetitor(int id)
        {
            var crudCompetitor = new CrudCompetitor();
            return crudCompetitor.Read(id);
        }

        public void GetCompetitors(int productID)
        {
            var crudCompetitors = new CrudCompetitorInventory();
            CompetitorsList = crudCompetitors.Read(x => x.ChannelFK == _channelId && x.ProductInventoryFK == productID);
        }

        public CompetitorViewModel GetCompetitorList()
        {
            var crud = new CrudCompetitor();
            CompetitorList = crud.ReadCompetitorQuery(x => x.ChannelFK == _channelId, _ctx).AsTelerikCompetitorViewModel();

            return this;
        }

        public SaveReturn Update(Competitor competitorEntry)
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = false;

            try
            {
                var crud = new CrudCompetitor();

                var isFound = crud.Read(x => x.ChannelFK == competitorEntry.ChannelFK
                    && x.CompetitorID == competitorEntry.CompetitorID).Count > 0;

                if (isFound)
                {
                    crud.Update(competitorEntry);
                    sr.IsSuccess = true;
                }
                else
                {
                    sr.Message = "Record does not exist";
                }
            }
            catch (Exception e)
            {
                sr.Message = e.Message;
            }

            return sr;
        }

        public List<CompetitorInventory> Search(string term, int competitorFk = 0)
        {
            var crud = new CrudCompetitorInventory();

            if (competitorFk > 0)
            {
                return crud.Read(x => x.ChannelFK == _channelId && x.CompetitorFK == competitorFk && x.ManufacturerPartNo.Contains(term));
            }

            return crud.Read(x => x.ChannelFK == _channelId && x.ManufacturerPartNo.Contains(term));
        }

        public List<Telerik> Search(int competitorFk = 0)
        {
            var crud = new CrudCompetitorInventory();
            List<Telerik> entryExclusions;

            if (competitorFk > 0)
            {
                entryExclusions = crud.Read(x => x.ChannelFK == _channelId && x.CompetitorFK == competitorFk).AsQueryable().AsTelerikViewModel().ToList();
            }
            else
            {
                entryExclusions = crud.Read(x => x.ChannelFK == _channelId).AsQueryable().AsTelerikViewModel().ToList();
            }

            var crudProviderExclusion = new CrudProviderExclusion();
            var currentExclusions = crudProviderExclusion.Read(x => x.ChannelFK == _channelId && x.ProviderFK == competitorFk);
            var currentEntryExclusions = currentExclusions.Where(y => y.Lookup1.LookupName == "Item")
                .Select(w => new { mfpn = w.ManufacturerPartNo, brand = w.BrandName})
                .ToList();

            foreach (var exclusion in entryExclusions)
            {
                if (currentEntryExclusions.FirstOrDefault(x => x.brand == exclusion.BrandName && x.mfpn == exclusion.ManufacturerPartNo) != null)
                {
                    exclusion.IsExcluded = true;
                }
            }

            return entryExclusions;
        }

        public List<TelerikCompetitorBrand> SearchBrands(int competitorFk = 0)
        {
            var crud = new CrudCompetitorInventory();
            List<TelerikCompetitorBrand> brandExclusions;

            if (competitorFk > 0)
            {
                brandExclusions = crud.Read(x => x.ChannelFK == _channelId && x.CompetitorFK == competitorFk).AsQueryable().AsTelerikBrandViewModel().ToList();
                brandExclusions = brandExclusions.DistinctBy(x => x.BrandName).OrderBy(x => x.BrandName).ToList();
            }
            else
            {
                brandExclusions = crud.Read(x => x.ChannelFK == _channelId).DistinctBy(x => x.Brand.BrandName).AsQueryable().AsTelerikBrandViewModel().ToList();
            }

            var crudProviderExclusion = new CrudProviderExclusion();
            var currentExclusions = crudProviderExclusion.Read(x => x.ChannelFK == _channelId && x.ProviderFK == competitorFk);
            var currentBrandExclusions = currentExclusions.Where(y => y.Lookup1.LookupName == "Brand")
                .Select(w => w.BrandName)
                .ToList();

            foreach (var brandExclusion in brandExclusions)
            {
                if (currentBrandExclusions.Contains(brandExclusion.BrandName))
                {
                    brandExclusion.IsExcluded = true;
                }
            }

            return brandExclusions;
        }

        public CompetitorViewModel SearchInventory(string term, int brandFK)
        {
            CrudCompetitorInventory crud = new CrudCompetitorInventory();
            SearchResults = crud.Read(x =>
                (x.ManufacturerPartNo.Contains(term) &&
                x.BrandFK == brandFK &&
                x.ChannelFK == _channelId), 20);

            return this;
        }

        public class Telerik
        {
            public long CompetitorInventoryId { get; set; }
            public long? ProductInventoryId { get; set; }
            public string ClientProductId { get; set; }
            public string ProductName { get; set; }
            public string ManufacturerPartNo { get; set; }
            public string Description { get; set; }
            public string BrandName { get; set; }
            public string CompetitorBrandName { get; set; }
            public decimal? Price { get; set; }
            public string CompetitorName { get; set; }
            public int CompetitorId { get; set; }
            public bool IsExcluded { get; set; }

            private DateTime? _dateLastUpdated;
            public DateTime? DateLastUpdated
            {
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
        }

        public class TelerikCompetitor
        {
            public int CompetitorId { get; set; }
            public string CompetitorName { get; set; }
            public int? NumberOfReviews { get; set; }
            public decimal? AverageReviewRating { get; set; }
            public int? ProductMatches { get; set; }
            public bool Active { get; set; }
        }

        public class TelerikCompetitorBrand
        {
            public string BrandName { get; set; }
            public string CompetitorName { get; set; }
            public bool IsExcluded { get; set; }
        }
    }

    public static class CompetitorExtensions
    {
        public static IQueryable<CompetitorViewModel.Telerik> AsTelerikViewModel(this IQueryable<CompetitorInventory> competitorQuery)
        {
            return competitorQuery.Select(o => new CompetitorViewModel.Telerik
            {
                CompetitorInventoryId = o.CompetitorInventoryID,
                ProductInventoryId = o.ProductInventory != null ? o.ProductInventory.ProductInventoryID : 0,
                ClientProductId = o.ProductInventory != null ? o.ProductInventory.ClientProductID : "",
                ProductName = o.ProductInventory != null ? o.ProductInventory.Description : "",
                ManufacturerPartNo = o.ManufacturerPartNo,
                Description = o.Description,
                BrandName = o.Brand != null ? o.Brand.BrandName : "",
                CompetitorBrandName = o.OriginalBrand,
                Price = o.Price,
                CompetitorName = o.Competitor != null ? o.Competitor.CompetitorName : "",
                CompetitorId = o.CompetitorFK,
                DateLastUpdated = o.DateLastUpdated
            });
        }

        public static IQueryable<CompetitorViewModel.TelerikCompetitor> AsTelerikCompetitorViewModel(this IQueryable<Competitor> competitorQuery)
        {
            return competitorQuery.Select(o => new CompetitorViewModel.TelerikCompetitor
            {
                CompetitorId = o.CompetitorID,
                CompetitorName = o.CompetitorName,
                NumberOfReviews = o.ReviewTotal,
                AverageReviewRating = o.ReviewRating,
                ProductMatches = o.ProductMatchCount,
                Active = o.IsActive
            });
        }

        public static IQueryable<CompetitorViewModel.TelerikCompetitorBrand> AsTelerikBrandViewModel(this IQueryable<CompetitorInventory> competitorQuery)
        {
            return competitorQuery.Select(o => new CompetitorViewModel.TelerikCompetitorBrand()
            {
                BrandName = o.Brand.BrandName,
                CompetitorName = o.Competitor.CompetitorName
            });
        }
    }
}
