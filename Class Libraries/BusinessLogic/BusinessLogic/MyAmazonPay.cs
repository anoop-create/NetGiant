using BusinessLogic.ViewModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Configuration;
using System;
using System.Linq;
using System.Web;
using VMerchantWrapper.Entities;
using System.Net;
using System.Threading.Tasks;
using DataAccess.EntityFramework;
using Amazon.Pay.API;
using Amazon.Pay.API.Types;
using Amazon.Pay.API.WebStore;
using Amazon.Pay.API.WebStore.CheckoutSession;
using Amazon.Pay.API.WebStore.Types;
using System.Data;
using System.Web.Security;
using System.Text;

namespace BusinessLogic
{
    public class MyAmazonPay
    {
        private WebStoreClient MyClient { get; set; }
        public string AmazonButtonJSONPayLoad { get; set; }
        public string AmazonButtonSignature { get; set; }
        public CheckoutSessionResponse AmazonSession { get; set; }
        public bool IsError { get; set; } = false;
        public string ErrorMessage { get; set; }
        public string AmazonPayRedirectUrl { get; set; }
        public decimal AmazonPayTotalAmount { get; set; }
        public string AmazonPayShippingPostcode { get; set; }
        public string AmazonBillingAddress { get; set; }
        public string AmazonShippingAddress { get; set; }
        public string AmazonPaymentMethod { get; set; }
        private string mySummaryURL { get; set; }
        private string myCompleteURL { get; set; }
        private string myStoreID { get; set; }


        public MyAmazonPay()
        {
            string myPublicKey = ConfigurationManager.AppSettings["AmazonPayPublicKeyId"];
            string myPrivateKey = ConfigurationManager.AppSettings["AmazonPayPrivateKey"];

            try
            {
                ApiConfiguration payConfiguration = new ApiConfiguration
                (
                    region: Region.Europe,
                    environment: Amazon.Pay.API.Types.Environment.Sandbox,
                    publicKeyId: myPublicKey,
                    privateKey: myPrivateKey
                );

                MyClient = new WebStoreClient(payConfiguration);
            }
            catch(Exception e)
            {
                Utilities.LogInformationMessage("Error with Amazon MyClient - check Amazon settings");
            }
        }

        public void GetButton()
        {
            CheckoutViewModel myCheckoutViewModel = new CheckoutViewModel();

            CreateReturnUrls();

            CreateCheckoutSessionRequest request = new CreateCheckoutSessionRequest
            (
                checkoutReviewReturnUrl: mySummaryURL,
                storeId: myStoreID
            );

            request.DeliverySpecifications.AddressRestrictions.Type = RestrictionType.Allowed;
            request.DeliverySpecifications.AddressRestrictions.AddCountryRestriction("GB");

            if (MyClient != null)
            {
                AmazonButtonSignature = MyClient.GenerateButtonSignature(request);
                AmazonButtonJSONPayLoad = request.ToJson();
            }
        }

        public void GetCheckoutSession(string ID)
        {
            AmazonSession = MyClient.GetCheckoutSession(ID);
            if (!AmazonSession.Success)
            {
                IsError = true;
                ErrorMessage = "Error retrieving the Amazon Session";
                Utilities.LogInformationMessage("MyAmazonPay-GetCheckoutSession - retrieving the Amazon session failed.");
                return;
            }
        }

        public void FillDeliveryOptions(CheckoutViewModel model)
        {
            model.DeliveryOptions = CheckoutViewModel.RetrieveDeliveryOptions(AmazonSession.ShippingAddress.PostalCode);
            model.CheckoutDetails.DeliveryServiceId = model.DeliveryOptions.First().DeliveryServiceId;
        }

