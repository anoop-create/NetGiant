using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Nest;
using System.Web.Mvc;
using DataAccess.EntityFramework;
using System.Linq.Expressions;
using System.Data.Entity;
using DataAccess.Utilities;
using System.Data;
using System.Web;
using System.ComponentModel.DataAnnotations;

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
        public List<TelerikBackOrders> BackOrders { get; private set; }
        public List<TelerikOrderTracking> OrderTracking { get; private set; }
        public string OrderTrackingAcc { get; set; }
        public VoucherPromo Voucher { get; set; }
        public string VoucherScope { get; set; }
        public string SearchTerm
        {
            set => _searchTerm = value.FormatForIndex();
        }
        public string DbName { get; set; }
        public string VoucherCode { get; set; }
        [Required(ErrorMessage = "Please enter an email address")]
        [RegularExpression(@"^([a-zA-Z0-9_\-\.\']+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{2,20}|[0-9]{1,3})(\]?)$", ErrorMessage = "Please enter a valid email address")]
        public string UserEmail { get; set; }
        public string NextUrl { get; set; }
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
                //removing spaces from postcode only to fix "order by" errors
                _searchTerm = _searchTerm.Replace(" ", "");

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
                Vouchers = EntityAccess.ReadVoucherPromo(x => ((x.AccountNumber != null && x.AccountNumber != "") || x.IsSingleUse))
                    .OrderBy(x => x.WebsiteFk).ThenByDescending(x => x.ValidFrom)
                    .AsQueryable().AsTelerikViewModel();
            }
            else
            {
                Vouchers = EntityAccess.ReadVoucherPromo(x => x.AccountNumber == null && x.ValidFrom <= DateTime.Now && x.ValidTo >= DateTime.Now && x.ForGeneralUse)
                    .OrderBy(x => x.WebsiteFk).ThenByDescending(x => x.ValidFrom)
                    .AsQueryable().AsTelerikViewModel();
            }
        }

        public void GetBackOrders()
        {
            BackOrders = EntityAccess.ReadBackOrder(x => true)
                .OrderBy(x => x.BackOrder.OrderDate).ThenBy(x => x.BackOrder.OrderReferenceNumber)
                .AsQueryable().AsTelerikViewModel();
        }

        public void GetOrderTracking(string acc = "")
        {
            if (string.IsNullOrEmpty(acc))
            {
                OrderTracking = EntityAccess.ReadOrderTracking(x => true)
                    .OrderByDescending(x => x.OrderDate).ThenBy(x => x.OrderNumber)
                    .AsQueryable().AsTelerikViewModel();
            }
            else
            {
                OrderTracking = EntityAccess.ReadOrderTracking(x => x.CustomerRef == acc)
                    .OrderByDescending(x => x.OrderDate).ThenBy(x => x.OrderNumber)
                    .AsQueryable().AsTelerikViewModel();

            }
        }

        public SaveReturn SendTrackingLink(int id)
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = true;

            try
            {

                OrderTracking ot = EntityAccess.ReadOrderTracking(x => x.OrderTrackingId == id).FirstOrDefault();
                provider p = EntityAccess.ReadProvider(x => x.providerID == ot.CourierFk).FirstOrDefault();

                // Get the correct email template/support email address for the website associated with the order
                cmsEntry cms = EntityAccess.ReadCms(x => x.cmsSection.sectionName == "EmailData" && x.entryName == "TrackingLinkEmail", ot.WebsiteFk).FirstOrDefault();
                string supportEmailAddress = EntityAccess.ReadCms(x => x.cmsSection.sectionName == "CommonData" && x.entryName == "SupportEmail", ot.WebsiteFk).FirstOrDefault().cmsContent;
                Dictionary<string, string> replacements = new Dictionary<string, string>
                {
                    { "[trackinglink]", p.url + ot.TrackingCode },
                    { "[ordernumber]", ot.OrderNumber },
                    { "[courier]", p.providerName },
                    { "[Year]", DateTime.Now.Year.ToString() }
                };

                if (cms != null)
                {
                    string body = cms.cmsContent;
                    foreach (KeyValuePair<string, string> kvp in replacements)
                    {
                        body = body.Replace(kvp.Key, kvp.Value);
                    }
                    Utilities.SendEmail(supportEmailAddress.ToLower(),
                        ot.Email.ToLower(), "Order Tracking Link", body);
                }
            }
            catch (Exception ex)
            {
                Utilities.ProcessException(ex);
                sr.IsSuccess = false;
                sr.Message = ex.Message;
            }

            return sr;
        }

        public int FindCSUserId()
        {
            return DataCache.GetCustLookups(x => x.LookupType.LookupTypeName == "Customer Service Users" && UserEmail.Contains(x.LookupName.Split('|')[1]))
                    .FirstOrDefault()?.Sequence ?? 19;
        }

        public bool DuoIsEnabled()
        {
            string sql = @"SELECT ISNULL ((SELECT TwoFactorEnabled
              FROM [netgiantMembership].[dbo].[AspNetUsers]
              WHERE UserName = '" + UserEmail + "'), 1) AS [TwoFactorEnabled]";

            DataTable dt = SQL.ExecuteReadInline("netgiantmembership", sql).Tables[0];

            return Convert.ToBoolean(dt.Rows[0]["TwoFactorEnabled"].ToString());
        }

        public void SetVarsForPortal()
        {
            TimeSpan ts = ConfigurationManager.AppSettings["Environment"] == "Live" ? new TimeSpan(0, 20, 0, 0) : new TimeSpan(0, 20, 0, 0);
            HttpContext.Current.Session["U_CSUser"] = FindCSUserId().ToString();
            Authentication.WriteCookie("__skipportalauth", "y", ts);
            Authentication.WriteCookie("__csuser", HttpContext.Current.Session["U_CSUser"].ToString(), new TimeSpan(30, 0, 0, 0));
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
            public bool HasTracking { get; set; }
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

        public class TelerikBackOrders
        {
            public int BackOrderId { get; set; }
            public int BackOrderItemId { get; set; }
            public DateTime? OrderDate { get; set; }
            public string Provider { get; set; }
            public string Website { get; set; }
            public string OrderReferenceNumber { get; set; }
            public string SupplierOrderNumber { get; set; }
            public string PurchaseOrderNumber { get; set; }
            public string CustomerName { get; set; }
            public string Status { get; set; }
            public string ItemReference { get; set; }
            public string SupplierItemReference { get; set; }
            public string Description { get; set; }
            public int QuantityOrdered { get; set; }
            public DateTime? StockReplenishmentDate { get; set; }
        }

        public class TelerikOrderTracking
        {
            public int OrderTrackingId { get; set; }
            public string Website { get; set; }
            public string OrderNumber { get; set; }
            public string PurchaseOrderNumber { get; set; }
            public DateTime OrderDate { get; set; }
            public string CustomerRef { get; set; }
            public string Courier { get; set; }
            public string Name { get; set; }
            public string Email { get; set; }
            public string TrackingCode { get; set; }
            public string TrackingLink { get; set; }
            public string IsSent { get; set; }
            public string Blank { get; set; }
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
                WebsiteName = o.Website.Abbreviation,
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
                IsPrimaryContact = o.IsPrimaryContact,
                HasTracking = o.HasTracking ?? false
            }).ToList();
        }

        public static List<PortalViewModel.TelerikVouchers> AsTelerikViewModel(this IEnumerable<VoucherPromo> query)
        {
            return query.Select(o => new PortalViewModel.TelerikVouchers
            {
                VoucherPromoId = o.VoucherPromoId,
                WebsiteName = o.Website.Abbreviation,
                WebsiteId = o.WebsiteFk,
                VoucherTypeId = o.VoucherTypeFk,
                VoucherTypeName = o.VoucherTypeName,
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

        public static List<PortalViewModel.TelerikBackOrders> AsTelerikViewModel(this IEnumerable<BackOrderItem> query)
        {

            return query.Select(o => new PortalViewModel.TelerikBackOrders
            {
                BackOrderId = o.BackOrder.BackOrderId,
                BackOrderItemId = o.BackOrderItemId,
                OrderDate = o.BackOrder.OrderDate,
                Provider = o.BackOrder.provider.providerName.Replace(" Back Orders", ""),
                Website = o.BackOrder.Website.Abbreviation,
                OrderReferenceNumber = o.BackOrder.OrderReferenceNumber,
                SupplierOrderNumber = o.BackOrder.SupplierOrderNumber,
                PurchaseOrderNumber = o.BackOrder.PurchaseOrderNumber,
                CustomerName = o.BackOrder.CustomerName,
                ItemReference = o.ItemReference,
                SupplierItemReference = o.SupplierItemReference,
                Description = o.Description,
                QuantityOrdered = o.QuantityOrdered,
                StockReplenishmentDate = o.StockReplenishmentDate,
                Status = o.Lookup.LookupName
            }).ToList();
        }

        public static List<PortalViewModel.TelerikOrderTracking> AsTelerikViewModel(this IEnumerable<OrderTracking> query)
        {

            return query.Select(o => new PortalViewModel.TelerikOrderTracking
            {
                OrderTrackingId = o.OrderTrackingId,
                Website = o.Website.WebsiteName,
                OrderNumber = o.OrderNumber,
                PurchaseOrderNumber = o.PurchaseOrderNumber,
                OrderDate = o.OrderDate,
                CustomerRef = o.CustomerRef,
                Courier = o.provider.providerName == "Dummy Courier" ? "No Information" : o.provider.providerName,
                Name = o.FirstName + " " + o.Surname,
                Email = o.Email,
                TrackingCode = o.TrackingCode,
                //TrackingLink = string.IsNullOrEmpty(o.provider.url) ? o.TrackingLink : o.provider.url + o.TrackingCode,
                TrackingLink = 
                string.IsNullOrEmpty(o.provider.url) ? 
                    string.IsNullOrEmpty(o.TrackingLink) ?
                        "javascript: $.alert({ title : 'No Tracking Information', content: 'The supplier has not provided tracking information' });" 
                        : o.TrackingLink
                    : o.provider.url + o.TrackingCode,
                Blank = string.IsNullOrEmpty(o.TrackingLink) && string.IsNullOrEmpty(o.provider.url) ? "" : "_blank",
                IsSent = o.IsSent ? "Yes" : "No"
            }).ToList();
        }
    }
}


