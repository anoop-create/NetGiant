using PayPal.Api;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Web;
using BusinessLogic.ViewModels;
using DataAccess.EntityFramework;
using VMerchantWrapper.Entities;
using System;

namespace BusinessLogic
{
    public class MyPayPal
    {
        private static string GetAccessToken(bool bypass = false)
        {
            string cacheKey = "PayPalToken";
            string accessToken = DataCache.GetCache<string>(cacheKey);
            if (accessToken == null || bypass)
            {
                Dictionary<string, string> config = new Dictionary<string, string>();
                config.Add("mode", ConfigurationManager.AppSettings["PayPalMode"]);
                config.Add("clientId", ConfigurationManager.AppSettings["PayPalClientId"]);
                config.Add("clientSecret", ConfigurationManager.AppSettings["PayPalSecret"]);

                // Use OAuthTokenCredential to request an access token from PayPal
                var fullToken = new OAuthTokenCredential(config);
                accessToken = fullToken.GetAccessToken();
                int validFor = (fullToken.AccessTokenExpirationInSeconds / 3600) - 1;

                DataCache.PutCache(cacheKey, accessToken, validFor);
            }

            return accessToken;
        }

        public static Payment CreatePayment(CheckoutDetails cd, BasketTotals bt)
        {
            var commonData = DataCache.GetSectionData("CommonData");
            var accessToken = GetAccessToken();
            var apiContext = CreateApiContext(accessToken);
            var experienceProfileId = GetExperienceProfile(apiContext, commonData);
            //string experienceProfileId = GetExperienceProfile(apiContext, commonData, true); //Use this to refresh the profile
            var deliveryPostcode = "M46 0SY";
            if (cd != null)
            {
                deliveryPostcode = cd.DeliveryAddress != null ? cd.DeliveryAddress.PostCode : deliveryPostcode;
            }
            var deliveryServiceId = CheckoutViewModel.RetrieveDeliveryOptions(deliveryPostcode).First().DeliveryServiceId;

            // Add Delivery
            Basket.ProcessDelivery(deliveryServiceId);

            var redirectUrls = new RedirectUrls
            {
                return_url = "https://www.paypal.com/return",
                cancel_url = "https://www.paypal.com/cancel"
            };

            var payer = new Payer
            {
                payment_method = "paypal"
            };

            var transactions = new List<Transaction>
            {
                new Transaction
                {
                    amount = new Amount
                    {
                        total = bt.GrandTotalIncVat.ToString("#.00"),
                        currency = "GBP"
                    },
                    description = "Purchase from " + Utilities.GetItemFromDict(commonData, "SiteName")
                }
            };

            if (cd != null && cd.IsNewCustomer == false)
            {
                var line1 = cd.DeliveryAddress.Line1;
                var line2 = cd.DeliveryAddress.Line2;
                if (line1 == "")
                {
                    line1 = cd.DeliveryAddress.Line2;
                    line2 = cd.DeliveryAddress.Line3;
                }
                if (line2 == "")
                {
                    line2 = cd.DeliveryAddress.Line3;
                }

                var items = new ItemList
                {
                    shipping_address = new ShippingAddress
                    {
                        recipient_name = cd.RecipientName.Title + " " + cd.RecipientName.Firstname + " " +
                                         cd.RecipientName.Surname,
                        line1 = line1,
                        line2 = line2,
                        city = cd.DeliveryAddress.Line4,
                        country_code = "GB",
                        postal_code = cd.DeliveryAddress.PostCode,
                        phone = cd.TelephoneNumber,
                        state = ""
                    }
                };

                if (transactions.FirstOrDefault() != null)
                    transactions.First().item_list = items;
            }

            var payload = new Payment
            {
                intent = "sale",
                payer = payer,
                transactions = transactions,
                experience_profile_id = experienceProfileId,
                redirect_urls = redirectUrls
            };

            var payment = Payment.Create(apiContext, payload);

            return payment;
        }

