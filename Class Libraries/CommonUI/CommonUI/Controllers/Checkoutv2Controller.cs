using BusinessLogic;
using BusinessLogic.ViewModels;
using DataAccess.EntityFramework;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using VmVoucherType = VMerchantWrapper.Entities.VoucherType;
using System.Threading.Tasks;

namespace CommonUI.Controllers
{
    [SessionExpiredFilter]
    [SiteOfflineCheck]
    public class Checkoutv2Controller : ApplicationController
    {
        private CheckoutViewModel model;

        public ActionResult ViewBasket()
        {
            model = new CheckoutViewModel();
            Basket.RemoveDelivery();
            Basket.GetBallparkDelivery();
            Session["D_IsVatExempt"] = false;
            ViewBag.VoucherMessage = "";
            ViewBag.HideVoucher = HideVoucher(model);
            if (Session["V_Voucher"] != null)
            {
                Basket.RemoveFromBasket(x => x.ItemType == BasketItemType.Voucher || x.ItemType == BasketItemType.CompatibleDiscount);
                string msg = Basket.ApplyVoucher();
                if (msg != "")
                {
                    ViewBag.VoucherMessage = "<i class=\"fa fa-exclamation-triangle fa-lg\"></i><span class=\"g-p-l-10\">" + msg + "</span>";
                }
            }
            else
            {
                Basket.UpdateBasketSession(model.BasketContents);
            }

            model.ExtendBasket();
            ViewBag.SuppressEdit = false;
            ViewBag.IsPortalUser = false;
            if (Convert.ToBoolean(Session["U_IsPortalUser"]))
            {
                ViewBag.IsPortalUser = true;
            }
            ViewBag.ReturningCustomer = Authentication.IsAuthenticated();

            if (ViewBag.IsPortalUser)
            {
                model.SignIn = new SignIn();
                if (Authentication.IsAuthenticated())
                {
                    model.SignIn.UserName = Session["U_Email"].ToString();
                    model.SignIn.Password = Session["U_Password"].ToString();
                }
                else
                {
                    model.SignIn.UserName = "";
                    model.SignIn.Password = "temp-password";
                }

                // Get 'Product' info for on-hold products
                ViewBag.OrderIsOnHold = false;
                if (model.BasketContents.Find(x => x.PartNo == "ONHOLD") != null)
                {
                    ViewBag.OrderIsOnHold = true;
                }
            }

            model.BreadcrumbTrail.Add("Your Basket", "checkout/");
            ViewBag.BreadcrumbJson = model.BuildBreadcrumbJson();

            return View(model);
        }

