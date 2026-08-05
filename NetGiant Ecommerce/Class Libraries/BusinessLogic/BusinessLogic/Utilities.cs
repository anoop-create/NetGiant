using DataAccess.EntityFramework;
using RestSharp;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Data;
using System.Data.Entity.Validation;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Routing;
using VMerchantWrapper.Entities;
using VmVoucherType = VMerchantWrapper.Entities.VoucherType;
using System.Reflection;
using System.IO;
using System.Security.Cryptography;
using System.Web.ModelBinding;
using System.Web.Mvc;
using ExpressiveAnnotations.Attributes;
using Nest;
using System.Web.SessionState;
using MailChimp.Net.Models;
using System.Threading.Tasks;
using static BusinessLogic.MyOpayo;
using static System.Net.Mime.MediaTypeNames;

namespace BusinessLogic
{
    public class Utilities
    {
        /// <summary>
        /// Build a comma separated string of Stock Refs from the current basket
        /// </summary>
        /// <returns></returns>
        public static string GetStockRefArray()
        {
            List<BasketContents> lbc = Utilities.LoadSession<List<BasketContents>>("B_BasketArray");
            return GetStockRefArray(lbc);
        }

        /// <summary>
        /// Build a comma separated string of Stock Refs from the current basket
        /// </summary>
        /// <param name="lbc"></param>
        /// <returns></returns>
        public static string GetStockRefArray(List<BasketContents> lbc)
        {
            StringBuilder refarray = new StringBuilder();
            StringBuilder summary = new StringBuilder();
            string comma = "";

            foreach (BasketContents bc in lbc)
            {
                if (bc.ItemType == BasketItemType.Item)
                {
                    refarray.Append(comma + bc.StockRef);
                    if (comma == "")
                    {
                        comma = ", ";
                    }
                }
            }

            return refarray.ToString();
        }

        /// <summary>
        /// Set Price and vat tag
        /// </summary>
        /// <param name="incVat"></param>
        /// <param name="exVat"></param>
        /// <returns></returns>
        public static Tuple<string, decimal> SetPrice(decimal incVat, decimal exVat)
        {
            Tuple<string, decimal> price;
            if (ConfigurationManager.AppSettings["UseVatInclusivePrices"] == "1")
            {
                price = new Tuple<string, decimal>("inc VAT", incVat);
            }
            else
            {
                price = new Tuple<string, decimal>("ex VAT", exVat);
            }

            return price;
        }

        public static SaveReturn LoadVoucher(string voucherCode)
        {
            string refArray = GetStockRefArray();
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = false;
            string reason = "";
            int w = int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString());
            VoucherPromo v = new VoucherPromo();

            // Get Promo Vouchers
            List<VoucherPromo> lvp = EntityAccess.ReadVoucherPromo(x => x.VoucherCode == voucherCode && x.WebsiteFk == w).ToList();
            
            if (lvp.Count > 0)
            {
                // Process Vouchers
                lvp.RemoveAll(x => x.ValidFrom > DateTime.Now || x.ValidTo < DateTime.Now || x.IsUsed);
                if (lvp.Count > 0)
                {
                    v = lvp[0];

                    if (!string.IsNullOrEmpty(v.AccountNumber))
                    {
                        // Customer Voucher
                        if (Authentication.IsNotAuthenticated())
                        {
                            reason = "Please Sign In to use this voucher.";
                        }
                        else
                        {
                            if (v.AccountNumber != HttpContext.Current.Session["U_AccountNo"].ToString())
                            {
                                reason = "The voucher is invalid for this Account - please try again.";
                            }
                        }
                    }

                    if (!v.IsGlobal)
                    {
                        foreach (VoucherPromoGroupMapping vpgm in v.VoucherPromoGroup.VoucherPromoGroupMappings)
                        {
                            int[] addCats = EntityAccess.GetChildCategoryCodes(vpgm.CategoryCodeFk).ToArray();
                            v.Categories.AddRange(addCats);
                        }
                        if (v.Categories.Count == 0)
                        {
                            reason = "Invalid voucher code - please try again.";
                        }
                        v.Categories.Sort();
                    }
                }
                else
                {
                    reason = "Voucher has expired - please try again.";
                }
            }
            else
            {
                reason = "The voucher is invalid - please try again.";
            }

            if (string.IsNullOrEmpty(v.AccountNumber))
            {
                // Promo Voucher
                bool tradeCustomer = Convert.ToBoolean(HttpContext.Current.Session["U_IsTradeCustomer"]);
                if (tradeCustomer && !v.IsForTrade)
                {
                    reason = "The voucher is not valid for Trade Customers.";
                }
                if (!tradeCustomer && v.IsForTrade && !v.IsForCustomer)
                {
                    reason = "The voucher is invalid - please try again.";
                }
            }

            if (reason != "")
            {
                // Invalid Voucher
                HttpContext.Current.Session.Remove("B_VoucherCode");
                HttpContext.Current.Session.Remove("V_Voucher");
                sr.IsSuccess = false;
                sr.Html =
                    "<div class=\"g-fc-nm\"><i class=\"fa fa-exclamation-triangle fa-lg\"></i><span class=\"g-p-l-10\">" +
                    reason + "</span></div>";
                sr.Message = reason;
            }
            else
            {
                sr.IsSuccess = true;
                HttpContext.Current.Session["B_VoucherCode"] = voucherCode.ToUpper();
                HttpContext.Current.Session["V_Voucher"] = v;
                Basket.RemoveFromBasket(x => x.ItemType == BasketItemType.Voucher || x.ItemType == BasketItemType.CompatibleDiscount);
                sr.Message = Basket.ApplyVoucher();
                sr.Html = "<i class=\"fa fa-exclamation-triangle fa-lg\"></i><span class=\"g-p-l-10\">" + sr.Message + "</span>";
            }

            return sr;
        }
        public static string GetRandomNumber(int a = 100000, int b = 999999)
        {
            Random rnd = new Random();
            return rnd.Next(a, b).ToString();
        }

        public static string GetStaticFilePrefix()
        {
            string prefix = "/data/static-files";
            if (HttpContext.Current.Request.Url.Host == "localhost")
            {
                prefix = "/data/local-static-files";
            }
            return prefix;
        }

        public static void SendPersonalVoucherEmail(VoucherPromo v)
        {
            Decimal amt = v.Amount ?? 0;
            string websiteUrl = EntityAccess.ReadWebsite(x => x.WebsiteID == v.WebsiteFk).FirstOrDefault().WebURL; // v.websiteFK because request may come from the portal
            Dictionary<string, string> commondata = DataCache.GetSectionData("CommonData");
            Dictionary<string, string> replacements = new Dictionary<string, string>
            {
                {"[vouchernumber]", v.VoucherCode},
                {"[url]", "https://" + websiteUrl + "/cvoucher/" + v.VoucherCode},
                {"[voucheramount]", "&pound;" + amt.ToString("#,###,##0.00")},
                {"[TelephoneNumber]", Utilities.GetItemFromDict(commondata, "TelephoneNumber")},
                {"[SupportEmail]", Utilities.GetItemFromDict(commondata, "SupportEmail")}
            };

            // Must retirieve the correct CMS entry for the website for which the voucher is being created
            cmsEntry cms = EntityAccess.ReadCms(x => x.cmsSection.sectionName == "EmailData" && x.entryName == "CustomerVoucher", v.WebsiteFk).FirstOrDefault();
            if (cms != null)
            {
                string body = cms.cmsContent;
                foreach (KeyValuePair<string, string> kvp in replacements)
                {
                    body = body.Replace(kvp.Key, kvp.Value);
                }
                SendEmail(Utilities.GetItemFromDict(commondata, "SupportEmail").ToLower(),
                    v.Email.ToLower(), "Customer Voucher", body);
            }
        }

        public static void SendEmail(string from, string to, string subject, string cmsentry, Dictionary<string, string> replacements, string bcc = "")
        {
            if (HttpContext.Current.Request.IsLocal)
                return;

            cmsEntry cms = EntityAccess.ReadCms(x => x.cmsSection.sectionName == "EmailData" && x.entryName == cmsentry).FirstOrDefault();
            if (cms != null)
            {
                string body = cms.cmsContent;
                if (replacements != null)
                {
                    foreach (KeyValuePair<string, string> kvp in replacements)
                    {
                        body = body.Replace(kvp.Key, kvp.Value);
                    }
                }
                SendEmail(from, to, subject, body, bcc);
            }
        }

        public static void SendEmail(string from, string to, string subject, string body, string bcc = "")
        {
            if (HttpContext.Current.Request.IsLocal)
                return;

            if (ConfigurationManager.AppSettings["Platform"] == "Azure")
            {
                _ = SendEmailAzureAsync(from, to, subject, body, bcc);
            }
            else
            {
                SendEmailSmtp(from, to, subject, body, bcc);
            }
        }

        private static void SendEmailSmtp(string from, string to, string subject, string body, string bcc = "")
        {
            MailMessage mail = new MailMessage(from, to, subject, body);

            if (ConfigurationManager.AppSettings["Environment"] != "Live")
            {
                Dictionary<string, string> emaildata = DataCache.GetSectionData("EmailData");
                mail.To.Clear();
                mail.To.Add(new MailAddress("devteam@netgiant.com"));
                mail.CC.Add(new MailAddress(Utilities.GetItemFromDict(emaildata, "TestEmail").ToString()));
                string subj = "GENERATED FROM TEST SYSTEM: EmailTo: " + to + ": " + subject;
                mail.Subject = subj.Length < 79 ? subj : subj.Substring(0, 78);
            }
            else if (bcc != "")
            {
                mail.Bcc.Add(new MailAddress(bcc));
            }

            mail.IsBodyHtml = true;
            SmtpClient smtp = new SmtpClient("localhost");
            try
            {
                smtp.Send(mail);
            }
            catch (Exception e)
            {
                ProcessException(e);
            }
        }

        private static async Task SendEmailSmtpAsync(string from, string to, string subject, string body, string bcc = "")
        {
            MailMessage mail = new MailMessage(from, to, subject, body);

            if (ConfigurationManager.AppSettings["Environment"] != "Live")
            {
                Dictionary<string, string> emaildata = DataCache.GetSectionData("EmailData");
                mail.To.Clear();
                mail.To.Add(new MailAddress("devteam@netgiant.com"));
                mail.CC.Add(new MailAddress(Utilities.GetItemFromDict(emaildata, "TestEmail").ToString()));
                string subj = "GENERATED FROM TEST SYSTEM: EmailTo: " + to + ": " + subject;
                mail.Subject = subj.Length < 79 ? subj : subj.Substring(0, 78);
            }
            else if (bcc != "")
            {
                mail.Bcc.Add(new MailAddress(bcc));
            }

            mail.IsBodyHtml = true;
            SmtpClient smtp = new SmtpClient("localhost");
            try
            {
                await smtp.SendMailAsync(mail);
            }
            catch (Exception e)
            {
                ProcessException(e);
            }
        }

