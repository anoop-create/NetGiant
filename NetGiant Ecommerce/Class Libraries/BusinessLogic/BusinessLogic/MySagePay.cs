//using BusinessLogic.ViewModels;
//using DataAccess.EntityFramework;
//using Newtonsoft.Json;
//using System;
//using System.Collections.Generic;
//using System.Configuration;
//using System.IO;
//using System.Linq;
//using System.Net;
//using System.Text;
//using System.Web;
//using Newtonsoft.Json.Linq;
//using RestSharp;

//namespace BusinessLogic
//{
//    public class MySagePay
//    {
//        public static Dictionary<string, string> RegisterSagePay(CheckoutDetails cd, string action)
//        {
//            Dictionary<string, string> commonData = DataCache.GetSectionData("CommonData");

//            string sToken = "";
//            string sTokenDetails = "";
//            string sACSTransID = "";
//            string sSchemeTraceID = "SP999999999";
//            string cardid = "";
//            string sageurl = "";
//            if (action == "saved" || action == "delete")
//            {
//                if (action == "saved")
//                {
//                    cardid = cd.SagePayCardId;
//                }
//                if (action == "delete")
//                {
//                    cardid = HttpContext.Current.Request["cardid"];
//                }
//                SagePayToken spt = GetSagePayTokenNew(cardid);
//                sToken = spt.token;
//                sTokenDetails = "$$" + sToken;
//                sACSTransID = spt.acs_trans_id;
//                sSchemeTraceID = spt.scheme_trace_id;
//            }

//            string notificationArray = "?array=" + cd.BackOfficeCustRef + "$" + (HttpContext.Current.Session["U_AccountNo"] ?? "") + "$" + cd.Email + "$" + cd.JsonStoreId.ToString();
//            string notificationURL = ConfigurationManager.AppSettings["SagePayNotificationURL"] + notificationArray;
//            string customerOrderNo = cd.BackOfficeOrderRef;

