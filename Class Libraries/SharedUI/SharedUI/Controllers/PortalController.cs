using BusinessLogic;
using BusinessLogic.ViewModels;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DataAccess.EntityFramework;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using System.Net;
using System.Text;

namespace SharedUI.Controllers
{
    public class PortalController : ApplicationController
    {
        private PortalViewModel model;

        // POST page used to redirect from pages other than the main grid page
        [HttpPost]
        [AuthorizeIpAddress]
        public ActionResult Index(string srch, string pco)
        {
            if (!string.IsNullOrEmpty(srch))
            {
                Session["P_SearchTerm"] = srch;
            }
            if (!string.IsNullOrEmpty(pco))
            {
                Session["P_Postcode"] = pco;
            }

            return RedirectToAction("Index", "Portal");
        }

        [HttpGet]
        [AuthorizeIpAddress]
        public ActionResult Index()
        {
            var model = new PortalViewModel();
            
            switch (ConfigurationManager.AppSettings["Environment"])
            {
                case "Live":
                {
                    ViewBag.UrlPrefix = "//www.";
                    break;
                }
                case "Dev":
                {
                    ViewBag.UrlPrefix = "//beta.";
                    break;
                }
                default:
                {
                    ViewBag.UrlPrefix = "";
                    break;
                }
            }

            return View(model);
        }

        [HttpGet]
        [AuthorizeIpAddress]
        public ActionResult DeleteVoucher()
        {
            var model = new PortalViewModel();
            model.Sites = new List<SelectListItem>();
            model.Sites.Add(new SelectListItem() { Value = "tonergiant", Text = "Tonergiant" });
            model.Sites.Add(new SelectListItem() { Value = "cartridgemonkey", Text = "CartridgeMonkey" });
            model.Sites.Add(new SelectListItem() { Value = "netgiant", Text = "Netgiant" });

            return View(model);
        }

        [HttpPost]
        //[ValidateAntiForgeryToken]
        [AuthorizeIpAddress]
        public JsonResult DeleteVoucher(PortalViewModel model)
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = true;
            if (!string.IsNullOrEmpty(model.VoucherCode) && !string.IsNullOrEmpty(model.DbName))
            {
                // Check voucher exists
                VoucherPromo v = EntityAccess.ReadVoucherPromo(x => x.VoucherCode == model.VoucherCode)
                    .FirstOrDefault();
                if (v != null)
                {
                    v.IsUsed = true;
                    EntityAccess.SaveVoucher(v);
                }
                else
                {
                    sr.IsSuccess = false;
                    sr.Message = "Voucher Code does not exist";
                }
            }
            else
            {
                sr.IsSuccess = false;
                sr.Message = "Invalid voucher code or website selection";
            }

            return Json(new
            {
                savereturn = sr
            });
        }

        [HttpGet]
        [AuthorizeIpAddress]
        public ActionResult AddVoucher(string acc = "", int site = 0)
        {
            var model = new PortalViewModel();
            model.Sites = new List<SelectListItem>();
            model.Sites.Add(new SelectListItem() { Value = "1", Text = "Tonergiant", Selected = site == 1 });
            model.Sites.Add(new SelectListItem() { Value = "2", Text = "CartridgeMonkey", Selected = site == 2 });
            model.Sites.Add(new SelectListItem() { Value = "3", Text = "Netgiant", Selected = site == 3 });

            model.Voucher = new VoucherPromo();
            if (acc != "")
            {
                model.Voucher.AccountNumber = "01/" + acc;
            }
            model.Voucher.WebsiteFk = site;
            model.Voucher.VoucherCode = Utilities.GetVoucherCode();
            model.Voucher.ValidFrom = DateTime.Now;
            model.Voucher.ValidTo = DateTime.Now;
            model.Voucher.Description = "Customer Voucher";
            model.Voucher.IsGlobal = true;
            model.Voucher.IsUsed = false;

            return View(model);
        }

        [HttpPost]
        //[ValidateAntiForgeryToken]
        [AuthorizeIpAddress]
        public JsonResult AddVoucher(PortalViewModel model)
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = true;
            if (!string.IsNullOrEmpty(model.Voucher.VoucherCode))
            {
                // Check voucher exists
                if (!EntityAccess.VoucherExists(model.Voucher.WebsiteFk, model.Voucher.VoucherCode))
                {
                    model.Voucher.VoucherPromoGroupFk = EntityAccess.ReadVoucherPromoGroup(x => x.GroupName == "Global" && x.WebsiteFk == model.Voucher.WebsiteFk).FirstOrDefault().VoucherPromoGroupId;
                    sr = EntityAccess.SaveVoucher(model.Voucher);

                }
                else
                {
                    sr.IsSuccess = false;
                    sr.Message = "Voucher Code already exists";
                }
            }
            else
            {
                sr.IsSuccess = false;
                sr.Message = "Invalid voucher code";
            }

