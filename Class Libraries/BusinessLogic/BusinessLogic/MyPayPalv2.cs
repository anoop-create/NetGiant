using BusinessLogic.ViewModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PayPalCheckoutSdk.Core;
using PayPalCheckoutSdk.Payments;
using PayPalHttp;
using System.Collections.Generic;
using System.Configuration;
using System;
using System.Linq;
using System.Web;
using VMerchantWrapper.Entities;
using System.Net;
using System.Threading.Tasks;
using DataAccess.EntityFramework;

namespace BusinessLogic
{
    public class MyPayPalv2
    {
        public static async Task<SaveReturn> Capture(string trans, string paypalType, string randomPassword, BasketTotals bt)
        {
            SaveReturn sr = new SaveReturn();
            Dictionary<string, string> commonData = DataCache.GetSectionData("CommonData");
            JObject transaction = JsonConvert.DeserializeObject<JObject>(trans);
            var checkoutData = DataCache.GetSectionData("CheckoutData");
            //bool causeError = checkoutData.ContainsKey("CausePayPalError") ? Convert.ToBoolean(checkoutData["CausePayPalError"].ToString()) : false;

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

            // Check that order amount matches
            string paypalAmount = transaction["purchase_units"].First()["payments"]["authorizations"].First()["amount"]["value"].ToString();
            decimal amt = Decimal.Parse(paypalAmount);
            if (amt != bt.GrandTotalIncVat)
            {
                sr.Message = "There was a problem processing your PayPal payment. Please try again";
                sr.IsSuccess = false;
                return sr;
            }

            string paypalRecipientName = transaction["purchase_units"].First()["shipping"]["name"]["full_name"].ToString();
            string paymentTitle = transaction["payer"]["name"]["title"]?.ToString() ?? "Mr";
            string paymentFirstname = transaction["payer"]["name"]["given_name"].ToString();
            string paymentSurname = transaction["payer"]["name"]["surname"].ToString();
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

            // Get Post Code and Delivery Service Id
            var payerPostcode = "M46 0SY";
            var deliveryServiceId = CheckoutViewModel.RetrieveDeliveryOptions(payerPostcode).First().DeliveryServiceId;

            // Add Delivery to basket
            Basket.ProcessDelivery(deliveryServiceId);

            string recipientFirstname;
            string recipientSurname;
            //var paypalRecipientName = paymentDetails.payer.payer_info.shipping_address.recipient_name ?? "";

            var nameParts = paypalRecipientName.Split(' ');
            if (nameParts.Length == 2)
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
                Touchpoints.SaveUser(signUp);
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
            cd.PayPalRef = paypalOrderId + "-" + paypalAuthId;
            try
            {
                var saveReturn = Touchpoints.SaveOrder(cd, OrderStatus.Completed, null);

                // TESTING: To cause an error in SaveOrder set the LockAccounts flag to 1 in the UploadStats table in the vMerchant database
                if (saveReturn.IsSuccess)
                {
                    // SaveOrder successful
                    HttpContext.Current.Session["C_CheckoutDetails"] = cd;

                    // Capture the paypal authorised payment
                    Capture result = await CaptureAuthorisation(paypalAuthId, ConfigurationManager.AppSettings["Environment"] == "Live" ? false : true);
                    if (result.Status != "COMPLETED")
                    {
                        Utilities.LogInformationMessage("Unable to capture PayPal payment for : " + paypalRecipientName + " reference: " + paypalOrderId + "-" + paypalAuthId);
                        sr.IsSuccess = false;
                        sr.Message = "There was a problem completing your PayPal transaction. Please contact customer services on " + commonData["TelephoneNumber"];
                    }

                    MyAccountViewModel.CreateCustomer(cd);
                }
                else
                {
                    // Error, Cancel the PayPal Authorisation
                    sr = await CancelPayPalAuth(paypalAuthId, paypalRecipientName);
                    sr.IsSuccess = false;
                }
            }
            catch
            {
                // Error, Cancel the PayPal Authorisation
                sr = await CancelPayPalAuth(paypalAuthId, paypalRecipientName);
                sr.IsSuccess = false;
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

        // ***************** PayPal API v2 ***************************        

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
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
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
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
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

        // ***************** PayPal API v1 ****************************

        //private static APIContext CreateApiContext(string accessToken)
        //{
        //    APIContext apiContext = new APIContext(accessToken);
        //    Dictionary<string, string> config = new Dictionary<string, string>();
        //    config.Add("mode", ConfigurationManager.AppSettings["PayPalMode"]);
        //    config.Add("clientId", ConfigurationManager.AppSettings["PayPalClientId"]);
        //    config.Add("clientSecret", ConfigurationManager.AppSettings["PayPalSecret"]);
        //    apiContext.Config = config;

        //    return apiContext;
        //}

        //private static string GetAccessToken(bool bypass = false)
        //{
        //    string cacheKey = "PayPalToken";
        //    string accessToken = DataCache.GetCache<string>(cacheKey);
        //    if (accessToken == null || bypass)
        //    {
        //        Dictionary<string, string> config = new Dictionary<string, string>();
        //        config.Add("mode", ConfigurationManager.AppSettings["PayPalMode"]);
        //        config.Add("clientId", ConfigurationManager.AppSettings["PayPalClientId"]);
        //        config.Add("clientSecret", ConfigurationManager.AppSettings["PayPalSecret"]);

        //        // Use OAuthTokenCredential to request an access token from PayPal
        //        var fullToken = new OAuthTokenCredential(config);
        //        accessToken = fullToken.GetAccessToken();
        //        int validFor = (fullToken.AccessTokenExpirationInSeconds / 3600) - 1;

        //        DataCache.PutCache(cacheKey, accessToken, validFor);
        //    }

        //    return accessToken;
        //}

        //public static Payment CreatePayment(CheckoutDetails cd, BasketTotals bt)
        //{
        //    var commonData = DataCache.GetSectionData("CommonData");
        //    var accessToken = GetAccessToken();
        //    var apiContext = CreateApiContext(accessToken);
        //    var experienceProfileId = GetExperienceProfile(apiContext, commonData);
        //    var deliveryPostcode = "M46 0SY";
        //    if (cd != null)
        //    {
        //        deliveryPostcode = cd.DeliveryAddress != null ? cd.DeliveryAddress.PostCode : deliveryPostcode;
        //    }
        //    var deliveryServiceId = CheckoutViewModel.RetrieveDeliveryOptions(deliveryPostcode).First().DeliveryServiceId;

        //    // Add Delivery
        //    Basket.ProcessDelivery(deliveryServiceId);

        //    var redirectUrls = new RedirectUrls
        //    {
        //        return_url = "https://www.paypal.com/return",
        //        cancel_url = "https://www.paypal.com/cancel"
        //    };

        //    var payer = new Payer
        //    {
        //        payment_method = "paypal"
        //    };

        //    var transactions = new List<Transaction>
        //    {
        //        new Transaction
        //        {
        //            amount = new Amount
        //            {
        //                total = bt.GrandTotalIncVat.ToString("#.00"),
        //                currency = "GBP"
        //            },
        //            description = "Purchase from " + Utilities.GetItemFromDict(commonData, "SiteName")
        //        }
        //    };

        //    if (cd != null && cd.IsNewCustomer == false)
        //    {
        //        var line1 = cd.DeliveryAddress.Line1;
        //        var line2 = cd.DeliveryAddress.Line2;
        //        if (line1 == "")
        //        {
        //            line1 = cd.DeliveryAddress.Line2;
        //            line2 = cd.DeliveryAddress.Line3;
        //        }
        //        if (line2 == "")
        //        {
        //            line2 = cd.DeliveryAddress.Line3;
        //        }

        //        var items = new ItemList
        //        {
        //            shipping_address = new ShippingAddress
        //            {
        //                recipient_name = cd.RecipientName.Title + " " + cd.RecipientName.Firstname + " " +
        //                                 cd.RecipientName.Surname,
        //                line1 = line1,
        //                line2 = line2,
        //                city = cd.DeliveryAddress.Line4,
        //                country_code = "GB",
        //                postal_code = cd.DeliveryAddress.PostCode,
        //                phone = cd.TelephoneNumber,
        //                state = ""
        //            }
        //        };

        //        if (transactions.FirstOrDefault() != null)
        //            transactions.First().item_list = items;
        //    }

        //    var payload = new Payment
        //    {
        //        intent = "sale",
        //        payer = payer,
        //        transactions = transactions,
        //        experience_profile_id = experienceProfileId,
        //        redirect_urls = redirectUrls
        //    };

        //    var payment = Payment.Create(apiContext, payload);

        //    return payment;
        //}

        //public static Payment ExecutePayment(string paymentid, string payerid, string paypalType, string randomPassword, BasketTotals bt)
        //{
        //    var checkoutData = DataCache.GetSectionData("CheckoutData");
        //    bool causeError = checkoutData.ContainsKey("CausePayPalError") ? Convert.ToBoolean(checkoutData["CausePayPalError"].ToString()) : false;
        //    var accessToken = GetAccessToken();
        //    var apiContext = CreateApiContext(accessToken);
        //    var payment = new Payment();

        //    var payload = new PaymentExecution
        //    {
        //        payer_id = payerid
        //    };

        //    var paymentDetails = Payment.Get(apiContext, paymentid);
        //    if (CheckoutViewModel.InvalidPostCodeCheck(paymentDetails.payer.payer_info.shipping_address.postal_code))
        //    {
        //        payment.failure_reason = "We are currently unable to deliver to your postcode " + paymentDetails.payer.payer_info.shipping_address.postal_code;
        //        payment.state = "REJECTED";
        //        return payment;
        //    }
        //    var payerPostcode = paypalType == "checkout" ? paymentDetails.payer.payer_info.shipping_address.postal_code : "M46 0SY";
        //    var deliveryServiceId = CheckoutViewModel.RetrieveDeliveryOptions(payerPostcode).First().DeliveryServiceId;

        //    // Add Delivery
        //    Basket.ProcessDelivery(deliveryServiceId);

        //    if (paypalType == "checkout")
        //    {
        //        payment = Payment.Execute(apiContext, paymentid, payload);
        //        ((CheckoutDetails)HttpContext.Current.Session["C_CheckoutDetails"]).PayPalRef = payment.transactions.First().related_resources.First().sale.id;
        //    }
        //    else if (paypalType == "viewBasket")
        //    {
        //        string recipientFirstname;
        //        string recipientSurname;
        //        var paypalRecipientName = paymentDetails.payer.payer_info.shipping_address.recipient_name ?? "";

        //        var nameParts = paypalRecipientName.Split(' ');
        //        if (nameParts.Length == 2)
        //        {
        //            recipientFirstname = nameParts[0];
        //            recipientSurname = nameParts[1];
        //        }
        //        else if (nameParts.Length >= 3)
        //        {
        //            recipientSurname = nameParts[nameParts.Length - 1];
        //            recipientFirstname = string.Join(" ", nameParts.Take(nameParts.Length - 1));
        //        }
        //        else
        //        {
        //            recipientFirstname = paymentDetails.payer.payer_info.first_name;
        //            recipientSurname = paymentDetails.payer.payer_info.last_name;
        //        }

        //        var cd = new CheckoutDetails
        //        {
        //            Name = new Name
        //            {
        //                Title = paymentDetails.payer.payer_info.salutation.Truncate(25) ?? "Mr",
        //                Firstname = paymentDetails.payer.payer_info.first_name.Truncate(20),
        //                Surname = paymentDetails.payer.payer_info.last_name.Truncate(20)
        //            },
        //            RecipientName = new Name
        //            {
        //                Title = paymentDetails.payer.payer_info.salutation.Truncate(25) ?? "Mr",
        //                Firstname = recipientFirstname.Truncate(20),
        //                Surname = recipientSurname.Truncate(20)
        //            },
        //            BillingAddress = new Address
        //            {
        //                Country = paymentDetails.payer.payer_info.shipping_address.country_code.Truncate(2),
        //                Line1 = "",
        //                Line2 = paymentDetails.payer.payer_info.shipping_address.line1.Truncate(30),
        //                Line3 = paymentDetails.payer.payer_info.shipping_address.line2.Truncate(30),
        //                Line4 = paymentDetails.payer.payer_info.shipping_address.city.Truncate(30),
        //                Line5 = paymentDetails.payer.payer_info.shipping_address.state.Truncate(30),
        //                PostCode = paymentDetails.payer.payer_info.shipping_address.postal_code.Truncate(30)
        //            },
        //            DeliveryAddress = new Address
        //            {
        //                Country = paymentDetails.payer.payer_info.shipping_address.country_code.Truncate(2),
        //                Line1 = "",
        //                Line2 = paymentDetails.payer.payer_info.shipping_address.line1.Truncate(30),
        //                Line3 = paymentDetails.payer.payer_info.shipping_address.line2.Truncate(30),
        //                Line4 = paymentDetails.payer.payer_info.shipping_address.city.Truncate(30),
        //                Line5 = paymentDetails.payer.payer_info.shipping_address.state.Truncate(30),
        //                PostCode = paymentDetails.payer.payer_info.shipping_address.postal_code.Truncate(30)
        //            },
        //            TelephoneNumber = paymentDetails.payer.payer_info.shipping_address.phone.Truncate(20) ??
        //                              paymentDetails.payer.payer_info.phone.Truncate(20) ?? "0",
        //            Email = paymentDetails.payer.payer_info.email.Truncate(50),
        //            DeliveryServiceId = deliveryServiceId,
        //            PaymentMethod = "PayPal"
        //        };

        //        // Save Order to Axis API
        //        var user = Touchpoints.GetUserData("", cd.Email, "", true);
        //        if (user.Rows.Count > 0)
        //        {
        //            if (user.Rows[0]["password"].ToString() == "")
        //            {
        //                MyAccountViewModel.SetNewPassword(cd.Email, randomPassword);
        //                cd.Password = randomPassword;
        //            }
        //            else
        //            {
        //                cd.Password = user.Rows[0]["password"].ToString();
        //            }
        //            cd.Newsletter = Convert.ToBoolean(user.Rows[0]["isOnMailingList"] != DBNull.Value ? user.Rows[0]["isOnMailingList"] : false);
        //            cd.AccountNumber = user.Rows[0]["account"].ToString();
        //        }
        //        else
        //        {
        //            var signUp = new SignUp
        //            {
        //                Address = cd.BillingAddress,
        //                Name = cd.Name,
        //                Newsletter = false,
        //                Password = randomPassword,
        //                TelNumber = cd.TelephoneNumber,
        //                UserName = cd.Email
        //            };
        //            Touchpoints.SaveUser(signUp);
        //            cd.Password = randomPassword;
        //            cd.AccountNumber = "";
        //        }

        //        if (HttpContext.Current.Session["B_VoucherCode"] != null)
        //            cd.VoucherCode = HttpContext.Current.Session["B_VoucherCode"].ToString();

        //        // Add Voucher to BasketContents
        //        CheckoutViewModel.AddVoucherToBasket();

        //        var saveReturn = Touchpoints.SaveOrder(cd, OrderStatus.Draft, null);
        //        if (saveReturn.IsSuccess)
        //        {
        //            HttpContext.Current.Session["C_CheckoutDetails"] = cd;
        //            if (ConfigurationManager.AppSettings["Environment"] != "Live" && causeError)
        //            {
        //                apiContext.HTTPHeaders.Add("PayPal-Mock-Response", "{\"mock_application_codes\":\"INSTRUMENT_DECLINED\"}");
        //            }
        //            payment = Payment.Execute(apiContext, paymentid, payload);

        //            if (payment.state == "approved")
        //            {
        //                cd.PayPalRef = payment.transactions.First().related_resources.First().sale.id;
        //                Touchpoints.SaveOrder(cd, OrderStatus.Completed, cd.BackOfficeOrderRef);
        //                MyAccountViewModel.CreateCustomer(cd);
        //            }
        //        }
        //    }

        //    return payment;
        //}

        //private static string GetExperienceProfile(APIContext apiContext, Dictionary<string, string> commonData, bool refreshProfile = false)
        //{
        //    WebProfileList profiles = WebProfile.GetList(apiContext);
        //    WebProfile wp = null;

        //    if (profiles.Count > 0)
        //    {
        //        wp = new WebProfile();
        //        wp = profiles.Find(x => x.name == "default");
        //    }
        //    if (wp != null && !refreshProfile)
        //    {
        //        return wp.id;
        //    }
        //    if (wp != null)
        //    {
        //        // Delete Profile
        //        WebProfile.Delete(apiContext, wp.id);
        //    }
        //    WebProfile payload = new WebProfile()
        //    {
        //        name = "default",
        //        flow_config = new FlowConfig()
        //        {
        //            user_action = "commit"
        //        },
        //        input_fields = new InputFields()
        //        {
        //            address_override = 1
        //        },
        //        presentation = new Presentation()
        //        {
        //            brand_name = Utilities.GetItemFromDict(commonData, "SiteName"),
        //            logo_image = "https:" + ConfigurationManager.AppSettings["CDN"] + "/Images/paypal-logo.png"
        //        }
        //    };

        //    CreateProfileResponse resp = WebProfile.Create(apiContext, payload);

        //    return resp.id;
        //}
    }
}

