using DataAccess.EntityFramework;
using MailChimp.Net.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Ocsp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Data.Entity.SqlServer;
using System.Linq;
using System.Linq.Expressions;
using System.Web.Mvc;
using static BusinessLogic.MyOpayo;
using Customer = DataAccess.EntityFramework.Customer;

namespace BusinessLogic
{
    public class EntityAccess
    {
        /// <summary>
        /// Read product entities
        /// </summary>
        /// <param name="where"></param>
        /// <returns></returns>
        public static List<product> ReadProduct(Expression<Func<product, bool>> where)
        {
            List<product> ret = new List<product>();

            try
            {
                using (Ngmd db = new Ngmd())
                {
                    ret = db.product
                        .Where(where)
                        .Include(x => x.AxisFields)
                        .Include(x => x.websiteInventory.Select(y => y.productImages))
                        .Include(x => x.productGroup)
                        .Where(where)
                        .ToList();

                }
            }
            catch (Exception e)
            {
                Utilities.ProcessException(e);
            }
            return ret;
        }

        /// <summary>
        /// Read AxisFieldsAdditional entities
        /// </summary>
        /// <param name="where"></param>
        /// <returns></returns>
        public static List<AxisFieldsAdditional> ReadAxisFieldsAdditional(Expression<Func<AxisFieldsAdditional, bool>> where)
        {
            List<AxisFieldsAdditional> ret = new List<AxisFieldsAdditional>();
            int w = int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString());

            try
            {
                using (Ngmd db = new Ngmd())
                {
                    ret = db.AxisFieldsAdditionals
                        .Where(where)
                        .Where(x => x.websiteFK == w)
                        .ToList();
                }
            }
            catch (Exception e)
            {
                Utilities.ProcessException(e);
            }
            return ret;
        }

