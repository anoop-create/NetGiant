using DataAccess.EntityFramework;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using VMerchantWrapper.Framework;
using VMerchantWrapper.Entities;
using DataAccess.Utilities;

namespace BusinessLogic.ViewModels
{
    public class MyAccountViewModel : CommonViewModel
    {
        public MyAccountViewModel()
        {
            MyAccountData = DataCache.GetSectionData("MyAccountData");
        }

        public Dictionary<string, string> MyAccountData { get; set; }
        public List<OrderHistory> OrderHistoryList { get; set; }
        public OrderHistory Order { get; set; }
        public List<MiniProductEntry> PurchasedProducts { get; set; }
        public ResetPassword ResetPassword { get; set; }
        public string ResetGuid { get; set; }

        public void GetOrderData()
        {
            DataTable dt = GetOrderDetails();

            string orderno = "";
            bool firsttime = true;

            OrderHistoryList = new List<OrderHistory>();
            if (dt.Rows.Count > 0)
            {
                OrderHistory oh = new OrderHistory();

                foreach (DataRow dr in dt.Rows)
                {
                    // The following can be re-organised once the new Stored Procedure is in place
                    if (dr["OrderNumber"].ToString() != orderno)
                    {
                        if (!firsttime)
                        {
                            OrderHistoryList.Add(oh);
                        }
                        firsttime = false;
                        orderno = dr["OrderNumber"].ToString();
                        oh = new OrderHistory
                        {
                            OrderNumber = dr["OrderNumber"].ToString(),
                            CustomerReference = dr["CustomerReference"].ToString(),
                            OrderDate = Convert.ToDateTime(dr["OrderDate"]),
                            TotalNet = Convert.ToDecimal(dr["TotalNet"]),
                            TotalVat = Convert.ToDecimal(dr["TotalVat"]),
                            TotalDel = 0,
                            Total = oh.TotalNet + oh.TotalVat,
                            Status = int.Parse(dr["Status"].ToString())
                        };
                    }
                }
                OrderHistoryList.Add(oh);
            }
        }

        public void GetOrder(string orderno)
        {
            DataTable dt = GetOrderDetail(orderno);

            decimal acctotal = 0;
            bool firsttime = true;

            Order = new OrderHistory();
            if (dt.Rows.Count > 0)
            {
                OrderHistory oh = new OrderHistory();

                foreach (DataRow dr in dt.Rows)
                {
                    if (firsttime)
                    {
                        firsttime = false;
                        orderno = dr["OrderNumber"].ToString();
                        oh = new OrderHistory();
                        oh.OrderNumber = dr["OrderNumber"].ToString();
                        oh.CustomerReference = dr["CustomerReference"].ToString();
                        oh.OrderDate = Convert.ToDateTime(dr["OrderDate"]);
                        oh.TotalNet = Convert.ToDecimal(dr["TotalNet"]);
                        oh.TotalVat = Convert.ToDecimal(dr["TotalVat"]);
                        oh.TotalDel = 0;
                        oh.Total = oh.TotalNet + oh.TotalVat;
                        oh.Status = int.Parse(dr["Status"].ToString());
                        oh.DeliveryAddress = new Address();
                        oh.DeliveryAddress.Line1 = dr["DeliveryAddress1"].ToString();
                        oh.DeliveryAddress.Line2 = dr["DeliveryAddress2"].ToString();
                        oh.DeliveryAddress.Line3 = dr["DeliveryAddress3"].ToString();
                        oh.DeliveryAddress.Line4 = dr["DeliveryAddress4"].ToString();
                        oh.DeliveryAddress.Line5 = dr["DeliveryAddress5"].ToString();
                        oh.DeliveryAddress.PostCode = dr["DeliveryPostcode"].ToString();
                        oh.BillingAddress = new Address();
                        oh.BillingAddress.Line1 = dr["BillingAddress1"].ToString();
                        oh.BillingAddress.Line2 = dr["BillingAddress2"].ToString();
                        oh.BillingAddress.Line3 = dr["BillingAddress3"].ToString();
                        oh.BillingAddress.Line4 = dr["BillingAddress4"].ToString();
                        oh.BillingAddress.Line5 = dr["BillingAddress5"].ToString();
                        oh.BillingAddress.PostCode = dr["BillingPostcode"].ToString();
                        oh.OrderLines = new List<OrderLine>();
                    }

                    OrderLine ol = new OrderLine();
                    ol.Reference = dr["Reference"].ToString().Trim();
                    ol.AltRef = dr["AltRef"].ToString();
                    ol.Description = dr["Description"].ToString();
                    ol.Quantity = Convert.ToInt16(dr["Quantity"]);
                    ol.PriceNet = Convert.ToDecimal(dr["PriceNet"]) / ol.Quantity;
                    ol.PriceVat = Convert.ToDecimal(dr["PriceVat"]);

                    ol.ImageUrl = dr["ImageUrl"].ToString();
                    ol.Availability = int.Parse(dr["Availability"].ToString());
                    ol.IsDiscontinued = Convert.ToBoolean(dr["IsDiscontinued"]);
                    if (ol.IsDiscontinued)
                    {
                        ol.Availability = 0;
                    }
                    ol.IsVoucher = Convert.ToBoolean(dr["IsVoucher"]);
                    ol.IsDelivery = Convert.ToBoolean(dr["IsDelivery"]);
                    ol.SubTotal = ol.PriceNet * ol.Quantity;

                    if (ol.IsVoucher)
                    {
                        oh.TotalVoucher = ol.SubTotal * -1;
                    }
                    if (ol.IsDelivery)
                    {
                        oh.TotalDel = ol.SubTotal;
                    }
                    if (!ol.IsVoucher && !ol.IsDelivery)
                    {
                        acctotal = acctotal + ol.SubTotal;
                    }
                    oh.OrderLines.Add(ol);
                }
                oh.AccTotal = acctotal;
                Order = oh;
            }
        }