//            StringBuilder sb = new StringBuilder();
//            try
//            {
//                if (action == "new" || action == "saved")
//                {
//                    sageurl = ConfigurationManager.AppSettings["SagePayRegisterURL"];
//                    sb.Append("VPSProtocol=4.00");
//                    sb.Append("&TxType=PAYMENT");
//                    sb.Append("&Vendor=" + ConfigurationManager.AppSettings["SagePayVendor"]);
//                    sb.Append("&VendorTxCode=" + cd.SagePayTxCode);
//                    sb.Append("&Amount=" + cd.TotalIncVat.ToString("N2"));
//                    sb.Append("&Currency=GBP");
//                    sb.Append("&Description=" + Utilities.GetItemFromDict(commonData, "SiteName").ToString() + "-" + customerOrderNo);
//                    sb.Append("&NotificationURL=" + notificationURL);
//                    sb.Append("&BillingSurname=" + HttpUtility.UrlEncode(cd.Name.Surname.Trim()));
//                    sb.Append("&BillingFirstnames=" + HttpUtility.UrlEncode(cd.Name.Firstname.Trim()));
//                    //Used as requested by D. Bailey on instruction from SagePay
//                    int i = 1;
//                    if (cd.BillingAddress.Line1.Any(c => char.IsDigit(c)))
//                    {
//                        sb.Append("&BillingAddress" + i.ToString() + "=" + HttpUtility.UrlEncode(cd.BillingAddress.Line1.Trim()));
//                        i = i + 1;
//                    }
//                    if (cd.BillingAddress.Line2.Any(c => char.IsDigit(c)))
//                    {
//                        sb.Append("&BillingAddress" + i.ToString() + "=" + HttpUtility.UrlEncode(cd.BillingAddress.Line2.Trim()));
//                        i = i + 1;
//                    }
//                    if (cd.BillingAddress.Line3.Any(c => char.IsDigit(c)) && i < 3)
//                    {
//                        sb.Append("&BillingAddress" + i.ToString() + "=" + HttpUtility.UrlEncode(cd.BillingAddress.Line3.Trim()));
//                        i = i + 1;
//                    }
//                    if (i == 1)
//                    {
//                        sb.Append("&BillingAddress" + i.ToString() + "=" + (HttpUtility.UrlEncode(cd.BillingAddress.Line2?.Trim()) ?? ""));
//                    }
//                    sb.Append("&BillingCity=" + HttpUtility.UrlEncode(cd.BillingAddress.Line4.Trim()));
//                    sb.Append("&BillingPostCode=" + HttpUtility.UrlEncode(cd.BillingAddress.PostCode.Trim()));
//                    sb.Append("&BillingCountry=" + cd.BillingAddress.Country.Trim());
//                    sb.Append("&DeliverySurname=" + HttpUtility.UrlEncode(cd.Name.Surname.Trim()));
//                    sb.Append("&DeliveryFirstnames=" + HttpUtility.UrlEncode(cd.Name.Firstname.Trim()));
//                    if (cd.DeliveryAddress.Line1 != "")
//                    {
//                        sb.Append("&DeliveryAddress1=" + HttpUtility.UrlEncode(cd.DeliveryAddress.Line1.Trim()));
//                        sb.Append("&DeliveryAddress2=" + HttpUtility.UrlEncode(cd.DeliveryAddress.Line2.Trim()));
//                    }
//                    else
//                    {
//                        sb.Append("&DeliveryAddress1=" + HttpUtility.UrlEncode(cd.DeliveryAddress.Line2.Trim()));
//                    }
//                    sb.Append("&DeliveryCity=" + HttpUtility.UrlEncode(cd.DeliveryAddress.Line4.Trim()));
//                    sb.Append("&DeliveryPostCode=" + HttpUtility.UrlEncode(cd.DeliveryAddress.PostCode.Trim()));
//                    sb.Append("&DeliveryCountry=" + cd.DeliveryAddress.Country.Trim());
//                    sb.Append("&DeliveryPhone=" + HttpUtility.UrlEncode(cd.TelephoneNumber.Trim()));
//                    sb.Append("&CustomerEMail=" + HttpUtility.UrlEncode(cd.Email.Trim()));
//                    if (Convert.ToBoolean(HttpContext.Current.Session["U_IsPortalUser"]))
//                    {
//                        sb.Append("&AccountType=M");
//                    }
//                    else
//                    {
//                        sb.Append("&AccountType=E");
//                    }
//                    sb.Append("&Profile=LOW");
//                    sb.Append("&VendorData=" + customerOrderNo);

//                    if (action == "saved")
//                    {
//                        // Using a saved card
//                        sb.Append("&ApplyAVSCV2=2");
//                        sb.Append("&StoreToken=1");
//                        sb.Append("&Token=" + sToken);
//                        sb.Append("&COFUsage=SUBSEQUENT");
//                        sb.Append("&InitiatedType=CIT");
//                        sb.Append("&MITType=UNSCHEDULED");
//                        if (!string.IsNullOrEmpty(sACSTransID))
//                        {
//                            sb.Append("&ThreeDSRequestorPriorAuthenticationInfoXML=<threeDSRequestorPriorAuthenticationInfo><threeDSReqPriorAuthMethod>02</threeDSReqPriorAuthMethod><threeDSReqPriorRef>" + sACSTransID + "</threeDSReqPriorRef></threeDSRequestorPriorAuthenticationInfo>");
//                        }
//                    }
//                    else
//                    {
//                        // Using a new card
//                        if (cd.SaveThisCard)
//                        {
//                            sb.Append("&CreateToken=1");
//                            sb.Append("&Apply3DSecure=1");
//                            sb.Append("&COFUsage=FIRST");
//                            sb.Append("&InitiatedType=CIT");
//                            sb.Append("&MITType=UNSCHEDULED");
//                        }
//                    }
//                    sb.Append("&TransType=01");                 
//                }
//                if (action == "delete")
//                {
//                    sb.Append("VPSProtocol=4.00");
//                    sb.Append("&TxType=REMOVETOKEN");
//                    sb.Append("&Vendor=" + ConfigurationManager.AppSettings["SagePayVendor"]);
//                    sb.Append("&Token=" + sToken);

//                    sageurl = ConfigurationManager.AppSettings["SagePayDeleteTokenUrl"];

