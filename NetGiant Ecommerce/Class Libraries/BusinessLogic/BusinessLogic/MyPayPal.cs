using BusinessLogic.ViewModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PayPalCheckoutSdk.Core;
using PayPalCheckoutSdk.Payments;
using PayPalHttp;
using RestSharp;
using RestSharp.Authenticators;
using System.Collections.Generic;
using System.Configuration;
using System;
using System.Linq;
using System.Web;
using VMerchantWrapper.Entities;
using System.Net;
using System.Threading.Tasks;
using DataAccess.EntityFramework;
using System.Text;
using System.IO;

namespace BusinessLogic
{
    public class MyPayPal
    {
        public static async Task<SaveReturn> Capture(string trans, string paypalType, string randomPassword, BasketTotals bt)
        {
            SaveReturn sr = new SaveReturn();
            Dictionary<string, string> commonData = DataCache.GetSectionData("CommonData");
            JObject transaction = JsonConvert.DeserializeObject<JObject>(trans);
            var checkoutData = DataCache.GetSectionData("CheckoutData");

            //fix B_BasketTotals VAT before passing to Axis
            BasketTotals basketTotals = HttpContext.Current.Session["B_BasketTotals"] as BasketTotals;
            bool isVatExempt = Convert.ToBoolean(HttpContext.Current.Session["D_IsVatExempt"]);

            if (isVatExempt)
            {
                basketTotals.Voucher = basketTotals.Voucher + basketTotals.VoucherVat;
                basketTotals.VoucherVat = 0;
            }

            HttpContext.Current.Session["B_BasketTotals"] = basketTotals;

            // Check for failed transaction
            sr.IsSuccess = true;
            if (transaction["status"].ToString() != "COMPLETED")
            {
                sr.IsSuccess = false;
                sr.Message = "PayPal transaction failed";
                return sr;
            }

            string paypalAuthId = transaction["purchase_units"].First()["payments"]["authorizations"].First()["id"].ToString();
            WritePayPalLog("AUTHORIZE", paypalAuthId, trans);

            // Check for valid Post Code
            string postCode = transaction["purchase_units"].First()["shipping"]["address"]["postal_code"].ToString();
            if (CheckoutViewModel.InvalidPostCodeCheck(postCode))
            {
                sr.Message = "We are currently unable to deliver to your postcode " + postCode;
                sr.IsSuccess = false;
                return sr;
            }

            //find the selected shipping option from the JSON
            int deliveryServiceId = 0;

            string paypalRecipientName = transaction["purchase_units"].First()["shipping"]["name"]["full_name"].ToString().Trim();
            if (transaction["purchase_units"].First()["shipping"]["options"] == null)
            {
                sr = await CancelPayPalAuth(paypalAuthId, paypalRecipientName);
                sr.IsSuccess = false;
                return sr;
            }

            foreach (JToken x in transaction["purchase_units"].First()["shipping"]["options"])
            {
                if (Convert.ToBoolean(x["selected"]) == true)
                {
                    deliveryServiceId = Convert.ToInt32(x["id"]);
                }
            }

            // Add Delivery to basket
            Basket.ProcessDelivery(deliveryServiceId);

            // Check that order amount matches
            string paypalAmount = transaction["purchase_units"].First()["payments"]["authorizations"].First()["amount"]["value"].ToString();
            decimal amt = Decimal.Parse(paypalAmount);
            if (amt != bt.GrandTotalIncVat)
            {
                sr.Message = "There was a problem processing your PayPal payment. Please try again";
                sr.IsSuccess = false;
                return sr;
            }

            string paymentTitle = transaction["payer"]["name"]["title"]?.ToString() ?? "-";
            string paymentFirstname = transaction["payer"]["name"]["given_name"].ToString().Trim();
            string paymentSurname = transaction["payer"]["name"]["surname"].ToString().Trim();
            string telno = transaction["payer"]["phone"]?["phone_number"]["national_number"].ToString() ?? "0";
            string email = transaction["payer"]["email_address"].ToString();
            Address address = new Address
            {
                Country = transaction["purchase_units"].First()["shipping"]["address"]["country_code"].ToString().Truncate(2),
                Line1 = "",
                Line2 = transaction["purchase_units"].First()["shipping"]["address"]["address_line_1"]?.ToString().Truncate(30) ?? "",
                Line3 = transaction["purchase_units"].First()["shipping"]["address"]["address_line_2"]?.ToString().Truncate(30) ?? "",
                Line4 = transaction["purchase_units"].First()["shipping"]["address"]["admin_area_2"]?.ToString().Truncate(30) ?? "",
                Line5 = transaction["purchase_units"].First()["shipping"]["address"]["admin_area_1"]?.ToString().Truncate(30) ?? "",
                PostCode = postCode.Truncate(30)
            };
            string paypalOrderId = transaction["id"].ToString();

            string recipientFirstname;
            string recipientSurname;

            var nameParts = paypalRecipientName.Split(' ');
            if (nameParts.Length == 2 || (nameParts.Length == 3 && String.IsNullOrEmpty(nameParts[2])))
            {
                recipientFirstname = nameParts[0];
                recipientSurname = nameParts[1];
            }
            else if (nameParts.Length >= 3)
            {
                recipientSurname = nameParts[nameParts.Length - 1];
                recipientFirstname = string.Join(" ", nameParts.Take(nameParts.Length - 1));
            }
            else
            {
                recipientFirstname = paymentFirstname;
                recipientSurname = paymentSurname;
            }

            var cd = new CheckoutDetails
            {
                Name = new Name
                {
                    Title = paymentTitle.Truncate(20),
                    Firstname = paymentFirstname.Truncate(20),
                    Surname = paymentSurname.Truncate(20)
                },
                RecipientName = new Name
                {
                    Title = paymentTitle.Truncate(20),
                    Firstname = recipientFirstname.Truncate(20),
                    Surname = recipientSurname.Truncate(20)
                },
                BillingAddress = address,
                DeliveryAddress = address,
                TelephoneNumber = telno,
                Email = email.Truncate(50),
                DeliveryServiceId = deliveryServiceId,
                PaymentMethod = "PayPal"
            };

            // Save User to Axis API
            var user = Touchpoints.GetUserData("", cd.Email, "", true);
            if (user.Rows.Count > 0)
            {
                if (user.Rows[0]["password"].ToString() == "")
                {
                    MyAccountViewModel.SetNewPassword(cd.Email, randomPassword);
                    cd.Password = randomPassword;
                }
                else
                {
                    cd.Password = user.Rows[0]["password"].ToString();
                }
                cd.Newsletter = Convert.ToBoolean(user.Rows[0]["isOnMailingList"] != DBNull.Value ? user.Rows[0]["isOnMailingList"] : false);
                cd.AccountNumber = user.Rows[0]["account"].ToString();
            }
            else
            {
                var signUp = new SignUp
                {
                    Address = cd.BillingAddress,
                    Name = cd.Name,
                    Newsletter = false,
                    Password = randomPassword,
                    TelNumber = cd.TelephoneNumber,
                    UserName = cd.Email
                };
                var createUser = Touchpoints.SaveUser(signUp);
                if (!createUser.IsSuccess)
                {
                    // Error, Cancel the PayPal Authorisation
                    createUser = await CancelPayPalAuth(paypalAuthId, paypalRecipientName);
                    createUser.IsSuccess = false;
                    return createUser;
                }
                // Make sure the InterimOrder flag is carried accross
                cd.IsInterimOrder = createUser.Message == "IsInterimOrder";

                cd.Password = randomPassword;
                cd.AccountNumber = "";
            }

            // Add Voucher to BasketContents
            if (HttpContext.Current.Session["B_VoucherCode"] != null)
            {
                cd.VoucherCode = HttpContext.Current.Session["B_VoucherCode"].ToString();
            }
            CheckoutViewModel.AddVoucherToBasket();

            // Save Order to Axis API
            cd.PayPalRef = paypalAuthId;

            var createOrder = Touchpoints.SaveOrder(cd, OrderStatus.Completed, null);
            if (!createOrder.IsSuccess)
            {
                // Error, Cancel the PayPal Authorisation
                createOrder = await CancelPayPalAuth(paypalAuthId, paypalRecipientName);
                createOrder.IsSuccess = false;
                return createOrder;
            }

            // Capture the payment
            HttpContext.Current.Session["C_CheckoutDetails"] = cd;
            try
            {
                Capture result = await CaptureAuthorisation(paypalAuthId, ConfigurationManager.AppSettings["Environment"] == "Live" ? false : true);
                if (result.Status != "COMPLETED")
                {
                    Utilities.LogInformationMessage("Unable to capture PayPal payment for : " + paypalRecipientName + " reference: " + paypalOrderId + "-" + paypalAuthId);
                    sr.IsSuccess = false;
                    sr.Message = "There was a problem completing your PayPal transaction. Please contact customer services on " + commonData["TelephoneNumber"];
                }

                MyAccountViewModel.CreateCustomer(cd);
            }
            catch (Exception e)
            {
                Utilities.ProcessException(e);
            }

            return sr;
        }