        private DataTable GetOrderDetails()
        {
            int websiteId = Convert.ToInt32(ConfigurationManager.AppSettings["WebsiteId"]);
            string account = Convert.ToString(HttpContext.Current.Session["U_AccountNo"]);

            List<SqlParameter> sqlParms = new List<SqlParameter>();
            SqlParameter sqlParm = new SqlParameter("@WebsiteId", SqlDbType.Int);
            sqlParm.Value = websiteId;
            sqlParms.Add(sqlParm);
            sqlParm = new SqlParameter("@Language", SqlDbType.Int);
            sqlParm.Value = Utilities.GetLanguage(websiteId.ToString());
            sqlParms.Add(sqlParm);
            sqlParm = new SqlParameter("@Account", SqlDbType.VarChar);
            sqlParm.Value = account;
            sqlParms.Add(sqlParm);
            return SQL.ExecuteReadStoredProcedure("netgiantmasterdata", "ngmd.GetOrderDetails", sqlParms, "results").Tables[0];
        }

        private DataTable GetOrderDetail(string orderno)
        {
            int websiteId = Convert.ToInt32(ConfigurationManager.AppSettings["WebsiteId"]);

            List<SqlParameter> sqlParms = new List<SqlParameter>();
            SqlParameter sqlParm = new SqlParameter("@WebsiteId", SqlDbType.Int);
            sqlParm.Value = websiteId;
            sqlParms.Add(sqlParm);
            sqlParm = new SqlParameter("@OrderNo", SqlDbType.VarChar);
            sqlParm.Value = orderno;
            sqlParms.Add(sqlParm);
            return SQL.ExecuteReadStoredProcedure("netgiantmasterdata", "ngmd.GetOrderDetail", sqlParms, "results").Tables[0];
        }

        public void GetResetData(string guid)
        {
            DateTime cutoff = DateTime.Now.AddHours(-2);
            passwordReset pr = EntityAccess.ReadPasswordReset(x => x.guid == guid && x.dateCreated > cutoff).FirstOrDefault();

            ResetPassword = new ResetPassword();
            ResetPassword.OKToReset = false;
            if (pr != null)
            {
                ResetPassword.OKToReset = true;
                ResetGuid = guid;
            }
        }

