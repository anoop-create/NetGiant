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
using Braintree;
using Newtonsoft.Json;
using VMerchantWrapper.Entities;
using VmVoucherType = VMerchantWrapper.Entities.VoucherType;

namespace BusinessLogic.ViewModels
{
    public class CheckoutViewModel : CommonViewModel
    {
        public CheckoutViewModel()
        {
            CheckoutData = DataCache.GetSectionData("CheckoutData");
            BasketTotals = new BasketTotals();
            if (HttpContext.Current.Session["B_BasketTotals"] != null)
            {
                BasketTotals = (BasketTotals)HttpContext.Current.Session["B_BasketTotals"];
            }
            CheckoutDetails = new CheckoutDetails();
            CardList = new List<SagePayToken>();
            EmailData = DataCache.GetSectionData("EmailData");
        }

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
                        add.Id = i;
                        AdditionalAddresses.Add(add);
                        i += 1;

                        CheckoutDetails.DeliveryAddress = CheckoutDetails.BillingAddress;
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
                        else
                        {
                            CheckoutDetails.DeliveryAddress = CheckoutDetails.BillingAddress;
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
            CheckoutDetails cd = new CheckoutDetails();
            if (HttpContext.Current.Session["C_CheckoutDetails"] != null)
            {
                cd = (CheckoutDetails)HttpContext.Current.Session["C_CheckoutDetails"];
            }

            cd.ZeroStock = CheckoutDetails.ZeroStock;
            cd.Email = CheckoutDetails.Email;
           
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
        }