        // Redirect if not posted
        public ActionResult Ident()
        {
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public JsonResult Ident(CheckoutViewModel model)
        {
            SaveReturn sr = new SaveReturn();
            string message = "";

            try
            {
                if (model.SignIn.IsNewCustomer)
                {
                    UserAccountInfo userAccountInfo = Authentication.GetAccountInfo(model.SignIn.UserName);
                    sr.IsSuccess = userAccountInfo.IsNewAccount;

                    if (!sr.IsSuccess)
                    {
                        if (userAccountInfo.HasBlankPassword)
                        {
                            sr.Message = "3";
                            message = "Password reset required for this account.";
                        }
                        else
                        {
                            sr.Message = "1";
                            message = "An account for this user already exists.";
                        }
                    }
                }
                else
                {
                    sr.IsSuccess = Authentication.Authenticate(model.SignIn.UserName, model.SignIn.Password);
                    if (!sr.IsSuccess)
                    {
                        UserAccountInfo userAccountInfo = Authentication.GetAccountInfo(model.SignIn.UserName);
                        if (userAccountInfo.HasBlankPassword)
                        {
                            sr.Message = "3";
                            message = "Password reset required for this account.";
                        }
                        else
                        {
                            sr.Message = "2";
                            message = "Incorrect email or password.";
                        }
                    }
                    else
                    {
                        Session["U_FullyAuthenticated"] = true;
                    }
                }
            }
            catch (Exception)
            {
                sr.Message = "2";
                message = "There is a problem with your details.";
            }

            if (sr.Message != "")
            {
                sr.Html = message;
            }

            return Json(new
            {
                savereturn = sr,
                signin = model.SignIn
            });
        }

        // Stage1 uses Post-Redirect-Get pattern in order to allow back button to be used
        [HttpGet]
        public ActionResult Stage1()
        {
            if (Session["C_IsInCheckout"] != null)
            {
                return RedirectToAction("ViewBasket", "Checkout");
            }

            model = new CheckoutViewModel();

            if (Session["C_CheckoutDetails"] == null)
            {
                return RedirectToAction("ViewBasket", "Checkout");
            }

            model.CheckoutDetails = Utilities.LoadSession<CheckoutDetails>("C_CheckoutDetails");
            model.GetAddressDetails();
            if (model.DeliveryOptions.Count > 0)
            {
                Basket.ProcessDelivery(model.DeliveryOptions[0].DeliveryServiceId);
            }
            model.ExtendBasket();

            ViewBag.IsAccountCustomer = Convert.ToBoolean(Session["U_IsAccountCustomer"]);
            model.AccountApplicationDetails = new AccountApplicationDetails();
            if (!ViewBag.IsAccountCustomer)
            {
                model.GetCreditApplicationDetails();
            }

            model.GetCustomerType();
            if (model.AccountApplicationDetails.CustomerType == 1)

                ViewBag.Stage1 = "active";
            ViewBag.AllowEdit = true;
            ViewBag.IsAuthenticated = false;
            if (Authentication.IsAuthenticated())
            {
                ViewBag.IsAuthenticated = true;
            }
            ViewBag.HideVoucher = HideVoucher(model);
            ViewBag.SingleAddress = true;
            if (model.AdditionalAddresses.Count > 1)
            {
                ViewBag.SingleAddress = false;
            }
            ViewBag.ShowAddress = false;
            if (Authentication.IsAuthenticated() && ViewBag.SingleAddress && model.AdditionalAddresses.Count > 0)
            {
                ViewBag.ShowAddress = true;
            }
            ViewBag.HideNewsletter = false;
            if (model.CheckoutDetails.Newsletter == true && !model.CheckoutDetails.IsNewCustomer)
            {
                ViewBag.HideNewsletter = true;
            }
            ViewBag.IsPortalUser = false;
            if (Convert.ToBoolean(Session["U_IsPortalUser"]))
            {
                ViewBag.IsPortalUser = true;
            }
            if (ViewBag.IsPortalUser && Authentication.IsNotAuthenticated())
            {
                // Generate Random Password
                model.CheckoutDetails.Password = Membership.GeneratePassword(16, 4);
            }
            if (ViewBag.IsPortalUser && Authentication.IsAuthenticated() && model.CheckoutDetails.Password == "")
            {
                // Generate Random Password
                model.CheckoutDetails.Password = Membership.GeneratePassword(16, 4);
            }
            if (model.BasketTotals.GrandTotalIncVat <= 0)
            {
                model.CheckoutDetails.PaymentMethod = PaymentMethod.Cheque.ToString();
            }

            ModelState.Clear();
            return View(model);
        }

        // Stage1 uses Post-Redirect-Get pattern in order to allow back button to be used
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult Stage1(CheckoutViewModel model)
        {
            var lbc = (List<BasketContents>)Session["B_BasketArray"];
            var items = lbc.Count(x => x.ItemType != BasketItemType.Delivery);

            // Redirect user back to view basket if they have nothing in their basket or already in checkout
            if (lbc == null || lbc.Count == 0 || Session["C_IsInCheckout"] != null || items == 0)
            {
                return RedirectToAction("ViewBasket", "Checkout");
            }

            if (Authentication.IsAuthenticated())
            {
                Authentication.FixTempRecord(Session["U_Record"].ToString(), Session["U_Email"].ToString());
            }
            model.SetStage0Fields();

            return RedirectToAction("Stage1", "Checkout");
        }

        // Redirect if not posted
        public ActionResult Stage2(bool orderSuccess = false)
        {
            if (orderSuccess)
            {
                return RedirectToAction("Stage3", "Checkout");
            }
            else
            {
                return RedirectToAction("ViewBasket", "Checkout");
            }
        }

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult Stage2(CheckoutViewModel model)
        {
            if (Session["C_CheckoutDetails"] == null)
            {
                var sessionView = RenderPartialViewToString("~/Views/Portal/SessionData.cshtml", model);
                Utilities.LogInformationMessage("CheckoutController/Stage2/1 " + sessionView);
            }

            try
            {
                model.ProcessStage1();
            }
            catch (Exception e)
            {
                // Added code to help debug some strange object reference errors. Should be removed once we have found and fixed the issue.
                var sessionView = RenderPartialViewToString("~/Views/Portal/SessionData.cshtml", model);
                Utilities.LogInformationMessage("CheckoutController/Stage2/2 " + sessionView);
                // Possible redirect to basket page
            }
            ViewBag.Stage2 = "active";
            ViewBag.AllowEdit = false;
            ViewBag.ShowSavedCards = false;
            if (model.CardList.Count > 0 && model.CheckoutDetails.UseASavedCard)
            {
                ViewBag.ShowSavedCards = true;
            }
            ViewBag.HideVoucher = HideVoucher(model);

            ModelState.Clear();
            return View(model);
        }

        // Stage3 uses Post-Redirect-Get pattern
        [HttpPost]
        public ActionResult Stage3(CheckoutViewModel model)
        {
            // Traffic arriving at this point has come via the PayPal / Pay on Account route

            if (Session["C_CheckoutDetails"] == null)
            {
                var sessionView = RenderPartialViewToString("~/Views/Portal/SessionData.cshtml", model);
                Utilities.LogInformationMessage("CheckoutController/Stage3/1 " + sessionView);

                return RedirectToAction("ViewBasket", "Checkout", new { pm = "Stage3Error" });
            }

            if (!model.PayPalPaid)
            {
                model.ProcessStage2();
            }
            else
            {
                model.CheckoutDetails = (CheckoutDetails)Session["C_CheckoutDetails"];
                if (!String.IsNullOrEmpty(Request.Form["PaymentAmountPaid"]))
                {
                    decimal p = 0;
                    if (Decimal.TryParse(Request.Form["PaymentAmountPaid"].ToString(), out p))
                    {
                        model.CheckoutDetails.PaymentAmountPaid = p;
                    }
                }
            }

            return RedirectToAction("Stage3", "Checkout");
        }

        // Stage3 uses Post-Redirect-Get pattern
        [HttpGet]
        public ActionResult Stage3()
        {
            // Traffic arriving directly at this point has come via SagePay route 
            // (Note: PayPal traffic also comes through here but originates from the post method)

            model = new CheckoutViewModel();
            if (Session["C_CheckoutDetails"] == null)
            {
                return RedirectToAction("Index", "Home");
            }
            model.CheckoutDetails = Utilities.LoadSession<CheckoutDetails>("C_CheckoutDetails");

            ViewBag.BackOfficeOrderRef = model.CheckoutDetails.BackOfficeOrderRef == null ? "" : model.CheckoutDetails.BackOfficeOrderRef;
            ViewBag.Email = model.CheckoutDetails.Email == null ? "" : model.CheckoutDetails.Email;
            ViewBag.CardType = model.CheckoutDetails.CardType == null ? "" : model.CheckoutDetails.CardType;
            ViewBag.PaymentMethod = model.CheckoutDetails.PaymentMethod.ToString();

            model.CheckZeroStock();

            ViewBag.Stage3 = "active";
            ViewBag.AllowEdit = false;
            deliveryService ds = DataCache.GetDeliveryService().FirstOrDefault(x => x.DeliveryServiceId == model.CheckoutDetails.DeliveryServiceId);
            if ((model.CheckoutDetails.ZeroStock || model.CheckoutDetails.IsSpecialOrder) && !Convert.ToBoolean(Session["U_IsPortalUser"]))
            {
                ViewBag.DeliveryMessage = Utilities.GetItemFromDict(model.CheckoutData, "ZeroStock", true);
            }
            else
            {
                ViewBag.DeliveryMessage = ds == null ? "" : "<strong>" + ds.InfoMessage
                    .Replace("[Standard-Delivery-Date]", Session["D_StandardDeliveryDay"].ToString() + " " + Session["D_StandardDeliveryMonthDay"].ToString() + " " + ((DateTime)Session["D_StandardDeliveryDate"]).ToString("MMMM yyyy"))
                    .Replace("[Saturday-Delivery-Date]", "Saturday " + Session["D_SaturdayDeliveryDate"]) + "</strong>";
            }
            ViewBag.HideVoucher = HideVoucher(model);

            // Disable any Customer Vouchers used
            if (Session["V_Voucher"] != null)
            {
                VoucherPromo v = (VoucherPromo)Session["V_Voucher"];
                if (!string.IsNullOrEmpty(v.AccountNumber) || v.IsSingleUse)
                {
                    if (model.BasketTotals.Voucher != 0)
                    {
                        v.IsUsed = true;
                        EntityAccess.SaveVoucher(v);

                        if (!string.IsNullOrEmpty(v.AccountNumber))
                        {
                            decimal amountOutstanding = (v.Amount ?? 0) + model.BasketTotals.Voucher;
                            if (amountOutstanding > (decimal)0.50)
                            {
                                SaveReturn sr = new SaveReturn();

                                v.VoucherPromoGroup = null;
                                v.VoucherType = null;
                                v.VoucherCode = Utilities.GetVoucherCode();
                                v.Amount = amountOutstanding;
                                v.IsUsed = false;
                                v.VoucherPromoId = 0;
                                sr = EntityAccess.SaveVoucher(v);
                                if (sr.IsSuccess)
                                {
                                    ViewBag.VoucherMessage =
                                        "A new voucher has been issued and has been emailed to you for the remaining voucher amount.";
                                    v.Email = model.CheckoutDetails.Email;
                                    Utilities.SendPersonalVoucherEmail(v);
                                }
                            }
                        }
                    }
                }
            }

            ViewBag.ShowPasswordForm = false;
            if (model.CheckoutDetails.PaymentMethod == "PayPal")
            {
                // Determine if first time user
                ViewBag.ShowPasswordForm = true;
                if (Authentication.IsAuthenticated() || model.CheckoutDetails.AccountNumber.Contains("/"))
                {
                    ViewBag.ShowPasswordForm = false;
                }
            }

            // This processing is repeated for CreditDebit transactions in MySagePay.cs
            if (model.CheckoutDetails.PaymentMethod != "CreditDebit")
            {
                if (!Convert.ToBoolean(Session["U_IsPortalUser"]) || (Convert.ToBoolean(Session["U_IsPortalUser"]) && !model.CheckoutDetails.SuppressEmail))
                {
                    Utilities.SendEmail(
                        Utilities.GetItemFromDict(model.CheckoutData, "SalesEmail"),
                        model.CheckoutDetails.Email,
                        "Thank you for your order",
                        BuildConfirmationEmail(model),
                        "transactional.emails@netgiant.com");
                }
            }

            ViewBag.Awin = string.Join("|", model.BasketContents
                .Where(x => x.ItemType == BasketItemType.Item)
                .GroupBy(x => x.AffiliateCommissionGroup)
                .Select(x => String.Format("{0}:{1}", x.Key, (x.Sum(y => y.PriceEx * y.Quantity) - x.Sum(z => z.VoucherAmount)).ToString("F2")))
                .ToList());
            ViewBag.IsVatExempt = Session["D_IsVatExempt"] != null && Convert.ToBoolean(Session["D_IsVatExempt"]);

            // Reset Session Variables            
            Session.Remove("C_CheckoutDetails");
            Session.Remove("C_IsInCheckout");
            Session.Remove("V_Voucher");
            Session["D_IsVatExempt"] = false;

            // Tidy up MailChimp stuff
            int w = int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString());
            if (Session["U_MC_CampaignId"] != null && w != 3)
            {
                EntityAccess.InsertMcOrderData(new McOrderData
                {
                    OrderDate = DateTime.Now,
                    McCampaignId = Session["U_MC_CampaignId"].ToString(),
                    OrderNumber = model.CheckoutDetails.BackOfficeOrderRef
                });
            }

            if (Session["U_CartId"] != null)
            {
                Task t = Touchpoints.MailChimpDeleteCartAsync(Session["U_CartId"].ToString());
                Session.Remove("U_CartId");
            }

            Basket.ResetBasket();
            Basket.GetBallparkDelivery();
            ModelState.Clear();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult SetPassword(CheckoutViewModel cvm)
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = false;

            sr = MyAccountViewModel.SetNewPassword(
                cvm.CheckoutDetails.Email,
                cvm.CheckoutDetails.Password);

            if (sr.IsSuccess && cvm.CheckoutDetails.Newsletter)
            {
                if (!Touchpoints.InsertMailingList(
                    cvm.CheckoutDetails.Email,
                    cvm.CheckoutDetails.Newsletter,
                    cvm.CheckoutDetails.Name.Firstname,
                    cvm.CheckoutDetails.Name.Surname))
                {
                    sr.IsSuccess = false;
                }
            }

            if (sr.IsSuccess)
            {
                Authentication.Authenticate(cvm.CheckoutDetails.Email, cvm.CheckoutDetails.Password);
            }

            return Json(new
            {
                savereturn = sr
            });
        }

