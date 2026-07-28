using Newtonsoft.Json;
using RestSharp.Authenticators;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Web;
using BusinessLogic.ViewModels;
using System.Web.Mvc;
using DataAccess.EntityFramework;
using System.Data.Entity;
using Newtonsoft.Json.Linq;
using MailChimp.Net.Models;
using Nest;
using System.Security.Principal;
using VMerchantWrapper.Entities;
using Org.BouncyCastle.Asn1.Ocsp;
using System.Web.Util;
using EllipticCurve;
using PayPalCheckoutSdk.Orders;
using System.Numerics;
using Org.BouncyCastle.Crypto.Generators;
using System.Xml;
using System.Web.UI.WebControls;

namespace BusinessLogic
{
    public class MyOpayo
    {
        public static MerchantSessionKey GetMerchantSessionKey()
        {
            Utilities.SetTlsVersion();
            var client = new RestClient(ConfigurationManager.AppSettings["OpayoApiEndpoint"]);
            var request = new RestRequest("merchant-session-keys")
            {
                Authenticator = new HttpBasicAuthenticator(ConfigurationManager.AppSettings["OpayoApiKey"], ConfigurationManager.AppSettings["OpayoApiPassword"])
            }
                .AddJsonBody("{ \"vendorName\": \"" + ConfigurationManager.AppSettings["OpayoVendor"] + "\" }");
            var response = client.Execute(request, RestSharp.Method.Post);
            if (response.StatusCode != HttpStatusCode.Created)
            {
                Utilities.LogInformationMessage("Opayo Error - Unable to create Merchant Session Key: " + response.StatusCode + " - " + response.Content);
                return null;
            }
            return JsonConvert.DeserializeObject<MerchantSessionKey>(response.Content);
        }

