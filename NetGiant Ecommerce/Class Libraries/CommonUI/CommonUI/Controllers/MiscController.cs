using BusinessLogic;
using BusinessLogic.ViewModels;
using DataAccess.EntityFramework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;

namespace CommonUI.Controllers
{
    [SiteOfflineCheck]
    public class MiscController : ApplicationController
    {
        private MiscViewModel model;

        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public JsonResult Popup(string popupname, string popupid, string popupwidth = "md", string replacements = "")
        {
            model = new MiscViewModel();
            SaveReturn sr = model.GetPopup(popupname, popupid, popupwidth, replacements);

            return Json(new
            {
                savereturn = sr
            });
        }

        [HttpPost]
        public JsonResult PopupContent(string popupname, string replacements = "")
        {
            SaveReturn sr = new SaveReturn();
            Dictionary<string, string> dict = DataCache.GetSectionData("PopupData");

            sr.IsSuccess = false;
            if (dict.ContainsKey(popupname))
            {
                sr.Html = dict[popupname].ToString();
                sr.IsSuccess = true;
            }

            replacements = Utilities.AddStandardReplacements(replacements);
            if (replacements != "")
            {
                string[] a = replacements.Split('&');
                foreach (string b in a)
                {
                    string[] c = b.Split('=');
                    sr.Html = sr.Html.Replace("[" + c[0] + "]", c[1]);
                }
            }

            return Json(new
            {
                savereturn = sr
            });
        }

        [HttpPost]
        public JsonResult AskQuestion()
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = true;

            qa_Main qam = new qa_Main();
            qam.Question = Request.Form["question"];
            qam.Answer = "";
            qam.Email = Request.Form["email"];
            qam.AskedDate = qam.dateLastUpdate = DateTime.Now;
            qam.GranularityFK = !String.IsNullOrEmpty(Request.Form["granularity"])
                ? int.Parse(Request.Form["granularity"])
                : 2;
            qam.SourceWebsiteID = int.Parse(ConfigurationManager.AppSettings["WebsiteId"]);
            qam.ProductID = !String.IsNullOrEmpty(Request.Form["prodid"])
                ? int.Parse(Request.Form["prodid"])
                : (int?)null;
            qam.AltRef = !String.IsNullOrEmpty(Request.Form["altref"]) ? Request.Form["altref"] : null;
            qam.eqEquipmentFK = !String.IsNullOrEmpty(Request.Form["equipid"])
                ? int.Parse(Request.Form["equipid"])
                : (int?)null;


            if (string.IsNullOrEmpty(qam.Email) || !Regex.IsMatch(qam.Email, @"^([a-zA-Z0-9_\-\.\']+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{2,20}|[0-9]{1,3})(\]?)$"))
            {
                sr.IsSuccess = false;
                sr.Message = "Email";
                sr.Html = "Please enter a valid email address";
            }
            else if (string.IsNullOrEmpty(qam.Question))
            {
                sr.IsSuccess = false;
                sr.Message = "Question";
                sr.Html = "Please enter a question";
            }
            else
            {
                EntityAccess.InsertQandA(qam);
                Dictionary<string, string> commondata = DataCache.GetSectionData("CommonData");
                Dictionary<string, string> replacements = new Dictionary<string, string>();

                var product = EntityAccess.ReadProduct(x => x.productID == qam.ProductID).FirstOrDefault();
                var equip = EntityAccess.ReadEquipment(x => x.eqEquipmentID == qam.eqEquipmentFK).FirstOrDefault();

                replacements.Add("[email]", qam.Email);
                replacements.Add("[sitename]", Utilities.GetItemFromDict(commondata, "SiteName"));
                replacements.Add("[productdescription]", product != null ? product.productName : equip != null ? equip.description : "");
                replacements.Add("[question]", qam.Question);
                Utilities.SendEmail(Utilities.GetItemFromDict(commondata, "HelpEmail").ToLower(), Utilities.GetItemFromDict(commondata, "QandAEmail").ToLower(), "Ask A Question", "NewQuestionInternal", replacements);
                Utilities.SendEmail(Utilities.GetItemFromDict(commondata, "HelpEmail").ToLower(), qam.Email, "Thank you for your question", "NewQuestionCustomer", replacements);
            }

            return Json(new
            {
                savereturn = sr
            });
        }

        public ActionResult PrinterFinder()
        {
            model = new MiscViewModel();
            model.GetMeta();
            model.ProcessPrinterFinderFile();

            model.BreadcrumbTrail.Add("Best 100 Printers", "Printer-Finder/");
            ViewBag.BreadcrumbJson = model.BuildBreadcrumbJson();

            return View(model);
        }