        public static MyAccountDetails GetMyAccountDetails(string recordId)
        {
            MyAccountDetails details = new MyAccountDetails();

            details.CommonData = DataCache.GetSectionData("CommonData");

            details.Name = new Name();
            details.CustomerAddress = new Address();
            DataTable dt = Touchpoints.GetUserData(recordId, "", "");

            if (dt.Rows.Count > 0)
            {
                DataRow dr = dt.Rows[0];
                details.Name.Title = dr["title"].ToString();
                details.Name.Firstname = dr["forename"].ToString();
                details.Name.Surname = dr["surname"].ToString();
                details.Email = dr["email"].ToString();
                details.TelephoneNumber = dr["telephone"].ToString();
                details.OldPassword = dr["Password"].ToString();
                details.Password = "";
                //details.Record = HttpContext.Current.Session["U_Record"].ToString();
                details.Record = recordId;

                details.CustomerAddress.Line1 = dr["Address1"].ToString();
                details.CustomerAddress.Line2 = dr["Address2"].ToString();
                details.CustomerAddress.Line3 = dr["Address3"].ToString();
                details.CustomerAddress.Line4 = dr["Address4"].ToString();
                details.CustomerAddress.Line5 = dr["Address5"].ToString();
                details.CustomerAddress.PostCode = dr["PostCode"].ToString();

                details.NewsLetter = Convert.ToBoolean(dr["isOnMailingList"]);
            }
            else
            {
                // Error
            }

            return details;
        }

        public SaveReturn SetNewPassword()
        {
            SaveReturn sr = new SaveReturn { IsSuccess = false };

            passwordReset pr = EntityAccess.ReadPasswordReset(x => x.guid == ResetGuid).FirstOrDefault();
            if (pr != null)
            {
                string record = Touchpoints.GetRecordNoFromEmail(pr.email);
                if (record != "")
                {
                    try
                    {
                        using (var axisDataConnection =
                        new SqlConnection(ConfigurationManager.ConnectionStrings[Touchpoints.GetBackOfficeConnection()].ToString()))
                        using (var axisConfigConnection =
                            new SqlConnection(ConfigurationManager.ConnectionStrings[Touchpoints.GetAxisWebConnection()].ToString()))
                        {
                            axisDataConnection.Open();
                            axisConfigConnection.Open();

                            var boLicence = ConfigurationManager.AppSettings["BoLicense"];

                            Service vmerch = new Service(axisDataConnection, new HttpContextWrapper(HttpContext.Current));

                            vmerch.SaveUserIdentity(new UserIdentity
                            {
                                RecordNumber = record,
                                Password = ResetPassword.NewPassword
                            });

                            axisDataConnection.Close();
                            axisConfigConnection.Close();

                            sr.IsSuccess = true;
                        }
                    }
                    catch (Exception)
                    {
                        sr.IsSuccess = false;
                    }
                }
            }

            return sr;
        }

        public static SaveReturn SetNewPassword(string email, string newPassword)
        {
            SaveReturn sr = new SaveReturn { IsSuccess = false };

            if (!string.IsNullOrEmpty(email))
            {
                string record = Touchpoints.GetRecordNoFromEmail(email);
                if (record != "")
                {
                    sr.IsSuccess = Touchpoints.ResetPassword(record, newPassword);
                }
            }

            return sr;
        }

        public static SaveReturn UpdateAccountDetails(MyAccountDetails details, string record)
        {
            var currentDetails = GetMyAccountDetails(record);

            var signUp = new SignUp
            {
                Name = details.Name,
                Address = currentDetails.CustomerAddress,
                UserName = details.Email,
                Password = details.Password ?? currentDetails.OldPassword,
                TelNumber = String.IsNullOrEmpty(details.TelephoneNumber) ? "0" : details.TelephoneNumber,
                Newsletter = currentDetails.NewsLetter
            };

            return Touchpoints.SaveUser(signUp, record);
        }

        public static SaveReturn UpdateAddress(MyAccountDetails details)
        {
            var currentDetails = GetMyAccountDetails(HttpContext.Current.Session["U_Record"].ToString());

            var signUp = new SignUp
            {
                Name = currentDetails.Name,
                Address = details.CustomerAddress,
                UserName = currentDetails.Email,
                Password = currentDetails.OldPassword,
                TelNumber = String.IsNullOrEmpty(currentDetails.TelephoneNumber) ? "0" : currentDetails.TelephoneNumber,
                Newsletter = currentDetails.NewsLetter
            };

            return Touchpoints.SaveUser(signUp, details.Record);
        }

        public static SaveReturn CreateUser(SignUp suDetails)
        {
            return Touchpoints.SaveUser(suDetails);
        }