        private void CreateReturnUrls()
        {
            string myReturnURL = "";
            Uri myContext = HttpContext.Current.Request.Url;

            if (ConfigurationManager.AppSettings["Environment"] != "Live")
            {
                //If not live we can get http or https from the request
                myReturnURL =
                    myContext.Scheme
                    + "://"
                    + myContext.Authority;
            }
            else
            {
                //if live the load balancers hide the https - we can still the get domain name though
                myReturnURL =
                    "https://" + myContext.Authority;
            }

            //the URL after logging in to Amazon
            mySummaryURL = myReturnURL + "/Checkout/AmazonPaySummary";
            //in pay summary the "place order" button... goes to UpdateAmazon on this page. this tells Amazon the 
            //amount we are trying to take. Then we go to the AmazonPayRedirectUrl (ie Amazon) who give us the money.
            //Amazon then respone.redirects to myCompleteURL
            myCompleteURL = myReturnURL + "/Checkout/AmazonPayNotification";

            myStoreID = ConfigurationManager.AppSettings["AmazonPayStoreId"];
        }

        public void FillVariables(CheckoutViewModel model)
        {
            //ie Visa Mastercard.
            model.AmazonPaymentMethod = AmazonSession.PaymentPreferences[0].PaymentDescriptor;
            //customer's SHIPPING postcode for checking delivery options
            AmazonPayShippingPostcode = AmazonSession.ShippingAddress.PostalCode;

            //for display on AmazonPaySummary - billing
            StringBuilder qwe = new StringBuilder();
            if (!string.IsNullOrEmpty(AmazonSession.BillingAddress.Name))
                qwe.Append(AmazonSession.BillingAddress.Name + "<br />");
            if (!string.IsNullOrEmpty(AmazonSession.BillingAddress.AddressLine1))
                qwe.Append(AmazonSession.BillingAddress.AddressLine1 + "<br />");
            if (!string.IsNullOrEmpty(AmazonSession.BillingAddress.AddressLine2))
                qwe.Append(AmazonSession.BillingAddress.AddressLine2 + "<br />");
            if (!string.IsNullOrEmpty(AmazonSession.BillingAddress.AddressLine3))
                qwe.Append(AmazonSession.BillingAddress.AddressLine2 + "<br />");
            if (!string.IsNullOrEmpty(AmazonSession.BillingAddress.City))
                qwe.Append(AmazonSession.BillingAddress.City + "<br />");
            if (!string.IsNullOrEmpty(AmazonSession.BillingAddress.County))
                qwe.Append(AmazonSession.BillingAddress.County + "<br />");

            qwe.Append(AmazonSession.BillingAddress.PostalCode);
            model.AmazonBillingAddress = qwe.ToString();

            //for display on AmazonPaySummary - shipping
            qwe = new StringBuilder();
            if (!string.IsNullOrEmpty(AmazonSession.ShippingAddress.Name))
                qwe.Append(AmazonSession.ShippingAddress.Name + "<br />");
            if (!string.IsNullOrEmpty(AmazonSession.ShippingAddress.AddressLine1))
                qwe.Append(AmazonSession.ShippingAddress.AddressLine1 + "<br />");
            if (!string.IsNullOrEmpty(AmazonSession.ShippingAddress.AddressLine2))
                qwe.Append(AmazonSession.ShippingAddress.AddressLine2 + "<br />");
            if (!string.IsNullOrEmpty(AmazonSession.ShippingAddress.AddressLine3))
                qwe.Append(AmazonSession.ShippingAddress.AddressLine2 + "<br />");
            if (!string.IsNullOrEmpty(AmazonSession.ShippingAddress.City))
                qwe.Append(AmazonSession.ShippingAddress.City + "<br />");
            if (!string.IsNullOrEmpty(AmazonSession.ShippingAddress.County))
                qwe.Append(AmazonSession.ShippingAddress.County + "<br />");

            qwe.Append(AmazonSession.ShippingAddress.PostalCode);
            model.AmazonShippingAddress = qwe.ToString();
        }

