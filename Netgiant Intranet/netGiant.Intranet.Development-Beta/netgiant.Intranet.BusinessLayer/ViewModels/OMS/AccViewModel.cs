//using System;
//using System.Linq;
//using netGiant.Intranet.DataLayer.NetgiantMasterData;
//using System.Data.Entity;
//using System.Web.Mvc;
//using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
//using PagedList;
//using System.Linq.Expressions;
//using System.Collections.Generic;
//using netGiant.Intranet.BusinessLayer.Utilities;

//namespace netGiant.Intranet.BusinessLayer.ViewModels.OMS
//{
//    public class AccViewModel : CommonViewModel
//    {
//        private ngmdEntities _ctx;

//        public AccViewModel()
//        {
//            _ctx = new ngmdEntities();
//        }

//        public IQueryable<TelerikAccount> AccountList { get; set; }
//        public List<TelerikContact> ContactList { get; set; }
//        public List<TelerikAddress> AddressList { get; set; }
//        public IQueryable<SelectListItem> AccountTypeList { get; set; }
//        public IQueryable<SelectListItem> CreditStatusList { get; set; }
//        public IQueryable<SelectListItem> AccountStatusList { get; set; }
//        public IQueryable<SelectListItem> PaymentMethodList { get; set; }
//        public IQueryable<SelectListItem> WebsiteList { get; set; }
//        public IQueryable<SelectListItem> CustomerGroupList { get; set; }
//        public IQueryable<SelectListItem> ContactStatusList { get; set; }
//        public IQueryable<SelectListItem> AddressTypeList { get; set; }
//        public Account Account { get; set; }
//        public Contact Contact { get; set; }
//        public Address Address { get; set; }


//        public void GetAccounts(string SearchString)
//        {

//            AccountList = _ctx.Accounts
//            .Include(x => x.Addresses)
//            .Include(x => x.Contacts)
//            .Where(
//                x =>
//                    x.Addresses.Any(y => y.Postcode.Contains(SearchString))
//                    ||
//                    x.Addresses.Any(y => y.Line1.Contains(SearchString))
//                    || 
//                    x.Addresses.Any(y => y.Line2.Contains(SearchString))
//                    ||
//                    x.Contacts.Any(y => y.Email.Contains(SearchString))
//                    ||
//                    x.ShortName.Contains(SearchString)
//                    ||
//                    x.Name.Contains(SearchString)
//                    ||
//                    x.AxisNo.Contains(SearchString)
//            )
//            .Join(
//                _ctx.Lookup,
//                Account => Account.CustomerGroupFk,
//                Lookup => Lookup.LookupId,
//                (Account, Lookup) => new { Account, Lookup }
//            )
//            .Where(x => x.Lookup.LookupType.LookupTypeName == "CustomerGroup")
//            .Join(
//                _ctx.Lookup,
//                Account => Account.Account.AccountStatusFk,
//                Lookup => Lookup.LookupId,
//                (Account, Lookup) => new { Account, Lookup }
//            )
//            .Where(x => x.Lookup.LookupType.LookupTypeName == "AccountStatus")
//            .Join(
//                _ctx.Lookup,
//                Account => Account.Account.Account.PaymentMethodFk,
//                Lookup => Lookup.LookupId,
//                (Account, Lookup) => new { Account, Lookup }
//            )
//            .Where(x => x.Lookup.LookupType.LookupTypeName == "PaymentMethod")
//            .Select(x => new TelerikAccount
//            {
//                Id = x.Account.Account.Account.AccountId,
//                ShortName = x.Account.Account.Account.ShortName,
//                Name = x.Account.Account.Account.Name,
//                Postcode = x.Account.Account.Account.Addresses.FirstOrDefault().Postcode,
//                DateLastTransaction = x.Account.Account.Account.DateLastTransaction,
//                Status = x.Account.Lookup.LookupName,
//                CustomerGroup = x.Account.Account.Lookup.LookupName,
//                AxisNo = x.Account.Account.Account.AxisNo,
//                TownCity =
//                (
//                    x.Account.Account.Account.Addresses.FirstOrDefault().Line5 != ""
//                    ?
//                    x.Account.Account.Account.Addresses.FirstOrDefault().Line4
//                    :
//                    x.Account.Account.Account.Addresses.FirstOrDefault().Line3
//                ),
//                PaymentMethod = x.Lookup.LookupName
//            })
//            .AsQueryable();
//        }

//        public void GetAccount(int id)
//        {
//            AccountTypeList = SelectListViewModel.GetLookupTypeList("AccountType");
//            CreditStatusList = SelectListViewModel.GetLookupTypeList("CreditStatus");
//            AccountStatusList = SelectListViewModel.GetLookupTypeList("AccountStatus");
//            PaymentMethodList = SelectListViewModel.GetLookupTypeList("PaymentMethod");
//            WebsiteList = SelectListViewModel.GetAllWebsites();
//            CustomerGroupList = SelectListViewModel.GetLookupTypeList("CustomerGroup");