        private static async Task<SaveReturn> CancelPayPalAuth(string paypalAuthId, string paypalRecipientName)
        {
            SaveReturn sr = new SaveReturn();
            Dictionary<string, string> commonData = DataCache.GetSectionData("CommonData");
            bool isCancelled = await CancelAuthorisation(paypalAuthId, ConfigurationManager.AppSettings["Environment"] == "Live" ? false : true);
            if (isCancelled)
            {
                sr.Message = "Transaction failed. Your PayPal transaction was cancelled ";
                sr.IsSuccess = true;
            }
            else
            {
                sr.Message = "There was a problem completing your PayPal transaction. Please contact customer services on " + commonData["TelephoneNumber"];
                sr.IsSuccess = false;
            }
            Utilities.LogInformationMessage("Unable to SaveOrder after successfully processing PayPal transaction. Customer Name: " + paypalRecipientName);

            return sr;
        }

        private static PayPalEnvironment environment()
        {
            if (ConfigurationManager.AppSettings["PayPalMode"] != "sandbox")
            {
                return new PayPalEnvironment(
                    ConfigurationManager.AppSettings["PayPalClientId"],
                    ConfigurationManager.AppSettings["PayPalSecret"],
                    "https://api-m.paypal.com",
                    "https://www.paypal.com");
            }
            else
            {
                return new SandboxEnvironment(
                    ConfigurationManager.AppSettings["PayPalClientId"],
                    ConfigurationManager.AppSettings["PayPalSecret"]);
            }
        }

