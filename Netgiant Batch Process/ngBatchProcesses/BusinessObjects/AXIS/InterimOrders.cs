using Google.Apis.Util;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ngBatchProcesses.BusinessObjects.Shared;
using NGBP.DataAccessLayer.DataUtilities;
using RestSharp;
using RestSharp.Authenticators;
using RestSharp.Serializers.NewtonsoftJson;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using VMerchantWrapper.Entities;
using VMerchantWrapper.Framework;
using static VMerchantWrapper.Entities.UserIdentity;
using VmOrder = VMerchantWrapper.Entities.Order;
using VmUser = VMerchantWrapper.Entities.User;
using VmVoucherType = VMerchantWrapper.Entities.VoucherType;

namespace ngBatchProcesses.BusinessObjects.Axis
{
    public class InterimOrders
    {
        public InterimOrders(Dictionary<string, string> parms)
        {
            Parms = parms;
            WebsiteId = Parms.ContainsKey("websiteid") ? Int32.Parse(Parms["websiteid"]) : 0;
            ErrorHasOccurred = false;
            ConnName = "netgiantMasterData";
            Settings = Properties.Settings.Default;
            ErrorCount = 0;
            SuccessCount = 0;
        }

        public bool ErrorHasOccurred { get; set; }
        public string ConnName { get; set; }
        Properties.Settings Settings { get; }
        public int WebsiteId { get; set; }
        public Dictionary<string, string> Parms { get; set; }
        public int ErrorCount { get; set; }
        public int SuccessCount { get; set; }
        public Dictionary<string, string> CheckoutData { get; set; }
        public Dictionary<string, string> CommonData { get; set; }
        public Dictionary<string, string> EmailData { get; set; }
        public DateTime OrderDate { get; set; }
        public string PaymentMethod { get; set; }
        public bool ZeroStock { get; set; }
        public List<product> ProductList { get; set; }
        public Service VMerchantService { get; set; }

        public void LoadInterimOrders()
        {
            // Get SQL connection name and Stored Orders
            List<InterimOrder> lio = new List<InterimOrder>();
            ConnName = EntityFunctions.GetWebsiteList(x => x.WebsiteID == WebsiteId).FirstOrDefault().WebsiteName;
            lio = EntityFunctions.GetInterimOrder(x => x.WebsiteFk == WebsiteId && !x.IsOrdered)
                    .OrderByDescending(x => x.InterimOrderTypeFk).ThenBy(x => x.DateTime)
                    .ToList();

            // Bomb out if there's nothing to do
            if (lio.Count == 0)
            {
                return;
            }

            StandardFunctions.WriteProcessStarted();

            CheckoutData = EntityFunctions.GetAllCmsEntry(WebsiteId, "CheckoutData");
            CommonData = EntityFunctions.GetAllCmsEntry(WebsiteId, "CommonData");
            EmailData = EntityFunctions.GetAllCmsEntry(WebsiteId, "EmailData");

            using (var axisDataConnection =
                new SqlConnection(ConfigurationManager.ConnectionStrings[ConnName].ToString()))
            {
                axisDataConnection.Open();

                var boLicence = ConfigurationManager.AppSettings["BoLicense"];
                VMerchantService = new Service(axisDataConnection, null);

                foreach (InterimOrder io in lio)
                {
                    if (io.InterimOrderTypeFk == 1)
                    {
                        ProcessStoredUser(io);
                    }

                    if (io.InterimOrderTypeFk == 0)
                    {
                        ProcessStoredOrder(io);
                    }
                }
                axisDataConnection.Close();
            }

            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Success count: " + SuccessCount });
            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Error count: " + ErrorCount });
            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
        }


