using BusinessLogic.ViewModels;
using DataAccess.EntityFramework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using Newtonsoft.Json.Linq;

namespace BusinessLogic
{
    public class MySagePay
    {
        public static Dictionary<string, string> RegisterSagePay(CheckoutDetails cd, string action)
        {
            Dictionary<string, string> commonData = DataCache.GetSectionData("CommonData");

            string sToken = "";
            string sTokenDetails = "";
            string cardid = "";
            string sageurl = "";
            if (action == "saved" || action == "delete")
            {
                if (action == "saved")
                {
                    cardid = cd.SagePayCardId;
                }
                if (action == "delete")
                {
                    cardid = HttpContext.Current.Request["cardid"];
                }
                sToken = GetSagePayToken(cardid);
                sTokenDetails = "$$" + sToken;
            }

            string notificationArray = "?array=" + cd.BackOfficeCustRef + "$" + (HttpContext.Current.Session["U_AccountNo"] ?? "") + "$" + cd.Email + "$" + cd.JsonStoreId.ToString();
            string notificationURL = ConfigurationManager.AppSettings["SagePayNotificationURL"] + notificationArray;
            string customerOrderNo = cd.BackOfficeOrderRef;

            StringBuilder sb = new StringBuilder();
            try
            {
                if (action == "new" || action == "saved")
                {
                    sb.Append("VPSProtocol=3.00");
                    sb.Append("&TxType=PAYMENT");
                    sb.Append("&Vendor=" + ConfigurationManager.AppSettings["SagePayVendor"]);
                    sb.Append("&VendorTxCode=" + cd.SagePayTxCode);
                    sb.Append("&Amount=" + cd.TotalIncVat.ToString("N2"));
                    sb.Append("&Currency=GBP");
                    sb.Append("&Description=" + Utilities.GetItemFromDict(commonData, "SiteName").ToString() + "-" + customerOrderNo);
                    sb.Append("&NotificationURL=" + notificationURL);
                    sb.Append("&BillingSurname=" + cd.Name.Surname.Trim());
                    sb.Append("&BillingFirstnames=" + cd.Name.Firstname.Trim());
                    //Used as requested by D. Bailey on instruction from SagePay
                    int i = 1;
                    if (cd.BillingAddress.Line1.Any(c => char.IsDigit(c)))
                    {
                        sb.Append("&BillingAddress" + i.ToString() + "=" + cd.BillingAddress.Line1.Trim());
                        i = i + 1;
                    }
                    if (cd.BillingAddress.Line2.Any(c => char.IsDigit(c)))
                    {
                        sb.Append("&BillingAddress" + i.ToString() + "=" + cd.BillingAddress.Line2.Trim());
                        i = i + 1;
                    }
                    if (cd.BillingAddress.Line3.Any(c => char.IsDigit(c)) && i < 3)
                    {
                        sb.Append("&BillingAddress" + i.ToString() + "=" + cd.BillingAddress.Line3.Trim());
                        i = i + 1;
                    }
                    if (i == 1)
                    {
                        sb.Append("&BillingAddress" + i.ToString() + "=" + cd.BillingAddress.Line2.Trim());
                    }
                    //sb.Append("&BillingAddress1=" + cd.BillingAddress.Line2.Trim()); //Used as requested by D. Bailey on instruction from SagePay
                    sb.Append("&BillingCity=" + cd.BillingAddress.Line4.Trim());
                    sb.Append("&BillingPostCode=" + cd.BillingAddress.PostCode.Trim());
                    sb.Append("&BillingCountry=" + cd.BillingAddress.Country.Trim());
                    sb.Append("&DeliverySurname=" + cd.Name.Surname.Trim());
                    sb.Append("&DeliveryFirstnames=" + cd.Name.Firstname.Trim());
                    if (cd.DeliveryAddress.Line1 != "")
                    {
                        sb.Append("&DeliveryAddress1=" + cd.DeliveryAddress.Line1.Trim());
                        sb.Append("&DeliveryAddress2=" + cd.DeliveryAddress.Line2.Trim());
                    }
                    else
                    {
                        sb.Append("&DeliveryAddress1=" + cd.DeliveryAddress.Line2.Trim());
                    }
                    sb.Append("&DeliveryCity=" + cd.DeliveryAddress.Line4.Trim());
                    sb.Append("&DeliveryPostCode=" + cd.DeliveryAddress.PostCode.Trim());
                    sb.Append("&DeliveryCountry=" + cd.DeliveryAddress.Country.Trim());
                    sb.Append("&DeliveryPhone=" + cd.TelephoneNumber.Trim());
                    sb.Append("&CustomerEMail=" + cd.Email.Trim());
                    if (Convert.ToBoolean(HttpContext.Current.Session["U_IsPortalUser"]))
                    {
                        sb.Append("&Apply3DSecure=2");       // Bypass 3d Security for portal users
                    }
                    else
                    {
                        sb.Append("&Apply3DSecure=0");       // Request 3d Security for all users
                    }
                    sb.Append("&Profile=LOW");
                    sb.Append("&AccountType=E");
                    //sb.Append("&BasketXML=");           //No Used for RED fraud rules
                    sb.Append("&VendorData=" + customerOrderNo);

                    if (action == "saved")
                    {
                        // Using a saved card
                        sb.Append("&ApplyAVSCV2=2");
                        sb.Append("&StoreToken=1");
                        sb.Append("&Token=" + sToken);
                    }
                    else
                    {
                        // Using a new card
                        //sb.Append("&StoreToken=");
                        if (cd.SaveThisCard)
                        {
                            sb.Append("&CreateToken=1");
                        }
                    }
                    sageurl = ConfigurationManager.AppSettings["SagePayRegisterURL"];
                }
                if (action == "delete")
                {
                    sb.Append("VPSProtocol=3.00");
                    sb.Append("&TxType=REMOVETOKEN");
                    sb.Append("&Vendor=" + ConfigurationManager.AppSettings["SagePayVendor"]);
                    sb.Append("&Token=" + sToken);

                    sageurl = ConfigurationManager.AppSettings["SagePayDeleteTokenUrl"];

                    int sageid = int.Parse(cardid.Split('_')[0].ToString());
                    SagePayToken spt = EntityAccess.ReadSagePayTokens(x => x.id == sageid).FirstOrDefault();
                    if (spt != null)
                    {
                        spt.deleted = 1;
                        EntityAccess.DeleteSagePayToken(spt);
                    }
                }
            }
            catch (Exception e)
            {
                Utilities.ProcessException(e);
            }

            // Remove this
            //HttpContext.Current.Session["SagePost"] = sb.ToString();

            return PostSagePayRequest(sageurl, sb.ToString());
        }