        public void ProcessStage1()
        {
            CheckoutDetails cd = new CheckoutDetails();
            if (HttpContext.Current.Session["C_CheckoutDetails"] != null)
            {
                cd = (CheckoutDetails)HttpContext.Current.Session["C_CheckoutDetails"];
            }
            if (Authentication.IsAuthenticated())
            {
                if (Authentication.FixTempRecord(HttpContext.Current.Session["U_Record"].ToString(), HttpContext.Current.Session["U_Email"].ToString()))
                {
                    cd.AccountNumber = HttpContext.Current.Session["U_AccountNo"].ToString();
                    cd.AccountRecord = HttpContext.Current.Session["U_Record"].ToString();
                }
            }

            SetStage1Fields(cd);

            // Write Axis Draft Order
            var createDraftOrder = Touchpoints.SaveOrder(cd, OrderStatus.Draft);
            if (!createDraftOrder.IsSuccess)
            {
                throw new ApplicationException("There was a problem creating your order. Please call us on " + Utilities.GetItemFromDict(CommonData, "TelephoneNumber") + " for assistance.");
            }

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
                    case "MCDEBIT":
                    case "MC":
                    {
                        spg.CardName = "Master Card";
                        break;
                    }
                    case "VISA":
                    case "DELTA":
                    case "UKE":
                    {
                        spg.CardName = "Visa / Delta";
                        break;
                    }
                    case "AMEX":
                    {
                        spg.CardName = "American Express";
                        break;
                    }
                    case "MAESTRO":
                    {
                        spg.CardName = "Maestro";
                        break;
                    }
                    default:
                    {
                        spg.CardName = "";
                        break;
                    }
                }
            }
        }

        public void ProcessStage2()
        {
            CheckoutDetails cd = new CheckoutDetails();
            if (HttpContext.Current.Session["C_CheckoutDetails"] != null)
            {
                cd = (CheckoutDetails)HttpContext.Current.Session["C_CheckoutDetails"];
            }
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
                SetStage1Fields(cd);

                if (cd.PaymentMethod == PaymentMethod.AccountApplication.ToString())
                {
                    cd.BillingAddress = AccountApplicationDetails.BillingAddress;
                }

                if (string.IsNullOrEmpty(cd.Reference))
                {
                    // Write Axis Draft Order
                    var createDraftOrder = Touchpoints.SaveOrder(cd, OrderStatus.Draft);
                    if (!createDraftOrder.IsSuccess)
                    {
                        throw new ApplicationException("There was a problem creating your order. Please call us on " + Utilities.GetItemFromDict(CommonData, "TelephoneNumber") + " for assistance.");
                    }
                }
            }

            if (cd.PaymentMethod == "CreditDebit")
            {
                cd.CardLast4Digits = CheckoutDetails.CardLast4Digits;
                cd.SagePayAuthCode = CheckoutDetails.SagePayAuthCode;
                cd.SageToken = CheckoutDetails.SageToken;
                cd.CardType = CheckoutDetails.CardType;

                //Utilities.LogInformationMessage("Order has been placed. Last 4 Digits = " + cd.CardLast4Digits);
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
            if (!createCompleteOrder.IsSuccess)
            {
                throw new ApplicationException("There was a problem creating your order. Please call us on " + Utilities.GetItemFromDict(CommonData, "TelephoneNumber") + " for assistance.");
            }

            HttpContext.Current.Session["C_CheckoutDetails"] = cd;
            CheckoutDetails = cd;
        }

        private void SetStage1Fields(CheckoutDetails cd)
        {
            CheckUpdateUser(cd);

            cd.Name = CheckoutDetails.Name;
            cd.TelephoneNumber = CheckoutDetails.TelephoneNumber;
            cd.RecipientName = CheckoutDetails.RecipientName;
            cd.DeliveryAddress = CheckoutDetails.DeliveryAddress;
            cd.PaymentMethod = CheckoutDetails.PaymentMethod;
            cd.Newsletter = CheckoutDetails.Newsletter;
            cd.SuppressEmail = CheckoutDetails.SuppressEmail;
            cd.BillingAddress = CheckoutDetails.BillingAddress;
            cd.Reference = CheckoutDetails.Reference;
            cd.Password = Authentication.IsAuthenticated() ? cd.Password : CheckoutDetails.Password;
            cd.TotalIncVat = ((BasketTotals)HttpContext.Current.Session["B_BasketTotals"]).GrandTotalIncVat;
            cd.OrderNote = CheckoutDetails.OrderNote ?? "";

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
                if (createUser.IsSuccess)
                {
                    Authentication.Authenticate(signUp.UserName, signUp.Password);
                    HttpContext.Current.Session["U_FullyAuthenticated"] = true;
                }
            }
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

            //DataTable dt = Touchpoints.GetDeliveryData(countrycode, postcode);

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
            //if (add6 != "")
            //{
                add.Line1 = add1;
                add.Line2 = add2;
                add.Line3 = add3;
                add.Line4 = add4;
                add.Line5 = add5;
                add.PostCode = add6;
            //}
            //else
            //{
            //    if (add5 != "")
            //    {
            //        add.Line1 = "";
            //        add.Line2 = add1;
            //        add.Line3 = add2;
            //        add.Line4 = add3;
            //        add.Line5 = add4;
            //        add.PostCode = add5;
            //    }
            //    else
            //    {
            //        if (add4 != "")
            //        {
            //            add.Line1 = "";
            //            add.Line2 = add1;
            //            add.Line3 = "";
            //            add.Line4 = add2;
            //            add.Line5 = add3;
            //            add.PostCode = add4;
            //        }
            //        else
            //        {
            //            if (add3 != "")
            //            {
            //                add.Line1 = "";
            //                add.Line2 = add1;
            //                add.Line3 = "";
            //                add.Line4 = add2;
            //                add.Line5 = "";
            //                add.PostCode = add3;
            //            }
            //        }
            //    }
            //}

            return add;
        }

        // Static Functions
        public static List<deliveryService> RetrieveDeliveryOptions(string postcode)
        {
            postcode = postcode.ToUpper();
            deliveryZone dz = GetPostcodeDeliveryZone(postcode);
            HttpContext.Current.Session["D_IsVatExempt"] = !dz.ApplyVat;
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
        
        public static deliveryZone GetPostcodeDeliveryZone(string postcode)
        {
            postcode = postcode.Replace(" ", "").ToUpper();
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

            return dz;
        }

        public static void AddVoucherToBasket()
        {
            if (HttpContext.Current.Session["B_VoucherCode"] != null)
            {
                List<BasketContents> lbc = new List<BasketContents>();
                if (HttpContext.Current.Session["B_BasketArray"] != null)
                {
                    lbc = (List<BasketContents>) HttpContext.Current.Session["B_BasketArray"];
                }
                BasketTotals bt = new BasketTotals();
                if (HttpContext.Current.Session["B_BasketTotals"] != null)
                {
                    bt = (BasketTotals)HttpContext.Current.Session["B_BasketTotals"];
                }
                VoucherPromo v = new VoucherPromo();
                if (HttpContext.Current.Session["V_Voucher"] != null)
                {
                    v = (VoucherPromo) HttpContext.Current.Session["V_Voucher"];
                }

                var i = lbc.FindIndex(x => x.IsVoucher);
                if (i >= 0)
                {
                    // voucher exists, delete the item
                    lbc.Remove(lbc[i]);
                }

                if (v.VoucherTypeFk != (int)VmVoucherType.FreeGift)
                {
                    BasketContents bc = new BasketContents();
                    bc.IsVoucher = true;
                    bc.VoucherType = (int)v.VoucherTypeFk;
                    bc.StockRef = v.StockRef;
                    bc.Description = v.Description;
                    bc.Quantity = 1;
                    bc.PriceInc = bt.Voucher;
                    bc.PriceEx = bt.Voucher + bt.VoucherVat;
                    if (v.VoucherTypeFk == (int)VmVoucherType.Amount)
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

                    lbc.Add(bc);
                }

                HttpContext.Current.Session["B_BasketArray"] = lbc;
            }
        }
        
        public static string GeneratePayPalToken()
        {
            BraintreeGateway gateway = new BraintreeGateway("access_token$sandbox$5x2sbgbsf9w65wd8$ff856a75590de309ec9019f4104dbeea");

            return gateway.ClientToken.generate();
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
    }
}