        private static async Task SendEmailAzureAsync(string from, string to, string subject, string body, string bcc = "")
        {
            // Via SendGrid
            var apikey = ConfigurationManager.AppSettings["SendGridApiKey"];
            var client = new SendGridClient(apikey);
            var message = new SendGridMessage()
            {
                From = new EmailAddress(from),
                Subject = subject,
                PlainTextContent = body,
                HtmlContent = body
            };
            if (ConfigurationManager.AppSettings["Environment"] != "Live")
            {
                Dictionary<string, string> emaildata = DataCache.GetSectionData("EmailData");
                message.AddTo("devteam@netgiant.com");
                message.AddCc(Utilities.GetItemFromDict(emaildata, "TestEmail").ToString());
                string subj = "GENERATED FROM TEST SYSTEM: EmailTo: " + to + ": " + subject;
                message.Subject = subj.Length < 79 ? subj : subj.Substring(0, 78);
            }
            else
            {
                message.AddTo(to);
                if (bcc != "")
                {
                    message.AddBcc(bcc);
                }
            }

            var response = await client.SendEmailAsync(message);
            if (!response.IsSuccessStatusCode)
            {
                LogInformationMessage("Error sending email via SendGrid");
            }
        }

        public static void ScriptException(string url, string description)
        {
            Log log = new Log
            {
                WebsiteFk = int.Parse(ConfigurationManager.AppSettings["WebsiteId"]),
                User = (string)HttpContext.Current.Session["U_Email"],
                Type = 2,
                DateTime = DateTime.Now,
                StatusCode = 999,
                Url = url.Length > 200 ? url.Substring(0, 200) : url,
                QueryString = "",
                FormData = "",
                Description = "Script Error",
                Entry = $"<b>MESSAGE:</b> {description}{Environment.NewLine}{Environment.NewLine}",
                UserAgent = Touchpoints.GetUserAgent(HttpContext.Current.Request.UserAgent),
                LogStatusFk = DataCache.GetNgmdLookups(x => x.LookupType.LookupTypeName == "Log Status" && x.LookupName == "Error").FirstOrDefault().LookupId
            };

            EntityAccess.InsertLogEntry(log);
        }

        public static void ProcessException(Exception ex, string furtherInfo = "")
        {
            try
            {
                if (ConfigurationManager.AppSettings["Environment"] == "Local")
                    throw ex;

                var innerException = ex.InnerException != null ? ex.InnerException.ToString() : "";

                var route = HttpContext.Current.Request.RequestContext.RouteData.Values;
                var controller = route["controller"];
                var action = route["action"];
                var entityValidationErrors = "";

                if (ex is DbEntityValidationException)
                {
                    var entityException = ex as DbEntityValidationException;
                    foreach (var eve in entityException.EntityValidationErrors)
                    {
                        entityValidationErrors += $"Entity of type {eve.Entry.Entity.GetType().Name} in state {eve.Entry.State} has the following validation errors:{Environment.NewLine}";
                        foreach (var ve in eve.ValidationErrors)
                        {
                            entityValidationErrors += $"Property: {ve.PropertyName}, Error: {ve.ErrorMessage}{Environment.NewLine}";
                        }
                    }
                }

                Log log = new Log
                {
                    WebsiteFk = int.Parse(ConfigurationManager.AppSettings["WebsiteId"]),
                    User = (string)HttpContext.Current.Session["U_Email"],
                    Type = 1,
                    DateTime = DateTime.Now,
                    StatusCode = 500,
                    Url = HttpContext.Current.Request.Url.ToString().Length > 200 ? HttpContext.Current.Request.Url.ToString().Substring(0, 200) : HttpContext.Current.Request.Url.ToString(),
                    QueryString = HttpContext.Current.Request.QueryString.ToString(),
                    FormData = HttpContext.Current.Request.Form.ToString(),
                    Description = ex.GetType().ToString(),
                    Entry = $"<b>MESSAGE:</b> {ex.Message}{Environment.NewLine}{Environment.NewLine}" +
                            $"<b>INNER EXCEPTION:</b> {innerException}{Environment.NewLine}{Environment.NewLine}" +
                            $"<b>STACK TRACE:</b> {ex.StackTrace}{Environment.NewLine}{Environment.NewLine}" +
                            $"<b>ENTITY VALIDATION:</b> {entityValidationErrors}{Environment.NewLine}" +
                            $"<b>FURTHER INFO:</b> {furtherInfo}",
                    UserAgent = Touchpoints.GetUserAgent(HttpContext.Current.Request.UserAgent),
                    LogStatusFk = DataCache.GetNgmdLookups(x => x.LookupType.LookupTypeName == "Log Status" && x.LookupName == "Error").FirstOrDefault().LookupId
                };

                EntityAccess.InsertLogEntry(log);
            }
            catch { }
        }

        public static void ProcessException(WebException ex, string url)
        {
            var log = new Log
            {
                WebsiteFk = Convert.ToInt32(ConfigurationManager.AppSettings["WebsiteId"]),
                User = Convert.ToString(HttpContext.Current.Session["U_Email"]),
                Type = 1,
                DateTime = DateTime.Now,
                Url = Convert.ToString(HttpContext.Current.Request.Url).Length > 200 ? Convert.ToString(HttpContext.Current.Request.Url).Substring(0, 200) : Convert.ToString(HttpContext.Current.Request.Url),
                QueryString = Convert.ToString(HttpContext.Current.Request.QueryString),
                FormData = Convert.ToString(HttpContext.Current.Request.Form),
                Entry = $"<b>URL:</b> {url}{Environment.NewLine}{Environment.NewLine}" +
                        $"<b>MESSAGE:</b> {ex.Message}{Environment.NewLine}{Environment.NewLine}" +
                        $"<b>INNER EXCEPTION:</b> {Convert.ToString(ex.InnerException)}{Environment.NewLine}{Environment.NewLine}" +
                        $"<b>STACK TRACE:</b> {ex.StackTrace}{Environment.NewLine}{Environment.NewLine}",
                UserAgent = Touchpoints.GetUserAgent(HttpContext.Current.Request.UserAgent),
                LogStatusFk = DataCache.GetNgmdLookups(x => x.LookupType.LookupTypeName == "Log Status" && x.LookupName == "Error").FirstOrDefault().LookupId
            };

            if (ex.Response != null)
            {
                using (WebResponse response = ex.Response)
                {
                    HttpWebResponse httpResponse = (HttpWebResponse)response;
                    log.StatusCode = Convert.ToInt32(Convert.ToString(httpResponse.StatusCode));
                    using (Stream data = response.GetResponseStream())
                    {
                        string text = new StreamReader(data).ReadToEnd();
                        log.Description = text;
                    }
                }
            }
            else
            {
                log.StatusCode = 500;
                log.Description = Convert.ToString(ex.GetType());
            }

            EntityAccess.InsertLogEntry(log);
        }

        //public static async Task LogInformationMessage(string message)
        public static void LogInformationMessage(string message)
        {
            if (ConfigurationManager.AppSettings["VerboseLogging"] == "True" && HttpContext.Current != null)
            {
                Log log = new Log
                {
                    WebsiteFk = int.Parse(ConfigurationManager.AppSettings["WebsiteId"]),
                    User = (string)HttpContext.Current.Session["U_Email"],
                    Type = 2,
                    DateTime = DateTime.Now,
                    StatusCode = 0,
                    Url = HttpContext.Current.Request.Url.ToString().Length > 200 ? HttpContext.Current.Request.Url.ToString().Substring(0, 200) : HttpContext.Current.Request.Url.ToString(),
                    QueryString = HttpContext.Current.Request.QueryString.ToString(),
                    FormData = HttpContext.Current.Request.Form.ToString(),
                    Description = message.Length > 500 ? "Error" : message,
                    Entry = $"<b>MESSAGE:</b> {message}{Environment.NewLine}{Environment.NewLine}",
                    UserAgent = Touchpoints.GetUserAgent(HttpContext.Current.Request.UserAgent),
                    LogStatusFk = DataCache.GetNgmdLookups(x => x.LookupType.LookupTypeName == "Log Status" && x.LookupName == "Information").FirstOrDefault().LookupId
                };

                EntityAccess.InsertLogEntry(log);
            }
        }

        /// <summary>
        /// Used for testing purposes, only available on Dev system
        /// </summary>
        /// <param name="message"></param>
        /// <param name="newLog"></param>
        public static void WriteLogFile(string message, bool newLog = false)
        {
            if (ConfigurationManager.AppSettings["Environment"].ToString() != "Live")
            {
                string filePath = ConfigurationManager.AppSettings["LocalDirectory"].ToString() + "Log";
                if (!Directory.Exists(filePath))
                {
                    Directory.CreateDirectory(filePath);
                }

                using (StreamWriter sw = new StreamWriter(filePath + "\\Log.txt", true))
                {
                    sw.WriteLine(DateTime.Now + ": " + message);
                }
            }
        }

        public static string FormatViewData(ViewDataDictionary viewdata)
        {
            StringBuilder content = new StringBuilder();
            content.Append(Environment.NewLine + "------------- ViewData ----------" + Environment.NewLine);
            foreach (KeyValuePair<string, object> item in viewdata)
            {
                var type = item.Value == null ? "NULL" : item.Value.GetType().Name;
                string val = "UNKNOWN";
                try
                {
                    switch (type)
                    {
                        case "NULL":
                            {
                                val = "ISNULL";
                                break;
                            }
                        default:
                            {
                                val = item.Value.ToString();
                                break;
                            }
                    }
                }
                catch
                {
                    // Do nothing
                }
                content.Append(item.Key + ": " + val + Environment.NewLine);
            }
            return content.ToString();
        }

        public static string FormatSession(HttpSessionState session)
        {
            StringBuilder content = new StringBuilder();
            content.Append(Environment.NewLine + "------------- Session ----------" + Environment.NewLine);
            for (int i = 0; i < session.Contents.Count; i++)
            {
                if (session[i] != null)
                {
                    string type = session[i].GetType().Name;
                    string val = "UNKNOWN";
                    if ("Int32|String|Boolean|DateTime".Contains(type))
                    {
                        val = session[i].ToString();
                    }
                    else
                    {
                        val = "Type is " + type;
                    }
                    content.Append(session.Keys[i] + ": " + val + Environment.NewLine);
                }
                else
                {
                    content.Append(session.Keys[i] + ": is NULL" + Environment.NewLine);
                }
            }
            return content.ToString();
        }