        private static HttpClient client()
        {
            return new PayPalHttpClient(environment());
        }

        private static HttpClient client(string refreshToken)
        {
            return new PayPalHttpClient(environment(), refreshToken);
        }

        private static async Task<Capture> CaptureAuthorisation(string authId, bool debug = false)
        {
            Utilities.SetTlsVersion();
            Capture result = new Capture();

            try
            {
                AuthorizationsCaptureRequest request = new AuthorizationsCaptureRequest(authId);

                request.Prefer("return=representation");
                request.RequestBody(new CaptureRequest());

                var response = await client().Execute(request);
                result = response.Result<Capture>();
            }
            catch (Exception e)
            {
                Utilities.ProcessException(e);
            }

            WritePayPalLog("CAPTURE", authId, JsonConvert.SerializeObject(result));
            return result;
        }

        private static async Task<bool> CancelAuthorisation(string authId, bool debug = false)
        {
            Utilities.SetTlsVersion();
            bool status = false;

            try
            {
                AuthorizationsVoidRequest request = new AuthorizationsVoidRequest(authId);

                var response = await client().Execute(request);
                status = response.StatusCode == HttpStatusCode.NoContent;
            }
            catch (Exception e)
            {
                Utilities.ProcessException(e);
            }

            WritePayPalLog("CANCEL", authId, status == true ? "{\"status\":\"SUCCESS\"}" : "{\"status\":\"FAIL\"}");
            return status;
        }

