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
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using VMerchantWrapper.Entities;
using VMerchantWrapper.Framework;
using VmVoucherType = VMerchantWrapper.Entities.VoucherType;
using VmOrder = VMerchantWrapper.Entities.Order;

namespace BusinessLogic
{
    public class Touchpoints
    {
        public static string GetBackOfficeConnection()
        {
            return ConfigurationManager.AppSettings["Environment"] == "Live" ? "backoffice_Live" : "backoffice_Dev";
        }

        public static string GetAxisWebConnection()
        {
            return ConfigurationManager.AppSettings["Environment"] == "Live" ? "axisweb_Live" : "axisweb_Dev";
        }

        public static DataTable GetUserData(string userId, string userName = "", string password = "")
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
            if (password != "")
            {
                selector += " AND U.password = '" + password.Replace("'", "''") + "'";
            }

            string sql = @"SELECT TOP 1 
                U.email, U.password, U.record, U.account, U.mailing_list, ISNULL(UL.totallogins, 0) AS totallogins, 
                U.title, U.forename, U.surname, U.telephone,
                C.adr_organisation As [Address1], 
                C.adr_additional1 As [Address2], 
                C.adr_additional2 As [Address3], 
                C.adr_town As [Address4], 
                C.adr_county As [Address5], 
                C.adr_postcode As [PostCode],
                ISNULL(ML.operation, 0) AS [isOnMailingList],
	            0 As [isContractPricing],
                CASE
					WHEN C.grp IN (1,3,6,7,11,12) THEN 1
					ELSE 0
				END As [isAccountCustomer]
                FROM dbo.Users U
                LEFT OUTER JOIN dbo.mailing_list ML ON U.email = ML.email
                LEFT OUTER JOIN dbo.user_log UL ON UL.account = U.account
                LEFT OUTER JOIN dbo.Customers C ON C.account = U.account
                WHERE " + selector + @"
                AND active = 1 
                ORDER BY U.main_contact DESC, UL.lastlogin DESC, ML.[timestamp] DESC";

            ds = SQL.ExecuteReadInline(GetBackOfficeConnection(), sql, "id");
            dt = ds.Tables[0];

            return dt;
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
                    ISNULL(ML.operation, 0) As [MailingList],
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
                OUTER APPLY (SELECT TOP 1 operation FROM dbo.mailing_list WHERE email = U.email ORDER BY timestamp DESC) ML
                WHERE " + selector;

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

            DataTable dt = GetMailingList(email);

            try
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

                if (dt.Rows.Count == 0 || Convert.ToBoolean(dt.Rows[0]["operation"]) != newsletter)
                {
                    firstname = firstname ?? name1;
                    lastname = lastname ?? name3;

                    if (email.Length < 51)
                    {
                        string sql = @"INSERT INTO dbo.mailing_list
                            (title, forename, secondname, surname, email, timestamp, operation, status) 
                            VALUES ('', '" + firstname.Replace("'", "''").Truncate(20) + "', '" + name2.Replace("'", "''").Truncate(20) + "', '" + lastname.Replace("'", "''").Truncate(20) + "', '" + email.Replace("'", "''") + "', '" +
                                     DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "', " +
                                     Convert.ToInt16(newsletter) + @", 0)";

                        if (SQL.ExecuteInlineProcedure(GetBackOfficeConnection(), sql))
                        {
                            ret = true;
                        }
                    }

                    // Attempt to add to MailChimp Mailing List
                    if (newsletter)
                    {
                        Task<string> result = MailChimpCreateAsync(email, firstname, lastname);
                    }
                }
            }
            catch(Exception e)
            {
                Utilities.ProcessException(e);
            }

            return ret;
        }

