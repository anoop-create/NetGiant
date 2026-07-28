using BusinessLogic.ViewModels;
using DataAccess.EntityFramework;
using DataAccess.Utilities;
using MailChimp.Net;
using MailChimp.Net.Core;
using MailChimp.Net.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using VMerchantWrapper.Entities;
using VMerchantWrapper.Framework;
using VmVoucherType = VMerchantWrapper.Entities.VoucherType;
using VmOrder = VMerchantWrapper.Entities.Order;
using VmUser = VMerchantWrapper.Entities.User;
using VmPaymentSource = VMerchantWrapper.Entities.PaymentSource;
using System.Text;
using static VMerchantWrapper.Entities.UserIdentity;
using RestSharp;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using SendGrid;
using PayPalCheckoutSdk.Orders;
using UAParser;

namespace BusinessLogic
{
    public class Touchpoints
    {
        public static string GetBackOfficeConnection()
        {
            return ConfigurationManager.AppSettings["Environment"] == "Live" ? "backoffice_Live" : "backoffice_Dev";
        }

        public static DataTable GetUserData(string userId, string userName = "", string password = "", bool bypassPasswordCheck = false)
        {
            DataSet ds = new DataSet("id");
            DataTable dt = new DataTable();

            string selector = "";
            if (userId != null)
            {
                selector = "U.record = '" + userId + "' ";
            }
            if (userName != "")
            {
                selector = "U.email = '" + userName.Replace("'", "''") + "'";
            }

            //string sql = @"SELECT TOP 1 
            string sql = @"SELECT 
                U.email, U.password, U.record, U.account, U.mailing_list, ISNULL(UL.totallogins, 0) AS totallogins, 
                U.title, U.forename, U.surname, U.telephone,
                C.adr_organisation As [Address1], 
                C.adr_additional1 As [Address2], 
                C.adr_additional2 As [Address3], 
                C.adr_town As [Address4], 
                C.adr_county As [Address5], 
                C.adr_postcode As [PostCode],
                CASE 
                    WHEN ML.MailingListId IS NULL THEN 0
                    ELSE 1
                END As[isOnMailingList],
	            0 As [isContractPricing],
                CASE
					WHEN C.grp IN (1,3,6,7,11,12) THEN 1
					ELSE 0
				END As [isAccountCustomer],
                ISNULL(CD.McCartDataId, 0) As [McCartDataId],
                ISNULL(CD.CartId, '') As [CartId],
                ISNULL(U.hash, 0) As [hash]
                FROM dbo.Users U
                LEFT OUTER JOIN dbo.user_log UL ON UL.account = U.account
                LEFT OUTER JOIN dbo.Customers C ON C.account = U.account
                LEFT OUTER JOIN CustomerData.dbo.MailingList ML ON ML.EmailAddress = U.email AND ML.WebsiteFk = " + int.Parse(ConfigurationManager.AppSettings["WebsiteId"]) + @"
                LEFT OUTER JOIN netgiantMasterData.ngmd.McCartData CD ON CD.RecordId = U.record 
                WHERE " + selector + @"
                AND active = 1 
                ORDER BY U.main_contact DESC, UL.lastlogin DESC";

            ds = SQL.ExecuteReadInline(GetBackOfficeConnection(), sql, "id");
            dt = ds.Tables[0];

            DataTable dto = dt.Clone();

            if (!string.IsNullOrEmpty(userId) || bypassPasswordCheck)
            {
                return dt;
            }
            else
            {
                using (var axisDataConnection =
                        new SqlConnection(ConfigurationManager.ConnectionStrings[GetBackOfficeConnection()].ToString()))
                {
                    axisDataConnection.Open();
                    var vMerchantService = new Service(axisDataConnection, new HttpContextWrapper(HttpContext.Current));
                    foreach (DataRow dr in dt.Rows)
                    {
                        string un = dr["email"].ToString();
                        string pw = dr["password"].ToString();
                        HashVersionType hashVn = (HashVersionType)Enum.Parse(typeof(HashVersionType), dr["hash"].ToString(), true);

                        if (vMerchantService.CheckPasswordHash(password, pw, hashVn))
                        {
                            dto.ImportRow(dr);
                            break;
                        }
                    }
                    vMerchantService.Dispose();
                }
            }
            if (dto.Rows.Count == 0)
            {
                //Utilities.LogInformationMessage("Touchpoints:GetUserData - Unable to match user. UserId: " + (String.IsNullOrEmpty(userId) ? "" : userId) + ", UserName: " + userName);
            }

            return dto;
        }

        /// <summary>
        /// Gets information about account customers
        /// </summary>
        /// <param name="account"></param>
        /// <param name="record"></param>
        /// <returns></returns>
        public static DataTable GetAccountDetails(string account)
        {
            DataSet ds = new DataSet("id");
            DataTable dt = new DataTable();

            string sql = @"SELECT TOP 1 
	            U.account As [AccountNumber],
	            U.contact As [AccountContact],
                CASE 
                    WHEN U.telephone = '' THEN C.telephone
                    ELSE U.telephone
                END As [AccountTelNo],
	            U.email As [AccountEmail],
	            CASE WHEN LEN(C.adr0) > 0 THEN C.adr0 ELSE '' END + 
                CASE WHEN LEN(C.adr1) > 0 THEN + ', ' + C.adr1 ELSE '' END + 
                CASE WHEN LEN(C.adr2) > 0 THEN + ', ' + C.adr2 ELSE '' END + 
                CASE WHEN LEN(C.adr3) > 0 THEN + ', ' + C.adr3 ELSE '' END + 
                CASE WHEN LEN(C.adr4) > 0 THEN + ', ' + C.adr4 ELSE '' END + 
                CASE WHEN LEN(C.adr5) > 0 THEN + ', ' + C.adr5 ELSE '' END As [AccountInvoiceAddress] 
            FROM dbo.Users U
            LEFT OUTER JOIN dbo.Customers C ON C.account = U.account
            WHERE U.account = '" + account + @"'
            AND U.active = 1 
            ORDER BY U.main_contact DESC";

            ds = SQL.ExecuteReadInline(GetBackOfficeConnection(), sql, "acc");
            dt = ds.Tables[0];

            return dt;
        }