        public static void CreateCustomer(CheckoutDetails cd, AccountApplicationDetails aad = null)
        {
            int websiteId = Convert.ToInt32(ConfigurationManager.AppSettings["WebsiteId"]);

            if (websiteId == 1)
            {
                EntityAccess.SaveCustomer(new Customer
                {
                    WebsiteFk = websiteId,
                    AccountNumber = string.IsNullOrEmpty(cd.AccountNumber) ? "@" : cd.AccountNumber,
                    CustomerTypeId = aad != null ? aad.CustomerType == 0 ? (short)1 : aad.CustomerType : (short)1,
                    OriginalEmailAddress = cd.Email
                });
            }
        }

        public static SaveReturn ProcessCreditAccountApplication(CheckoutDetails cd, AccountApplicationDetails aad, Dictionary<string, string> data)
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = false;
            int websiteId = Convert.ToInt32(ConfigurationManager.AppSettings["WebsiteId"]);
            BasketTotals bt = new BasketTotals();
            if (HttpContext.Current.Session["B_BasketTotals"] != null)
            {
                bt = (BasketTotals)HttpContext.Current.Session["B_BasketTotals"];
            }

            string rf = "";
            decimal amt = 0;
            string email = aad.ContactEmail;

            if (cd.DeliveryAddress != null) // Indicates application is via an Order
            {
                rf = cd.BackOfficeOrderRef ?? cd.Reference;
                amt = bt.GrandTotalExcVat;
                email = cd.Email;
            }


            // create entities and pass to function for saving in the database
            sr.IsSuccess = EntityAccess.InsertCreditAccountApplication(new Account
            {
                StatusId = aad.Status,
                OrganisationTypeId = aad.OrganisationType,
                SectorId = aad.Sector,
                TradingName = aad.TradingName,
                ContactName = aad.ContactName,
                ContactEmailAddress = aad.ContactEmail,
                ContactTelephoneNo = aad.ContactTelephoneNumber,
                TotalStaffCountId = aad.StaffCount,
                OrderStaffCountId = aad.StaffOrderCount,
                EstMonthlySpend = aad.MonthlySpend,
                CreditLimit = null,
                CompanyRegNo = aad.CompanyRegistrationNumber ?? "",
                CompanyVatNo = aad.CompanyVATNumber ?? "",
                AcceptStandardTerms = true,
                AcceptCreditTerms = true,
                DateOfApplication = DateTime.Now,
                DateLastUpdated = DateTime.Now,
                FirstOrderRef = rf,
                FirstOrderAmt = amt

            },
            new Customer()
            {
                WebsiteFk = websiteId,
                AccountNumber = string.IsNullOrEmpty(cd.AccountNumber) ? "@" : cd.AccountNumber,
                CustomerTypeId = aad.CustomerType,
                OriginalEmailAddress = email
            },
            new Billing
            {
                ContactName = aad.BillingFullName,
                ContactEmailAddress = aad.BillingEmail,
                ContactTelephoneNo = aad.BillingTelephoneNumber,
                AddressLine1 = aad.BillingAddress.Line1,
                AddressLine2 = aad.BillingAddress.Line2,
                AddressLine3 = aad.BillingAddress.Line3,
                AddressLine4 = aad.BillingAddress.Line4,
                AddressLine5 = aad.BillingAddress.Line5,
                PostCode = aad.BillingAddress.PostCode,
                Country = aad.BillingAddress.Country,
                DirectDebit = aad.DirectDebit
            });

            if (sr.IsSuccess)
            {
                var replacements = new Dictionary<string, string>();
                replacements.Add("[email]", email);
                replacements.Add("[sitename]", Utilities.GetItemFromDict(DataCache.GetSectionData("CommonData"), "SiteName"));
                Utilities.SendEmail(Utilities.GetItemFromDict(data, "SalesEmail"), Utilities.GetItemFromDict(data, "CreditAccountEmail"), "New credit account application", "CreditAccountEmailData", replacements);
                sr.Html = "<div class=\"g-p-40 g-b-1-p g-flex-allcenter\">Your credit account application has been successfully submitted. We will update you on progress via email. </div>";
            }
            else
            {
                sr.Html = "There was a problem processing the form, please check the form and try again. If problems persist, please contact Customer Services.";
            }

            return sr;
        }
    }
}
