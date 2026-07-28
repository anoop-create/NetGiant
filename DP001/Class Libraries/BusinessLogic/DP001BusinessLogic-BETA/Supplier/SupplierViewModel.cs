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

namespace DP001BusinessLogic.ViewModels
{
    public class SupplierViewModel
    {
        public SupplierViewModel()
        {

        }

        public SupplierViewModel(int channelId)
        {
            _channelId = channelId;
            _ctx = new DP001Entities();
        }

        public List<Supplier> SupplierList { get; set; }
        public List<SelectListItem> SupplierSelectList { get; set; }
        public IQueryable<Telerik> InventoryList { get; set; }
        public Supplier SupplierEntry { get; set; }
        public List<SupplierInventory> SearchResults { get; set; }

        public IQueryable<TelerikSupplierBrandAliases> BrandAliases { get; set; }
        public SupplierBrandMatching SupplierBrandMatchingEntry { get; set; }

        public IQueryable<TelerikSupplierMfpnAliases> MfpnAliases { get; set; }
        public SupplierMfpnMatching SupplierMfpnMatchingEntry { get; set; }
        public List<SelectListItem> MatchTypes { get; set; }
        public ReportConfiguration ReportConfiguration { get; set; }
        public List<SelectListItem> ReportSecurityList { get; set; }
        public List<SharedViewModel.SelectListItemExtended> ReportConfigList { get; set; }
        public bool UserCanModify { get; set; }
        public int ChannelID { get; set; }
        public int? RequestedReportTenantId { get; set; }

        private int _channelId;
        private DP001Entities _ctx;

        public SupplierViewModel InitializeReport(int? reportConfigId, string userId, int? tenantFk)
        {
            ReportSecurityList = SharedViewModel.GetLookupList("ReportSecurity");
            ReportConfigList = SharedViewModel.GetReportConfigSelectList(tenantFk, userId, "Supplier");

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

        public SupplierViewModel GetSupplierList()
        {
            var crud = new CrudSupplier();
            SupplierList = crud.Read(x => x.ChannelFK == _channelId);

            return this;
        }

        public SupplierViewModel GetInventory()
        {
            var crud = new CrudSupplierInventory();
            InventoryList = crud.ReadSupplierInventoryQuery(x => x.ChannelFK == _channelId && x.Lookup.LookupName == "Active", _ctx).AsTelerikViewModel();

            return this;
        }

        public SupplierViewModel New()
        {
            SupplierEntry = new Supplier();

            return this;
        }

        public SupplierViewModel Edit(int SupplierId)
        {
            var crud = new CrudSupplier();

            SupplierEntry = crud.Read(x => x.ChannelFK == _channelId
                && x.SupplierID == SupplierId, 100)
                .FirstOrDefault();

            return this;
        }

        public void Create()
        {
            var crud = new CrudSupplier();
            crud.Create(SupplierEntry);
        }

        public void Update(Supplier supplierEntry)
        {
            var crud = new CrudSupplier();

            var isValid = crud.Read(x => x.ChannelFK == supplierEntry.ChannelFK
                && x.SupplierID == supplierEntry.SupplierID).Count > 0;

            if (isValid)
                crud.Update(supplierEntry);
        }

        public void Delete(int id)
        {
            var crud = new CrudSupplier();

            var deleteRecord = crud.Read(x => x.ChannelFK == _channelId
                && x.SupplierID == id).FirstOrDefault();

            if (deleteRecord != null)
                crud.Delete(deleteRecord);
        }

        public SupplierViewModel SearchInventory(string term, int brandFK)
        {
            var crud = new CrudSupplierInventory();
            SearchResults = crud.Read(x =>
                ((x.ManufacturerPartNo.Contains(term) ||
                x.Description.Contains(term)) &&
                x.BrandFK == brandFK &&
                x.ChannelFK == _channelId), 20);

            return this;
        }

        //Brand Alias
        public SupplierViewModel GetBrandAliases()
        {
            var crud = new CrudSupplierBrandMatching();
            BrandAliases = crud.ReadQuery(x => x.Supplier.ChannelFK == _channelId, _ctx).AsTelerikBrandAliasesViewModel();

            return this;
        }

        public SupplierViewModel NewBrandAlias()
        {
            SupplierBrandMatchingEntry = new SupplierBrandMatching();
            GetSupplierList();
            SupplierSelectList = SupplierList.Select(x => new SelectListItem { Text = x.SupplierName, Value = x.SupplierID.ToString() }).ToList();

            return this;
        }

        public SaveReturn CreateBrandAlias(int channelId)
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = false;
            bool isValid = true;
            _channelId = channelId;

            if (!isValid)
            {
                sr.IsSuccess = false;
                return sr;
            }

            try
            {
                GetSupplierList();
                var crud = new CrudSupplierBrandMatching();
                crud.Create(SupplierBrandMatchingEntry, channelId);
                sr.IsSuccess = true;
            }
            catch (Exception e)
            {
                sr.Message = e.Message;
            }

            return sr;
        }

