using DataAccess.EntityFramework;
using DataAccess.Utilities;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Nest;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Web.UI.WebControls;

namespace BusinessLogic.ViewModels
{
    public class CommonViewModel
    {
        public CommonViewModel()
        {
            CommonData = DataCache.GetSectionData("CommonData");
            if (HttpContext.Current.Session["D_StandardDeliveryDate"] == null)
            {
                Utilities.SetDeliveryDate();
            }

            DomainNameTrunc = Convert.ToString(CommonData["DomainName"]);
            DomainNameTrunc = DomainNameTrunc.Remove(DomainNameTrunc.Length - 1);

            FeeFoScore = DataCache.GetFeeFoScore();
            AggregateRating = "\"aggregateRating\" : {" +
                         "\"@type\" : \"AggregateRating\"," +
                         "\"ratingValue\" : \"" + Utilities.GetItemFromDict(FeeFoScore, "FiveStar").ToString() + "\"," +
                         "\"ratingCount\" : \"" + Utilities.GetItemFromDict(FeeFoScore, "Count").ToString() + "\"," +
                         "\"reviewCount\" : \"" + Utilities.GetItemFromDict(FeeFoScore, "Count").ToString() + "\"" +
                         "},";
            IsCompatibleSaleActive = Convert.ToBoolean(Utilities.GetItemFromDict(CommonData, "IsCompatibleSaleActive")) && !Convert.ToBoolean(HttpContext.Current.Session["U_IsTradeCustomer"]);
            IsOEMSaleActive = Convert.ToBoolean(Utilities.GetItemFromDict(CommonData, "IsOEMSaleActive")) && !Convert.ToBoolean(HttpContext.Current.Session["U_IsTradeCustomer"]);
            IsStationerySaleActive = Convert.ToBoolean(Utilities.GetItemFromDict(CommonData, "IsStationerySaleActive"));
            ShowCustomerAlert = ConfigurationManager.AppSettings["AlertLevel"] == "4"
                ? true
                : Convert.ToBoolean(Utilities.GetItemFromDict(CommonData, "ShowCustomerAlert"));
            IsInMaintenanceMode = Convert.ToBoolean(EntityAccess.ReadCms(x => x.cmsSection.sectionName == "CommonData"
                                                                              && x.entryName == "IsInMaintenanceMode")
                .FirstOrDefault().cmsContent);

            var controller = HttpContext.Current.Request.RequestContext.RouteData.Values["controller"].ToString()
                .ToLower();
            var action = HttpContext.Current.Request.RequestContext.RouteData.Values["action"].ToString().ToLower();

            if (IsInMaintenanceMode && controller != "error")
            {
                throw new WebsiteInMaintenanceException();
            }

            if (IsCompatibleSaleActive || IsOEMSaleActive || IsStationerySaleActive)
            {
                SaleData = DataCache.GetSectionData("SaleData");
                CommonData["FeatureBackground"] = Utilities.GetItemFromDict(SaleData, "FeatureBackground");
                if (IsCompatibleSaleActive)
                {
                    CompatibleDiscount = Convert.ToDecimal(Utilities.GetItemFromDict(SaleData, "CompatibleDiscount"));
                    CompatibleSaleType = Utilities.GetItemFromDict(SaleData, "CompatibleSaleType");
                }
                if (IsOEMSaleActive)
                {
                    OEMDiscount = Convert.ToDecimal(Utilities.GetItemFromDict(SaleData, "OEMDiscount"));
                    OEMSaleType = Utilities.GetItemFromDict(SaleData, "OEMSaleType");
                }
                if (IsStationerySaleActive)
                {
                    StationeryDiscount = Convert.ToDecimal(Utilities.GetItemFromDict(SaleData, "StationeryDiscount"));
                }
            }

            StaticFilePrefix = Utilities.GetStaticFilePrefix();
            Menu = DataCache.GetMenu("Menu");
            MobileMenu = DataCache.GetMenu("Mobile");
            VatMultiplier = Convert.ToDecimal(ConfigurationManager.AppSettings["VatMultiplier"].ToString());

            // If Session has been lost
            if (HttpContext.Current.Session["B_BasketTotals"] == null)
            {
                Basket.LoadCookie();
            }

            BasketContents = Utilities.LoadSession<List<BasketContents>>("B_BasketArray");
            GetMeta();
            if (ConfigurationManager.AppSettings["Environment"].ToString() == "Live")
            {
                IsLiveSystem = true;
            }
            SearchApplication = ConfigurationManager.AppSettings["SearchApplication"];
            TitleList = Utilities.BuildTitleList();

            string utm_medium = HttpContext.Current.Request.QueryString["utm_medium"];
            if (!string.IsNullOrEmpty(utm_medium))
            {
                if (utm_medium.Contains("email"))
                {
                    string cam = HttpContext.Current.Request.QueryString["utm_campaign"];
                    if (!string.IsNullOrEmpty(cam))
                    {
                        HttpContext.Current.Session["U_MC_CampaignID"] = cam.Split('-')[0] ?? "";
                        if (string.IsNullOrEmpty(HttpContext.Current.Session["U_Campaign"] as string))
                        {
                            HttpContext.Current.Session["U_CampaignSource"] = "EMAIL";
                            if (HttpContext.Current.Request.QueryString["utm_campaign"].Contains("-"))
                            {
                                HttpContext.Current.Session["U_Campaign"] = cam.Split(new char[] { '-' }, 2)[1] ?? cam.ToString();
                            }
                            else
                            {
                                HttpContext.Current.Session["U_Campaign"] = cam;
                            }
                        }
                    }
                }

                if (utm_medium.Contains("Affiliate"))
                {
                    HttpContext.Current.Session["U_AffiliateNo"] = HttpContext.Current.Request.QueryString["utm_campaign"] ?? "";
                    HttpContext.Current.Session["U_AwinCheckSumId"] = HttpContext.Current.Request.QueryString["awc"] ?? "";
                    if (string.IsNullOrEmpty(HttpContext.Current.Session["U_Campaign"] as string))
                    {
                        HttpContext.Current.Session["U_CampaignSource"] = "AWIN";
                        HttpContext.Current.Session["U_Campaign"] = HttpContext.Current.Request.QueryString["utm_campaign"] ?? "";
                    }

                    HttpCookie myCookie = new HttpCookie("awc");
                    myCookie.Value = HttpContext.Current.Request.QueryString["awc"] ?? "";
                    myCookie.Expires = DateTime.Now.AddDays(45);
                    HttpContext.Current.Response.Cookies.Add(myCookie);
                }
            }

            IsMobile = Convert.ToBoolean(HttpContext.Current.Session["U_IsMobile"]);

            // First Time Processing
            IsFirstTime = false;
            if (Convert.ToBoolean(HttpContext.Current.Session["U_IsFirstTime"]))
            {
                HttpContext.Current.Session.Remove("U_IsFirstTime");
                DoFirstTimeProcess();
            }

            // Check Cookie Consent
            if (!Convert.ToBoolean(HttpContext.Current.Session["U_CookieConsentAccepted"]))
            {
                ShowCookieConsent = true;
            }

            if (Convert.ToBoolean(HttpContext.Current.Session["U_IsPortalUser"]) && HttpContext.Current.Request.Cookies["__csuser"] != null)
            {
                HttpContext.Current.Session["U_CSUser"] = Convert.ToString(HttpContext.Current.Request.Cookies["__csuser"].Value);
            }

            EncryptedDate = Utilities.SimpleEncryptString(DateTime.Now.ToUniversalTime().ToString("u"));

            BreadcrumbTrail = new Dictionary<string, string>();
            BreadcrumbTrail.Add("Home", "");
            SignIn = new SignIn
            {
                UserName = string.Empty,
                Password = string.Empty
            };

            // ✅ Initialize SignUp and all nested objects
            SignUp = new SignUp
            {
                UserName = string.Empty,
                Password = string.Empty,
                Newsletter = false,
                TelNumber = string.Empty,
                AddressLookup = string.Empty,
                Name = new Name
                {
                    Firstname = string.Empty,
                    Surname = string.Empty
                },
                Address = new Address
                {
                    Line1 = string.Empty,   // Company name
                    Line2 = string.Empty,   // Address line 1
                    Line3 = string.Empty,   // Address line 2
                    Line4 = string.Empty,   // Town/City
                    Line5 = string.Empty,   // County
                    PostCode = string.Empty // Postcode
                }
            };
        }