        public static RestResponse SubmitTransaction(CheckoutViewModel cvm, HttpRequestBase req)
        {

            bool isSavedCard = cvm.CheckoutDetails.UseASavedCard;
            bool isNewSavedCard = cvm.CheckoutDetails.SaveThisCard && !cvm.CheckoutDetails.UseASavedCard;

            CheckoutDetails cd = new CheckoutDetails();
            if (HttpContext.Current.Session["C_CheckoutDetails"] != null)
            {
                cd = (CheckoutDetails)HttpContext.Current.Session["C_CheckoutDetails"];
                cd.UseASavedCard = cvm.CheckoutDetails.UseASavedCard;
                cd.SaveThisCard = cvm.CheckoutDetails.SaveThisCard;
            }
            else
            {
                return null;
            }
            cvm.CheckoutDetails = cd;
            cvm.CheckoutDetails.SagePayTxCode = GenerateTxCode();

            OpayoBillingAddress oba = new OpayoBillingAddress()
            {
                city = cvm.CheckoutDetails.BillingAddress.Line4,
                postalCode = cvm.CheckoutDetails.BillingAddress.PostCode,
                country = cvm.CheckoutDetails.BillingAddress.Country
            };
            if (string.IsNullOrEmpty(cvm.CheckoutDetails.BillingAddress.Line1))
            {
                if (string.IsNullOrEmpty(cvm.CheckoutDetails.BillingAddress.Line2))
                {
                    if (string.IsNullOrEmpty(cvm.CheckoutDetails.BillingAddress.Line3))
                    {
                        //No Address
                    }
                    else
                    {
                        oba.address1 = cvm.CheckoutDetails.BillingAddress.Line3;
                    }
                }
                else
                {
                    oba.address1 = cvm.CheckoutDetails.BillingAddress.Line2;
                    oba.address2 = cvm.CheckoutDetails.BillingAddress.Line3;
                }
            }
            else
            {
                oba.address1 = cvm.CheckoutDetails.BillingAddress.Line1;
                oba.address2 = cvm.CheckoutDetails.BillingAddress.Line2;
                oba.address3 = cvm.CheckoutDetails.BillingAddress.Line3;
            }

            OpayoShippingDetails osd = new OpayoShippingDetails()
            {
                recipientFirstName = cvm.CheckoutDetails.RecipientName.Firstname,
                recipientLastName = cvm.CheckoutDetails.RecipientName.Surname,
                shippingCity = cvm.CheckoutDetails.DeliveryAddress.Line4,
                shippingPostalCode = cvm.CheckoutDetails.DeliveryAddress.PostCode,
                shippingCountry = cvm.CheckoutDetails.DeliveryAddress.Country
            };
            if (string.IsNullOrEmpty(cvm.CheckoutDetails.DeliveryAddress.Line1))
            {
                if (string.IsNullOrEmpty(cvm.CheckoutDetails.DeliveryAddress.Line2))
                {
                    if (string.IsNullOrEmpty(cvm.CheckoutDetails.DeliveryAddress.Line3))
                    {
                        //No Address
                    }
                    else
                    {
                        osd.shippingAddress1 = cvm.CheckoutDetails.DeliveryAddress.Line3;
                    }
                }
                else
                {
                    osd.shippingAddress1 = cvm.CheckoutDetails.DeliveryAddress.Line2;
                    osd.shippingAddress2 = cvm.CheckoutDetails.DeliveryAddress.Line3;
                }
            }
            else
            {
                osd.shippingAddress1 = cvm.CheckoutDetails.DeliveryAddress.Line1;
                osd.shippingAddress2 = cvm.CheckoutDetails.DeliveryAddress.Line2;
                osd.shippingAddress3 = cvm.CheckoutDetails.DeliveryAddress.Line3;
            }

            OpayoCredentialType oct = new OpayoCredentialType()
            {
                cofUsage = isNewSavedCard ? "First" : "Subsequent",
                initiatedType = isNewSavedCard ? "CIT" : Convert.ToBoolean(HttpContext.Current.Session["U_IsPortalUser"]) ? "MIT" : "CIT",
                mitType = "Unscheduled"
            };            

            OpayoTransaction ot = new OpayoTransaction()
            {
                transactionType = "Payment",
                paymentMethod = new OpayoPaymentMethod()
                {
                    card = new OpayoCard()
                    {
                        merchantSessionKey = cvm.CheckoutDetails.MerchantSessionKey.merchantSessionKey,
                        cardIdentifier = cvm.CardIdentifier,                     
                        reusable = isSavedCard ? true : (bool?)null,
                        save = isNewSavedCard ? true : (bool?)null
                    }
                },
                vendorTxCode = cvm.CheckoutDetails.SagePayTxCode,
                amount = Convert.ToInt32(cvm.CheckoutDetails.TotalIncVat * 100),
                description = Utilities.GetItemFromDict(cvm.CommonData, "ShortSiteName").ToString() + "-" + cvm.CheckoutDetails.BackOfficeOrderRef,
                customerFirstName = cvm.CheckoutDetails.Name.Firstname,
                customerLastName = cvm.CheckoutDetails.Name.Surname,
                billingAddress = oba,
                entryMethod = Convert.ToBoolean(HttpContext.Current.Session["U_IsPortalUser"]) ? "TelephoneOrder" : "Ecommerce",
                apply3DSecure = isNewSavedCard ? "Force" : "UseMSPSetting",
                applyAvsCvcCheck = "UseMSPSetting",
                //applyAvsCvcCheck = Convert.ToBoolean(HttpContext.Current.Session["U_IsPortalUser"]) ? "Disable" : "UseMSPSetting", // ???
                customerEmail = cvm.CheckoutDetails.Email.Trim(),
                customerPhone = cvm.CheckoutDetails.TelephoneNumber.Trim(),
                shippingDetails = osd,
                strongCustomerAuthentication = new OpayoStrongCustomerAuthentication()
                {
                    notificationURL = ConfigurationManager.AppSettings["OpayoNotificationUrl"] + "?jsonid=" + cvm.CheckoutDetails.JsonStoreId.ToString(),
                    browserIP = Utilities.GetClientIPAddress(req),
                    browserColorDepth = cvm.BrowserColorDepth,
                    browserScreenHeight = cvm.BrowserScreenHeight,
                    browserScreenWidth = cvm.BrowserScreenWidth,
                    browserTZ = (DateTimeOffset.Now.Offset.TotalMinutes * -1).ToString(),
                    browserAcceptHeader = req.ServerVariables["HTTP_ACCEPT"],
                    browserJavascriptEnabled = true,
                    browserLanguage = req.UserLanguages[0],
                    browserUserAgent = req.ServerVariables["HTTP_USER_AGENT"],
                    challengeWindowSize = "Medium",     // W390 x H400
                    transType = "GoodsAndServicePurchase"
                },
                credentialType = isSavedCard || isNewSavedCard ? oct : null
            };

            var client = new RestClient(ConfigurationManager.AppSettings["OpayoApiEndpoint"]);
            string jsonBody = JsonConvert.SerializeObject(ot,
                new JsonSerializerSettings()
                {
                    NullValueHandling = NullValueHandling.Ignore
                }
            );
            var request = new RestRequest("transactions")
            {
                Authenticator = new HttpBasicAuthenticator(ConfigurationManager.AppSettings["OpayoApiKey"], ConfigurationManager.AppSettings["OpayoApiPassword"])
            }
                .AddJsonBody(jsonBody);
            var response = client.Execute(request, RestSharp.Method.Post);

            EntityAccess.InsertOpayoLog(jsonBody, cvm.CheckoutDetails.BackOfficeOrderRef, cvm.CheckoutDetails.MerchantSessionKey.merchantSessionKey, "Create Transaction");
            return response;
        }