        public ActionResult Error()
        {
            model = new CheckoutViewModel();
            ViewBag.AllowEdit = false;
            return View(model);
        }

        private string BuildConfirmationEmail(CheckoutViewModel vm)
        {
            vm.BasketItemsEmail = RenderPartialViewToString("~/Views/Misc/OrderConfirmationBasketItems.cshtml", vm.CheckoutDetails);
            return RenderPartialViewToString("~/Views/Misc/OrderConfirmationEmail.cshtml", vm);
        }

        public ActionResult SagePayRegistration(string id)
        {
            model = new CheckoutViewModel();
            CheckoutDetails cd = Utilities.LoadSession<CheckoutDetails>("C_CheckoutDetails");

            if (string.IsNullOrEmpty(id))
            {
                id = "new";
            }

            if ((id == "saved" && string.IsNullOrEmpty(cd.SagePayCardId)) || (id != "new" && id != "delete" && id != "saved"))
            {
                id = "new";
            }

            cd.SagePayTxCode = MySagePay.GenerateTxCode();
            cd.JsonStoreId = MySagePay.SetJsonSession(true, 0);

            // Attempt to Register the transaction with SagePay
            Dictionary<string, string> response = MySagePay.RegisterSagePay(cd, id);

            // Process the response
            SagePayNotification spn = new SagePayNotification();
            spn.DocId = cd.BackOfficeOrderRef;
            spn.VendorTxCode = cd.SagePayTxCode;
            spn.VPSProtocol = response.FirstOrDefault(x => x.Key == "VPSProtocol").Value;
            spn.Status = response.FirstOrDefault(x => x.Key == "Status").Value;
            spn.StatusDetail = response.FirstOrDefault(x => x.Key == "StatusDetail").Value;
            spn.VPSTxID = response.FirstOrDefault(x => x.Key == "VPSTxId").Value;
            spn.SecurityKey = response.FirstOrDefault(x => x.Key == "SecurityKey").Value;
            spn.TxAuthNo = "";
            spn.AVSCV2 = "";
            spn.AddressResult = "";
            spn.PostCodeResult = "";
            spn.CV2Result = "";
            spn.PostString = response.FirstOrDefault(x => x.Key == "PostString").Value;
            spn.ThreeDSecureStatus = "";
            spn.CAVV = "";
            spn.DocType = 2;
            spn.ReDScreened = false;

            string nextUrl = response.FirstOrDefault(x => x.Key == "NextURL").Value + "=" + spn.VPSTxID;

            spn.ResponseString = "Status=" + spn.Status + "\r\nRedirectURL=" + nextUrl + "\r\nStatusDetail=" + spn.StatusDetail + "\r\n";
            EntityAccess.InsertSagePayTransaction(spn);

            cd.SagePaySecurityKey = spn.SecurityKey;
            cd.SagePayUid = spn.VPSTxID;

            if (spn.Status == "OK")
            {
                MySagePay.SetJsonSession(false, cd.JsonStoreId);
                // redirect to the SagePay Return URL
            }

            // If not successful then 
            if (spn.Status != "OK")
            {
                if (id == "delete")
                {
                    return Json(new
                    {
                        success = true
                    });
                }
                // redirect to a SagePay Error page and breakout
                SagePayNotification spn2 = new SagePayNotification();
                spn2.Breakout = true;

                string error = "form.action='/Error/Index/1002/?status=" + HttpUtility.UrlEncode(spn.Status) +
                               "&statusDetail=" + HttpUtility.UrlEncode(spn.StatusDetail) + "';";
                spn2.Breakout = true;
                spn2.ResponseString =
                    "<script type=\"text/javascript\">var form = window.parent.document.getElementById('co-stage2');" +
                    error + "form.submit();</script>";

                Utilities.ProcessException(new Exception(HttpUtility.UrlEncode(spn.StatusDetail)));

                return PartialView("SagePayNotification", spn2);
            }
            if (id == "delete")
            {
                return Json(new
                {
                    success = true
                });
            }
            else
            {
                return Redirect(nextUrl);
            }
        }