        private static void WritePayPalLog(string action, string authId, string response)
        {
            PayPalLog log = new PayPalLog()
            {
                DateTime = DateTime.Now,
                Action = action,
                AuthId = authId,
                Response = response
            };

            EntityAccess.InsertPayPalLogEntry(log);
        }

        public void DeliveryOptions(string payPalID, string updateDeliveryOptionsJSON)
        {
            string url = "v2/checkout/orders/" + payPalID;
            PatchOrder(url, updateDeliveryOptionsJSON);
        }

        public string CreateDeliveryOptionsJSON(string PostCode, string SelectedID, CheckoutViewModel model)
        {
            //create ALL shipping options, which incedentally sets the VAT status
            List<deliveryService> deliveryServices = CheckoutViewModel.RetrieveDeliveryOptions(PostCode);

            List<PPJson.ShippingValue> shippingValueList = new List<PPJson.ShippingValue>();

            IsVATExempt = Convert.ToBoolean(HttpContext.Current.Session["D_IsVatExempt"]);

            for (int i = 0; i < deliveryServices.Count; i++)
            {
                PPJson.ShippingValue x = new PPJson.ShippingValue();
                x = CreateShippingValue
                    (
                        deliveryServices[i].Price,
                        Convert.ToString(deliveryServices[i].DeliveryServiceId),
                        deliveryServices[i].ServiceName
                    );
                shippingValueList.Add(x);
            }

            //set the selected shipping option
            bool shippingTrueIsSet = false;
            for (int i = 0; i < shippingValueList.Count; i++)
            {
                if (shippingValueList[i].id == SelectedID)
                {
                    //PP's JSON selected shipping id is the same as THIS id so mark it selected true
                    shippingValueList[i].selected = true;
                    //we don't need to set any other shipping options to true
                    shippingTrueIsSet = true;
                }
            }
            if (!shippingTrueIsSet)
            {
                //we never set ANY selected shipping option true and PP needs at least one setting, set the first one
                shippingValueList[0].selected = true;
                //we need to know the selected ID later, and its not necessarily the same as was sent initially
                SelectedID = Convert.ToString(shippingValueList[0].id);
            }

            //sort out ALL the final values
            decimal shippingCost = 0.00M;
            for (int i = 0; i < shippingValueList.Count; i++)
            {
                if (shippingValueList[i].selected == true)
                {
                    //shippingCost VAT is sorted by CheckoutViewModel.RetrieveDeliveryOption :)
                    //the selected shipping option's value is required for the totals
                    shippingCost = Convert.ToDecimal(shippingValueList[i].amount.value);
                }
            }

            decimal itemsCost = Basket.GetBasketTotal(model.BasketContents);

            //decimal itemsCost = model.BasketTotals.GrandTotalIncVat;
            //if (IsVATExempt)
            //{
            //    itemsCost = model.BasketTotals.GrandTotalExcVat;
            //}

            decimal totalCost = shippingCost + itemsCost;

            //we have the shipping list, now to add the "top" bit
            PPJson.Shipping shipping = new PPJson.Shipping();
            shipping.op = "add";
            shipping.path = "/purchase_units/@reference_id=='default'/shipping/options";
            shipping.value = shippingValueList.ToArray();

            //shipping all done, now the order details
            PPJson.OrderDetailsItemTotal orderDetailsItemTotal = new PPJson.OrderDetailsItemTotal();
            orderDetailsItemTotal.currency_code = "GBP";
            orderDetailsItemTotal.value = Convert.ToString(itemsCost);

            PPJson.OrderDetailsShipping orderDetailsShipping = new PPJson.OrderDetailsShipping();
            orderDetailsShipping.currency_code = "GBP";
            orderDetailsShipping.value = Convert.ToString(shippingCost);

            PPJson.OrderDetailBreakDown orderDetailBreakDown = new PPJson.OrderDetailBreakDown();
            orderDetailBreakDown.item_total = orderDetailsItemTotal;
            orderDetailBreakDown.shipping = orderDetailsShipping;

            PPJson.OrderDetailValue orderDetailValue = new PPJson.OrderDetailValue();
            orderDetailValue.breakdown = orderDetailBreakDown;
            orderDetailValue.currency_code = "GBP";
            orderDetailValue.value = Convert.ToString(totalCost);

            PPJson.OrderDetail orderDetail = new PPJson.OrderDetail();
            orderDetail.op = "replace";
            orderDetail.path = "/purchase_units/@reference_id=='default'/amount";
            orderDetail.value = orderDetailValue;

            //combine the whole lot into an object and turn it into JSON
            object[] wholeWrapper = new object[] { shipping, orderDetail };

            return JsonConvert.SerializeObject(wholeWrapper);
        }