        public Dictionary<string, string> CommonData { get; set; }
        public Dictionary<string, string> FeeFoScore { get; set; }
        public Dictionary<string, string> SaleData { get; set; }
        public Dictionary<string, string> MetaData { get; set; }
        public List<LookupNgmd> CartridgeTypes { get; set; }
        public string AggregateRating { get; set; }
        public DataTable PopularPrinters { get; set; }
        public DataTable PopularCartridges { get; set; }
        public DataTable PopularRanges { get; set; }
        public SignIn SignIn { get; set; }
        public SignUp SignUp { get; set; }
        public decimal VatMultiplier { get; set; }
        public List<BasketContents> BasketContents { get; set; }
        public List<BasketContents> AddonProducts { get; set; }
        public bool IsLiveSystem { get; set; } = false;
        public string Menu { get; set; }
        public string MobileMenu { get; set; }
        public bool IsCompatibleSaleActive { get; set; }
        public bool IsOEMSaleActive { get; set; }
        public bool IsStationerySaleActive { get; set; }
        public string OEMSaleType { get; set; }
        public string CompatibleSaleType { get; set; }
        public bool IsInMaintenanceMode { get; set; }
        public bool ShowCustomerAlert { get; set; }

        public bool ShowCookieConsent { get; set; } = false;
        public decimal OEMDiscount { get; set; }
        public decimal CompatibleDiscount { get; set; }
        public decimal StationeryDiscount { get; set; }
        public List<SelectListItem> TitleList { get; set; }
        public List<MiniProductEntry> BestSellers1 { get; set; }
        public List<MiniProductEntry> BestSellers2 { get; set; }
        public string RedirectUrl { get; set; }
        public bool IsFirstTime { get; set; }
        public string FirstTimePopup { get; set; } = "";
        public string FirstTimeMessage { get; set; } = "";
        public string StaticFilePrefix { get; set; } = "";
        public string SearchApplication { get; set; } = "";
        public string SLITrackingCode { get; set; } = "";
        public string DomainNameTrunc { get; set; }
        public string EncryptedDate { get; set; }
        public bool IsMobile { get; set; } = false;
        public List<QandA> FaqList { get; set; }
        public List<CarouselEntry> CarouselList { get; set; }
        public Dictionary<string, string> BreadcrumbTrail { get; set; }
        //public StructuredData.BreadcrumbList BreadcrumbTrail { get; set; }