        public ActionResult SagePayNotification(string breakout)
        {
            // Note: When this page is launched without 'breakout=true' it will not be associated with the Session currently running for the customer. 
            // It will have it's own Session context. Hence, the usual set of Session variables will not be available.

            CheckoutViewModel cvm = null;

            if (Request["breakout"] != "true")
            {
                int jsonStoreId = 0;
                bool jsonFound = false;
                if (!String.IsNullOrEmpty(Request["array"]))
                {
                    string[] arr = Request["array"].Split('$');
                    if (arr.Length == 4)
                    {
                        jsonStoreId = int.Parse(arr[3]);
                    }
                    jsonFound = MySagePay.GetJsonSession(jsonStoreId);
                }

                cvm = new CheckoutViewModel
                {
                    CheckoutDetails = new CheckoutDetails(),
                    JsonStoreId = jsonFound ? jsonStoreId : 0
                };

                if (Session["C_CheckoutDetails"] != null)
                {
                    if (Request["Status"].Contains("OK"))
                    {
                        cvm.CheckoutDetails = (CheckoutDetails)Session["C_CheckoutDetails"];
                        cvm.BasketEmail = BuildConfirmationEmail(cvm);
                    }
                }
                else
                {
                    if (jsonStoreId != 0 && jsonFound)
                    {
                        Utilities.LogInformationMessage("SagePayNotification - failed to send email CheckoutDetails is null");
                    }
                }
            }

            SagePayNotification model = MySagePay.ProcessNotification(Request, cvm);

            if (Request["breakout"] != "true")
            {
                // Destroy Session state created from Json stream to conserve memory
                Session.RemoveAll();
            }

            return PartialView(model);
        }

