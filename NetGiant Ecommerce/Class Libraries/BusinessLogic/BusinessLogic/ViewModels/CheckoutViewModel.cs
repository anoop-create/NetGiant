using System;
using System.Collections.Generic;
using System.Linq;
using DataAccess.EntityFramework;
using DataAccess.Utilities;
using System.Data;
using System.Data.SqlClient;
using System.Web;
using System.Configuration;
using System.Web.Mvc;
using Newtonsoft.Json;
using VMerchantWrapper.Entities;
using VmVoucherType = VMerchantWrapper.Entities.VoucherType;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Text;
using static BusinessLogic.MyOpayo;

namespace BusinessLogic.ViewModels
{
    public class CheckoutViewModel : CommonViewModel
    {
        public CheckoutViewModel()
        {
            CheckoutData = DataCache.GetSectionData("CheckoutData");
            BasketTotals = Utilities.LoadSession<BasketTotals>("B_BasketTotals");
            CheckoutDetails = new CheckoutDetails();
            CardList = new List<SagePayToken>();
            EmailData = DataCache.GetSectionData("EmailData");
            JsonStoreId = 0;

            var route = HttpContext.Current.Request.RequestContext.RouteData.Values;
            string action = String.IsNullOrEmpty(route["action"].ToString()) ? "" : route["action"].ToString().ToLower();
            if (action == "viewbasket" || action == "amazonpaysummary")
            {
                SetBasketCounts();
            }
        }

  
        public string AmazonButtonJSONPayLoad { get; set; }
        public string AmazonButtonSignature { get; set; }
        public string AmazonPayRedirectUrl { get; set; }
        public string AmazonCheckoutSessionId { get; set; }
        public decimal AmazonPayTotalAmount { get; set; }
        public string AmazonBillingAddress { get; set; }
        public string AmazonShippingAddress { get; set; }
        public string AmazonPaymentMethod { get; set; }
        public Dictionary<string, string> CheckoutData { get; set; }
        public BasketTotals BasketTotals { get; set; }
        public CheckoutDetails CheckoutDetails { get; set; }
        public AccountApplicationDetails AccountApplicationDetails { get; set; }
        public List<Address> AdditionalAddresses { get; set; }
        public string AdditionalAddressesJson { get; set; }
        public List<SelectListItem> AdditionalAddressesSel { get; set; }
        public List<SagePayToken> CardList { get; set; }
        public string PayPayClientToken { get; set; }
        public List<deliveryService> DeliveryOptions { get; set; }
        public string DeliveryMessage { get; set; }
        public Dictionary<string, string> EmailData { get; set; }
        public string BasketEmail { get; set; }
        public string BasketItemsEmail { get; set; }
        public string PreviousPage { get; set; }
        public bool PayPalPaid { get; set; }
        public int JsonStoreId { get; set; }
        public string CardIdentifier { get; set; }
        public OpayoCard OpayoCard { get; set; }
        public string BrowserColorDepth { get; set; }
        public string BrowserScreenHeight { get; set; }
        public string BrowserScreenWidth { get; set; }

        public void ExtendBasket()
        {
            string refArray = Utilities.GetStockRefArray(BasketContents);

            List<SqlParameter> sqlParms = new List<SqlParameter>();
            SqlParameter sqlParm = new SqlParameter("@WebsiteId", SqlDbType.Int);
            sqlParm.Value = int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString());
            sqlParms.Add(sqlParm);
            sqlParm = new SqlParameter("@ProductIDArray", SqlDbType.VarChar);
            sqlParm.Value = refArray;
            sqlParms.Add(sqlParm);
            sqlParm = new SqlParameter("@Account", SqlDbType.VarChar);
            sqlParm.Value = HttpContext.Current.Session["U_AccountNo"] != null ? HttpContext.Current.Session["U_AccountNo"].ToString() : " ";

            sqlParms.Add(sqlParm);
            DataTable dt = SQL.ExecuteReadStoredProcedure("netgiantmasterdata", "ngmd.GetSummaryData", sqlParms, "summarydata").Tables[0];

            foreach (DataRow dr in dt.Rows)
            {
                string stockref = dr["axisRef"].ToString();
                int i = BasketContents.FindIndex(x => x.StockRef == stockref);

                if (i != -1)
                {
                    BasketContents[i].Description = dr["Description"].ToString();
                    BasketContents[i].ImageUrl = dr["ImageURL"].ToString();
                    BasketContents[i].Availability = int.Parse(dr["Availability"].ToString());
                    BasketContents[i].GroupName = dr["EBGName"].ToString();
                    BasketContents[i].AffiliateCommissionGroup = dr["AffiliateCommissionGroup"].ToString();
                    BasketContents[i].CrossSellingStockRef = dr["CrossSellingStockRef"].ToString();
                    BasketContents[i].CrossSellingPriceEx = Convert.ToDecimal(dr["CrossSellingPriceEx"]);
                    BasketContents[i].CrossSellingAvailability = int.Parse(dr["CrossSellingAvailability"].ToString());
                    BasketContents[i].CrossSellingDescription = dr["CrossSellingDescription"].ToString();
                    BasketContents[i].ExcludeFromUpSell = dr["ManufacturerName"].ToString() == "HP" ? true : false;
                    BasketContents[i].CrossSellingImageURL = dt.Columns.Contains("CrossSellingImageURL") && dr["CrossSellingImageURL"] != DBNull.Value
                        ? dr["CrossSellingImageURL"].ToString()
                        : "";
                    BasketContents[i].CrossSellingProductUrl = dt.Columns.Contains("CrossSellingProductURL") && dr["CrossSellingProductURL"] != DBNull.Value
                        ? dr["CrossSellingProductURL"].ToString()
                        : "";
                }
            }
            HttpContext.Current.Session["B_BasketArray"] = BasketContents;
        }