//                    int sageid = int.Parse(cardid.Split('_')[0].ToString());
//                    SagePayToken spt = EntityAccess.ReadSagePayTokens(x => x.id == sageid).FirstOrDefault();
//                    if (spt != null)
//                    {
//                        spt.deleted = 1;
//                        EntityAccess.DeleteSagePayToken(spt);
//                    }
//                }
//            }
//            catch (Exception e)
//            {
//                Utilities.ProcessException(e, "sb = " + sb.ToString());
//            }

//            return PostSagePayRequest(sageurl, sb.ToString());
//        }

//        public static SagePayNotification ProcessNotification(HttpRequestBase request, CheckoutViewModel cvm)
//        {
//            // Note: When this page is launched it will not be associated with the Session currently running for the customer. 
//            // It will have it's own Session context. However, a partial restoration of the session variables has been done in the Controller.

//            //Utilities.WriteLogFile("MySagePay ProcessNotification");

//            SagePayNotification spn = new SagePayNotification();

//            spn.PostString = request.Form.ToString();
//            spn.VPSProtocol = request["VPSProtocol"];
//            spn.TxType = request["TxType"];
//            spn.VendorTxCode = request["VendorTxCode"];
//            spn.Status = request["Status"];
//            spn.StatusDetail = request["StatusDetail"];
//            spn.TxAuthNo = request["TxAuthNo"] ?? "";
//            spn.AVSCV2 = request["AVSCV2"];
//            spn.AddressResult = request["AddressResult"];
//            spn.PostCodeResult = request["PostCodeResult"];
//            spn.CV2Result = request["CV2Result"];
//            spn.GiftAid = request["GiftAid"];
//            spn.ThreeDSecureStatus = request["3DSecureStatus"];
//            spn.CAVV = request["CAVV"] ?? "";
//            spn.AddressStatus = request["AddressStatus"];
//            spn.PayerStatus = request["PayerStatus"];
//            spn.CardType = request["CardType"];
//            spn.Last4Digits = request["Last4Digits"];
//            spn.VPSSignature = request["VPSSignature"];
//            spn.FraudResponse = request["FraudResponse"];
//            spn.Surcharge = request["Surcharge"];
//            spn.DeclineCode = request["DeclineCode"];
//            spn.ExpiryDate = request["ExpiryDate"];
//            spn.BankAuthCode = request["BankAuthCode"];
//            spn.Token = request["Token"];

//            //New
//            spn.ACSTransID = request["ACSTransID"];
//            spn.DSTransID = request["DSTransID"];
//            spn.SchemeTraceID = request["SchemeTraceID"];

//            //Old ?
//            spn.VPSTxID = request["VPSTxID"];
//            spn.SecurityKey = request["SecurityKey"] ?? "";

//            if (!String.IsNullOrEmpty(request["array"]))
//            {
//                string[] arr = request["array"].Split('$');
//                if (arr.Length == 4)
//                {
//                    spn.DocId = arr[0];
//                    spn.Account = arr[1];
//                    spn.Email = arr[2];
//                    spn.JsonStoreId = int.Parse(arr[3]);
//                }
//            }

//            string redirectUrl = "";
//            string status = "";

//            switch (spn.Status)
//            {
//                case "OK":
//                case "OK REPEATED":
//                    {
//                        // Check if valid transaction
//                        EntityAccess.ReadSagePayTransaction(x => x.protx_uid.ToLower() == spn.VendorTxCode.ToLower() && x.protx_key != null && x.protx_key != "");
//                        redirectUrl = ConfigurationManager.AppSettings["SagePayBreakoutUrl"] + "?cardtype=" + spn.CardType + "&error=false" + "&last4=" + spn.Last4Digits + "&token=" + spn.Token + "&authcode=" + spn.TxAuthNo + "&txcode=" + spn.VendorTxCode + "&jsonstoreid=" + cvm.JsonStoreId;
//                        status = "OK";