        public static SagePayNotification ProcessNotification(HttpRequestBase request, CheckoutViewModel cvm)
        {
            // Note: When this page is launched wihout 'breakout=true' it will not be associated with the Session currently running for the customer. 
            // It will have it's own Session context. Hence, the usual set of Session variables will not be available.

            SagePayNotification spn = new SagePayNotification();
            spn.Breakout = false;
            if (request["breakout"] == "true")
            {
                var sb = new StringBuilder();
                sb.AppendLine("<script type=\"text/javascript\">");

                string addon = "form.submit();";

                if (request["error"] == "true")
                {
                    if (request["status"] != "ERROR")
                    {
                        if (request["status"] == "INVALID" && request["statusDetail"].StartsWith("9010: "))
                        {
                            addon = "window.parent.location = '/checkout/'";
                        }
                        else
                        {
                            addon = "window.parent.showSavedCards(\"" + request["statusDetail"] + "\");";
                        }
                    }
                    else
                    {
                        sb.AppendLine("var form = window.parent.document.getElementById('co-stage2');");
                        addon = "form.action='/Error/Index/1002/?status=" + request["status"] + "&statusDetail=" + request["statusDetail"] + "';form.submit();";

                    }
                }
                else
                {
                    addon = "window.parent.location = '/checkout/stage2?orderSuccess=true';";
                }

                // Fix fields in the CheckoutDetails
                CheckoutDetails cd = new CheckoutDetails();
                if (HttpContext.Current.Session["C_CheckoutDetails"] != null)
                {
                    cd = (CheckoutDetails)HttpContext.Current.Session["C_CheckoutDetails"];
                    cd.CardType = request["cardtype"];
                    cd.CardLast4Digits = request["last4"];
                    cd.SagePayTxCode = request["txcode"];
                    cd.SagePayAuthCode = request["authcode"];
                    cd.SageToken = request["token"];
                }

                spn.Breakout = true;
                sb.Append(addon);
                sb.AppendLine("</script>");
                spn.ResponseString = sb.ToString();
            }
            else
            {
                spn.PostString = request.Form.ToString();
                spn.Status = request["Status"]; 
                spn.VPSProtocol = request["VPSProtocol"];
                spn.TxType = request["TxType"];
                spn.VendorTxCode = request["VendorTxCode"];
                spn.VPSTxID = request["VPSTxID"];
                spn.StatusDetail = request["StatusDetail"];
                spn.TxAuthNo = request["TxAuthNo"] ?? "";
                spn.Token = request["Token"];
                spn.AVSCV2 = request["AVSCV2"];
                spn.AddressResult = request["AddressResult"];
                spn.PostCodeResult = request["PostCodeResult"];
                spn.CV2Result = request["CV2Result"];
                spn.GiftAid = request["GiftAid"];
                spn.ThreeDSecureStatus = request["3DSecureStatus"];
                spn.CAVV = request["CAVV"] ?? "";
                spn.AddressStatus = request["AddressStatus"];
                spn.PayerStatus = request["PayerStatus"];
                spn.CardType = request["CardType"];
                spn.Last4Digits = request["Last4Digits"];
                spn.VPSSignature = request["VPSSignature"];
                spn.FraudResponse = request["FraudResponse"];
                spn.Surcharge = request["Surcharge"];
                spn.ExpiryDate = request["ExpiryDate"];
                spn.BankAuthCode = request["BankAuthCode"];
                spn.DeclineCode = request["DeclineCode"];
                spn.SecurityKey = request["SecurityKey"] ?? "";

                if (!String.IsNullOrEmpty(request["array"]))
                {
                    string[] arr = request["array"].Split('$');
                    if (arr.Length == 4)
                    {
                        spn.DocId = arr[0];
                        spn.Account = arr[1];
                        spn.Email = arr[2];
                        spn.JsonStoreId = int.Parse(arr[3]);
                    }
                }

                string redirectUrl = "";
                string status = "";
                switch (spn.Status)
                {
                    case "OK":
                    case "OK REPEATED":
                        {
                            // Check if valid transaction
                            EntityAccess.ReadSagePayTransaction(x => x.protx_uid.ToLower() == spn.VendorTxCode.ToLower() && x.protx_key != null && x.protx_key != "");
                            redirectUrl = ConfigurationManager.AppSettings["SagePayNotificationURL"] + "?breakout=true&cardtype=" + spn.CardType + "&error=false" + "&last4=" + spn.Last4Digits + "&token=" + spn.Token + "&authcode=" + spn.TxAuthNo + "&txcode=" + spn.VendorTxCode;
                            status = "OK";

                            if (status == "OK")
                            {
                                MyAccountViewModel.CreateCustomer(cvm.CheckoutDetails);
                                // Write Axis Order and Send Email
                                var session = HttpContext.Current.Session;
                                bool orderSuccess = ProcessOrder(request);

                                if (orderSuccess && 
                                    (!Convert.ToBoolean(session["U_IsPortalUser"]) 
                                    || (Convert.ToBoolean(session["U_IsPortalUser"]) && !cvm.CheckoutDetails.SuppressEmail)))
                                {
                                    Utilities.SendEmail(Utilities.GetItemFromDict(cvm.CheckoutData, "SalesEmail"), cvm.CheckoutDetails.Email, "Thank you for your order", cvm.BasketEmail);
                                }
                            }
                            if (!string.IsNullOrEmpty(spn.Token))
                            {
                                SagePayToken token = new SagePayToken
                                {
                                    account = spn.Account,
                                    email = spn.Email,
                                    timestamp = DateTime.Now,
                                    sid = spn.SecurityKey,
                                    deleted = 0,
                                    uid = spn.VendorTxCode,
                                    sp_uid = spn.VPSTxID,
                                    sp_security_key = spn.SecurityKey,
                                    token = spn.Token,
                                    card_type = spn.CardType,
                                    last_4_digits = spn.Last4Digits,
                                    expiry_date = spn.ExpiryDate,
                                    used = 0,
                                    websiteID = int.Parse(ConfigurationManager.AppSettings["WebsiteId"])
                                };

                                WriteSagePayToken(token);
                                // Update the SagePay Token
                            }
                            break;
                        }
                    case "ABORT":
                    case "ERROR":
                    case "REJECTED":
                    case "NOTAUTHED":
                        {
                            status = "OK";
                            if (spn.Status == "ERROR")
                            {
                                status = "ERROR";
                                try
                                {
                                    throw new Exception(spn.StatusDetail);
                                }
                                catch (Exception e)
                                {
                                    Utilities.ProcessException(e);
                                }
                            }
                            redirectUrl = ConfigurationManager.AppSettings["SagePayNotificationURL"] + "?breakout=true&error=true&status=" + spn.Status + "&statusDetail=" + spn.StatusDetail.Replace(" : ", "");

                            break;
                        }
                }
                spn.ResponseString = "Status=" + status + "\r\nRedirectURL=" + redirectUrl + "\r\nStatusDetail=Done\r\n";

                WriteSagePayNotification(spn); 
            }
            
            return spn;
        }