        public void GetAddressDetails()
        {
            AdditionalAddresses = new List<Address>();
            DeliveryOptions = new List<deliveryService>();
            if (Authentication.IsAuthenticated())
            {
                DataTable dt = Touchpoints.GetAddressDetails(HttpContext.Current.Session["U_Record"].ToString(), HttpContext.Current.Session["U_Email"].ToString());
                // Populate Address Details
                bool firstrow = true;
                int i = 0;

                foreach (DataRow dr in dt.Rows)
                {
                    if (firstrow)
                    {
                        CheckoutDetails.Name = new Name();
                        CheckoutDetails.Name.Title = dr["Title"].ToString();
                        CheckoutDetails.Name.Firstname = dr["FirstName"].ToString();
                        CheckoutDetails.Name.Surname = dr["Surname"].ToString();
                        CheckoutDetails.RecipientName = CheckoutDetails.Name;
                        CheckoutDetails.Email = dr["Email"].ToString();
                        CheckoutDetails.TelephoneNumber = dr["TelNo"].ToString();
                        CheckoutDetails.BillingAddress = BuildAddress(
                            dr["AddLine1"].ToString(),
                            dr["AddLine2"].ToString(),
                            dr["AddLine3"].ToString(),
                            dr["AddLine4"].ToString(),
                            dr["AddLine5"].ToString(),
                            dr["AddLine6"].ToString()
                        );

                        // Set up default delivery address in Additional Addresses
                        Address add = new Address();
                        add = CheckoutDetails.BillingAddress;
                        if (!InvalidPostCodeCheck(add.PostCode))
                        {
                            add.Id = i;
                            AdditionalAddresses.Add(add);
                            i += 1;
                            CheckoutDetails.DeliveryAddress = CheckoutDetails.BillingAddress;
                        }
                        //CheckoutDetails.DeliveryAddress = CheckoutDetails.BillingAddress;

                        if (dr["AddAddLine2"].ToString() != "" && dr["AddAddLine4"].ToString() != "" && dr["AddAddLine6"].ToString() != "")
                        {
                            add = BuildAddress(
                                dr["AddAddLine1"].ToString(),
                                dr["AddAddLine2"].ToString(),
                                dr["AddAddLine3"].ToString(),
                                dr["AddAddLine4"].ToString(),
                                dr["AddAddLine5"].ToString(),
                                dr["AddAddLine6"].ToString()
                            );

                            if (!InvalidPostCodeCheck(add.PostCode))
                            {
                                if (add.Line1 != CheckoutDetails.BillingAddress.Line1
                                || add.Line2 != CheckoutDetails.BillingAddress.Line2
                                || add.Line3 != CheckoutDetails.BillingAddress.Line3
                                || add.Line4 != CheckoutDetails.BillingAddress.Line4
                                || add.Line5 != CheckoutDetails.BillingAddress.Line5
                                || add.PostCode != CheckoutDetails.BillingAddress.PostCode)
                                {
                                    add.Id = i;
                                    AdditionalAddresses.Add(add);
                                    i += 1;
                                }
                            }
                        }
                        else
                        {
                            if (!InvalidPostCodeCheck(CheckoutDetails.BillingAddress.PostCode))
                            {
                                CheckoutDetails.DeliveryAddress = CheckoutDetails.BillingAddress;
                            }
                        }
                        CheckoutDetails.Newsletter = Convert.ToBoolean(dr["MailingList"]);

                        firstrow = false;
                    }
                    else
                    {
                        if (dr["AddAddLine2"].ToString() != "" && dr["AddAddLine4"].ToString() != "" && dr["AddAddLine6"].ToString() != "")
                        {
                            Address add = BuildAddress(
                                dr["AddAddLine1"].ToString(),
                                dr["AddAddLine2"].ToString(),
                                dr["AddAddLine3"].ToString(),
                                dr["AddAddLine4"].ToString(),
                                dr["AddAddLine5"].ToString(),
                                dr["AddAddLine6"].ToString()
                            );

                            if (!InvalidPostCodeCheck(add.PostCode))
                            {
                                if (add.Line1 != CheckoutDetails.BillingAddress.Line1
                                || add.Line2 != CheckoutDetails.BillingAddress.Line2
                                || add.Line3 != CheckoutDetails.BillingAddress.Line3
                                || add.Line4 != CheckoutDetails.BillingAddress.Line4
                                || add.Line5 != CheckoutDetails.BillingAddress.Line5
                                || add.PostCode != CheckoutDetails.BillingAddress.PostCode)
                                {
                                    add.Id = i;
                                    AdditionalAddresses.Add(add);
                                    i += 1;
                                }
                            }
                        }
                    }
                }
                if (AdditionalAddresses.Count > 1)
                {
                    AdditionalAddressesJson = JsonConvert.SerializeObject(AdditionalAddresses);
                    AdditionalAddressesSel = AdditionalAddresses.Select(x => new SelectListItem
                    {
                        Text = x.Line1 + ", " + x.PostCode,
                        Value = x.Id.ToString()
                    })
                .ToList();
                }
                if (CheckoutDetails.DeliveryAddress != null)
                {
                    try
                    {
                        CheckPostcode(CheckoutDetails.DeliveryAddress.PostCode);
                        DeliveryOptions = RetrieveDeliveryOptions(CheckoutDetails.DeliveryAddress.PostCode);
                    }
                    catch (Exception)
                    {
                    }

                    if (DeliveryOptions.Count > 0)
                    {
                        CheckoutDetails.DeliveryServiceId = DeliveryOptions[0].DeliveryServiceId;
                    }
                }
            }
        }