        public static string FormatRequest(HttpRequest request)
        {
            StringBuilder content = new StringBuilder();
            content.Append(Environment.NewLine + "------------- Request ----------" + Environment.NewLine);
            foreach (string key in request.Params.Keys)
            {
                content.Append(key + ": " + request.Params[key] + Environment.NewLine);
            }
            return content.ToString();
        }

        public static void AddToRecentViewed(RecentlyViewed rv)
        {
            List<RecentlyViewed> lrv = new List<RecentlyViewed>();
            if (HttpContext.Current.Session["U_RecentlyViewed"] != null)
            {
                lrv = (List<RecentlyViewed>)HttpContext.Current.Session["U_RecentlyViewed"];
            }

            var i = lrv.FindIndex(x => x.Reference == rv.Reference);
            //if already visited remove it
            if (i >= 0)
            {
                lrv.Remove(lrv[i]);
            }

            lrv.Insert(0, rv);
            //limit to 6
            lrv.Take(6);
            HttpContext.Current.Session["U_RecentlyViewed"] = lrv;
        }

        public static List<SelectListItem> BuildTitleList()
        {
            List<SelectListItem> titles = new List<SelectListItem>();

            titles.Add(new SelectListItem() { Text = "Mr", Value = "Mr" });
            titles.Add(new SelectListItem() { Text = "Mrs", Value = "Mrs" });
            titles.Add(new SelectListItem() { Text = "Miss", Value = "Miss" });
            titles.Add(new SelectListItem() { Text = "Ms", Value = "Ms" });
            titles.Add(new SelectListItem() { Text = "Dr", Value = "Dr" });
            titles.Add(new SelectListItem() { Text = "Rev", Value = "Rev" });
            titles.Add(new SelectListItem() { Text = "Sir", Value = "Sir" });
            titles.Add(new SelectListItem() { Text = "Lord", Value = "Lord" });
            titles.Add(new SelectListItem() { Text = "Rt. Hon.", Value = "Rt. Hon." });
            titles.Add(new SelectListItem() { Text = "Hon.", Value = "Hon." });
            titles.Add(new SelectListItem() { Text = "Other", Value = "Other" });

            return titles;
        }