            // If OK email the customer
            if (sr.IsSuccess && model.Voucher.SendEmail)
            {
                Utilities.SendPersonalVoucherEmail(model.Voucher);

                //Decimal amt = model.Voucher.Amount ?? 0;
                //string websiteUrl = EntityAccess.ReadWebsite(x => x.WebsiteID == model.Voucher.WebsiteFk)
                //    .FirstOrDefault().WebURL;
                //Dictionary<string, string> commondata = DataCache.GetSectionData("CommonData");
                //Dictionary<string, string> replacements = new Dictionary<string, string>
                //{
                //    {"[vouchernumber]", model.Voucher.VoucherCode},
                //    {"[url]", "https://" + websiteUrl + "/cvoucher/" + model.Voucher.VoucherCode},
                //    {"[voucheramount]", "&pound;" + amt.ToString("#,###,##0.00")},
                //    {"[TelephoneNumber]", Utilities.GetItemFromDict(commondata, "TelephoneNumber")},
                //    {"[SupportEmail]", Utilities.GetItemFromDict(commondata, "SupportEmail")}
                //};

                //// Must retirieve the correct CMS entry for the website for which the voucher is being created
                //cmsEntry cms = EntityAccess.ReadCms(x => x.cmsSection.sectionName == "EmailData" && x.entryName == "CustomerVoucher"
                //    , model.Voucher.WebsiteFk).FirstOrDefault();
                //if (cms != null)
                //{
                //    string body = cms.cmsContent;
                //    foreach (KeyValuePair<string, string> kvp in replacements)
                //    {
                //        body = body.Replace(kvp.Key, kvp.Value);
                //    }
                //    Utilities.SendEmail(Utilities.GetItemFromDict(commondata, "SupportEmail").ToLower(),
                //        model.Voucher.Email.ToLower(), "Customer Voucher", body);
                //}
            }