        public static bool DeleteCard(string tokenId, int id)
        {
            string post = "VPSProtocol=4.00" +
                "&TxType=REMOVETOKEN" +
                "&Vendor=" + ConfigurationManager.AppSettings["OpayoVendor"] + 
                "&Token=" + tokenId;

            string env = ConfigurationManager.AppSettings["Environment"] == "Live" ? "live" : "sandbox";
            RestClient client = new RestClient("https://" +  env + ".opayo.eu.elavon.com/gateway/service/");
            var request = new RestRequest("removetoken.vsp");
            request.AddHeader("content-type", "application/x-www-form-urlencoded");
            request.AddParameter("application/x-www-form-urlencoded", post, ParameterType.RequestBody);
            var response = client.Execute(request, RestSharp.Method.Post);
            if (response.IsSuccessful)
            {
                EntityAccess.DeleteSagePayToken(id);
            }
            return response.IsSuccessful;
        }

        public static RestResponse Submit3DAuthTransaction(string cres, string transactionId)
        {
            RestClient client = new RestClient(ConfigurationManager.AppSettings["OpayoApiEndpoint"]);
            var request = new RestRequest("transactions/" + transactionId + "/3d-secure-challenge")
            {
                Authenticator = new HttpBasicAuthenticator(ConfigurationManager.AppSettings["OpayoApiKey"], ConfigurationManager.AppSettings["OpayoApiPassword"])
            }
                .AddJsonBody("{ \"cRes\":\"" + cres + "\"}");
            var response = client.Execute(request, RestSharp.Method.Post);
            return response;
        }

        public static SaveReturn ProcessTransactionResponse(RestResponse response, CheckoutViewModel cvm)
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = false;