        [HttpPost]
        public JsonResult SagePayChangeCard(string id, string cardtype)
        {
            bool isSuccess = true;
            if (Session["C_CheckoutDetails"] == null)
            {
                isSuccess = false;
            }
            else
            {
                ((CheckoutDetails)Session["C_CheckoutDetails"]).SagePayCardId = id;
            }

            return Json(new
            {
                IsSuccess = isSuccess
            });
        }

        [HttpPost]
        public JsonResult SagePayChangeSaveCard(bool saveTheCard)
        {
            ((CheckoutDetails)Session["C_CheckoutDetails"]).SaveThisCard = saveTheCard;
            return Json(new
            {
                IsSuccess = true
            });
        }

        // ***************** PayPal API v2 ***************************    

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> PayPalCapture(string details, string paypaltype)
        {
            SaveReturn sr = new SaveReturn();
            string randomPassword = Membership.GeneratePassword(16, 4);
            sr = await MyPayPalv2.Capture(details, paypaltype, randomPassword, (BasketTotals)Session["B_BasketTotals"]);
            Session.Remove("C_IsInCheckout");

            return Json(sr);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult PayPalGetAmount()
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = true;
            try
            {
                BasketTotals bt = Utilities.LoadSession<BasketTotals>("B_BasketTotals");
                sr.Html = string.Format("{0:N2}", bt.GrandTotalIncVat);
            }
            catch (Exception ex)
            {
                Utilities.ProcessException(ex);
                sr.Message = ex.Message;
                sr.IsSuccess = false;
            }

            return Json(sr);
        }