        public static String GetRecordNoFromEmail(string email)
        {
            DataSet ds = new DataSet("id");
            DataTable dt = new DataTable();

            string sql = @"SELECT TOP 1
	            U.Record As [RecordNumber]
            FROM dbo.Users U
            LEFT JOIN dbo.user_log UL ON UL.email = U.email AND UL.account = U.account
            WHERE U.Email = '" + email.Replace("'", "''") + @"'
            AND U.active = 1
            ORDER BY U.main_contact DESC, UL.lastlogin DESC";

            ds = SQL.ExecuteReadInline(GetBackOfficeConnection(), sql, "acc");
            dt = ds.Tables[0];

            if (dt.Rows.Count == 0)
            {
                return "";
            }

            return dt.Rows[0]["RecordNumber"].ToString();
        }

        public static bool CheckEmailAvailable(string email)
        {
            DataSet ds = new DataSet();
            DataTable dt = new DataTable();

            string sql = @"select * from dbo.Users where email = '" + email.Replace("'", "''") + "'";

            ds = SQL.ExecuteReadInline(GetBackOfficeConnection(), sql);
            dt = ds.Tables[0];

            return dt.Rows.Count == 0 ? true : false;
        }

        public static DataTable GetDeliveryData(string countrycode, string postcode)
        {
            DataSet ds = new DataSet("id");
            DataTable dt = new DataTable();

            string sql = @"SELECT 'xxxx'";

            ds = SQL.ExecuteReadInline(GetBackOfficeConnection(), sql);
            dt = ds.Tables[0];

            return dt;
        }

        public static DataTable CheckTempRecord(string recordId, string email)
        {
            DataSet ds = new DataSet("chk");
            DataTable dt = new DataTable();

            string selector = "";
            if (recordId != "")
            {
                selector = "U.record = '" + recordId + "'";
            }
            if (email != "")
            {
                selector = "U.email = '" + email.Replace("'", "''") + "' AND (U.main_contact = 1 OR U.main_contact IS NULL) AND U.active = 1";
            }

            string sql = @"SELECT
                    U.record As[Record],
                    U.account As[Account]
                    FROM dbo.Users U
                    WHERE " + selector;

            ds = SQL.ExecuteReadInline(GetBackOfficeConnection(), sql);
            dt = ds.Tables[0];

            return dt;
        }

        public static DataTable GetAddressDetails(string record, string email)
        {
            DataSet ds = new DataSet("adr");
            DataTable dt = new DataTable();

            string selector = "";
            if (record.Contains("/"))
            {
                selector = "U.record = '" + record + "' ";
            }
            else
            {
                selector = "U.email = '" + email.Replace("'", "''") + "' ";
            }

            string sql = @"SELECT
                    U.title As[Title],
                    U.forename As[FirstName],
                    U.surname As[Surname],
                    U.email As[Email],
                    CASE 
                        WHEN U.telephone = '' THEN C.telephone
                        ELSE U.telephone
                    END As[TelNo],
                    CASE 
                        WHEN ML.MailingListId IS NULL THEN 0
                        ELSE 1
                    END As[MailingList],
                    ISNULL(C.adr_organisation, '') As[AddLine1],
                    ISNULL(C.adr_additional1, '') As[AddLine2],
                    ISNULL(C.adr_additional2, '') As[AddLine3],
                    ISNULL(C.adr_town, '') As[AddLine4],
                    ISNULL(C.adr_county, '') As[AddLine5],
                    ISNULL(C.adr_postcode, '') As[AddLine6],
                    ISNULL(DA.adr_organisation, '') As[AddAddLine1],
                    ISNULL(DA.adr_additional1, '') As[AddAddLine2],
                    ISNULL(DA.adr_additional2, '') As[AddAddLine3],
                    ISNULL(DA.adr_town, '') As[AddAddLine4],
                    ISNULL(DA.adr_county, '') As[AddAddLine5],
                    ISNULL(DA.adr_postcode, '') As[AddAddLine6]
                FROM dbo.Users U
                INNER JOIN dbo.Customers C ON C.account = U.account
                LEFT OUTER JOIN dbo.Delivery_Addresses DA ON DA.account = U.account
                LEFT OUTER JOIN CustomerData.dbo.MailingList ML ON ML.EmailAddress = U.email AND ML.WebsiteFk = " + int.Parse(ConfigurationManager.AppSettings["WebsiteId"])
                + " WHERE " + selector;

            ds = SQL.ExecuteReadInline(GetBackOfficeConnection(), sql);
            dt = ds.Tables[0];

            return dt;
        }

        public static void UpdateLastLoggedIn(string userId, string email, int count)
        {
            string sql = @"UPDATE dbo.user_log 
                SET lastlogin = GETDATE(),
                    totallogins = " + count +
                         @" WHERE email = '" + email.Replace("'", "''") + @"' AND account = '" + userId + "'";

            if (!SQL.ExecuteInlineProcedure(GetBackOfficeConnection(), sql))
            {
                //error
            }
        }

        public static bool InsertMailingList(string email, bool newsletter, string firstname = null, string lastname = null)
        {
            bool ret = false;
            if (email.Length > 50)
            {
                return ret;
            }

            List<MailingList> lml = EntityAccess.GetMailingList(x => x.EmailAddress == email);

            try
            {
                if (lml.Count == 0 && newsletter)   // <-- if not already on mailing list and wants to be added
                {
                    string name = email.Split('@')[0];
                    if (name.Length > 40 || email.Contains("@") == false)
                    {
                        return ret;
                    }
                    string[] names = name.Split('.');
                    string name1 = "";
                    string name2 = "";
                    string name3 = "";

                    switch (names.Count())
                    {
                        case 3:
                            {
                                name3 = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(names[2].ToLower());
                                name2 = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(names[1].ToLower());
                                name1 = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(names[0].ToLower());
                                break;
                            }
                        case 2:
                            {
                                name3 = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(names[1].ToLower());
                                name1 = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(names[0].ToLower());
                                break;
                            }
                        case 1:
                            {
                                name1 = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(names[0].ToLower());
                                break;
                            }
                        default:
                            {
                                return ret;
                            }
                    }
                    firstname = firstname ?? name1;
                    lastname = lastname ?? name3;

                    // Attempt to add to MailChimp Mailing List
                    Task<string> result = MailChimpCreateMemberAsync(
                        new Member
                    {
                        EmailAddress = email,
                        Status = Status.Subscribed,
                        StatusIfNew = Status.Subscribed,
                        MergeFields = new Dictionary<string, object>
                            {
                                { "FNAME", firstname },
                                { "LNAME", lastname }
                            }
                    });

                    // Insert into Mailing List table
                    MailingList ml = new MailingList
                    {
                        EmailAddress = email,
                        WebsiteFk = int.Parse(ConfigurationManager.AppSettings["WebsiteId"])
                    };
                    EntityAccess.InsertMailingList(ml);
                }
            }
            catch (Exception e)
            {
                Utilities.ProcessException(e);
            }

            return ret;
        }