        public void GetCustomerType()
        {
            var customer = EntityAccess.GetCustomer(w => w.OriginalEmailAddress == CheckoutDetails.Email);

            if (customer != null)
            {
                AccountApplicationDetails.CustomerType = customer.CustomerTypeId;
            }
        }

        public void GetCreditApplicationDetails()
        {
            var account = EntityAccess.GetAccountDetails(w => w.Customer.OriginalEmailAddress == CheckoutDetails.Email);

            if (account != null)
            {
                var billing = EntityAccess.GetBillingDetails(w => w.CustomerFk == account.CustomerFk);

                if (billing != null)
                {

                    AccountApplicationDetails.TradingName = account.TradingName;
                    AccountApplicationDetails.ContactName = account.ContactName;
                    AccountApplicationDetails.ContactEmail = account.ContactEmailAddress;
                    AccountApplicationDetails.ContactTelephoneNumber = account.ContactTelephoneNo;
                    AccountApplicationDetails.OrganisationType = account.OrganisationTypeId;
                    AccountApplicationDetails.CompanyRegistrationNumber = account.CompanyRegNo;
                    AccountApplicationDetails.CompanyVATNumber = account.CompanyVatNo ?? "";
                    AccountApplicationDetails.Sector = account.SectorId;
                    AccountApplicationDetails.StaffCount = account.TotalStaffCountId;
                    AccountApplicationDetails.StaffOrderCount = account.OrderStaffCountId;
                    AccountApplicationDetails.MonthlySpend = account.EstMonthlySpend ?? 0;
                    AccountApplicationDetails.BillingAddress = new Address
                    {
                        Line1 = billing.AddressLine1,
                        Line2 = billing.AddressLine2,
                        Line3 = billing.AddressLine3,
                        Line4 = billing.AddressLine4,
                        Line5 = billing.AddressLine5,
                        Country = billing.Country,
                        PostCode = billing.PostCode,
                        Id = billing.BillingId
                    };
                    AccountApplicationDetails.BillingFullName = billing.ContactName;
                    AccountApplicationDetails.BillingEmail = billing.ContactEmailAddress;
                    AccountApplicationDetails.BillingTelephoneNumber = billing.ContactTelephoneNo;
                    AccountApplicationDetails.DirectDebit = billing.DirectDebit;
                }
            }
        }

        public void SetStage0Fields()
        {
            CheckoutDetails cd = Utilities.LoadSession<CheckoutDetails>("C_CheckoutDetails");

            cd.ZeroStock = CheckoutDetails.ZeroStock;
            cd.Email = CheckoutDetails.Email;
            //cd.Newsletter = EntityAccess.GetMailingList(x => x.EmailAddress == cd.Email).Count > 0;

            if (Authentication.IsAuthenticated())
            {
                cd.IsNewCustomer = false;
                cd.AccountNumber = HttpContext.Current.Session["U_AccountNo"].ToString();
                cd.AccountRecord = HttpContext.Current.Session["U_Record"].ToString();
                cd.Password = HttpContext.Current.Session["U_Password"].ToString();
                if (Convert.ToBoolean(HttpContext.Current.Session["U_IsAccountCustomer"]))
                {
                    DataTable dt = Touchpoints.GetAccountDetails(cd.AccountNumber);
                    cd.AccountContact = dt.Rows[0]["AccountContact"].ToString();
                    cd.AccountEmail = dt.Rows[0]["AccountEmail"].ToString();
                    cd.AccountTelNo = dt.Rows[0]["AccountTelNo"].ToString();
                    cd.AccountInvoiceAddress = dt.Rows[0]["AccountInvoiceAddress"].ToString();
                }
            }
            else
            {
                cd.IsNewCustomer = true;
            }
            if (HttpContext.Current.Session["B_VoucherCode"] != null)
            { 
                cd.VoucherCode = HttpContext.Current.Session["B_VoucherCode"].ToString();
            }

            HttpContext.Current.Session["C_CheckoutDetails"] = cd;
            CheckoutDetails = cd;

            // Add Voucher to BasketContents
            AddVoucherToBasket();

            Task t = Touchpoints.MailChimpUpdateCartAsync();
        }

