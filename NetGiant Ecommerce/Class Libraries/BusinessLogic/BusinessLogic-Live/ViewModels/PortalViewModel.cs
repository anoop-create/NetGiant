using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Nest;
using System.Web.Mvc;
using DataAccess.EntityFramework;
using System.Linq.Expressions;
using System.Data.Entity;

namespace BusinessLogic.ViewModels
{
    public class PortalViewModel : CommonViewModel
    {
        public PortalViewModel()
        {
            Results = new List<TelerikUsers>();
            //var node = new Uri(ConfigurationManager.AppSettings["ElasticSearchUri"]);
            //var settings = new ConnectionSettings(node);
            //_client = new ElasticClient(settings);
            //settings.DefaultIndex("portalindex");
        }

        public bool PostcodeOnly { private get; set; }
        public List<TelerikUsers> Results { get; private set; }
        public List<TelerikVouchers> Vouchers { get; private set; }
        public VoucherPromo Voucher { get; set; }
        public string VoucherScope { get; set; }
        public string SearchTerm
        {
            set => _searchTerm = value.FormatForIndex();
        }
        public string DbName { get; set; }
        public string VoucherCode { get; set; }
        public List<SelectListItem> Sites { get; set; }

        //private readonly ElasticClient _client;
        private string _searchTerm;

        public void CustomerSearch()
        {
            if (string.IsNullOrEmpty(_searchTerm)) return;

            // Uses Elastic Search
            //if (PostcodeOnly)
            //    _searchTerm = _searchTerm.Replace(" ", "");

            //ISearchResponse<UserDetailLookup> search;
            //if (PostcodeOnly)
            //{
            //    search = _client.Search<UserDetailLookup>(s => s
            //        .Query(q =>
            //            q.MatchPhrasePrefix(x => x
            //                .Field(p => p.Postcode)
            //                .Query(_searchTerm)
            //            )
            //        )
            //        .Take(200)
            //    );
            //}
            //else
            //{
            //    search = _client.Search<UserDetailLookup>(s => s
            //        .Query(q =>
            //            q.MatchPhrasePrefix(x => x
            //                .Field(p => p.FullName)
            //                .Query(_searchTerm)
            //            )
            //            ||
            //            q.MatchPhrasePrefix(x => x
            //                .Field(p => p.OrgName)
            //                .Query(_searchTerm)
            //            )
            //            ||
            //            q.MatchPhrasePrefix(x => x
            //                .Field(p => p.Email)
            //                .Query(_searchTerm)
            //            )
            //            ||
            //            q.MatchPhrasePrefix(x => x
            //                .Field(p => p.Account)
            //                .Query(_searchTerm)
            //            )
            //            ||
            //            q.MatchPhrasePrefix(x => x
            //                .Field(p => p.BillingAddress)
            //                .Query(_searchTerm)
            //            )
            //            ||
            //            q.MatchPhrasePrefix(x => x
            //                .Field(p => p.CustomerGroup)
            //                .Query(_searchTerm)
            //            )
            //        )
            //        .Take(200)
            //    );
            //}

            //Results = search.Hits.Select(x => x.Source).AsTelerikViewModel();

            // Uses SQL
            List<PortalIndex> lpi = new List<PortalIndex>();
            Expression<Func<PortalIndex, bool>> where;
            if (PostcodeOnly)
            {
                where = x => 
                    x.Postcode.Contains(_searchTerm);
            }
            else
            {
                where = x => 
                    x.FullName.Contains(_searchTerm)
                    || x.Email.Contains(_searchTerm)
                    || x.BillingAddress.Contains(_searchTerm)
                    || x.Record.Contains(_searchTerm)
                    || x.OrgName.Contains(_searchTerm)
                    || x.CustomerGroup.Contains(_searchTerm);
            }
            Results = EntityAccess.ReadPortalIndex(where).AsQueryable().AsTelerikViewModel();
        }

        public void GetCustomerVouchers(string scope)
        {
            if (scope == "customer")
            {
                Vouchers = EntityAccess.ReadVoucherPromo(x => x.AccountNumber != null && x.AccountNumber != "")
                    .OrderBy(x => x.WebsiteFk).ThenByDescending(x => x.ValidFrom)
                    .AsQueryable().AsTelerikViewModel();
            }
            else
            {
                Vouchers = EntityAccess.ReadVoucherPromo(x => x.AccountNumber == null && x.ValidFrom <= DateTime.Now && x.ValidTo >= DateTime.Now)
                    .OrderBy(x => x.WebsiteFk).ThenByDescending(x => x.ValidFrom)
                    .AsQueryable().AsTelerikViewModel();
            }
        }

        public class TelerikUsers
        {
            public string Email { get; set; }
            public string FullName { get; set; }
            public string BillingAddress { get; set; }
            public string PostCode { get; set; }
            public string FriendlyPostcode { get; set; }
            public DateTime? LastOrdered { get; set; }
            public string CustomerGroup { get; set; }
            public string WebsiteName { get; set; }
            public int WebsiteId { get; set; }
            public string Record { get; set; }
            public string OrgName { get; set; }
            public string CustomerGroupColor { get; set; }
            public bool IsPrimaryContact { get; set; }
        }