            EntityAccess.InsertOpayoLog(response.Content, cvm.CheckoutDetails.BackOfficeOrderRef, cvm.CheckoutDetails.MerchantSessionKey.merchantSessionKey, "Authentication Response");
            OpayoTransactionResponse otr = JsonConvert.DeserializeObject<OpayoTransactionResponse>(response.Content);
            if (otr.status == "Ok")
            {
                // For new saved cards - store the card identifier
                if (otr.paymentMethod.card.reusable == true)
                {
                    SagePayToken token = new SagePayToken()
                    {
                        //account = cvm.CheckoutDetails.AccountNumber,
                        account = HttpContext.Current.Session["U_AccountNo"].ToString(),
                        email = cvm.CheckoutDetails.Email,
                        timestamp = DateTime.Now,
                        sid = "",
                        deleted = 0,
                        uid = cvm.CheckoutDetails.SagePayTxCode,
                        sp_uid = otr.transactionId,
                        sp_security_key = "",
                        token = otr.paymentMethod.card.cardIdentifier,
                        card_type = otr.paymentMethod.card.cardType.ToUpper(),
                        last_4_digits = otr.paymentMethod.card.lastFourDigits,
                        expiry_date = otr.paymentMethod.card.expiryDate,
                        used = 0,
                        websiteID = int.Parse(ConfigurationManager.AppSettings["WebsiteId"]),
                        acs_trans_id = otr.acsTransId,
                        ds_trans_id = otr.dsTransId,
                        scheme_trace_id = ""
                    };
                    if (cvm.CheckoutDetails.SaveThisCard)
                    {
                        EntityAccess.InsertSagePayToken(token);
                    }
                }

                cvm.CheckoutDetails.CardType = otr.paymentMethod.card.cardType;

                // Retrieve Security Key
                //cvm.CheckoutDetails.SagePaySecurityKey = GetSecurityKey(otr.transactionId, cvm);

                // Save the order to Axis here
                MyAccountViewModel.CreateCustomer(cvm.CheckoutDetails);
                // Perform tolerance check
                BasketTotals bt = Utilities.LoadSession<BasketTotals>("B_BasketTotals");
                PaymentToleranceCheck(cvm.CheckoutDetails.PaymentAmountPaid, bt.GrandTotalIncVat, cvm.CheckoutDetails.BackOfficeOrderRef);

                // Write Axis Order and Send Email
                var session = HttpContext.Current.Session;
                if (cvm.CheckoutDetails.SaveOrderCount == 0)
                {
                    bool orderSuccess = ProcessOrder(otr, cvm.CheckoutDetails);
                    bool isInterimOrder = ((CheckoutDetails)session["C_CheckoutDetails"]).IsInterimOrder;

                    if (!isInterimOrder &&
                        (!Convert.ToBoolean(session["U_IsPortalUser"])
                        || (Convert.ToBoolean(session["U_IsPortalUser"]) && !cvm.CheckoutDetails.SuppressEmail)))
                    {
                        Dictionary<string, string> checkoutData = DataCache.GetSectionData("CheckoutData");
                        Utilities.SendEmail(
                            Utilities.GetItemFromDict(checkoutData, "SalesEmail"),
                            cvm.CheckoutDetails.Email,
                            "Thank you for your order",
                            cvm.BasketEmail,
                            "transactional.emails@netgiant.com");
                    }

                    //Increment SaveOrderCount
                    cvm.CheckoutDetails.SaveOrderCount += 1;
                }

                sr.IsSuccess = true;
                return sr;
            }

            if (otr.status != "Rejected")
            {
                Utilities.LogInformationMessage("Opayo Rejection (Other) - Transaction Failed: " + otr.statusCode + " - " + otr.statusDetail);
            }
            sr.Message = otr.status;
            sr.Html = otr.statusDetail;
            return sr;
        }

        public static string GetSecurityKey(CheckoutDetails cd)
        {
            string post = "<command>getTransactionDetail</command>" +
                    "<vendor>" + ConfigurationManager.AppSettings["OpayoVendor"] + "</vendor>" +
                    "<user>" + ConfigurationManager.AppSettings["OpayoRepAPIUser"]  + "</user>" +
                    "<vpstxid>" + cd.SagePayUid.Replace("{","").Replace("}","") + "</vpstxid>" +
                    "<algorithm>sha256</algorithm>";

            string signature = Utilities.HashSha256String(post + "<password>" + ConfigurationManager.AppSettings["OpayoRepAPIPassword"]  + "</password>");

            post = "<vspaccess>" + post + "<signature>" + signature + "</signature></vspaccess>";

            string env = ConfigurationManager.AppSettings["Environment"] == "Live" ? "live" : "sandbox";
            RestClient client = new RestClient("https://" + env + ".opayo.eu.elavon.com/access/");
            var request = new RestRequest("access.htm");
            request.AddHeader("content-type", "application/x-www-form-urlencoded");
            request.AddParameter("XML", post);
            var response = client.Execute(request, RestSharp.Method.Post);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                XmlDocument xml = new XmlDocument();
                xml.LoadXml(response.Content);

                EntityAccess.InsertOpayoLog(JsonConvert.SerializeXmlNode(xml), cd.BackOfficeOrderRef, cd.MerchantSessionKey.merchantSessionKey, "Get Detail Response");

                // Check for valid response 
                if (xml.SelectSingleNode("vspaccess/errorcode").InnerText == "0000")
                {
                    return xml.SelectSingleNode("vspaccess/securitykey").InnerText;
                }
            }