        public bool ProcessStage1()
        {
            CheckoutDetails cd = Utilities.LoadSession<CheckoutDetails>("C_CheckoutDetails");
            if (Authentication.IsAuthenticated())
            {
                if (Authentication.FixTempRecord(HttpContext.Current.Session["U_Record"].ToString(), HttpContext.Current.Session["U_Email"].ToString()))
                {
                    cd.AccountNumber = HttpContext.Current.Session["U_AccountNo"].ToString();
                    cd.AccountRecord = HttpContext.Current.Session["U_Record"].ToString();
                }
            }

            if (!SetStage1Fields(cd))
            {
                return false;
            }
            HttpContext.Current.Session["U_CustomerTypeId"] = AccountApplicationDetails == null
                ? 1
                : AccountApplicationDetails.CustomerType;

            // Write Axis Draft Order
            var createDraftOrder = Touchpoints.SaveOrder(cd, OrderStatus.Draft);

            HttpContext.Current.Session["C_CheckoutDetails"] = cd;
            CheckoutDetails = cd;

            if (Authentication.IsAuthenticated())
            {
                CardList = EntityAccess.ReadSagePayTokens(x => x.account == CheckoutDetails.AccountNumber && x.email == CheckoutDetails.Email && x.deleted == 0 && x.card_type != "");
            }
            // Check card expired
            int currYear = Int32.Parse(DateTime.Now.ToString("yy"));
            int currMonth = DateTime.Now.Month;
            cd.UseASavedCard = false;
            if (CardList.Count > 0)
            {
                foreach (SagePayToken spt in CardList)
                {
                    int cardYear = int.Parse(spt.expiry_date.Substring(2, 2));
                    int cardMonth = int.Parse(spt.expiry_date.Substring(0, 2));
                    if (cardYear > currYear || (currYear == cardYear && cardMonth >= currMonth))
                    {
                        cd.UseASavedCard = true;
                        cd.SagePayCardId = spt.id + "_" + spt.sid;
                        cd.CardType = spt.card_type;
                    }
                }
            }
            foreach (SagePayToken spg in CardList)
            {
                switch (spg.card_type)
                {
                    case "MASTERCARD":
                    case "MCDEBIT":
                    case "MC":
                    case "DEBITMASTERCARD":
                        {
                        spg.CardName = "Master Card";
                        break;
                    }
                    case "VISA":                    
                    case "VISADEBIT":
                    case "VISAELECTRON":
                    case "DELTA":
                    case "UKE":
                    {
                        spg.CardName = "Visa / Delta";
                        break;
                    }
                    case "AMEX":
                    case "AMERICANEXPRESS":
                        {
                        spg.CardName = "American Express";
                        break;
                    }
                    case "MAESTRO":
                    {
                        spg.CardName = "Maestro";
                        break;
                    }
                    case "SOLO":
                        {
                            spg.CardName = "Solo";
                            break;
                        }
                    default:
                    {
                        spg.CardName = "";
                        break;
                    }
                }
            }

            return true;
        }

        public bool ProcessStage2()
        {
            CheckoutDetails cd = Utilities.LoadSession<CheckoutDetails>("C_CheckoutDetails");
            if (Authentication.IsAuthenticated())
            {
                if (Authentication.FixTempRecord(HttpContext.Current.Session["U_Record"].ToString(), HttpContext.Current.Session["U_Email"].ToString()))
                {
                    cd.AccountNumber = HttpContext.Current.Session["U_AccountNo"].ToString();
                    cd.AccountRecord = HttpContext.Current.Session["U_Record"].ToString();
                }
            }

            if (PreviousPage == "Stage2")
            {
                // We've arrived here from Stage2
                SetStage2Fields(cd);
            }
            else
            {
                // We've arrived here from Stage1
                if (!SetStage1Fields(cd))
                {
                    return false;
                }

                if (cd.PaymentMethod == "PayPal" && string.IsNullOrEmpty(cd.PayPalRef))
                {
                    //if clicking enter when stage 1 PP Payment Method radio button is selected and highlighted we'll come here 
                    return false;
                }

                if (cd.PaymentMethod == PaymentMethod.AccountApplication.ToString())
                {
                    cd.BillingAddress = AccountApplicationDetails.BillingAddress;
                    MyAccountViewModel.AddAlert();
                }

                if (string.IsNullOrEmpty(cd.Reference))
                {
                    // Write Axis Draft Order
                    var createDraftOrder = Touchpoints.SaveOrder(cd, OrderStatus.Draft);
                }
            }

            if (cd.PaymentMethod == "CreditDebit")
            {
                cd.CardLast4Digits = CheckoutDetails.CardLast4Digits;
                cd.SagePayAuthCode = CheckoutDetails.SagePayAuthCode;
                cd.SageToken = CheckoutDetails.SageToken;
                cd.CardType = CheckoutDetails.CardType;
            }

            if (cd.PaymentMethod == "AccountApplication")
            {
                MyAccountViewModel.ProcessCreditAccountApplication(cd, AccountApplicationDetails, CheckoutData);
            }
            else
            {
                MyAccountViewModel.CreateCustomer(cd);
            }

            // Write Axis Completed Order
            var createCompleteOrder = Touchpoints.SaveOrder(cd, OrderStatus.Completed, cd.BackOfficeOrderRef);

            HttpContext.Current.Session["C_CheckoutDetails"] = cd;
            CheckoutDetails = cd;

            return true;
        }