        public SupplierViewModel EditBrandAlias(int id)
        {
            var crud = new CrudSupplierBrandMatching();

            SupplierBrandMatchingEntry = crud.Read(x => x.SupplierBrandMatchingID == id && x.Supplier.ChannelFK == _channelId)
                .FirstOrDefault();

            if (SupplierBrandMatchingEntry != null)
            {
                GetSupplierList();
                SupplierSelectList = SupplierList.Select(x => new SelectListItem { Text = x.SupplierName, Value = x.SupplierID.ToString() }).ToList();
            }

            return this;
        }

        public SaveReturn UpdateBrandAlias(SupplierBrandMatching supplierBrandMatchingEntry)
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = false;

            try
            {
                var crud = new CrudSupplierBrandMatching();

                var isFound = crud.Read(x => x.SupplierBrandMatchingID == supplierBrandMatchingEntry.SupplierBrandMatchingID &&
                    x.Supplier.ChannelFK == ChannelID).Count > 0;

                if (isFound)
                {
                    crud.Update(supplierBrandMatchingEntry);
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

        public void DeleteBrandAlias(int id)
        {
            var crud = new CrudSupplierBrandMatching();

            var deleteRecord = crud.Read(x => x.SupplierBrandMatchingID == id && x.Supplier.ChannelFK == _channelId).FirstOrDefault();

            if (deleteRecord != null)
                crud.Delete(deleteRecord);
        }

        //Mfpn Alias
        public SupplierViewModel GetMfpnAliases()
        {
            var crud = new CrudSupplierMfpnMatching();
            MfpnAliases = crud.ReadQuery(x => x.ChannelFK == _channelId, _ctx).AsTelerikMfpnAliasesViewModel();

            return this;
        }

        public SupplierViewModel NewMfpnAlias()
        {
            SupplierMfpnMatchingEntry = new SupplierMfpnMatching();
            MatchTypes = SharedViewModel.GetLookupList("MfpnMatchType");

            return this;
        }

        public SaveReturn CreateMfpnAlias()
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = false;
            bool isValid = true;

            //foreach (Supplier sup in SupplierList)
            //{
            //    //Unique Schedule name check
            //    if (sup.SupplierID == SupplierBrandMatchingEntry.SupplierFK)
            //    {
            //        sr.Message = "You cannot add a schedule with the same name as an existing schedule";
            //        isValid = false;
            //    }
            //}

            if (!isValid)
            {
                sr.IsSuccess = false;
                return sr;
            }

            try
            {
                //GetSupplierList();
                var crud = new CrudSupplierMfpnMatching();
                crud.Create(SupplierMfpnMatchingEntry);
                sr.IsSuccess = true;
            }
            catch (Exception e)
            {
                sr.Message = e.Message;
            }

            return sr;
        }

        public SupplierViewModel EditMfpnAlias(int id)
        {
            var crud = new CrudSupplierMfpnMatching();

            SupplierMfpnMatchingEntry = crud.Read(x => x.SupplierMfpnMatchingID == id && x.ChannelFK == _channelId)
                .FirstOrDefault();

            if (SupplierMfpnMatchingEntry != null)
            {
                MatchTypes = SharedViewModel.GetLookupList("MfpnMatchType");
            }

            return this;
        }

        public SaveReturn UpdateMfpnAlias(SupplierMfpnMatching supplierMfpnMatchingEntry)
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = false;

            try
            {
                var crud = new CrudSupplierMfpnMatching();

                var isFound = crud.Read(x => x.SupplierMfpnMatchingID == supplierMfpnMatchingEntry.SupplierMfpnMatchingID &&
                    x.ChannelFK == supplierMfpnMatchingEntry.ChannelFK).Count > 0;

                if (isFound)
                {
                    crud.Update(supplierMfpnMatchingEntry);
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

        public void DeleteMfpnAlias(int id)
        {
            var crud = new CrudSupplierMfpnMatching();

            var deleteRecord = crud.Read(x => x.SupplierMfpnMatchingID == id && x.ChannelFK == _channelId).FirstOrDefault();

            if (deleteRecord != null)
                crud.Delete(deleteRecord);
        }

        public void Dispose()
        {
            _ctx.Dispose();
        }

        public class Telerik
        {
            public long SupplierInventoryId { get; set; }
            public long? ProductInventoryId { get; set; }
            public string ClientProductId { get; set; }
            public string ProductName { get; set; }
            public string ManufacturerPartNo { get; set; }
            public string Description { get; set; }
            public string BrandName { get; set; }
            public string SupplierBrandName { get; set; }
            public int Quantity { get; set; }
            public decimal? Price { get; set; }
            public string SupplierName { get; set; }

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

        public class TelerikSupplierBrandAliases
        {
            public int SupplierBrandMatchingId { get; set; }
            public string SupplierName { get; set; }
            public string BrandName { get; set; }
            public string Alias { get; set; }
        }

        public class TelerikSupplierMfpnAliases
        {
            public int SupplierMfpnMatchingId { get; set; }
            public string BrandName { get; set; }
            public string MatchTerm { get; set; }
            public string Type { get; set; }
        }
    }

    public static class SupplierExtensions
    {
        public static IQueryable<SupplierViewModel.Telerik> AsTelerikViewModel(this IQueryable<SupplierInventory> supplierQuery)
        {
            return supplierQuery.Select(o => new SupplierViewModel.Telerik
            {
                SupplierInventoryId = o.SupplierInventoryID,
                ProductInventoryId = o.ProductInventory.ProductInventoryID,
                ClientProductId = o.ProductInventory.ClientProductID,
                ProductName = o.ProductInventory != null ? o.ProductInventory.Description : "",
                ManufacturerPartNo = o.ManufacturerPartNo,
                Description = o.Description,
                BrandName = o.Brand.BrandName,
                SupplierBrandName = o.OriginalBrand,
                Quantity = o.StockQuantity,
                Price = o.Price,
                SupplierName = o.Supplier.SupplierName,
                DateLastUpdated = o.DateLastUpdated
            });
        }

        public static IQueryable<SupplierViewModel.TelerikSupplierBrandAliases> AsTelerikBrandAliasesViewModel(this IQueryable<SupplierBrandMatching> query)
        {
            return query.Select(o => new SupplierViewModel.TelerikSupplierBrandAliases
            {
                SupplierBrandMatchingId = o.SupplierBrandMatchingID,
                SupplierName = o.Supplier.SupplierName,
                BrandName = o.BrandName,
                Alias = o.Reference
            });
        }

        public static IQueryable<SupplierViewModel.TelerikSupplierMfpnAliases> AsTelerikMfpnAliasesViewModel(this IQueryable<SupplierMfpnMatching> query)
        {
            return query.Select(o => new SupplierViewModel.TelerikSupplierMfpnAliases
            {
                SupplierMfpnMatchingId = o.SupplierMfpnMatchingID,
                BrandName = o.BrandName,
                MatchTerm = o.MatchTerm,
                Type = o.Lookup.LookupName
            });
        }
    }
}