        private void ProcessStoredUser(InterimOrder io)
        {
            JObject jo = JsonConvert.DeserializeObject<JObject>(io.Json);
            VmUser newUser = JsonConvert.DeserializeObject<VmUser>(jo["CustomerObject"].ToString());
            string pw = jo["PlainPass"].ToString();

            if (UserExists(newUser))
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = newUser.Email + " customer already exists in Diplomat" });
                MarkAsOrdered(io);
                return;
            }

            // Fix Fields
            FixFields(newUser);

            newUser.HashPassword(pw);

            try
            {
                VMerchantService.SaveUser(newUser);

                //Beyond this point: success!
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = newUser.Email + " sucessfully added" });
                SuccessCount += 1;
                MarkAsOrdered(io);
            }
            catch (Exception ex)
            {
                ErrorCount += 1;
                StandardFunctions.WriteException(ex);
            }
        }

        private void ProcessStoredOrder(InterimOrder io)
        {
            OrderDate = io.DateTime;
            JObject jo = JsonConvert.DeserializeObject<JObject>(io.Json);
            VmOrder newOrder = JsonConvert.DeserializeObject<VmOrder>(jo["OrderObject"].ToString());

            // Place the order On Hold
            List<BasketItem> lbi = newOrder.Basket.Items.ToList();
            lbi.Add(new BasketItem()
            {
                Reference = "INTERIMORD",
                Quantity = 1,
                UnitPrice = 0,
                Net = 0,
                Vat = 0,
                VatRate = 20,
                Description = "Interim Order",
                Notes = "",
                ItemType = ItemType.Stock,
                VoucherNet = 0,
                VoucherVat = 0,
            });
            newOrder.Basket.Items = lbi;
            if (newOrder.OrderNumber == "Interim Order")
            {
                newOrder.OrderNumber = null;
            }

            // Fix Fields
            FixFields(newOrder);

            try
            {
                VMerchantService.SaveOrder(newOrder);

                //Beyond this point: success!
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = newOrder.OrderNumber + " sucessfully added" });
                SuccessCount += 1;
                MarkAsOrdered(io);

                ZeroStock = false;
                if (CheckZeroOrSpecial(newOrder.Basket.Items))
                {
                    ZeroStock = true;
                }
                PaymentMethod = string.IsNullOrEmpty(newOrder.PayPalUidCode) ? "CreditDebit" : "PayPal";
                Email.SendEmail(
                    new List<string>() { newOrder.DeliveryUser.Email },
                    CheckoutData["SalesEmail"],
                    "Thank you for your order",
                    BuildConfirmationEmail(newOrder),
                    true);
            }
            catch (Exception ex)
            {
                ErrorCount += 1;
                StandardFunctions.WriteException(ex);
            }
        }

        private void MarkAsOrdered(InterimOrder io)
        {
            io.IsOrdered = true;
            if (!EntityFunctions.SaveInterimOrder(io))
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Error updating InterimOrder table for InterimOrderId " + io.InterimOrderId.ToString() });
            }
        }

        private int GetAvailablity(product p)
        {
            if (p.AxisFields.defaultDeliveryToCust > 9) return p.AxisFields.defaultDeliveryToCust ?? 0;
            if (p.supplierStock > 0) return 1;
            if (p.AxisFields.defaultDeliveryToCust == null) return 3;

            return (p.AxisFields.defaultDeliveryToCust ?? 0) + 2;
        }

        private bool UserExists(VmUser user)
        {
            string sql = @"SELECT TOP 1 U.email, U.record, U.account FROM dbo.Users U 
                    LEFT OUTER JOIN dbo.user_log UL ON UL.account = U.account
                    WHERE U.email = '" + user.Email + @"'
                    ORDER BY U.main_contact DESC, UL.lastlogin DESC";
            try
            {
                DataSet ds = SQLUtilities.ExecuteReadInline(ConnName, sql, "ds");
                return (ds.Tables[0].Rows.Count > 0);
            }
            catch (Exception)
            {
                // Do nothing
            }
            return false;
        }
        private void FixFields(VmUser user)
        {
            // Password
            user.HashPassword("Dummy");

            // Fix Telephone Number for DEV transactions
            if (ConfigurationManager.AppSettings["Environment"] != "Live")
            {
                user.TelephoneNumber = "01942880020"; //  <-- A valid telephone number
            }
        }
        private void FixFields(VmOrder order)
        {
            // Password
            order.BillingUser.HashPassword("Dummy");
            order.DeliveryUser.HashPassword("Dummy");

            // Record Number
            if (!order.BillingUser.RecordNumber.Contains("/"))
            {
                string sql = @"SELECT TOP 1 U.email, U.record, U.account FROM dbo.Users U 
                    LEFT OUTER JOIN dbo.user_log UL ON UL.account = U.account
                    WHERE U.email = '" + order.DeliveryUser.Email + @"'
                    ORDER BY U.main_contact DESC, UL.lastlogin DESC";
                try
                {
                    DataSet ds = SQLUtilities.ExecuteReadInline(ConnName, sql, "ds");
                    order.BillingUser.RecordNumber = ds.Tables[0].Rows[0]["record"].ToString();
                    order.DeliveryUser.RecordNumber = ds.Tables[0].Rows[0]["record"].ToString();
                }
                catch (Exception)
                {
                    // Do nothing
                }
            }

            // Email Address
            if (string.IsNullOrEmpty(order.BillingUser.Email) || string.IsNullOrEmpty(order.DeliveryUser.Email))
            {
                if (order.BillingUser.RecordNumber.Contains("/"))
                {
                    string sql = @"SELECT TOP 1 U.email, U.record, U.account FROM dbo.Users U 
                        LEFT OUTER JOIN dbo.user_log UL ON UL.account = U.account
                        WHERE U.record = '" + order.BillingUser.RecordNumber + @"'
                        ORDER BY U.main_contact DESC, UL.lastlogin DESC";
                    try
                    {
                        DataSet ds = SQLUtilities.ExecuteReadInline(ConnName, sql, "ds");
                        if (string.IsNullOrEmpty(order.BillingUser.Email))
                        {
                            if (ds.Tables[0].Rows.Count > 0)
                            {
                                order.BillingUser.Email = string.IsNullOrEmpty(ds.Tables[0].Rows[0]["email"].ToString()) ? order.DeliveryUser.Email : ds.Tables[0].Rows[0]["email"].ToString();
                            }
                            else
                            {
                                order.BillingUser.Email = order.DeliveryUser.Email;
                            }
                        }
                        if (string.IsNullOrEmpty(order.DeliveryUser.Email))
                        {
                            order.DeliveryUser.Email = ds.Tables[0].Rows[0]["email"].ToString();
                        }
                    }
                    catch (Exception)
                    {
                        // Do nothing
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(order.BillingUser.Email))
                    {
                        order.BillingUser.Email = order.DeliveryUser.Email;
                    }
                }
            }

            // Fix Security Key
            if (string.IsNullOrEmpty(order.SagePaySecurityKey))
            {
                order.SagePaySecurityKey = GetSecurityKey(order.SagePayCcUidId);
            }

            // Fix Telephone Number for DEV transactions
            if (ConfigurationManager.AppSettings["Environment"] != "Live")
            {
                order.BillingUser.TelephoneNumber = "01942880020"; //  <-- A valid telephone number
            }
        }

        private string GetSecurityKey(string transactionId)
        {
            string vendor = EntityFunctions.GetConfigurationSetting("Website Application Variables", "OpayoVendor", WebsiteId);
            string apiUser = EntityFunctions.GetConfigurationSetting("Website Application Variables", "OpayoRepAPIUser", WebsiteId);
            string pwd = EntityFunctions.GetConfigurationSetting("Website Application Variables", "OpayoRepAPIPassword", WebsiteId);

            string post = "<command>getTransactionDetail</command>" +
                    "<vendor>" + vendor + "</vendor>" +
                    "<user>" + apiUser + "</user>" +
                    "<vpstxid>" + transactionId + "</vpstxid>" +
                    "<algorithm>sha256</algorithm>";

            string signature = StandardFunctions.HashSha256String(post + "<password>" + pwd + "</password>");

            post = "<vspaccess>" + post + "<signature>" + signature + "</signature></vspaccess>";

            string env = ConfigurationManager.AppSettings["Environment"] == "Live" ? "live" : "sandbox";
            RestClient client = new RestClient("https://" + env + ".opayo.eu.elavon.com/access/", configureSerialization: s => s.UseNewtonsoftJson());
            var request = new RestRequest("access.htm");
            request.AddHeader("content-type", "application/x-www-form-urlencoded");
            request.AddParameter("XML", post);
            var response = client.Execute(request, RestSharp.Method.Post);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                XmlDocument xml = new XmlDocument();
                xml.LoadXml(response.Content);

                // Check for valid response 
                if (xml.SelectSingleNode("vspaccess/errorcode").InnerText == "0000")
                {
                    return xml.SelectSingleNode("vspaccess/securitykey").InnerText;
                }
            }

            // Error
            return "";
        }

        private string BuildConfirmationEmail(VmOrder order)
        {
            string template = EmailData["OrderConfirmation"];
            StringBuilder sb = new StringBuilder(template);

            sb.Replace("[SiteName]", CommonData["SiteName"]);
            sb.Replace("[OrderRef]", order.OrderNumber);
            sb.Replace("[YourOrderRef]", string.IsNullOrEmpty(order.CustomerReference) ? order.OrderNumber : order.CustomerReference);
            sb.Replace("[OrderDate]", OrderDate.ToString("dd MMM yyyy HH:mm:ss"));
            sb.Replace("[DeliveryName]", order.DeliveryUser.FirstName + " " + order.DeliveryUser.Surname);
            sb.Replace("[DeliveryLine1]", order.DeliveryUser.Address.Line1);
            sb.Replace("[DeliveryLine2]", order.DeliveryUser.Address.Line2);
            sb.Replace("[DeliveryLine3]", order.DeliveryUser.Address.Line3);
            sb.Replace("[DeliveryLine4]", order.DeliveryUser.Address.Line4);
            sb.Replace("[DeliveryLine5]", order.DeliveryUser.Address.Line5);
            sb.Replace("[DeliveryPostcode]", order.DeliveryUser.Address.Postcode);
            sb.Replace("[BillingName]", order.BillingUser.FirstName + " " + order.BillingUser.Surname);
            sb.Replace("[BillingLine1]", order.BillingUser.Address.Line1);
            sb.Replace("[BillingLine2]", order.BillingUser.Address.Line2);
            sb.Replace("[BillingLine3]", order.BillingUser.Address.Line3);
            sb.Replace("[BillingLine4]", order.BillingUser.Address.Line4);
            sb.Replace("[BillingLine5]", order.BillingUser.Address.Line5);
            sb.Replace("[BillingPostcode]", order.BillingUser.Address.Postcode);
            sb.Replace("[TelephoneNumber]", CommonData["TelephoneNumber"]);
            sb.Replace("[SupportEmail]", CommonData["SupportEmail"]);
            sb.Replace("[BasketContents]", BuildBasketDetails(order));

            if (order.PaymentSource == PaymentSource.Telephone)
            {
                sb.Replace("[PaymentMethod]", CheckoutData["ConfirmPhone1"].Replace("[Tel-No]", CommonData["TelephoneNumber"]) + "<br/>");
            }
            else if (order.PaymentSource == PaymentSource.Bacs)
            {
                sb.Replace("[PaymentMethod]", CheckoutData["ConfirmBACS"].Replace("[Tel-No]", CommonData["TelephoneNumber"]).Replace("[Ref-No]", order.OrderNumber).Replace("[Amt-Payable]", "£" + order.Net.ToString("0.00")) + "<br/>");
            }
            else if (order.PaymentSource == PaymentSource.None) // AccountApplication
            {
                sb.Replace("[PaymentMethod]", CheckoutData["ConfirmAccountApplication"] + "<br/>");
            }
            else
            {
                sb.Replace("[PaymentMethod]", "");
            }

            // Special Messages
            StringBuilder sm = new StringBuilder();
            if (ZeroStock)
            {
                sm.Append(CheckoutData["ConfirmZeroStock"] + "<br/>");
            }
            if (sm.Length != 0)
            {
                sm.Insert(0, "<br/>");
            }
            sb.Replace("[SpecialMessages]", sm.ToString());

            return sb.ToString();

        }

        private string BuildBasketDetails(VmOrder order)
        {
            string template = EmailData["OrderConfirmationBasket"];
            StringBuilder sb = new StringBuilder(template);

            BasketItem delivery = order.Basket.Items.FirstOrDefault(x => x.ItemType == ItemType.Delivery);
            BasketItem voucher = order.Basket.Items.FirstOrDefault(x => x.ItemType == ItemType.Voucher);

            if (voucher == null)
            {
                sb.Replace("[VoucherDetail]", "");
            }
            else
            {
                sb.Replace("[VoucherDetail]", BuildVoucherDetail(voucher));
            }
            sb.Replace("[DeliveryDescription]", delivery.Description);
            sb.Replace("[DeliveryPrice]", Math.Round(delivery.Net, 2).ToString("0.00"));
            sb.Replace("[SubTotal]", Math.Round((order.Net), 2).ToString("0.00"));
            sb.Replace("[TotalVAT]", Math.Round(order.Vat, 2).ToString("0.00"));
            sb.Replace("[GrandTotal]", Math.Round(order.Net + order.Vat, 2).ToString("0.00"));
            if (PaymentMethod == "Phone" || PaymentMethod == "BACS")
            {
                sb.Replace("[PhoneBACS]", "<p style=\"font-family:arial, verdana, helvetica, sans-serif; font-size:12px; text-align:left; \">" +
                    "<font face=\"Arial, sans-serif\" > **Please note this order will not be dispatched until payment has been made.**</ font > " +
                    "<o:p ></o:p></p>");
            }
            else
            {
                sb.Replace("[PhoneBACS]", "");
            }

            StringBuilder sb1 = new StringBuilder();
            foreach (BasketItem item in order.Basket.Items)
            {
                int i = 0;
                if (item.ItemType == ItemType.Stock && int.TryParse(item.Reference, out i))
                {
                    sb1.Append(BuildBasketRepeat(item));
                }
            }
            sb.Replace("[BasketRepeat]", sb1.ToString());

            return sb.ToString();
        }

        private string BuildBasketRepeat(BasketItem item)
        {
            string template = EmailData["OrderConfirmationRepeat"];
            StringBuilder sb = new StringBuilder(template);

            //sb.Replace("[ItemPartNo]", item.Reference);
            sb.Replace("[ItemPartNo]", ProductList.FirstOrDefault(x => x.AxisFields.stockReference == item.Reference).partNo);
            sb.Replace("[ItemDescription]", item.Description);
            sb.Replace("[ItemQuantity]", item.Quantity.ToString());
            sb.Replace("[ItemTotal]", Math.Round(item.Net, 2).ToString("0.00"));
            switch (ItemNotAvailable(item))
            {
                case 0:
                    {
                        sb.Replace("[ItemAvailability]", "<td width=\"60%\" colspan=\"4\"><p style=\"font-family: arial, helvetica, sans-serif; font-size: 12px; margin: 0; width: 100px; text-align: left; background-color: #fc6365; color: #ffffff; padding: 8px; text-align: center \">Out of Stock</p></td>");
                        break;
                    }
                case -1:
                    {
                        sb.Replace("[ItemAvailability]", "<td width=\"60%\" colspan=\"4\"><p style=\"font-family: arial, helvetica, sans-serif; font-size: 12px; margin: 0; width: 100px; text-align: left; background-color: #fc6365; color: #ffffff; padding: 8px; text-align: center \">Special Order</p></td>");
                        break;
                    }

                case 1:
                    {
                        sb.Replace("[ItemAvailability]", "<td width=\"60%\" colspan=\"4\"><p style=\"font-family: arial, helvetica, sans-serif; font-size: 12px; margin: 0; width: 100px; text-align: left; background-color: #fc6365; color: #ffffff; padding: 8px; text-align: center \">In stock</p></td>");
                        break;
                    }

            }

            return sb.ToString();
        }

        private string BuildVoucherDetail(BasketItem item)
        {
            string template = EmailData["OrderConfirmationVoucher"];
            StringBuilder sb = new StringBuilder(template);

            sb.Replace("[VoucherPrice]", Math.Round(item.Net, 2).ToString("0.00"));

            return sb.ToString();
        }

        private int ItemNotAvailable(BasketItem item)
        {
            if (item.Net == 0) return 1;
            product p = ProductList.FirstOrDefault(x => x.AxisFields.stockReference == item.Reference);

            return CheckZeroOrSpecial(p);
        }

        private bool CheckZeroOrSpecial(IEnumerable<BasketItem> Items)
        {
            List<string> lref = Items.Select(x => x.Reference).ToList();
            ProductList = new List<product>();
            ProductList = EntityFunctions.GetProduct(x => lref.Contains(x.AxisFields.stockReference));
            foreach (product p in ProductList)
            {
                if (CheckZeroOrSpecial(p) > 0) return true;
            }

            return false;
        }

        private int CheckZeroOrSpecial(product p)
        {
            List<int> noStock = new List<int> { 2, 3, 4, 8, 11, 12 }; // Availability codes which indicate 0 stock
            int availability = GetAvailablity(p);
            if (noStock.Contains(availability)) return 0;
            if (availability == 10) return -1;

            return 1;
        }
    }
}
