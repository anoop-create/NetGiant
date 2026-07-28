using netGiant.Intranet.DataLayer.CustomerData;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Google.Apis.Util;

namespace ngBatchProcesses.BusinessObjects.Shared
{
    public class EntityFunctions
    {
        #region Attributes
        public static List<Searchable> GetAttribute(string manufacturer, string partNo)
        {
            using (var db = new ngmdEntities())
            {
                // combine or and ng searchables
                return
                (from t1 in db.pim_products
                 join t2 in db.pim_attributes
                     on t1.prodID equals t2.prodID
                 where t1.manufacturer == manufacturer && t1.partno == partNo
                 select new Searchable()
                 {
                     Name = t2.name,
                     Value = t2.value
                 })
                .Union
                (from t1 in db.ng_products
                 join t2 in db.ng_searchables
                     on t1.prodID equals t2.prodID
                 where t1.manufacturer == manufacturer && t1.partno == partNo
                 select new Searchable()
                 {
                     Name = t2.name,
                     Value = t2.value
                 })
                .ToList();
            }
        }
        #endregion

        #region AxisValueLookup
        public static List<AxisValueLookup> GetAxisValueLookup(Expression<Func<AxisValueLookup, bool>> where)
        {
            using (var db = new ngmdEntities())
            {
                return db.AxisValueLookup
                    .Where(where)
                    .ToList();
            }
        }
        #endregion

        #region Back Order
        public static List<BackOrder> GetBackOrder(Expression<Func<BackOrder, bool>> where)
        {
            using (var db = new ngmdEntities())
            {
                return db.BackOrder
                    .Include(x => x.BackOrderItem)
                    .Include(x => x.Website)
                    .Where(where)
                    .ToList();
            }
        }

        public static List<BackOrderItem> GetBackOrderItem(Expression<Func<BackOrderItem, bool>> where)
        {
            using (var db = new ngmdEntities())
            {
                return db.BackOrderItem
                    .Where(where)
                    .ToList();
            }
        }

