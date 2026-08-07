using BusinessLogic;
using BusinessLogic.ViewModels;
using DataAccess.EntityFramework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using System.Web.Security;
using System.Web.Services.Protocols;
using static BusinessLogic.MyOpayo;
using static System.Collections.Specialized.BitVector32;
using VmVoucherType = VMerchantWrapper.Entities.VoucherType;

namespace CommonUI.Controllers
{
    [SessionExpiredFilter]
    [SiteOfflineCheck]
    public class CheckoutController : ApplicationController
    {
        private CheckoutViewModel model;

        [HttpPost]
        public ActionResult PayPalCreateOrderWithAddress()
        {
            StreamReader stream = new StreamReader(Request.InputStream);
            string json = stream.ReadToEnd();

            CheckoutViewModel model = new CheckoutViewModel();
            MyPayPal myPayPal = new MyPayPal();
            string x = myPayPal.CreateOrderWithAddress(json, model);

            return Content(x);
        }

        [HttpGet]
        public ActionResult PayPalDeliveryOptions(string PostCode, string PayPalID, string SelectedID)
        {
            try
            {
                CheckoutViewModel model = new CheckoutViewModel();
                MyPayPal myPayPal = new MyPayPal();

                string updateDeliveryOptionsJSON = myPayPal.CreateDeliveryOptionsJSON(PostCode, SelectedID, model);
                myPayPal.DeliveryOptions(PayPalID, updateDeliveryOptionsJSON);

                return Content("OK");

            }
            catch (Exception ex)
            {
                Utilities.LogInformationMessage("PayPal Delivery Options Error (CheckoutController.cs) - " + ex.Message);
                Response.StatusCode = 400;
                return Content("X");
            }
        }

        public ActionResult AmazonPaySummary(string amazonCheckoutSessionId)
        {
            CheckoutViewModel model = new CheckoutViewModel();
            MyAmazonPay myAmazonPay = new MyAmazonPay();

            myAmazonPay.GetCheckoutSession(amazonCheckoutSessionId);
            myAmazonPay.FillVariables(model);

            model.AmazonCheckoutSessionId = amazonCheckoutSessionId;
            model.AmazonPayRedirectUrl = "/Checkout/AmazonStartPayment?amazonCheckoutSessionId=" + amazonCheckoutSessionId;

            if (CheckoutViewModel.InvalidPostCodeCheck(myAmazonPay.AmazonPayShippingPostcode) == true)
            {
                return RedirectToAction(
                    "ViewBasket",
                    "Checkout",
                    new
                    {
                        pm = "CheckoutError",
                        sz = "md",
                        rpl = "errormessage_We are sorry but we do not currently deliver to your postcode"
                    });
            }

            myAmazonPay.FillDeliveryOptions(model);

            //set the square in the stage thingy at the top
            ViewBag.Stage2 = "active";
            //Voucher?
            ViewBag.HideVoucher = HideVoucher(model);
            //put the edit basket button in
            ViewBag.AllowEdit = true;

            //this "updates" the model to unsure we get the right details from the getgo.
            ChangeDeliveryMethod(model.DeliveryOptions[0].DeliveryServiceId);



            return View(model);
        }

        public ActionResult AmazonStartPayment(string amazonCheckoutSessionId)
        {
            //we are here from AmazonPaySummary. We need to update amazon then get payment

            CheckoutViewModel model = new CheckoutViewModel();
            MyAmazonPay myAmazonPay = new MyAmazonPay();

            //send an update call to Amazon with the price and a few other bits of info
            SaveReturn sr = myAmazonPay.UpdateAmazon(amazonCheckoutSessionId, model);

            if (sr.IsSuccess == false)
            {
                //error with the update
                return RedirectToAction(
                     "ViewBasket",
                     "Checkout",
                     new
                     {
                         pm = "CheckoutError",
                         sz = "md",
                         rpl = "errormessage_Amazon Pay Notification - " + myAmazonPay.ErrorMessage
                     });
            }
            else
            {
                //amazon has been informed of the total price (including extra postage etc) now get the money.
                return Redirect(model.AmazonPayRedirectUrl);
            }
        }

        public ActionResult AmazonPayNotification(string amazonCheckoutSessionId)
        {
            MyAmazonPay myAmazonPay = new MyAmazonPay();
            CheckoutViewModel model = new CheckoutViewModel();

            myAmazonPay.CompleteCheckoutSession(amazonCheckoutSessionId, model.BasketTotals.GrandTotalIncVat, model);

            if (myAmazonPay.IsError == true)
            {
                return RedirectToAction(
                    "ViewBasket",
                    "Checkout",
                    new
                    {
                        pm = "CheckoutError",
                        sz = "md",
                        rpl = "errormessage_Amazon Pay Notification - " + myAmazonPay.ErrorMessage
                    });
            }

            return RedirectToAction("Stage3", "Checkout");
        }