        private static async Task<string> MailChimpCreateMemberAsync(Member m)
        {
            Dictionary<string, string> miscdata = DataCache.GetSectionData("MiscData");
            string listid = miscdata["MailChimpListId"];
            string apikey = miscdata["MailChimpApiKey"];

            MailChimpManager mcm = new MailChimpManager(apikey);

            try
            {
                var result = await mcm.Members.AddOrUpdateAsync(listid, m);
                return result.EmailAddress;
            }
            catch (MailChimpException mce)
            {
                Utilities.ProcessException(mce);
                return mce.Message;
            }
            catch (Exception ex)
            {
                Utilities.ProcessException(ex);
                return ex.Message;
            }
        }

        public static async Task MailChimpUpdateCartAsync()
        {
            CheckoutDetails cd = Utilities.LoadSession<CheckoutDetails>("C_CheckoutDetails");
            List<BasketContents> lbc = Utilities.LoadSession<List<BasketContents>>("B_BasketArray");

            // Delete the cart if the basket is empty
            if (lbc.Count(x => x.ItemType == BasketItemType.Item) == 0)
            {
                Task t = Touchpoints.MailChimpDeleteCartAsync((String)HttpContext.Current.Session["U_CartId"]);
                return;
            }

            // Customer logged in: U_CartId available
            // New Customer: Use C_CheckoutDetails
            int w = int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString());
            if ((cd.AccountNumber == null && HttpContext.Current.Session["U_CartId"] == null)
            //    || !lbc.Exists(x => x.ItemType == BasketItemType.Item)
                || Convert.ToBoolean(HttpContext.Current.Session["U_IsPortalUser"])
                || w == 3)
            {
                return;
            }

            Dictionary<string, string> miscdata = DataCache.GetSectionData("MiscData");
            Dictionary<string, string> commondata = DataCache.GetSectionData("CommonData");
            string apikey = miscdata["MailChimpApiKey"];
            string storeId = (ConfigurationManager.AppSettings["Environment"] == "Live" ? "Live_" : "Dev_") + commondata["ShortSiteName"];

            Cart c = new Cart();
            c.Id = HttpContext.Current.Session["U_CartId"].ToString();            
            if (HttpContext.Current.Session["U_MC_CampaignId"] != null)
            {
                c.CampaignId = HttpContext.Current.Session["U_MC_CampaignId"].ToString();
            }
            c.CurrencyCode = CurrencyCode.GBP;
            c.CheckoutUrl = "https://" + commondata["DomainName"].ToString() + "checkout/";

            c.Customer = new MailChimp.Net.Models.Customer
            {
                OptInStatus = false,
                FirstName = "",
                LastName = ""
            };
            StringBuilder sb = new StringBuilder(HttpContext.Current.Session["U_Email"].ToString());
            c.Customer.Id = sb
                .Replace("@", "-")
                .Replace(".", string.Empty)
                .Replace("_", string.Empty)
                .Replace("+", string.Empty)
                .ToString();
            if (cd.Name != null)
            {
                // In checkout
                c.Customer.FirstName = cd.Name.Firstname;
                c.Customer.LastName = cd.Name.Surname;
                c.Customer.EmailAddress = cd.Email;
            }
            else
            {
                // Logged in user
                List<string> name = HttpContext.Current.Session["U_Name"].ToString().Split(' ').ToList();
                if (name.Count > 0)
                {
                    c.Customer.FirstName = name[0];
                    if (name.Count > 1)
                    {
                        c.Customer.LastName = name[name.Count - 1];
                    }
                }
                c.Customer.EmailAddress = HttpContext.Current.Session["U_Email"].ToString();
            }
            // Dev only
            //if (ConfigurationManager.AppSettings["Environment"] != "Live")
            //{
            //    c.Customer.EmailAddress = "stuart.deavall@netgiant.com";
            //}

            decimal total = decimal.Zero;
            for (int i = 0; i < lbc.Count; i++)
            {
                if (lbc[i].ItemType == BasketItemType.Item)
                {
                    c.Lines.Add(new Line
                    {
                        Id = c.Id + "-" + i.ToString(),
                        ProductId = lbc[i].StockRef,
                        ProductTitle = lbc[i].Description,
                        ProductVariantId = lbc[i].StockRef,
                        ProductVariantTitle = lbc[i].Description,
                        Quantity = lbc[i].Quantity,
                        Price = Convert.ToDecimal(lbc[i].PriceEx)
                    });
                    total += lbc[i].Quantity * lbc[i].PriceEx;
                }
            }
            c.OrderTotal = Convert.ToDecimal(total);

            MailChimpManager mcm = new MailChimpManager(apikey);
            Cart cart = new Cart();
            Utilities.SetTlsVersion();
            try
            {
                cart = await mcm.ECommerceStores.Carts(storeId).GetAsync(c.Id).ConfigureAwait(false);
                try
                {
                    cart = await mcm.ECommerceStores.Carts(storeId).UpdateAsync(c.Id, c).ConfigureAwait(false);
                }
                catch (MailChimpException) { }
                catch (Exception) { }

            }
            catch (Exception e)
            {
                try
                {
                    cart = await mcm.ECommerceStores.Carts(storeId).AddAsync(c).ConfigureAwait(false);
                }
                catch (MailChimpException) { }
                catch (Exception) { }
            }
        }