        public void GetPopularPrinters(int manufacturerId = 0, int typeId = 0, int limit = 18)
        {
            string cacheKey = "PopularPrinters/" + typeId.ToString() + "/" + manufacturerId.ToString();
            PopularPrinters = DataCache.GetCache<DataTable>(cacheKey);
            if (PopularPrinters == null)
            {
                List<SqlParameter> sqlParms = new List<SqlParameter>();
                SqlParameter sqlParm = new SqlParameter("@WebsiteId", SqlDbType.Int);
                sqlParm.Value = int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString());
                sqlParms.Add(sqlParm);
                sqlParm = new SqlParameter("@ManufacturerId", SqlDbType.Int);
                sqlParm.Value = manufacturerId;
                sqlParms.Add(sqlParm);
                sqlParm = new SqlParameter("@CTypeId", SqlDbType.Int);
                sqlParm.Value = typeId;
                sqlParms.Add(sqlParm);
                sqlParm = new SqlParameter("@Limit", SqlDbType.Int);
                sqlParm.Value = limit;
                sqlParms.Add(sqlParm);
                PopularPrinters = SQL
                    .ExecuteReadStoredProcedure("netgiantmasterdata", "ngmd.GetPopularPrinters", sqlParms,
                        "popprinters").Tables[0];

                DataCache.PutCache(cacheKey, PopularPrinters);
            }
        }