        private static async Task<string> MailChimpCreateAsync(string email, string fname, string lname)
        {
            Dictionary<string, string> miscdata = DataCache.GetSectionData("MiscData");
            string listid = miscdata["MailChimpListId"];
            string apikey = miscdata["MailChimpApiKey"];

            MailChimpManager mcm = new MailChimpManager(apikey);
            var member = new Member
            {
                EmailAddress = email,
                //ListId = listid,
                //Status = Status.Pending,
                StatusIfNew = Status.Pending,
                //EmailType = "html",
                //IpSignup = ip,
                //TimestampSignup = DateTime.UtcNow.ToString("s"),
                MergeFields = new Dictionary<string, object>
                {
                    { "FNAME", fname },
                    { "LNAME", lname }
                }
            };

            try
            {
                var result = await mcm.Members.AddOrUpdateAsync(listid, member);
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

        public static bool ResetPassword(string record, string newPassword)
        {
            //First read the user to estalish the state of the new_user field
            DataSet ds = new DataSet("userRecord");
            DataTable dt = new DataTable();

            string sqlSelect = @"SELECT TOP 1 new_user FROM dbo.Users WHERE record = '" + record + "'";

            ds = SQL.ExecuteReadInline(GetBackOfficeConnection(), sqlSelect);
            dt = ds.Tables[0];
            var newUser = Convert.ToBoolean(dt.Rows[0]["new_user"]);

            string updatePart = "";
            if (!newUser)
            {
                updatePart = ", U.new_user = 1, U.updated_user = 1";
            }

            var ret = false;
            string sql = @" UPDATE U
                            SET U.password = '" + newPassword.Replace("'", "''") + "'" + updatePart +
                          " FROM dbo.Users U" +
                          " WHERE U.record = '" + record + "'";

            if (SQL.ExecuteInlineTransaction(GetBackOfficeConnection(), sql))
            {
                ret = true;
            }

            return ret;
        }

        public static DataTable GetMailingList(string email)
        {
            DataSet ds = new DataSet("maillist");
            DataTable dt = new DataTable();

            string sql = @"SELECT TOP 1 email, operation FROM dbo.mailing_list WHERE email = '" + email.Replace("'", "''") + "' ORDER BY timestamp DESC";

            ds = SQL.ExecuteReadInline(GetBackOfficeConnection(), sql);
            dt = ds.Tables[0];

            return dt;
        }

        public static SaveReturn SaveUser(SignUp suDetails, string record = null)
        {
            var saveReturn = new SaveReturn();

            try
            {
                using (var axisDataConnection =
                    new SqlConnection(ConfigurationManager.ConnectionStrings[GetBackOfficeConnection()].ToString()))
                using (var axisConfigConnection =
                    new SqlConnection(ConfigurationManager.ConnectionStrings[GetAxisWebConnection()].ToString()))
                {
                    axisDataConnection.Open();
                    axisConfigConnection.Open();

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

                    var userAccountType = CheckoutViewModel.GetPostcodeDeliveryZone(suDetails.Address.PostCode).ApplyVat
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
                                //Line4 = dt.Rows[0]["AddLine4"].ToString(),
                                //Line5 = dt.Rows[0]["AddLine5"].ToString(),
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
                    var axisUser = new User
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
                        Password = suDetails.Password,
                        Surname = suDetails.Name.Surname,
                        TelephoneNumber = suDetails.TelNumber,
                        Title = suDetails.Name.Title,
                        SecondName = "",
                        MobileNumber = ""
                    };

                    if (record != null)
                        axisUser.RecordNumber = record;

                    vMerchantService.SaveUser(axisUser);

                    if (HttpContext.Current.Session["U_Record"] == null)
                    {
                        HttpContext.Current.Session["U_Record"] = axisUser.RecordNumber;
                    }

                    axisDataConnection.Close();
                    axisConfigConnection.Close();

                    InsertMailingList(suDetails.UserName, suDetails.Newsletter, suDetails.Name.Firstname, suDetails.Name.Surname);

                    saveReturn.IsSuccess = true;
                }
            }
            catch (Exception e)
            {
                saveReturn.IsSuccess = false;
                saveReturn.Message = "Sorry, there was a problem updating your details. Try again later or contact us for assistance.";
                Utilities.ProcessException(e);

                return saveReturn;
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

        public static SaveReturn SaveOrder(CheckoutDetails cd, OrderStatus orderStatus, string orderNumber = null)
        {
            var saveReturn = new SaveReturn();

            try
            {
                using (var axisDataConnection =
                    new SqlConnection(ConfigurationManager.ConnectionStrings[GetBackOfficeConnection()].ToString()))
                using (var axisConfigConnection =
                    new SqlConnection(ConfigurationManager.ConnectionStrings[GetAxisWebConnection()].ToString()))
                {
                    axisDataConnection.Open();
                    axisConfigConnection.Open();

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

                    string record = HttpContext.Current.Session["U_Record"] == null ? "" : HttpContext.Current.Session["U_Record"].ToString();
                    if (record == "")
                    {
                        var user = GetUserData("", cd.Email);
                        record = user.Rows[0]["record"].ToString();
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
                        Password = userPassword,
                        Surname = cd.Name.Surname,
                        TelephoneNumber = cd.TelephoneNumber,
                        Title = cd.Name.Title,
                        SecondName = "",
                        MobileNumber = "",
                        RecordNumber = record
                    };

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
                        Password = userPassword,
                        Surname = cd.RecipientName.Surname,
                        TelephoneNumber = cd.TelephoneNumber,
                        Title = cd.RecipientName.Title,
                        SecondName = "",
                        MobileNumber = "",
                        RecordNumber = record
                    };

                    // Setup Axis Order
                    var basketItems = GetBasketItems();
                    var lbc = new List<BasketContents>();
                    if (HttpContext.Current.Session["B_BasketArray"] != null)
                    {
                        lbc = (List<BasketContents>)HttpContext.Current.Session["B_BasketArray"];
                    }
                    VoucherPromo voucher = new VoucherPromo();
                    if (lbc.Exists(x => x.IsVoucher))
                    {
                        voucher = GetVoucher();
                    }
                    if (lbc.Exists(x => x.IsAdminDiscount))
                    {
                        voucher = new VoucherPromo
                        {
                            VoucherCode = "ADMINDISCOUNT",
                            VoucherTypeFk = (int)VmVoucherType.Amount
                        };
                    }

                    var newOrder = new VmOrder
                    {
                        Basket = new VMerchantWrapper.Entities.Basket
                        {
                            Items = basketItems,
                            Voucher = voucher != null ? voucher.VoucherCode : "",
                            VoucherType = voucher != null ? (VmVoucherType)voucher.VoucherTypeFk : VmVoucherType.None
                        },
                        BillingUser = axisBillingUser,
                        //CustomerReference = cd.Reference,
                        //CustomerReference = cd.Reference.Length > 20 ? cd.Reference.Substring(0, 20) : cd.Reference,
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
                        //newOrder.CustomerReference = string.IsNullOrEmpty(newOrder.CustomerReference)
                        //    ? orderNumber
                        //    : newOrder.CustomerReference;
                        newOrder.CustomerReference = string.IsNullOrEmpty(newOrder.CustomerReference)
                            ? orderNumber.Length > 20 ? orderNumber.Substring(0, 20) : orderNumber
                            : newOrder.CustomerReference;
                    }

                    if (orderStatus == OrderStatus.Draft)
                    {
                        newOrder.PaymentSource = PaymentSource.None;
                    }
                    else
                    {
                        newOrder.PaymentSource = GetPaymentMethod(cd.PaymentMethod);

                        if (newOrder.PaymentSource == PaymentSource.SagePayPaid)
                        {
                            newOrder.SagePayAuthCode = cd.SagePayAuthCode;
                            newOrder.SagePayUidCode = cd.SagePayTxCode;
                            newOrder.SagePaySecurityKey = cd.SagePaySecurityKey;
                            newOrder.SagePayCcUidId = cd.SagePayUid;
                            newOrder.PaymentCardType = cd.CardType;
                            newOrder.PaymentToken = cd.SageToken;
                            newOrder.PaymentCardLast4Digits = cd.CardLast4Digits;
                        }
                        else if (newOrder.PaymentSource == PaymentSource.Paypal)
                        {
                            newOrder.PayPalUidCode = cd.PayPalRef;
                        }
                    }

                    vMerchantService.SaveOrder(newOrder);
                    cd.OrderDate = DateTime.Now;
                    cd.BackOfficeOrderRef = newOrder.OrderNumber;

                    axisDataConnection.Close();
                    axisConfigConnection.Close();

                    InsertMailingList(cd.Email, cd.Newsletter, cd.Name.Firstname, cd.Name.Surname);

                    saveReturn.IsSuccess = true;
                }
            }
            catch (Exception e)
            {
                saveReturn.IsSuccess = false;
                Utilities.ProcessException(e);
            }

            return saveReturn;
        }

        private static int GetDeliveryMethod()
        {
            var lbc = new List<BasketContents>();
            if (HttpContext.Current.Session["B_BasketArray"] != null)
            {
                lbc = (List<BasketContents>) HttpContext.Current.Session["B_BasketArray"];
            }

            var deliveryItem = lbc.FirstOrDefault(x => x.IsDelivery);
            return deliveryItem?.DeliveryMethod ?? 1;
        }

        private static List<BasketItem> GetBasketItems()
        {
            var lbc = new List<BasketContents>();
            if (HttpContext.Current.Session["B_BasketArray"] != null)
            {
                lbc = (List<BasketContents>) HttpContext.Current.Session["B_BasketArray"];
            }

            var axisBasketItems = lbc
                .Select(x => new BasketItem
                {
                    Description = x.Description != null
                        ? x.Description.Length <= 120 
                            ? x.Description 
                            : x.Description.Substring(0, 120)
                        : "",
                    ItemType = GetItemType(x),
                    Net = !x.IsVoucher && !x.IsAdminDiscount ? Math.Round(x.PriceEx, 2) * x.Quantity : Math.Abs(Math.Round(x.PriceEx, 2)),
                    Notes = "",
                    Quantity = x.Quantity,
                    Reference = x.StockRef,
                    UnitPrice = !x.IsVoucher && !x.IsAdminDiscount ? Math.Floor(x.PriceInc * 100) / 100 : Math.Abs(Math.Floor(x.PriceInc * 100) / 100),
                    Vat = !x.IsVatExempt ? !x.IsVoucher && !x.IsAdminDiscount ? (Math.Floor((x.PriceInc - x.PriceEx) * 100) * x.Quantity / 100) : Math.Abs((Math.Floor(x.PriceInc * 100) / 100) - Math.Round(x.PriceEx, 2)) : 0,
                    VatRate = !x.IsVatExempt ? (Convert.ToDecimal(ConfigurationManager.AppSettings["VatMultiplier"]) - 1) * 100 : 0,
                    VoucherNet = !x.IsVoucher && !x.IsAdminDiscount ? GetVoucherNet(x) : 0,
                    VoucherVat = !x.IsVoucher && !x.IsAdminDiscount ? GetVoucherVat(x) : 0
                })
                .ToList();

            axisBasketItems.RemoveAll(x => x.ItemType == ItemType.Voucher && x.Net == 0);

            return axisBasketItems;
        }

        private static ItemType GetItemType(BasketContents item)
        {
            ItemType it = ItemType.Stock;
            if (item.IsVoucher || item.IsAdminDiscount)
            {
                it = ItemType.Voucher;
            }
            if (item.IsDelivery)
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
                v = (VoucherPromo) HttpContext.Current.Session["V_Voucher"];
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
                v = (VoucherPromo) HttpContext.Current.Session["V_Voucher"];
            }

            if (v == null) return 0;
            if (item.IsVoucherQualifyingItem && !item.IsVatExempt)
            {
                return item.PriceEx * ((v.Percentage ?? default(decimal)) / 100) *
                       (Convert.ToDecimal(ConfigurationManager.AppSettings["VatMultiplier"]) - 1);
            }

            return 0;
        }

        private static PaymentSource GetPaymentMethod(string paymentMethod)
        {
            switch (paymentMethod)
            {
                case "Account":
                    return PaymentSource.None;
                case "BACS":
                    return PaymentSource.Bacs;
                case "CreditDebit":
                    return PaymentSource.SagePayPaid;
                case "PayPal":
                    return PaymentSource.Paypal;
                case "Phone":
                    return PaymentSource.Telephone;
                case "AccountApplication":
                    return PaymentSource.Telephone;
            }

            return PaymentSource.None;
        }

        private static VoucherPromo GetVoucher(){

            VoucherPromo v = null;
            if (HttpContext.Current.Session["V_Voucher"] != null)
            {
                v = (VoucherPromo)HttpContext.Current.Session["V_Voucher"];
            }

            return v;
        }
    }
}