        public SaveReturn UpdateAmazon(string ID, CheckoutViewModel model)
        {
            CreateReturnUrls();
            SaveReturn sr = new SaveReturn();

            CheckoutSessionResponse mySession = MyClient.GetCheckoutSession(ID);
            if(mySession.Success == false)
            {
                sr.IsSuccess = false;
                return sr;
            }

            //if we are here we have got the session from Amazon
            //now we UPDATE amazon with the details
            UpdateCheckoutSessionRequest myRequest = new UpdateCheckoutSessionRequest();
            myRequest.PaymentDetails.ChargeAmount.Amount = Convert.ToDecimal(model.BasketTotals.GrandTotalIncVat);
            myRequest.PaymentDetails.ChargeAmount.CurrencyCode = Currency.GBP;
            myRequest.WebCheckoutDetails.CheckoutResultReturnUrl = myCompleteURL;
            myRequest.PaymentDetails.PaymentIntent = PaymentIntent.AuthorizeWithCapture;
            myRequest.MerchantMetadata.MerchantReferenceId = DateTime.Now.ToString("yyyy-MM-dd~HH:mm:ss");
            myRequest.MerchantMetadata.MerchantStoreName = model.CommonData["SiteName"];
            myRequest.MerchantMetadata.NoteToBuyer = "Thank you for shopping with " + model.CommonData["SiteName"];
            myRequest.PaymentDetails.CanHandlePendingAuthorization = false;
            myRequest.ChargePermissionType = ChargePermissionType.OneTime;

            var myResult = MyClient.UpdateCheckoutSession(ID, myRequest);
            if(myResult.Success == false)
            {
                sr.IsSuccess = false;
                return sr;
            }

            //hopefully Amazon has the right prices etc. Now to get payment.
            //we return a positive sr.IsSuccess and the controller should redirect to Amazon...
            model.AmazonPayRedirectUrl = myResult.WebCheckoutDetails.AmazonPayRedirectUrl;  
            sr.IsSuccess = true;
            return sr;
        }