        public static void LoadApplicationVariables()
        {
            string vat = EntityAccess.ReadConfigurationSetting(x => x.sectionName == "Pricing" && x.settingName == "vat", false).FirstOrDefault().settingValue.ToString();
            decimal vatD = Convert.ToDecimal(vat) + 1;
            List<configurationSetting> lcs = EntityAccess.ReadConfigurationSetting(x => x.sectionName == "Website Application Variables", false);

            ConfigurationManager.AppSettings["UnsupportedBrowser"] = lcs.Find(x => x.settingName == "UnsupportedBrowser").settingValue.ToString();
            ConfigurationManager.AppSettings["EnhancedDebugging"] = lcs.Find(x => x.settingName == "EnhancedDebugging").settingValue.ToString();
            ConfigurationManager.AppSettings["ExcludeDebugging"] = lcs.Find(x => x.settingName == "ExcludeDebugging").settingValue.ToString();
            ConfigurationManager.AppSettings["ReCaptchaSecretKey"] = lcs.Find(x => x.settingName == "ReCaptchaSecretKey").settingValue.ToString();
            ConfigurationManager.AppSettings["ReCaptchaSiteKey"] = lcs.Find(x => x.settingName == "ReCaptchaSiteKey").settingValue.ToString();
            ConfigurationManager.AppSettings["AllowHPCrossSell"] = lcs.Find(x => x.settingName == "AllowHPCrossSell").settingValue.ToString();
            ConfigurationManager.AppSettings["SendGridApiKey"] = lcs.Find(x => x.settingName == "SendGridApiKey").settingValue.ToString();

            ConfigurationManager.AppSettings["BoLicense"] = lcs.Find(x => x.settingName == "site").settingValue.ToString();  // <-- NO LONGER USED
            string versionNo = lcs.Find(x => x.settingName == "VersionNumber").settingValue.ToString();
            ConfigurationManager.AppSettings["Version"] = versionNo;
            ConfigurationManager.AppSettings["CDN"] = lcs.Find(x => x.settingName == "CDN").settingValue.ToString().Replace("[version]", versionNo);
            ConfigurationManager.AppSettings["VatMultiplier"] = vatD.ToString();

            //ConfigurationManager.AppSettings["SagePayMode"] = lcs.Find(x => x.settingName == "SagePayMode").settingValue.ToString();
            ConfigurationManager.AppSettings["SagePayVendor"] = lcs.Find(x => x.settingName == "SagePayVendor").settingValue.ToString();
            ConfigurationManager.AppSettings["SagePayHostName"] = lcs.Find(x => x.settingName == "SagePayHostName").settingValue.ToString();
            ConfigurationManager.AppSettings["SagePayRegisterUrl"] = lcs.Find(x => x.settingName == "sagePayRegisterURL").settingValue.ToString();
            ConfigurationManager.AppSettings["SagePayNotificationUrl"] = lcs.Find(x => x.settingName == "SagePayNotificationURL").settingValue.ToString();
            ConfigurationManager.AppSettings["SagePayDeleteTokenUrl"] = lcs.Find(x => x.settingName == "sagePayDeleteTokenURL").settingValue.ToString();
            ConfigurationManager.AppSettings["SagePayBreakoutUrl"] = lcs.Find(x => x.settingName == "SagePayBreakoutURL").settingValue.ToString();

            ConfigurationManager.AppSettings["OpayoVendor"] = lcs.Find(x => x.settingName == "OpayoVendor").settingValue.ToString();
            ConfigurationManager.AppSettings["OpayoApiKey"] = lcs.Find(x => x.settingName == "OpayoApiKey").settingValue.ToString();
            ConfigurationManager.AppSettings["OpayoApiPassword"] = lcs.Find(x => x.settingName == "OpayoApiPassword").settingValue.ToString();
            ConfigurationManager.AppSettings["OpayoApiEndpoint"] = lcs.Find(x => x.settingName == "OpayoApiEndpoint").settingValue.ToString();
            ConfigurationManager.AppSettings["OpayoNotificationUrl"] = lcs.Find(x => x.settingName == "OpayoNotificationUrl").settingValue.ToString();
            ConfigurationManager.AppSettings["OpayoRepAPIUser"] = lcs.Find(x => x.settingName == "OpayoRepAPIUser").settingValue.ToString();
            ConfigurationManager.AppSettings["OpayoRepAPIPassword"] = lcs.Find(x => x.settingName == "OpayoRepAPIPassword").settingValue.ToString();

            ConfigurationManager.AppSettings["SearchApplication"] = lcs.Find(x => x.settingName == "SearchApplication").settingValue.ToString();
            ConfigurationManager.AppSettings["SLIDomain"] = lcs.Find(x => x.settingName == "SLIDomain").settingValue.ToString();

            ConfigurationManager.AppSettings["PayPalMode"] = lcs.Find(x => x.settingName == "PayPalMode").settingValue.ToString();
            ConfigurationManager.AppSettings["PayPalClientId"] = lcs.Find(x => x.settingName == "PayPalClientId").settingValue.ToString();
            ConfigurationManager.AppSettings["PayPalSecret"] = lcs.Find(x => x.settingName == "PayPalSecret").settingValue.ToString();
            ConfigurationManager.AppSettings["PayPalApiUrl"] = lcs.Find(x => x.settingName == "PayPalApiUrl").settingValue.ToString();

            ConfigurationManager.AppSettings["AmazonPayPrivateKey"] = lcs.Find(x => x.settingName == "AmazonPayPrivateKey").settingValue.ToString();
            ConfigurationManager.AppSettings["AmazonPayPublicKeyId"] = lcs.Find(x => x.settingName == "AmazonPayPublicKeyId").settingValue.ToString();
            ConfigurationManager.AppSettings["AmazonPayStoreId"] = lcs.Find(x => x.settingName == "AmazonPayStoreId").settingValue.ToString();
            ConfigurationManager.AppSettings["AmazonPayMerchantId"] = lcs.Find(x => x.settingName == "AmazonPayMerchantId").settingValue.ToString();

            ConfigurationManager.AppSettings["FacebookIsOn"] = lcs.Find(x => x.settingName == "FacebookIsOn").settingValue.ToString();
            ConfigurationManager.AppSettings["TwitterIsOn"] = lcs.Find(x => x.settingName == "TwitterIsOn").settingValue.ToString();
            ConfigurationManager.AppSettings["LinkedInIsOn"] = lcs.Find(x => x.settingName == "LinkedInIsOn").settingValue.ToString();
            ConfigurationManager.AppSettings["ChatIsOn"] = lcs.Find(x => x.settingName == "ChatIsOn").settingValue.ToString();
            ConfigurationManager.AppSettings["DiplomatOrderSourceDefault"] = lcs.Find(x => x.settingName == "DiplomatOrderSourceDefault").settingValue;
            ConfigurationManager.AppSettings["DiplomatOrderSourceWebPortal"] = lcs.Find(x => x.settingName == "DiplomatOrderSourceWebPortal").settingValue;

            ConfigurationManager.AppSettings["SuppressCompatiblesForPPC"] = lcs.Find(x => x.settingName == "SuppressCompatiblesForPPC").settingValue;
            ConfigurationManager.AppSettings["VerboseLogging"] = lcs.Find(x => x.settingName == "VerboseLogging").settingValue;
            ConfigurationManager.AppSettings["CustomerVoucherStockRef"] = lcs.Find(x => x.settingName == "CustomerVoucherStockRef").settingValue;

            ConfigurationManager.AppSettings["IsCompatibleUpsellActive"] = lcs.Find(x => x.settingName == "IsCompatibleUpsellActive").settingValue;
            ConfigurationManager.AppSettings["CompatibleUpsellRates"] = lcs.Find(x => x.settingName == "CompatibleUpsellRates").settingValue;

            lcs = EntityAccess.ReadConfigurationSetting(x => x.sectionName == "Website Other Variables");

            string[] promo = lcs.Find(x => x.settingName == "PPC Promo").settingValue.ToString().Split('~');
            ConfigurationManager.AppSettings["PPCOEMPromoIsOn"] = "False";
            if (promo[2] == "On")
            {
                ConfigurationManager.AppSettings["PPCOEMPromoIsOn"] = "True";
                ConfigurationManager.AppSettings["PPCOEMPromoCode"] = promo[0];
                ConfigurationManager.AppSettings["PPCOEMPromoDisc"] = promo[1];
            }
            ConfigurationManager.AppSettings["PPCCOMPPromoIsOn"] = "False";
            if (promo[5] == "On")
            {
                ConfigurationManager.AppSettings["PPCCOMPPromoIsOn"] = "True";
                ConfigurationManager.AppSettings["PPCCOMPPromoCode"] = promo[3];
                ConfigurationManager.AppSettings["PPCCOMPPromoDisc"] = promo[4];
            }

            string affpromo = lcs.Find(x => x.settingName == "Affiliate Promo").settingValue.ToString();
            ConfigurationManager.AppSettings["AffiliatePromoIsOn"] = "False";
            if (affpromo.Split('|')[0] == "On")
            {
                ConfigurationManager.AppSettings["AffiliatePromoIsOn"] = "True";
                ConfigurationManager.AppSettings["AffPromo"] = affpromo.Split('|')[1];
            }

            ConfigurationManager.AppSettings["MetaTitle"] = lcs.Find(x => x.settingName == "General Meta Title").settingValue.ToString();
            ConfigurationManager.AppSettings["MetaDescription"] = lcs.Find(x => x.settingName == "General Meta Description").settingValue.ToString();

            lcs = EntityAccess.ReadConfigurationSetting(x => x.sectionName == "BatchProgram", false);
            ConfigurationManager.AppSettings["ElasticSearchUri"] = lcs.Find(x => x.settingName == "ElasticsearchUri").settingValue;
            switch (Int16.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString()))
            {
                case 1:
                    {
                        ConfigurationManager.AppSettings["WebsiteShortCode"] = "tg";
                        break;
                    }
                case 2:
                    {
                        ConfigurationManager.AppSettings["WebsiteShortCode"] = "cm";
                        break;
                    }
            }
            try
            {
                var appversion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                DateTime appdate = new DateTime(2000, 1, 1)
                    .Add(new TimeSpan(
                        TimeSpan.TicksPerDay * appversion.Build +               // days since 1 January 2000
                        TimeSpan.TicksPerSecond * 2 * appversion.Revision));    // seconds since midnight, (multiply by 2 to get original)
                ConfigurationManager.AppSettings["AppVersion"] = appversion.ToString();
                ConfigurationManager.AppSettings["AppDate"] = appdate.ToString("G");
            }
            catch (Exception)
            {
                ConfigurationManager.AppSettings["AppVersion"] = "Not Available";
                ConfigurationManager.AppSettings["AppDate"] = "Not Available";
            }
        }

        public static Dictionary<string, string> AddStandardReplacements(Dictionary<string, string> replacements)
        {
            Dictionary<string, string> commonData = DataCache.GetSectionData("CommonData");
            if (replacements == null)
            {
                replacements = new Dictionary<string, string>();
            }

            if (!replacements.ContainsKey("[CDN]"))
            {
                replacements.Add("[CDN]", ConfigurationManager.AppSettings["CDN"]);
            }

            if (!replacements.ContainsKey("[delivery-date]"))
            {
                replacements.Add("[delivery-date]", HttpContext.Current.Session["D_standardDeliveryDay"] + " " + HttpContext.Current.Session["D_standardDeliveryMonthDay"]);
            }

            if (!replacements.ContainsKey("[delivery-date-overridden]"))
            {
                replacements.Add("[delivery-date-overridden]", GetItemFromDict(commonData, "DeliveryDateIsOverridden"));
            }

            if (!replacements.ContainsKey("[Tel-No]"))
            {
                replacements.Add("[Tel-No]", GetItemFromDict(commonData, "TelephoneNumber"));
            }

            if (!replacements.ContainsKey("[SupportEmail]"))
            {
                replacements.Add("[SupportEmail]", GetItemFromDict(commonData, "SupportEmail"));
            }

            if (!replacements.ContainsKey("[Year]"))
            {
                replacements.Add("[Year]", DateTime.Now.Year.ToString());
            }
            if (!replacements.ContainsKey("[SiteName]"))
            {
                replacements.Add("[SiteName]", GetItemFromDict(commonData, "SiteName"));
            }
            if (!replacements.ContainsKey("[ShortSiteName]"))
            {
                replacements.Add("[ShortSiteName]", GetItemFromDict(commonData, "ShortSiteName"));
            }

            return replacements;
        }

        public static string AddStandardReplacements(string replacements)
        {
            Dictionary<string, string> commonData = DataCache.GetSectionData("CommonData");
            if (replacements != "")
            {
                replacements += "&";
            }
            replacements += "CDN=" + ConfigurationManager.AppSettings["CDN"];
            replacements += "&delivery-date=" + HttpContext.Current.Session["D_standardDeliveryDay"] + " " + HttpContext.Current.Session["D_standardDeliveryMonthDay"];
            replacements += "&delivery-date-overridden=" + GetItemFromDict(commonData, "DeliveryDateIsOverridden");
            replacements += "&Tel-No=" + GetItemFromDict(commonData, "TelephoneNumber");
            replacements += "&SupportEmail=" + GetItemFromDict(commonData, "SupportEmail");
            replacements += "&Year=" + DateTime.Now.Year.ToString();

            return replacements;
        }

        /// <summary>
        /// Set the default delivery date
        /// </summary>
        public static void SetDeliveryDate()
        {
            DateTime deliveryDate = new DateTime();
            DateTime now = DateTime.Now;
            DateTime saturdayDate = now.AddDays((int)DayOfWeek.Saturday - (int)now.DayOfWeek);
            Dictionary<string, string> commonData = DataCache.GetSectionData("CommonData");

            HttpContext.Current.Session["D_SaturdayDeliveryDate"] = saturdayDate.Day.ToString() + GetOrdinal(saturdayDate) + " " + saturdayDate.ToString("MMMM yyyy");

            if (Convert.ToBoolean(GetItemFromDict(commonData, "DeliveryDateIsOverridden")))
            {
                string[] dd = GetItemFromDict(commonData, "DeliveryDateOverride").Split('/');
                deliveryDate = new DateTime(int.Parse(dd[0]), int.Parse(dd[1]), int.Parse(dd[2]));
                HttpContext.Current.Session["D_StandardDeliveryDate"] = deliveryDate;
                HttpContext.Current.Session["D_StandardDeliveryDay"] = deliveryDate.DayOfWeek.ToString();
                HttpContext.Current.Session["D_StandardDeliveryMonthDay"] = deliveryDate.Day.ToString() + GetOrdinal(deliveryDate);
                return;
            }
            DateTime currentDate = DateTime.Now;
            TimeSpan cutOffTime = new TimeSpan(17, 30, 00);

            switch (currentDate.DayOfWeek)
            {
                case DayOfWeek.Monday:
                case DayOfWeek.Tuesday:
                case DayOfWeek.Wednesday:
                    {
                        if (currentDate.TimeOfDay > cutOffTime)
                        {
                            deliveryDate = currentDate.AddDays(2);
                        }
                        else
                        {
                            deliveryDate = currentDate.AddDays(1);
                        }
                        break;
                    }
                case DayOfWeek.Thursday:
                    {
                        if (currentDate.TimeOfDay > cutOffTime)
                        {
                            deliveryDate = currentDate.AddDays(4);
                        }
                        else
                        {
                            deliveryDate = currentDate.AddDays(1);
                        }
                        break;
                    }
                case DayOfWeek.Friday:
                    {
                        if (currentDate.TimeOfDay > cutOffTime)
                        {
                            deliveryDate = currentDate.AddDays(4);
                        }
                        else
                        {
                            deliveryDate = currentDate.AddDays(3);
                        }
                        break;
                    }
                case DayOfWeek.Saturday:
                    {
                        deliveryDate = currentDate.AddDays(3);
                        break;
                    }
                case DayOfWeek.Sunday:
                    {
                        deliveryDate = currentDate.AddDays(2);
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
            HttpContext.Current.Session["D_StandardDeliveryDate"] = deliveryDate;
            HttpContext.Current.Session["D_StandardDeliveryDay"] = deliveryDate.DayOfWeek.ToString();
            HttpContext.Current.Session["D_StandardDeliveryMonthDay"] = deliveryDate.Day.ToString() + GetOrdinal(deliveryDate);
        }

        public static string CleanUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return "";

            var newUrl = Regex.Replace(url, @"[\,\(\)\[\]']", "");
            newUrl = Regex.Replace(newUrl, @"[\/\s\+\.\&]", "-");
            newUrl = newUrl.Replace("&amp;", "-");
            newUrl = Regex.Replace(newUrl, @"\-+", "-");
            return newUrl;
        }

        public static string RemoveControlCharacters(string inString)
        {
            if (inString == null) return null;
            StringBuilder newString = new StringBuilder();
            char ch;
            for (int i = 0; i < inString.Length; i++)
            {
                ch = inString[i];
                if (char.IsLetter(ch) || char.IsDigit(ch) || char.IsPunctuation(ch))
                {
                    newString.Append(ch);
                }
            }
            return newString.ToString();
        }

        public static CheckoutDropDownLists GetDropDownLists()
        {
            var dropdowns = DataCache.GetCheckoutDropDowns();

            return new CheckoutDropDownLists
            {
                CustomerType = dropdowns["Customer Type"],
                OrganisationType = dropdowns["Organisation Type"],
                Sector = dropdowns["Sector"],
                StaffCount = dropdowns["Staff Count"]
            };
        }

        public static string RemoveSpecialCharacters(string str)
        {
            return Regex.Replace(str, "[^a-zA-Z0-9_. ]+", "", RegexOptions.Compiled);
        }

        public static string SimpleDecryptString(string str)
        {
            if (str == null)
            {
                return null;
            }
            string decrypted;

            string EncryptionKey = "LYJ8EZG1L3GZBR3LOBL562CWGZ9OK89R8GLW"; // Ideally, this should  be stored securely
            str = str.Replace(" ", "+");
            byte[] cipherBytes = Convert.FromBase64String(str);
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] {
                    0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76
                });
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(cipherBytes, 0, cipherBytes.Length);
                        cs.Close();
                    }
                    decrypted = Encoding.Unicode.GetString(ms.ToArray());
                }
            }

            return decrypted;
        }

        public static string SimpleEncryptString(string str)
        {
            string encrypted = "";

            string EncryptionKey = "LYJ8EZG1L3GZBR3LOBL562CWGZ9OK89R8GLW"; // Ideally, this should  be stored securely
            byte[] clearBytes = Encoding.Unicode.GetBytes(str);
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] {
                    0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76
                });
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(clearBytes, 0, clearBytes.Length);
                        cs.Close();
                    }
                    encrypted = Convert.ToBase64String(ms.ToArray());
                }
            }

            return encrypted;
        }

        public static string HashSha256String(string str)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(str);
            SHA256Managed hashstring = new SHA256Managed();
            byte[] hash = hashstring.ComputeHash(bytes);
            StringBuilder sbHash = new StringBuilder();
            foreach (byte x in hash)
            {
                sbHash.Append(x.ToString("X2"));
            }
            return sbHash.ToString();
        }

        public static string GetVoucherCode()
        {
            string voucherCode = "";
            string seconds = Math.Round((DateTime.Now - (new DateTime(2018, 01, 01))).TotalSeconds, 0).ToString();

            char[] letters = { 'W', 'H', 'T', 'Y', 'E', 'A', 'P', 'L', 'F', 'D' };
            foreach (char c in seconds)
            {
                voucherCode += letters[int.Parse(c.ToString())];
            }

            return voucherCode;
        }

        private static string GetOrdinal(DateTime dt)
        {
            string ord = "th";
            switch (dt.Day)
            {
                case 1:
                case 21:
                case 31:
                    {
                        ord = "st";
                        break;
                    }
                case 2:
                case 22:
                    {
                        ord = "nd";
                        break;
                    }
                case 3:
                case 23:
                    {
                        ord = "rd";
                        break;
                    }
            }

            return ord;
        }

        public static string[] SplitCamelCase(string source)
        {
            return Regex.Split(source, @"(?<!^)(?=[A-Z])");
        }

        public static RouteValueDictionary CondAddToDict(RouteValueDictionary dict, bool condition, string name, object value)
        {
            if (condition) dict.Add(name, value);
            return dict;
        }

        public static List<T> ConvertDataTable<T>(DataTable dt)
        {
            List<T> data = new List<T>();
            foreach (DataRow row in dt.Rows)
            {
                T item = GetItem<T>(row);
                data.Add(item);
            }
            return data;
        }

        private static T GetItem<T>(DataRow dr)
        {
            Type temp = typeof(T);
            T obj = Activator.CreateInstance<T>();

            foreach (DataColumn column in dr.Table.Columns)
            {
                foreach (PropertyInfo pro in temp.GetProperties())
                {
                    if (pro.Name == column.ColumnName)
                        pro.SetValue(obj, dr[column.ColumnName], null);
                    else
                        continue;
                }
            }
            return obj;
        }

        public static bool GetBoolItemFromDict(Dictionary<string, string> dict, string key)
        {
            if (dict.ContainsKey(key))
            {
                return Convert.ToBoolean(dict[key]);
            }
            return false;
        }

        public static string GetItemFromDict(Dictionary<string, string> dict, string key)
        {
            if (dict.ContainsKey(key))
            {
                return dict[key];
            }
            return "";
        }
        public static string GetItemFromDict(Dictionary<string, string> dict, string key, bool useStandardReplacements)
        {
            string s = GetItemFromDict(dict, key);
            if (s.Contains("[") && useStandardReplacements)
            {
                Dictionary<string, string> replacements = new Dictionary<string, string>();
                replacements = Utilities.AddStandardReplacements(replacements);
                foreach (KeyValuePair<string, string> kvp in replacements)
                {
                    s = s.Replace(kvp.Key, kvp.Value);
                }
            }

            return s;
        }

        public static string GetLanguage(string websiteId)
        {
            return websiteId == "3" ? "5" : websiteId;
        }

        public static decimal GetUpsellRate()
        {
            decimal rate = decimal.Zero;
            if (Convert.ToBoolean(ConfigurationManager.AppSettings["IsCompatibleUpsellActive"]))
            {
                rate = decimal.Parse(ConfigurationManager.AppSettings["CompatibleUpsellRates"]);
            }

            return rate;
        }

        public static List<Tuple<string, string, string>> BuildCacheDictionary<T>(List<T> list, string prop1, string prop2, string prop3) where T : class
        {
            List<Tuple<string, string, string>> d = new List<Tuple<string, string, string>>();

            foreach (T el in list)
            {
                d.Add(new Tuple<string, string, string>(
                    el.GetType().GetProperty(prop1).GetValue(el, null).ToString(),
                    el.GetType().GetProperty(prop2).GetValue(el, null).ToString(),
                    el.GetType().GetProperty(prop3).GetValue(el, null).ToString()
                    ));
            }

            return d;
        }

        public static T LoadSession<T>(string sessionVariable)
        {
            T obj = Activator.CreateInstance<T>();
            if (HttpContext.Current.Session[sessionVariable] != null)
            {
                obj = (T)HttpContext.Current.Session[sessionVariable];
            }

            return obj;
        }

        public static void SendAnalyticsEvent(string category, string action, string label)
        {
            int websiteId = int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString());
            string gaCode = websiteId == 1 ? "UA-1727271-1"
                                : websiteId == 2 ? "UA-788128-1" : "UA-49671214-1";
            ASCIIEncoding encoding = new ASCIIEncoding();
            string cid = Guid.NewGuid().ToString();

            string postData =
                "v=1&tid=" + gaCode + "&cid=" + cid + "&t=event" +
                "&ec=" + category +
                "&ea=" + action +
                "&el=" + label;

            byte[] data = encoding.GetBytes(postData);

            Utilities.SetTlsVersion();
            var client = new RestClient("https://www.google-analytics.com");

            var request = new RestRequest("/collect", RestSharp.Method.Post);
            request.AddParameter("application/x-www-form-urlencoded", postData);
            var response = client.Execute(request, RestSharp.Method.Get);
        }

        public static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        public static string GetClientIPAddress(HttpRequestBase request)
        {
            // CloudFlare
            if (request.Headers["CDN-LOOP"] != null)
            {
                return request.Headers["CF-CONNECTING-IP"].Split(',')[0];
            }
            else
            {
                //Forwarded For
                if (request.Headers["X-FORWARDED-FOR"] != null)
                {
                    return request.Headers["X-FORWARDED-FOR"].Split(',')[0];
                }
            }

            //None
            return request.UserHostAddress.Split(',')[0];
        }

        public static void SetTlsVersion()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls13 | SecurityProtocolType.Tls12;
        }
    }

    public class MVCTransferResult : RedirectResult
    {
        public MVCTransferResult(string url)
            : base(url)
        {
        }

        public MVCTransferResult(object routeValues) : base(GetRouteURL(routeValues))
        {
        }

        private static string GetRouteURL(object routeValues)
        {
            UrlHelper url = new UrlHelper(new RequestContext(new HttpContextWrapper(HttpContext.Current), new RouteData()), RouteTable.Routes);
            return url.RouteUrl(routeValues);
        }

        public override void ExecuteResult(ControllerContext context)
        {
            var httpContext = HttpContext.Current;

            // ASP.NET MVC 3.0
            if (context.Controller.TempData != null &&
                context.Controller.TempData.Count() > 0)
            {
                throw new ApplicationException("TempData won't work with Server.TransferRequest!");
            }

            httpContext.Server.TransferRequest(Url, true); // change to false to pass query string parameters if you have already processed them

            // ASP.NET MVC 2.0
            //httpContext.RewritePath(Url, false);
            //IHttpHandler httpHandler = new MvcHttpHandler();
            //httpHandler.ProcessRequest(HttpContext.Current);
        }
    }

    // Enums
    public enum BrandFlag : int
    {
        Original = 1,
        Compatible
    }
    public enum ProductFlag : int
    {
        Assembly,
        Product,
        Ancillary
    }

    public enum PaymentMethod : int
    {
        CreditDebit,
        PayPal,
        Phone,
        BACS,
        Account,
        Cheque,
        AccountApplication
    }

    public enum BasketItemType : int
    {
        Item = 1,
        AdminDiscount,
        Alert,
        Delivery,
        Voucher,
        CompatibleDiscount
    }

    public class SaveReturn
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = "";
        public string InnerException { get; set; }
        public string EntityValidationError { get; set; }
        public string Html { get; set; }
    }

    public class BasketContents
    {
        public string StockRef { get; set; }
        public int ProductId { get; set; }
        public string PartNo { get; set; }
        public int CategoryNo { get; set; }
        public int GroupNo { get; set; }
        public string GroupName { get; set; }
        public int Quantity { get; set; }
        public int QtyStart { get; set; }
        public int Type { get; set; }
        public BasketItemType ItemType { get; set; }
        public int LineUid { get; set; }
        public decimal PriceEx { get; set; }
        public decimal PriceInc { get; set; }
        public string Description { get; set; }
        public int Availability { get; set; }
        public string ImageUrl { get; set; }
        public string ProductUrl { get; set; }
        public bool IsCompatible { get; set; }
        public bool IsCompatibleInk { get; set; }
        public bool IsBulky { get; set; }
        public bool IsSpecialOrder { get; set; }
        public bool IsVatExempt { get; set; }
        public bool IsVoucherQualifyingItem { get; set; } = false;
        public bool IsUpsellTriggered { get; set; } = false;
        public int VoucherType { get; set; } = 0;
        public decimal VoucherAmount { get; set; }
        public bool IsFreeGift { get; set; } = false;
        public int DeliveryMethod { get; set; }
        public string AffiliateCommissionGroup { get; set; }
        public string CrossSellingStockRef { get; set; }
        public decimal CrossSellingPriceEx { get; set; }
        public int CrossSellingAvailability { get; set; }
        public string CrossSellingDescription { get; set; }
        public bool ExcludeFromUpSell { get; set; } = false;
        public string CrossSellingImageURL { get; set; }
        public List<BasketContents> AddonProducts = new List<BasketContents>();
    }

    public class BasketTotals
    {
        public int Quantity { get; set; }
        public decimal TotalExcVat { get; set; }
        public decimal TotalIncVat { get; set; }
        public decimal GrandTotalIncVat { get; set; }
        public decimal GrandTotalExcVat { get; set; }
        public decimal Vat { get; set; }
        public decimal Voucher { get; set; }
        public decimal VoucherVat { get; set; }
        public decimal Delivery { get; set; }
    }

    public class SignIn
    {
        [Required(ErrorMessage = "Please enter your email address")]
        [RegularExpression(@"^([a-zA-Z0-9_\-\.\']+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{2,20}|[0-9]{1,3})(\]?)$", ErrorMessage = "Please enter a valid email address")]
        public string UserName { get; set; }
        [Required(ErrorMessage = "Please enter a password")]
        public string Password { get; set; }
        public bool IsNewCustomer { get; set; }
    }

    public class SignUp
    {
        [Required(ErrorMessage = "Please enter your email address")]
        [MinLength(6, ErrorMessage = "Please enter your email address")]
        [StringLength(50, ErrorMessage = "Email cannot be longer than 50 characters")]
        [RegularExpression(@"^([a-zA-Z0-9_\-\.\']+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{2,20}|[0-9]{1,3})(\]?)$", ErrorMessage = "Please enter a valid email address")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Please enter your password")]
        [MaxLength(128, ErrorMessage = "Password cannot be longer than 128 characters")]
        [MinLength(4, ErrorMessage = "Password cannot be less than 4 characters")]
        public string Password { get; set; }

        public Name Name { get; set; }

        [Required(ErrorMessage = "Please enter a valid contact number")]
        [RegularExpression(@"^(?:(?:\(?(?:0(?:0|11)\)?[\s-]?\(?|\+)44\)?[\s-]?(?:\(?0\)?[\s-]?)?)|(?:\(?0))(?:(?:\d{5}\)?[\s-]?\d{4,5})|(?:\d{4}\)?[\s-]?(?:\d{5}|\d{3}[\s-]?\d{3}))|(?:\d{3}\)?[\s-]?\d{3}[\s-]?\d{3,4})|(?:\d{2}\)?[\s-]?\d{4}[\s-]?\d{4}))(?:[\s-]?(?:x|ext\.?|\#)\d{3,4})?$", ErrorMessage = "Please enter a valid telephone number.")]
        public string TelNumber { get; set; }

        public Address Address { get; set; }

        public string AddressLookup { get; set; }

        public bool Newsletter { get; set; }
    }

    public class CheckoutDetails
    {
        public Name Name { get; set; }
        public Name RecipientName { get; set; }
        public Address BillingAddress { get; set; }
        public Address DeliveryAddress { get; set; }

        [Required(ErrorMessage = "Please enter a valid contact number")]
        [RegularExpression(@"^(?:(?:\(?(?:0(?:0|11)\)?[\s-]?\(?|\+)44\)?[\s-]?(?:\(?0\)?[\s-]?)?)|(?:\(?0))(?:(?:\d{5}\)?[\s-]?\d{4,5})|(?:\d{4}\)?[\s-]?(?:\d{5}|\d{3}[\s-]?\d{3}))|(?:\d{3}\)?[\s-]?\d{3}[\s-]?\d{3,4})|(?:\d{2}\)?[\s-]?\d{4}[\s-]?\d{4}))(?:[\s-]?(?:x|ext\.?|\#)\d{3,4})?$", ErrorMessage = "Please enter a valid telephone number.")]
        public string TelephoneNumber { get; set; }
        public string Email { get; set; }
        [Required(ErrorMessage = "Password is required")]
        [MaxLength(128, ErrorMessage = "Password cannot be longer than 128 characters")]
        [MinLength(6, ErrorMessage = "Password cannot be less than 6 characters")]
        public string Password { get; set; }
        public bool IsNewCustomer { get; set; } = true;
        public bool UseASavedCard { get; set; }
        public bool SaveThisCard { get; set; } = false;
        public string VoucherCode { get; set; }
        public decimal TotalIncVat { get; set; }
        public bool Newsletter { get; set; } = false;
        public bool NewsletterInverse { get; set; } = false;
        public bool SuppressEmail { get; set; } = false;
        [StringLength(20, ErrorMessage = "Reference cannot be longer than 20 characters")]
        public string Reference { get; set; }
        public int DeliveryServiceId { get; set; }
        public int DeliveryMethod { get; set; }
        public bool ZeroStock { get; set; } = false;
        public bool IsSpecialOrder { get; set; } = false;
        public DateTime OrderDate { get; set; }
        public int SaveOrderCount { get; set; } = 0;

        // Payment
        [Required(ErrorMessage = "Please select a Payment Method")]
        public string PaymentMethod { get; set; }
        public string SagePayCardId { get; set; }
        public string SagePayRef { get; set; }
        public string CardType { get; set; }
        public string CardLast4Digits { get; set; }
        public string SageToken { get; set; }
        public string PayPalRef { get; set; }
        public string BackOfficeOrderRef { get; set; }
        public string BackOfficeCustRef { get; set; }
        public string SagePayUid { get; set; }
        public string SagePaySecurityKey { get; set; }
        public string SagePayTxCode { get; set; }
        public string SagePayAuthCode { get; set; }
        public int JsonStoreId { get; set; }
        public decimal PaymentAmountPaid { get; set; }
        public bool IsInterimOrder { get; set; } = false;
        public MerchantSessionKey MerchantSessionKey { get; set; }

        public OpayoChallengeAuthentication OpayoChallengeAuthentication { get; set; }

        // Account Customers
        public string AccountNumber { get; set; }
        public string AccountRecord { get; set; }
        public string AccountContact { get; set; }
        public string AccountTelNo { get; set; }
        public string AccountEmail { get; set; }
        public string AccountInvoiceAddress { get; set; }

        // Portal Only
        public string OrderNote { get; set; }
    }

    public enum AccountApplicationSection
    {
        CustomerType,
        CreditAccountText,
        ContactDetails,
        OrganisationInformation,
        CompanyIdentifier,
        BillingAddress,
        Payment,
        CreditTerms,
        All
    }

    public class AccountApplicationDetails
    {
        public AccountApplicationDetails()
        {
            OrganisationType = Int16.Parse(DropDownList.OrganisationType.Find(x => x.Text == "Unspecified").Value);
            Sector = Int16.Parse(DropDownList.Sector.Find(x => x.Text == "Unspecified").Value);
            StaffCount = Int16.Parse(DropDownList.StaffCount.Find(x => x.Text == "Unspecified").Value);
            StaffOrderCount = Int16.Parse(DropDownList.StaffCount.Find(x => x.Text == "Unspecified").Value);

        }
        public AccountApplicationSection Section { get; set; } = AccountApplicationSection.All;

        public CheckoutDropDownLists DropDownList { get; } = Utilities.GetDropDownLists();

        public Dictionary<string, string> Data { get; } = DataCache.GetSectionData("AccountApplicationData");

        public short CustomerType { get; set; }

        [Required(ErrorMessage = "Trading or organisation name is required")]
        public string TradingName { get; set; }

        [Required(ErrorMessage = "Contact full name is required")]
        public string ContactName { get; set; }

        [Required(ErrorMessage = "Contact email address is required")]
        [RegularExpression(@"^([a-zA-Z0-9_\-\.\']+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{2,20}|[0-9]{1,3})(\]?)$", ErrorMessage = "Please enter a valid email address")]
        public string ContactEmail { get; set; }

        [Required(ErrorMessage = "Contact telephone number is required")]
        [RegularExpression(@"^(?:(?:\(?(?:0(?:0|11)\)?[\s-]?\(?|\+)44\)?[\s-]?(?:\(?0\)?[\s-]?)?)|(?:\(?0))(?:(?:\d{5}\)?[\s-]?\d{4,5})|(?:\d{4}\)?[\s-]?(?:\d{5}|\d{3}[\s-]?\d{3}))|(?:\d{3}\)?[\s-]?\d{3}[\s-]?\d{3,4})|(?:\d{2}\)?[\s-]?\d{4}[\s-]?\d{4}))(?:[\s-]?(?:x|ext\.?|\#)\d{3,4})?$", ErrorMessage = "Please enter a valid telephone number.")]
        public string ContactTelephoneNumber { get; set; }

        [Required(ErrorMessage = "Organisation type is required")]
        public short OrganisationType { get; set; }

        [Required(ErrorMessage = "Company registration number is required")]
        public string CompanyRegistrationNumber { get; set; }

        [Required(ErrorMessage = "Company VAT number is required")]
        public string CompanyVATNumber { get; set; }

        [Required(ErrorMessage = "Sector is required")]
        public short Sector { get; set; }

        [Required(ErrorMessage = "Number of staff is required")]
        public short StaffCount { get; set; }

        [Required(ErrorMessage = "Number of staff ordering is required")]
        public short StaffOrderCount { get; set; }

        [AssertThat("(CustomerType==2 && MonthlySpend>=200) || (CustomerType==3 && MonthlySpend>0)", ErrorMessage = "Monthly spend must be over £200")]
        [Required(ErrorMessage = "Estimated monthly spend is required")]
        public decimal MonthlySpend { get; set; }

        public short Status { get; } = 4;

        public Address BillingAddress { get; set; }

        [Required(ErrorMessage = "Billing full name is required")]
        public string BillingFullName { get; set; }

        [Required(ErrorMessage = "Billing email address is required")]
        [RegularExpression(@"^([a-zA-Z0-9_\-\.\']+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{2,20}|[0-9]{1,3})(\]?)$", ErrorMessage = "Please enter a valid email address")]
        public string BillingEmail { get; set; }

        [Required(ErrorMessage = "Billing telephone number is required")]
        [RegularExpression(@"^(?:(?:\(?(?:0(?:0|11)\)?[\s-]?\(?|\+)44\)?[\s-]?(?:\(?0\)?[\s-]?)?)|(?:\(?0))(?:(?:\d{5}\)?[\s-]?\d{4,5})|(?:\d{4}\)?[\s-]?(?:\d{5}|\d{3}[\s-]?\d{3}))|(?:\d{3}\)?[\s-]?\d{3}[\s-]?\d{3,4})|(?:\d{2}\)?[\s-]?\d{4}[\s-]?\d{4}))(?:[\s-]?(?:x|ext\.?|\#)\d{3,4})?$", ErrorMessage = "Please enter a valid telephone number.")]
        public string BillingTelephoneNumber { get; set; }

        public bool DirectDebit { get; set; } = true;

        [AssertThat("AcceptStandardTerms == true", ErrorMessage = "Please accept the standard terms")]
        public bool AcceptStandardTerms { get; set; }

        [AssertThat("AcceptCreditTerms == true", ErrorMessage = "Please accept the credit terms")]
        public bool AcceptCreditTerms { get; set; }
    }

    public class TradeApplicationDetails
    {
        public TradeApplicationDetails()
        {

        }
        public AccountApplicationSection Section { get; set; } = AccountApplicationSection.All;
        public CheckoutDropDownLists DropDownList { get; } = Utilities.GetDropDownLists();

        public Dictionary<string, string> Data { get; } = DataCache.GetSectionData("AccountApplicationData");


        [Required(ErrorMessage = "Trading or organisation name is required")]
        public string TradingName { get; set; }

        [Required(ErrorMessage = "Contact full name is required")]
        public string ContactName { get; set; }

        [Required(ErrorMessage = "Contact email address is required")]
        [RegularExpression(@"^([a-zA-Z0-9_\-\.\']+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{2,20}|[0-9]{1,3})(\]?)$", ErrorMessage = "Please enter a valid email address")]
        public string ContactEmail { get; set; }

        [Required(ErrorMessage = "Contact telephone number is required")]
        [RegularExpression(@"^(?:(?:\(?(?:0(?:0|11)\)?[\s-]?\(?|\+)44\)?[\s-]?(?:\(?0\)?[\s-]?)?)|(?:\(?0))(?:(?:\d{5}\)?[\s-]?\d{4,5})|(?:\d{4}\)?[\s-]?(?:\d{5}|\d{3}[\s-]?\d{3}))|(?:\d{3}\)?[\s-]?\d{3}[\s-]?\d{3,4})|(?:\d{2}\)?[\s-]?\d{4}[\s-]?\d{4}))(?:[\s-]?(?:x|ext\.?|\#)\d{3,4})?$", ErrorMessage = "Please enter a valid telephone number.")]
        public string ContactTelephoneNumber { get; set; }

        [Required(ErrorMessage = "Company registration number is required")]
        public string CompanyRegistrationNumber { get; set; }

        [Required(ErrorMessage = "Company VAT number is required")]
        public string CompanyVATNumber { get; set; }

        public Address BillingAddress { get; set; }

        [AssertThat("AcceptStandardTerms == true", ErrorMessage = "Please accept the standard terms")]
        public bool AcceptStandardTerms { get; set; }

        [AssertThat("NumberOffices>0", ErrorMessage = "Please enter the Number of Offices you have")]
        [Required(ErrorMessage = "Number of offices is required")]
        public short NumberOffices { get; set; }

        [AssertThat("NumberPrinters>0", ErrorMessage = "Please enter the total Number of Printers you have")]
        [Required(ErrorMessage = "Number of printers is required")]
        public short NumberPrinters { get; set; }
    }

    public class Name
    {
        [Required(ErrorMessage = "Please select your title")]
        [StringLength(25, ErrorMessage = "Title cannot be longer than 25 characters")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Please enter a first name")]
        [StringLength(20, ErrorMessage = "First name cannot be longer than 20 characters")]
        public string Firstname { get; set; }

        [Required(ErrorMessage = "Please enter a last name")]
        [StringLength(20, ErrorMessage = "Last name cannot be longer than 20 characters")]
        public string Surname { get; set; }
    }

    public class Address
    {
        [DisplayFormat(ConvertEmptyStringToNull = false)]
        [StringLength(30, ErrorMessage = "Company Name cannot be longer than 30 characters")]
        public string Line1 { get; set; }

        [DisplayFormat(ConvertEmptyStringToNull = false)]
        [Required(ErrorMessage = "Address Line 1 is required")]
        [StringLength(30, ErrorMessage = "Address Line 1 cannot be longer than 30 characters")]
        public string Line2 { get; set; }

        [DisplayFormat(ConvertEmptyStringToNull = false)]
        [StringLength(30, ErrorMessage = "Address Line 2 cannot be longer than 30 characters")]
        public string Line3 { get; set; }

        [DisplayFormat(ConvertEmptyStringToNull = false)]
        [Required(ErrorMessage = "Town or City is required")]
        [StringLength(30, ErrorMessage = "Town or City cannot be longer than 30 characters")]
        public string Line4 { get; set; }

        [DisplayFormat(ConvertEmptyStringToNull = false)]
        [StringLength(30, ErrorMessage = "County cannot be longer than 30 characters")]
        public string Line5 { get; set; }

        [DisplayFormat(ConvertEmptyStringToNull = false)]
        [Required(ErrorMessage = "Postcode is required")]
        [StringLength(30, ErrorMessage = "Postcode cannot be longer than 30 characters")]
        [RegularExpression(@"([Gg][Ii][Rr] 0[Aa]{2})|((([A-Za-z][0-9]{1,2})|(([A-Za-z][A-Ha-hJ-Yj-y][0-9]{1,2})|(([A-Za-z][0-9][A-Za-z])|([A-Za-z][A-Ha-hJ-Yj-y][0-9]?[A-Za-z]))))\s?[0-9][A-Za-z]{2})", ErrorMessage = "Please enter a valid postcode")]
        public string PostCode { get; set; }

        [DisplayFormat(ConvertEmptyStringToNull = false)]
        [StringLength(2, ErrorMessage = "Country cannot be longer than 2 characters")]
        public string Country { get; set; } = "GB";

        public int Id { get; set; }
    }

    public class CheckoutDropDownLists
    {
        public List<SelectListItem> CustomerType { get; set; }
        public List<SelectListItem> OrganisationType { get; set; }
        public List<SelectListItem> Sector { get; set; }
        public List<SelectListItem> StaffCount { get; set; }
    }

    public class WizardLists
    {
        public List<SelectListItem> ManufacturerList { get; set; }
        public List<SelectListItem> FamilyList { get; set; }
        public List<ExtdSelectListItem> EquipList { get; set; }
        public List<ExtdSelectListItem> CartridgeList { get; set; }
        public string CartridgeTypeName { get; set; }
        public string CartridgeTypeNote { get; set; }
        public string PriorityNote { get; set; }
        public string SecondaryNote { get; set; }
    }

    public class ExtdSelectListItem
    {
        public string Text { get; set; }
        public string Value { get; set; }
        public object Data { get; set; }
        public int LinkingId { get; set; }
    }

    public class EquipmentEntry
    {
        public string ModelName { get; set; }
        public string ImageUrl { get; set; }
        public string UrlLink { get; set; }
        public string CartridgeType { get; set; }
        public int ManufacturerId { get; set; }
    }

    public class CategoryEntry
    {
        public string Name { get; set; }
        public int BoGroupNo { get; set; }
        public string Url { get; set; }
        public int HasCategories { get; set; }
        public int HasProducts { get; set; }
    }

    public class SearchList
    {
        public string Model { get; set; }
        public string Description { get; set; }
        public string CrossSellModel { get; set; }
        public string CrossSellDescription { get; set; }
        public string CrossSellManufacturer { get; set; }
        public string FriendlyModel { get; set; }
        public string FriendlyDescription { get; set; }
        public string ItemType { get; set; }
        public int ItemId { get; set; }
        public string ImageUrl { get; set; }
        public string UrlLink { get; set; }
        public string CartridgeType { get; set; }
        public string ManufacturerName { get; set; }
        public int ManufacturerId { get; set; }
        public int ProductCount { get; set; }
        public string ProductUrl { get; set; }
        public string SliLoggingUrl { get; set; }
        public string ProductType { get; set; }
        public string MetaKeywords { get; set; }
        public ProductEntry Product { get; set; }
    }

    public class SLIBanner
    {
        public string Name { get; set; }
        public string Placement { get; set; }
        public string Type { get; set; }
        public string Content { get; set; }
    }

    public class ProductComponent
    {
        public int ProductId { get; set; }
        public int AttValue8 { get; set; }
        public int AttValue9 { get; set; }
        public string AttDesc8 { get; set; }
        public int PageYield { get; set; }
        public string Capacity { get; set; }
        public int PackQuantity { get; set; }
    }

    public class AlertProduct
    {
        public int ProductId { get; set; }
        public string StockRef { get; set; }
        public string Url { get; set; }
        public string ImageUrl { get; set; }
        public string PartNo { get; set; }
        public string Description { get; set; }
    }

    public class ProductEntry
    {
        public int ProductId { get; set; }
        public string Url { get; set; }
        public string ImageUrl { get; set; }
        public string PartNo { get; set; }
        public string BarCode { get; set; }
        public string Description { get; set; }
        public string Brand { get; set; }
        public int Availability { get; set; }
        public int AttValue5 { get; set; }
        public int AttValue6 { get; set; }
        public int AttValue8 { get; set; }
        public int AttValue9 { get; set; }
        public string AttDesc6 { get; set; }
        public string AttDesc7 { get; set; }
        public string AttDesc8 { get; set; }
        public string AttDesc9 { get; set; }
        public int ManufacturerId { get; set; }
        public int BoBrandNo { get; set; }
        public string SpecLine2 { get; set; }
        public string SpecLine3 { get; set; }
        public string SpecLine6 { get; set; }
        public string OfferFilterText { get; set; }
        public string OfferSashImage { get; set; }
        public string OfferBullet { get; set; }
        public int ItemType { get; set; }
        public string Type { get; set; }
        public string Reference { get; set; }
        public BrandFlag BrandFlag { get; set; }
        public decimal PriceRetIncVat { get; set; }
        public decimal PriceTrExVat { get; set; }
        public decimal PriceSaleIncVat { get; set; }
        public decimal PPCPromoPriceIncVat { get; set; }
        public decimal BreakQty1 { get; set; }
        public decimal BreakQty2 { get; set; }
        public decimal BreakQty3 { get; set; }
        public decimal BreakPrice2IncVat { get; set; }
        public decimal BreakPrice3IncVat { get; set; }
        public int PageYield { get; set; }
        public string Capacity { get; set; }
        public double AssemblySaving { get; set; }
        public int AssemblyCount { get; set; }
        public int PackQuantity { get; set; }
        public bool IsStationerySaleItem { get; set; } = false;
        public List<ProductComponent> ComponentList { get; set; }
        public bool OEMSaleIsApplicable { get; set; } = false;
        public bool CompatibleSaleIsApplicable { get; set; } = false;

        // Generated
        public int ParentProductId { get; set; }
        public string ProductAltFilter { get; set; }

        //Product Only
        public string ManuRef { get; set; }
        public int AttribValue4 { get; set; }
        public int AttribValue7 { get; set; }
        public string SpecLine1 { get; set; }
        public string SpecLine4 { get; set; }
        public string ProductNotes { get; set; }
        public string AxisGroupNo { get; set; }
        public string ProductGroup { get; set; }
        public string CategoryCodeName { get; set; }
        public int ProductTypeID { get; set; }
        public string ProductVideoURL { get; set; }
        public int CrossSellProductID { get; set; }
        public string CrossSellProductURL { get; set; }
        public string CrossSellDescription { get; set; }
        public string CrossSellBrand { get; set; }
        public int CrossSellStatus { get; set; }
        public decimal CrossSellPriceIncVat { get; set; }
        public decimal CrossSellTradePrice { get; set; }
        public string CrossSellImage { get; set; }
        public string CrossSellRef { get; set; }
        public decimal CrossSellSaving { get; set; }
        public int CrossSellPageYield { get; set; }
        public string DSNotes { get; set; }
        public string PriorityNote { get; set; }
        public bool DSSuppress { get; set; }
        public string MetaTitle { get; set; }
        public string MetaDesc { get; set; }
        public string MetaKeywords { get; set; }
        public int FeeFoCount { get; set; }
        public decimal FeeFoRating { get; set; }

        //Sorting
        public ProductFlag PrimarySortSeq { get; set; }
        public int SecondarySortSeq { get; set; }

        // Attributes
        public List<ProductAttribute> Attributes { get; set; }
    }

    public class ProductAttribute
    {
        public int Number { get; set; }
        public string Name { get; set; }
        public string ValueId { get; set; }
        public string Value { get; set; }
    }

    public class MiniProductEntry
    {
        public int ProductId { get; set; }
        public string Url { get; set; }
        public string ImageUrl { get; set; }
        public string Description { get; set; }
        public string Brand { get; set; }
        public int Availability { get; set; }
        public string Reference { get; set; }
        public string PartNo { get; set; }
        public decimal PriceRetIncVat { get; set; }
        public decimal PriceTrExVat { get; set; }
        public int PageYield { get; set; }
        public string AttDesc8 { get; set; }
        public int AttValue4 { get; set; }
        public int AttValue8 { get; set; }
        public int AttValue9 { get; set; }
        public int AssemblyCount { get; set; }
        public ProductFlag PrimarySortSeq { get; set; }
    }


    public class QandA
    {
        public string Question { get; set; }
        public string Answer { get; set; }
        public string Date { get; set; }
        public bool GenerateSchema { get; set; }
    }

    public class ProductFilter
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ElementId { get; set; }
        public string ElementName { get; set; }
        public bool Selected { get; set; }
        public int Count { get; set; }
        public string AdditionalSortField { get; set; }
    }

    public class CarouselEntry
    {
        public string MainImageUrl { get; set; }
        public string ThumbnailUrl { get; set; }
        public string ZoomImageUrl { get; set; }
    }

    public class SagePayNotification
    {
        // Custom fields
        public bool Breakout { get; set; }
        public bool IsSuccess { get; set; } = false;
        public int JsonStoreId { get; set; }

        // SagePay fields
        public string VendorName { get; set; }
        public string VPSProtocol { get; set; }
        public string TxType { get; set; }
        public string VendorTxCode { get; set; }
        public string VPSTxID { get; set; }
        public string Status { get; set; }
        public string StatusDetail { get; set; }
        public string TxAuthNo { get; set; }
        public string Token { get; set; }
        public string AVSCV2 { get; set; }
        public string AddressResult { get; set; }
        public string PostCodeResult { get; set; }
        public string CV2Result { get; set; }
        public string GiftAid { get; set; }
        public string ThreeDSecureStatus { get; set; }
        public string CAVV { get; set; }
        public string AddressStatus { get; set; }
        public string PayerStatus { get; set; }
        public string CardType { get; set; }
        public string Last4Digits { get; set; }
        public string VPSSignature { get; set; }
        public string FraudResponse { get; set; }
        public string Surcharge { get; set; }
        public string ExpiryDate { get; set; }
        public string BankAuthCode { get; set; }
        public string DeclineCode { get; set; }
        public string SecurityKey { get; set; }
        public string Account { get; set; }
        public string Email { get; set; }
        public string PostString { get; set; }
        public string ResponseString { get; set; }
        public string DocId { get; set; }   // Back Office Order ID
        public byte DocType { get; set; } = 0;
        public bool? ReDScreened { get; set; } = null;
        public string ACSTransID { get; set; }
        public string DSTransID { get; set; }
        public string SchemeTraceID { get; set; }

    }
    public class MyAccountDetails
    {
        public Dictionary<string, string> CommonData { get; set; }
        public string Record { get; set; }
        public List<SelectListItem> TitleList { get; set; }
        public Name Name { get; set; }
        [Required(ErrorMessage = "Please enter an email address")]
        [RegularExpression(@"^([a-zA-Z0-9_\-\.\']+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{2,20}|[0-9]{1,3})(\]?)$", ErrorMessage = "Please enter a valid email address")]
        public string Email { get; set; }
        [Required(ErrorMessage = "Please enter a contact number")]
        [RegularExpression(@"^(?:(?:\(?(?:0(?:0|11)\)?[\s-]?\(?|\+)44\)?[\s-]?(?:\(?0\)?[\s-]?)?)|(?:\(?0))(?:(?:\d{5}\)?[\s-]?\d{4,5})|(?:\d{4}\)?[\s-]?(?:\d{5}|\d{3}[\s-]?\d{3}))|(?:\d{3}\)?[\s-]?\d{3}[\s-]?\d{3,4})|(?:\d{2}\)?[\s-]?\d{4}[\s-]?\d{4}))(?:[\s-]?(?:x|ext\.?|\#)\d{3,4})?$", ErrorMessage = "Please enter a valid telephone number.")]
        public string TelephoneNumber { get; set; }
        [Required(ErrorMessage = "Please enter current password")]
        public string OldPassword { get; set; }
        [MaxLength(128, ErrorMessage = "Password cannot be longer than 128 characters")]
        [MinLength(6, ErrorMessage = "Password cannot be less than 6 characters")]
        public string Password { get; set; }
        public string PasswordRepeat { get; set; }
        public Address CustomerAddress { get; set; }
        public bool NewsLetter { get; set; }
    }

    public class OrderHistory
    {
        public string OrderNumber { get; set; }
        public string CustomerReference { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalNet { get; set; }
        public decimal TotalVat { get; set; }
        public decimal TotalDel { get; set; }
        public decimal TotalVoucher { get; set; }
        public decimal Total { get; set; }
        public decimal AccTotal { get; set; }
        public int Status { get; set; }
        public string StatusDesc { get; set; }
        public bool InvoiceExists { get; set; }
        public List<OrderLine> OrderLines { get; set; }
        public Address BillingAddress { get; set; }
        public Address DeliveryAddress { get; set; }
        public string TrackingLinks { get; set; }
    }

    public class OrderLine
    {
        public string Reference { get; set; }
        public string AltRef { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public decimal SubTotal { get; set; }
        public decimal PriceNet { get; set; }
        public decimal PriceVat { get; set; }
        public int Quantity { get; set; }
        public string ImageUrl { get; set; }
        public int Availability { get; set; }
        public bool IsDiscontinued { get; set; }
        public bool IsVoucher { get; set; }
        public bool IsDelivery { get; set; }
        public BrandFlag BrandFlag { get; set; }
        public string XSStockReference { get; set; }
        public decimal XSSaving { get; set; }
        public string XSImageSash { get; set; }
        public ProductEntry XSProduct { get; set; }
    }

    public class ResetPassword
    {
        public bool OKToReset { get; set; }
        public SaveReturn Sr { get; set; }
        [Required(ErrorMessage = "Password is required")]
        public string NewPassword { get; set; } = "";
        [Required(ErrorMessage = "Confirm Password is required")]
        [System.ComponentModel.DataAnnotations.Compare("NewPassword", ErrorMessage = "Confirm password doesn't match, please try again.")]
        public string ConfirmPassword { get; set; } = "";
    }

    public class PrinterFinderEntry
    {
        public string Model { get; set; }
        public string Function { get; set; }
        public string Colour { get; set; }
        public string Type { get; set; }
        public string Pagesize { get; set; }
        public string Wifi { get; set; }
        public string Mobile { get; set; }
        public string Duplex { get; set; }
        public string Network { get; set; }
        public string Traysize { get; set; }
        public string StockRef { get; set; }
    }

    public class DataSupplierAttributeLookup
    {
        public string PartNo { get; set; }
        public string ManufacturerName { get; set; }
    }

    public class ProductPdf
    {
        public string Description { get; set; }
        public string Url { get; set; }
    }

    public class RecentlyViewed
    {
        public string Type { get; set; }
        public string Reference { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        public string ImageUrl { get; set; }
    }

    public class ZoneLookup
    {
        public string Type { get; set; }
        public string Prefix { get; set; }
        public int From { get; set; }
        public int To { get; set; }
        public int ZoneId { get; set; }
    }

    public class UserDetailLookup
    {
        public string Record { get; set; }
        public string Title { get; set; }
        public string Firstname { get; set; }
        public string Surname { get; set; }
        public string Email { get; set; }
        public string Account { get; set; }
        public string Postcode { get; set; }
        public string FriendlyPostcode { get; set; }
        public int WebsiteId { get; set; }
        public DateTime? LastOrderDate { get; set; }
        public string CustomerGroup { get; set; }
        public string BillingAddress { get; set; }
        public string FullName { get; set; }
        public string OrgName { get; set; }
    }

    public class UserAccountInfo
    {
        public bool IsNewAccount { get; set; }
        public bool HasBlankPassword { get; set; }
        public string AccountNumber { get; set; }
    }

    public class Return
    {
        public string Reason { get; set; }
        public string Description { get; set; }
        public string OrderNumber { get; set; }
        public bool Unopened { get; set; }
        public List<ReturnItem> ReturnItems { get; set; }
    }

    public class ReturnItem
    {
        public string ProductFk { get; set; }
        public int Quantity { get; set; }
        public bool IsOem { get; set; }
    }

    public class WebsiteInMaintenanceException : Exception
    {
        public WebsiteInMaintenanceException()
        {
        }

        public WebsiteInMaintenanceException(string message)
        : base(message)
        {
        }

        public WebsiteInMaintenanceException(string message, Exception inner)
        : base(message, inner)
        {
        }
    }

    public static class ExtensionMethods
    {
        /// <summary>
        /// Truncate the string to the specified max length. Returns the original string when length is less than the max length.
        /// </summary>
        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

        public static string RangeReplace(this object value, bool forUrl = true)
        {
            if (forUrl)
            {
                return (value ?? string.Empty).ToString().ToLower().Replace(" ", "-").Replace("hp-range", "toner-cartridges").Replace("toner-range", "toner-cartridges").Replace("ink-range", "ink-cartridges");
            }
            else
            {
                return (value ?? string.Empty).ToString().Replace("HP Range", "Toner Cartridges").Replace("Toner Range", "Toner Cartridges").Replace("Ink Range", "Ink Cartridges").Replace("toner-range", "toner-cartridges").Replace("ink-range", "ink-cartridges");
            }
        }
    }
}