        public static async Task MailChimpDeleteCartAsync(string cartid)
        {
            Dictionary<string, string> miscdata = DataCache.GetSectionData("MiscData");
            Dictionary<string, string> commondata = DataCache.GetSectionData("CommonData");
            string apikey = miscdata["MailChimpApiKey"];
            string storeId = (ConfigurationManager.AppSettings["Environment"] == "Live" ? "Live_" : "Dev_") + commondata["ShortSiteName"];

            MailChimpManager mcm = new MailChimpManager(apikey);
            Utilities.SetTlsVersion();

            try
            {
                await mcm.ECommerceStores.Carts(storeId).DeleteAsync(cartid).ConfigureAwait(false);
                //EntityAccess.DeleteMcCartData(cartid);

            }
            catch (MailChimpException mce)
            {
                if (mce.Status != 404)
                {
                    Utilities.ProcessException(mce);
                }
            }
            catch (Exception e)
            {
                Utilities.ProcessException(e);
            }
        }

        public static bool ResetPassword(string record, string newPassword)
        {
            bool ret = false;

            using (var axisDataConnection =
                    new SqlConnection(ConfigurationManager.ConnectionStrings[GetBackOfficeConnection()].ToString()))
            {
                axisDataConnection.Open();

                var vMerchantService = new Service(axisDataConnection, new HttpContextWrapper(HttpContext.Current));

                try
                {
                    if (!string.IsNullOrEmpty(newPassword))
                    {
                        UserIdentity ui = new UserIdentity();
                        ui.RecordNumber = record;
                        ui.HashPassword(newPassword);
                        vMerchantService.SaveUserIdentity(ui);
                    }

                    ret = true;
                }
                catch (Exception e)
                {
                    Utilities.ProcessException(e);
                }
                vMerchantService.Dispose();
            }

            return ret;
        }

        //public static DataTable GetMailingList(string email)
        //{
        //    DataSet ds = new DataSet("maillist");
        //    DataTable dt = new DataTable();

        //    string sql = @"SELECT TOP 1 email, operation FROM dbo.mailing_list WHERE email = '" + email.Replace("'", "''") + "' ORDER BY timestamp DESC";

        //    ds = SQL.ExecuteReadInline(GetBackOfficeConnection(), sql);
        //    dt = ds.Tables[0];

        //    return dt;
        //}