//                        if (status == "OK")
//                        {
//                            MyAccountViewModel.CreateCustomer(cvm.CheckoutDetails);
//                            // Perform tolerance check
//                            BasketTotals bt = Utilities.LoadSession<BasketTotals>("B_BasketTotals");
//                            PaymentToleranceCheck(cvm.CheckoutDetails.PaymentAmountPaid, bt.GrandTotalIncVat, cvm.CheckoutDetails.BackOfficeOrderRef);

//                            // Write Axis Order and Send Email
//                            var session = HttpContext.Current.Session;
//                            if (cvm.CheckoutDetails.SaveOrderCount == 0)
//                            {
//                                bool orderSuccess = ProcessOrder(request);
//                                bool isInterimOrder = ((CheckoutDetails)session["C_CheckoutDetails"]).IsInterimOrder;

//                                if (!isInterimOrder &&
//                                    (!Convert.ToBoolean(session["U_IsPortalUser"])
//                                    || (Convert.ToBoolean(session["U_IsPortalUser"]) && !cvm.CheckoutDetails.SuppressEmail)))
//                                {
//                                    Utilities.SendEmail(
//                                        Utilities.GetItemFromDict(cvm.CheckoutData, "SalesEmail"),
//                                        cvm.CheckoutDetails.Email,
//                                        "Thank you for your order",
//                                        cvm.BasketEmail,
//                                        "transactional.emails@netgiant.com");
//                                }

//                                //Increment SaveOrderCount
//                                cvm.CheckoutDetails.SaveOrderCount += 1;
//                                MySagePay.SetJsonSession(false, spn.JsonStoreId);
//                            }
//                        }
//                        if (!string.IsNullOrEmpty(spn.Token))
//                        {
//                            SagePayToken token = new SagePayToken
//                            {
//                                account = spn.Account,
//                                email = spn.Email,
//                                timestamp = DateTime.Now,
//                                sid = spn.SecurityKey,
//                                deleted = 0,
//                                uid = spn.VendorTxCode,
//                                sp_uid = spn.VPSTxID,
//                                sp_security_key = spn.SecurityKey,
//                                token = spn.Token,
//                                card_type = spn.CardType,
//                                last_4_digits = spn.Last4Digits,
//                                expiry_date = spn.ExpiryDate,
//                                used = 0,
//                                websiteID = int.Parse(ConfigurationManager.AppSettings["WebsiteId"]),
//                                acs_trans_id = spn.ACSTransID,
//                                ds_trans_id = spn.DSTransID,
//                                scheme_trace_id = spn.SchemeTraceID
//                            };

//                            WriteSagePayToken(token);
//                        }
//                        break;
//                    }
//                case "ABORT":
//                case "ERROR":
//                case "INVALID":
//                case "REJECTED":
//                case "NOTAUTHED":
//                case "MALFORMED":
//                    {
//                        status = "OK";
//                        if (spn.Status == "ERROR")
//                        {
//                            status = "ERROR";
//                            try
//                            {
//                                throw new Exception(spn.StatusDetail);
//                            }
//                            catch (Exception e)
//                            {
//                                Utilities.ProcessException(e);
//                            }
//                        }
//                        redirectUrl = ConfigurationManager.AppSettings["SagePayBreakoutUrl"] + "?error=true&status=" + spn.Status + "&statusDetail=" + spn.StatusDetail.Replace(" : ", "") + "&jsonstoreid=" + cvm.JsonStoreId + "&errorRoute=1";
//                        break;
//                    }
//            }
//            spn.ResponseString = "Status=" + status + "\r\nRedirectURL=" + redirectUrl + "\r\nStatusDetail=Done\r\n";

//            WriteSagePayNotification(spn);
//            return spn;
//        }

//        private static bool ProcessOrder(HttpRequestBase request)
//        {
//            CheckoutDetails cd = new CheckoutDetails();

//            if (HttpContext.Current.Session["C_CheckoutDetails"] != null)
//            {
//                cd = (CheckoutDetails)HttpContext.Current.Session["C_CheckoutDetails"];
//            }
//            else
//            {
//                Utilities.LogInformationMessage("ProcessOrder - CheckoutDetails is null");
//                return false;
//            }