        public ActionResult ViewBasket()
        {
            HttpCookie isLive = new HttpCookie("isLive");
            isLive.Value = ConfigurationManager.AppSettings["Environment"];
            isLive.Expires = DateTime.Now.AddDays(1);
            Response.Cookies.Add(isLive);

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
                Basket.RemoveDelivery();
                Basket.UpdateBasketSession(model.BasketContents);
            }

            model.ExtendBasket();
            model.GetAddOn();
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

            if (ConfigurationManager.AppSettings["AmazonPayMerchantId"] != "OFF")
            {
                MyAmazonPay myAmazonPay = new MyAmazonPay();
                myAmazonPay.GetButton();
                model.AmazonButtonJSONPayLoad = myAmazonPay.AmazonButtonJSONPayLoad;
                model.AmazonButtonSignature = myAmazonPay.AmazonButtonSignature;
            }

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
                        if (!string.IsNullOrEmpty(Session["B_VoucherCode"]?.ToString()))
                        {
                            SaveReturn sret = Utilities.LoadVoucher(Session["B_VoucherCode"].ToString());
                            if (!sret.IsSuccess)
                            {
                                Basket.RemoveFromBasket(x => x.ItemType == BasketItemType.Voucher || x.ItemType == BasketItemType.CompatibleDiscount);
                                Basket.ApplyVoucher();
                            }
                        }
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

            ViewBag.IsSoftOptIn = Utilities.GetBoolItemFromDict(model.CheckoutData, "IsSoftOptIn");
            if (ViewBag.IsSoftOptIn && model.CheckoutDetails.IsNewCustomer)
            {
                model.CheckoutDetails.Newsletter = true;
            }
            model.CheckoutDetails.NewsletterInverse = !model.CheckoutDetails.Newsletter;
            ViewBag.HideNewsletter = false;
            if ((model.CheckoutDetails.Newsletter || ViewBag.IsSoftOptIn) && !model.CheckoutDetails.IsNewCustomer)
            {
                ViewBag.HideNewsletter = true;
            }