//            Account = _ctx.Accounts
//                .Include(x => x.CreditAccounts)
//                .Include(x => x.Addresses)
//                .Include(x => x.Contacts)
//                .Where(x => x.AccountId == id)
//                .FirstOrDefault();
//        }


//        public void GetContacts(Expression<Func<Contact, bool>> where)
//        {
//            ContactList = _ctx.Contacts
//                .Include(x => x.Addresses)
//                .Where(where)
//                .Select(x => new TelerikContact
//                {
//                    ContactId = x.ContactId,
//                    AccountFk = x.AccountFk,
//                    FirstName = x.FirstName,
//                    LastName = x.LastName,
//                    TelephoneNumber = x.TelephoneNumber,
//                    ExtensionNumber = x.ExtensionNumber,
//                    FaxNumber = x.FaxNumber,
//                    Email = x.Email,
//                    DateJoined = x.DateJoined,
//                    PrimaryContact = x.IsPrimaryContact ? "Yes" : "No"
//                })
//                .ToList();
//        }

//        public void GetContact(int id)
//        {
//            Contact = _ctx.Contacts
//                .Include(x => x.Addresses)
//                .Where(x => x.ContactId == id)
//                .FirstOrDefault();

//           Contact.ContactStatus = _ctx.Lookup
//                .Where(x => x.LookupType.LookupTypeName == "ContactStatus")
//                .Where(x => x.LookupId == Contact.ContactStatusFk)
//                .Select(x => x.LookupName)
//                .FirstOrDefault();

//            ContactStatusList = SelectListViewModel.GetNgmdLookupSelectList("ContactStatus");
//        }

//        public void GetAddresses(Expression<Func<Address, bool>> where)
//        {
//            AddressList = _ctx.Addresses
//                .Where(where)
//                .Join(
//                    _ctx.Lookup,
//                    Address => Address.AddressTypeFk,
//                    Lookup => Lookup.LookupId,
//                    (Address, Lookup) => new { Address, Lookup }
//                )
//                .Where(x => x.Lookup.LookupType.LookupTypeName == "AddressType")
//                .Select(x => new TelerikAddress
//                {
//                    AddressId = x.Address.AddressId,
//                    AccountId = x.Address.AccountFk,
//                    ContactId = x.Address.ContactFk,
//                    Line1 = x.Address.Line1,
//                    Line2 = x.Address.Line2,
//                    Line3 = x.Address.Line3,
//                    Line4 = x.Address.Line4,
//                    Line5 = x.Address.Line5,
//                    Postcode = x.Address.Postcode,
//                    AddressType = x.Lookup.LookupName
//                })
//                .ToList();
//        }

//        public void GetAddress(int id)
//        {
//            Address = _ctx.Addresses
//                .Where(x => x.AddressId == id)
//                .FirstOrDefault();

//            Address.AddressType = _ctx.Lookup
//                .Where(x => x.LookupType.LookupTypeName == "AddressType")
//                .Where(x => x.LookupId == Address.AddressTypeFk)
//                .Select(x => x.LookupName)
//                .FirstOrDefault();

//            AddressTypeList = SelectListViewModel.GetLookupTypeList("AddressType");
//        }

//        public void GetVariousLookups()
//        {
//            //We already have Account populated
//            List<Lookup> LookupList = new List<Lookup>();
//            LookupList = _ctx.Lookup
//                .Include(x => x.LookupType)
//                .ToList();

//            Account.AccountType = LookupList
//                .Where(x => x.LookupType.LookupTypeName == "AccountType")
//                .Where(x => x.LookupId == Account.AccountTypeFk)
//                .Select(x => x.LookupName)
//                .FirstOrDefault();
//            Account.AccountStatus = LookupList
//                .Where(x => x.LookupType.LookupTypeName == "AccountStatus")
//                .Where(x => x.LookupId == Account.AccountStatusFk)
//                .Select(x => x.LookupName)
//                .FirstOrDefault();
//            Account.PaymentMethod = LookupList
//                .Where(x => x.LookupType.LookupTypeName == "PaymentMethod")
//                .Where(x => x.LookupId == Account.PaymentMethodFk)
//                .Select(x => x.LookupName)
//                .FirstOrDefault();
//            Account.WebsiteName = _ctx.Website
//                .Where(x => x.WebsiteID == Account.WebsiteFk)
//                .Select(x => x.WebsiteName)
//                .FirstOrDefault();
//            Account.CustomerGroup = LookupList
//                .Where(x => x.LookupType.LookupTypeName == "CustomerGroup")
//                .Where(x => x.LookupId == Account.CustomerGroupFk)
//                .Select(x => x.LookupName)
//                .FirstOrDefault();
//            Account.CreditStatus = LookupList
//                .Where(x => x.LookupType.LookupTypeName == "CreditStatus")
//                .Where(x => x.LookupId == Account.CreditStatusFk)
//                .Select(x => x.LookupName)
//                .FirstOrDefault();
//            Account.OrderSource = LookupList
//                .Where(x => x.LookupType.LookupTypeName == "OrderSource")
//                .Where(x => x.LookupId == Account.OrderSourceFk)
//                .Select(x => x.LookupName)
//                .FirstOrDefault();
//        }

