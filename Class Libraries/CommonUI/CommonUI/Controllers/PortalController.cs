using BusinessLogic;
using BusinessLogic.ViewModels;
using DuoUniversal;
using RestSharp;
using RestSharp.Authenticators;
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
using System.Threading.Tasks;

namespace CommonUI.Controllers
{
    // TIDYUP
    public class PortalController : ApplicationController
    {
        private PortalViewModel model;

        // POST page used to redirect from pages other than the main grid page
        [HttpPost]
        [DuoAuthentication]
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
        [DuoAuthentication]
        public ActionResult Index()
        {
            var model = new PortalViewModel();
            
            switch (ConfigurationManager.AppSettings["Environment"])
            {
                case "Live":
                {
                    ViewBag.UrlPrefix = "https://www.";
                    break;
                }
                case "Dev":
                {
                    ViewBag.UrlPrefix = "https://beta.";
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
        public ActionResult Login(string nexturl)
        {
            if (string.IsNullOrEmpty(nexturl))
            {
                return RedirectToAction("Index", "Error", new { id = 9001 });
            }

            var model = new PortalViewModel();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(PortalViewModel model)
        {
            if (!model.DuoIsEnabled())
            {
                model.SetVarsForPortal();
                return Redirect(model.NextUrl);
            }

            Session["Model"] = model;
            string returnUrl = "";
            switch (ConfigurationManager.AppSettings["Environment"])
            {
                case "Live":
                case "Dev":
                    {
                        returnUrl = "https://" + Request.ServerVariables["SERVER_NAME"] + "/Portal/LoginComplete";
                        break;
                    }
                case "Local":
                    {
                        returnUrl = "http://localhost:" + Request.ServerVariables["SERVER_PORT"] + "/Portal/LoginComplete";
                        break;
                    }
            }

            Client duoClient = new ClientBuilder("DIDKECJ9BO3N01GDM661", "8TL4ifBbHBUFPh7PJJXJ1ffGtcbs7UgrUoGcqshx", "api-544ee06f.duosecurity.com", returnUrl).Build();
            var isDuoHealthy = await duoClient.DoHealthCheck();
            string state = DuoUniversal.Client.GenerateState();
            string promptUri = duoClient.GenerateAuthUri(model.UserEmail, state);
            return Redirect(promptUri);
        }

        public ActionResult LoginComplete()
        {
            PortalViewModel model = (PortalViewModel)Session["Model"];
            if (model == null)
            {
                // If we got here, something went wrong, redisplay login form
                return RedirectToAction("Login");
            }
            model.SetVarsForPortal();
            string nextUrl = model.NextUrl;
            Session.Remove("Model");
            return Redirect(nextUrl);
        }

        [HttpGet]
        [DuoAuthentication]
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
        [DuoAuthentication]
        public JsonResult DeleteVoucher(PortalViewModel model)
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = true;
            if (!string.IsNullOrEmpty(model.VoucherCode) && !string.IsNullOrEmpty(model.DbName))
            {
                // Check voucher exists
                VoucherPromo v = EntityAccess.ReadVoucherPromo(x => x.VoucherCode == model.VoucherCode)
                    .ToList().FirstOrDefault();
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
        [DuoAuthentication]
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
                model.Voucher.AccountNumber = acc;
            }
            model.Voucher.WebsiteFk = site;
            model.Voucher.VoucherCode = Utilities.GetVoucherCode();
            model.Voucher.ValidFrom = DateTime.Now;
            model.Voucher.ValidTo = DateTime.Now;
            model.Voucher.Description = "Customer Voucher";
            model.Voucher.IsGlobal = true;
            model.Voucher.IsUsed = false;
            model.Voucher.ForGeneralUse = false;

            return View(model);
        }

        [HttpPost]
        //[ValidateAntiForgeryToken]
        [DuoAuthentication]
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
            }

            return Json(new
            {
                savereturn = sr
            });
        }

        [HttpGet]
        [DuoAuthentication]
        public ActionResult CustomerVoucherList(string id)
        {
            var model = new PortalViewModel();
            model.VoucherScope = id;

            return View(model);
        }

        [DuoAuthentication]
        public ActionResult Voucher_Read([DataSourceRequest]DataSourceRequest request, string scope)
        {
            PortalViewModel model = new PortalViewModel();
            model.GetCustomerVouchers(scope);

            var result = model.Vouchers.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        [HttpGet]
        [DuoAuthentication]
        public ActionResult BackOrderList()
        {
            var model = new PortalViewModel();

            return View(model);
        }

        [DuoAuthentication]
        public ActionResult BackOrder_Read([DataSourceRequest]DataSourceRequest request)
        {
            PortalViewModel model = new PortalViewModel();
            model.GetBackOrders();

            var result = model.BackOrders.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        [HttpGet]
        [DuoAuthentication]
        public ActionResult OrderTrackingList(string acc = "")
        {
            var model = new PortalViewModel();

            model.OrderTrackingAcc = acc;

            return View(model);
        }

        [DuoAuthentication]
        public ActionResult OrderTracking_Read([DataSourceRequest] DataSourceRequest request, string acc)
        {
            PortalViewModel model = new PortalViewModel();
            model.GetOrderTracking(acc);

            var result = model.OrderTracking.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        [DuoAuthentication]
        public ActionResult OrderTrackingSendEmail(int id)
        {
            PortalViewModel model = new PortalViewModel();

            SaveReturn sr = model.SendTrackingLink(id);

            return Json(new
            {
                savereturn = sr
            });
        }

        [DuoAuthentication]
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

        [DuoAuthentication]
        public ActionResult Authenticate(string userId, string act, string cont)
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
            string csUserId = Request.Cookies["__csuser"] == null ? "" : Request.Cookies["__csuser"].Value;
            Session["U_CSUser"] = csUserId;
            Session["U_IsPortalUser"] = true;
            return RedirectToAction(act, cont);
        }

        [DuoAuthentication]
        public ActionResult Reset()
        {
            Authentication.LogOut();
            Authentication.RemoveCookie("__portalid");
            Session.Remove("U_IsPortalUser");
            Session.Remove("U_CSUser");

            return RedirectToAction("Index", "Home");
        }

        // Debug Related Actions
        //[BasicAuthenticationAttribute("webadmin", "Innovation2020", BasicRealm = "NG")]
        [DuoAuthentication]
        public ActionResult Debug()
        {
            var model = new CommonViewModel
            {
                SignUp = new SignUp(),
                SignIn = new SignIn()
            };
            ViewBag.ClientIP = Utilities.GetClientIPAddress(Request);
            return View(model);
        }

        [DuoAuthentication]
        public ActionResult AddCookie()
        {
            var model = new CommonViewModel();
            return View(model);
        }

        [DuoAuthentication]
        public ActionResult CreateCookie(string name, string value)
        {
            HttpCookie aCookie = new HttpCookie(name);
            aCookie.Value = value;
            aCookie.Expires = DateTime.Now.AddDays(1);
            aCookie.SameSite = SameSiteMode.Lax;
            Response.Cookies.Add(aCookie);

            var model = new CommonViewModel
            {
                SignUp = new SignUp(),
                SignIn = new SignIn()
            };
            return RedirectToAction("Debug", model);
        }

        [BasicAuthenticationAttribute("webadmin", "Innovation2020", BasicRealm = "NG")]
        public JsonResult DeleteCache(string cacheKey = null, int exec = 0)
        {
            bool isSuccess = true;
            DataCache.DeleteCache(cacheKey);
            var model = new CommonViewModel
            {
                SignUp = new SignUp(),
                SignIn = new SignIn()
            };
            if (ConfigurationManager.AppSettings["Environment"] == "Live" && exec == 0)
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
        public JsonResult SessionAbandon()
        {
            Session.Abandon();

            return Json(new { issuccess = true }, JsonRequestBehavior.AllowGet);
        }

        //[DuoAuthentication]
        [BasicAuthenticationAttribute("webadmin", "Innovation2020", BasicRealm = "NG")]
        public JsonResult LoadAppVariable(string v, int exec = 0)
        {
            bool isSuccess = true;
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
            if (ConfigurationManager.AppSettings["Environment"] == "Live" && exec == 0)
            {
                isSuccess = MakeLiveRequest("LoadAppVariables?", "load app variables");
            }
            return Json(new { issuccess = isSuccess }, JsonRequestBehavior.AllowGet);
        }

        //[DuoAuthentication]
        [BasicAuthenticationAttribute("webadmin", "Innovation2020", BasicRealm = "NG")]
        public JsonResult LoadAppVariables(int exec = 0)
        {
            bool isSuccess = true;
            Utilities.LoadApplicationVariables();
            if (ConfigurationManager.AppSettings["Environment"] == "Live" && exec == 0)
            {
                isSuccess = MakeLiveRequest("LoadAppVariables?", "load app variables");
            }
            return Json(new { issuccess = isSuccess }, JsonRequestBehavior.AllowGet);
        }

        [DuoAuthentication]
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
                    List<configurationSetting> lcs = EntityAccess.ReadConfigurationSetting(x => x.sectionName == "Website Application Variables");
                    string versionNo = lcs.Find(x => x.settingName == "VersionNumber").settingValue.ToString();
                    ConfigurationManager.AppSettings["Version"] = versionNo;
                    ConfigurationManager.AppSettings["CDN"] = lcs.Find(x => x.settingName == "CDN").settingValue.ToString().Replace("[version]", versionNo);
                    if (ConfigurationManager.AppSettings["Environment"] == "Live")
                    {
                        sr.IsSuccess = MakeLiveRequest("LoadAppVariable?v=CDN&", "load version");
                    }
                }
                if (Convert.ToBoolean(Request["loadappvars"]))
                {
                    Utilities.LoadApplicationVariables();
                    var model = new CommonViewModel
                    {
                        SignUp = new SignUp(),
                        SignIn = new SignIn()
                    };
                    if (ConfigurationManager.AppSettings["Environment"] == "Live")
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

        [DuoAuthentication]
        public JsonResult RemoveCookie(string cookieName)
        {
            DateTime expiry = System.DateTime.Now.Add(new TimeSpan(-1, 0, 0, 0));

            HttpCookie cookie = new HttpCookie(cookieName);
            cookie.Expires = expiry;
            Response.Cookies.Add(cookie);

            return Json(new { isSuccess = true }, JsonRequestBehavior.AllowGet);
        }

        private bool MakeLiveRequest(string func, string message)
        {
            bool isSuccess = true;

            string ip = "10.0.0.5";
            if (Request.Params["LOCAL_ADDR"] == "10.0.0.5")
            {
                ip = "10.0.0.10";
            }

            try
            {
                var client = new RestClient("http://" + ip);
                var request = new RestRequest("/portal/" + func + "exec=1", RestSharp.Method.Get)
                {
                    Authenticator = new HttpBasicAuthenticator("webadmin", "Innovation2020")
                }
                    .AddParameter("grant_type", "client_credentials")
                    .AddHeader("Host", ConfigurationManager.AppSettings["DomainName_Live"].Replace("/", ""))
                    .AddHeader("X-FORWARDED-PROTO", "https");
                var response = client.Execute(request, RestSharp.Method.Get);
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    Utilities.LogInformationMessage("Unable to make portal request for server: " + ip + ": "+ func);
                    isSuccess = false;
                }
            }
            catch (Exception e)
            {
                Utilities.LogInformationMessage("Unable to make portal request for server: " + ip + ": " + func);
                isSuccess = false;
            }

            return isSuccess;
        }

        private bool MakeLiveRequest1(string func, string message)
        {
            bool isSuccess = true;

            List<string> ip = new List<string> { "45.145.102.190", "45.145.103.178" };
            if (ConfigurationManager.AppSettings["Environment"] == "Live")
            {
                ip = new List<string> { "10.0.0.5", "10.0.0.10" };
            }

            try
            {
                foreach (string i_p in ip)
                {
                    HttpWebRequest myHttpWebRequest =
                    (HttpWebRequest)WebRequest.Create("http://" + i_p + "/portal/" + func + "exec=1");
                    //myHttpWebRequest.AllowReadStreamBuffering = true;
                    //myHttpWebRequest.AllowWriteStreamBuffering = true;
                    myHttpWebRequest.Host = ConfigurationManager.AppSettings["DomainName_Live"].Replace("/", "");
                    myHttpWebRequest.Headers.Add("X-FORWARDED-PROTO", "https");
                    myHttpWebRequest.Headers.Add("Authorization",
                        "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes("webadmin:Innovation2020")));
                    HttpWebResponse response = (HttpWebResponse)myHttpWebRequest.GetResponse();
                    if ((HttpStatusCode)response.StatusCode != HttpStatusCode.OK)
                    {
                        isSuccess = false;
                        Utilities.LogInformationMessage("Unable to load " + message + " on " + i_p + ": " +
                                                        response.StatusCode.ToString() + " " +
                                                        response.StatusDescription);
                    }
                    else
                    {
                        //Utilities.LogInformationMessage("Loaded " + message + " on " + i_p + ": " +
                        //                                response.StatusCode.ToString() + " " +
                        //                                response.StatusDescription);
                    }
                }
            }
            catch (Exception e)
            {
                isSuccess = false;
                Utilities.LogInformationMessage("Unable to " + message + " " + e.Message);
            }

            return isSuccess;
        }

        //[DuoAuthentication]
        //public JsonResult SetCustomerServiceUser(int orderSource)
        //{
        //    var sr = new SaveReturn();
        //    sr.IsSuccess = false;

        //    if (orderSource > 0)
        //    {
        //        Authentication.WriteCookie("__csuser", Convert.ToString(orderSource), new TimeSpan(365, 0, 0, 0));

        //        sr.IsSuccess = true;
        //    }

        //    return Json(new
        //    {
        //        savereturn = sr
        //    });
        //}
    }
}