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
using System.Web.Mvc;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.ComponentModel.DataAnnotations;

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
        public List<VoucherPromo> VoucherList { get; set; }
        public List<SagePayToken> CardList { get; set; }
        public List<MiniProductEntry> PurchasedProducts { get; set; }
        public ResetPassword ResetPassword { get; set; }
        public string ResetGuid { get; set; }
        public List<SelectListItem> OrderList { get; set; }
        [Required]
        public List<SelectListItem> ResolutionList { get; set; }
        public List<SelectListItem> ResolutionListCR { get; set; }
        public List<SelectListItem> ReasonList { get; set; }


        public void GetOrderData()
        {
            DataTable dt = GetOrderDetails();

            OrderHistoryList = new List<OrderHistory>();
            if (dt.Rows.Count > 0)
            {
                foreach (DataRow dr in dt.Rows)
                {
                    OrderHistory oh = new OrderHistory
                    {
                        OrderNumber = dr["OrderNumber"].ToString(),
                        CustomerReference = dr["CustomerReference"].ToString(),
                        OrderDate = Convert.ToDateTime(dr["OrderDate"]),
                        TotalNet = Convert.ToDecimal(dr["TotalNet"]),
                        TotalVat = Convert.ToDecimal(dr["TotalVat"]),
                        TotalDel = 0,
                        Total = Convert.ToDecimal(dr["TotalNet"]) + Convert.ToDecimal(dr["TotalVat"]),
                        TrackingLinks = dr["TrackingLinks"].ToString(),
                        Status = int.Parse(dr["Status"].ToString()),
                        StatusDesc = dr["StatusDesc"].ToString()
                    };
                    OrderHistoryList.Add(oh);
                }
            }
        }

        //public DataTable GetOrderLines(string orderno)
        //{
        //    DataTable dt = GetOrderDetail(orderno).Tables[0];

        //    return dt;
        //}

        public void GetOrder(string orderno)
        {
            DataSet ds = GetOrderDetail(orderno);

            decimal acctotal = 0;
            bool firsttime = true;

            Order = new OrderHistory();
            //OrderHistory oh = new OrderHistory();

            DataTable dt = ds.Tables[0];
            if (dt.Rows.Count > 0)
            {
                DataRow dr = dt.Rows[0];
                //orderno = dr["OrderNumber"].ToString();
                Order = new OrderHistory();
                Order.OrderNumber = dr["OrderNumber"].ToString();
                Order.CustomerReference = dr["CustomerReference"].ToString();
                Order.OrderDate = Convert.ToDateTime(dr["OrderDate"]);
                Order.TotalNet = Convert.ToDecimal(dr["TotalNet"]);
                Order.TotalVat = Convert.ToDecimal(dr["TotalVat"]);
                Order.TotalDel = 0;
                Order.Total = Order.TotalNet + Order.TotalVat;
                Order.Status = int.Parse(dr["Status"].ToString());
                Order.DeliveryAddress = new Address();
                Order.DeliveryAddress.Line1 = dr["DeliveryAddress1"].ToString();
                Order.DeliveryAddress.Line2 = dr["DeliveryAddress2"].ToString();
                Order.DeliveryAddress.Line3 = dr["DeliveryAddress3"].ToString();
                Order.DeliveryAddress.Line4 = dr["DeliveryAddress4"].ToString();
                Order.DeliveryAddress.Line5 = dr["DeliveryAddress5"].ToString();
                Order.DeliveryAddress.PostCode = dr["DeliveryPostcode"].ToString();
                Order.BillingAddress = new Address();
                Order.BillingAddress.Line1 = dr["BillingAddress1"].ToString();
                Order.BillingAddress.Line2 = dr["BillingAddress2"].ToString();
                Order.BillingAddress.Line3 = dr["BillingAddress3"].ToString();
                Order.BillingAddress.Line4 = dr["BillingAddress4"].ToString();
                Order.BillingAddress.Line5 = dr["BillingAddress5"].ToString();
                Order.BillingAddress.PostCode = dr["BillingPostcode"].ToString();
            }

            Order.OrderLines = new List<OrderLine>();
            dt = ds.Tables[1];
            if (dt.Rows.Count > 0)
            {
                foreach (DataRow dr in dt.Rows)
                {
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
                    ol.BrandFlag = int.Parse(dr["BrandFlag"].ToString()) == 1 ? BrandFlag.Original : BrandFlag.Compatible;

                    ol.XSStockReference = dr["XSStockReference"].ToString();
                    ol.XSSaving = 0;
                    if (!string.IsNullOrEmpty(ol.XSStockReference) && ol.BrandFlag == BrandFlag.Original)
                    {
                        // Get details about the Cross Selling product

                        ProductViewModel pvm = new ProductViewModel();
                        pvm.GetProductDetail(ol.XSStockReference);
                        ol.XSProduct = pvm.Product;
                        if (pvm.Product != null)
                        {
                            ol.XSProduct.Url = "/" + ol.XSProduct.Url;
                            if (pvm.XsProduct != null)
                            {
                                if (Convert.ToBoolean(HttpContext.Current.Session["U_IsTradeCustomer"]))
                                {
                                    ol.XSSaving = pvm.XsProduct.PriceTrExVat - pvm.Product.PriceTrExVat;
                                }
                                else
                                {
                                    ol.XSSaving = Decimal.Divide(pvm.XsProduct.PriceRetIncVat, VatMultiplier) - Decimal.Divide(pvm.Product.PriceRetIncVat, VatMultiplier);
                                }
                            }
                        }
                    }

                    if (ol.IsVoucher)
                    {
                        Order.TotalVoucher = ol.SubTotal * -1;
                    }
                    if (ol.IsDelivery)
                    {
                        Order.TotalDel = ol.SubTotal;
                    }
                    if (!ol.IsVoucher && !ol.IsDelivery)
                    {
                        acctotal = acctotal + ol.SubTotal;
                    }
                    Order.OrderLines.Add(ol);
                }
            }

            Order.AccTotal = acctotal;


            //if (dt.Rows.Count > 0)
            //{
            //    OrderHistory oh = new OrderHistory();

            //    foreach (DataRow dr in dt.Rows)
            //    {
            //        if (firsttime)
            //        {
            //            firsttime = false;
            //            orderno = dr["OrderNumber"].ToString();
            //            oh = new OrderHistory();
            //            oh.OrderNumber = dr["OrderNumber"].ToString();
            //            oh.CustomerReference = dr["CustomerReference"].ToString();
            //            oh.OrderDate = Convert.ToDateTime(dr["OrderDate"]);
            //            oh.TotalNet = Convert.ToDecimal(dr["TotalNet"]);
            //            oh.TotalVat = Convert.ToDecimal(dr["TotalVat"]);
            //            oh.TotalDel = 0;
            //            oh.Total = oh.TotalNet + oh.TotalVat;
            //            oh.Status = int.Parse(dr["Status"].ToString());
            //            oh.DeliveryAddress = new Address();
            //            oh.DeliveryAddress.Line1 = dr["DeliveryAddress1"].ToString();
            //            oh.DeliveryAddress.Line2 = dr["DeliveryAddress2"].ToString();
            //            oh.DeliveryAddress.Line3 = dr["DeliveryAddress3"].ToString();
            //            oh.DeliveryAddress.Line4 = dr["DeliveryAddress4"].ToString();
            //            oh.DeliveryAddress.Line5 = dr["DeliveryAddress5"].ToString();
            //            oh.DeliveryAddress.PostCode = dr["DeliveryPostcode"].ToString();
            //            oh.BillingAddress = new Address();
            //            oh.BillingAddress.Line1 = dr["BillingAddress1"].ToString();
            //            oh.BillingAddress.Line2 = dr["BillingAddress2"].ToString();
            //            oh.BillingAddress.Line3 = dr["BillingAddress3"].ToString();
            //            oh.BillingAddress.Line4 = dr["BillingAddress4"].ToString();
            //            oh.BillingAddress.Line5 = dr["BillingAddress5"].ToString();
            //            oh.BillingAddress.PostCode = dr["BillingPostcode"].ToString();
            //            oh.OrderLines = new List<OrderLine>();
            //        }

            //        OrderLine ol = new OrderLine();
            //        ol.Reference = dr["Reference"].ToString().Trim();
            //        ol.AltRef = dr["AltRef"].ToString();
            //        ol.Description = dr["Description"].ToString();
            //        ol.Quantity = Convert.ToInt16(dr["Quantity"]);
            //        ol.PriceNet = Convert.ToDecimal(dr["PriceNet"]) / ol.Quantity;
            //        ol.PriceVat = Convert.ToDecimal(dr["PriceVat"]);

            //        ol.ImageUrl = dr["ImageUrl"].ToString();
            //        ol.Availability = int.Parse(dr["Availability"].ToString());
            //        ol.IsDiscontinued = Convert.ToBoolean(dr["IsDiscontinued"]);
            //        if (ol.IsDiscontinued)
            //        {
            //            ol.Availability = 0;
            //        }
            //        ol.IsVoucher = Convert.ToBoolean(dr["IsVoucher"]);
            //        ol.IsDelivery = Convert.ToBoolean(dr["IsDelivery"]);
            //        ol.SubTotal = ol.PriceNet * ol.Quantity;

            //        if (ol.IsVoucher)
            //        {
            //            oh.TotalVoucher = ol.SubTotal * -1;
            //        }
            //        if (ol.IsDelivery)
            //        {
            //            oh.TotalDel = ol.SubTotal;
            //        }
            //        if (!ol.IsVoucher && !ol.IsDelivery)
            //        {
            //            acctotal = acctotal + ol.SubTotal;
            //        }
            //        oh.OrderLines.Add(ol);
            //    }
            //    oh.AccTotal = acctotal;
            //    Order = oh;
            //}
        }

        public string CreateReturnField(string field, FormCollection formfields)
        {
            StringBuilder sb = new StringBuilder();
            if (field == "Desc")
            {
                if (formfields["ReturnReason"] == "Customer Return")
                {
                    sb.Append("The Box " + (formfields["Unopened"].Contains("true") ? " is UNOPENED | " : " has been OPENED | "));
                }
                if (!string.IsNullOrEmpty(formfields["ProductError"]))
                {
                    sb.Append("PRODUCT DELIVERED: " + formfields["ProductError"] + " | ");
                }
                if (Authentication.IsNotAuthenticated())
                {
                    sb.Append("FULL NAME: " + formfields["FullName"] + " | ");
                    sb.Append("POSTCODE: " + formfields["Postcode"] + " | ");
                    sb.Append("TELNO: " + formfields["TelNo"] + " | ");
                }
                foreach (var x in formfields.Keys)
                {
                    string[] stringArray1 = { "Info" };
                    if (stringArray1.Any(x.ToString().Contains))
                    {
                        sb.Append(@x.ToString().ToUpper() + ": " + formfields[x.ToString()] + " | ");
                    }
                }
            }
            if (field == "Acc")
            {
                if (Authentication.IsAuthenticated())
                {
                    foreach (var x in formfields.Keys)
                    {
                        if (x.ToString().Contains("Qty-"))
                        {
                            if (formfields[x.ToString()].ToString().Contains(","))
                            {
                                continue;
                            }
                            if (Int32.Parse(formfields[x.ToString()].ToString()) > 0)
                            {
                                string itemno = x.ToString().Split('-')[1];
                                sb.Append(formfields["Qty-" + itemno] + " X " + @itemno + " - " + formfields["Desc-" + itemno] + " | " + "\r\n");
                            }
                        }
                    }
                }
                else
                {
                    sb.Append(formfields["Items"]);
                }
            }

            return sb.ToString();
        }
        public async Task CreateCaseAsync(Dictionary<string, string> dict)
        {
            ServicePointManager.Expect100Continue = true;
            Utilities.SetTlsVersion();
            HttpClient client = new HttpClient();

            string url = "https://webto.salesforce.com/servlet/servlet.WebToCase?encoding=UTF-8";
            var response = await client.PostAsync(url, new FormUrlEncodedContent(dict));
            var responseString = await response.Content.ReadAsStringAsync();
            //JObject j = JObject.Parse(responseString);

        }

        public DataTable GetOrderDetails()
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

        private DataSet GetOrderDetail(string orderno)
        {
            int websiteId = Convert.ToInt32(ConfigurationManager.AppSettings["WebsiteId"]);

            List<SqlParameter> sqlParms = new List<SqlParameter>();
            SqlParameter sqlParm = new SqlParameter("@WebsiteId", SqlDbType.Int);
            sqlParm.Value = websiteId;
            sqlParms.Add(sqlParm);
            sqlParm = new SqlParameter("@OrderNo", SqlDbType.VarChar);
            sqlParm.Value = orderno;
            sqlParms.Add(sqlParm);
            return SQL.ExecuteReadStoredProcedure("netgiantmasterdata", "ngmd.GetOrderDetail", sqlParms, "results");
        }

        public DataTable GetReturnOrderDetail(string orderno)
        {
            int websiteId = Convert.ToInt32(ConfigurationManager.AppSettings["WebsiteId"]);

            List<SqlParameter> sqlParms = new List<SqlParameter>();
            SqlParameter sqlParm = new SqlParameter("@WebsiteId", SqlDbType.Int);
            sqlParm.Value = websiteId;
            sqlParms.Add(sqlParm);
            sqlParm = new SqlParameter("@OrderNo", SqlDbType.VarChar);
            sqlParm.Value = orderno;
            sqlParms.Add(sqlParm);
            return SQL.ExecuteReadStoredProcedure("netgiantmasterdata", "ngmd.GetReturnOrderDetail", sqlParms, "results").Tables[0];
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
                    sr.IsSuccess = Touchpoints.ResetPassword(record, ResetPassword.NewPassword);

                    //try
                    //{                      
                    //// Debugging - remove
                    //DataSet ds = new DataSet("userRecord");
                    //DataTable dt = new DataTable();
                    //string dbgSelect = @"SELECT TOP 1 new_user, updated_user FROM dbo.Users WHERE record = '" + record + "'";
                    //ds = SQL.ExecuteReadInline(Touchpoints.GetBackOfficeConnection(), dbgSelect);
                    //dt = ds.Tables[0];
                    //if (dt.Rows.Count > 0)
                    //{
                    //    Utilities.LogInformationMessage("Password Change: Record Number: " + record + ", new_user: " + dt.Rows[0]["new_user"].ToString() + ", updated_user: " + dt.Rows[0]["updated_user"].ToString());
                    //}

                    //using (var axisDataConnection =
                    //new SqlConnection(ConfigurationManager.ConnectionStrings[Touchpoints.GetBackOfficeConnection()].ToString()))
                    //using (var axisConfigConnection =
                    //    new SqlConnection(ConfigurationManager.ConnectionStrings[Touchpoints.GetAxisWebConnection()].ToString()))
                    //{
                    //    axisDataConnection.Open();
                    //    axisConfigConnection.Open();

                    //    var boLicence = ConfigurationManager.AppSettings["BoLicense"];

                    //    Service vmerch = new Service(axisDataConnection, new HttpContextWrapper(HttpContext.Current));

                    //    vmerch.SaveUserIdentity(new UserIdentity
                    //    {
                    //        RecordNumber = record,
                    //        Password = ResetPassword.NewPassword
                    //    });

                    //    axisDataConnection.Close();
                    //    axisConfigConnection.Close();

                    //    sr.IsSuccess = true;

                    //    // Debugging - remove
                    //    ds = SQL.ExecuteReadInline(Touchpoints.GetBackOfficeConnection(), dbgSelect);
                    //    dt = ds.Tables[0];
                    //    if (dt.Rows.Count > 0)
                    //    {
                    //        Utilities.LogInformationMessage("Password Change: Record Number: " + record + ", new_user: " + dt.Rows[0]["new_user"].ToString() + ", updated_user: " + dt.Rows[0]["updated_user"].ToString());
                    //    }
                    //}
                    //}
                    //catch (Exception)
                    //{
                    //    sr.IsSuccess = false;
                    //}
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
            if (!string.IsNullOrEmpty(details.Password) && details.OldPassword != details.Password)
            {
                // Password change
                SaveReturn sr1 = SetNewPassword(currentDetails.Email, details.Password);
                if (!sr1.IsSuccess)
                {
                    return sr1;
                }
            }

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

        public static void AddAlert()
        {
            // Add Alert to basket
            BasketContents bc = new BasketContents
            {
                Quantity = 1,
                Description = "Account Application In Progress",
                StockRef = "ACCOUNTAPP",
                Type = 0,
                LineUid = 0,
                PriceInc = 0,
                PriceEx = 0,
                Availability = 1,
                ImageUrl = "unknown.jpg",
                ProductUrl = "",
                IsCompatibleInk = false,
                IsBulky = false,
                ItemType = BasketItemType.Alert
            };

            Basket.Update(bc);
        }

        public static SaveReturn ProcessCreditAccountApplication(CheckoutDetails cd, AccountApplicationDetails aad, Dictionary<string, string> data)
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = false;
            int websiteId = Convert.ToInt32(ConfigurationManager.AppSettings["WebsiteId"]);
            BasketTotals bt = Utilities.LoadSession<BasketTotals>("B_BasketTotals");
            int tradeStatusId = EntityAccess.ReadCustLookUp(x => x.LookupType.LookupTypeName == "Account Status" && x.LookupName == "None").FirstOrDefault().LookupID;
            int accountStatusId = EntityAccess.ReadCustLookUp(x => x.LookupType.LookupTypeName == "Account Status" && x.LookupName == "Submitted").FirstOrDefault().LookupID;


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
            sr.IsSuccess = EntityAccess.SaveCreditAccountApplication(new Account
            {
                StatusId = (short)accountStatusId,
                TradeStatusId = (short)tradeStatusId,
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
                FirstOrderAmt = amt,
                IsAccountCustomer = true

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
                replacements.Add("[accounttype]", "Credit");
                Utilities.SendEmail(Utilities.GetItemFromDict(data, "SalesEmail"), Utilities.GetItemFromDict(data, "CreditAccountEmail"), "New credit account application", "CreditAccountEmailData", replacements);
                sr.Html = "<div class=\"g-p-40 g-b-1-p g-flex-allcenter\">Your credit account application has been successfully submitted. We will update you on progress via email. </div>";
            }
            else
            {
                sr.Html = "There was a problem processing the form, please check the form and try again. If problems persist, please contact Customer Services.";
            }

            return sr;
        }

        public static SaveReturn ProcessTradeAccountApplication(CheckoutDetails cd, TradeApplicationDetails tad, Dictionary<string, string> data)
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = false;
            int websiteId = Convert.ToInt32(ConfigurationManager.AppSettings["WebsiteId"]);
            BasketTotals bt = Utilities.LoadSession<BasketTotals>("B_BasketTotals");
            int tradeStatusId = EntityAccess.ReadCustLookUp(x => x.LookupType.LookupTypeName == "Account Status" && x.LookupName == "Submitted").FirstOrDefault().LookupID;
            int accountStatusId = EntityAccess.ReadCustLookUp(x => x.LookupType.LookupTypeName == "Account Status" && x.LookupName == "None").FirstOrDefault().LookupID;
            int customerTypeId = EntityAccess.ReadCustLookUp(x => x.LookupType.LookupTypeName == "Customer Type" && x.LookupName == "Business").FirstOrDefault().LookupID;

            string rf = "";
            decimal amt = 0;
            string email = tad.ContactEmail;

            if (EntityAccess.IsTradeAccount(email))
            {
                sr.IsSuccess = false;
                return sr;
            }

            if (cd.DeliveryAddress != null) // Indicates application is via an Order
            {
                rf = cd.BackOfficeOrderRef ?? cd.Reference;
                amt = bt.GrandTotalExcVat;
                email = cd.Email;
            }
            // Retrieve the account number
            UserAccountInfo accountInfo = Authentication.GetAccountInfo(email);

            // create entities and pass to function for saving in the database
            sr.IsSuccess = EntityAccess.SaveTradeAccountApplication(new Account
            {
                TradeStatusId = (short)tradeStatusId,
                StatusId = (short)accountStatusId,
                //OrganisationTypeId = tad.OrganisationType,
                //SectorId = aad.Sector,
                TradingName = tad.TradingName,
                ContactName = tad.ContactName,
                ContactEmailAddress = tad.ContactEmail,
                ContactTelephoneNo = tad.ContactTelephoneNumber,
                //TotalStaffCountId = tad.StaffCount,
                //OrderStaffCountId = tad.StaffOrderCount,
                EstMonthlySpend = 0,
                CreditLimit = null,
                CompanyRegNo = tad.CompanyRegistrationNumber ?? "",
                CompanyVatNo = tad.CompanyVATNumber ?? "",
                AcceptStandardTerms = true,
                //AcceptCreditTerms = true,
                DateOfApplication = DateTime.Now,
                DateLastUpdated = DateTime.Now,
                FirstOrderRef = rf,
                FirstOrderAmt = amt,
                IsTradeCustomer = true,
                NumberOffices = tad.NumberOffices,
                NumberPrinters = tad.NumberPrinters
            },
            new Customer()
            {
                WebsiteFk = websiteId,
                AccountNumber = accountInfo.AccountNumber,
                CustomerTypeId = (short)customerTypeId,
                OriginalEmailAddress = email
            },
            new Billing
            {
                ContactName = tad.ContactName,
                ContactEmailAddress = tad.ContactEmail,
                ContactTelephoneNo = tad.ContactTelephoneNumber,
                AddressLine1 = tad.BillingAddress.Line1,
                AddressLine2 = tad.BillingAddress.Line2,
                AddressLine3 = tad.BillingAddress.Line3,
                AddressLine4 = tad.BillingAddress.Line4,
                AddressLine5 = tad.BillingAddress.Line5,
                PostCode = tad.BillingAddress.PostCode,
                Country = tad.BillingAddress.Country
                //DirectDebit = tad.DirectDebit
            });

            if (sr.IsSuccess)
            {
                var replacements = new Dictionary<string, string>();
                replacements.Add("[email]", email);
                replacements.Add("[sitename]", Utilities.GetItemFromDict(DataCache.GetSectionData("CommonData"), "SiteName"));
                replacements.Add("[accounttype]", "Trade");
                Utilities.SendEmail(Utilities.GetItemFromDict(data, "SalesEmail"), Utilities.GetItemFromDict(data, "TradeAccountEmail"), "New trade account application", "CreditAccountEmailData", replacements);
                sr.Html = "<div class=\"g-p-40 g-b-1-p g-flex-allcenter\">Your trade account application has been successfully submitted. We will progress your application as quickly as possible and update you on " +
                    "progress via email. In the meantime, as a gesture of goodwill you can enjoy our Trade Prices right now by just logging into your account <a href=\"/\">Continue Shopping</a>.</div>";
                if (Authentication.IsAuthenticated())
                {
                    HttpContext.Current.Session["U_IsTradeCustomer"] = true;
                }
            }
            else
            {
                sr.Html = "There was a problem processing the form, please check the form and try again. If problems persist, please contact Customer Services.";
            }

            return sr;
        }


        public static List<OrderLine> RemoveUnrequiredOrderLines(List<OrderLine> OrderLinesFromModel)
        {
            if (OrderLinesFromModel != null)
            {
                List<int> OrderLinesToRemove = new List<int>();

                foreach (OrderLine myOrderLine in OrderLinesFromModel)
                {
                    if (
                        myOrderLine.IsVoucher == true ||
                        myOrderLine.IsDelivery == true ||
                        string.IsNullOrEmpty(myOrderLine.Description) == true ||
                        myOrderLine.Description.Contains("Interim") == true ||
                        myOrderLine.Description.Contains("Application") == true
                        )
                    {
                        OrderLinesToRemove.Add(OrderLinesFromModel.IndexOf(myOrderLine));
                    }
                }

                //when using RemoveAt the maths requires reversing the order
                OrderLinesToRemove.Reverse();

                foreach (int x in OrderLinesToRemove)
                {
                    OrderLinesFromModel.RemoveAt(x);
                }
            }

            return OrderLinesFromModel;
        }
    }
}