            // Error
            Utilities.LogInformationMessage("Opayo Error - Unable to retrieve Security Key");
            return ConfigurationManager.AppSettings["Environment"] == "Live" ? "" : "NOTFOUND";
        }

        public static int SetJsonSession(bool isInsert, int id = 0)
        {
            var session = HttpContext.Current.Session;
            VoucherPromo v1 = (VoucherPromo)session["V_Voucher"];
            VoucherPromo v2 = null;
            if (v1 != null)
            {
                v2 = new VoucherPromo
                {
                    VoucherTypeFk = v1.VoucherTypeFk,
                    VoucherCode = v1.VoucherCode
                };
            }
            string json = "{\"C_CheckoutDetails\": " + JsonConvert.SerializeObject((CheckoutDetails)session["C_CheckoutDetails"]) + ", "
                          + "\"B_BasketArray\": " + JsonConvert.SerializeObject((List<BasketContents>)session["B_BasketArray"]) + ", "
                          + "\"B_BasketTotals\": " + JsonConvert.SerializeObject((BasketTotals)session["B_BasketTotals"]) + ", "
                          + "\"V_Voucher\": " + JsonConvert.SerializeObject(v2) + ", "
                          + "\"U_Name\": " + JsonConvert.SerializeObject(session["U_Name"]) + ", "
                          + "\"U_AccountNo\": " + JsonConvert.SerializeObject(session["U_AccountNo"]) + ", "
                          + "\"U_Record\": " + JsonConvert.SerializeObject(session["U_Record"]) + ", "
                          + "\"U_Email\": " + JsonConvert.SerializeObject(session["U_Email"]) + ", "
                          + "\"U_IsPortalUser\": " + JsonConvert.SerializeObject(session["U_IsPortalUser"]) + ", "
                          + "\"U_Authenticated\": " + JsonConvert.SerializeObject(session["U_Authenticated"]) + ", "
                          + "\"U_CSUser\": " + JsonConvert.SerializeObject(session["U_CSUser"]) + "}";

            JsonStore jsonStore = new JsonStore
            {
                JsonStoreId = id,
                DateTime = DateTime.Now,
                Json = json
            };
            if (isInsert)
            {
                EntityAccess.InsertJsonStore(jsonStore);
            }
            else
            {
                EntityAccess.UpdateJsonStore(jsonStore);
            }

            return jsonStore.JsonStoreId;
        }

        public static bool GetJsonSession(int id)
        {
            bool isSuccess = false;
            var session = HttpContext.Current.Session;
            JsonStore js = EntityAccess.ReadJsonStore(x => x.JsonStoreId == id).FirstOrDefault();
            if (js != null)
            {
                JObject o = JObject.Parse(js.Json);
                session["C_CheckoutDetails"] = JsonConvert.DeserializeObject<CheckoutDetails>(WebUtility.HtmlDecode(o["C_CheckoutDetails"].ToString()));
                session["B_BasketArray"] = JsonConvert.DeserializeObject<List<BasketContents>>(o["B_BasketArray"].ToString());
                session["B_BasketTotals"] = JsonConvert.DeserializeObject<BasketTotals>(o["B_BasketTotals"].ToString());
                session["V_Voucher"] = JsonConvert.DeserializeObject<VoucherPromo>(o["V_Voucher"].ToString());
                session["U_Name"] = o["U_Name"].ToString();
                session["U_AccountNo"] = o["U_AccountNo"].ToString();
                session["U_Record"] = o["U_Record"].ToString();
                session["U_Email"] = o["U_Email"].ToString();
                session["U_IsPortalUser"] = o["U_IsPortalUser"].ToString() != "" && Convert.ToBoolean(o["U_IsPortalUser"].ToString());
                session["U_Authenticated"] = o["U_Authenticated"].ToString() != "" && Convert.ToBoolean(o["U_Authenticated"].ToString());
                session["U_CSUser"] = o["U_CSUser"].ToString();

                isSuccess = true;
            }

            return isSuccess;
        }

        public static string GetCardTypeFromJsonSession(int id)
        {
            JsonStore js = EntityAccess.ReadJsonStore(x => x.JsonStoreId == id).FirstOrDefault();
            if (js != null)
            {
                JObject o = JObject.Parse(js.Json);
                CheckoutDetails cd = JsonConvert.DeserializeObject<CheckoutDetails>(WebUtility.HtmlDecode(o["C_CheckoutDetails"].ToString()));
                return cd.CardType.ToString();
            }
            return "";
        }

        private static bool ProcessOrder(OpayoTransactionResponse otr, CheckoutDetails cd)
        {
            if (Authentication.IsAuthenticated())
            {
                if (Authentication.FixTempRecord(HttpContext.Current.Session["U_Record"].ToString(), HttpContext.Current.Session["U_Email"].ToString()))
                {
                    cd.AccountNumber = HttpContext.Current.Session["U_AccountNo"].ToString();
                    cd.AccountRecord = HttpContext.Current.Session["U_Record"].ToString();
                }
            }
            else
            {
                //cd.AccountNumber = "0";
                //cd.AccountRecord = "0";
                //Utilities.LogInformationMessage("ProcessOrder - Not Authenticated");
                //return false;
            }

            cd.CardType = otr.paymentMethod.card.cardType;
            cd.CardLast4Digits =otr.paymentMethod.card.lastFourDigits;
            cd.SagePayAuthCode = otr.retrievalReference.ToString();
            cd.SagePayUid = "{" + otr.transactionId + "}";
            cd.SageToken = otr.paymentMethod.card.cardIdentifier;

            try
            {
                var order = Touchpoints.SaveOrder(cd, VMerchantWrapper.Entities.OrderStatus.Completed, cd.BackOfficeOrderRef);
            }
            catch (Exception ex)
            {
                // Do nothing
            }

            HttpContext.Current.Session["C_CheckoutDetails"] = cd;
            return true;
        }

        public static string GenerateTxCode()
        {
            string txCode = "";

            int diff = Convert.ToInt32((DateTime.Now - new DateTime(1970, 1, 1)).TotalSeconds);
            int rnd = new Random().Next(100000, 999999);
            txCode = ConfigurationManager.AppSettings["SagePayVendor"].ToLower() + "-" + diff.ToString() + "555" + "-" + rnd.ToString();

            return txCode;
        }

        private static bool PaymentToleranceCheck(decimal actualPayment, decimal expectedPayment, string orderRef)
        {
            bool isSuccess = true;
            // Check that the amount paid matches the basket amount
            BasketTotals bt = (BasketTotals)HttpContext.Current.Session["B_BasketTotals"];
            if (bt != null && Math.Abs(expectedPayment - actualPayment) > 1)
            {
                isSuccess = false;
                Utilities.LogInformationMessage(string.Format("Payment Mismatch Error: Order Number: {0}, Amount Paid: {1}, Basket Amount: {2}.", orderRef, actualPayment, expectedPayment));
            }

            return isSuccess;
        }

        public class MerchantSessionKey
        {
            public DateTime expiry { get; set; }
            public string merchantSessionKey { get; set; }
        }

        public class CardIdentifier
        {
            public string cardIdentifier { get; set; }
            public DateTime expiry { get; set; }
        }
        public class OpayoPaymentMethod
        {
            public OpayoCard card { get; set; }
        }

        public class OpayoCard
        {
            public string cardType { get; set; }
            public string lastFourDigits { get; set; }
            public string expiryDate { get; set; }
            public string merchantSessionKey { get; set; }
            public string cardIdentifier { get; set; }            
            public bool? save { get; set; } = null;
            public bool? reusable { get; set; } = null;
        }

        public class OpayoBillingAddress
        {
            public string address1 { get; set; }
            public string address2 { get; set; }
            public string address3 { get; set; }
            public string city { get; set; }
            public string postalCode { get; set; }
            public string country { get; set; }
            public string state { get; set; }
        }

        public class OpayoShippingDetails
        {
            public string recipientFirstName { get; set; }
            public string recipientLastName { get; set; }
            public string shippingAddress1 { get; set; }
            public string shippingAddress2 { get; set; }
            public string shippingAddress3 { get; set; }
            public string shippingCity { get; set; }
            public string shippingPostalCode { get; set; }
            public string shippingCountry { get; set; }
            public string shippingState { get; set; }
        }

        public class OpayoCredentialType
        {
            public string cofUsage { get; set; }
            public string initiatedType { get; set; }
            public string mitType { get; set; }
            public string recurringExpiry { get; set; }
            public string recurringFrequency { get; set; }
            public string purchaseInstalData { get; set; }
        }

        public class OpayoTransaction
        {
            public string transactionType { get; set; }
            public OpayoPaymentMethod paymentMethod { get; set; }
            public string vendorTxCode { get; set; }
            public Int32 amount { get; set; }
            public string currency { get; set; } = "GBP";
            public string description { get; set; }
            public string settlementReferenceText { get; set; }
            public string customerFirstName { get; set; }
            public string customerLastName { get; set; }
            public OpayoBillingAddress billingAddress { get; set; }
            public string entryMethod { get; set; }
            public string giftAid { get; set; }
            public string apply3DSecure { get; set; }
            public string applyAvsCvcCheck { get; set; }
            public string customerEmail { get; set; }
            public string customerPhone { get; set; }
            public OpayoShippingDetails shippingDetails { get; set; }
            public string referrerId { get; set; }
            public OpayoStrongCustomerAuthentication strongCustomerAuthentication { get; set; }
            public string customerMobilePhone { get; set; }
            public string customerWorkPhone { get; set; }
            public OpayoCredentialType credentialType { get; set; }
            public string fiRecipient { get; set; }
        }

        public class OpayoTransactionResponse
        {
            public string statusCode { get; set; }
            public string statusDetail { get; set; }
            public string transactionId { get; set; }
            public string transactionType { get; set; }
            public int retrievalReference { get; set; }
            public string bankAuthorisationCode { get; set; }
            public string bankResponseCode { get; set; }
            public OpayoPaymentMethod paymentMethod { get; set; }
            public OpayoAmount amount { get; set; }
            public string currency { get; set; }
            public string acsTransId { get; set; }
            public string dsTransId { get; set; }
            public string status { get; set; }
            public OpayoAvsCvCheck avsCvCheck { get; set; }
            public Opayo3DSecure threeDSecure { get; set; }
        }

        public class OpayoAmount
        {
            public int totalAmount { get; set; }
            public int saleAmount { get; set; }
            public int surchargeAmount { get; set; }
        }

        public class OpayoAvsCvCheck
        {
            public string status { get; set; }
            public string address { get; set; }
            public string postalCode { get; set; }
            public string securityCode { get; set; }
        }

        public class Opayo3DSecure
        {
            public string status { get; set; }
        }

        public class OpayoStrongCustomerAuthentication
        {
            public string notificationURL { get; set; }
            public string browserIP { get; set; }
            public string browserAcceptHeader { get; set; }
            public bool browserJavascriptEnabled { get; set; }
            public bool browserJavaEnabled { get; set; }
            public string browserLanguage { get; set; }
            public string browserColorDepth { get; set; }
            public string browserScreenHeight { get; set; }
            public string browserScreenWidth { get; set; }
            public string browserTZ { get; set; }
            public string browserUserAgent { get; set; }
            public string challengeWindowSize { get; set; }
            public string acctID { get; set; }
            public string transType { get; set; }
            public string threeDSRequestorAuthenticationInfo { get; set; }
            public string threeDSRequestorPriorAuthenticationInfo { get; set; }
            public string acctInfo { get; set; }
            public string merchantRiskIndicator { get; set; }
            public string threeDSExemptionIndicator { get; set; }
            public string website { get; set; }
        }

        public class OpayoChallengeAuthentication
        {
            public string statusCode { get; set; }
            public string statusDetail { get; set; }
            public string transactionId { get; set; }
            public string acsUrl { get; set; }
            public string acsTransId { get; set; }
            public string dsTransId { get; set; }
            public string status { get; set; }
            public string cReq { get; set; }
        }
    }
}