        public PPJson.ShippingValue CreateShippingValue(decimal shippingAmountValue, string shippingID, string shippingLabel)
        {
            //we're building one of possibly several shipping options
            PPJson.ShippingAmount amount = new PPJson.ShippingAmount();
            amount.currency_code = "GBP";
            amount.value = Convert.ToString(shippingAmountValue);
            if (!IsVATExempt)
            {
                shippingAmountValue = Math.Round(shippingAmountValue * Convert.ToDecimal(ConfigurationManager.AppSettings["VatMultiplier"]), 2);
                amount.value = Convert.ToString(shippingAmountValue);
            }

            PPJson.ShippingValue value = new PPJson.ShippingValue();
            value.amount = amount;
            value.id = shippingID;
            value.label = shippingLabel;
            value.selected = false;
            value.type = "SHIPPING";

            return value;
        }

        private string PatchOrder(string requestURL, string requestBody)
        {
            string accessToken = "Bearer " + DataCache.GetPayPalAccessToken();
            Utilities.SetTlsVersion();

            var client = new RestClient(ConfigurationManager.AppSettings["PayPalApiUrl"]);
            var request = new RestRequest(requestURL)
                .AddParameter("Authorization", accessToken, ParameterType.HttpHeader)
                .AddHeader("Content-Type", "application/json")
                .AddHeader("Accept", "application/json");
            request.AddJsonBody(requestBody);
            var response = client.Execute(request, RestSharp.Method.Patch);
            if (response.StatusCode != HttpStatusCode.NoContent)
            {
                Utilities.LogInformationMessage("PayPal Delivery Options Error (MyPayPal.cs) - " + response.StatusDescription + " requestBody: " + requestBody);
                return "";
            }
            return response.Content;
        }