//        public void SaveAccount(AccViewModel model)
//        {
//            using (ngmdEntities db = new ngmdEntities())
//            {
//                Account EntryInDB = db.Accounts.FirstOrDefault(x => x.AccountId == model.Account.AccountId);
//                EntryInDB.ShortName = model.Account.ShortName;
//                EntryInDB.Name = model.Account.Name;
//                //EntryInDB.VatNumber = EntityAccess.ReturnEmptyString(model.Account.CreditAccounts.FirstOrDefault().VatNumber);
//                EntryInDB.AccountTypeFk = model.Account.AccountTypeFk;
//                EntryInDB.CreditStatusFk = model.Account.CreditStatusFk;
//                EntryInDB.AccountStatusFk = model.Account.AccountStatusFk;
//                EntryInDB.PaymentMethodFk = model.Account.PaymentMethodFk;
//                EntryInDB.WebsiteFk = model.Account.WebsiteFk;
//                EntryInDB.CustomerGroupFk = model.Account.CustomerGroupFk;
//                //EntryInDB.SageNominalAccount = model.Account.CreditAccounts.FirstOrDefault().SageNominalAccount;
//                //EntryInDB.CreditLimit = model.Account.CreditAccounts.FirstOrDefault().CreditLimit;
//                EntryInDB.CustomerNotes = model.Account.CustomerNotes;

//                db.SaveChanges();
//            }
//        }

//        public void SaveContact(AccViewModel model)
//        {
//            using (ngmdEntities db = new ngmdEntities())
//            {
//                Contact ContactInDB = db.Contacts.FirstOrDefault(x => x.ContactId == model.Contact.ContactId);
//                ContactInDB.Title = EntityAccess.ReturnEmptyString(model.Contact.Title);
//                ContactInDB.FirstName = model.Contact.FirstName;
//                ContactInDB.LastName = model.Contact.LastName;
//                ContactInDB.Email = EntityAccess.ReturnEmptyString(model.Contact.Email);
//                ContactInDB.TelephoneNumber = EntityAccess.ReturnEmptyString(model.Contact.TelephoneNumber);
//                ContactInDB.FaxNumber = EntityAccess.ReturnEmptyString(model.Contact.FaxNumber);
//                ContactInDB.ContactStatusFk = Convert.ToInt32(model.Contact.ContactStatusFk);

//                db.SaveChanges();
//            }
//        }

//        public void SaveAddress(AccViewModel model)
//        {
//            using(ngmdEntities db = new ngmdEntities())
//            {
//                Address AddressInDB = db.Addresses.FirstOrDefault(x => x.AddressId == model.Address.AddressId);
//                AddressInDB.AddressTypeFk = model.Address.AddressTypeFk;
//                AddressInDB.Line1 = EntityAccess.ReturnEmptyString(model.Address.Line1);
//                AddressInDB.Line2 = EntityAccess.ReturnEmptyString(model.Address.Line2);
//                AddressInDB.Line3 = EntityAccess.ReturnEmptyString(model.Address.Line3);
//                AddressInDB.Line4 = EntityAccess.ReturnEmptyString(model.Address.Line4);
//                AddressInDB.Line5 = EntityAccess.ReturnEmptyString(model.Address.Line5);
//                AddressInDB.Postcode = EntityAccess.ReturnEmptyString(model.Address.Postcode);

//                db.SaveChanges();
//            }
//        }

        

//        public class TelerikAccount
//        {
//            public int Id { get; set; }
//            public string ShortName { get; set; }
//            public string Name { get; set; }
//            public decimal CreditLimit { get; set; }
//            public DateTime? DateLastTransaction { get; set; }
//            public string Postcode { get; set; }
//            public string PostcodeMod { get; set; }
//            public string Status { get; set; }
//            public string CustomerGroup { get; set; }
//            public string AxisNo { get; set; }
//            public string TownCity { get; set; }
//            public string PaymentMethod { get; set; }
//        }

//        public class TelerikContact
//        {
//            public int ContactId { get; set; }
//            public int AccountFk { get; set; }
//            public int ContactStatusFk { get; set; }
//            public string Title { get; set; }
//            public string FirstName { get; set; }
//            public string LastName { get; set; }
//            public string TelephoneNumber { get; set; }
//            public string ExtensionNumber { get; set; }
//            public string FaxNumber { get; set; }
//            public string Email { get; set; }
//            public DateTime? DateJoined { get; set; }
//            public string PrimaryContact { get; set; }
//        }

//        public class TelerikAddress
//        {
//            public int AddressId { get; set; }
//            public int AccountId { get; set; }
//            public int? ContactId { get; set; }
//            public string AddressType { get; set; }
//            public string Line1 { get; set; }
//            public string Line2 { get; set; }
//            public string Line3 { get; set; }
//            public string Line4 { get; set; }
//            public string Line5 { get; set; }
//            public string Postcode { get; set; }
//        }
//    }
//}