        public static SaveReturn SaveUser(SignUp suDetails, string record = null, bool isOrderPlaced = false)
        {
            var saveReturn = new SaveReturn();
            var axisUser = new VmUser();

            try
            {
                using (var axisDataConnection =
                    new SqlConnection(ConfigurationManager.ConnectionStrings[GetBackOfficeConnection()].ToString()))
                {
                    axisDataConnection.Open();

                    var boLicence = ConfigurationManager.AppSettings["BoLicense"];
                    var vMerchantService = new Service(axisDataConnection, new HttpContextWrapper(HttpContext.Current));
                    byte group = 10;
                    switch (int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString()))
                    {
                        case 1:
                            {
                                group = 10;
                                break;
                            }
                        case 2:
                            {
                                group = 0;
                                break;
                            }
                        case 3:
                            {
                                group = 5;
                                break;
                            }
                    }

                    // Determines the 'default' VAT status for the account based on Billing Address
                    var userAccountType = CheckoutViewModel.GetPostcodeDeliveryZone(suDetails.Address.PostCode, false).ApplyVat
                        ? AccountType.StandardUk
                        : AccountType.UkVatExempt;

                    Address billingAddress = new Address();
                    var companyEntered = suDetails.Address.Line1;
                    var townEntered = suDetails.Address.Line4;
                    var countyEntered = suDetails.Address.Line5;
                    if (Convert.ToBoolean(HttpContext.Current.Session["U_IsAccountCustomer"]))
                    {
                        // Belt & Braces: Can't change billing address if account customer
                        DataTable dt = GetAddressDetails(HttpContext.Current.Session["U_Record"].ToString(), HttpContext.Current.Session["U_Email"].ToString());
                        if (dt.Rows.Count > 0)
                        {
                            billingAddress = new Address
                            {
                                Country = "GB",
                                Line1 = dt.Rows[0]["AddLine1"].ToString(),
                                Line2 = dt.Rows[0]["AddLine2"].ToString(),
                                Line3 = dt.Rows[0]["AddLine3"].ToString(),
                                PostCode = dt.Rows[0]["AddLine6"].ToString()
                            };
                            companyEntered = dt.Rows[0]["AddLine1"].ToString();
                            townEntered = dt.Rows[0]["AddLine4"].ToString();
                            countyEntered = dt.Rows[0]["AddLine5"].ToString();
                        }
                    }
                    else
                    {
                        billingAddress = suDetails.Address;
                    }

                    suDetails.Name.Firstname = suDetails.Name.Firstname.Trim();
                    suDetails.Name.Surname = suDetails.Name.Surname.Trim();
                    axisUser = new User
                    {
                        AccountType = userAccountType,
                        Address = new VMerchantWrapper.Entities.Address
                        {
                            CountryCode = billingAddress.Country,
                            Line1 = billingAddress.Line2 == null ? "" : billingAddress.Line2.Length > 30 ? billingAddress.Line2.Substring(0, 30) : billingAddress.Line2,
                            Line2 = billingAddress.Line3 == null ? "" : billingAddress.Line3.Length > 30 ? billingAddress.Line3.Substring(0, 30) : billingAddress.Line3,
                            Line3 = "",
                            Line4 = "",
                            Line5 = "",
                            Postcode = billingAddress.PostCode,
                            Town = townEntered,
                            Organisation = companyEntered.Trim(),
                            County = countyEntered
                        },
                        ContactName = TruncateName(suDetails.Name.Firstname, suDetails.Name.Surname),
                        Email = suDetails.UserName,
                        FirstName = suDetails.Name.Firstname,
                        GroupCode = group,
                        Newsletter = suDetails.Newsletter,
                        OrderSource = 0,
                        Surname = suDetails.Name.Surname,
                        TelephoneNumber = suDetails.TelNumber,
                        Title = suDetails.Name.Title,
                        SecondName = "",
                        MobileNumber = ""
                    };
                    if (string.IsNullOrEmpty(record))
                    {
                        axisUser.HashPassword(suDetails.Password);
                    }
                    else
                    {
                        axisUser.HashPassword("Dummy");
                        axisUser.RecordNumber = record;
                    }

                    //Testing only
                    if (ConfigurationManager.AppSettings["Environment"] != "Live")
                    {
                        VoucherPromo v = Utilities.LoadSession<VoucherPromo>("V_Voucher");
                        if (v.VoucherCode == "507")
                        {
                            axisUser.TelephoneNumber = "";    // <-- This should be enough to cause an error
                        }
                    }
                    vMerchantService.SaveUser(axisUser);

                    if (HttpContext.Current.Session["U_Record"] == null)
                    {
                        HttpContext.Current.Session["U_Record"] = axisUser.RecordNumber;
                    }

                    axisDataConnection.Close();
                    vMerchantService.Dispose();                                       

                    saveReturn.IsSuccess = true;
                }
            }
            catch (Exception e)
            {
                var iot = DataCache.GetNgmdLookups(w => w.LookupType.LookupTypeName == "InterimOrderType").ToList();
                string pw = string.IsNullOrEmpty(suDetails.Password) ? "Dummy" : suDetails.Password;
                string json = "{\"PlainPass\":\"" + pw + "\", \"CustomerObject\": " + JsonConvert.SerializeObject(axisUser) + "}";

                var cd = Utilities.LoadSession<CheckoutDetails>("C_CheckoutDetails");
                cd.IsInterimOrder = true;
                saveReturn.Message = "IsInterimOrder";

                InterimOrder io = new InterimOrder
                {
                    InterimOrderId = 0,
                    DateTime = DateTime.Now,
                    IsOrdered = false,
                    Json = json,
                    WebsiteFk = int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString()),
                    InterimOrderTypeFk = iot.Find(x => x.LookupName == "Customer").AltLookupId ?? 0
                };
                saveReturn.IsSuccess = true;
                if (!EntityAccess.SaveInterimOrder(io).IsSuccess)
                {
                    Utilities.ProcessException(e, "SaveUser: ERROR: Unable to save interim customer.Email: " + suDetails.UserName + ".Name: " + suDetails.Name.Firstname + " " + suDetails.Name.Surname);
                    saveReturn.IsSuccess = false;
                }
            }

            return saveReturn;
        }

        private static string TruncateName(string firstname, string surname)
        {
            string name = firstname + " " + surname;

            int excess = name.Length - 20;
            if (excess > 0)
            {
                if (excess < firstname.Length)
                {
                    name = firstname.Substring(0, 1) + " " + surname;
                }
                else
                {
                    name = firstname.Substring(0, 1) + " " + surname.Substring(0, 18);
                }
            }

            return name;
        }

        public static bool VerifyPassword(string providedPassword, string hashedPassword, string hashVersion)
        {
            bool result = false;
            using (var axisDataConnection =
                        new SqlConnection(ConfigurationManager.ConnectionStrings[GetBackOfficeConnection()].ToString()))
            {
                axisDataConnection.Open();
                var vMerchantService = new Service(axisDataConnection, new HttpContextWrapper(HttpContext.Current));
                HashVersionType hashVn = (HashVersionType)Enum.Parse(typeof(HashVersionType), hashVersion, true);

                result = vMerchantService.CheckPasswordHash(providedPassword, hashedPassword, hashVn);
            }

            return result;
        }

        public static SaveReturn SaveOrder(CheckoutDetails cd, OrderStatus orderStatus, string orderNumber = null)
        {
            var saveReturn = new SaveReturn();
            var newOrder = new VmOrder();

            try
            {
                using (var axisDataConnection =
                    new SqlConnection(ConfigurationManager.ConnectionStrings[GetBackOfficeConnection()].ToString()))
                {
                    axisDataConnection.Open();

                    var boLicence = ConfigurationManager.AppSettings["BoLicense"];
                    var vMerchantService = new Service(axisDataConnection, new HttpContextWrapper(HttpContext.Current));

                    var billingCompanyEntered = cd.BillingAddress.Line1;
                    var billingTownEntered = cd.BillingAddress.Line4;
                    var billingCountyEntered = cd.BillingAddress.Line5;
                    var billingAddress = cd.BillingAddress;
                    var userPassword = cd.Password;
                    var userAccountType = CheckoutViewModel.GetPostcodeDeliveryZone(cd.DeliveryAddress.PostCode).ApplyVat
                        ? AccountType.StandardUk
                        : AccountType.UkVatExempt;

                    string record = "";
                    string rt = (string)HttpContext.Current.Session["U_Record"];
                    if (!string.IsNullOrEmpty(rt)
                        && rt.Contains("/"))
                    {
                        record = rt;
                    }
                    if (record == "")
                    {
                        var user = GetUserData("", cd.Email, "", true);
                        if (user.Rows.Count > 0)
                        {
                            record = user.Rows[0]["record"].ToString();
                        }                        
                    }

                    cd.Name.Firstname = cd.Name.Firstname.Trim();
                    cd.Name.Surname = cd.Name.Surname.Trim();
                    var axisBillingUser = new User
                    {
                        AccountType = userAccountType,
                        Address = new VMerchantWrapper.Entities.Address
                        {
                            CountryCode = billingAddress.Country,
                            Line1 = billingAddress.Line2 == null ? "" : billingAddress.Line2.Length > 30 ? billingAddress.Line2.Substring(0, 30) : billingAddress.Line2,
                            Line2 = billingAddress.Line3 == null ? "" : billingAddress.Line3.Length > 30 ? billingAddress.Line3.Substring(0, 30) : billingAddress.Line3,
                            Line3 = "",
                            Line4 = "",
                            Line5 = "",
                            Postcode = billingAddress.PostCode,
                            Town = billingTownEntered,
                            Organisation = billingCompanyEntered.Trim(),
                            County = billingCountyEntered
                        },
                        ContactName = TruncateName(cd.Name.Firstname, cd.Name.Surname),
                        Email = cd.Email,
                        FirstName = cd.Name.Firstname,
                        GroupCode = 0,
                        Newsletter = cd.Newsletter,
                        OrderSource = 0,
                        Surname = cd.Name.Surname,
                        TelephoneNumber = cd.TelephoneNumber,
                        Title = cd.Name.Title,
                        SecondName = "",
                        MobileNumber = "",
                        RecordNumber = record
                    };
                    axisBillingUser.HashPassword("Dummy");

                    var deliveryCompanyEntered = cd.DeliveryAddress.Line1;
                    var deliveryTownEntered = cd.DeliveryAddress.Line4;
                    var deliveryCountyEntered = cd.DeliveryAddress.Line5;
                    var deliveryAddress = cd.DeliveryAddress;
                    cd.RecipientName.Firstname = cd.RecipientName.Firstname.Trim();
                    cd.RecipientName.Surname = cd.RecipientName.Surname.Trim();
                    var axisDeliveryUser = new User
                    {
                        AccountType = userAccountType,
                        Address = new VMerchantWrapper.Entities.Address
                        {
                            CountryCode = deliveryAddress.Country,
                            Line1 = deliveryAddress.Line2 == null ? "" : deliveryAddress.Line2.Length > 30 ? deliveryAddress.Line2.Substring(0, 30) : deliveryAddress.Line2,
                            Line2 = deliveryAddress.Line3 == null ? "" : deliveryAddress.Line3.Length > 30 ? deliveryAddress.Line3.Substring(0, 30) : deliveryAddress.Line3,
                            Line3 = "",
                            Line4 = "",
                            Line5 = "",
                            Postcode = deliveryAddress.PostCode,
                            Town = deliveryTownEntered,
                            Organisation = deliveryCompanyEntered.Trim(),
                            County = deliveryCountyEntered
                        },
                        ContactName = TruncateName(cd.RecipientName.Firstname, cd.RecipientName.Surname),
                        Email = cd.Email,
                        FirstName = cd.RecipientName.Firstname,
                        GroupCode = 0,
                        Newsletter = cd.Newsletter,
                        OrderSource = 0,
                        Surname = cd.RecipientName.Surname,
                        TelephoneNumber = cd.TelephoneNumber,
                        Title = cd.RecipientName.Title,
                        SecondName = "",
                        MobileNumber = "",
                        RecordNumber = record
                    };
                    axisDeliveryUser.HashPassword("Dummy");

                    // Setup Axis Order
                    var basketItems = GetBasketItems();
                    var lbc = Utilities.LoadSession<List<BasketContents>>("B_BasketArray");
                    VoucherPromo voucher = new VoucherPromo();
                    if (lbc.Exists(x => x.ItemType == BasketItemType.Voucher))
                    {
                        voucher = GetVoucher();
                    }
                    if (lbc.Exists(x => x.ItemType == BasketItemType.AdminDiscount))
                    {
                        voucher = new VoucherPromo
                        {
                            VoucherCode = "ADMINDISCOUNT",
                            VoucherTypeFk = (int)VmVoucherType.Amount
                        };
                    }
                    if (lbc.Exists(x => x.ItemType == BasketItemType.CompatibleDiscount))
                    {
                        voucher = new VoucherPromo
                        {
                            VoucherCode = "MULTIBUY",
                            VoucherTypeFk = (int)VmVoucherType.Amount
                        };
                    }

                    newOrder = new VmOrder
                    {
                        Basket = new VMerchantWrapper.Entities.Basket
                        {
                            Items = basketItems,
                            Voucher = voucher != null ? voucher.VoucherCode : "",
                            VoucherType = voucher != null ? (VmVoucherType)voucher.VoucherTypeFk : VmVoucherType.None
                        },
                        BillingUser = axisBillingUser,
                        CustomerReference = string.IsNullOrEmpty(cd.Reference)
                            ? ""
                            : cd.Reference.Length > 20 ? cd.Reference.Substring(0, 20) : cd.Reference,
                        DeliveryMethod = GetDeliveryMethod(),
                        DeliveryUser = axisDeliveryUser,
                        Net = basketItems.Sum(x => x.ItemType == ItemType.Voucher ? x.Net * -1 : x.Net),
                        OrderStatus = orderStatus,
                        OrderSource = Convert.ToBoolean(HttpContext.Current.Session["U_IsPortalUser"])
                            ? !string.IsNullOrEmpty(HttpContext.Current.Session["U_CSUser"].ToString())
                                ? Convert.ToUInt16(HttpContext.Current.Session["U_CSUser"])
                                : Convert.ToUInt16(ConfigurationManager.AppSettings["DiplomatOrderSourceWebPortal"])
                            : Convert.ToUInt16(ConfigurationManager.AppSettings["DiplomatOrderSourceDefault"]),
                        SpecialInstructions = cd.OrderNote,
                        Vat = basketItems.Sum(x => x.ItemType == ItemType.Voucher ? x.Vat * -1 : x.Vat)
                    };

                    if (orderNumber != null)
                    {
                        newOrder.OrderNumber = orderNumber;
                        newOrder.CustomerReference = string.IsNullOrEmpty(newOrder.CustomerReference)
                            ? orderNumber.Length > 20 ? orderNumber.Substring(0, 20) : orderNumber
                            : newOrder.CustomerReference;
                    }

                    if (orderStatus == OrderStatus.Draft)
                    {
                        newOrder.PaymentSource = VmPaymentSource.None;
                    }
                    else
                    {
                        newOrder.PaymentSource = GetPaymentMethod(cd.PaymentMethod);

                        if (newOrder.PaymentSource == VmPaymentSource.SagePayPaid)
                        {
                            newOrder.SagePayAuthCode = cd.SagePayAuthCode;
                            newOrder.SagePayUidCode = cd.SagePayTxCode;
                            newOrder.SagePaySecurityKey = MyOpayo.GetSecurityKey(cd);
                            newOrder.SagePayCcUidId = cd.SagePayUid;
                            newOrder.PaymentCardType = cd.CardType;
                            newOrder.PaymentToken = cd.SageToken;
                            newOrder.PaymentCardLast4Digits = cd.CardLast4Digits;
                        }
                        else if (newOrder.PaymentSource == VmPaymentSource.Paypal)
                        {
                            newOrder.PayPalUidCode = cd.PayPalRef;
                        }
                        else if (newOrder.PaymentSource == VmPaymentSource.AmazonPay)
                        {
                            newOrder.AmazonPayUid = cd.PayPalRef;
                        }
                    }

                    //Testing only
                    if (ConfigurationManager.AppSettings["Environment"] != "Live")
                    {
                        if (orderStatus == OrderStatus.Completed && cd.VoucherCode == "505" || cd.VoucherCode == "506")
                        {
                            newOrder.BillingUser.TelephoneNumber = "";    // <-- This should be enough to cause an error
                        }
                    }
                    if (cd.IsInterimOrder)
                    {
                        // SaveUser for a New Customer failed
                        newOrder.BillingUser.RecordNumber = "99";   // <-- Invalid RecordNumber should cause SaveOrder to fail
                    }
                    vMerchantService.SaveOrder(newOrder);
                    cd.OrderDate = DateTime.Now;
                    cd.BackOfficeOrderRef = newOrder.OrderNumber;

                    axisDataConnection.Close();

                    saveReturn.IsSuccess = true;
                }
            }
            catch (Exception e)
            {
                saveReturn.IsSuccess = true;
                cd.BackOfficeOrderRef = "Interim Order";
                if (orderStatus == OrderStatus.Completed)
                {
                    cd.IsInterimOrder = true;
                    var session = HttpContext.Current.Session;
                    deliveryZone dz = CheckoutViewModel.GetPostcodeDeliveryZone(newOrder.DeliveryUser.Address.Postcode);
                    if (!dz.ApplyVat)
                    {
                        newOrder.Vat = 0;
                    }
                    var iot = DataCache.GetNgmdLookups(w => w.LookupType.LookupTypeName == "InterimOrderType").ToList();
                    string json = "{\"OrderObject\": " + JsonConvert.SerializeObject(newOrder) + "}";
                    InterimOrder io = new InterimOrder
                    {
                        InterimOrderId = 0,
                        DateTime = DateTime.Now,
                        IsOrdered = false,
                        Json = json,
                        WebsiteFk = int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString()),
                        InterimOrderTypeFk = iot.Find(x => x.LookupName == "Order").AltLookupId ?? 0,
                        Reason = e.Message
                    };
                    if (!EntityAccess.SaveInterimOrder(io).IsSuccess)
                    {
                        saveReturn.IsSuccess = false;
                        Utilities.LogInformationMessage("ERROR: Unable to save interim order. Payment may have been taken. Email: " + cd.Email + ". Name: " + cd.Name.Firstname + " " + cd.Name.Surname);
                    }                    
                }
                if (!cd.IsInterimOrder)
                {
                    //saveReturn.IsSuccess = false;
                    Utilities.ProcessException(e, "SaveOrder: Order Status: " + orderStatus.ToString());
                }
            }

            if (orderStatus == OrderStatus.Completed)
            {
                InsertMailingList(cd.Email, cd.Newsletter, cd.Name.Firstname, cd.Name.Surname);
            }

            return saveReturn;
        }

        public static string GetUserAgent(string ua)
        {
            try
            {
                var uaParser = Parser.GetDefault();
                ClientInfo c = uaParser.Parse(ua);

                return (c.UA.Family ?? "") + " " + (c.UA.Major ?? "") + " on " + (c.OS.Family ?? "") + " " + (c.OS.Major ?? "");
            }
            catch {
                return "Not known";
            }
        }

        private static int GetDeliveryMethod()
        {
            var lbc = Utilities.LoadSession<List<BasketContents>>("B_BasketArray");

            var deliveryItem = lbc.FirstOrDefault(x => x.ItemType == BasketItemType.Delivery);
            return deliveryItem?.DeliveryMethod ?? 1;
        }

        private static List<BasketItem> GetBasketItems()
        {
            var lbc = Utilities.LoadSession<List<BasketContents>>("B_BasketArray");

            var axisBasketItems = lbc
                .Select(x => new BasketItem
                {
                    Description = x.Description != null
                        ? x.Description.Length <= 120
                            ? x.Description
                            : x.Description.Substring(0, 120)
                        : "",
                    ItemType = GetItemType(x),
                    Net = x.ItemType == BasketItemType.Item || x.ItemType == BasketItemType.Delivery ? Math.Round(x.PriceEx, 2) * x.Quantity : Math.Abs(Math.Round(x.PriceEx, 2)),
                    Notes = "",
                    Quantity = x.Quantity,
                    Reference = x.StockRef,
                    UnitPrice = x.ItemType == BasketItemType.Item || x.ItemType == BasketItemType.Delivery
                        ? Math.Floor(x.PriceInc * 100) / 100
                        : !x.IsVatExempt
                            ? Math.Abs(Math.Floor(x.PriceInc * 100) / 100)
                            : Math.Abs(Math.Floor(x.PriceEx * 100) / 100),
                    Vat = !x.IsVatExempt
                        ? x.ItemType == BasketItemType.Item || x.ItemType == BasketItemType.Delivery
                            ? (Math.Floor((x.PriceInc - x.PriceEx) * 100) * x.Quantity / 100)
                            : Math.Abs((Math.Floor(x.PriceInc * 100) / 100) - Math.Round(x.PriceEx, 2))
                        : 0,
                    VatRate = !x.IsVatExempt ? (Convert.ToDecimal(ConfigurationManager.AppSettings["VatMultiplier"]) - 1) * 100 : 0,
                    VoucherNet = x.ItemType == BasketItemType.Item || x.ItemType == BasketItemType.Delivery ? GetVoucherNet(x) : 0,
                    VoucherVat = x.ItemType == BasketItemType.Item || x.ItemType == BasketItemType.Delivery ? GetVoucherVat(x) : 0
                })
                .ToList();

            axisBasketItems.RemoveAll(x => x.ItemType == ItemType.Voucher && x.Net == 0);

            return axisBasketItems;
        }

        private static ItemType GetItemType(BasketContents item)
        {
            ItemType it = ItemType.Stock;
            if (item.ItemType == BasketItemType.Voucher || item.ItemType == BasketItemType.AdminDiscount || item.ItemType == BasketItemType.CompatibleDiscount)
            {
                it = ItemType.Voucher;
            }
            if (item.ItemType == BasketItemType.Delivery)
            {
                it = ItemType.Delivery;
            }

            return it;
        }

        private static decimal GetVoucherNet(BasketContents item)
        {
            VoucherPromo v = null;
            if (HttpContext.Current.Session["V_Voucher"] != null)
            {
                v = (VoucherPromo)HttpContext.Current.Session["V_Voucher"];
            }

            if (v == null) return 0;
            if (item.IsVoucherQualifyingItem)
            {
                decimal p = v.Percentage ?? default(decimal);
                return item.PriceEx * ((v.Percentage ?? default(decimal)) / 100);
            }

            return 0;
        }

        private static decimal GetVoucherVat(BasketContents item)
        {
            VoucherPromo v = null;
            if (HttpContext.Current.Session["V_Voucher"] != null)
            {
                v = (VoucherPromo)HttpContext.Current.Session["V_Voucher"];
            }

            if (v == null) return 0;
            if (item.IsVoucherQualifyingItem && !item.IsVatExempt)
            {
                return item.PriceEx * ((v.Percentage ?? default(decimal)) / 100) *
                       (Convert.ToDecimal(ConfigurationManager.AppSettings["VatMultiplier"]) - 1);
            }

            return 0;
        }

        private static VmPaymentSource GetPaymentMethod(string paymentMethod)
        {
            switch (paymentMethod)
            {
                case "Account":
                    return VmPaymentSource.None;
                case "BACS":
                    return VmPaymentSource.Bacs;
                case "CreditDebit":
                    return VmPaymentSource.SagePayPaid;
                case "PayPal":
                    return VmPaymentSource.Paypal;
                case "Phone":
                    return VmPaymentSource.Telephone;
                case "AccountApplication":
                    return VmPaymentSource.Telephone;
                case "AmazonPay":
                    return VmPaymentSource.AmazonPay;
            }

            return VmPaymentSource.None;
        }

        private static VoucherPromo GetVoucher()
        {

            VoucherPromo v = null;
            if (HttpContext.Current.Session["V_Voucher"] != null)
            {
                v = (VoucherPromo)HttpContext.Current.Session["V_Voucher"];
            }

            return v;
        }

        public static void DoCampaignTracking(CheckoutViewModel model)
        {
            var sess = HttpContext.Current.Session;
            string source = sess["U_CampaignSource"] as string;

            if (!string.IsNullOrEmpty(sess["U_Campaign"] as string))
            {
                CampaignTracking ct = new CampaignTracking
                {
                    CampaignTrackingId = 0,
                    OrderDate = DateTime.Now,
                    OrderNumber = model.CheckoutDetails.BackOfficeOrderRef,
                    OrderSourceFk = EntityAccess.ReadNgmdLookUp(
                        x => x.LookupType.LookupTypeName == "OrderSource" && x.LookupName == source)
                        .FirstOrDefault().LookupId,
                    Campaign = sess["U_Campaign"].ToString()
                };
                if (!EntityAccess.SaveCampaignTracking(ct).IsSuccess)
                {
                    Utilities.LogInformationMessage("Save Campaign Tracking: ERROR: Unable to save Campaign Tracking for order: " + model.CheckoutDetails.BackOfficeOrderRef);
                }
            }
        }

        public static async Task SendAwinData(CheckoutViewModel model)
        {
            StringBuilder awinURL = new StringBuilder();
            awinURL.Append("?tt=ss&tv=2&merchant=");

            switch (ConfigurationManager.AppSettings["WebsiteId"].ToString())
            {
                case "1":
                    //TonerGiant
                    awinURL.Append("5500");
                    awinURL.Append("&amount=");
                    awinURL.Append(model.BasketTotals.GrandTotalExcVat.ToString("F2"));

                    awinURL.Append("&parts=");
                    awinURL.Append(
                        string.Join("|", model.BasketContents
                        .Where(x => x.ItemType == BasketItemType.Item)
                        .GroupBy(x => x.AffiliateCommissionGroup)
                        .Select(x => String.Format("{0}:{1}", x.Key, (x.Sum(y => y.PriceEx * y.Quantity) - x.Sum(z => z.VoucherAmount)).ToString("F2")))
                        .ToList())
                        );
                    break;
                case "2":
                    //CartridgeMonkey
                    awinURL.Append("808");
                    awinURL.Append("&amount=");
                    awinURL.Append(model.BasketTotals.GrandTotalIncVat.ToString("F2"));

                    awinURL.Append("&parts=");
                    awinURL.Append(
                        string.Join("|", model.BasketContents
                        .Where(x => x.ItemType == BasketItemType.Item)
                        .GroupBy(x => x.AffiliateCommissionGroup)
                        .Select(x => String.Format("{0}:{1}", x.Key, (x.Sum(y => y.PriceInc * y.Quantity) - x.Sum(z => z.VoucherAmount)).ToString("F2")))
                        .ToList())
                        );
                    break;
            }

            awinURL.Append("&ref=");
            awinURL.Append(model.CheckoutDetails.BackOfficeOrderRef);
            awinURL.Append("&vc=");
            awinURL.Append(
                model.BasketContents.Exists(x => x.ItemType == BasketItemType.CompatibleDiscount) ? "MULTIBUY" : model.CheckoutDetails.VoucherCode
                );

            awinURL.Append("&testmode=");
            awinURL.Append(ConfigurationManager.AppSettings["Environment"] == "Live" ? "0" : "1");

            awinURL.Append("&cr=GBP");

            awinURL.Append("&cks=");

            if (HttpContext.Current.Request.Cookies["awc"] != null)
            {
                awinURL.Append(HttpContext.Current.Request.Cookies["awc"].Value);
            }

            try
            {
                Utilities.SetTlsVersion();
                var client = new RestClient("https://www.awin1.com/sread.php");

                var request = new RestRequest(awinURL.ToString(), RestSharp.Method.Get);
                var response = client.Execute(request, RestSharp.Method.Get);
                
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    Utilities.LogInformationMessage(
                        "ERROR: Unable to complete SendAwinData (Touchpoints) -- Status code was "
                        + response.StatusCode.ToString());
                }
            }
            catch (Exception e)
            {
                Utilities.LogInformationMessage("ERROR: Unable to complete SendAwinData (Touchpoints) -- " + e.ToString());
            }
        }
    }
}
