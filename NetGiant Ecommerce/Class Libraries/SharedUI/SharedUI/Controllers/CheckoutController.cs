using BusinessLogic;
using BusinessLogic.ViewModels;
using DataAccess.EntityFramework;
using PayPal.Api;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Configuration;
using System.Runtime.CompilerServices;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Newtonsoft.Json;
using VmVoucherType = VMerchantWrapper.Entities.VoucherType;
using System.Threading.Tasks;

namespace SharedUI.Controllers
{
    [SessionExpiredFilter]
    [SiteOfflineCheck]
    public class CheckoutController : ApplicationController
    {
        private CheckoutViewModel model;

        public ActionResult ViewBasket()
        {
            model = new CheckoutViewModel();
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

                ViewBag.OrderIsOnHold = false;
                if (model.BasketContents.Find(x => x.PartNo == "ONHOLD") != null)
                {
                    ViewBag.OrderIsOnHold = true;
                }
            }

            // Get 'Product' info for on-hold products
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
            catch(Exception)
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
            
            if (Session["C_CheckoutDetails"] != null)
            {
                model.CheckoutDetails = (CheckoutDetails)Session["C_CheckoutDetails"];
            }
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
            if (Authentication.IsAuthenticated() && ViewBag.SingleAddress)
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
            var lbc = (List<BasketContents>) Session["B_BasketArray"];
            var items = lbc.Count(x => x.ItemType != BasketItemType.Delivery);

            // Redirect user back to view basket if they have nothing in their basket or already in checkout
            if (lbc == null || lbc.Count == 0 || Session["C_IsInCheckout"] != null || items == 0)
            //if (lbc == null || lbc.Count == 0 || items == 0)
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
            catch
            {
                // Added code to help debug some strange object reference errors. Should be removed once we have found and fixed the issue.
                var sessionView = RenderPartialViewToString("~/Views/Portal/SessionData.cshtml", model);
                Utilities.LogInformationMessage("CheckoutController/Stage2/2 " + sessionView);
            }
            //model.RegisterSagePay();
            //model.ExtendBasket();
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
            }