        public ActionResult PayPalError()
        {
            model = new CheckoutViewModel();
            return View(model);
        }

        public JsonResult ApplyVoucher(string voucherCode)
        {
            if (Session["V_Voucher"] != null || Session["C_IsInCheckout"] != null)
            {
                return Json("");
            }

            SaveReturn sr = Utilities.LoadVoucher(voucherCode);

            string basketSummary = "";
            if (sr.IsSuccess)
            {
                model = new CheckoutViewModel();
                model.ExtendBasket();
                model.CheckoutDetails.ZeroStock = false;
                ViewBag.SuppressEdit = false;
                ViewBag.VoucherMessage = "";
                ViewBag.HideVoucher = HideVoucher(model);
                if (sr.Message != "")
                {
                    ViewBag.VoucherMessage = sr.Html;
                }
                ViewBag.OrderIsOnHold = false;
                if (model.BasketContents.Find(x => x.PartNo == "ONHOLD") != null)
                {
                    ViewBag.OrderIsOnHold = true;
                }

                sr.Html = RenderPartialViewToString("~/Views/Checkout/BasketDetails.cshtml", model);
                basketSummary = RenderPartialViewToString("~/Views/Shared/BasketSummary.cshtml", model);
            }

            BasketTotals bt = (BasketTotals)Session["B_BasketTotals"];
            return Json(new
            {
                savereturn = sr,
                basketTotals = Session["B_BasketTotals"],
                basketQuantity = bt.Quantity.ToString("##0"),
                basketTotal = ConfigurationManager.AppSettings["UseVatInclusivePrices"] == "1"
                    ? bt.GrandTotalIncVat.ToString("#,###,##0.00")
                    : bt.GrandTotalExcVat.ToString("#,###,##0.00"),
                basketSummary = basketSummary
            });
        }