//            if (Authentication.IsAuthenticated())
//            {
//                if (Authentication.FixTempRecord(HttpContext.Current.Session["U_Record"].ToString(), HttpContext.Current.Session["U_Email"].ToString()))
//                {
//                    cd.AccountNumber = HttpContext.Current.Session["U_AccountNo"].ToString();
//                    cd.AccountRecord = HttpContext.Current.Session["U_Record"].ToString();
//                }
//            }
//            else
//            {
//                //cd.AccountNumber = "0";
//                //cd.AccountRecord = "0";
//                //Utilities.LogInformationMessage("ProcessOrder - Not Authenticated");
//                //return false;
//            }

//            cd.CardType = Convert.ToString(request["CardType"]);
//            cd.SagePayTxCode = Convert.ToString(request["VendorTxCode"]);
//            cd.CardLast4Digits = Convert.ToString(request["Last4Digits"]);
//            cd.SagePayAuthCode = Convert.ToString(request["TxAuthNo"]);
//            cd.SageToken = Convert.ToString(request["Token"]);

//            try
//            {
//                var order = Touchpoints.SaveOrder(cd, VMerchantWrapper.Entities.OrderStatus.Completed, cd.BackOfficeOrderRef);
//            }
//            catch (Exception ex)
//            {
//                // Do nothing
//            }

//            HttpContext.Current.Session["C_CheckoutDetails"] = cd;

//            return true;
//        }

//        //public static string GenerateTxCode()
//        //{
//        //    string txCode = "";

//        //    int diff = Convert.ToInt32((DateTime.Now - new DateTime(1970, 1, 1)).TotalSeconds);
//        //    int rnd = new Random().Next(100000, 999999);
//        //    txCode = ConfigurationManager.AppSettings["SagePayVendor"].ToLower() + "-" + diff.ToString() + "555" + "-" + rnd.ToString();

//        //    return txCode;
//        //}

//        public static int SetJsonSession(bool isInsert, int id = 0)
//        {
//            var session = HttpContext.Current.Session;
//            VoucherPromo v1 = (VoucherPromo)session["V_Voucher"];
//            VoucherPromo v2 = null;
//            if (v1 != null)
//            {
//                v2 = new VoucherPromo
//                {
//                    VoucherTypeFk = v1.VoucherTypeFk,
//                    VoucherCode = v1.VoucherCode
//                };
//            }
//            string json = "{\"C_CheckoutDetails\": " + JsonConvert.SerializeObject((CheckoutDetails)session["C_CheckoutDetails"]) + ", "
//                          + "\"B_BasketArray\": " + JsonConvert.SerializeObject((List<BasketContents>)session["B_BasketArray"]) + ", "
//                          + "\"B_BasketTotals\": " + JsonConvert.SerializeObject((BasketTotals)session["B_BasketTotals"]) + ", "
//                          + "\"V_Voucher\": " + JsonConvert.SerializeObject(v2) + ", "
//                          + "\"U_AccountNo\": " + JsonConvert.SerializeObject(session["U_AccountNo"]) + ", "
//                          + "\"U_Record\": " + JsonConvert.SerializeObject(session["U_Record"]) + ", "
//                          + "\"U_Email\": " + JsonConvert.SerializeObject(session["U_Email"]) + ", "
//                          + "\"U_IsPortalUser\": " + JsonConvert.SerializeObject(session["U_IsPortalUser"]) + ", "
//                          + "\"U_Authenticated\": " + JsonConvert.SerializeObject(session["U_Authenticated"]) + ", "
//                          + "\"U_CSUser\": " + JsonConvert.SerializeObject(session["U_CSUser"]) + "}";

//            JsonStore jsonStore = new JsonStore
//            {
//                JsonStoreId = id,
//                DateTime = DateTime.Now,
//                Json = json
//            };
//            if (isInsert)
//            {
//                EntityAccess.InsertJsonStore(jsonStore);
//            }
//            else
//            {
//                EntityAccess.UpdateJsonStore(jsonStore);
//            }

//            return jsonStore.JsonStoreId;
//        }