            return Json(new
            {
                savereturn = sr
            });
        }

        [HttpGet]
        [AuthorizeIpAddress]
        public ActionResult CustomerVoucherList(string id)
        {
            var model = new PortalViewModel();
            model.VoucherScope = id;

            return View(model);
        }

        public ActionResult Voucher_Read([DataSourceRequest]DataSourceRequest request, string scope)
        {
            PortalViewModel model = new PortalViewModel();
            model.GetCustomerVouchers(scope);

            var result = model.Vouchers.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        [AuthorizeIpAddress]
        public ActionResult CustomerSearch([DataSourceRequest]DataSourceRequest request, 
            string keyword,
            bool postcodeOnly = true)
        {
            var model = new PortalViewModel
            {
                SearchTerm = keyword,
                PostcodeOnly = postcodeOnly
            };
            model.CustomerSearch();

            var result = model.Results.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        [AuthorizeIpAddress]
        public ActionResult Authenticate(string userId)
        {
            Session.Clear();
            Utilities.SetDeliveryDate();
            Session["U_Authenticated"] = false;
            Basket.UpdateBasketSession(new List<BasketContents>());

            if (userId != "")
            {
                var successfulLogin = Authentication.PortalAuthenticate(userId.Replace("-", ""));
                if (!successfulLogin)
                {
                    return RedirectToAction("Index", "Error", new {id = 9001});
                }

                Session["U_FullyAuthenticated"] = true;
            }
            Session["U_IsPortalUser"] = true;
            return RedirectToAction("Index", "Home");
        }

        [AuthorizeIpAddress]
        public ActionResult Reset()
        {
            Authentication.LogOut();
            Authentication.RemoveCookie("__portalid");
            Session.Remove("U_IsPortalUser");
            Session.Remove("U_CSUser");

            return RedirectToAction("Index", "Home");
        }

        // Debug Related Actions
        [BasicAuthenticationAttribute("webadmin", "Innovation2020", BasicRealm = "NG")]
        public ActionResult Debug()
        {
            var model = new CommonViewModel();
            return View(model);
        }

        public ActionResult AddCookie()
        {
            var model = new CommonViewModel();
            return View(model);
        }

        public ActionResult CreateCookie(string name, string value)
        {
            HttpCookie aCookie = new HttpCookie(name);
            aCookie.Value = value;
            aCookie.Expires = DateTime.Now.AddDays(1);
            Response.Cookies.Add(aCookie);

            var model = new CommonViewModel();
            return RedirectToAction("Debug", model);
        }

        [BasicAuthenticationAttribute("webadmin", "Innovation2020", BasicRealm = "NG")]
        public JsonResult DeleteCache(string cacheKey = null, int exec = 0)
        {
            bool isSuccess = true;
            if (ConfigurationManager.AppSettings["Environment"] != "Live" || exec == 1)
            {
                DataCache.DeleteCache(cacheKey);
                var model = new CommonViewModel();
            }
            else
            {
                string extras = "";
                if (!string.IsNullOrEmpty(cacheKey))
                {
                    extras += "cacheKey=" + cacheKey + "&";
                }
                isSuccess = MakeLiveRequest("DeleteCache?" + extras, "delete cache");
            }

            return Json(new { issuccess = isSuccess }, JsonRequestBehavior.AllowGet);
        }

        [BasicAuthenticationAttribute("webadmin", "Innovation2020", BasicRealm = "NG")]
        public JsonResult LoadAppVariable(string v, int exec = 0)
        {
            bool isSuccess = true;
            if (ConfigurationManager.AppSettings["Environment"] != "Live" || exec == 1)
            {
                List<configurationSetting> lcs = EntityAccess.ReadConfigurationSetting(x => x.sectionName == "Website Application Variables");
                if (v == "CDN")
                {
                    string versionNo = lcs.Find(x => x.settingName == "VersionNumber").settingValue.ToString();
                    ConfigurationManager.AppSettings["Version"] = versionNo;
                    ConfigurationManager.AppSettings[v] = lcs.Find(x => x.settingName == v).settingValue.ToString().Replace("[version]", versionNo);
                }
                else
                {
                    ConfigurationManager.AppSettings[v] = lcs.Find(x => x.settingName == v).settingValue.ToString();
                }
            }
            else
            {
                isSuccess = MakeLiveRequest("LoadAppVariables?", "load app variables");
            }
            return Json(new { issuccess = isSuccess }, JsonRequestBehavior.AllowGet);
        }

        [BasicAuthenticationAttribute("webadmin", "Innovation2020", BasicRealm = "NG")]
        public JsonResult LoadAppVariables(int exec = 0)
        {
            bool isSuccess = true;
            if (ConfigurationManager.AppSettings["Environment"] != "Live" || exec == 1)
            {
                Utilities.LoadApplicationVariables();
                var model = new CommonViewModel();
            }
            else
            {
                isSuccess = MakeLiveRequest("LoadAppVariables?", "load app variables");
            }
            return Json(new { issuccess = isSuccess }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult Settings()
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = true;
            sr.Message = "OK";

            model = new PortalViewModel();

            try
            {
                if (Convert.ToBoolean(Request["loadversion"]))
                {
                    if (ConfigurationManager.AppSettings["Environment"] != "Live")
                    {
                        List<configurationSetting> lcs = EntityAccess.ReadConfigurationSetting(x => x.sectionName == "Website Application Variables");
                        string versionNo = lcs.Find(x => x.settingName == "VersionNumber").settingValue.ToString();
                        ConfigurationManager.AppSettings["Version"] = versionNo;
                        ConfigurationManager.AppSettings["CDN"] = lcs.Find(x => x.settingName == "CDN").settingValue.ToString().Replace("[version]", versionNo);
                    }
                    else
                    {
                        sr.IsSuccess = MakeLiveRequest("LoadAppVariable?v=CDN&", "load version");
                    }
                }
                if (Convert.ToBoolean(Request["loadcompupsell"]))
                {
                    if (ConfigurationManager.AppSettings["Environment"] != "Live")
                    {
                        List<configurationSetting> lcs = EntityAccess.ReadConfigurationSetting(x => x.sectionName == "Website Application Variables");
                        ConfigurationManager.AppSettings["IsCompatibleUpsellActive"] = lcs.Find(x => x.settingName == "IsCompatibleUpsellActive").settingValue.ToString();
                    }
                    else
                    {
                        sr.IsSuccess = MakeLiveRequest("LoadAppVariable?v=IsCompatibleUpsellActive&", "reload compatible upsell");
                    }
                }
                if (Convert.ToBoolean(Request["loadappvars"]))
                {
                    if (ConfigurationManager.AppSettings["Environment"] != "Live")
                    {
                        Utilities.LoadApplicationVariables();
                        var model = new CommonViewModel();
                    }
                    else
                    {
                        sr.IsSuccess = MakeLiveRequest("LoadAppVariables?", "load app variables");
                    }
                }
                if (Convert.ToBoolean(Request["clearsession"]))
                {
                    Session.Clear();

                    Authentication.LoadCookie();
                    Basket.LoadCookie();
                    Utilities.SetDeliveryDate();
                }

                // Dev only
                if (ConfigurationManager.AppSettings["Environment"] != "Live")
                {
                    model.CommonData["IsOEMSaleActive"] = Convert.ToBoolean(Request["saleoem"]).ToString();
                    model.CommonData["IsCompatibleSaleActive"] = Convert.ToBoolean(Request["salecompat"]).ToString();
                    DataCache.PutCache("CommonData", model.CommonData);

                    if (Convert.ToBoolean(Request["ppcuser"]))
                    {
                        Session["U_IsFromPPC"] = true;
                    }
                    else
                    {
                        Session.Remove("U_IsFromPPC");
                    }
                    if (Convert.ToBoolean(Request["upsellcompat"]))
                    {
                        ConfigurationManager.AppSettings["IsCompatibleUpsellActive"] = "False";
                    }
                }
            }
            catch (Exception)
            {
                sr.IsSuccess = false;
                sr.Message = "Error";
            }

            return Json(new { saveReturn = sr }, JsonRequestBehavior.AllowGet);
        }

        //public JsonResult WriteBasketCookie(string basketCookie)
        //{
        //    DateTime expiry = System.DateTime.Now.Add(new System.TimeSpan(365, 0, 0, 0));

        //    HttpCookie basket = new HttpCookie("basket");
        //    basket.Value = basketCookie;
        //    basket.Expires = expiry;
        //    Response.Cookies.Add(basket);

        //    return Json(new { issuccess = true }, JsonRequestBehavior.AllowGet);
        //}

        private bool MakeLiveRequest(string func, string message)
        {
            bool isSuccess = true;
            string ip1 = "109.108.157.113";
            string ip2 = "109.108.157.114";
            if (ConfigurationManager.AppSettings["Environment"] == "Live")
            {
                ip1 = "172.21.224.140";
                ip2 = "172.21.224.141";
            }

            try
            {
                HttpWebRequest myHttpWebRequest =
                    (HttpWebRequest)WebRequest.Create("http://" + ip1 + "/portal/" + func + "exec=1");
                myHttpWebRequest.Host = ConfigurationManager.AppSettings["DomainName_Live"].Replace("/", "");
                myHttpWebRequest.Headers.Add("X-FORWARDED-PROTO", "https");
                myHttpWebRequest.Headers.Add("Authorization",
                    "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes("webadmin:Innovation2020")));
                HttpWebResponse response = (HttpWebResponse)myHttpWebRequest.GetResponse();
                if ((HttpStatusCode)response.StatusCode != HttpStatusCode.OK)
                {
                    isSuccess = false;
                    Utilities.LogInformationMessage("Unable to load " + message + " on " + ip1 + ": " +
                                                    response.StatusCode.ToString() + " " +
                                                    response.StatusDescription);
                }

                myHttpWebRequest = (HttpWebRequest)WebRequest.Create("http://" + ip2 + "/portal/" + func + "exec=1");
                myHttpWebRequest.Host = ConfigurationManager.AppSettings["DomainName_Live"].Replace("/", "");
                myHttpWebRequest.Headers.Add("X-FORWARDED-PROTO", "https");
                myHttpWebRequest.Headers.Add("Authorization",
                    "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes("webadmin:Innovation2020")));
                response = (HttpWebResponse)myHttpWebRequest.GetResponse();
                if ((HttpStatusCode)response.StatusCode != HttpStatusCode.OK)
                {
                    isSuccess = false;
                    Utilities.LogInformationMessage("Unable to " + message + " on " + ip2 + ": " +
                                                    response.StatusCode.ToString() + " " +
                                                    response.StatusDescription);
                }
            }
            catch (Exception e)
            {
                isSuccess = false;
                Utilities.LogInformationMessage("Unable to " + message + " " + e.Message);
            }

            return isSuccess;
        }

        [AuthorizeIpAddress]
        public JsonResult SetCustomerServiceUser(int orderSource)
        {
            var sr = new SaveReturn();
            sr.IsSuccess = false;

            if (orderSource > 0)
            {
                Authentication.WriteCookie("__csuser", Convert.ToString(orderSource), new TimeSpan(365, 0, 0, 0));

                sr.IsSuccess = true;
            }

            return Json(new
            {
                savereturn = sr
            });
        }
    }
}