        private bool SetStage1Fields(CheckoutDetails cd)
        {
            CheckUpdateUser(cd);

            Dictionary<string, string> checkoutData = DataCache.GetSectionData("CheckoutData");
            bool isSoftOptIn = Utilities.GetBoolItemFromDict(checkoutData, "IsSoftOptIn");

            cd.Name = CheckoutDetails.Name;
            cd.TelephoneNumber = CheckoutDetails.TelephoneNumber;
            cd.RecipientName = CheckoutDetails.RecipientName;
            cd.DeliveryAddress = CheckoutDetails.DeliveryAddress;
            cd.PaymentMethod = CheckoutDetails.PaymentMethod;
            cd.Newsletter = CheckoutDetails.Newsletter;
            if (isSoftOptIn)
            {
                cd.Newsletter = !CheckoutDetails.NewsletterInverse;
            }            
            cd.SuppressEmail = CheckoutDetails.SuppressEmail;
            cd.BillingAddress = CheckoutDetails.BillingAddress;
            cd.Reference = CheckoutDetails.Reference;
            cd.Password = Authentication.IsAuthenticated() ? cd.Password : CheckoutDetails.Password;
            cd.TotalIncVat = ((BasketTotals)HttpContext.Current.Session["B_BasketTotals"]).GrandTotalIncVat;
            cd.OrderNote = CheckoutDetails.OrderNote ?? "";
            cd.PaymentAmountPaid = BasketTotals.GrandTotalIncVat;

            // Add/Update Delivery to the basket
            cd.DeliveryServiceId = CheckoutDetails.DeliveryServiceId;
            cd.DeliveryMethod = Basket.ProcessDelivery(cd.DeliveryServiceId);

            if (!Authentication.IsAuthenticated())
            {
                // Create user
                var signUp = new SignUp
                {
                    Address = cd.PaymentMethod != "PayPal" ? cd.PaymentMethod == PaymentMethod.AccountApplication.ToString() ? AccountApplicationDetails.BillingAddress : cd.BillingAddress : cd.DeliveryAddress,
                    Name = cd.Name,
                    Newsletter = cd.Newsletter,
                    Password = cd.Password,
                    TelNumber = cd.TelephoneNumber,
                    UserName = cd.Email
                };
                var createUser = Touchpoints.SaveUser(signUp);
                if (!createUser.IsSuccess)
                {
                    return false;
                }                
                HttpContext.Current.Session["U_FullyAuthenticated"] = Authentication.Authenticate(signUp.UserName, signUp.Password);
            }

            if (!Utilities.GetBoolItemFromDict(checkoutData, "IsSoftOptIn"))
            {
                Touchpoints.InsertMailingList(cd.Email, cd.Newsletter, cd.Name.Firstname, cd.Name.Surname);
            }

            Task t = Touchpoints.MailChimpUpdateCartAsync();

            return true;
        }

        private void SetStage2Fields(CheckoutDetails cd)
        {
            cd.UseASavedCard = CheckoutDetails.UseASavedCard;
            cd.SagePayCardId = CheckoutDetails.SagePayCardId;
            cd.CardType = CheckoutDetails.CardType;
            cd.SagePayTxCode = CheckoutDetails.SagePayTxCode;
        }

        public SaveReturn BuildDeliveryOptions(string countrycode, string postcode)
        {
            SaveReturn sr = new SaveReturn();

            try
            {

                DeliveryOptions = RetrieveDeliveryOptions(postcode);
                if (DeliveryOptions.Count > 0)
                {
                    CheckoutDetails.DeliveryServiceId = DeliveryOptions[0].DeliveryServiceId;
                }

                sr.Html = "<h2>OK</h2>";

            } catch (Exception e)
            {
                sr.IsSuccess = false;
                sr.Message = e.Message;
            }

            return sr;
        }

        private Address BuildAddress(string add1, string add2, string add3, string add4, string add5, string add6)
        {
            Address add = new Address();
            add.Line1 = add1;
            add.Line2 = add2;
            add.Line3 = add3;
            add.Line4 = add4;
            add.Line5 = add5;
            add.PostCode = add6;

            return add;
        }

        // Static Functions
        public static List<deliveryService> RetrieveDeliveryOptions(string postcode)
        {
            postcode = postcode.ToUpper();
            deliveryZone dz = GetPostcodeDeliveryZone(postcode);

            List<deliveryService> lds = dz.deliveryLookups
                .OrderBy(x => x.Sequence)
                .Select(x => x.deliveryService)
                .ToList();

            //remove any services that are marked as being not for compatibles only
            if (!Convert.ToBoolean(HttpContext.Current.Session["B_CompatibleInkOnly"]))
            {
                lds.RemoveAll(x => x.IsCompatibleInkOnly);
            }

            //remove any services that are marked as being not for special orders only
            if (Convert.ToBoolean(HttpContext.Current.Session["B_SpecialOrderOnly"]))
            {
                lds.RemoveAll(x => !x.IsSpecialOrderOnly);
            }

            //remove any services that are marked as being for saturday only
            DateTime now = DateTime.Now;
            TimeSpan cutOffTime = new TimeSpan(17, 30, 00);
            if (!(now.DayOfWeek == DayOfWeek.Thursday && now.TimeOfDay > cutOffTime) && !(now.DayOfWeek == DayOfWeek.Friday && now.TimeOfDay < cutOffTime))
            {
                lds.RemoveAll(x => x.IsSaturdayOnly);
            }

            //IF DeliveryDateIsOverridden CMS flag is set to true then remove the SATURDAY options
            var commonData = DataCache.GetSectionData("CommonData");
            bool IsOverRidden = Convert.ToBoolean(commonData["DeliveryDateIsOverridden"]);
            if (IsOverRidden == true)
            {
                lds.RemoveAll(x => x.IsSaturdayOnly);
            }
            
            //provide appropriate services for bulky items
            if (Convert.ToBoolean(HttpContext.Current.Session["B_IsBulky"]))
            {
                lds.RemoveAll(x => !x.IsBulky);
            }
            else
            {
                lds.RemoveAll(x => x.IsBulky);
            }

            //remove any non matching threshold services
            BasketTotals bt = (BasketTotals)HttpContext.Current.Session["B_BasketTotals"];
            decimal baskettotal = bt.TotalExcVat - bt.Voucher - bt.Delivery;
            lds.RemoveAll(x => x.UsesThresholds && (x.ThresholdStart > baskettotal || x.ThresholdEnd < baskettotal));

            return lds;
        }
        
