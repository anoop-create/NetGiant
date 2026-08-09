using Kendo.Mvc.Extensions;
using netGiant.Intranet.BusinessLayer.Utilities;
using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Import;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using netGiant.Intranet.DataLayer.CustomerData;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Mail;
using System.Web.Mvc;
using CusAccount = netGiant.Intranet.DataLayer.CustomerData.Account;
using EntityState = System.Data.Entity.EntityState;

namespace netGiant.Intranet.BusinessLayer.ViewModels.CrMS
{
    public class CreditAccountViewModel : CommonViewModel
    {
        public CreditAccountViewModel()
        {
            _ctx = new customerEntities();
        }

        public IQueryable<Telerik> AccountList { get; set; }
        public Customer CustomerEntry { get; set; }
        public CusAccount AccountEntry { get; set; }
        public Billing BillingEntry { get; set; }
        private customerEntities _ctx;

        public List<SelectListItem> CustomerTypeList { get; set; }
        public List<SelectListItem> AccountStatusList { get; set; }
        public List<SelectListItem> OrganisationTypeList { get; set; }
        public List<SelectListItem> SectorList { get; set; }
        public List<SelectListItem> StaffCountList { get; set; }
        public IQueryable<SelectListItem> WebsiteNameList { get; set; }

        public string CustomerType { get; set; }
        public string AccountStatus { get; set; }
        public string TradeAccountStatus { get; set; }
        public string OrganisationType { get; set; }
        public string Sector { get; set; }
        public string StaffCount { get; set; }
        public string OrderCount { get; set; }
        public string WebsiteName { get; set; }
        public string ReturnToPage { get; set; }