        public ActionResult ApplyVoucher(string voucherCode)
        {
            SaveReturn sr = Utilities.LoadVoucher(voucherCode);
            if (sr.IsSuccess && sr.Message == "")
            {
                sr.Html = "<div class=\"\"><i class=\"fa fa-exclamation-triangle fa-lg\"></i><span class=\"g-p-l-10\">Voucher " +
                    voucherCode.ToUpper() + " has been recognised.</span></div>";
            }
            else
            {
                sr.Html = "<div class=\"g-fc-nm\">" + sr.Html + "</div>";
            }

            TempData["VoucherSR"] = sr;
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public ActionResult AccountApplication()
        {
            model = new MiscViewModel();
            model.AccountApplicationDetails = new AccountApplicationDetails
            {
                Section = AccountApplicationSection.All
            };
            ViewBag.CaptchaError = TempData["InvalidCaptcha"] == null ? "" : TempData["InvalidCaptcha"].ToString();

            return View(model);
        }

        [HttpGet]
        public ActionResult TradeApplication()
        {
            model = new MiscViewModel();
            model.TradeApplicationDetails = new TradeApplicationDetails
            {
                Section = AccountApplicationSection.All
            };
            ViewBag.CaptchaError = TempData["InvalidCaptcha"] == null ? "" : TempData["InvalidCaptcha"].ToString();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateGoogleCaptcha]
        public JsonResult AccountApplication(MiscViewModel model)
        {
            CheckoutDetails cd = new CheckoutDetails();
            Dictionary<string, string> checkoutdata = DataCache.GetSectionData("CheckoutData");
            //Dictionary<string, string> emaildata = DataCache.GetSectionData("EmailData");

            SaveReturn sr = MyAccountViewModel.ProcessCreditAccountApplication(cd, model.AccountApplicationDetails, checkoutdata);
            if (sr.IsSuccess)
            {
                Dictionary<string, string> replacements = new Dictionary<string, string>();
                replacements.Add("[contactname]", model.AccountApplicationDetails.ContactName);
                Utilities.SendEmail(Utilities.GetItemFromDict(checkoutdata, "SalesEmail"), model.AccountApplicationDetails.ContactEmail, "Thank you for your credit application", "AccountApplication", replacements);
            }

            return Json(new
            {
                savereturn = sr
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateGoogleCaptcha]
        public JsonResult TradeApplication(MiscViewModel model)
        {
            CheckoutDetails cd = new CheckoutDetails();
            Dictionary<string, string> checkoutdata = DataCache.GetSectionData("CheckoutData");
            //Dictionary<string, string> emaildata = DataCache.GetSectionData("EmailData");

            SaveReturn sr = MyAccountViewModel.ProcessTradeAccountApplication(cd, model.TradeApplicationDetails, checkoutdata);
            //if (sr.IsSuccess)
            //{
            //    Dictionary<string, string> replacements = new Dictionary<string, string>();
            //    replacements.Add("[contactname]", model.AccountApplicationDetails.ContactName);
            //    Utilities.SendEmail(Utilities.GetItemFromDict(checkoutdata, "SalesEmail"), model.AccountApplicationDetails.ContactEmail, "Thank you for your trade application", "TradeApplication", replacements);
            //}

            return Json(new
            {
                savereturn = sr
            });
        }

        [HttpGet]
        public ActionResult FrameLauncher(string url, string p = "0", string bc = "")
        {
            model = new MiscViewModel();
            ViewBag.Url = url;
            ViewBag.P = p;
            ViewBag.Bc = bc;

            return View(model);
        }

        [HttpGet]
        public ActionResult ShowCms(string section, string entry, string rpl = "")
        {
            if (string.IsNullOrEmpty(section) || string.IsNullOrEmpty(entry))
            {
                return new EmptyResult();
            }
            model = new MiscViewModel();

            Dictionary<string, string> replacements = new Dictionary<string, string>();
            if (!String.IsNullOrEmpty(rpl))
            {
                string[] arpl = rpl.Split('$');
                foreach (string r in arpl)
                {
                    string[] s = r.Split('_');
                    replacements.Add(s[0], s[1]);
                }
            }
            replacements = Utilities.AddStandardReplacements(replacements);

            ViewData.Add("Dict", DataCache.GetSectionData(section));
            ViewData.Add("CmsEntry", entry);
            ViewData.Add("Replacements", replacements);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult NewsletterSignUp(string email)
        {
            if (!string.IsNullOrEmpty(email) && Utilities.IsValidEmail(email))
            {
                if (Touchpoints.InsertMailingList(email, true))
                {
                    Dictionary<string, string> commondata = DataCache.GetSectionData("CommonData");
                    Dictionary<string, string> emailData = DataCache.GetSectionData("EmailData");
                    Utilities.SendEmail(Utilities.GetItemFromDict(commondata, "SupportEmail").ToLower(), email,
                        "Thankyou for joining our mailing list",
                        Utilities.GetItemFromDict(emailData, "NewMailingList"), "");
                }
            }

            return Json(new { issuccess = true, message = "" }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult AuthenticationCheck()
        {
            return Json(new { isSuccess = Authentication.IsAuthenticated(), message = "" }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult FullAuthenticationCheck()
        {
            return Json(new { isSuccess = Authentication.IsFullyAuthenticated(), message = "" }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Redirects old pw-xxxx pages to the new urls
        /// </summary>
        /// <param name="type"></param>
        /// <param name="typename"></param>
        /// <param name="manuname"></param>
        /// <param name="familyname"></param>
        /// <param name="equipname"></param>
        /// <returns></returns>
        public ActionResult OldPrinterRedirect(string typename, string manuname, string familyname, string equipname)
        {
            // Fix typename
            if (!String.IsNullOrEmpty(typename))
            {
                typename = typename.ToLower();
                if (typename == "solid-ink")
                {
                    typename = "solid-ink-cartridges";
                }
                if (typename == "franking")
                {
                    typename = "franking-cartridges";
                }
                if (!"toner-cartridges|ink-cartridges|franking-cartridges|solid-ink-cartridges|".Contains(typename))
                {
                    typename = "toner-cartridges";
                }
            }
            // Fix manuname
            if (!String.IsNullOrEmpty(manuname))
            {
                if (manuname == "Hewlett-Packard")
                {
                    manuname = "HP";
                }
                if (manuname == "Konica-Minolta")
                {
                    manuname = "Konica";
                }
            }

            if (!String.IsNullOrEmpty(equipname))
            {
                // Determine if model exists or is obsolete
                string pattern = equipname.Replace("-", "_");
                eqEquipment e = EntityAccess.ReadEquipment(pattern);
                if (e != null)
                {
                    return RedirectToActionPermanent("ProductList", "Product", new { equipname = equipname });
                }

                obsoleteItem oi = EntityAccess.ReadObsoleteItem(pattern);
                if (oi != null)
                {
                    return RedirectPermanent("/" + oi.URL);
                }
            }

            if (!String.IsNullOrEmpty(manuname))
            {
                manufacturer m = EntityAccess.ReadManufacturer(x => x.manufacturerName == manuname).FirstOrDefault();
                if (m == null)
                {
                    manuname = "";
                }
            }

            if (!String.IsNullOrEmpty(typename) && !String.IsNullOrEmpty(manuname))
            {
                return RedirectToActionPermanent("PrinterWizard", "Equipment", new { typename = typename, manuname = manuname });
            }
            if (!String.IsNullOrEmpty(typename))
            {
                return RedirectToActionPermanent("PrinterWizard", "Equipment", new { typename = typename });
            }

            return RedirectToActionPermanent("PrinterWizard", "Equipment", new { typename = "toner-cartridges" });
        }

        //public JsonResult SuppressCustomerAlert()
        //{
        //    Session["SuppressCustomerAlert"] = true;
        //    SaveReturn sr = new SaveReturn {IsSuccess = true};
        //    return Json(new
        //    {
        //        savereturn = sr
        //    });
        //}

        public ActionResult SiteUp1()
        {
            return TestDbConnection();
        }

        public ActionResult SiteUp2()
        {
            return TestDbConnection();
        }

        private ActionResult TestDbConnection()
        {
            MiscViewModel.TestDbConnection();
            return Content("Success");
        }

        [HttpPost]
        public JsonResult SetSession(string name, string value = null)
        {
            if (String.IsNullOrEmpty(value) || value == "null")
            {
                Session.Remove(name);
            }
            else
            {
                Session[name] = value;
            }

            SaveReturn sr = new SaveReturn { IsSuccess = true };
            return Json(new
            {
                savereturn = sr
            });
        }

        [HttpPost]
        public JsonResult SessionExists(string name)
        {
            bool exists = Session[name] != null ? true : false;

            return Json(new
            {
                exists = exists
            });
        }

        [HttpPost]
        public JsonResult HighlightTooltip(string name)
        {
            SaveReturn sr = new SaveReturn();
            Dictionary<string, string> dict = DataCache.GetSectionData("TooltipData");

            sr.IsSuccess = false;
            if (!String.IsNullOrEmpty(name) && dict.ContainsKey(name))
            {
                sr.Html = dict[name].ToString();
                sr.IsSuccess = true;
            }

            return Json(new
            {
                savereturn = sr
            });
        }

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public JsonResult ServerAction(int action)
        {
            // 1 - Cookie Consent Accepted
            // 2 - Suppress Customer Alert

            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = false;

            switch (action)
            {
                case 1:
                    {
                        Session["U_CookieConsentAccepted"] = true;

                        DateTime expiry = System.DateTime.Now.Add(new System.TimeSpan(395, 0, 0, 0));
                        HttpCookie cc = new HttpCookie("__cc")
                        {
                            Value = "true",
                            Expires = expiry,
                            SameSite = SameSiteMode.Lax
                        };
                        Response.Cookies.Add(cc);
                        sr.IsSuccess = true;
                        break;
                    }
                case 2:
                    {
                        Session["SuppressCustomerAlert"] = true;
                        sr.IsSuccess = true;
                        break;
                    }
                default:
                    {
                        break;
                    }
            }

            return Json(new
            {
                savereturn = sr
            });
        }

        public ActionResult NgRedirect(string url = "")
        {
            Website w = EntityAccess.ReadWebsite(x => x.WebsiteID == 1).FirstOrDefault();
            string root = "https://" + w.WebURL + "/";
            string redirectUrl = root + "stationery/";
            if (url == "")
            {
                redirectUrl = root;
            }
            if (url.ToLower().StartsWith("product/"))
            {
                redirectUrl = root + url;
                // Get StockRef
                string[] nodes1 = url.Split('/');
                string[] nodes2 = nodes1[1].Split('-');
                string stockref = nodes2[nodes2.Length - 1];

                // check item exists
                product p = EntityAccess.ReadProduct(x => x.AxisFields.stockReference == stockref && (x.productStatusFK == 1 || x.productStatusFK == 8)).FirstOrDefault();
                if (p != null)
                {
                    websiteInventory wi = EntityAccess
                        .ReadWebsiteInventory(x => x.productFK == p.productID, w.WebsiteID)
                        .FirstOrDefault();
                    if (wi == null)
                    {
                        redirectUrl = root + "stationery/";
                        obsoleteItem oi = EntityAccess.ReadObsoleteItem(x => x.stockReference == stockref, w.WebsiteID).FirstOrDefault();
                        if (oi != null)
                        {
                            redirectUrl = root + oi.URL;
                        }
                    }
                }
            }
            if (url.ToLower().StartsWith("model/"))
            {
                redirectUrl = root + url;
                // Get model
                string[] nodes1 = url.Split('/');
                string equip = nodes1[1]
                    .Replace("-toner-cartridges", "")
                    .Replace("-solid-ink-cartridges", "")
                    .Replace("-ink-cartridges", "")
                    .Replace("-franking-cartridges", "");

                // check item exists
                string pattern = equip.Replace("-", "_");
                eqEquipment e = EntityAccess.ReadEquipment(pattern);
                if (e == null)
                {
                    redirectUrl = root + "stationery/";
                    obsoleteItem oi = EntityAccess.ReadObsoleteItem(pattern, w.WebsiteID);
                    if (oi != null)
                    {
                        redirectUrl = root + oi.URL;
                    }
                }
            }
            if (url.ToLower().StartsWith("toner-cartridges/")
                || url.ToLower().StartsWith("ink-cartridges/")
                || url.ToLower().StartsWith("solid-ink-cartridges/")
                || url.ToLower().StartsWith("franking-cartridges/"))
            {
                redirectUrl = root + url;
            }

            return RedirectPermanent(redirectUrl);
        }

        //[BasicAuthentication("facebook", "3uWvGDRBfSHN776gQbrj", BasicRealm = "NG")]
        //public ActionResult GetFacebookCatalogue()
        //{
        //    string filename = ConfigurationManager.AppSettings["WebsiteShortCode"].ToUpper() + "Facebook-Products.csv";
        //    string filepath = Server.MapPath("~/data/static-files/facebook/" + filename);

        //    if (!System.IO.File.Exists(filepath))
        //    {
        //        return HttpNotFound();
        //    }

        //    byte[] fileBytes = System.IO.File.ReadAllBytes(filepath);

        //    return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, filename);
        //}

        [AuthorizeIpAddress]
        public ActionResult OrderConfirmationEmail()
        {
            CheckoutViewModel m = new CheckoutViewModel();
            if (Session["C_CheckoutDetails"] == null)
            {
                return RedirectToAction("ViewBasket", "Checkout");
            }
            m.CheckoutDetails = Utilities.LoadSession<CheckoutDetails>("C_CheckoutDetails");
            m.GetAddressDetails();

            m.BasketItemsEmail = RenderPartialViewToString("~/Views/Misc/OrderConfirmationBasketItems.cshtml", m.CheckoutDetails);
            return View(m);
        }

        public ActionResult Test()
        {
            CheckoutViewModel m = new CheckoutViewModel();
            m.GetMeta();

            //throw new Exception("Error");
            Utilities.LogInformationMessage("Testing");
            return View(m);
        }
    }
}