        public static bool SaveBackOrder(BackOrder bo)
        {
            bool isSuccess = true;
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    if (bo.BackOrderId > 0)
                    {
                        db.Entry(bo).State = EntityState.Modified;
                    }
                    else
                    {
                        db.Entry(bo).State = EntityState.Added;
                    }
                    db.SaveChanges();
                }
            }
            catch
            {
                isSuccess = false;
            }
            return isSuccess;
        }

        public static bool SaveBackOrderItem(BackOrderItem boi)
        {
            bool isSuccess = true;
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    if (boi.BackOrderItemId > 0)
                    {
                        db.Entry(boi).State = EntityState.Modified;
                    }
                    else
                    {
                        db.Entry(boi).State = EntityState.Added;
                    }
                    db.SaveChanges();
                }
            }
            catch
            {
                isSuccess = false;
            }
            return isSuccess;
        }
        #endregion

        #region Batch Log
        public static List<BatchLog> GetBatchLog(Expression<Func<BatchLog, bool>> where)
        {
            using (var db = new ngmdEntities())
            {
                return db.BatchLog
                    .Include(x => x.BatchLogDetail)
                    .Where(where)
                    .ToList();
            }
        }

        public static bool SaveBatchLog(BatchLog bl)
        {
            bool isSuccess = true;
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    if (bl.BatchLogId > 0)
                    {
                        db.Entry(bl).State = EntityState.Modified;
                    }
                    else
                    {
                        db.Entry(bl).State = EntityState.Added;
                    }
                    db.SaveChanges();
                }
            }
            catch
            {
                isSuccess = false;
            }
            return isSuccess;
        }

        public static bool SaveBatchLogDetail(BatchLogDetail bld)
        {
            bool isSuccess = true;
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    if (bld.BatchLogDetailId > 0)
                    {
                        db.Entry(bld).State = EntityState.Modified;
                    }
                    else
                    {
                        db.Entry(bld).State = EntityState.Added;
                    }
                    db.SaveChanges();
                }
            }
            catch
            {
                isSuccess = false;
            }
            return isSuccess;
        }
        #endregion

        #region CategoryCode/SecondaryCategoryCode
        public static List<categoryCode> GetCategoryCodeList(Expression<Func<categoryCode, bool>> where)
        {
            using (var db = new ngmdEntities())
            {
                return db.categoryCode
                    .Include("secondaryCategoryLookup.websiteInventory.product")
                    .Include("websiteInventory.product")
                    .Where(where)
                    .ToList();
            }
        }

        public static List<secondaryCategoryLookup> GetSecondaryCategoryList(Expression<Func<secondaryCategoryLookup, bool>> where)
        {
            using (var db = new ngmdEntities())
            {
                return db.secondaryCategoryLookup
                    .Where(where)
                    .ToList();
            }
        }
        #endregion

        #region CmsEntry/Section
        public static string GetNgmdCMSEntry(int websiteId, string sectionName, string entryName, Dictionary<string, string> replacements = null)
        {
            // Please note that the 'Events' model is not supported within the batch system. This means
            // that temporary redirects for CMS Entries will not be honoured.

            cmsEntry entry = new cmsEntry();
            string s = "";

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    int sectionId = db.cmsSection
                        .Where(x => x.sectionName == sectionName && x.websiteFK == websiteId).FirstOrDefault().cmsSectionID;
                    entry = db.cmsEntry
                        .Where(x => x.entryName == entryName && x.cmsSectionFK == sectionId)
                        .FirstOrDefault();
                    s = entry.cmsContent;
                }
                if (replacements != null)
                {
                    foreach (KeyValuePair<string, string> kvp in replacements)
                    {
                        s = s.Replace(kvp.Key, kvp.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                throw (new ApplicationException(ex.Message));
            }

            return s;
        }

        public static Dictionary<string, string> GetAllCmsEntry(int websiteId, string sectionName)
        {
            // Please note that the 'Events' model is not supported within the batch system. This means
            // that temporary redirects for CMS Entries will not be honoured.

            List<cmsEntry> lcms = new List<cmsEntry>();
            Dictionary<string, string> sectionData = new Dictionary<string, string>();

            using (ngmdEntities db = new ngmdEntities())
            {
                lcms = db.cmsEntry
                    .Where(x => x.cmsSection.sectionName == sectionName && x.cmsSection.websiteFK == websiteId)
                    .ToList();
            }

            foreach (cmsEntry cms in lcms)
            {
                sectionData.Add(cms.entryName, cms.cmsContent);
            }

            return sectionData;
        }

        // Now obsolete
        public static string GetCMSEntry(int websiteId, int seriesId, int entryId, string entryType)
        {
            //Retrieve website variables
            string siteRoot;
            string siteRootSecure;
            string siteRootPlusVn;

            try
            {
                if (WebsiteConfigSettings == null)
                {
                    SetSiteConfigSettings(websiteId);
                }
                siteRoot = "http://" + WebsiteConfigSettings.Where(x => x.settingName == "siteRoot").FirstOrDefault().settingValue;
                siteRootSecure = siteRoot.Replace("http://", "https://");
                siteRootPlusVn = siteRoot + "version" + WebsiteConfigSettings.Where(x => x.settingName == "ResourceVersion").FirstOrDefault().settingValue + "/";
            }
            catch (Exception ex)
            {
                throw (new ApplicationException(ex.Message));
            }

            string html = "";
            List<string> cmsResults;

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    cmsResults = db.GetCMSEntry(
                        WebsiteConfigSettings.Where(x => x.settingName == "site").FirstOrDefault().settingValue,
                        seriesId,
                        entryId,
                        entryType
                    ).ToList();
                }

                //Loop through the string array and merge the records
                foreach (string cmsRow in cmsResults)
                {
                    html += cmsRow;
                }
            }
            catch (Exception ex)
            {
                throw (new ApplicationException(ex.Message));
            }

            //Deal with replacements
            html = ReplacePlaceholders(siteRoot, siteRootSecure, siteRootPlusVn, html);

            return html;
        }
        #endregion

        #region ConfigurationSetting
        public static List<configurationSetting> GetConfigurationSettings(Expression<Func<configurationSetting, bool>> where)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                return db.configurationSetting
                         .Where(where)
                         .ToList();
            }
        }

        public static string GetConfigurationSetting(string sectionName, string settingName, int websiteFK)
        {
            string settingValue = string.Empty;

            configurationSetting cs = GetConfigurationSettings(x => x.sectionName.ToLower() == sectionName.ToLower() &&
                        x.settingName.ToLower() == settingName.ToLower() &&
                        x.websiteFK == websiteFK).FirstOrDefault();
            if (cs != null)
            {
                settingValue = cs.settingValue;
            }

            return settingValue;
        }

        public static bool SaveConfigurationSetting(configurationSetting cs)
        {
            bool isSuccess = true;
            cs.dateLastUpdate = DateTime.Now;
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    if (cs.configurationSettingID > 0)
                    {
                        db.Entry(cs).State = EntityState.Modified;
                    }
                    else
                    {
                        db.Entry(cs).State = EntityState.Added;
                    }
                    db.SaveChanges();
                }
            }
            catch
            {
                isSuccess = false;
            }
            return isSuccess;
        }

        public static List<configurationSetting> WebsiteConfigSettings { get; set; }

        public static void SetSiteConfigSettings(int websiteId)
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    WebsiteConfigSettings = db.configurationSetting
                            .Where(x => x.websiteFK == websiteId &&
                                x.sectionName == "Website Application Variables")
                            .ToList();
                }
            }
            catch (Exception ex)
            {
                throw (new ApplicationException(ex.Message));
            }
        }
        #endregion

        #region EqEquipment/EqProductMembership
        public static List<eqEquipment> GetEquipmentList(Expression<Func<eqEquipment, bool>> where)
        {
            using (var db = new ngmdEntities())
            {
                return db.eqEquipment
                    .Include(x => x.manufacturer)
                    .Include(x => x.eqProductMembership)
                    .Where(where)
                    .ToList();
            }
        }

        public static List<MenuManu> GetEquipmentManufacturers(Expression<Func<eqEquipment, bool>> where)
        {
            using (var db = new ngmdEntities())
            {
                return (db.eqEquipment
                    .Include(y => y.manufacturer)
                    .Where(where)
                    .Select(x => new MenuManu()
                    {
                        Item1 = x.manufacturer.manufacturerName,
                        Item2 = db.Lookup.FirstOrDefault(y => y.LookupType.LookupTypeName == "CartridgeType" && y.AltLookupId == x.eqCartridgeTypeFK).LookupName,
                        Item3 = x.manufacturer.axisManufacturerNo
                    })
                    .Distinct()
                    .ToList())
                .OrderBy(y => y.Item1)
                .ToList();
            }
        }
        public static List<eqProductMembership> GetProductMembershipList(Expression<Func<eqProductMembership, bool>> where)
        {
            using (var db = new ngmdEntities())
            {
                return db.eqProductMembership
                    .Include(x => x.product.websiteInventory)
                    .Where(where)
                    .ToList();
            }
        }

        #endregion

        #region Event
        public static List<Event> GetEvent(Expression<Func<Event, bool>> where)
        {
            using (var db = new ngmdEntities())
            {
                return db.Event
                    .Include(x => x.Website)
                    .Where(where)
                    .ToList();
            }
        }
        #endregion

        #region FtpDetails/FieldMappings
        public static List<ftpDetails> GetFtpDetails(Expression<Func<ftpDetails, bool>> where)
        {
            using (var db = new ngmdEntities())
            {
                return db.ftpDetails
                    .Where(where)
                    .ToList();
            }
        }
        public static bool SaveFtpDetails(ftpDetails ftpDetails)
        {
            bool success = true;

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    if (ftpDetails.ftpDetailID > 0)
                    {
                        db.Entry(ftpDetails).State = EntityState.Modified;
                    }
                    else
                    {
                        db.Entry(ftpDetails).State = EntityState.Added;
                    }

                    db.SaveChanges();
                }
            }
            catch
            {
                success = false;
            }

            return success;
        }
        public static List<KeyValuePair<string, string>> GetFieldMappings(Expression<Func<fieldMapping, bool>> where)
        {
            List<KeyValuePair<string, string>> l;

            using (ngmdEntities db = new ngmdEntities())
            {
                l = db.fieldMapping
                    .Where(where)
                    .Select(x => new
                    {
                        x.fieldMappingTo,
                        x.fieldMappingWith
                    })
                    .AsEnumerable()
                    .Select(x => new KeyValuePair<string, string>(
                        x.fieldMappingTo,
                        x.fieldMappingWith
                    ))
                    .ToList();
            }

            return l;
        }
        #endregion

        #region Interim Orders
        public static List<InterimOrder> GetInterimOrder(Expression<Func<InterimOrder, bool>> where)
        {
            using (var db = new ngmdEntities())
            {
                return db.InterimOrder
                    .Where(where)
                    .ToList();
            }
        }

        public static bool SaveInterimOrder(InterimOrder io)
        {
            bool isSuccess = true;
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    if (io.InterimOrderId > 0)
                    {                    
                        db.Entry(io).State = EntityState.Modified;
                    }
                    else
                    {
                        db.Entry(io).State = EntityState.Added;
                    }
                    db.SaveChanges();
                }
            }
            catch
            {
                isSuccess = false; 
            }
            return isSuccess;
        }
        #endregion

        #region Lookup
        public static List<netGiant.Intranet.DataLayer.NetgiantMasterData.Lookup> GetNgmdLookup(Expression<Func<netGiant.Intranet.DataLayer.NetgiantMasterData.Lookup, bool>> where)
        {
            List<netGiant.Intranet.DataLayer.NetgiantMasterData.Lookup> l;

            using (ngmdEntities db = new ngmdEntities())
            {
                l = db.Lookup
                    .Where(where)
                    .ToList();
            }

            return l;
        }

        public static List<netGiant.Intranet.DataLayer.CustomerData.Lookup> GetCustLookup(Expression<Func<netGiant.Intranet.DataLayer.CustomerData.Lookup, bool>> where)
        {
            List<netGiant.Intranet.DataLayer.CustomerData.Lookup> l;

            using (customerEntities db = new customerEntities())
            {
                l = db.Lookup
                    .Where(where)
                    .ToList();
            }

            return l;
        }
        #endregion

        #region Manufacturer/SupplierManuMappings
        public static List<manufacturer> GetManufacturers(Expression<Func<manufacturer, bool>> where)
        {
            using (var db = new ngmdEntities())
            {
                return db.manufacturer
                    .Where(where)
                    .ToList();
            }
        }

        public static List<supplierManuMapping> GetSupplierManuMappings(Expression<Func<supplierManuMapping, bool>> where)
        {
            using (var db = new ngmdEntities())
            {
                return db.supplierManuMapping
                    .Where(where)
                    .ToList();
            }
        }
        #endregion

        #region MfpnExtensions
        public static List<mfpnExtensions> GetMfpnExtensions(Expression<Func<mfpnExtensions, bool>> where)
        {
            using (var db = new ngmdEntities())
            {
                return db.mfpnExtensions
                    .Where(where)
                    .ToList();
            }
        }
        #endregion

        #region Pimberly Products
        public static List<pim_products> GetPimberlyProduct(Expression<Func<pim_products, bool>> where)
        {
            using (var db = new ngmdEntities())
            {
                return db.pim_products
                    .Where(where)
                    .ToList();
            }
        }
        #endregion

        #region Product
        public static List<product> GetProduct(Expression<Func<product, bool>> where)
        {
            using (var db = new ngmdEntities())
            {
                return db.product
                    .Include(x => x.manufacturer)
                    .Include(x => x.AxisFields.AxisFieldsAdditional)
                    .Include(x => x.productGroup)
                    //.Include("crossSellingLink.product1.manufacturer")
                    //.Include("crossSellingLink1.product")
                    .Include(x => x.websiteInventory)
                    .Where(where)
                    .ToList();
            }
        }
        #endregion

        #region Provider
        public static List<provider> GetProviderList(Expression<Func<provider, bool>> whereProvider, string LookupName)
        {
            using (var db = new ngmdEntities())
            {
                int? lookupId = db.Lookup
                     .Where(x => x.LookupName == LookupName && x.LookupType.LookupTypeName == "ProviderType")
                     .FirstOrDefault().AltLookupId;

                return db.provider
                    .Include(x => x.ftpDetails)
                    .Include(x => x.fieldMapping)
                    .Where(whereProvider)
                    .Where(x => x.providerTypeFK == lookupId)
                    .OrderBy(x => x.providerName)
                    .ToList();
            }
        }

        public static List<provider> GetProvider(Expression<Func<provider, bool>> where)
        {
            using (var db = new ngmdEntities())
            {
                return db.provider
                    .Include(x => x.ftpDetails)
                    .Include(x => x.fieldMapping)
                    .Where(where)
                    .OrderBy(x => x.providerName)
                    .ToList();
            }
        }
        #endregion

        #region SagePayToken
        public static List<SagePayTokens> GetSagePayTokens(Expression<Func<SagePayTokens, bool>> where)
        {
            using (var db = new ngmdEntities())
            {
                return db.SagePayTokens
                    .Where(where)
                    .ToList();
            }
        }

        public static void DeleteSagePayToken(SagePayTokens obj)
        {
            try
            {
                using (var db = new ngmdEntities())
                {
                    db.Entry(obj).State = EntityState.Deleted;
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {

            }
        }
        #endregion

        #region SalesForceBatchJob
        public static List<SalesforceBatchJob> GetSalesForceBatchJob(Expression<Func<SalesforceBatchJob, bool>> where)
        {
            using (var db = new ngmdEntities())
            {
                return db.SalesforceBatchJob
                    .Where(where)
                    .ToList();
            }
        }

        public static bool SaveSalesForceBatchJob(SalesforceBatchJob s)
        {
            bool success = true;

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    SalesforceBatchJob job = db.SalesforceBatchJob.FirstOrDefault(x => x.JobId == s.JobId);
                    if (job == null)
                    {
                        db.Entry(s).State = EntityState.Added;
                    }

                    db.SaveChanges();
                }
            }
            catch (Exception)
            {
                success = false;
            }

            return success;
        }
        #endregion

        #region VoucherPromo
        public static VoucherPromo GetVoucher(Expression<Func<VoucherPromo, bool>> where)
        {
            VoucherPromo vp;

            using (ngmdEntities db = new ngmdEntities())
            {
                vp = db.VoucherPromo
                    .Where(where).FirstOrDefault();
            }

            return vp;
        }
        public static List<VoucherPromo> GetVouchers(Expression<Func<VoucherPromo, bool>> where)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                return db.VoucherPromo
                    .Include(x => x.Website)
                    .Where(where).ToList();
            }
        }
        public static bool SaveVoucher(VoucherPromo voucher)
        {
            bool success = true;

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    if (voucher.VoucherPromoId > 0)
                    {
                        db.Entry(voucher).State = EntityState.Modified;
                    }
                    else
                    {
                        db.Entry(voucher).State = EntityState.Added;
                    }

                    db.SaveChanges();
                }
            }
            catch
            {
                success = false;
            }

            return success;
        }
        public static bool VoucherExists(Expression<Func<VoucherPromo, bool>> where)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                if (db.VoucherPromo.Any(where))
                {
                    return true;
                }
                return false;
            }
        }
        #endregion

        #region Website
        public static List<Website> GetAllWebsites()
        {
            using (var db = new ngmdEntities())
            {
                return db.Website.Where(x => x.FriendlyName != "Intranet").ToList();
            }
        }
        public static List<Website> GetWebsiteList(Expression<Func<Website, bool>> where)
        {
            using (var db = new ngmdEntities())
            {
                return db.Website
                    .Where(where)
                    .OrderBy(x => x.WebsiteID)
                    .ToList();
            }
        }
        #endregion

        #region WebsiteInventory
        public static List<websiteInventory> GetWebsiteInventoryList(Expression<Func<websiteInventory, bool>> where, bool shortlist = false)
        {
            using (var db = new ngmdEntities())
            {
                if (shortlist)
                {
                    return db.websiteInventory
                        .Where(where)
                        .ToList();
                }
                else
                {
                    db.Database.CommandTimeout = (3 * 60);
                    return db.websiteInventory
                        .Include(x => x.productPrice)
                        .Include(x => x.product.manufacturer)
                        .Include(x => x.product.AxisFields.AxisFieldsAdditional)
                        .Include(x => x.product.productGroup)
                        .Include(x => x.product.crossSellingLink)
                        .Include(x => x.product.crossSellingLink1)
                        .Where(where)
                        .ToList();
                }
            }
        }
        public static List<websiteInventory> GetCategoryMembershipList(Expression<Func<websiteInventory, bool>> where)
        {
            using (var db = new ngmdEntities())
            {
                return db.websiteInventory
                    .Where(where)
                    .ToList();
            }
        }

        #endregion

        #region Shared
        public static int TruncateTable(string tableName)
        {
            using (var db = new ngmdEntities())
            {
                return db.Database.ExecuteSqlCommand("TRUNCATE TABLE ngmd." + tableName);
            }
        }

        #endregion

        #region Private Functions
        private static string ReplacePlaceholders(string siteRoot, string siteRootSecure, string siteRootPlusVn, string html)
        {
            if (html.Contains("{"))
            {
                string pattern1 = @"\{ResourceURL,(.*)\}";
                string replacement1 = "$1";

                html = html.Replace("{SiteRoot}", siteRoot);
                html = html.Replace("{SiteRootSecure}", siteRootSecure);
                html = Regex.Replace(html, pattern1, siteRootPlusVn + replacement1);
            }
            return html;
        }
        #endregion

    }
}