        [HttpPost]
        public JsonResult RemoveVoucher()
        {
            if (Session["V_Voucher"] == null || Session["C_IsInCheckout"] != null)
            {
                return Json("");
            }

            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = true;
            //sr.Html = "<div class=\"g-fc-pm\"><i class=\"fa fa-check fa-lg\"></i><span class=\"g-p-l-10\">Voucher removed<span></div>";

            Session.Remove("B_VoucherCode");
            Basket.RemoveFromBasket(x => x.ItemType == BasketItemType.Voucher || x.ItemType == BasketItemType.CompatibleDiscount);
            Basket.ApplyVoucher();
            Session.Remove("V_Voucher");

            model = new CheckoutViewModel();
            model.ExtendBasket();
            model.CheckoutDetails.ZeroStock = false;
            ViewBag.SuppressEdit = false;
            ViewBag.HideVoucher = HideVoucher(model);
            ViewBag.OrderIsOnHold = false;
            if (model.BasketContents.Find(x => x.PartNo == "ONHOLD") != null)
            {
                ViewBag.OrderIsOnHold = true;
            }

            sr.Html = RenderPartialViewToString("~/Views/Checkout/BasketDetails.cshtml", model);
            string basketSummary = RenderPartialViewToString("~/Views/Shared/BasketSummary.cshtml", model);

            BasketTotals bt = (BasketTotals)Session["B_BasketTotals"];
            return Json(new
            {
                savereturn = sr,
                basketTotals = Session["B_BasketTotals"],
                basketQuantity = bt.Quantity.ToString("##0"),
                basketTotal = ConfigurationManager.AppSettings["UseVatInclusivePrices"] == "1"
                    ? bt.GrandTotalIncVat.ToString("#,###,##0.00")
                    : bt.GrandTotalExcVat.ToString("#,###,##0.00"),
                basketSummary = basketSummary
            });
        }

        public JsonResult RefreshViewBasket()
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = true;

            string basketSummary = "";
            if (sr.IsSuccess)
            {
                model = new CheckoutViewModel();
                model.ExtendBasket();
                model.CheckoutDetails.ZeroStock = false;
                ViewBag.SuppressEdit = false;
                ViewBag.HideVoucher = HideVoucher(model);
                ViewBag.OrderIsOnHold = false;
                if (model.BasketContents.Find(x => x.PartNo == "ONHOLD") != null)
                {
                    ViewBag.OrderIsOnHold = true;
                }

                sr.Html = RenderPartialViewToString("~/Views/Checkout/BasketDetails.cshtml", model);
                basketSummary = RenderPartialViewToString("~/Views/Shared/BasketSummary.cshtml", model);
            }

            BasketTotals bt = (BasketTotals)Session["B_BasketTotals"];
            return Json(new
            {
                savereturn = sr,
                basketTotals = Session["B_BasketTotals"],
                basketQuantity = bt.Quantity.ToString("##0"),
                basketTotal = ConfigurationManager.AppSettings["UseVatInclusivePrices"] == "1"
                    ? bt.GrandTotalIncVat.ToString("#,###,##0.00")
                    : bt.GrandTotalExcVat.ToString("#,###,##0.00"),
                basketSummary = basketSummary
            });
        }

        [HttpPost]
        public JsonResult ClearBasket()
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = true;

            List<BasketContents> lbc = Utilities.LoadSession<List<BasketContents>>("B_BasketArray");

            for (int i = lbc.Count - 1; i >= 0; i--)
            {
                Basket.Delete(lbc[i].StockRef);
            }

