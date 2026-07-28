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
            IsCompatibleSaleActive = Convert.ToBoolean(Utilities.GetItemFromDict(CommonData, "IsCompatibleSaleActive"));
            IsOEMSaleActive = Convert.ToBoolean(Utilities.GetItemFromDict(CommonData, "IsOEMSaleActive"));
            IsStationerySaleActive = Convert.ToBoolean(Utilities.GetItemFromDict(CommonData, "IsStationerySaleActive"));
            ShowCustomerAlert = ConfigurationManager.AppSettings["AlertLevel"] == "4" ? true : Convert.ToBoolean(Utilities.GetItemFromDict(CommonData, "ShowCustomerAlert"));
            IsInMaintenanceMode = Convert.ToBoolean(EntityAccess.ReadCms(x => x.cmsSection.sectionName == "CommonData"
                && x.entryName == "IsInMaintenanceMode")
                .FirstOrDefault().cmsContent);

            var controller = HttpContext.Current.Request.RequestContext.RouteData.Values["controller"].ToString().ToLower();
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
            Menu = DataCache.GetMenu();
            VatMultiplier = Convert.ToDecimal(ConfigurationManager.AppSettings["VatMultiplier"].ToString());
            BasketContents = new List<BasketContents>();
            if (HttpContext.Current.Session["B_BasketArray"] != null)
            {
                BasketContents = (List<BasketContents>) HttpContext.Current.Session["B_BasketArray"];
            }
            GetMeta();
            if (ConfigurationManager.AppSettings["Environment"].ToString() == "Live")
            {
                IsLiveSystem = true;
            }
            SearchApplication = ConfigurationManager.AppSettings["SearchApplication"];
            TitleList = Utilities.BuildTitleList();

            if (!string.IsNullOrEmpty(HttpContext.Current.Request.QueryString["utm_medium"]))
            {
                HttpContext.Current.Session["U_AffiliateNo"] = HttpContext.Current.Request.QueryString["utm_campaign"] ?? "";
            }

            // First Time Processing
            IsFirstTime = false;
            if (Convert.ToBoolean(HttpContext.Current.Session["U_IsFirstTime"]))
            {
                HttpContext.Current.Session.Remove("U_IsFirstTime");
                DoFirstTimeProcess();
            }

            EncryptedDate = Utilities.SimpleEncryptString(DateTime.Now.ToUniversalTime().ToString("u"));
        }

        public Dictionary<string, string> CommonData { get; set; }
        public Dictionary<string, string> FeeFoScore { get; set; }
        public Dictionary<string, string> SaleData { get; set; }
        public Dictionary<string, string> MetaData { get; set; }
        public List<eqCartridgeType> CartridgeTypes { get; set; }
        public DataTable PopularPrinters { get; set; }
        public DataTable PopularCartridges { get; set; }
        public SignIn SignIn { get; set; }
        public SignUp SignUp { get; set; }
        public decimal VatMultiplier { get; set; }
        public List<BasketContents> BasketContents { get; set; }
        public bool IsLiveSystem { get; set; } = false;
        public string Menu { get; set; }
        public bool IsCompatibleSaleActive { get; set; }
        public bool IsOEMSaleActive { get; set; }
        public bool IsStationerySaleActive { get; set; }
        public string OEMSaleType { get; set; }
        public string CompatibleSaleType { get; set; }
        public bool IsInMaintenanceMode { get; set; }
        public bool ShowCustomerAlert { get; set; }
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
                    PriceTrIncVat = Convert.ToDecimal(dr["PriceTrade"])
                };
                lmpe.Add(mpe);
            }
            return lmpe;
        }

        private void DoFirstTimeProcess()
        {
            IsFirstTime = true;
            if (IsOEMSaleActive || IsCompatibleSaleActive || IsStationerySaleActive)
            {
                FirstTimePopup = "SalePromo~Sale~oemVoucher=" + Utilities.GetItemFromDict(SaleData, "OEMVoucher") + "&compVoucher=" + Utilities.GetItemFromDict(SaleData, "CompatibleVoucher") + "&statVoucher=" + Utilities.GetItemFromDict(SaleData, "StationeryVoucher") + 
                    " &oemDiscount=" + OEMDiscount + "&compDiscount=" + CompatibleDiscount + "&statDiscount=" + StationeryDiscount;

                FirstTimeMessage = Convert.ToString(CommonData["SalePromoMobile"]);
            }
            else
            {
                var controller = HttpContext.Current.Request.RequestContext.RouteData.Values["controller"].ToString().ToLower();
                var action = HttpContext.Current.Request.RequestContext.RouteData.Values["action"].ToString().ToLower();

                if (Convert.ToBoolean(ConfigurationManager.AppSettings["PPCPromoIsOn"]) &&
                    Convert.ToBoolean(HttpContext.Current.Session["U_IsFromPPC"]) &&
                    controller != "home")
                {
                    FirstTimePopup = "PPCPromo~PPC~ppcVoucher=" + ConfigurationManager.AppSettings["PPCPromoCode"] +
                                     "&ppcDiscount=" + ConfigurationManager.AppSettings["PPCPromoDisc"];

                    FirstTimeMessage = Convert.ToString(CommonData["PPCPromoMobile"])
                        .Replace("ppcVoucher", ConfigurationManager.AppSettings["PPCPromoCode"])
                        .Replace("ppcDiscount", ConfigurationManager.AppSettings["PPCPromoDisc"]);
                }
                if (Convert.ToBoolean(ConfigurationManager.AppSettings["AffiliatePromoIsOn"]) &&
                    HttpContext.Current.Session["U_AffiliateNo"].ToString() != "")
                {
                    if (ConfigurationManager.AppSettings["AffPromo"]
                        .Contains(HttpContext.Current.Session["U_AffiliateNo"].ToString() + "~"))
                    {
                        FirstTimeMessage = Convert.ToString(CommonData["AffiliatePromoMobile"]);
                        string[] aff1 = ConfigurationManager.AppSettings["AffPromo"].Split('#');
                        string repl = "";
                        foreach (string aff2 in aff1)
                        {
                            string[] aff3 = aff2.Split('~');
                            if (aff3[0] == HttpContext.Current.Session["U_AffiliateNo"].ToString())
                            {
                                repl = "affName=" + aff3[1] + "&affVoucher=" + aff3[2] + "&affMessage=" + aff3[3];

                                FirstTimeMessage.Replace("affName", aff3[1])
                                                .Replace("affVoucher", aff3[2])
                                                .Replace("affMessage", aff3[3]);
                                break;
                            }
                        }
                        FirstTimePopup = "AffiliatePromo~Affiliate~" + repl;
                    }
                }
            }
        }

        public void GetCartridgeTypes()
        {
            CartridgeTypes = DataCache.GetCartridgeTypes();
        }

        public void GetMeta()
        {
            string title = EntityAccess
                .ReadConfigurationSetting(x => x.sectionName == "Website Other Variables" &&
                                               x.settingName == "General Meta Title").First().settingValue;
            string desc = EntityAccess
                .ReadConfigurationSetting(x => x.sectionName == "Website Other Variables" &&
                                               x.settingName == "General Meta Description").First().settingValue;
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
    }
}