        public class TelerikVouchers
        {
            public int VoucherPromoId { get; set; }
            public int WebsiteId { get; set; }
            public string WebsiteName { get; set; }
            public int VoucherTypeId { get; set; }
            public string VoucherTypeName { get; set; }
            public int VoucherPromoGroupId { get; set; }
            public string VoucherPromoGroupName { get; set; }
            public string VoucherCode { get; set; }
            public string Description { get; set; }
            public DateTime? ValidFrom { get; set; }
            public DateTime? ValidTo { get; set; }
            public string StockRef { get; set; }
            public decimal MinBasketValue { get; set; }
            public decimal MinQualValue { get; set; }
            public decimal? Amount { get; set; }
            public decimal? Percentage { get; set; }
            //public string GiftStockRef { get; set; }
            //public int? MultiByuyQualNo { get; set; }
            //public int? MultiBuyNoDiscounted { get; set; }
            public string AccountNumber { get; set; }
            public bool? IsGlobal { get; set; }
            public bool? IsUsed { get; set; }
        }
    }

    public static class PortalExtensions
    {
        public static List<PortalViewModel.TelerikUsers> AsTelerikViewModel(this IEnumerable<UserDetailLookup> query)
        {
            return query.Select(o => new PortalViewModel.TelerikUsers
            {
                WebsiteName = o.WebsiteId == 1 ? "TG" : o.WebsiteId == 2 ? "CM" : o.WebsiteId == 3 ? "NG" : "",
                WebsiteId = o.WebsiteId,
                Email = string.IsNullOrEmpty(o.Email) ? "-" : o.Email,
                FullName = o.FullName,
                BillingAddress = o.BillingAddress,
                PostCode = string.IsNullOrEmpty(o.Postcode) ? "-" : o.Postcode,
                FriendlyPostcode = o.FriendlyPostcode,
                LastOrdered = o.LastOrderDate,
                CustomerGroup = o.CustomerGroup,
                CustomerGroupColor = o.CustomerGroup.Contains("Account Cust") || o.CustomerGroup.Contains("Public Sectr") || o.CustomerGroup.Contains("School") ? "green" : "transparent",
                Record = o.Record.Insert(o.Record.Length - 4, "-"),
                OrgName = o.OrgName
            }).ToList();
        }

        public static List<PortalViewModel.TelerikUsers> AsTelerikViewModel(this IEnumerable<PortalIndex> query)
        {
            return query.Select(o => new PortalViewModel.TelerikUsers
            {
                WebsiteName = o.WebsiteFk == 1 ? "TG" : o.WebsiteFk == 2 ? "CM" : o.WebsiteFk == 3 ? "NG" : "",
                WebsiteId = o.WebsiteFk,
                Email = string.IsNullOrEmpty(o.Email) ? "-" : o.Email,
                FullName = o.FullName,
                BillingAddress = o.BillingAddress,
                PostCode = string.IsNullOrEmpty(o.Postcode) ? "-" : o.Postcode,
                FriendlyPostcode = string.IsNullOrEmpty(o.FriendlyPostcode) ? "-" : o.FriendlyPostcode,
                LastOrdered = o.LastOrderDate,
                CustomerGroup = o.CustomerGroup,
                CustomerGroupColor = (o.CustomerGroup.Contains("Account Cust") || o.CustomerGroup.Contains("Public Sectr") || o.CustomerGroup.Contains("School")) ? "g-fc-p" : "hidden",
                Record = o.Record.Insert(o.Record.Length - 4, "-"),
                OrgName = o.OrgName,
                IsPrimaryContact = o.IsPrimaryContact
            }).ToList();
        }

        public static List<PortalViewModel.TelerikVouchers> AsTelerikViewModel(this IEnumerable<VoucherPromo> query)
        {
            return query.Select(o => new PortalViewModel.TelerikVouchers
            {
                VoucherPromoId = o.VoucherPromoId,
                WebsiteName = o.WebsiteFk == 1 ? "TG" : o.WebsiteFk == 2 ? "CM" : o.WebsiteFk == 3 ? "NG" : "",
                WebsiteId = o.WebsiteFk,
                VoucherTypeId = o.VoucherPromoId,
                VoucherTypeName = o.VoucherType.Description,
                VoucherPromoGroupId = o.VoucherPromoId,
                VoucherPromoGroupName = o.VoucherPromoGroup.GroupName,
                VoucherCode = o.VoucherCode,
                Description = o.Description,
                ValidFrom = o.ValidFrom,
                ValidTo = o.ValidTo,
                StockRef = o.StockRef,
                MinBasketValue = o.MinQualValue,
                MinQualValue = o.MinQualValue,
                Amount = o.Amount,
                Percentage = o.Percentage,
                //GiftStockRef = o.GiftStockRef,
                //MultiByuyQualNo = o.MultiBuyQualNo,
                //MultiBuyNoDiscounted = o.MultiBuyNoDiscounted,
                AccountNumber = o.AccountNumber,
                IsGlobal = o.IsGlobal,
                IsUsed = o.IsUsed
            }).ToList();
        }
    }
}