        public CreditAccountViewModel GetAccounts()
        {
            var list = _ctx.Customers
                .Where(o => o.Account.FirstOrDefault() != null && o.Account.FirstOrDefault().IsAccountCustomer == true)
                .Select(o => new Telerik
                {
                    CustomerId = o.CustomerId,
                    Email = o.OriginalEmailAddress,
                    AccountId = o.Account.FirstOrDefault().AccountId,
                    AccountNumber = o.AccountNumber,
                    Website = _ctx.WebsiteView.FirstOrDefault(w => w.WebsiteID == o.WebsiteFk).FriendlyName,
                    WebsiteId = o.WebsiteFk,
                    Status = o.Account.FirstOrDefault().Lookup3.LookupName,
                    TradingName = o.Account.FirstOrDefault().TradingName,
                    ContactName = o.Account.FirstOrDefault().ContactName,
                    ContactEmailAddress = o.Account.FirstOrDefault().ContactEmailAddress,
                    ContactTelephoneNo = o.Account.FirstOrDefault().ContactTelephoneNo,
                    EstMonthlySpend = (decimal)o.Account.FirstOrDefault().EstMonthlySpend,
                    CreditLimit = o.Account.FirstOrDefault().CreditLimit,
                    DateOfApplication = o.Account.FirstOrDefault().DateOfApplication,
                    FirstOrderRef = o.Account.FirstOrDefault().FirstOrderRef,
                    FirstOrderAmt = (decimal)o.Account.FirstOrDefault().FirstOrderAmt,
                    IsAccountCustomer = o.Account.FirstOrDefault().IsAccountCustomer,
                    IsTradeCustomer = o.Account.FirstOrDefault().IsTradeCustomer,
                })
                .ToList();

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].AccountNumber == "@")
                {
                    list[i].AccountNumber = GetAccountNumber(list[i].WebsiteId, list[i].Email);
                }
            }

            AccountList = list.AsQueryable();

            return this;
        }

        public CreditAccountViewModel GetTradeAccounts()
        {
            var list = _ctx.Customers
                .Where(o => o.Account.FirstOrDefault() != null && o.Account.FirstOrDefault().IsTradeCustomer)
                .Select(o => new Telerik
                {
                    CustomerId = o.CustomerId,
                    Email = o.OriginalEmailAddress,
                    AccountId = o.Account.FirstOrDefault().AccountId,
                    AccountNumber = o.AccountNumber,
                    Website = _ctx.WebsiteView.FirstOrDefault(w => w.WebsiteID == o.WebsiteFk).FriendlyName,
                    WebsiteId = o.WebsiteFk,
                    Status = o.Account.FirstOrDefault().Lookup5.LookupName,
                    TradingName = o.Account.FirstOrDefault().TradingName,
                    ContactName = o.Account.FirstOrDefault().ContactName,
                    ContactEmailAddress = o.Account.FirstOrDefault().ContactEmailAddress,
                    ContactTelephoneNo = o.Account.FirstOrDefault().ContactTelephoneNo,
                    EstMonthlySpend = (decimal)o.Account.FirstOrDefault().EstMonthlySpend,
                    CreditLimit = o.Account.FirstOrDefault().CreditLimit,
                    DateOfApplication = o.Account.FirstOrDefault().DateOfApplication,
                    FirstOrderRef = o.Account.FirstOrDefault().FirstOrderRef,
                    FirstOrderAmt = (decimal)o.Account.FirstOrDefault().FirstOrderAmt,
                    NumberOffices = (int)o.Account.FirstOrDefault().NumberOffices,
                    NumberPrinters = (int)o.Account.FirstOrDefault().NumberPrinters,
                    IsAccountCustomer = o.Account.FirstOrDefault().IsAccountCustomer,
                    IsTradeCustomer = o.Account.FirstOrDefault().IsTradeCustomer,
                })
                .ToList();

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].AccountNumber == "@")
                {
                    list[i].AccountNumber = GetAccountNumber(list[i].WebsiteId, list[i].Email);
                }
            }

            AccountList = list.AsQueryable();

            return this;
        }

        public CreditAccountViewModel CreateAccount(int id)
        {
            if (id > 0)
            {
                GetAccountDetail(id);
            }
            else
            {
                AccountEntry = new CusAccount();
            }
            AccountEntry.DateLastUpdated = DateTime.Now;
            SetupSelectLists();

            return this;
        }

        public CreditAccountViewModel GetAccountDetail(int id)
        {
            using (customerEntities db = new customerEntities())
            {
                AccountEntry = db.Account
                    .Include("Customer").FirstOrDefault(x => x.AccountId == id);

                if (AccountEntry != null)
                {
                    BillingEntry = db.Billing
                        .FirstOrDefault(x => x.CustomerFk == AccountEntry.CustomerFk);
                    CustomerEntry = db.Customers
                        .FirstOrDefault(x => x.CustomerId == AccountEntry.CustomerFk);
                }
            }
            if (AccountEntry != null)
            {
                CustomerType = GetLookupValue(AccountEntry.Customer.CustomerTypeId);
                AccountStatus = GetLookupValue(AccountEntry.StatusId);
                TradeAccountStatus = GetLookupValue(AccountEntry.StatusId);
                OrganisationType = GetLookupValue(AccountEntry.OrganisationTypeId);
                Sector = GetLookupValue(AccountEntry.SectorId);
                StaffCount = GetLookupValue(AccountEntry.TotalStaffCountId);
                OrderCount = GetLookupValue(AccountEntry.OrderStaffCountId);
                WebsiteName = GetWebsiteName(AccountEntry.Customer.WebsiteFk);
            }
            if (CustomerEntry != null)
            {
                if (CustomerEntry.AccountNumber == "@")
                {
                    // Attempt to retrieve allocated Account Number
                    CustomerEntry.AccountNumber = GetAccountNumber(CustomerEntry.WebsiteFk, CustomerEntry.OriginalEmailAddress);
                }
            }

            return this;
        }

        private string GetAccountNumber(int websiteId, string email)
        {
            List<SqlParameter> sqlParms = new List<SqlParameter>();
            SqlParameter sqlParm = new SqlParameter("@WebsiteId", SqlDbType.Int);
            sqlParm.Value = websiteId;
            sqlParms.Add(sqlParm);
            sqlParm = new SqlParameter("@Email", SqlDbType.VarChar);
            sqlParm.Value = email;
            sqlParms.Add(sqlParm);
            DataTable accDetails = SQLUtilities
                .ExecuteReadStoredProcedure("customersqldata", "dbo.GetAccountNumber", sqlParms, "acdata")
                .Tables[0];

            return accDetails.Rows.Count > 0 ? Convert.ToString(accDetails.Rows[0]["AccountNumber"]) : "@";
        }

        public bool SaveAccountEntry()
        {
            bool success = true;
            string status = "";
            string tradeStatus = "";
            try
            {
                Customer oldCus = CheckCustomerExists();
                CusAccount oldAc = CheckAccountExists();
                Billing oldBill = CheckBillingExists();
                using (customerEntities db = new customerEntities())
                {
                    status = db.Lookup.FirstOrDefault(x => x.LookupID == AccountEntry.StatusId)?.LookupName;
                    tradeStatus = db.Lookup.FirstOrDefault(x => x.LookupID == AccountEntry.TradeStatusId)?.LookupName;
                    if (AccountEntry.AccountId > 0)
                    {
                        db.Entry(AccountEntry).State = EntityState.Modified;
                    }
                    else
                    {
                        //Check if an account already exists
                        if (oldAc != null)
                        {
                            throw new Exception("Account already exists.");
                        }
                        db.Entry(AccountEntry).State = EntityState.Added;
                    }

                    if (BillingEntry.BillingId > 0)
                    {
                        db.Entry(BillingEntry).State = EntityState.Modified;
                    }
                    else
                    {
                        //Check if an billing already exists
                        if (oldBill != null)
                        {
                            throw new Exception("Billing details already exists.");
                        }
                        db.Entry(BillingEntry).State = EntityState.Added;
                    }

                    if (CustomerEntry.CustomerId > 0)
                    {
                        db.Entry(CustomerEntry).State = EntityState.Modified;
                    }
                    else
                    {
                        //Check if customer already exists
                        if (oldCus != null)
                        {
                            throw new Exception("Customer already exists.");
                        }
                        db.Entry(CustomerEntry).State = EntityState.Added;
                    }

                    db.SaveChanges();
                }

                if (AccountEntry.AccountId > 0)
                {
                    string emailSubject = "";
                    string emailCMSBody = "";
                    if (oldAc.StatusId != AccountEntry.StatusId && status == "Approved")
                    {
                        emailSubject = "Credit Account Approved For TonerGiant.co.uk";
                        emailCMSBody = "CreditAccountAcceptance";
                    }
                    if (oldAc.TradeStatusId != AccountEntry.TradeStatusId && tradeStatus == "Approved")
                    {
                        emailSubject = "Trade Account Approved For TonerGiant.co.uk";
                        emailCMSBody = "TradeAccountAcceptance";
                    }

                    if (emailSubject != "")
                    {
                        // Generate email for customer
                        string supportEmail = SharedFunctions.GetConfigurationSetting(
                            "Website Application Variables",
                            "supportEmailAddress",
                            oldAc.Customer.WebsiteFk
                        );
                        string htmlEmail = GetAcceptedEmailBody(emailCMSBody);
                        EmailUtilities.SendEmail(
                            emailSubject,
                            htmlEmail,
                            true,
                            MailPriority.Normal,
                            new List<string> { AccountEntry.ContactEmailAddress },
                            supportEmail
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                success = false;
                SetupSelectLists();
                throw new ApplicationException(ex.Message + ex.StackTrace);
            }

            return success;
        }

        public SaveReturn DeleteAccount(int id)
        {
            SaveReturn sr = new SaveReturn();

            try
            {
                if (id > 0)
                {
                    using (customerEntities db = new customerEntities())
                    {
                        CusAccount a = db.Account.FirstOrDefault(x => x.AccountId == id);
                        db.Entry(a).State = EntityState.Deleted;
                        db.SaveChanges();
                        sr.IsSuccess = true;
                    }
                }
            }
            catch (Exception ex)
            {
                sr.IsSuccess = false;
                sr.Message = ex.Message;
            }

            return sr;
        }

        public void SetupSelectLists()
        {
            WebsiteNameList = GetWebsiteNames();
            CustomerTypeList = GetLookupList("Customer Type");
            AccountStatusList = GetLookupList("Account Status");
            OrganisationTypeList = GetLookupList("Organisation Type");
            SectorList = GetLookupList("Sector");
            StaffCountList = GetLookupList("Staff Count");
        }

        private IQueryable<SelectListItem> GetWebsiteNames()
        {
            IQueryable<SelectListItem> query;

            using (customerEntities db = new customerEntities())
            {
                query = db.WebsiteView.OrderBy(x => x.WebsiteName).Select(x => new SelectListItem
                {
                    Value = x.WebsiteID.ToString(),
                    Text = x.FriendlyName.ToString()
                }).ToList().AsQueryable();
            }
            return query;
        }

        private string GetWebsiteName(int websiteId)
        {
            string name = "";
            using (customerEntities db = new customerEntities())
            {
                name = db.WebsiteView.FirstOrDefault(x => x.WebsiteID == websiteId)?.WebsiteName;
            }
            return name;
        }

        public List<SelectListItem> GetLookupList(string lookupTypeName)
        {
            List<SelectListItem> oList;

            using (customerEntities db = new customerEntities())
            {
                oList = db.Lookup
                    .Where(x => x.LookupType.LookupTypeName == lookupTypeName)
                    .OrderBy(x => x.Sequence)
                    .Select(x => new SelectListItem
                    {
                        Value = x.LookupID.ToString(),
                        Text = x.LookupName.ToString()
                    }).ToList();
            }
            return oList;
        }

        public string GetLookupValue(int id)
        {
            using (customerEntities db = new customerEntities())
            {
                return db.Lookup
                    .FirstOrDefault(x => x.LookupID == id)
                    ?.LookupName;
            }
        }

        public Customer CheckCustomerExists()
        {
            Customer cus = new Customer();
            using (customerEntities db = new customerEntities())
            {
                cus = db.Customers
                    .FirstOrDefault(x => x.CustomerId == CustomerEntry.CustomerId);
            }

            return cus;
        }

        public CusAccount CheckAccountExists()
        {
            CusAccount ac = new CusAccount();
            using (customerEntities db = new customerEntities())
            {
                ac = db.Account
                    .Include(x => x.Customer)
                    .FirstOrDefault(x => x.CustomerFk == AccountEntry.CustomerFk);
            }

            return ac;
        }

        public Billing CheckBillingExists()
        {
            Billing bill = new Billing();
            using (customerEntities db = new customerEntities())
            {
                bill = db.Billing
                    .FirstOrDefault(x => x.CustomerFk == BillingEntry.CustomerFk);
            }

            return bill;
        }

        private string GetAcceptedEmailBody(string emailCMSBody)
        {
            string body = SharedFunctions.GetCmsEntry("IntranetEmailData", emailCMSBody) ?? "";

            //using (ngmdEntities db = new ngmdEntities())
            //{
            //    body = db.cmsEntry
            //        .FirstOrDefault(w => w.cmsSection.sectionName == "IntranetEmailData" && w.entryName == emailCMSBody && w.cmsSection.websiteFK == CustomerEntry.WebsiteFk)
            //        ?.cmsContent;
            //}

            var replacements = new Dictionary<string, string>();
            replacements.Add("[creditlimit]", String.Format("{0:#,###,##0.00}", AccountEntry.CreditLimit));
            replacements.Add("[accountnumber]", CustomerEntry.AccountNumber == "@" ? GetAccountNumber(CustomerEntry.WebsiteFk, CustomerEntry.OriginalEmailAddress) : CustomerEntry.AccountNumber);
            replacements.Add("[name]", AccountEntry.ContactName);

            return SharedFunctions.DoReplacements(body, replacements);
        }















        private string GetRejectedEmailBody()
        {
            string body;

            using (ngmdEntities db = new ngmdEntities())
            {
                body = db.cmsEntry
                    .FirstOrDefault(w => w.cmsSection.sectionName == "IntranetEmailData" && w.entryName == "CreditAccountRejected" && w.cmsSection.websiteFK == CustomerEntry.WebsiteFk)
                    ?.cmsContent;
            }

            var replacements = new Dictionary<string, string>();
            replacements.Add("[creditlimit]", String.Format("{0:#,###,##0.00}", AccountEntry.CreditLimit));
            replacements.Add("[accountnumber]", CustomerEntry.AccountNumber == "@" ? GetAccountNumber(CustomerEntry.WebsiteFk, CustomerEntry.OriginalEmailAddress) : CustomerEntry.AccountNumber);
            replacements.Add("[name]", AccountEntry.ContactName);

            return SharedFunctions.DoReplacements(body, replacements);
        }










        public class Telerik
        {
            public int CustomerId { get; set; }
            public string Email { get; set; }
            public int AccountId { get; set; }
            public string AccountNumber { get; set; }
            public string Website { get; set; }
            public int WebsiteId { get; set; }
            public string Status { get; set; }
            public string TradingName { get; set; }
            public string ContactName { get; set; }
            public string ContactEmailAddress { get; set; }
            public string ContactTelephoneNo { get; set; }
            public decimal? EstMonthlySpend { get; set; }
            public decimal? CreditLimit { get; set; }
            public DateTime? DateOfApplication { get; set; }
            public string FirstOrderRef { get; set; }
            public decimal? FirstOrderAmt { get; set; }
            public int NumberOffices { get; set; }
            public int NumberPrinters { get; set; }
            public bool IsTradeCustomer { get; set; }
            public bool IsAccountCustomer { get; set; }
        }
    }
}