        public string CreateOrderWithAddress(string json, CheckoutViewModel model)
        {
            CheckoutDetails details = new CheckoutDetails();
            Name name = new Name();
            Address address = new Address();
            AccountApplicationDetails accountApplicationDetails = new AccountApplicationDetails();

            CheckoutDetails sessionCheckoutDetails = HttpContext.Current.Session["C_CheckoutDetails"] as CheckoutDetails;
            BasketTotals sessionBasketTotals = HttpContext.Current.Session["B_BasketTotals"] as BasketTotals;

            JObject jObject = JObject.Parse(json);

            name.Firstname = jObject["CheckoutDetails_Name_Firstname"].ToString();
            name.Surname = jObject["CheckoutDetails_Name_Surname"].ToString();
            details.Name = name;
            name = new Name();
            name.Firstname = jObject["CheckoutDetails_RecipientName_Firstname"].ToString();
            name.Surname = jObject["CheckoutDetails_RecipientName_Surname"].ToString();
            details.RecipientName = name;
            address.Line1 = jObject["CheckoutDetails_DeliveryAddress_Line1"].ToString();
            address.Line2 = jObject["CheckoutDetails_DeliveryAddress_Line2"].ToString();
            address.Line3 = jObject["CheckoutDetails_DeliveryAddress_Line3"].ToString();
            address.Line4 = jObject["CheckoutDetails_DeliveryAddress_Line4"].ToString();
            address.Line5 = jObject["CheckoutDetails_DeliveryAddress_Line5"].ToString();
            address.PostCode = jObject["CheckoutDetails_DeliveryAddress_PostCode"].ToString();
            details.DeliveryAddress = address;
            details.TelephoneNumber = jObject["CheckoutDetails_TelephoneNumber"].ToString();
            details.Email = sessionCheckoutDetails.Email;
            accountApplicationDetails.CustomerType = Convert.ToInt16(jObject["AccountApplicationDetails_CustomerType"]);
            details.Password = jObject["CheckoutDetails_Password"].ToString();
            details.Reference = jObject["CheckoutDetails_Reference"].ToString();

            model.AccountApplicationDetails = accountApplicationDetails;
            model.CheckoutDetails = details;

            HttpContext.Current.Session["C_CheckoutDetails"] = details;

            //////////////////////////////////////////////////////////////////////////////// purchase units
            PPJson.PurchaseUnitsAmount amount = new PPJson.PurchaseUnitsAmount();
            amount.currency_code = "GBP";
            amount.value = sessionBasketTotals.GrandTotalIncVat.ToString();

            PPJson.PUShippingAddress pUShippingAddress = new PPJson.PUShippingAddress();
            pUShippingAddress.address_line_1 = address.Line2;
            pUShippingAddress.address_line_2 = address.Line3;
            pUShippingAddress.admin_area_2 = address.Line4;
            pUShippingAddress.admin_area_1 = address.Line5;
            pUShippingAddress.postal_code = address.PostCode;
            pUShippingAddress.country_code = "GB";

            PPJson.PUShippingName pUShippingName = new PPJson.PUShippingName();
            pUShippingName.full_name = details.RecipientName.Firstname + " " + details.RecipientName.Surname;

            PPJson.PUShipping pUShipping = new PPJson.PUShipping();
            pUShipping.address = pUShippingAddress;
            pUShipping.name = pUShippingName;

            PPJson.PurchaseUnits purchase_units_single = new PPJson.PurchaseUnits();
            purchase_units_single.amount = amount;
            purchase_units_single.shipping = pUShipping;

            PPJson.PurchaseUnits[] purchase_units = { purchase_units_single };

            ///////////////////////////////////////////////////////////////////////////////// payment source
            PPJson.PaymentSourcePayPalExperienceContext paymentSourcePayPalExperienceContext = new PPJson.PaymentSourcePayPalExperienceContext();
            paymentSourcePayPalExperienceContext.shipping_preference = "SET_PROVIDED_ADDRESS";

            PPJson.PaymentSourcePaypal paymentSourcePaypal = new PPJson.PaymentSourcePaypal();
            paymentSourcePaypal.experience_context = paymentSourcePayPalExperienceContext;

            PPJson.PaymentSource paymentSource = new PPJson.PaymentSource();
            paymentSource.paypal = paymentSourcePaypal;

            PPJson.root myRoot = new PPJson.root();
            myRoot.intent = "AUTHORIZE";
            myRoot.purchase_units = purchase_units;
            myRoot.payment_source = paymentSource;

            string requestBody = JsonConvert.SerializeObject(myRoot, Newtonsoft.Json.Formatting.Indented);
            string accessToken = "Bearer " + DataCache.GetPayPalAccessToken();

            Guid myGuid = Guid.NewGuid();
            string paypalRequestId = Convert.ToString(myGuid);

            var client = new RestClient(ConfigurationManager.AppSettings["PayPalApiUrl"]);
            var request = new RestRequest("v2/checkout/orders/", RestSharp.Method.Post)
                .AddParameter("Authorization", accessToken, ParameterType.HttpHeader)
                .AddHeader("Content-Type", "application/json")
                .AddHeader("Accept", "application/json")
                .AddHeader("PayPal-Request-Id", paypalRequestId);

            request.AddJsonBody(requestBody);
            var response = client.Execute(request, Method.Post);

            return response.Content.ToString();
        }


        public static async Task<SaveReturn> CaptureStage1(string trans)
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = true;

            JObject transaction = JsonConvert.DeserializeObject<JObject>(trans);