//        public static bool GetJsonSession(int id)
//        {
//            bool isSuccess = false;
//            var session = HttpContext.Current.Session;
//            JsonStore js = EntityAccess.ReadJsonStore(x => x.JsonStoreId == id).FirstOrDefault();
//            if (js != null)
//            {
//                JObject o = JObject.Parse(js.Json);
//                session["C_CheckoutDetails"] = JsonConvert.DeserializeObject<CheckoutDetails>(WebUtility.HtmlDecode(o["C_CheckoutDetails"].ToString()));
//                session["B_BasketArray"] = JsonConvert.DeserializeObject<List<BasketContents>>(o["B_BasketArray"].ToString());
//                session["B_BasketTotals"] = JsonConvert.DeserializeObject<BasketTotals>(o["B_BasketTotals"].ToString());
//                session["V_Voucher"] = JsonConvert.DeserializeObject<VoucherPromo>(o["V_Voucher"].ToString());
//                session["U_AccountNo"] = o["U_AccountNo"].ToString();
//                session["U_Record"] = o["U_Record"].ToString();
//                session["U_Email"] = o["U_Email"].ToString();
//                session["U_IsPortalUser"] = o["U_IsPortalUser"].ToString() != "" && Convert.ToBoolean(o["U_IsPortalUser"].ToString());
//                session["U_Authenticated"] = o["U_Authenticated"].ToString() != "" && Convert.ToBoolean(o["U_Authenticated"].ToString());
//                session["U_CSUser"] = o["U_CSUser"].ToString();

//                isSuccess = true;
//            }

//            return isSuccess;
//        }

//        public static string GetJsonObject(int id)
//        {
//            JsonStore js = EntityAccess.ReadJsonStore(x => x.JsonStoreId == id).FirstOrDefault();
//            if (js != null)
//            {
//                return js.Json;
//            }

//            return "";
//        }

//        private static bool PaymentToleranceCheck(decimal actualPayment, decimal expectedPayment, string orderRef)
//        {
//            bool isSuccess = true;
//            // Check that the amount paid matches the basket amount
//            BasketTotals bt = (BasketTotals)HttpContext.Current.Session["B_BasketTotals"];
//            if (bt != null && Math.Abs(expectedPayment - actualPayment) > 1)
//            {
//                isSuccess = false;
//                Utilities.LogInformationMessage(string.Format("Payment Mismatch Error: Order Number: {0}, Amount Paid: {1}, Basket Amount: {2}.", orderRef, actualPayment, expectedPayment));
//            }

//            return isSuccess;
//        }

//        private static string GetSagePayToken(string id)
//        {
//            string[] idArray = id.Split('_');
//            int tokenId = int.Parse(idArray[0]);
//            string sid = idArray[1];
//            SagePayToken spt = EntityAccess.ReadSagePayTokens(x => x.id == tokenId && x.sid == sid).FirstOrDefault();

//            return spt.token;
//        }

//        private static SagePayToken GetSagePayTokenNew(string id)
//        {
//            string[] idArray = id.Split('_');
//            int tokenId = int.Parse(idArray[0]);
//            string sid = idArray[1];
//            SagePayToken spt = EntityAccess.ReadSagePayTokens(x => x.id == tokenId && x.sid == sid).FirstOrDefault();

//            return spt;
//        }

//        private static Dictionary<string, string> PostSagePayRequest(string url, string post)
//        {
//            Utilities.SetTlsVersion();
//            var client = new RestClient(url);
//            var request = new RestRequest("", Method.Post);
//            request.AddHeader("content-type", "application/x-www-form-urlencoded");
//            request.AddParameter("application/x-www-form-urlencoded", post, ParameterType.RequestBody);
//            var response = client.Execute(request, Method.Post);

//            Dictionary<string, string> dict = response.Content.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
//             .Select(x => x.Split('='))
//             .ToDictionary(x => x[0], y => y[1]);
//            dict.Add("PostString", post);

//            return dict;
//        }

//        private static void WriteSagePayNotification(SagePayNotification spn)
//        {
//            EntityAccess.InsertSagePayTransaction(spn);
//        }

//        private static void WriteSagePayToken(SagePayToken token)
//        {
//            EntityAccess.InsertSagePayToken(token);
//        }
//    }
//}