        private static bool ProcessOrder(HttpRequestBase request)
        {
            CheckoutDetails cd = new CheckoutDetails();

            if (HttpContext.Current.Session["C_CheckoutDetails"] != null)
            {
                cd = (CheckoutDetails)HttpContext.Current.Session["C_CheckoutDetails"];
            }
            else
            {
                Utilities.LogInformationMessage("ProcessOrder - CheckoutDetails is null");
                return false;
            }

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
                Utilities.LogInformationMessage("ProcessOrder - Not Authenticated");
                return false;
            }

            cd.CardType = Convert.ToString(request["CardType"]);
            cd.SagePayTxCode = Convert.ToString(request["VendorTxCode"]);
            cd.CardLast4Digits = Convert.ToString(request["Last4Digits"]);
            cd.SagePayAuthCode = Convert.ToString(request["TxAuthNo"]);
            cd.SageToken = Convert.ToString(request["Token"]);

            try
            {
                var order = Touchpoints.SaveOrder(cd, VMerchantWrapper.Entities.OrderStatus.Completed, cd.BackOfficeOrderRef);

                if (!order.IsSuccess)
                { 
                    Utilities.LogInformationMessage("ProcessOrder - Order was not successful");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Utilities.ProcessException(ex);
                return false;
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


        private static string GetSagePayToken(string id)
        {
            string[] idArray = id.Split('_');
            int tokenId = int.Parse(idArray[0]);
            string sid = idArray[1];
            SagePayToken spt = EntityAccess.ReadSagePayTokens(x => x.id == tokenId && x.sid == sid).FirstOrDefault();

            return spt.token;
        }

        //private static string GenerateTXCode()
        //{
        //    string newTXCode = "";
        //    double dDiff;
        //    DateTime startDate = new DateTime(1970, 01, 01, 0, 0, 0);
        //    DateTime endDate = DateTime.Now;
        //    dDiff = (DateTime.Now - startDate).TotalSeconds;
        //    newTXCode = ConfigurationManager.AppSettings["SagePayVendor"].ToLower() + "-" + Math.Round(dDiff, 0).ToString() + "555" + "-" + Utilities.GetRandomNumber();

        //    return newTXCode;
        //}

        private static Dictionary<string, string> PostSagePayRequest(string url, string post)
        {
            ASCIIEncoding encoding = new ASCIIEncoding();
            byte[] data = encoding.GetBytes(post);

            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";
            req.ContentLength = data.Length;

            // Send the data.
            using (Stream dataStream = req.GetRequestStream())
            {
                dataStream.Write(data, 0, data.Length);
            }
            HttpWebResponse response = (HttpWebResponse)req.GetResponse();

            string responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

            Dictionary<string, string> dict = responseString.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
             .Select(x => x.Split('='))
             .ToDictionary(x => x[0], y => y[1]);
            dict.Add("PostString", post);

            return dict;
        }

        private static void WriteSagePayNotification(SagePayNotification spn)
        {
            EntityAccess.InsertSagePayTransaction(spn);
        }

        private static void WriteSagePayToken(SagePayToken token)
        {
            EntityAccess.InsertSagePayToken(token);
        }
    }
}