            CheckoutDetails sessionCheckoutDetails = HttpContext.Current.Session["C_CheckoutDetails"] as CheckoutDetails;
            BasketTotals sessionBasketTotals = HttpContext.Current.Session["B_BasketTotals"] as BasketTotals;

            // Check for failed transaction
            if (transaction["status"].ToString() != "COMPLETED")
            {
                sr.IsSuccess = false;
                sr.Message = "PayPal transaction failed";
                return sr;
            }

            string paypalAuthId = transaction["purchase_units"].First()["payments"]["authorizations"].First()["id"].ToString();
            WritePayPalLog("AUTHORIZE", paypalAuthId, trans);

            // Add Delivery to basket
            Basket.ProcessDelivery(sessionCheckoutDetails.DeliveryMethod);

            // Check that order amount matches
            string paypalAmount = transaction["purchase_units"].First()["payments"]["authorizations"].First()["amount"]["value"].ToString();
            decimal amt = Decimal.Parse(paypalAmount);
            if (amt != sessionBasketTotals.GrandTotalIncVat)
            {
                sr.Message = "There was a problem processing your PayPal payment. Please try again";
                sr.IsSuccess = false;
                return sr;
            }

            string paypalOrderId = transaction["id"].ToString();
            string RecipientName = sessionCheckoutDetails.Name.Firstname + " " + sessionCheckoutDetails.Name.Surname;

            sessionCheckoutDetails.PaymentMethod = "PayPal";

            // Save User to Axis API
            System.Data.DataTable user = Touchpoints.GetUserData("", sessionCheckoutDetails.Email, "", true);
            if (user.Rows.Count > 0)
            {
                if (user.Rows[0]["password"].ToString() == "")
                {
                    MyAccountViewModel.SetNewPassword(sessionCheckoutDetails.Email, sessionCheckoutDetails.Password);
                }
                else
                {
                    sessionCheckoutDetails.Password = user.Rows[0]["password"].ToString();
                }
                sessionCheckoutDetails.Newsletter = Convert.ToBoolean(user.Rows[0]["isOnMailingList"] != DBNull.Value ? user.Rows[0]["isOnMailingList"] : false);
                sessionCheckoutDetails.AccountNumber = user.Rows[0]["account"].ToString();
                Address existingAddress = new Address();
                existingAddress.Line1 = user.Rows[0]["Address1"].ToString();
                existingAddress.Line2 = user.Rows[0]["Address2"].ToString();
                existingAddress.Line3 = user.Rows[0]["Address3"].ToString();
                existingAddress.Line4 = user.Rows[0]["Address4"].ToString();
                existingAddress.Line5 = user.Rows[0]["Address5"].ToString();
                existingAddress.PostCode = user.Rows[0]["PostCode"].ToString();
                sessionCheckoutDetails.BillingAddress = existingAddress;
            }
            else
            {
                sessionCheckoutDetails.BillingAddress = sessionCheckoutDetails.DeliveryAddress;
                SignUp signUp = new SignUp
                {
                    Address = sessionCheckoutDetails.BillingAddress,
                    Name = sessionCheckoutDetails.Name,
                    Newsletter = false,
                    Password = sessionCheckoutDetails.Password,
                    TelNumber = sessionCheckoutDetails.TelephoneNumber,
                    UserName = sessionCheckoutDetails.Email
                };
                var createUser = Touchpoints.SaveUser(signUp);
                if (!createUser.IsSuccess)
                {
                    // Error, Cancel the PayPal Authorisation
                    createUser = await CancelPayPalAuth(paypalAuthId, RecipientName);
                    createUser.IsSuccess = false;
                    return createUser;
                }
                // Make sure the InterimOrder flag is carried accross
                sessionCheckoutDetails.IsInterimOrder = createUser.Message == "IsInterimOrder";

                sessionCheckoutDetails.Password = sessionCheckoutDetails.Password;
                sessionCheckoutDetails.AccountNumber = "";
            }

            // Add Voucher to BasketContents
            if (HttpContext.Current.Session["B_VoucherCode"] != null)
            {
                sessionCheckoutDetails.VoucherCode = HttpContext.Current.Session["B_VoucherCode"].ToString();
            }
            CheckoutViewModel.AddVoucherToBasket();