        public static Payment ExecutePayment(string paymentid, string payerid, string paypalType, string randomPassword, BasketTotals bt)
        {
            var checkoutData = DataCache.GetSectionData("CheckoutData");
            bool causeError = checkoutData.ContainsKey("CausePayPalError") ? Convert.ToBoolean(checkoutData["CausePayPalError"].ToString()) : false;
            var accessToken = GetAccessToken();
            var apiContext = CreateApiContext(accessToken);
            var payment = new Payment();

            var payload = new PaymentExecution
            {
                payer_id = payerid
            };

            var paymentDetails = Payment.Get(apiContext, paymentid);
            var payerPostcode = paypalType == "checkout" ? paymentDetails.payer.payer_info.shipping_address.postal_code : "M46 0SY";
            var deliveryServiceId = CheckoutViewModel.RetrieveDeliveryOptions(payerPostcode).First().DeliveryServiceId;

            // Add Delivery
            Basket.ProcessDelivery(deliveryServiceId);

            if (paypalType == "checkout")
            {
                payment = Payment.Execute(apiContext, paymentid, payload);
                ((CheckoutDetails)HttpContext.Current.Session["C_CheckoutDetails"]).PayPalRef = payment.transactions.First().related_resources.First().sale.id;
            }
            else if (paypalType == "viewBasket")
            {
                string recipientFirstname;
                string recipientSurname;
                var paypalRecipientName = paymentDetails.payer.payer_info.shipping_address.recipient_name ?? "";

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
                    recipientFirstname = paymentDetails.payer.payer_info.first_name;
                    recipientSurname = paymentDetails.payer.payer_info.last_name;
                }

                var cd = new CheckoutDetails
                {
                    Name = new Name
                    {
                        Title = paymentDetails.payer.payer_info.salutation.Truncate(25) ?? "Mr",
                        Firstname = paymentDetails.payer.payer_info.first_name.Truncate(20),
                        Surname = paymentDetails.payer.payer_info.last_name.Truncate(20)
                    },
                    RecipientName = new Name
                    {
                        Title = paymentDetails.payer.payer_info.salutation.Truncate(25) ?? "Mr",
                        Firstname = recipientFirstname.Truncate(20),
                        Surname = recipientSurname.Truncate(20)
                    },
                    BillingAddress = new Address
                    {
                        Country = paymentDetails.payer.payer_info.shipping_address.country_code.Truncate(2),
                        Line1 = "",
                        Line2 = paymentDetails.payer.payer_info.shipping_address.line1.Truncate(30),
                        Line3 = paymentDetails.payer.payer_info.shipping_address.line2.Truncate(30),
                        Line4 = paymentDetails.payer.payer_info.shipping_address.city.Truncate(30),
                        Line5 = paymentDetails.payer.payer_info.shipping_address.state.Truncate(30),
                        PostCode = paymentDetails.payer.payer_info.shipping_address.postal_code.Truncate(30)
                    },
                    DeliveryAddress = new Address
                    {
                        Country = paymentDetails.payer.payer_info.shipping_address.country_code.Truncate(2),
                        Line1 = "",
                        Line2 = paymentDetails.payer.payer_info.shipping_address.line1.Truncate(30),
                        Line3 = paymentDetails.payer.payer_info.shipping_address.line2.Truncate(30),
                        Line4 = paymentDetails.payer.payer_info.shipping_address.city.Truncate(30),
                        Line5 = paymentDetails.payer.payer_info.shipping_address.state.Truncate(30),
                        PostCode = paymentDetails.payer.payer_info.shipping_address.postal_code.Truncate(30)
                    },
                    TelephoneNumber = paymentDetails.payer.payer_info.shipping_address.phone.Truncate(20) ??
                                      paymentDetails.payer.payer_info.phone.Truncate(20) ?? "0",
                    Email = paymentDetails.payer.payer_info.email.Truncate(50),
                    DeliveryServiceId = deliveryServiceId,
                    PaymentMethod = "PayPal"
                };

                // Save Order to Axis API
                var user = Touchpoints.GetUserData("", cd.Email);
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

                if (HttpContext.Current.Session["B_VoucherCode"] != null)
                    cd.VoucherCode = HttpContext.Current.Session["B_VoucherCode"].ToString();

                // Add Voucher to BasketContents
                CheckoutViewModel.AddVoucherToBasket();

                var saveReturn = Touchpoints.SaveOrder(cd, OrderStatus.Draft, null);
                if (saveReturn.IsSuccess)
                {
                    HttpContext.Current.Session["C_CheckoutDetails"] = cd;
                    if (ConfigurationManager.AppSettings["Environment"] != "Live" && causeError)
                    {
                        apiContext.HTTPHeaders.Add("PayPal-Mock-Response", "{\"mock_application_codes\":\"INSTRUMENT_DECLINED\"}");
                    }
                    payment = Payment.Execute(apiContext, paymentid, payload);

                    if (payment.state == "approved")
                    {
                        cd.PayPalRef = payment.transactions.First().related_resources.First().sale.id;
                        Touchpoints.SaveOrder(cd, OrderStatus.Completed, cd.BackOfficeOrderRef);
                        MyAccountViewModel.CreateCustomer(cd);
                    }
                }
            }

            return payment;
        }

        private static string GetExperienceProfile(APIContext apiContext, Dictionary<string, string> commonData, bool refreshProfile = false)
        {
            WebProfileList profiles = WebProfile.GetList(apiContext);
            WebProfile wp = null;

            if (profiles.Count > 0)
            {
                wp = new WebProfile();
                wp = profiles.Find(x => x.name == "default");
            }
            if (wp != null && !refreshProfile)
            {
                return wp.id;
            }
            if (wp != null)
            {
                // Delete Profile
                WebProfile.Delete(apiContext, wp.id);
            }
            WebProfile payload = new WebProfile()
            {
                name = "default",
                flow_config = new FlowConfig()
                {
                    user_action = "commit"
                },
                input_fields = new InputFields()
                {
                    address_override = 1
                },
                presentation = new Presentation()
                {
                    brand_name = Utilities.GetItemFromDict(commonData, "SiteName"),
                    logo_image = "https:" + ConfigurationManager.AppSettings["CDN"] + "/Images/paypal-logo.png"
                }
            };

            CreateProfileResponse resp = WebProfile.Create(apiContext, payload);

            return resp.id;
        }

        private static APIContext CreateApiContext(string accessToken)
        {
            APIContext apiContext = new APIContext(accessToken);
            Dictionary<string, string> config = new Dictionary<string, string>();
            config.Add("mode", ConfigurationManager.AppSettings["PayPalMode"]);
            config.Add("clientId", ConfigurationManager.AppSettings["PayPalClientId"]);
            config.Add("clientSecret", ConfigurationManager.AppSettings["PayPalSecret"]);
            apiContext.Config = config;

            return apiContext;
        }
    }
}