        public static List<websiteInventory> ReadWebsiteInventory(Expression<Func<websiteInventory, bool>> where)
        {
            int w = int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString());
            return ReadWebsiteInventory(where, w);
        }

        public static List<websiteInventory> ReadWebsiteInventory(Expression<Func<websiteInventory, bool>> where, int websiteid)
        {
            List<websiteInventory> ret = new List<websiteInventory>();

            try
            {
                using (Ngmd db = new Ngmd())
                {
                    ret = db.websiteInventories
                        .Include(el => el.product.AxisFields.AxisFieldsAdditionals) //??
                        .Include(el => el.categoryCode)
                        .Include(el => el.productPrices)
                        .Where(where)
                        .Where(x => x.websiteFK == websiteid)
                        .ToList();
                }
            }
            catch (Exception e)
            {
                Utilities.ProcessException(e);
            }
            return ret;
        }

        /// <summary>
        /// Read cms entities
        /// </summary>
        /// <param name="where"></param>
        /// <returns></returns>
        public static List<cmsEntry> ReadCms(Expression<Func<cmsEntry, bool>> where)
        {
            int w = int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString());
            return ReadCms(where, w);
        }

        public static List<cmsEntry> ReadCms(Expression<Func<cmsEntry, bool>> where, int websiteid)
        {
            List<cmsEntry> ret = new List<cmsEntry>();

            try
            {
                using (Ngmd db = new Ngmd())
                {
                    ret = db.cmsEntry
                        .Where(where)
                        .Where(x => x.cmsSection.websiteFK == websiteid)
                        .OrderBy(x => x.entryName)
                        .ToList();
                }
            }
            catch (Exception e)
            {
                Utilities.ProcessException(e);
            }
            return ret;
        }

        /// <summary>
        /// Read manufacturer entities
        /// </summary>
        /// <param name="where"></param>
        /// <returns></returns>
        public static List<manufacturer> ReadManufacturer(Expression<Func<manufacturer, bool>> where)
        {
            List<manufacturer> ret = new List<manufacturer>();

            try
            {
                using (Ngmd db = new Ngmd())
                {
                    ret = db.manufacturers
                        .Include(el => el.manufacturerNotes)
                        .Where(where)
                        .OrderBy(x => x.manufacturerName)
                        .ToList();
                }
            }
            catch (Exception e)
            {
                Utilities.ProcessException(e);
            }
            return ret;
        }

        /// <summary>
        /// Read eqFamiliy entities
        /// </summary>
        /// <param name="where"></param>
        /// <returns></returns>
        public static List<eqFamily> ReadFamily(Expression<Func<eqFamily, bool>> where)
        {
            List<eqFamily> ret = new List<eqFamily>();

            try
            {
                using (Ngmd db = new Ngmd())
                {
                    ret = db.eqFamilies
                        .Include(el => el.manufacturer.manufacturerNotes)
                        .Where(where)
                        .OrderBy(x => x.description)
                        .ToList();
                }
            }
            catch (Exception e)
            {
                Utilities.ProcessException(e);
            }
            return ret;
        }

        /// <summary>
        /// Read eqEquipment manufacturer entities
        /// </summary>
        /// <param name="where"></param>
        /// <returns></returns>
        public static List<eqEquipment> ReadEquipment(Expression<Func<eqEquipment, bool>> where)
        {
            List<eqEquipment> ret = new List<eqEquipment>();

            try
            {
                using (Ngmd db = new Ngmd())
                {
                    ret = db.eqEquipments
                        .Include(x => x.manufacturer)
                        .Include(x => x.eqFamilyMemberships)
                        .Where(where)
                        .OrderBy(x => x.description)
                        .ToList();
                }
            }
            catch (Exception e)
            {
                Utilities.ProcessException(e);
            }
            return ret;
        }

        public static eqEquipment ReadEquipment(string pattern)
        {
            eqEquipment ret = new eqEquipment();
            try
            {
                using (Ngmd db = new Ngmd())
                {
                    ret = db.eqEquipments
                        .FirstOrDefault(x => SqlFunctions.PatIndex(pattern, x.description) == 1 && x.statusFK == 1);
                }
            }
            catch (Exception e)
            {
                Utilities.ProcessException(e);
            }
            return ret;
        }

        public static List<eqProductMembership> ReadProductMembership(Expression<Func<eqProductMembership, bool>> where)
        {
            List<eqProductMembership> ret = new List<eqProductMembership>();
            try
            {
                using (Ngmd db = new Ngmd())
                {
                    ret = db.eqProductMemberships
                        .Include(x => x.product)
                        .Where(where)
                        .Where(x => x.product.productStatusFK == 1 || x.product.productStatusFK == 8)
                        .OrderBy(x => x.product.productName)
                        .ToList();
                }
            }
            catch (Exception e)
            {
                Utilities.ProcessException(e);
            }
            return ret;
        }

        public static List<categoryCode> ReadCategoryCode(Expression<Func<categoryCode, bool>> where)
        {
            List<categoryCode> ret = new List<categoryCode>();

            try
            {
                using (Ngmd db = new Ngmd())
                {
                    ret = db.categoryCodes
                        .Where(where)
                        .OrderBy(x => x.categoryCodeName)
                        .ToList();
                }
            }
            catch (Exception e)
            {
                Utilities.ProcessException(e);
            }
            return ret;
        }

        /// <summary>
        /// Returns a list of category codes contained within the parent category code
        /// </summary>
        /// <param name="parentCategoryCode"></param>
        /// <returns></returns>
        public static List<int> GetChildCategoryCodes(int parentCategoryCode)
        {
            List<int> ret = new List<int>();
            List<int> cats = ReadChildCategoryCodes(x => x.parentCategoryCodeID == parentCategoryCode);

            ret.Add(parentCategoryCode);
            if (cats.Count > 0)
            {
                ret.AddRange(cats);
                DoCategoryIteration(cats, ret);
            }

            return ret;
        }

        public static void DoCategoryIteration(List<int> categories, List<int> ret)
        {
            foreach (int cat in categories)
            {
                List<int> cats = ReadChildCategoryCodes(x => x.parentCategoryCodeID == cat);
                if (cats.Count > 0)
                {
                    ret.AddRange(cats);
                    DoCategoryIteration(cats, ret);
                }
            }
        }

        public static List<int> ReadChildCategoryCodes(Expression<Func<categoryCode, bool>> where)
        {
            List<int> ret = new List<int>();
            try
            {
                using (Ngmd db = new Ngmd())
                {
                    ret = db.categoryCodes
                        .Where(where)
                        .Select(x => x.categoryCodeID)
                        .ToList();
                }
            }
            catch (Exception e)
            {
                Utilities.ProcessException(e);
            }
            return ret;
        }

        public static List<secondaryCategoryLookup> ReadSecondaryCategoryLookup(Expression<Func<secondaryCategoryLookup, bool>> where)
        {
            List<secondaryCategoryLookup> ret = new List<secondaryCategoryLookup>();
            int w = int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString());

            try
            {
                using (Ngmd db = new Ngmd())
                {
                    ret = db.secondaryCategoryLookups
                        .Where(where)
                        .Where(x => x.websiteInventory.websiteFK == w)
                        .ToList();
                }
            }
            catch (Exception e)
            {
                Utilities.ProcessException(e);
            }
            return ret;
        }

        /// <summary>
        /// Red qa_Main entities
        /// </summary>
        /// <param name="where"></param>
        /// <returns></returns>
        public static List<qa_Main> ReadQandA(Expression<Func<qa_Main, bool>> where)
        {
            List<qa_Main> ret = new List<qa_Main>();

            try
            {
                using (Ngmd db = new Ngmd())
                {
                    ret = db.qa_Main
                        .Include("qa_WebsiteMapping")
                        .Where(where)
                        .OrderByDescending(x => x.AskedDate)
                        .ToList();
                }
            }
            catch (Exception e)
            {
                Utilities.ProcessException(e);
            }
            return ret;
        }

        public static void InsertQandA(qa_Main qam)
        {
            using (Ngmd db = new Ngmd())
            {
                try
                {
                    db.Entry(qam).State = EntityState.Added;
                    db.SaveChanges();

                    var newMapping = new qa_WebsiteMapping
                    {
                        WebsiteFK = qam.SourceWebsiteID,
                        QuestionAnswerFK = qam.QuestionAnswerID
                    };

                    db.Entry(newMapping).State = EntityState.Added;
                    db.SaveChanges();
                }
                catch (Exception e)
                {
                    Utilities.ProcessException(e);
                }
            }
        }

        public static List<QandA> ReadFaq(Expression<Func<Faq, bool>> where)
        {
            List<QandA> ret = new List<QandA>();
            int w = int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString());

            try
            {
                using (Ngmd db = new Ngmd())
                {
                    ret = db.Faq
                        .Where(where)
                        .Where(x => x.WebsiteFk == w)
                        .OrderByDescending(x => x.Priority).ThenByDescending(x => x.FaqId)
                        .Select(x => new QandA
                        {
                            Question = x.Question,
                            Answer = x.Answer,
                            GenerateSchema = x.GenerateSchema
                        })
                        .ToList();
                }
            }
            catch (Exception e)
            {
                Utilities.ProcessException(e);
            }
            return ret;
        }

        /// <summary>
        /// Read ConfigurationSetting
        /// </summary>
        /// <param name="where"></param>
        /// <returns></returns>
        public static List<configurationSetting> ReadConfigurationSetting(Expression<Func<configurationSetting, bool>> where1, bool isSiteSpecific = true)
        {
            List<configurationSetting> ret = new List<configurationSetting>();
            int? w = null;
            w = int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString());
            Expression<Func<configurationSetting, bool>> where2;
            if (isSiteSpecific)
            {
                where2 = x => x.websiteFK == w;
            }
            else
            {
                where2 = x => x.websiteFK == w || x.websiteFK == null;
            }

            try
            {
                using (Ngmd db = new Ngmd())
                {
                    ret = db.configurationSettings
                        .Where(where1)
                        .Where(where2)
                        .ToList();
                }
            }
            catch (Exception e)
            {
                Utilities.ProcessException(e);
            }
            return ret;
        }

        /// <summary>
        /// Read ManufacturerNotes
        /// </summary>
        /// <param name="where"></param>
        /// <returns></returns>
        public static List<manufacturerNote> ReadManufacturerNotes(Expression<Func<manufacturerNote, bool>> where)
        {
            List<manufacturerNote> ret = new List<manufacturerNote>();
            int w = int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString());

            try
            {
                using (Ngmd db = new Ngmd())
                {
                    ret = db.manufacturerNotes
                        .Where(where)
                        .Where(x => x.websiteFK == w)
                        .ToList();
                }
            }
            catch (Exception e)
            {
                Utilities.ProcessException(e);
            }
            return ret;
        }

        public static List<favouritePrinter> ReadFavouritePrinter(Expression<Func<favouritePrinter, bool>> where)
        {
            List<favouritePrinter> ret = new List<favouritePrinter>();
            int w = int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString());
            try
            {
                using (Ngmd db = new Ngmd())
                {
                    ret = db.favouritePrinters
                        .Where(where)
                        .Where(x => x.siteId == w)
                        .OrderByDescending(x => x.dateLastUpdated)
                        .ToList();
                }
            }
            catch (Exception e)
            {
                Utilities.ProcessException(e);
            }
            return ret;
        }

        public static void InsertFavouritePrinter(favouritePrinter fp)
        {
            using (Ngmd db = new Ngmd())
            {
                try
                {
                    // Check if this entry already exists
                    if (ReadFavouritePrinter(x => x.customerId == fp.customerId && x.eqEquipmentFK == fp.eqEquipmentFK).Count == 0)
                    {
                        db.Entry(fp).State = EntityState.Added;
                        db.SaveChanges();
                    }
                }
                catch (Exception e)
                {
                    Utilities.ProcessException(e);
                }
            }
        }

        public static void DeleteFavouritePrinter(favouritePrinter obj)
        {
            try
            {
                using (Ngmd db = new Ngmd())
                {
                    db.Entry(obj).State = EntityState.Deleted;
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                Utilities.ProcessException(e);
            }
        }

        public static void DeleteMcCartData(string id)
        {
            using (Ngmd db = new Ngmd())
            {
                McCartData mcd = db.McCartDatas
                    .Where(x => x.CartId == id)
                    .FirstOrDefault();

                if (mcd != null)
                {
                    DeleteMcCartData(mcd);
                }
            }
        }

        public static void DeleteMcCartData(McCartData obj)
        {
            try
            {
                using (Ngmd db = new Ngmd())
                {
                    db.Entry(obj).State = EntityState.Deleted;
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                Utilities.ProcessException(e);
            }
        }

        public static List<feefoFeedback> ReadFeeFoFeedback(Expression<Func<feefoFeedback, bool>> where)
        {
            List<feefoFeedback> ret = new List<feefoFeedback>();
            int w = int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString());

            try
            {
                using (Ngmd db = new Ngmd())
                {
                    ret = db.feefoFeedbacks
                        .Where(where)
                        .Where(x => x.websiteFK == w)
                        //.Where(x => x.isHidden != true)
                        .OrderByDescending(x => x.feedbackDate)
                        .ToList();
                }
            }
            catch (Exception e)
            {
                Utilities.ProcessException(e);
            }

            //foreach (var feedback in ret)
            //{
            //    if (feedback.productComment != "") continue;

            //    if (feedback.productRating == 5)
            //    {
            //        feedback.productComment = "Excellent";
            //    }
            //    if (feedback.productRating == 4)
            //    {
            //        feedback.productComment = "Very good";
            //    }
            //    if (feedback.productRating == 3)
            //    {
            //        feedback.productComment = "Not tried yet";
            //    }
            //}

            return ret;
        }

        public static List<crossSellingLink> ReadCrossSellingLinks(Expression<Func<crossSellingLink, bool>> where)
        {
            List<crossSellingLink> ret = new List<crossSellingLink>();

            try
            {
                using (Ngmd db = new Ngmd())
                {
                    ret = db.crossSellingLinks
                        .Include("product")
                        .Where(where)
                        .ToList();
                }
            }
            catch (Exception e)
            {
                Utilities.ProcessException(e);
            }
            return ret;
        }

        public static List<FraudCriteria> ReadFraudCriteria(Expression<Func<FraudCriteria, bool>> where)
        {
            List<FraudCriteria> ret = new List<FraudCriteria>();

            try
            {
                using (Ngmd db = new Ngmd())
                {
                    ret = db.FraudCriterias
                        .Where(where)
                        .ToList();
                }
            }
            catch (Exception e)
            {
                Utilities.ProcessException(e);
            }
            return ret;
        }

        public static List<deliveryZone> ReadDeliveryZones(Expression<Func<deliveryZone, bool>> where)
        {
            List<deliveryZone> ret = new List<deliveryZone>();
            int w = int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString());

            try
            {
                using (Ngmd db = new Ngmd())
                {
                    ret = db.deliveryZones
                        .Where(where)
                        .Where(x => x.WebsiteFK == w)
                        .OrderBy(x => x.IsDefault)
                        .ToList();
                }
            }
            catch (Exception e)
            {
                Utilities.ProcessException(e);
            }
            return ret;
        }

        public static List<deliveryZone> ReadDeliveryZonesAndServices(Expression<Func<deliveryZone, bool>> where)
        {
            List<deliveryZone> ret = new List<deliveryZone>();
            int w = int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString());

            try
            {
                using (Ngmd db = new Ngmd())
                {
                    ret = db.deliveryZones
                        .Include("deliveryLookups.deliveryService")
                        .Where(where)
                        .Where(x => x.WebsiteFK == w)
                        .OrderBy(x => x.IsDefault)
                        .ToList();
                }
            }
            catch (Exception e)
            {
                Utilities.ProcessException(e);
            }
            return ret;
        }

        public static List<deliveryService> ReadDeliveryService(Expression<Func<deliveryService, bool>> where)
        {
            List<deliveryService> ret = new List<deliveryService>();
            int w = int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString());

            try
            {
                using (Ngmd db = new Ngmd())
                {
                    ret = db.deliveryServices
                        .Where(where)
                        .Where(x => x.WebsiteFK == w)
                        .ToList();
                }
            }
            catch (Exception e)
            {
                Utilities.ProcessException(e);
            }
            return ret;
        }

        public static List<SagePayToken> ReadSagePayTokens(Expression<Func<SagePayToken, bool>> where)
        {
            List<SagePayToken> ret = new List<SagePayToken>();
            int w = int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString());
            try
            {
                using (Ngmd db = new Ngmd())
                {
                    ret = db.SagePayTokens
                        .Where(where)
                        .Where(x => x.websiteID == w)
                        .ToList();
                }
            }
            catch (Exception e)
            {
                Utilities.ProcessException(e);
            }
            return ret;
        }

        public static void InsertSagePayToken(SagePayToken token)
        {
            try
            {
                using (Ngmd db = new Ngmd())
                {
                    db.Entry(token).State = EntityState.Added;
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                Utilities.ProcessException(e);
            }
        }

        public static void DeleteSagePayToken(string tokenId)
        {
            using (Ngmd db = new Ngmd())
            {
                SagePayToken spt = db.SagePayTokens
                    .Where(x => x.token.Contains(tokenId))
                    .FirstOrDefault();

                if (spt != null)
                {
                    spt.deleted = 1;
                    DeleteSagePayToken(spt);
                }
            }
        }

        public static void DeleteSagePayToken(int id)
        {
            using (Ngmd db = new Ngmd())
            {
                SagePayToken spt = db.SagePayTokens
                    .Where(x => x.id == id)
                    .FirstOrDefault();

                if (spt != null)
                {
                    spt.deleted = 1;
                    DeleteSagePayToken(spt);
                }
            }
        }

        public static void DeleteSagePayToken(SagePayToken obj)
        {
            try
            {
                using (Ngmd db = new Ngmd())
                {
                    db.Entry(obj).State = EntityState.Modified;
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                Utilities.ProcessException(e);
            }
        }

        public static List<SagePayTransaction> ReadSagePayTransaction(Expression<Func<SagePayTransaction, bool>> where)
        {
            List<SagePayTransaction> ret = new List<SagePayTransaction>();
            int w = int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString());

            try
            {
                using (Ngmd db = new Ngmd())
                {
                    ret = db.SagePayTransactions
                        .Where(where)
                        .Where(x => x.websiteID == w)
                        .ToList();
                }
            }
            catch (Exception e)
            {
                Utilities.ProcessException(e);
            }
            return ret;
        }

        public static void InsertSagePayTransaction(SagePayNotification spn)
        {
            SagePayTransaction spt = new SagePayTransaction();
            spt.protx_id = 0;
            spt.doc_id = spn.DocId;
            spt.protx_time = DateTime.Now;
            spt.vm_uid = spn.VendorTxCode;
            spt.protx_protocol = spn.VPSProtocol;
            spt.protx_status = spn.Status;
            spt.protx_detail = spn.StatusDetail;
            spt.protx_uid = spn.VPSTxID ?? "";
            spt.protx_key = spn.SecurityKey ?? "";
            spt.protx_auth_code = spn.TxAuthNo;
            spt.protx_avscv2 = spn.AVSCV2 ?? "";
            spt.protx_address = spn.AddressResult ?? "";
            spt.protx_postcode = spn.PostCodeResult ?? "";
            spt.protx_cv2 = spn.CV2Result ?? "";
            spt.protx_string = spn.PostString;
            spt.protx_3dsecurestatus = spn.ThreeDSecureStatus ?? "";
            spt.protx_response = spn.ResponseString;
            spt.protx_cavv = spn.CAVV;
            spt.protx_md = "";
            spt.protx_acsurl = "";
            spt.doc_type = spn.DocType;
            spt.red_screened = spn.ReDScreened;
            spt.websiteID = int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString());

            try
            {
                using (Ngmd db = new Ngmd())
                {
                    if (spt.protx_id == 0)
                    {
                        db.Entry(spt).State = EntityState.Added;
                        db.SaveChanges();
                    }
                }
            }
            catch (Exception e)
            {
                Utilities.ProcessException(e);
            }
        }

        public static void InsertOpayoLog(string json, string orderNumber, string merchandiseSessionKey, string action)
        {
            int actionId = DataCache.GetNgmdLookups(x => x.LookupType.LookupTypeName == "OpayoAction" && x.LookupName == action).FirstOrDefault().LookupId;

            OpayoLog ol = new OpayoLog();
            ol.DateTime = DateTime.Now;
            ol.WebsiteFk = int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString());
            ol.ActionFk = actionId;
            ol.OrderNumber = orderNumber;
            ol.MerchandiseSessionKey = merchandiseSessionKey;
            ol.OrderNumber = orderNumber;
            ol.Json = json;

            try
            {
                using (Ngmd db = new Ngmd())
                {
                    if (ol.OpayoLogId == 0)
                    {
                        db.Entry(ol).State = EntityState.Added;
                        db.SaveChanges();
                    }
                }
            }
            catch (Exception e)
            {
                Utilities.ProcessException(e);
            }
        }

        private static string GetOpayoJsonItem(string json, string item)
        {
            JObject jO = JsonConvert.DeserializeObject<JObject>(json);
            string val = "";

            if (item == "cardidentifier")
            {
                val = jO["paymentMethod"]?["card"]?["cardIdentifier"]?.ToString();
            }
            if (item == "acstransid")
            {
                val = jO["acsTransId"]?.ToString();
            }

            return val == null ? "n/a" : val;
        }

        public static void InsertLogEntry(Log log)
        {
            try
            {
                using (Ngmd db = new Ngmd())
                {
                    db.Entry(log).State = EntityState.Added;
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                Utilities.ProcessException(e);
            }
        }

        public static void InsertPayPalLogEntry(PayPalLog log)
        {
            try
            {
                using (Ngmd db = new Ngmd())
                {
                    db.Entry(log).State = EntityState.Added;
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                Utilities.ProcessException(e);
            }
        }

        public static void InsertAmazonPayLogEntry(AmazonPayLog log)
        {
            try
            {
                using (Ngmd db = new Ngmd())
                {
                    db.Entry(log).State = EntityState.Added;
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                Utilities.ProcessException(e);
            }
        }

        public static List<passwordReset> ReadPasswordReset(Expression<Func<passwordReset, bool>> where)
        {
            List<passwordReset> ret = new List<passwordReset>();
            int w = int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString());
            try
            {
                using (Ngmd db = new Ngmd())
                {
                    ret = db.passwordResets
                        .Where(where)
                        .Where(x => x.siteId == w)
                        .ToList();
                }
            }
            catch (Exception e)
            {
                Utilities.ProcessException(e);
            }
            return ret;
        }

        public static void InsertPasswordReset(passwordReset pr)
        {
            using (Ngmd db = new Ngmd())
            {
                try
                {
                    db.Entry(pr).State = EntityState.Added;
                    db.SaveChanges();
                }
                catch (Exception e)
                {
                    Utilities.ProcessException(e);
                }
            }
        }

        public static void DeletePasswordReset(passwordReset obj)
        {
            try
            {
                using (Ngmd db = new Ngmd())
                {
                    db.Entry(obj).State = EntityState.Deleted;
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                Utilities.ProcessException(e);
            }
        }

        public static List<ProductPdf> GetProductPdfs(string partNo, string manufacturerName)
        {
            List<ProductPdf> pdfDocuments;

            using (var db = new Ngmd())
            {
                // Use HP Q8696A in testing
                pdfDocuments = (from m in db.pim_mediaLinks
                                join p in db.pim_products on m.prodID equals p.prodID
                                where p.partno == partNo && p.manufacturer == manufacturerName && m.type == "PDF"
                                select new ProductPdf { Description = "Click for details", Url = m.url }).ToList();
            }

            return pdfDocuments;
        }

        public static List<obsoleteItem> ReadObsoleteItem(Expression<Func<obsoleteItem, bool>> where)
        {
            int w = int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString());
            return ReadObsoleteItem(where, w);
        }

        public static List<obsoleteItem> ReadObsoleteItem(Expression<Func<obsoleteItem, bool>> where, int websiteid)
        {
            List<obsoleteItem> ret = new List<obsoleteItem>();
            try
            {
                using (Ngmd db = new Ngmd())
                {
                    ret = db.obsoleteItems
                        .Where(where)
                        //.Where(x => SqlFunctions.PatIndex("Olivetti_Faxlab_444", x.equipmentName) > 0)
                        .Where(x => x.websiteFK == websiteid)
                        .ToList();
                }
            }
            catch (Exception e)
            {
                Utilities.ProcessException(e);
            }
            return ret;
        }

        public static obsoleteItem ReadObsoleteItem(string pattern)
        {
            int w = int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString());
            return ReadObsoleteItem(pattern, w);
        }

        public static obsoleteItem ReadObsoleteItem(string pattern, int websiteid)
        {
            obsoleteItem ret = new obsoleteItem();
            try
            {
                using (Ngmd db = new Ngmd())
                {
                    ret = db.obsoleteItems
                        .FirstOrDefault(x => SqlFunctions.PatIndex(pattern, x.equipmentName) == 1 && x.websiteFK == websiteid);
                }
            }
            catch (Exception e)
            {
                Utilities.ProcessException(e);
            }
            return ret;
        }

        public static List<VoucherPromo> ReadVoucherPromo(Expression<Func<VoucherPromo, bool>> where)
        {
            List<VoucherPromo> ret = null;
            List<LookupNgmd> lvt = DataCache.GetNgmdLookups(x => x.LookupType.LookupTypeName == "VoucherType");
            try
            {
                using (Ngmd db = new Ngmd())
                {
                    ret = db.VoucherPromoes
                        .Include(x => x.VoucherPromoGroup.VoucherPromoGroupMappings)
                        .Include(x => x.Website)
                        .Where(where)
                        .ToList();
                }
                foreach (VoucherPromo vp in ret)
                {
                    vp.VoucherTypeName = lvt.Find(x => x.AltLookupId == vp.VoucherTypeFk).LookupName;
                }
            }

            catch (Exception e)
            {
                Utilities.ProcessException(e);
            }
            return ret;
        }

        public static List<BackOrderItem> ReadBackOrder(Expression<Func<BackOrderItem, bool>> where)
        {
            List<BackOrderItem> ret = new List<BackOrderItem>();
            try
            {
                using (Ngmd db = new Ngmd())
                {
                    ret = db.BackOrders
                        .SelectMany(BackOrder => BackOrder.BackOrderItems)
                        .Include(x => x.BackOrder.provider)
                        .Include(x => x.Lookup)
                        .Include(x => x.BackOrder.Website)
                        .Where(where)
                        .ToList();
                }
            }
            catch (Exception e)
            {
                Utilities.ProcessException(e);
            }
            return ret;
        }

        public static List<OrderTracking> ReadOrderTracking(Expression<Func<OrderTracking, bool>> where)
        {
            List<OrderTracking> ret = new List<OrderTracking>();
            try
            {
                using (Ngmd db = new Ngmd())
                {
                    ret = db.OrderTracking
                        .Include(x => x.provider)
                        .Include(x => x.Website)
                        .Where(where)
                        .ToList();
                }
            }
            catch (Exception e)
            {
                Utilities.ProcessException(e);
            }
            return ret;
        }
        public static List<provider> ReadProvider(Expression<Func<provider, bool>> where)
        {
            List<provider> ret = new List<provider>();
            try
            {
                using (Ngmd db = new Ngmd())
                {
                    ret = db.providers
                        .Where(where)
                        .ToList();
                }
            }
            catch (Exception e)
            {
                Utilities.ProcessException(e);
            }
            return ret;
        }

        public static SaveReturn SaveVoucher(VoucherPromo voucher)
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = true;

            try
            {
                using (Ngmd db = new Ngmd())
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
                sr.IsSuccess = false;
            }

            return sr;
        }

        public static bool VoucherExists(int websiteId, string voucherCode)
        {
            return VoucherExists(x => x.VoucherCode == voucherCode && x.WebsiteFk == websiteId);
        }

        public static bool VoucherExists(string voucherCode)
        {
            return VoucherExists(x => x.VoucherCode == voucherCode);
        }

        public static bool VoucherExists(Expression<Func<VoucherPromo, bool>> where)
        {
            using (Ngmd db = new Ngmd())
            {
                if (db.VoucherPromoes.Any(where))
                {
                    return true;
                }
                return false;
            }
        }

        public static List<VoucherPromoGroup> ReadVoucherPromoGroup(Expression<Func<VoucherPromoGroup, bool>> where)
        {
            List<VoucherPromoGroup> ret = new List<VoucherPromoGroup>();
            try
            {
                using (Ngmd db = new Ngmd())
                {
                    ret = db.VoucherPromoGroups
                        .Include(x => x.VoucherPromoGroupMappings)
                        .Where(where)
                        .ToList();
                }
            }
            catch (Exception e)
            {
                Utilities.ProcessException(e);
            }
            return ret;
        }

        public static SaveReturn SaveInterimOrder(InterimOrder interimOrder)
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = true;

            try
            {
                using (Ngmd db = new Ngmd())
                {
                    if (interimOrder.InterimOrderId > 0)
                    {
                        db.Entry(interimOrder).State = EntityState.Modified;
                    }
                    else
                    {
                        db.Entry(interimOrder).State = EntityState.Added;
                    }

                    db.SaveChanges();
                }
            }
            catch
            {
                sr.IsSuccess = false;
            }

            return sr;
        }

        public static SaveReturn SaveCampaignTracking(CampaignTracking ct)
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = true;

            try
            {
                using (Ngmd db = new Ngmd())
                {
                    if (ct.CampaignTrackingId > 0)
                    {
                        db.Entry(ct).State = EntityState.Modified;
                    }
                    else
                    {
                        db.Entry(ct).State = EntityState.Added;
                    }

                    db.SaveChanges();
                }
            }
            catch
            {
                sr.IsSuccess = false;
            }

            return sr;
        }

        public static List<Website> ReadWebsite(Expression<Func<Website, bool>> where)
        {
            List<Website> ret = new List<Website>();
            try
            {
                using (Ngmd db = new Ngmd())
                {
                    ret = db.Websites
                        .Where(where)
                        .ToList();
                }
            }
            catch (Exception e)
            {
                Utilities.ProcessException(e);
            }
            return ret;
        }

        public static List<PortalIndex> ReadPortalIndex(Expression<Func<PortalIndex, bool>> where)
        {
            List<PortalIndex> ret = new List<PortalIndex>();
            try
            {
                using (Ngmd db = new Ngmd())
                {
                    ret = db.PortalIndexes
                        .Include(x => x.Website)
                        .Where(where)
                        .Take(200)
                        .ToList();
                }
            }
            catch (Exception e)
            {
                Utilities.ProcessException(e);
            }
            return ret;
        }

        public static List<JsonStore> ReadJsonStore(Expression<Func<JsonStore, bool>> where)
        {
            List<JsonStore> ret = new List<JsonStore>();
            try
            {
                using (Ngmd db = new Ngmd())
                {
                    ret = db.JsonStores
                        .Where(where)
                        .ToList();
                }
            }
            catch (Exception e)
            {
                Utilities.ProcessException(e);
            }
            return ret;
        }

        public static void InsertJsonStore(JsonStore js)
        {
            try
            {
                using (Ngmd db = new Ngmd())
                {
                    db.Entry(js).State = EntityState.Added;
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                Utilities.ProcessException(e);
            }
        }

        public static void UpdateJsonStore(JsonStore js)
        {
            try
            {
                using (Ngmd db = new Ngmd())
                {
                    db.Entry(js).State = EntityState.Modified;
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                Utilities.ProcessException(e);
            }
        }

        public static void InsertMcOrderData(McOrderData mcod)
        {
            try
            {
                using (Ngmd db = new Ngmd())
                {
                    db.Entry(mcod).State = EntityState.Added;
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                Utilities.ProcessException(e);
            }
        }

        public static void SaveMcCartData(McCartData mccd)
        {
            try
            {
                using (Ngmd db = new Ngmd())
                {
                    if (mccd.McCartDataId > 0)
                    {
                        db.Entry(mccd).State = EntityState.Modified;
                    }
                    else
                    {
                        db.Entry(mccd).State = EntityState.Added;
                    }
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                Utilities.ProcessException(e);
            }
        }

        public static List<LookupCust> ReadCustLookUp(Expression<Func<LookupCust, bool>> where)
        {
            var list = new List<LookupCust>();

            try
            {
                using (var db = new customerData())
                {
                    list = db.LookupCusts
                            .Include("LookupType")
                            .Where(where)
                            .ToList();
                }
            }
            catch (Exception ex)
            {
                Utilities.ProcessException(ex);
            }

            return list;
        }

        public static List<LookupNgmd> ReadNgmdLookUp(Expression<Func<LookupNgmd, bool>> where)
        {
            var list = new List<LookupNgmd>();

            try
            {
                using (var db = new Ngmd())
                {
                    list = db.LookupNgmds
                            .Include("LookupType")
                            .Where(where)
                            .ToList();
                }
            }
            catch (Exception ex)
            {
                Utilities.ProcessException(ex);
            }

            return list;
        }

        //public static IQueryable<Lookup> ReadAllLookUps(Expression<Func<Lookup, bool>> where)
        //{
        //    IQueryable<Lookup> list = null;

        //    try
        //    {
        //        using (var db = new customerData())
        //        {
        //            list = db.Lookups
        //                .Include("LookupType")
        //                .Where(where);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Utilities.ProcessException(ex);
        //    }

        //    return list;
        //}

        public static Customer GetCustomer(Expression<Func<Customer, bool>> where)
        {
            var customer = new Customer();

            try
            {
                using (var db = new customerData())
                {
                    customer = db.Customers
                                     .Where(where)
                                     .FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                Utilities.ProcessException(ex);
            }

            return customer;
        }

        public static int SaveCustomer(Customer customer)
        {
            int customerId = 0;

            try
            {
                Customer cust;
                using (var db = new customerData())
                {
                    cust = db.Customers.Where(w => w.OriginalEmailAddress == customer.OriginalEmailAddress && w.WebsiteFk == customer.WebsiteFk).FirstOrDefault();
                }
                using (var db = new customerData())
                {
                    //var cust = db.Customers.Where(w => w.OriginalEmailAddress == customer.OriginalEmailAddress && w.WebsiteFk == customer.WebsiteFk).FirstOrDefault();

                    if (cust != null)
                    {
                        customerId = cust.CustomerId;
                        customer.CustomerId = cust.CustomerId;

                        //cust.AccountNumber = customer.AccountNumber;
                        //cust.OriginalEmailAddress = customer.OriginalEmailAddress;
                        //cust.CustomerTypeId = customer.CustomerTypeId;

                        db.Entry(customer).State = EntityState.Modified;
                        db.SaveChanges();
                    }
                    else
                    {
                        db.Entry(customer).State = EntityState.Added;
                        db.SaveChanges();
                        customerId = customer.CustomerId;
                    }
                }
            }
            catch (Exception ex)
            {
                Utilities.LogInformationMessage(customer.ToString());
                Utilities.ProcessException(ex);
            }

            return customerId;
        }

        public static bool SaveCreditAccountApplication(Account account, Customer customer, Billing billing)
        {
            bool success;

            try
            {
                int customerId = SaveCustomer(customer);

                using (var db = new customerData())
                {
                    using (DbContextTransaction transaction = db.Database.BeginTransaction())
                    {
                        var acc = db.Accounts.Where(w => w.CustomerFk == customerId).FirstOrDefault();

                        if (acc != null)
                        {
                            acc.CustomerFk = customerId;
                            acc.StatusId = account.StatusId;
                            acc.OrganisationTypeId = account.OrganisationTypeId;
                            acc.SectorId = account.SectorId;
                            acc.TradingName = account.TradingName;
                            acc.ContactName = account.ContactName;
                            acc.ContactEmailAddress = account.ContactEmailAddress;
                            acc.ContactTelephoneNo = account.ContactTelephoneNo;
                            acc.TotalStaffCountId = account.TotalStaffCountId;
                            acc.OrderStaffCountId = account.OrderStaffCountId;
                            acc.EstMonthlySpend = account.EstMonthlySpend;
                            acc.CreditLimit = account.CreditLimit;
                            acc.CompanyRegNo = account.CompanyRegNo;
                            acc.CompanyVatNo = account.CompanyVatNo;
                            acc.AcceptStandardTerms = account.AcceptStandardTerms;
                            acc.AcceptCreditTerms = account.AcceptCreditTerms;
                            acc.DateOfApplication = DateTime.Now;
                            acc.DateLastUpdated = DateTime.Now;
                            acc.FirstOrderRef = account.FirstOrderRef;
                            acc.FirstOrderAmt = account.FirstOrderAmt;
                            acc.IsAccountCustomer = account.IsAccountCustomer;

                            db.Entry(acc).State = EntityState.Modified;
                        }
                        else
                        {
                            account.CustomerFk = customerId;
                            db.Entry(account).State = EntityState.Added;
                        }
                        db.SaveChanges();

                        var bill = db.Billings.Where(w => w.CustomerFk == customerId).FirstOrDefault();

                        if (bill != null)
                        {
                            bill.CustomerFk = customerId;
                            bill.ContactName = billing.ContactName;
                            bill.ContactEmailAddress = billing.ContactEmailAddress;
                            bill.ContactTelephoneNo = billing.ContactTelephoneNo;
                            bill.AddressLine1 = billing.AddressLine1;
                            bill.AddressLine2 = billing.AddressLine2;
                            bill.AddressLine3 = billing.AddressLine3;
                            bill.AddressLine4 = billing.AddressLine4;
                            bill.AddressLine5 = billing.AddressLine5;
                            bill.PostCode = billing.PostCode;
                            bill.Country = billing.Country;
                            bill.DirectDebit = billing.DirectDebit;

                            db.Entry(bill).State = EntityState.Modified;
                        }
                        else
                        {
                            billing.CustomerFk = customerId;
                            db.Entry(billing).State = EntityState.Added;
                        }
                        db.SaveChanges();

                        transaction.Commit();
                    }
                }

                success = true;
            }
            catch (Exception ex)
            {
                Utilities.ProcessException(ex);
                success = false;
            }

            return success;
        }

        public static bool IsTradeAccount(string email)
        {
            using (var db = new customerData())
            {
                var acc = db.Accounts
                    .Include(x => x.Customer)
                    .Include(x => x.Lookup5)
                    .Where(x => x.Customer.OriginalEmailAddress == email)
                    .FirstOrDefault();

                if (acc != null)
                {
                    if (acc.Customer != null)
                    {
                        if (acc.Customer.Accounts.FirstOrDefault().IsTradeCustomer && (acc.Lookup5.LookupName == "Approved" || acc.Lookup5.LookupName == "Submitted"))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public static bool SaveTradeAccountApplication(Account account, Customer customer, Billing billing)
        {
            bool success;

            // Fill in any missing mandatory fields with default values
            account.OrderStaffCountId = EntityAccess.ReadCustLookUp(x => x.LookupType.LookupTypeName == "Staff Count" && x.LookupName == "Unspecified").FirstOrDefault().LookupID;
            account.TotalStaffCountId = EntityAccess.ReadCustLookUp(x => x.LookupType.LookupTypeName == "Staff Count" && x.LookupName == "Unspecified").FirstOrDefault().LookupID;
            account.OrganisationTypeId = EntityAccess.ReadCustLookUp(x => x.LookupType.LookupTypeName == "Organisation Type" && x.LookupName == "Unspecified").FirstOrDefault().LookupID;
            account.SectorId = EntityAccess.ReadCustLookUp(x => x.LookupType.LookupTypeName == "Sector" && x.LookupName == "Unspecified").FirstOrDefault().LookupID;

            try
            {
                int customerId = SaveCustomer(customer);

                using (var db = new customerData())
                {
                    using (DbContextTransaction transaction = db.Database.BeginTransaction())
                    {
                        var acc = db.Accounts.Where(w => w.CustomerFk == customerId).FirstOrDefault();

                        if (acc != null)
                        {
                            acc.CustomerFk = customerId;
                            acc.TradeStatusId = account.TradeStatusId;
                            //acc.OrganisationTypeId = account.OrganisationTypeId;
                            //acc.SectorId = account.SectorId;
                            acc.TradingName = account.TradingName;
                            acc.ContactName = account.ContactName;
                            acc.ContactEmailAddress = account.ContactEmailAddress;
                            acc.ContactTelephoneNo = account.ContactTelephoneNo;
                            //acc.TotalStaffCountId = account.TotalStaffCountId;
                            //acc.OrderStaffCountId = account.OrderStaffCountId;
                            acc.EstMonthlySpend = account.EstMonthlySpend;
                            //acc.CreditLimit = account.CreditLimit;
                            acc.CompanyRegNo = account.CompanyRegNo;
                            acc.CompanyVatNo = account.CompanyVatNo;
                            acc.AcceptStandardTerms = account.AcceptStandardTerms;
                            //acc.AcceptCreditTerms = account.AcceptCreditTerms;
                            acc.DateOfApplication = DateTime.Now;
                            acc.DateLastUpdated = DateTime.Now;
                            //acc.FirstOrderRef = account.FirstOrderRef;
                            //acc.FirstOrderAmt = account.FirstOrderAmt;
                            acc.IsTradeCustomer = account.IsTradeCustomer;
                            acc.NumberOffices = account.NumberOffices;
                            acc.NumberPrinters = account.NumberPrinters;

                            db.Entry(acc).State = EntityState.Modified;
                        }
                        else
                        {
                            account.CustomerFk = customerId;
                            db.Entry(account).State = EntityState.Added;
                        }
                        db.SaveChanges();

                        var bill = db.Billings.Where(w => w.CustomerFk == customerId).FirstOrDefault();

                        if (bill != null)
                        {
                            bill.CustomerFk = customerId;
                            bill.ContactName = billing.ContactName;
                            bill.ContactEmailAddress = billing.ContactEmailAddress;
                            bill.ContactTelephoneNo = billing.ContactTelephoneNo;
                            bill.AddressLine1 = billing.AddressLine1;
                            bill.AddressLine2 = billing.AddressLine2;
                            bill.AddressLine3 = billing.AddressLine3;
                            bill.AddressLine4 = billing.AddressLine4;
                            bill.AddressLine5 = billing.AddressLine5;
                            bill.PostCode = billing.PostCode;
                            bill.Country = billing.Country;
                            bill.DirectDebit = billing.DirectDebit;

                            db.Entry(bill).State = EntityState.Modified;
                        }
                        else
                        {
                            billing.CustomerFk = customerId;
                            db.Entry(billing).State = EntityState.Added;
                        }
                        db.SaveChanges();

                        transaction.Commit();
                    }
                }

                success = true;
            }
            catch (Exception ex)
            {
                Utilities.ProcessException(ex);
                success = false;
            }

            return success;
        }

        public static Account GetAccountDetails(Expression<Func<Account, bool>> where)
        {
            var account = new Account();

            try
            {
                using (var db = new customerData())
                {
                    account = db.Accounts
                                .Include("Customer")
                                .Where(where)
                                .FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                Utilities.ProcessException(ex);
            }

            return account;
        }

        public static Billing GetBillingDetails(Expression<Func<Billing, bool>> where)
        {
            var billing = new Billing();

            try
            {
                using (var db = new customerData())
                {
                    billing = db.Billings
                                .Where(where)
                                .FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                Utilities.ProcessException(ex);
            }

            return billing;
        }

        public static List<MailingList> GetMailingList(Expression<Func<MailingList, bool>> where)
        {
            List<MailingList> lml = new List<MailingList>();
            int w = int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString());

            try
            {
                using (var db = new customerData())
                {
                    lml = db.MailingList
                                .Where(where)
                                .Where(x => x.WebsiteFk == w)
                                .ToList();
                }
            }
            catch (Exception ex)
            {
                Utilities.ProcessException(ex);
            }

            return lml;
        }

        public static void InsertMailingList(MailingList ml)
        {
            try
            {
                using (customerData db = new customerData())
                {
                    db.Entry(ml).State = EntityState.Added;
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                Utilities.ProcessException(e);
            }
        }

        public static List<Account> ReadAccount(Expression<Func<Account, bool>> where)
        {
            List<Account> la = new List<Account>();
            int w = int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString());

            try
            {
                using (var db = new customerData())
                {
                    la = db.Accounts
                        .Include(x => x.Customer)
                        .Include(x => x.Lookup)
                        .Include(x => x.Lookup1)
                        .Include(x => x.Lookup2)
                        .Include(x => x.Lookup3)
                        .Include(x => x.Lookup4)
                        .Include(x => x.Lookup5)
                        .Where(where)
                        .Where(x => x.Customer.WebsiteFk == w)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                Utilities.ProcessException(ex);
            }

            return la;
        }

    }
}