        public void GetPopularCartridges(int manufacturerId, int typeId, int limit = 20)
        {
            string cacheKey = "PopularCartridges/" + typeId.ToString() + "/" + manufacturerId.ToString();
            PopularCartridges = DataCache.GetCache<DataTable>(cacheKey);
            if (PopularCartridges == null)
            {
                List<SqlParameter> sqlParms = new List<SqlParameter>();
                SqlParameter sqlParm = new SqlParameter("@CartridgeTypeID", SqlDbType.Int);
                sqlParm.Value = typeId;
                sqlParms.Add(sqlParm);
                sqlParm = new SqlParameter("@ManufacturerId", SqlDbType.Int);
                sqlParm.Value = manufacturerId;
                sqlParms.Add(sqlParm);
                sqlParm = new SqlParameter("@Count", SqlDbType.Int);
                sqlParm.Value = limit;
                sqlParms.Add(sqlParm);

                DataTable ds1 = SQL.ExecuteReadStoredProcedure("netgiantmasterdata", "ngmd.GetBestSellerIDs", sqlParms,
                    "popcartridges").Tables[0];
                string idList = "";
                if (ds1.Rows.Count > 0)
                {
                    idList = ds1.Rows[0]["IDList"].ToString();
                }

                sqlParms = new List<SqlParameter>();
                sqlParm = new SqlParameter("@WebsiteID", SqlDbType.Int);
                sqlParm.Value = int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString());
                sqlParms.Add(sqlParm);
                sqlParm = new SqlParameter("@ProductIDArray", SqlDbType.VarChar);
                sqlParm.Value = idList;
                sqlParms.Add(sqlParm);
                sqlParm = new SqlParameter("@Account", SqlDbType.VarChar);
                sqlParm.Value = HttpContext.Current.Session["U_AccountNo"] != null
                    ? HttpContext.Current.Session["U_AccountNo"].ToString()
                    : "";
                sqlParms.Add(sqlParm);
                sqlParm = new SqlParameter("@PageSize", SqlDbType.Int);
                sqlParm.Value = 200;
                sqlParms.Add(sqlParm);
                sqlParm = new SqlParameter("@PageNumber", SqlDbType.Int);
                sqlParm.Value = 1;
                sqlParms.Add(sqlParm);
                sqlParm = new SqlParameter("@productTypeID", SqlDbType.Int);
                sqlParm.Value = 0;
                sqlParms.Add(sqlParm);
                sqlParm = new SqlParameter("@manufacturerID", SqlDbType.Int);
                sqlParm.Value = 0;
                sqlParms.Add(sqlParm);
                sqlParm = new SqlParameter("@attribute8ID", SqlDbType.Int);
                sqlParm.Value = 0;
                sqlParms.Add(sqlParm);
                sqlParm = new SqlParameter("@showCompatibles", SqlDbType.Int);
                sqlParm.Value = 1;
                sqlParms.Add(sqlParm);

                PopularCartridges = SQL
                    .ExecuteReadStoredProcedure("netgiantmasterdata", "ngmd.GetSearchResults", sqlParms,
                        "searchResults").Tables[0];

                DataCache.PutCache(cacheKey, PopularCartridges);
            }
        }

        public void GetPopularRanges(int manufacturerId = 0, int typeId = 0, int limit = 18)
        {
            List<LookupNgmd> ctl = DataCache.GetCartridgeTypes();
            if (ctl.Find(x => x.AltLookupId == typeId).LookupName.Contains("Ink"))
            {
                typeId = ctl.Find(x => x.LookupName == "Ink Range").AltLookupId.Value;
            }
            else
            {
                typeId = ctl.Find(x => x.LookupName == "Toner Range").AltLookupId.Value;
            }
            string cacheKey = "PopularRanges/" + typeId.ToString() + "/" + manufacturerId.ToString();
            PopularRanges = DataCache.GetCache<DataTable>(cacheKey);
            if (PopularRanges == null)
            {
                List<SqlParameter> sqlParms = new List<SqlParameter>();
                SqlParameter sqlParm = new SqlParameter("@WebsiteId", SqlDbType.Int);
                sqlParm.Value = int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString());
                sqlParms.Add(sqlParm);
                sqlParm = new SqlParameter("@ManufacturerId", SqlDbType.Int);
                sqlParm.Value = manufacturerId;
                sqlParms.Add(sqlParm);
                sqlParm = new SqlParameter("@CTypeId", SqlDbType.Int);
                sqlParm.Value = typeId;
                sqlParms.Add(sqlParm);
                sqlParm = new SqlParameter("@Limit", SqlDbType.Int);
                sqlParm.Value = limit;
                sqlParms.Add(sqlParm);
                PopularRanges = SQL
                    .ExecuteReadStoredProcedure("netgiantmasterdata", "ngmd.GetPopularPrinters", sqlParms,
                        "popprinters").Tables[0];

                DataCache.PutCache(cacheKey, PopularRanges);
            }
        }

        public void GetBestSellers(int limit = 4)
        {
            string cacheKey = "BestSellers1";
            BestSellers1 = DataCache.GetCache<List<MiniProductEntry>>(cacheKey);
            if (BestSellers1 == null)
            {
                int type = 1; // Ink
                List<SqlParameter> sqlParms = new List<SqlParameter>();
                SqlParameter sqlParm = new SqlParameter("@WebsiteId", SqlDbType.Int);
                sqlParm.Value = int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString());
                sqlParms.Add(sqlParm);
                sqlParm = new SqlParameter("@CartridgeType", SqlDbType.Int);
                sqlParm.Value = type;
                sqlParms.Add(sqlParm);
                sqlParm = new SqlParameter("@Manufacturer", SqlDbType.Int);
                sqlParm.Value = 0;
                sqlParms.Add(sqlParm);
                sqlParm = new SqlParameter("@Limit", SqlDbType.Int);
                sqlParm.Value = limit;
                sqlParms.Add(sqlParm);

                DataTable bs1 = SQL.ExecuteReadStoredProcedure("netgiantmasterdata", "ngmd.GetBestSellers",
                    sqlParms, "bestsellers").Tables[0];
                BestSellers1 = BuildBestSeller(BestSellers1, bs1);

                DataCache.PutCache(cacheKey, BestSellers1);
            }


            cacheKey = "BestSellers2";
            BestSellers2 = DataCache.GetCache<List<MiniProductEntry>>(cacheKey);
            if (BestSellers2 == null)
            {
                int type = 2; // Toner
                List<SqlParameter> sqlParms = new List<SqlParameter>();
                SqlParameter sqlParm = new SqlParameter("@WebsiteId", SqlDbType.Int);
                sqlParm.Value = int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString());
                sqlParms.Add(sqlParm);
                sqlParm = new SqlParameter("@CartridgeType", SqlDbType.Int);
                sqlParm.Value = type;
                sqlParms.Add(sqlParm);
                sqlParm = new SqlParameter("@Manufacturer", SqlDbType.Int);
                sqlParm.Value = 0;
                sqlParms.Add(sqlParm);
                sqlParm = new SqlParameter("@Limit", SqlDbType.Int);
                sqlParm.Value = limit;
                sqlParms.Add(sqlParm);

                DataTable bs2 = SQL.ExecuteReadStoredProcedure("netgiantmasterdata", "ngmd.GetBestSellers",
                    sqlParms, "bestsellers").Tables[0];
                BestSellers2 = BuildBestSeller(BestSellers2, bs2);

                DataCache.PutCache(cacheKey, BestSellers2);
            }
        }

        private List<MiniProductEntry> BuildBestSeller(List<MiniProductEntry> lmpe, DataTable bs)
        {
            lmpe = new List<MiniProductEntry>();
            foreach (DataRow dr in bs.Rows)
            {
                MiniProductEntry mpe = new MiniProductEntry
                {
                    ProductId = int.Parse(dr["ProductID"].ToString()),
                    Url = dr["ProductURL"].ToString(),
                    ImageUrl = dr["ImageURL"].ToString(),
                    Description = dr["Description"].ToString(),
                    Availability = int.Parse(dr["Availability"].ToString()),
                    Reference = dr["ProductReference"].ToString(),
                    PartNo = dr["PartNo"].ToString(),
                    PriceRetIncVat = Convert.ToDecimal(dr["PriceRetail"]),
                    PriceTrExVat = Convert.ToDecimal(dr["PriceTrade"])
                };
                lmpe.Add(mpe);
            }
            return lmpe;
        }

        private void DoFirstTimeProcess()
        {
            IsFirstTime = true;

            string[] popupParms = new string[4];

            var controller = HttpContext.Current.Request.RequestContext.RouteData.Values["controller"].ToString().ToLower();
            var action = HttpContext.Current.Request.RequestContext.RouteData.Values["action"].ToString().ToLower();

            if ((Convert.ToBoolean(ConfigurationManager.AppSettings["PPCOEMPromoIsOn"])
                    || Convert.ToBoolean(ConfigurationManager.AppSettings["PPCCOMPPromoIsOn"]))
                && Convert.ToBoolean(HttpContext.Current.Session["U_IsFromPPC"])
                && !Convert.ToBoolean(HttpContext.Current.Session["U_IsTradeCustomer"])
                && controller != "home")
            {
                popupParms[0] = "PPCPromo";
                popupParms[1] = "PPC";
                popupParms[2] = "ppcOEMVoucher=" + (ConfigurationManager.AppSettings["PPCOEMPromoCode"] ?? "") +
                                 "&ppcOEMDiscount=" + (ConfigurationManager.AppSettings["PPCOEMPromoDisc"] ?? "0") +
                                 "&ppcCOMPVoucher=" + (ConfigurationManager.AppSettings["PPCCOMPPromoCode"] ?? "") +
                                 "&ppcCOMPDiscount=" + (ConfigurationManager.AppSettings["PPCCOMPPromoDisc"] ?? "0");

                FirstTimeMessage = Convert.ToString(CommonData["PPCPromoMobile"])
                    .Replace("ppcOEMVoucher", (ConfigurationManager.AppSettings["PPCOEMPromoCode"] ?? ""))
                    .Replace("ppcOEMDiscount", (ConfigurationManager.AppSettings["PPCOEMPromoDisc"] ?? "0"))
                    .Replace("ppcCOMPVoucher", (ConfigurationManager.AppSettings["PPCCOMPPromoCode"] ?? ""))
                    .Replace("ppcCOMPDiscount", (ConfigurationManager.AppSettings["PPCCOMPPromoDisc"] ?? "0"));
            }
            else
            {
                if (Convert.ToBoolean(ConfigurationManager.AppSettings["AffiliatePromoIsOn"])
                    && HttpContext.Current.Session["U_AffiliateNo"].ToString() != ""
                    && !Convert.ToBoolean(HttpContext.Current.Session["U_IsTradeCustomer"]))
                {
                    string[] affiliateNos = HttpContext.Current.Session["U_AffiliateNo"].ToString().Split(',');
                    foreach (string affiliateNo in affiliateNos)
                    {
                        if (ConfigurationManager.AppSettings["AffPromo"].Contains(affiliateNo + "~"))
                        {
                            HttpContext.Current.Session["U_AffiliateNo"] = affiliateNo;
                            FirstTimeMessage = Convert.ToString(CommonData["AffiliatePromoMobile"]);
                            string[] aff1 = ConfigurationManager.AppSettings["AffPromo"].Split('#');
                            string repl = "";
                            foreach (string aff2 in aff1)
                            {
                                string[] aff3 = aff2.Split('~');
                                if (aff3[0] == affiliateNo)
                                {
                                    repl = "affName=" + aff3[1] + "&affVoucher=" + aff3[2] + "&affMessage=" + aff3[3];

                                    FirstTimeMessage.Replace("affName", aff3[1])
                                                    .Replace("affVoucher", aff3[2])
                                                    .Replace("affMessage", aff3[3]);
                                    break;
                                }
                            }
                            popupParms[0] = "AffiliatePromo";
                            popupParms[1] = "Affiliate";
                            popupParms[2] = repl;
                            break;
                        }
                    }
                }
                if ((IsOEMSaleActive || IsCompatibleSaleActive || IsStationerySaleActive)
                    && !Convert.ToBoolean(HttpContext.Current.Session["U_IsTradeCustomer"]))
                {
                    popupParms[0] = "SalePromo";
                    popupParms[1] = "Sale";
                    popupParms[2] = "oemVoucher=" + Utilities.GetItemFromDict(SaleData, "OEMVoucher") +
                        "&compVoucher=" + Utilities.GetItemFromDict(SaleData, "CompatibleVoucher") +
                        "&statVoucher=" + Utilities.GetItemFromDict(SaleData, "StationeryVoucher") +
                        "&oemDiscount=" + OEMDiscount +
                        "&compDiscount=" + CompatibleDiscount +
                        "&statDiscount=" + StationeryDiscount;

                    FirstTimeMessage = Convert.ToString(CommonData["SalePromoMobile"]);
                }
            }

            SaveReturn sr = GetPopup(popupParms[0], "firsttimepopup", "md", popupParms[2]);
            if (sr.IsSuccess)
            {
                FirstTimePopup = sr.Html;
            }

            string cookieConsent = HttpContext.Current.Request.Cookies["__cc"] == null
                ? ""
                : HttpContext.Current.Request.Cookies["__cc"].Value;
            HttpContext.Current.Session["U_CookieConsentAccepted"] = false;
            if (Boolean.TryParse(cookieConsent, out _))
            {
                HttpContext.Current.Session["U_CookieConsentAccepted"] = true;
            }
        }

        public void GetCartridgeTypes()
        {
            CartridgeTypes = DataCache.GetCartridgeTypes();
        }

        public void GetMeta()
        {
            string title = ConfigurationManager.AppSettings["MetaTitle"];
            string desc = ConfigurationManager.AppSettings["MetaDescription"];
            GetMeta(title, desc);
        }

        public void GetMeta(string title, string desc)
        {
            MetaData = new Dictionary<string, string>();
            MetaData.Add("Title", title);
            MetaData.Add("Description", desc);
        }

        public string CommonDataLookup(string name)
        {
            var returnValue = "";

            if (CommonData.ContainsKey(name))
            {
                returnValue = CommonData[name];
            }

            return returnValue;
        }

        public string BuildBreadcrumbJson(bool showHome = true)
        {
            string domain = "https://" + ConfigurationManager.AppSettings["DomainName_Live"];
            string json =
                "{" +
                "\"@context\":\"http://schema.org\"," +
                "\"@type\":\"BreadcrumbList\"," +
                "\"itemListElement\":[";
            string comma = "";
            int i = 1;
            foreach (var bc in BreadcrumbTrail)
            {
                if (bc.Key != "Home" || (bc.Key == "Home" && showHome))
                {
                    json +=
                        comma + "{" +
                        "\"@type\":\"ListItem\"," +
                        "\"item\":{" + // Required
                        "\"@type\":\"webPage\"," +
                        "\"@id\":" + JsonConvert.ToString(domain + bc.Value) + "," +    // Required
                        "\"name\":" + JsonConvert.ToString(bc.Key) + "" +               // Required
                        "}," +
                        "\"position\":" + i.ToString() +                                // Required
                        "}";
                    comma = ", ";
                    i += 1;
                }
            }
            json +=
                "]" +
                "}";

            return json;
        }

        public string BuildFaqJson()
        {
            string json = "";
            List<QandA> schemaList = FaqList.Where(x => x.GenerateSchema == true).ToList();
            // FAQ
            if (schemaList.Count > 0)
            {
                json += ",{\"@context\" : \"http://schema.org\"," +
                    "\"@type\" : \"FAQPage\"," +
                    "\"mainEntity\" : [";

                string comma = "";
                foreach (var faq in schemaList)
                {
                    json += comma +
                    "{\"@type\": \"Question\"," +
                        "\"name\": \"" + Regex.Replace(faq.Question, @"\r\n?|\n", "").Replace("\"", "\\\"") + "\"," +
                        "\"acceptedAnswer\": {" +
                            "\"@type\": \"Answer\"," +
                            "\"text\": \"" + Regex.Replace(faq.Answer, @"\r\n?|\n", "").Replace("\"", "\\\"") + "\"" +
                        "}}";
                    comma = ",";
                }
                json += "]}";
            }

            return json;
        }

        public SaveReturn GetPopup(string popupname, string popupid, string popupwidth, string replacements)
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = false;
            if (string.IsNullOrEmpty(popupname))
            {
                return sr;
            }

            popupwidth = popupwidth == "" ? "md" : popupwidth;
            Dictionary<string, string> dict = DataCache.GetSectionData("PopupData");
            string html = "";
            if (dict.ContainsKey(popupname))
            {
                html = dict[popupname].ToString();
                sr.IsSuccess = true;
            }

            replacements = Utilities.AddStandardReplacements(replacements);
            if (replacements != "")
            {
                string[] a = replacements.Split('&');
                foreach (string b in a)
                {
                    string[] c = b.Split('=');
                    html = html.Replace("[" + c[0] + "]", c[1]);
                }
            }

            // Create Modal
            sr.Html = @"<section class=""modal fade"" id=""" + popupid +
                      @""" tabindex=""-1\"" role=""dialog"" aria-labelledby=""myModalLabel"">
                        <div class=""modal-dialog modal-" + popupwidth + @""" role=""document"">
                            <div class=""modal-content"">" + HttpUtility.UrlDecode(html) + @"</div>
                        </div>
                    </section>";

            return sr;
        }

    }
}