        public static deliveryZone GetPostcodeDeliveryZone(string postcode, bool setIsVatExempt = true)
        {
            postcode = postcode.Replace(" ", "").ToUpper();
            if (postcode.Length < 5)
            {
                // Invalid postcode, return the default zone
                return EntityAccess.ReadDeliveryZonesAndServices(x => x.IsDefault).FirstOrDefault();
            }
            
            string shortpostcode = postcode.Substring(0, postcode.Length - 3);
            string postcodepfx = "";
            int postcodesfx = 0;
            int n;
            switch (shortpostcode.Length)
            {
                case 4:
                    {
                        postcodepfx = shortpostcode.Substring(0, 2);
                        int.TryParse(postcode.Substring(2, 2), out postcodesfx);
                        break;
                    }
                case 3:
                    {
                        if (int.TryParse(postcode.Substring(1, 1), out n))
                        {
                            postcodepfx = shortpostcode.Substring(0, 1);
                            int.TryParse(postcode.Substring(1, 2), out postcodesfx);
                        }
                        else
                        {
                            postcodepfx = shortpostcode.Substring(0, 2);
                            int.TryParse(postcode.Substring(2, 1), out postcodesfx);
                        }
                        break;
                    }
                case 2:
                    {
                        postcodepfx = shortpostcode.Substring(0, 1);
                        int.TryParse(postcode.Substring(1, 1), out postcodesfx);
                        break;
                    }
            }

            int zoneId = 0;
            List<ZoneLookup> lzl = DataCache.BuildZoneLookup();
            List<ZoneLookup> z = lzl.FindAll(x => x.Prefix == postcodepfx);
            foreach (ZoneLookup zl in z)
            {
                if (zl.Type == "Range" && postcodesfx >= zl.From && postcodesfx <= zl.To)
                {
                    zoneId = zl.ZoneId;
                }
                if (zl.Type == "Prefix")
                {
                    zoneId = zl.ZoneId;
                }
            }

            deliveryZone dz = EntityAccess.ReadDeliveryZonesAndServices(x => x.DeliveryZoneId == zoneId || x.IsDefault).FirstOrDefault();

            if (setIsVatExempt)
            {
                HttpContext.Current.Session["D_IsVatExempt"] = !dz.ApplyVat;
            }

            return dz;
        }

        public static void AddVoucherToBasket()
        {
            if (HttpContext.Current.Session["B_VoucherCode"] != null)
            {
                List<BasketContents> lbc = Utilities.LoadSession<List<BasketContents>>("B_BasketArray");
                BasketTotals bt = Utilities.LoadSession<BasketTotals>("B_BasketTotals");
                VoucherPromo v = Utilities.LoadSession<VoucherPromo>("V_Voucher");

                var i = lbc.FindIndex(x => x.ItemType == BasketItemType.Voucher);
                if (i >= 0)
                {
                    // voucher exists, delete the item
                    lbc.Remove(lbc[i]);
                }

                if (v.VoucherTypeFk != (int)VmVoucherType.FreeGift)
                {
                    BasketContents bc = new BasketContents();
                    //bc.IsVoucher = true;
                    bc.VoucherType = (int)v.VoucherTypeFk;
                    bc.StockRef = v.StockRef;
                    bc.Description = v.Description;
                    bc.Quantity = 1;
                    bc.PriceInc = bt.Voucher;
                    bc.PriceEx = bt.Voucher + bt.VoucherVat;
                    if (v.VoucherTypeFk == (int)VmVoucherType.Amount && bt.Voucher != 0)
                        {
                        // Special Case where 'Amount' voucher ensure correct amount
                        bool isVatExempt = HttpContext.Current.Session["D_IsVatExempt"] != null && Convert.ToBoolean(HttpContext.Current.Session["D_IsVatExempt"]);
                        if ((bt.Voucher * -1) < v.Amount && !isVatExempt)
                        {
                            bc.PriceEx = lbc.Sum(x => x.IsVoucherQualifyingItem ? Math.Round(x.PriceEx, 2) * x.Quantity : 0) * -1;
                            bc.PriceInc = lbc.Sum(x => x.IsVoucherQualifyingItem ? Math.Floor(x.PriceInc * 100) * x.Quantity / 100 : 0) * -1;
                            bt.Voucher = bc.PriceInc;
                            bt.VoucherVat = (bc.PriceInc - bc.PriceEx) * -1;
                        }
                        if (isVatExempt)
                        {
                            decimal vatExemptVoucherAmt = (bt.Voucher * Convert.ToDecimal(ConfigurationManager.AppSettings["VatMultiplier"])) - 0.10M;
                            if ((vatExemptVoucherAmt * -1) < v.Amount)
                            {
                                bc.PriceEx = lbc.Sum(x => x.IsVoucherQualifyingItem ? Math.Round(x.PriceEx, 2) * x.Quantity : 0) * -1;
                                bc.PriceInc = lbc.Sum(x => x.IsVoucherQualifyingItem ? Math.Floor(x.PriceInc * 100) * x.Quantity / 100 : 0) * -1;
                                bt.Voucher = bc.PriceEx;
                                bt.VoucherVat = 0;
                            }
                        }
                    }
                    bc.Type = 0;
                    bc.LineUid = 0;
                    bc.ItemType = BasketItemType.Voucher;

                    lbc.Add(bc);
                }

                HttpContext.Current.Session["B_BasketArray"] = lbc;
            }
        }