            // Portal settings/overrides
            ViewBag.IsPortalUser = false;
            if (Convert.ToBoolean(Session["U_IsPortalUser"]))
            {
                ViewBag.IsPortalUser = true;
                if (model.CheckoutDetails.IsNewCustomer)
                {
                    model.CheckoutDetails.Newsletter = false;
                    model.CheckoutDetails.NewsletterInverse = true;
                    ViewBag.HideNewsletter = true;
                }
                if (Authentication.IsNotAuthenticated())
                {
                    // Generate Random Password
                    model.CheckoutDetails.Password = Membership.GeneratePassword(16, 4);
                }
                if (Authentication.IsAuthenticated() && model.CheckoutDetails.Password == "")
                {
                    // Generate Random Password
                    model.CheckoutDetails.Password = Membership.GeneratePassword(16, 4);
                }
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
        //public ActionResult Stage2(bool orderSuccess = false)
        //{
        //    if (orderSuccess)
        //    {
        //        return RedirectToAction("Stage3", "Checkout");
        //    }
        //    else
        //    {
        //        return RedirectToAction("ViewBasket", "Checkout");
        //    }
        //}

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult Stage2(CheckoutViewModel model)
        //{
        //    if (Session["C_CheckoutDetails"] == null)
        //    {
        //        var sessionView = RenderPartialViewToString("~/Views/Portal/SessionData.cshtml", model);
        //        Utilities.LogInformationMessage("CheckoutController/Stage2/1 " + Request.UrlReferrer + "--" + sessionView);
        //        return RedirectToAction("ViewBasket", "Checkout", new { pm = "CheckoutError", sz = "md", rpl = "errormessage_Please sign in and try again" });
        //    }

        //    try
        //    {
        //        if (!model.ProcessStage1())
        //        {
        //            // Error: Can't create or find user
        //            return RedirectToAction("ViewBasket", "Checkout", new { pm = "CheckoutError", sz = "md", rpl = "errormessage_Please try again" });
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        // Added code to help debug some strange object reference errors. Should be removed once we have found and fixed the issue.
        //        var sessionView = RenderPartialViewToString("~/Views/Portal/SessionData.cshtml", model);
        //        Utilities.LogInformationMessage("CheckoutController/Stage2/2 " + sessionView + e.Message.ToString());
        //        // Possible redirect to basket page
        //    }
        //    ViewBag.Stage2 = "active";
        //    ViewBag.AllowEdit = false;
        //    ViewBag.ShowSavedCards = false;
        //    if (model.CardList.Count > 0 && model.CheckoutDetails.UseASavedCard)
        //    {
        //        ViewBag.ShowSavedCards = true;
        //    }
        //    ViewBag.HideVoucher = HideVoucher(model);

        //    ModelState.Clear();

        //    //if the back button is used at stage 2 back to 1 this automatically clears the expensive shipping (if it's been chosen)
        //    //then going forward again to stage 2 the basket is updated and displaey - but NOT the total in C_ChekoutDetails
        //    //And THAT is what is passed to Opayo. This syncs the checkoutdetails with the basektotals.
        //    //There's probably a better method... if you find it delete this...
        //    CheckoutDetails myCheckoutDetails = Session["C_CheckoutDetails"] as CheckoutDetails;
        //    BasketTotals myBasketTotals = Session["B_BasketTotals"] as BasketTotals;
        //    myCheckoutDetails.TotalIncVat = myBasketTotals.GrandTotalIncVat;
        //    Session["C_CheckoutDetails"] = myCheckoutDetails;

        //    return View(model);
        //}

        #region Opayo PI Integration Pages

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult Stage2a(CheckoutViewModel model)
        {
            // For processing Opayo PI integration
            if (Session["C_CheckoutDetails"] == null)
            {
                var sessionView = RenderPartialViewToString("~/Views/Portal/SessionData.cshtml", model);
                Utilities.LogInformationMessage("CheckoutController/Stage2/1 " + Request.UrlReferrer + "--" + sessionView);
                return RedirectToAction("ViewBasket", "Checkout", new { pm = "CheckoutError", sz = "md", rpl = "errormessage_Please sign in and try again" });
            }

            if (Utilities.GetBoolItemFromDict(model.CheckoutData, "IsSoftOptIn"))
            {
                model.CheckoutDetails.Newsletter = !model.CheckoutDetails.Newsletter;
            }

            try
            {
                if (!model.ProcessStage1())
                {
                    // Error: Can't create or find user
                    return RedirectToAction("ViewBasket", "Checkout", new { pm = "CheckoutError", sz = "md", rpl = "errormessage_Please try again" });
                }
            }
            catch (Exception e)
            {
                // Added code to help debug some strange object reference errors. Should be removed once we have found and fixed the issue.
                var sessionView = RenderPartialViewToString("~/Views/Portal/SessionData.cshtml", model);
                Utilities.LogInformationMessage("CheckoutController/Stage2/2 " + sessionView + e.Message.ToString());
                return RedirectToAction("ViewBasket", "Checkout", new { pm = "CheckoutError", sz = "md", rpl = "errormessage_An error was encountered. Please try again" });
            }

            model.CheckoutDetails.MerchantSessionKey = MyOpayo.GetMerchantSessionKey();
            foreach (SagePayToken spt in model.CardList)
            {
                spt.token = spt.token.Replace("{", "").Replace("}", "");
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

            //if the back button is used at stage 2 back to 1 this automatically clears the expensive shipping (if it's been chosen)
            //then going forward again to stage 2 the basket is updated and displaey - but NOT the total in C_ChekoutDetails
            //And THAT is what is passed to Opayo. This syncs the checkoutdetails with the basektotals.
            //There's probably a better method... if you find it delete this...
            CheckoutDetails myCheckoutDetails = Session["C_CheckoutDetails"] as CheckoutDetails;
            BasketTotals myBasketTotals = Session["B_BasketTotals"] as BasketTotals;
            myCheckoutDetails.TotalIncVat = myBasketTotals.GrandTotalIncVat;
            Session["C_CheckoutDetails"] = myCheckoutDetails;

            return View(model);
        }

        // Stage2b processes the payment card information via Opayo. It is called via AJAX from Stage2a
        [HttpPost]
        public JsonResult Stage2b(string UseASavedCard,
            string SaveThisCard,
            string BrowserColorDepth,
            string BrowserScreenHeight,
            string BrowserScreenWidth,
            string MerchantSessionKey,
            string CardIdentifier)
        {
            // Process Opayo Transaction
            CheckoutViewModel cvm = new CheckoutViewModel
            {
                CheckoutDetails = (CheckoutDetails)Session["C_CheckoutDetails"],
            };
            cvm.CheckoutDetails.UseASavedCard = Convert.ToBoolean(UseASavedCard);
            cvm.CheckoutDetails.SaveThisCard = Convert.ToBoolean(SaveThisCard);
            cvm.BrowserColorDepth = BrowserColorDepth;
            cvm.BrowserScreenHeight = BrowserScreenHeight;
            cvm.BrowserScreenWidth = BrowserScreenWidth;
            cvm.CheckoutDetails.MerchantSessionKey = new MerchantSessionKey()
            {
                merchantSessionKey = MerchantSessionKey
            };
            cvm.CardIdentifier = CardIdentifier;

            SaveReturn savereturn = new SaveReturn()
            {
                IsSuccess = true
            };

            cvm.BasketEmail = BuildConfirmationEmail(cvm);
            if (cvm.CheckoutDetails != null)
            {
                cvm.CheckoutDetails.JsonStoreId = SetJsonSession(true, 0);
                RestResponse response = MyOpayo.SubmitTransaction(cvm, Request);

                if (response != null)
                {
                    if (response.ResponseStatus == ResponseStatus.Error)
                    {
                        EntityAccess.InsertOpayoLog(response.Content, cvm.CheckoutDetails.BackOfficeOrderRef, cvm.CheckoutDetails.MerchantSessionKey.merchantSessionKey, "Error");

                        savereturn.IsSuccess = false;
                        savereturn.Message = "Error";
                        savereturn.Html = JValue.Parse(response.Content).ToString(Formatting.Indented);
                        return Json(new
                        {
                            savereturn = savereturn
                        });
                    }

                    cvm.CheckoutDetails.CardType = JObject.Parse(response.Content)["paymentMethod"]?["card"]?["cardType"]?.ToString() ?? "";

                    if (response.StatusCode == HttpStatusCode.Accepted)
                    {
                        // Usually requires 3D Auth to be initiated which ultimately involves the Notification page being called
                        OpayoChallengeAuthentication oca = JsonConvert.DeserializeObject<OpayoChallengeAuthentication>(response.Content);
                        if (oca.status == "3DAuth")
                        {
                            // For 3DAuth we need to redirect the customer to oca.acsUrl via a POST
                            EntityAccess.InsertOpayoLog(response.Content, cvm.CheckoutDetails.BackOfficeOrderRef, cvm.CheckoutDetails.MerchantSessionKey.merchantSessionKey, "Request For Challenge");
                            cvm.CheckoutDetails.OpayoChallengeAuthentication = oca;
                            Session["C_CheckoutDetails"] = cvm.CheckoutDetails;
                            MyOpayo.SetJsonSession(false, cvm.CheckoutDetails.JsonStoreId);

                            savereturn.Message = "3DAuth";
                            return Json(new
                            {
                                savereturn = savereturn
                            });
                        }
                        Utilities.LogInformationMessage("Opayo Error - Unable to create Transaction: " + oca.statusCode + " - " + oca.statusDetail);
                    }

                    if (response.StatusCode == HttpStatusCode.Created)
                    {
                        // No 3D Auth required. Just process the response
                        SaveReturn sr = MyOpayo.ProcessTransactionResponse(response, cvm);
                        if (sr.IsSuccess)
                        {
                            savereturn.Message = "Authorised";
                            return Json(new
                            {
                                savereturn = savereturn
                            });
                        }
                        savereturn.IsSuccess = false;
                        savereturn.Message = sr.Message == "Rejected" ? "Rejected" : "Error";
                        savereturn.Html = sr.Html;
                        return Json(new
                        {
                            savereturn = savereturn
                        });
                    }
                }

                EntityAccess.InsertOpayoLog(response.Content, cvm.CheckoutDetails.BackOfficeOrderRef, cvm.CheckoutDetails.MerchantSessionKey.merchantSessionKey, "Error");
                Utilities.LogInformationMessage("ProcessOrder via Opayo - Invalid Response");
                savereturn.Message = "Error";
                savereturn.Html = JValue.Parse(response.Content).ToString(Formatting.Indented);
                return Json(new
                {
                    savereturn = savereturn
                });
            }

            Utilities.LogInformationMessage("ProcessOrder via Opayo - CheckoutDetails is null");
            savereturn.Message = "Error";
            savereturn.Html = "";
            return Json(new
            {
                savereturn = savereturn
            });
        }

        public ActionResult OpayoNotification(string jsonid)
        {
            // Note: When this page is launched it will not be associated with the Session currently running for the customer. 
            // It will have it's own Session context. Hence, the usual set of Session variables will NOT be available.

            RestResponse response = MyOpayo.Submit3DAuthTransaction(Request.Form["cres"], Request.Form["threeDSSessionData"]);
            int jsonId;
            if (Int32.TryParse(jsonid, out jsonId))
            {
                MyOpayo.GetJsonSession(jsonId);
            }
            else
            {
                // Error
            }

            CheckoutViewModel cvm = new CheckoutViewModel
            {
                CheckoutDetails = (CheckoutDetails)Session["C_CheckoutDetails"],
            };
            cvm.BasketEmail = BuildConfirmationEmail(cvm);

            SaveReturn sr = MyOpayo.ProcessTransactionResponse(response, cvm);

            MyOpayo.SetJsonSession(false, jsonId);

            if (sr.IsSuccess)
            {
                return RedirectToAction("Opayo3DAuth", new { type = 1 });
            }
            else
            {
                if (sr.Message == "Rejected")
                {
                    return RedirectToAction("Opayo3DAuth", new { type = 3, message = "Please check that you have entered your card details, including your security code, correctly." });
                }
            }
            return RedirectToAction("Opayo3DAuth", new { type = 2, message = sr.Message });
        }

        [HttpPost]
        public JsonResult OpayoRefreshKey()
        {
            CheckoutViewModel cvm = new CheckoutViewModel
            {
                CheckoutDetails = (CheckoutDetails)Session["C_CheckoutDetails"],
            };
            cvm.CheckoutDetails.MerchantSessionKey = MyOpayo.GetMerchantSessionKey();
            return Json(new
            {
                savereturn = new SaveReturn()
                {
                    IsSuccess = true,
                    Message = cvm.CheckoutDetails.MerchantSessionKey.merchantSessionKey
                }
            });
        }

        public ActionResult OpayoDeleteCard(string tokenId, int id)
        {
            bool success = MyOpayo.DeleteCard(tokenId, id);
            return Json(new { success = success });
        }

        public ActionResult Opayo3DAuth()
        {
            // type = 0: First call
            // type = 1: Success
            // type = 2: Failure with "Rejected" messsage (possibly data entry error)
            // type = 3: Failure (reason given in 'message')

            ViewBag.Action = int.Parse(Request.QueryString["type"] ?? "0");
            ViewBag.Message = Request.QueryString["message"] ?? "";
            return View();
        }
        #endregion

        // Stage3 uses Post-Redirect-Get pattern
        [HttpPost]
        public ActionResult Stage3(CheckoutViewModel model)
        {
            // Traffic arriving at this point has come via the PayPal / Pay on Account / Telephone / BACS route

            if (Session["C_CheckoutDetails"] == null)
            {
                var sessionView = RenderPartialViewToString("~/Views/Portal/SessionData.cshtml", model);
                Utilities.LogInformationMessage("CheckoutController/Stage3/1 " + sessionView);

                return RedirectToAction("ViewBasket", "Checkout", new { pm = "Stage3Error" });
            }
            Session["U_CustomerTypeId"] = model.AccountApplicationDetails == null
                ? 1
                : model.AccountApplicationDetails.CustomerType;

            if (!model.PayPalPaid)
            {
                if (!model.ProcessStage2())
                {
                    return RedirectToAction("ViewBasket", "Checkout", new { pm = "CheckoutError", size = "md", rpl = "errormessage_An error was encountered - please try again" });
                }
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
            // Traffic arriving directly at this point has come via SagePay or Amazon route 
            // (Note: PayPal traffic also comes through here but originates from the post method)

            model = new CheckoutViewModel();
            if (Session["C_CheckoutDetails"] == null)
            {
                return RedirectToAction("Index", "Home");
            }
            model.CheckoutDetails = Utilities.LoadSession<CheckoutDetails>("C_CheckoutDetails");

            if (model.CheckoutDetails.PaymentMethod == "AmazonPay" || model.CheckoutDetails.PaymentMethod == "PayPal")
            {
                model.CheckoutDetails.RecipientName.Title = "";
                model.CheckoutDetails.Name.Title = "";
            }

            // Retrieve CardType from Json Store as it may be incorrect
            if (model.CheckoutDetails.CardType == "")
            {
                model.CheckoutDetails.CardType = MyOpayo.GetCardTypeFromJsonSession(model.CheckoutDetails.JsonStoreId);
            }

            ViewBag.BackOfficeOrderRef = model.CheckoutDetails.BackOfficeOrderRef == null ? "Order Reference To Follow" : model.CheckoutDetails.BackOfficeOrderRef;
            ViewBag.Email = model.CheckoutDetails.Email == null ? "" : model.CheckoutDetails.Email;
            ViewBag.CardType = model.CheckoutDetails.CardType == null ? "" : model.CheckoutDetails.CardType;
            ViewBag.PaymentMethod = model.CheckoutDetails.PaymentMethod.ToString();
            ViewBag.IsNewCustomer = model.CheckoutDetails.IsNewCustomer;
            ViewBag.CustomerTypeId = Session["U_CustomerTypeId"] ?? "1";

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
                                VoucherPromo vn = new VoucherPromo();
                                SaveReturn sr = new SaveReturn();

                                // Ensure that all related entities are set to null
                                v.VoucherPromoGroup = null;
                                v.Website = null;

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
            if (model.CheckoutDetails.PaymentMethod == "PayPal" || model.CheckoutDetails.PaymentMethod == "AmazonPay")
            {
                // Determine if first time user
                ViewBag.ShowPasswordForm = true;
                if (Authentication.IsAuthenticated() || model.CheckoutDetails.AccountNumber.Contains("/") || model.CheckoutDetails.IsInterimOrder)
                {
                    ViewBag.ShowPasswordForm = false;
                }
            }

            // This processing is repeated for CreditDebit transactions in MySagePay.cs
            if (model.CheckoutDetails.PaymentMethod != "CreditDebit" && !model.CheckoutDetails.IsInterimOrder)
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

            switch (ConfigurationManager.AppSettings["WebsiteId"].ToString())
            {
                case "1":
                    {
                        ViewBag.Awin = string.Join("|", model.BasketContents
                            .Where(x => x.ItemType == BasketItemType.Item)
                            .GroupBy(x => x.AffiliateCommissionGroup)
                            .Select(x => String.Format("{0}:{1}", x.Key, (x.Sum(y => y.PriceEx * y.Quantity) - x.Sum(z => z.VoucherAmount)).ToString("F2")))
                            .ToList());
                        break;
                    }
                case "2":
                    {
                        ViewBag.Awin = string.Join("|", model.BasketContents
                            .Where(x => x.ItemType == BasketItemType.Item)
                            .GroupBy(x => x.AffiliateCommissionGroup)
                            .Select(x => String.Format("{0}:{1}", x.Key, (x.Sum(y => y.PriceInc * y.Quantity) - x.Sum(z => z.VoucherAmount)).ToString("F2")))
                            .ToList());
                        break;
                    }
            }

            if (!Convert.ToBoolean(Session["U_IsPortalUser"]))
            {
                Task SendAwinData = Touchpoints.SendAwinData(model);
            }
            Touchpoints.DoCampaignTracking(model);

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

                Session["U_CartId"] = Session["U_Record"].ToString().Replace("/", "") + "_" + DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss-fffffff");
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

            if (sr.IsSuccess)
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

        //public ActionResult SagePayRegistration(string id, string jsonid)
        //{
        //    //Utilities.WriteLogFile("Checkout Controller SagePayRegistration", true);

        //    model = new CheckoutViewModel();
        //    CheckoutDetails cd = Utilities.LoadSession<CheckoutDetails>("C_CheckoutDetails");

        //    if (string.IsNullOrEmpty(id))
        //    {
        //        id = "new";
        //    }

        //    if ((id == "saved" && string.IsNullOrEmpty(cd.SagePayCardId)) || (id != "new" && id != "delete" && id != "saved"))
        //    {
        //        id = "new";
        //    }

        //    cd.SagePayTxCode = MySagePay.GenerateTxCode();
        //    cd.JsonStoreId = MySagePay.SetJsonSession(true, 0);

        //    // Attempt to Register the transaction with SagePay
        //    Dictionary<string, string> response = MySagePay.RegisterSagePay(cd, id);

        //    // Process the response
        //    SagePayNotification spn = new SagePayNotification();
        //    spn.DocId = cd.BackOfficeOrderRef;
        //    spn.VendorTxCode = cd.SagePayTxCode;
        //    spn.VPSProtocol = response.FirstOrDefault(x => x.Key == "VPSProtocol").Value;
        //    spn.Status = response.FirstOrDefault(x => x.Key == "Status").Value;
        //    spn.StatusDetail = response.FirstOrDefault(x => x.Key == "StatusDetail").Value;
        //    spn.VPSTxID = response.FirstOrDefault(x => x.Key == "VPSTxId").Value;
        //    spn.SecurityKey = response.FirstOrDefault(x => x.Key == "SecurityKey").Value;
        //    spn.TxAuthNo = "";
        //    spn.AVSCV2 = "";
        //    spn.AddressResult = "";
        //    spn.PostCodeResult = "";
        //    spn.CV2Result = "";
        //    spn.PostString = response.FirstOrDefault(x => x.Key == "PostString").Value;
        //    spn.ThreeDSecureStatus = "";
        //    spn.CAVV = "";
        //    spn.DocType = 2;
        //    spn.ReDScreened = false;

        //    string nextUrl = response.FirstOrDefault(x => x.Key == "NextURL").Value + "=" + spn.VPSTxID;

        //    spn.ResponseString = "Status=" + spn.Status + "\r\nRedirectURL=" + nextUrl + "\r\nStatusDetail=" + spn.StatusDetail + "\r\n";
        //    EntityAccess.InsertSagePayTransaction(spn);

        //    cd.SagePaySecurityKey = spn.SecurityKey;
        //    cd.SagePayUid = spn.VPSTxID;

        //    if (spn.Status == "OK")
        //    {
        //        MySagePay.SetJsonSession(false, cd.JsonStoreId);
        //        // redirect to the SagePay Return URL
        //    }

        //    // If not successful then 
        //    if (spn.Status != "OK")
        //    {
        //        if (id == "delete")
        //        {
        //            return Json(new
        //            {
        //                success = true
        //            });
        //        }
        //        // redirect to a SagePay Error page and breakout
        //        SagePayNotification spn2 = new SagePayNotification();
        //        spn2.Breakout = true;

        //        string error = "form.action='/Error/Index/1002/?status=" + HttpUtility.UrlEncode(spn.Status) +
        //                       "&statusDetail=" + HttpUtility.UrlEncode(spn.StatusDetail) + "';";
        //        spn2.Breakout = true;
        //        spn2.ResponseString =
        //            "<script type=\"text/javascript\">var form = window.parent.document.getElementById('co-stage2');" +
        //            error + "form.submit();</script>";

        //        Utilities.ProcessException(new Exception(HttpUtility.UrlEncode(spn.StatusDetail)));

        //        return PartialView("SagePayNotification", spn2);
        //    }
        //    if (id == "delete")
        //    {
        //        return Json(new
        //        {
        //            success = true
        //        });
        //    }
        //    else
        //    {
        //        return Redirect(nextUrl);
        //    }
        //}

        //public ActionResult SagePayNotification(string breakout)
        //{
        //    // Note: When this page is launched it will not be associated with the Session currently running for the customer. 
        //    // It will have it's own Session context. Hence, the usual set of Session variables will not be available.

        //    //Utilities.WriteLogFile("Checkout Controller SagePayNotification");

        //    CheckoutViewModel cvm = null;

        //    if (Request["breakout"] != "true")
        //    {
        //        int jsonStoreId = 0;
        //        bool jsonFound = false;
        //        if (!String.IsNullOrEmpty(Request["array"]))
        //        {
        //            string[] arr = Request["array"].Split('$');
        //            if (arr.Length == 4)
        //            {
        //                jsonStoreId = int.Parse(arr[3]);
        //            }
        //            // Partially restoration of Session
        //            jsonFound = MySagePay.GetJsonSession(jsonStoreId);
        //        }

        //        cvm = new CheckoutViewModel
        //        {
        //            CheckoutDetails = new CheckoutDetails(),
        //            JsonStoreId = jsonFound ? jsonStoreId : 0
        //        };

        //        if (Session["C_CheckoutDetails"] != null)
        //        {
        //            if (Request["Status"].Contains("OK"))
        //            {                        
        //                cvm.CheckoutDetails = (CheckoutDetails)Session["C_CheckoutDetails"];
        //                cvm.BasketEmail = BuildConfirmationEmail(cvm);
        //            }
        //        }
        //        else
        //        {
        //            if (jsonStoreId != 0 && jsonFound)
        //            {
        //                Utilities.LogInformationMessage("SagePayNotification - failed to send email CheckoutDetails is null");
        //            }
        //        }
        //    }

        //    SagePayNotification model = MySagePay.ProcessNotification(Request, cvm);

        //    if (Request["breakout"] != "true")
        //    {
        //        // Destroy Session state created from Json stream to conserve memory
        //        Session.RemoveAll();
        //    }

        //    return PartialView(model);
        //}

        //public ActionResult SagePayBreakout()
        //{
        //    var sb = new StringBuilder();
        //    sb.AppendLine("<script type=\"text/javascript\">");

        //    string addon = "form.submit();";

        //    if (Request["error"] == "true")
        //    {
        //        if (Request["status"] != "ERROR")
        //        {
        //            if (Request["status"] == "INVALID" && Request["statusDetail"].StartsWith("9010: "))
        //            {
        //                addon = "window.parent.location = '/checkout/'";
        //            }
        //            else
        //            {
        //                addon = "window.parent.showSavedCards(\"" + Request["statusDetail"] + "\");";
        //            }
        //        }
        //        else
        //        {
        //            sb.AppendLine("var form = window.parent.document.getElementById('co-stage2');");
        //            switch (Request["errorRoute"])
        //            {
        //                case "1":
        //                    {
        //                        addon = "form.action='/Error/Index/1002/?status=" + Request["status"] + "&statusDetail=" + Request["statusDetail"] + "';form.submit();";
        //                        break;
        //                    }
        //                case "2":
        //                    {
        //                        addon = "window.parent.location = '/checkout/?pm=Stage3Error';";
        //                        break;
        //                    }
        //            }
        //        }
        //    }
        //    else
        //    {
        //        addon = "window.parent.location = '/checkout/stage2?orderSuccess=true';";
        //    }

        //    // If we have lost the session then get it back from the SQL table (Firefox and Safari has this behavoir)               
        //    if (Session["C_CheckoutDetails"] == null)
        //    {
        //        int jsonStoreId = Int32.Parse(Request["jsonstoreid"]);
        //        bool jsonFound = MySagePay.GetJsonSession(jsonStoreId);
        //        if (jsonFound)
        //        {
        //            CheckoutDetails cdTemp = Utilities.LoadSession<CheckoutDetails>("C_CheckoutDetails");
        //            // Password may be encrypted
        //            Authentication.Authenticate(cdTemp.Email, "");
        //        }
        //        else
        //        {
        //            Utilities.LogInformationMessage("Unable to load JSON session for id: " + jsonStoreId.ToString());
        //        }
        //    }
        //    // Fix fields in the CheckoutDetails   
        //    CheckoutDetails cd = new CheckoutDetails();
        //    if (Session["C_CheckoutDetails"] != null)
        //    {
        //        cd = (CheckoutDetails)Session["C_CheckoutDetails"];
        //        cd.CardType = Request["cardtype"];
        //        cd.CardLast4Digits = Request["last4"];
        //        cd.SagePayTxCode = Request["txcode"];
        //        cd.SagePayAuthCode = Request["authcode"];
        //        cd.SageToken = Request["token"];

        //        Session["C_CheckoutDetails"] = cd;
        //    }

        //    sb.Append(addon);
        //    sb.AppendLine("</script>");

        //    return PartialView((object)sb.ToString());
        //}

        [HttpPost]
        public JsonResult OpayoChangeCard(string id, string cardtype)
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
        public JsonResult OpayoChangeSaveCard(bool saveTheCard)
        {
            ((CheckoutDetails)Session["C_CheckoutDetails"]).SaveThisCard = saveTheCard;
            return Json(new
            {
                IsSuccess = true
            });
        }

        // PayPal API v2 Integration   

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> PayPalCapture(string details, string paypaltype)
        {
            SaveReturn sr = new SaveReturn();
            string randomPassword = Membership.GeneratePassword(16, 4);
            sr = await MyPayPal.Capture(details, paypaltype, randomPassword, (BasketTotals)Session["B_BasketTotals"]);
            Session.Remove("C_IsInCheckout");

            return Json(sr);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> PayPalCaptureStage1(string details, string paypaltype)
        {
            SaveReturn sr = await MyPayPal.CaptureStage1(details);
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
                List<BasketContents> lbc = Utilities.LoadSession<List<BasketContents>>("B_BasketArray");
                sr.Html = Basket.GetBasketTotal(lbc).ToString();
                //decimal? del = lbc.Find(x => x.StockRef == "DELIVERY")?.PriceInc;
                //sr.Html = (bt.GrandTotalIncVat - (del ?? 0)).ToString();
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

        // End of PayPal API v2 Integration

        public JsonResult ApplyVoucher(string voucherCode)
        {
            if (Session["V_Voucher"] != null || Session["C_IsInCheckout"] != null)
            {
                return Json("");
            }

            SaveReturn sr = Utilities.LoadVoucher(voucherCode);
            System.Diagnostics.Debug.WriteLine(sr.IsSuccess);
            System.Diagnostics.Debug.WriteLine(sr.Html);
            string basketSummary = "";
            string voucherMessage = sr.Html;
            if (sr.IsSuccess)
            {
                model = new CheckoutViewModel();
                model.ExtendBasket();

                ViewBag.VoucherMessage = voucherMessage;

                ViewBag.SuppressEdit = false;
                ViewBag.HideVoucher = HideVoucher(model);

                ViewBag.OrderIsOnHold = false;

                if (model.BasketContents.Any(x => x.PartNo == "ONHOLD"))
                {
                    ViewBag.OrderIsOnHold = true;
                }

                // Also render the mini-cart summary so it can be refreshed in place when the
                // voucher is applied from there (previously this was left blank and only the
                // full basket page picked up the applied voucher).
                basketSummary = RenderPartialViewToString(
                    "~/Views/Shared/BasketSummary.cshtml",
                    model);

                sr.Html = RenderPartialViewToString(
                    "~/Views/Checkout/BasketDetails.cshtml",
                    model);
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

        /// <summary>
        /// Called when the mini-cart's "Proceed to Checkout" button is clicked. If any basket
        /// item has in-stock add-on products linked via ProductAddon, returns the "You May Also
        /// Need" popup markup so the mini-cart JS can show it instead of navigating straight to
        /// /checkout/. Capped at 3 products total across the whole basket, deduped, and excludes
        /// anything already in the basket.
        /// </summary>
        [HttpPost]
        public JsonResult GetAddSellPopup()
        {
            model = new CheckoutViewModel();
            model.ExtendBasket();
            model.GetAddOn();

            List<string> basketRefs = model.BasketContents.Select(x => x.StockRef).ToList();

            // No cap here (matches the inline "You May Also Need" list on BasketDetails.cshtml,
            // which also doesn't cap) - the popup's carousel (YouMayAlsoNeed.cshtml) shows 3 at a
            // time and only renders its prev/next arrows when there are more than 3, so capping
            // the list to exactly 3 here meant those arrows could never have anything to scroll to.
            List<BasketContents> addSell = model.BasketContents
                .Where(x => x.AddonProducts != null)
                .SelectMany(x => x.AddonProducts)
                .Where(x => !basketRefs.Contains(x.StockRef))
                .GroupBy(x => x.StockRef)
                .Select(g => g.First())
                .ToList();

            if (addSell.Count == 0)
            {
                return Json(new { hasAddSell = false });
            }

            string html = RenderPartialViewToString("~/Views/Shared/YouMayAlsoNeed.cshtml", addSell);

            return Json(new { hasAddSell = true, html = html });
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

                ViewBag.OrderIsOnHold = false;
                if (model.BasketContents.Find(x => x.PartNo == "ONHOLD") != null)
                {
                    ViewBag.OrderIsOnHold = true;
                }

                if (ConfigurationManager.AppSettings["AmazonPayMerchantId"] != "OFF")
                {
                    MyAmazonPay myAmazonPay = new MyAmazonPay();
                    myAmazonPay.GetButton();
                    model.AmazonButtonJSONPayLoad = myAmazonPay.AmazonButtonJSONPayLoad;
                    model.AmazonButtonSignature = myAmazonPay.AmazonButtonSignature;
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