            return Json(new
            {
                savereturn = sr
            });
        }

        [HttpPost]
        public JsonResult GetDeliveryOptions(string countrycode, string postcode)
        {
            SaveReturn sr = new SaveReturn();
            model = new CheckoutViewModel();
            sr = model.BuildDeliveryOptions(countrycode, postcode);
            //ViewBag.DeliveryServiceId = model.DeliveryOptions[0].DeliveryServiceId;
            sr.Html = RenderPartialViewToString("~/Views/Checkout/DeliveryOptions.cshtml", model.DeliveryOptions);

            return Json(new
            {
                savereturn = sr
            });
        }

        [HttpPost]
        public JsonResult RemoveCard(int id)
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = false;

            try
            {
                SagePayToken spt = EntityAccess.ReadSagePayTokens(x => x.id == id).FirstOrDefault();
                if (spt != null)
                {
                    spt.deleted = 1;
                    EntityAccess.DeleteSagePayToken(spt);
                    sr.IsSuccess = true;
                }
            }
            catch (Exception)
            {
            }

            return Json(new
            {
                savereturn = sr
            });
        }

        [HttpPost]
        public JsonResult ChangePostCode(string postcode)
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = false;

            CheckoutViewModel.CheckPostcode(postcode);

            try
            {
                if (CheckoutViewModel.InvalidPostCodeCheck(postcode))
                {
                    sr.Message = "We are sorry, but we do not currently deliver to your postcode.";
                }
                else
                {
                    List<deliveryService> deliveryOptions = CheckoutViewModel.RetrieveDeliveryOptions(postcode);
                    if (deliveryOptions != null)
                    {
                        Basket.ProcessDelivery(deliveryOptions[0].DeliveryServiceId);
                        sr.Html = RenderPartialViewToString("~/Views/Checkout/DeliveryOptions.cshtml", deliveryOptions);
                        sr.IsSuccess = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Utilities.LogInformationMessage("CheckoutController/ChangePostCode " + postcode.ToString() + " - " + ex.Message);
            }

            return Json(new
            {
                savereturn = sr
            });
        }

        [HttpPost]
        public JsonResult ChangeDeliveryMethod(int deliveryServiceId)
        {
            SaveReturn sr = new SaveReturn();

            try
            {
                Basket.ProcessDelivery(deliveryServiceId);
                sr.IsSuccess = true;
            }
            catch (Exception)
            {
                sr.IsSuccess = false;
            }

            model = new CheckoutViewModel();
            model.ExtendBasket();
            model.CheckoutDetails.ZeroStock = false;
            model.CheckoutDetails.VoucherCode = Session["B_VoucherCode"]?.ToString() ?? "";
            ViewBag.AllowEdit = true;
            ViewBag.SuppressEdit = false;
            ViewBag.HideVoucher = HideVoucher(model);

            sr.Html = RenderPartialViewToString("~/Views/Checkout/OrderSummary.cshtml", model);

            return Json(new
            {
                savereturn = sr,
                basketTotal = ((BasketTotals)Session["B_BasketTotals"]).GrandTotalIncVat
            });
        }

        [HttpPost]
        public JsonResult BasketChangeQty(string productref, int productqty)
        {
            SaveReturn sr = new SaveReturn();

            if (Session["C_IsInCheckout"] != null)
            {
                sr.IsSuccess = false;

                return Json(new
                {
                    savereturn = sr
                });
            }

            model = new CheckoutViewModel();
            ViewBag.HideVoucher = HideVoucher(model);
            sr = Basket.UpdateQty(productref, productqty);

            Basket.RemoveDelivery();
            Basket.GetBallparkDelivery();

            model = new CheckoutViewModel();
            model.ExtendBasket();
            model.CheckoutDetails.ZeroStock = false;
            ViewBag.SuppressEdit = false;
            if (sr.IsSuccess)
            {
                ViewBag.VoucherMessage = sr.Message;
            }
            ViewBag.HideVoucher = HideVoucher(model);
            ViewBag.OrderIsOnHold = false;
            if (model.BasketContents.Find(x => x.PartNo == "ONHOLD") != null)
            {
                ViewBag.OrderIsOnHold = true;
            }

            sr.Html = RenderPartialViewToString("~/Views/Checkout/BasketDetails.cshtml", model);
            string basketSummary = RenderPartialViewToString("~/Views/Shared/BasketSummary.cshtml", model);

            BasketTotals bt = (BasketTotals)Session["B_BasketTotals"];
            return Json(new
            {
                savereturn = sr,
                basketTotals = Session["B_BasketTotals"],
                basketQuantity = bt.Quantity.ToString("##0"),
                basketTotal = ConfigurationManager.AppSettings["UseVatInclusivePrices"] == "1"
                    ? bt.GrandTotalIncVat.ToString("#,###,##0.00")
                    : bt.GrandTotalExcVat.ToString("#,###,##0.00"),
                basketSummary = basketSummary
            });
        }

        private bool HideVoucher(CheckoutViewModel model)
        {
            if (Session["V_Voucher"] != null)
            {
                VoucherPromo v = (VoucherPromo)Session["V_Voucher"];
                if (v.VoucherTypeFk == (int)VmVoucherType.FreeGift || model.BasketTotals.Voucher != 0)
                {
                    return false;
                }
            }
            return true;
        }

        public ActionResult LoadBasket(string basket = "")
        {
            Basket.LoadCookie(basket);

            return RedirectToAction("Index", "Checkout");
        }
    }
}