        public static void CheckPostcode(string postcode)
        {
            //call GetPostcodeDeliveryZone so we update the session's VAT exemption status
            GetPostcodeDeliveryZone(postcode);
            
            Basket.Delete("BADADDRESS");

            postcode = postcode.Replace(" ", "").ToLower();
            if (DataCache.GetFraudulentPostcodes().Contains(postcode))
            {
                BasketContents bc = new BasketContents
                {
                    Quantity = 1,
                    Description = "Invalid Post Code",
                    StockRef = "BADADDRESS",
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
        }

        public static bool InvalidPostCodeCheck(string postcode)
        {
            if (postcode.Length < 5)
            {
                return true;
            }

            bool isInvalid = false;
            Dictionary<string, string> checkoutData = DataCache.GetSectionData("CheckoutData");
            string ep = Utilities.GetItemFromDict(checkoutData, "ExcludedPostCodes");
            if (ep != "")
            {                
                string[] excludedPostcodes = ep.Split(',');

                postcode = postcode.Replace(" ", "").ToUpper();
                string shortpostcode = postcode.Substring(0, postcode.Length - 3);
                string postcodepfx = "";
                int n;
                switch (shortpostcode.Length)
                {
                    case 4:
                        {
                            postcodepfx = shortpostcode.Substring(0, 2);
                            break;
                        }
                    case 3:
                        {
                            if (int.TryParse(postcode.Substring(1, 1), out n))
                            {
                                postcodepfx = shortpostcode.Substring(0, 1);
                            }
                            else
                            {
                                postcodepfx = shortpostcode.Substring(0, 2);
                            }
                            break;
                        }
                    case 2:
                        {
                            postcodepfx = shortpostcode.Substring(0, 1);
                            break;
                        }
                    default:
                        {
                            return true;
                        }
                }

                if (excludedPostcodes.Contains(postcodepfx))
                {
                    isInvalid = true;
                }
            }            

            return isInvalid;
        }

        public void CheckZeroStock(List<BasketContents> lbc = null)
        {
            if (lbc == null)
            {
                lbc = BasketContents;
            }
            List<int> noStock = new List<int> { 2, 3, 4, 8, 11, 12 }; // Availability codes which indicate 0 stock
            if (lbc.Any(x => noStock.Contains(x.Availability)))
            {
                CheckoutDetails.ZeroStock = true;
            }
            if (lbc.Any(x => x.Availability == 10))
            {
                CheckoutDetails.IsSpecialOrder = true;
            }
        }  
        
        public bool IsIrishPostcode(string postcode)
        {
            postcode = postcode.Replace(" ", "").ToLower();
            if (postcode.StartsWith("BT"))
            {
                return true;
            }
            if (Regex.IsMatch(postcode, @"(?:^[AC-FHKNPRTV-Y][0-9]{2}|D6W)[ -]?[0-9AC-FHKNPRTV-Y]{4}$"))
            {
                return true;
            }
            return false;
        }

        // Private Functions
        private void CheckUpdateUser(CheckoutDetails cd)
        {
            if (UserHasChanged(cd))
            {
                // Touchpoints - update the user in Axis
                var currentDetails = MyAccountViewModel.GetMyAccountDetails(HttpContext.Current.Session["U_Record"].ToString());

                var signUp = new SignUp
                {
                    Name = CheckoutDetails.Name,
                    Address = CheckoutDetails.BillingAddress,
                    UserName = currentDetails.Email,
                    Password = currentDetails.OldPassword != "" ? currentDetails.OldPassword : cd.Password ,
                    TelNumber = CheckoutDetails.TelephoneNumber,
                    Newsletter = currentDetails.NewsLetter
                };

                Touchpoints.SaveUser(signUp, HttpContext.Current.Session["U_Record"].ToString());
            }
        }
        
        private bool UserHasChanged(CheckoutDetails cd)
        {
            if (cd.BillingAddress != null && AccountApplicationDetails == null)
            {
                // Billing Address
                if (cd.BillingAddress.Line1 != CheckoutDetails.BillingAddress.Line1)
                    return true;
                if (cd.BillingAddress.Line2 != CheckoutDetails.BillingAddress.Line2)
                    return true;
                if (cd.BillingAddress.Line3 != CheckoutDetails.BillingAddress.Line3)
                    return true;
                if (cd.BillingAddress.Line4 != CheckoutDetails.BillingAddress.Line4)
                    return true;
                if (cd.BillingAddress.Line5 != CheckoutDetails.BillingAddress.Line5)
                    return true;
                if (cd.BillingAddress.Country != CheckoutDetails.BillingAddress.Country)
                    return true;
                if (cd.BillingAddress.PostCode != CheckoutDetails.BillingAddress.PostCode)
                    return true;
            }

            if (cd.Name != null)
            {
                // Billing Name
                if (cd.Name.Title != CheckoutDetails.Name.Title)
                    return true;
                if (cd.Name.Firstname != CheckoutDetails.Name.Firstname)
                    return true;
                if (cd.Name.Surname != CheckoutDetails.Name.Surname)
                    return true;
            }

            if (cd.RecipientName != null)
            {
                // Delivery Name
                if (cd.RecipientName.Title != CheckoutDetails.RecipientName.Title)
                    return true;
                if (cd.RecipientName.Firstname != CheckoutDetails.RecipientName.Firstname)
                    return true;
                if (cd.RecipientName.Surname != CheckoutDetails.RecipientName.Surname)
                    return true;
            }

            if (cd.TelephoneNumber != null)
            {
                // Telephone Number
                if (cd.TelephoneNumber != CheckoutDetails.TelephoneNumber)
                    return true;
            }

            return false;
        }

        private void SetBasketCounts()
        {
            List<BasketContents> lbc = Utilities.LoadSession<List<BasketContents>>("B_BasketArray");

            foreach (BasketContents bc in lbc)
            {
                if (bc.QtyStart == 0 && bc.IsCompatible)
                {
                    bc.QtyStart = bc.Quantity;
                    bc.IsUpsellTriggered = true;
                }
            }

            HttpContext.Current.Session["B_BasketArray"] = lbc;
        }

        private string BuildBasketDetails()
        {
            List<BasketContents> lbc = Utilities.LoadSession<List<BasketContents>>("B_BasketArray");
            BasketTotals bt = Utilities.LoadSession<BasketTotals>("B_BasketTotals");
            string template = EmailData["OrderConfirmationBasket"];
            StringBuilder sb = new StringBuilder(template);

            BasketContents delivery = lbc.FirstOrDefault(x => x.ItemType == BasketItemType.Delivery);
            BasketContents voucher = lbc.FirstOrDefault(x => x.ItemType == BasketItemType.Voucher);

            if (voucher == null)
            {
                sb.Replace("[VoucherDetail]", "");
            }
            else
            {
                sb.Replace("[VoucherDetail]", BuildVoucherDetail(voucher));
            }
            sb.Replace("[DeliveryDescription]", delivery.Description);
            sb.Replace("[DeliveryPrice]", Math.Round(delivery.PriceInc, 2).ToString("0.00"));
            sb.Replace("[SubTotal]", Math.Round((bt.GrandTotalIncVat - bt.Vat), 2).ToString("0.00"));
            sb.Replace("[TotalVAT]", Math.Round(bt.Vat, 2).ToString("0.00"));
            sb.Replace("[GrandTotal]", Math.Round(bt.GrandTotalIncVat, 2).ToString("0.00"));
            if (CheckoutDetails.PaymentMethod == "Phone" || CheckoutDetails.PaymentMethod == "BACS")
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
            foreach (BasketContents item in lbc)
            {
                if (item.ItemType == BasketItemType.Item || item.ItemType == BasketItemType.AdminDiscount || item.ItemType == BasketItemType.CompatibleDiscount)
                {
                    sb1.Append(BuildBasketRepeat(item));
                }
            }
            sb.Replace("[BasketRepeat]", sb1.ToString());

            return sb.ToString();
        }

        private string BuildBasketRepeat(BasketContents item)
        {
            string template = EmailData["OrderConfirmationRepeat"];
            StringBuilder sb = new StringBuilder(template);

            sb.Replace("[ItemPartNo]", item.PartNo);
            sb.Replace("[ItemDescription]", item.Description);
            sb.Replace("[ItemQuantity]", item.Quantity.ToString());
            sb.Replace("[ItemTotal]", Math.Round(item.PriceInc, 2).ToString("0.00"));
            if (item.Availability == 1 || item.Availability == 7)
            {
                sb.Replace("[ItemAvailability]", "<td width=\"60%\" colspan=\"4\"><p style=\"font-family: arial, helvetica, sans-serif; font-size: 12px; margin: 0; width: 100px; text-align: left; background-color: #1eb271; color: #ffffff; padding: 8px; text-align: center \">In stock</p></td>");
            }
            else
            {
                sb.Replace("[ItemAvailability]", "<td width=\"60%\" colspan=\"4\"><p style=\"font-family: arial, helvetica, sans-serif; font-size: 12px; margin: 0; width: 100px; text-align: left; background-color: #fc6365; color: #ffffff; padding: 8px; text-align: center \">Out of Stock</p></td>");
            }

            return sb.ToString();
        }

        private string BuildVoucherDetail(BasketContents item)
        {
            string template = EmailData["OrderConfirmationVoucher"];
            StringBuilder sb = new StringBuilder(template);

            sb.Replace("[VoucherPrice]", Math.Round(item.PriceInc, 2).ToString("0.00"));

            return sb.ToString();
        }
        public void GetAddOn()
        {
            using (Ngmd db = new Ngmd())
            {
                List<BasketContents> lbc = Utilities.LoadSession<List<BasketContents>>("B_BasketArray");
                var basketProductIds = lbc.Select(x => x.ProductId)
                                            .ToList();

                var addons = db.ProductAddons
                    .Where(x => basketProductIds.Contains(x.ProductId) && x.IsActive)
                    .OrderByDescending(x => x.CreatedDate).Take(3)
                    .ToList();
                ProductViewModel pv= new ProductViewModel();
                foreach (var basketItem in lbc)
                {
                    var addonIds = db.ProductAddons
                    .Where(x => x.ProductId == basketItem.ProductId && x.IsActive)
                    .OrderByDescending(x => x.CreatedDate).Take(3)
                    .Select(x => x.AddonProductId)
                    .ToList();

                    basketItem.AddonProducts = new List<BasketContents>();

                    foreach (var addonId in addonIds)
                    {
                        // Don't suggest something the customer already has in their basket.
                        if (basketProductIds.Contains(addonId))
                        {
                            continue;
                        }

                        ProductEntry product = pv.GetProductDetailById(addonId);

                        // Only show add-on products that are actually in stock (Availability
                        // 1 = in stock, 7 = in stock at an alternate warehouse) - an out-of-stock
                        // linked product shouldn't be offered in the "You May Also Need" region.
                        if (product != null)
                        {
                            basketItem.AddonProducts.Add(pv.CreateBasketContent(product));
                        }
                    }
                }
            }
        }
    }
}