            return RedirectToAction("Stage3", "Checkout");
        }

        // Stage3 uses Post-Redirect-Get pattern
        [HttpGet]
        public ActionResult Stage3()
        {
            model = new CheckoutViewModel();
            if (Session["C_CheckoutDetails"] == null)
            {
                return RedirectToAction("Index", "Home");
            }
            model.CheckoutDetails = (CheckoutDetails)Session["C_CheckoutDetails"];

            ViewBag.Stage3 = "active";
            ViewBag.AllowEdit = false;
            ViewBag.PayMethod = model.CheckoutDetails.PaymentMethod.ToString();
            deliveryService ds = DataCache.GetDeliveryService().FirstOrDefault(x => x.DeliveryServiceId == model.CheckoutDetails.DeliveryServiceId);
            ViewBag.DeliveryMessage = ds == null ? "" : ds.InfoMessage
                .Replace("[Standard-Delivery-Date]", Session["D_StandardDeliveryDay"].ToString() + " " + Session["D_StandardDeliveryMonthDay"].ToString() + " " + ((DateTime)Session["D_StandardDeliveryDate"]).ToString("MMMM yyyy"))
                .Replace("[Saturday-Delivery-Date]", "Saturday " + Session["D_SaturdayDeliveryDate"]);
            ViewBag.HideVoucher = HideVoucher(model);

            // Disable any Customer Vouchers used
            if (Session["V_Voucher"] != null)
            {
                VoucherPromo v = (VoucherPromo)Session["V_Voucher"];
                if (!string.IsNullOrEmpty(v.AccountNumber))
                {
                    if (model.BasketTotals.Voucher != 0)
                    {
                        v.IsUsed = true;
                        EntityAccess.SaveVoucher(v);

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

            if (model.CheckoutDetails.PaymentMethod != "CreditDebit")
            {
                model.BasketItemsEmail = RenderPartialViewToString("~/Views/Misc/OrderConfirmationBasketItems.cshtml", model.CheckoutDetails);
                if (!Convert.ToBoolean(Session["U_IsPortalUser"]) || (Convert.ToBoolean(Session["U_IsPortalUser"]) && !model.CheckoutDetails.SuppressEmail))
                {
                    Utilities.SendEmail(Utilities.GetItemFromDict(model.CheckoutData, "SalesEmail"), model.CheckoutDetails.Email, "Thank you for your order", BuildConfirmationEmail(model));
                }
            }

            ViewBag.Awin = string.Join("|", model.BasketContents
                .Where(x => x.ItemType == BasketItemType.Item)
                .GroupBy(x => x.AffiliateCommissionGroup)
                .Select(x => String.Format("{0}:{1}", x.Key, (x.Sum(y => y.PriceEx * y.Quantity) - x.Sum(z => z.VoucherAmount)).ToString("F2")))
                .ToList());

            // Reset Session Variables            
            Session.Remove("C_CheckoutDetails");
            Session.Remove("C_IsInCheckout");
            Session.Remove("V_Voucher");

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
            return RenderPartialViewToString("~/Views/Misc/OrderConfirmationEmail.cshtml", vm);
        }

        public ActionResult SagePayRegistration(string id)
        {
            model = new CheckoutViewModel();
            CheckoutDetails cd = new CheckoutDetails();
            if (Session["C_CheckoutDetails"] != null)
            {
                cd = (CheckoutDetails) Session["C_CheckoutDetails"];
            }

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
                    CheckoutDetails = new CheckoutDetails()
                };

                if (Session["C_CheckoutDetails"] != null)
                {
                    if (Request["Status"].Contains("OK"))
                    {
                        cvm.CheckoutDetails = (CheckoutDetails)Session["C_CheckoutDetails"];

                        cvm.BasketItemsEmail = RenderPartialViewToString(
                            "~/Views/Misc/OrderConfirmationBasketItems.cshtml",
                            cvm.CheckoutDetails);
                        cvm.BasketEmail = RenderPartialViewToString("~/Views/Misc/OrderConfirmationEmail.cshtml", cvm);
                    }
                }
                else
                {
                    if (jsonStoreId != 0 && jsonFound)
                    {
                        Utilities.LogInformationMessage(
                        "SagePayNotification - failed to send email CheckoutDetails is null");
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
            ((CheckoutDetails)Session["C_CheckoutDetails"]).SagePayCardId = id;
            return Json(new
            {
                IsSuccess = true
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

        [HttpPost]
        public JsonResult PayPalPayment()
        {
            if (Session["C_IsInCheckout"] != null)
            {
                return null;
            }

            var basketTotal = 0;
            var basketContents = (List<BasketContents>)Session["B_BasketArray"];

            for(int i=0; i < basketContents.Count; i++)
            {
                if(basketContents[i].StockRef != "DELIVERY")
                {
                    basketTotal++;
                }
            }

            if(basketTotal == 0)
            {
                return Json("refresh");
            }

            Session["C_IsInCheckout"] = true;

            Payment payment = new Payment();
            try
            {
                payment = MyPayPal.CreatePayment((CheckoutDetails) Session["C_CheckoutDetails"],
                    (BasketTotals) Session["B_BasketTotals"]);
            }
            catch (PayPal.PaymentsException ex)
            {
                Session.Remove("C_IsInCheckout");
                var sb = new StringBuilder();
                sb.AppendLine("Error:    " + ex.Details.name);
                sb.AppendLine("Message:  " + ex.Details.message);
                sb.AppendLine("URI:      " + ex.Details.information_link);
                sb.AppendLine("Debug ID: " + ex.Details.debug_id);

                foreach (var errorDetails in ex.Details.details)
                {
                    sb.AppendLine("Details:  " + errorDetails.field + " -> " + errorDetails.issue);
                }

                Utilities.ProcessException(ex);
                Utilities.ProcessException(new ApplicationException("PayPal Error: " + sb));
                throw new ApplicationException(ex.Message + "\n" + ex.StackTrace);
            }
            catch (PayPal.PayPalException ex)
            {
                Session.Remove("C_IsInCheckout");
                Utilities.ProcessException(ex);
                throw new ApplicationException(ex.Message + "\n" + ex.StackTrace);
            }
            catch (Exception e)
            {
                Session.Remove("C_IsInCheckout");
                throw new ApplicationException(e.Message + "\n" + e.StackTrace);
            }
            
            return Json(payment);
        }

        [HttpPost]
        public JsonResult PayPalExecute(string paymentid, string payerid, string paypalType)
        {
            Payment payment = new Payment();
            try
            {
                string randomPassword = Membership.GeneratePassword(16, 4);
                payment = MyPayPal.ExecutePayment(paymentid, payerid, paypalType, randomPassword, (BasketTotals)Session["B_BasketTotals"]);

            }
            catch (PayPal.PaymentsException ex)
            {
                Session.Remove("C_IsInCheckout");
                if (ex.StatusCode == HttpStatusCode.BadRequest)
                {
                    payment.failure_reason = ex.Details.message;
                    // throw control back to page with error message
                }
                else
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("Error:    " + ex.Details.name);
                    sb.AppendLine("Message:  " + ex.Details.message);
                    sb.AppendLine("URI:      " + ex.Details.information_link);
                    sb.AppendLine("Debug ID: " + ex.Details.debug_id);

                    if (ex.Details.details != null)
                    {
                        foreach (var errorDetails in ex.Details.details)
                        {
                            sb.AppendLine("Details:  " + errorDetails.field + " -> " + errorDetails.issue);
                        }
                    }
                    Utilities.ProcessException(ex);
                    Utilities.ProcessException(new ApplicationException("PayPal Error: " + sb));
                    throw new ApplicationException(ex.Message + "\n" + ex.StackTrace);
                }
            }
            catch (PayPal.PayPalException ex)
            {
                Session.Remove("C_IsInCheckout");
                Utilities.ProcessException(ex);
                throw new ApplicationException(ex.Message + "\n" + ex.StackTrace);
            }
            catch (Exception e)
            {
                Session.Remove("C_IsInCheckout");
                throw new ApplicationException(e.Message + "\n" + e.StackTrace);
            }

            return Json(payment);
        }

        public ActionResult PayPalError()
        {
            model = new CheckoutViewModel();
            return View(model);
        }

        public JsonResult ApplyVoucher(string voucherCode)
        {
            if(Session["V_Voucher"] != null || Session["C_IsInCheckout"] != null)
            //if (Session["V_Voucher"] != null)
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

            BasketTotals bt = (BasketTotals) Session["B_BasketTotals"];
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
            //if (Session["V_Voucher"] == null)
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

            BasketTotals bt = (BasketTotals) Session["B_BasketTotals"];
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

            List<BasketContents> lbc = new List<BasketContents>();
            if (Session["B_BasketArray"] != null)
            {
                lbc = (List<BasketContents>)Session["B_BasketArray"];
            }

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
                List<deliveryService> deliveryOptions = CheckoutViewModel.RetrieveDeliveryOptions(postcode);
                if (deliveryOptions != null)
                {
                    Basket.ProcessDelivery(deliveryOptions[0].DeliveryServiceId);
                    sr.Html = RenderPartialViewToString("~/Views/Checkout/DeliveryOptions.cshtml", deliveryOptions);
                    sr.IsSuccess = true;
                }
            }
            catch (Exception ex)
            {
                Utilities.LogInformationMessage("CheckoutController/ChangePostCode " + postcode.ToString() + " - " + ex.Message );
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

            return Json(new {
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

            //Basket.ApplyVoucher();

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

            BasketTotals bt = (BasketTotals) Session["B_BasketTotals"];
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
                VoucherPromo v = (VoucherPromo) Session["V_Voucher"];
                if (v.VoucherTypeFk == (int)VmVoucherType.FreeGift || model.BasketTotals.Voucher != 0)
                {
                    return false;
                }
            }
            return true;
        }

        public ActionResult Temp0()
        {
            model = new CheckoutViewModel();
            return View(model);
        }

        public ActionResult Temp1()
        {
            model = new CheckoutViewModel();
            return View(model);
        }
    }
}