        public void CompleteCheckoutSession(string CheckoutSessionID, decimal Amount, CheckoutViewModel model)
        {
            GetCheckoutSession(CheckoutSessionID);
            WriteLog(CheckoutSessionID, "AUTHORIZE", JsonConvert.SerializeObject(AmazonSession));

            //fix B_BasketTotals VAT before passing to Axis
            BasketTotals basketTotals = HttpContext.Current.Session["B_BasketTotals"] as BasketTotals;
            bool isVatExempt = Convert.ToBoolean(HttpContext.Current.Session["D_IsVatExempt"]);

            if (isVatExempt)
            {
                basketTotals.Voucher = basketTotals.Voucher + basketTotals.VoucherVat;
                basketTotals.VoucherVat = 0;
            }

            HttpContext.Current.Session["B_BasketTotals"] = basketTotals;

            //checks to see if cancelled-failed etc
            if (
                AmazonSession.StatusDetails.ReasonCode == "BuyerCanceled"
                ||
                AmazonSession.StatusDetails.ReasonCode == "BuyerCancelled"
                )
            {
                IsError = true;
                ErrorMessage = "Buyer Cancelled";
                //Utilities.LogInformationMessage("MyAmazonPay-CompleteCheckoutSession - Amazon returned 'Buyer Cancelled'.");
                return;
            }
            if (AmazonSession.StatusDetails.ReasonCode == "Declined")
            {
                IsError = true;
                ErrorMessage = "Declined By Amazon";
                //Utilities.LogInformationMessage("MyAmazonPay-CompleteCheckoutSession - Amazon returned 'Declined'.");
                return;
            }
            if (Amount != AmazonSession.PaymentDetails.ChargeAmount.Amount)
            {
                IsError = true;
                ErrorMessage = "Error - mismatched prices";
                Utilities.LogInformationMessage("MyAmazonPay-CompleteCheckoutSession - Our basket and Amazon's amount to pay do not match.");
                return;
            }

            //if we've got here we can fill the page variables
            FillVariables(model);

            var myCheckoutDetails = new CheckoutDetails();

            string[] nameArray = AmazonSession.Buyer.Name.Split();
            string firstName = "";
            for (int i = 0; i < nameArray.Length - 1; i++)
            {
                firstName += nameArray[i] + " ";
            }
            firstName = firstName.Trim();
            string lastName = nameArray[nameArray.Length - 1];

            Name myName = new Name();
            myName.Title = "-";
            myName.Firstname = string.IsNullOrEmpty(firstName) ? "-" : firstName;
            myName.Firstname = myName.Firstname.Truncate(20);
            myName.Surname = lastName.Truncate(20);
            myCheckoutDetails.Name = myName;

            string[] nameArray2 = AmazonSession.ShippingAddress.Name.Split();
            firstName = "";
            for (int i = 0; i < nameArray2.Length - 1; i++)
            {
                firstName += nameArray2[i] + " ";
            }
            firstName = firstName.Trim();
            lastName = nameArray2[nameArray2.Length - 1];

            myName = new Name();
            myName.Title = "-";
            myName.Firstname = string.IsNullOrEmpty(firstName) ? "-" : firstName;
            myName.Firstname = myName.Firstname.Truncate(20);
            myName.Surname = lastName.Truncate(20);
            myCheckoutDetails.RecipientName = myName;

            Address billAddress = new Address
            {
                PostCode = AmazonSession.BillingAddress.PostalCode,
                Line1 = "",
                Line2 = string.IsNullOrEmpty(AmazonSession.BillingAddress.AddressLine1) ? "-" : AmazonSession.BillingAddress.AddressLine1.Truncate(30),
                Line3 = string.IsNullOrEmpty(AmazonSession.BillingAddress.AddressLine2) ? "" : AmazonSession.BillingAddress.AddressLine2.Truncate(30),
                Line4 = AmazonSession.BillingAddress.City.Truncate(30),
                Line5 = AmazonSession.BillingAddress.County.Truncate(30)
            };
            Address shipAddress = new Address
            {
                PostCode = AmazonSession.ShippingAddress.PostalCode,
                Line1 = "",
                Line2 = string.IsNullOrEmpty(AmazonSession.ShippingAddress.AddressLine1) ? "-" : AmazonSession.ShippingAddress.AddressLine1.Truncate(30),
                Line3 = string.IsNullOrEmpty(AmazonSession.ShippingAddress.AddressLine2) ? "" : AmazonSession.ShippingAddress.AddressLine2.Truncate(30),
                Line4 = AmazonSession.ShippingAddress.City.Truncate(30),
                Line5 = AmazonSession.ShippingAddress.County.Truncate(30)
            };

            //NOTE! 
            //Line 1 REQUIRED - NAME??
            //Line 2 REQUIRED - Street Address
            //Line 3 can be blank/null
            //Line 4 REQUIRED - Town
            //Line 5 can be blank/null - county

            myCheckoutDetails.BillingAddress = billAddress;
            myCheckoutDetails.DeliveryAddress = shipAddress;
            myCheckoutDetails.TelephoneNumber = string.IsNullOrEmpty(AmazonSession.Buyer.PhoneNumber) ? "0" : AmazonSession.Buyer.PhoneNumber;
            myCheckoutDetails.Email = AmazonSession.Buyer.Email;
            myCheckoutDetails.DeliveryServiceId = CheckoutViewModel.RetrieveDeliveryOptions(AmazonSession.ShippingAddress.PostalCode).First().DeliveryServiceId;
            myCheckoutDetails.PaymentMethod = "AmazonPay";
            myCheckoutDetails.Password = Membership.GeneratePassword(16, 4);
            myCheckoutDetails.Newsletter = false;
            myCheckoutDetails.AccountNumber = "";

            if (HttpContext.Current.Session["B_VoucherCode"] != null)
            {
                myCheckoutDetails.VoucherCode = HttpContext.Current.Session["B_VoucherCode"].ToString();
            }
            CheckoutViewModel.AddVoucherToBasket();

            //we have called Amazon to get the Auth
            //We have saved amazon's response to the log file
            //we have populated myCheckoutDetails with info from the auth call to amazon
            //BEFORE we save mycheckoutdetails to Axis we are going to COMPLETE on amazon
            //and add the SO2 number in this last response to mycheckoutdetails

            CompleteCheckoutSessionRequest request = new CompleteCheckoutSessionRequest();
            request.ChargeAmount.Amount = Amount;
            request.ChargeAmount.CurrencyCode = Currency.GBP;

            CheckoutSessionResponse result = MyClient.CompleteCheckoutSession(CheckoutSessionID, request);
            if (result.Success)
            {
                WriteLog(CheckoutSessionID, "CAPTURE", JsonConvert.SerializeObject(result));
                myCheckoutDetails.PayPalRef = result.ChargeId;
            }
            else
            {
                IsError = true;
                ErrorMessage = "Error - Amazon failed to complete - please try again.";
                Utilities.LogInformationMessage("MyAmazonPay-CompleteCheckoutSession - request to Amazon to complete the payment failed.");

                // Possible bug here, if failure then surely we should return with Error.
            }

            // we have GOT the payment...hopefully
            // Save User to Axis API

            var user = Touchpoints.GetUserData("", myCheckoutDetails.Email, "", true);
            if (user.Rows.Count > 0)
            {
                if (user.Rows[0]["password"].ToString() == "")
                {
                    MyAccountViewModel.SetNewPassword(myCheckoutDetails.Email, myCheckoutDetails.Password);
                }
                else
                {
                    myCheckoutDetails.Password = user.Rows[0]["password"].ToString();
                }
                myCheckoutDetails.Newsletter = Convert.ToBoolean(user.Rows[0]["isOnMailingList"] != DBNull.Value ? user.Rows[0]["isOnMailingList"] : false);
                myCheckoutDetails.AccountNumber = user.Rows[0]["account"].ToString();
            }
            else
            {
                var signUp = new SignUp
                {
                    Address = myCheckoutDetails.BillingAddress,
                    Name = myCheckoutDetails.Name,
                    Newsletter = false,
                    Password = myCheckoutDetails.Password,
                    TelNumber = myCheckoutDetails.TelephoneNumber,
                    UserName = myCheckoutDetails.Email
                };
                SaveReturn createUser = Touchpoints.SaveUser(signUp);
                if (!createUser.IsSuccess)
                {
                    //ERROR saving to Axis
                    IsError = true;
                    ErrorMessage = "Error saving the user details";
                    Utilities.LogInformationMessage("MyAmazonPay-CompleteCheckoutSession - Error saving the user details to Axis via Touchpoints.");
                }
                // Make sure the InterimOrder flag is carried accross
                myCheckoutDetails.IsInterimOrder = createUser.Message == "IsInterimOrder";

                myCheckoutDetails.Password = Membership.GeneratePassword(16, 4);
                myCheckoutDetails.AccountNumber = "";
            }

            SaveReturn saveOrder = Touchpoints.SaveOrder(myCheckoutDetails, OrderStatus.Completed, null);
            if (!saveOrder.IsSuccess)
            {
                //ERROR saving to Axis
                IsError = true;
                ErrorMessage = "Error saving the order";
                Utilities.LogInformationMessage("MyAmazonPay-CompleteCheckoutSession - Error saving the order to Axis via Touchpoints.");
            }

            HttpContext.Current.Session["C_CheckoutDetails"] = myCheckoutDetails;
        }

        private void WriteLog(string CheckoutSessionID, string Action, string JSON)
        {
            AmazonPayLog myAmazonPayLog = new AmazonPayLog();
            myAmazonPayLog.CheckoutSessionId = CheckoutSessionID;
            myAmazonPayLog.DateTime = DateTime.Now;
            myAmazonPayLog.Action = Action;
            myAmazonPayLog.WebsiteFk = Convert.ToInt32(ConfigurationManager.AppSettings["WebsiteId"]);

            JObject myObject = JObject.Parse(JSON);
            myObject.Remove("RawRequest");
            myObject.Remove("RawResponse");
            myAmazonPayLog.Json = JsonConvert.SerializeObject(myObject);

            EntityAccess.InsertAmazonPayLogEntry(myAmazonPayLog);
        }
    }
}