            // Save Order to Axis API
            sessionCheckoutDetails.PayPalRef = paypalAuthId;

            var createOrder = Touchpoints.SaveOrder(sessionCheckoutDetails, OrderStatus.Completed, null);
            if (!createOrder.IsSuccess)
            {
                // Error, Cancel the PayPal Authorisation
                createOrder = await CancelPayPalAuth(paypalAuthId, RecipientName);
                createOrder.IsSuccess = false;
                return createOrder;
            }

            // Capture the payment
            HttpContext.Current.Session["C_CheckoutDetails"] = sessionCheckoutDetails;
            try
            {
                Capture result = await CaptureAuthorisation(paypalAuthId, ConfigurationManager.AppSettings["Environment"] == "Live" ? false : true);
                if (result.Status != "COMPLETED")
                {
                    Utilities.LogInformationMessage("Unable to capture PayPal payment for : " + RecipientName + " reference: " + paypalOrderId + "-" + paypalAuthId);
                    sr.IsSuccess = false;
                    Dictionary<string, string> commonData = DataCache.GetSectionData("CommonData");
                    sr.Message = "There was a problem completing your PayPal transaction. Please contact customer services on " + commonData["TelephoneNumber"];
                }

                MyAccountViewModel.CreateCustomer(sessionCheckoutDetails);
            }
            catch (Exception e)
            {
                Utilities.ProcessException(e);
            }

            return sr;
        }




        public class PPJson
        {
            public class root
            {
                public string intent { get; set; }
                public PurchaseUnits[] purchase_units { get; set; }
                public PaymentSource payment_source { get; set; }
            }

            public class PaymentSource
            {
                public PaymentSourcePaypal paypal { get; set; }
            }

            public class PaymentSourcePaypal
            {
                public PaymentSourcePayPalExperienceContext experience_context { get; set; }
            }

            public class PaymentSourcePayPalExperienceContext
            {
                public string shipping_preference { get; set; } = "SET_PROVIDED_ADDRESS";
            }

            public class PurchaseUnits
            {
                public PurchaseUnitsAmount amount { get; set; }
                public PUShipping shipping { get; set; }
            }

            public class PUShipping
            {
                public PUShippingName name { get; set; }
                public PUShippingAddress address { get; set; }
            }

            public class PUShippingAddress
            {
                public string address_line_1 { get; set; }
                public string address_line_2 { get; set; }
                public string admin_area_1 { get; set; }
                public string admin_area_2 { get; set; }
                public string postal_code { get; set; }
                public string country_code { get; set; }
            }

            public class PUShippingName
            {
                public string full_name { get; set; }
            }

            public class PurchaseUnitsAmount
            {
                public string currency_code { get; set; }
                public string value { get; set; }
            }

            public class Shipping
            {
                public string op { get; set; }
                public string path { get; set; }
                public ShippingValue[] value { get; set; }
            }

            public class ShippingValue
            {
                public ShippingAmount amount { get; set; }
                public string id { get; set; }
                public string label { get; set; }
                public bool selected { get; set; }
                public string type { get; set; }
            }

            public class ShippingAmount
            {
                public string currency_code { get; set; }
                public string value { get; set; }
            }

            public class OrderDetail
            {
                public string op { get; set; }
                public string path { get; set; }
                public OrderDetailValue value { get; set; }
            }

            public class OrderDetailValue
            {
                public OrderDetailBreakDown breakdown { get; set; }
                public string currency_code { get; set; }
                public string value { get; set; }
            }

            public class OrderDetailBreakDown
            {
                public OrderDetailsItemTotal item_total { get; set; }
                public OrderDetailsShipping shipping { get; set; }
            }

            public class OrderDetailsItemTotal
            {
                public string currency_code { get; set; }
                public string value { get; set; }
            }

            public class OrderDetailsShipping
            {
                public string currency_code { get; set; }
                public string value { get; set; }
            }
        }

        public bool IsVATExempt { get; set; } = false;
    }
}
