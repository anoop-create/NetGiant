//using System;
//using System.Linq;
//using netGiant.Intranet.DataLayer.NetgiantMasterData;
//using System.Data.Entity;
//using System.Web.Mvc;
//using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
//using PagedList;
//using System.Linq.Expressions;
//using System.Collections.Generic;

//namespace netGiant.Intranet.BusinessLayer.ViewModels.OMS
//{
//    public class LedgerViewModel : CommonViewModel
//    {

//        private ngmdEntities _ctx;

//        public LedgerViewModel()
//        {
//            _ctx = new ngmdEntities();
//        }

//        public IQueryable<TelerikLedger> LedgerList { get; set; }
//        public List<TelerikLedgerLine> LedgerLineList { get; set; }
//        public IQueryable<SelectListItem> LedgerTypeList { get; set; }
//        public int LedgerId { get; set; }
//        public int LedgerLineCount { get; set; }
//        public Ledger Ledger { get; set; }
//        public LedgerLine LedgerLine { get; set; }
//        public Lookup Lookup { get; set; }

//        public void GetLedgers(string SearchString)
//        {
//            LedgerList = _ctx.Ledgers
//                .Include(x => x.provider)
//                .Include(x => x.Order.Contact.Account)
//                .Include(x => x.OrderSource)
//                .Where(
//                    x =>
//                        x.LedgerId.ToString().Contains(SearchString)
//                        ||
//                        x.Order.Contact.Account.ShortName.Contains(SearchString)
//                        || 
//                        x.provider.providerName.Contains(SearchString)
//                )
//                .Join(
//                    _ctx.Lookup,
//                    Ledger => Ledger.LedgerTypeFk,
//                    Lookup => Lookup.LookupId,
//                    (Ledger, Lookup) => new { Ledger, Lookup }
//                )
//                .Where(x => x.Lookup.LookupType.LookupTypeName == "LedgerType")
//                .Join(
//                    _ctx.Lookup,
//                    Ledger => Ledger.Ledger.LedgerTransTypeFk,
//                    Lookup => Lookup.LookupId,
//                    (Ledger, Lookup) => new { Ledger, Lookup }
//                )
//                .Where(x => x.Lookup.LookupType.LookupTypeName == "LedgerTransType")
//                .Select(x => new TelerikLedger
//                {
//                    Id = x.Ledger.Ledger.LedgerId,
//                    OrderId = x.Ledger.Ledger.OrderFk,
//                    AccountId = x.Ledger.Ledger.Order.Contact.Account.AccountId,
//                    AccountShortName = x.Ledger.Ledger.Order.Contact.Account.ShortName,
//                    ProviderName = x.Ledger.Ledger.provider.providerName,
//                    PurchaseOrderId = x.Ledger.Ledger.PurchaseOrderFk,
//                    Year = x.Ledger.Ledger.Year,
//                    Period =  x.Ledger.Ledger.Period,
//                    Type = x.Ledger.Lookup.LookupName,
//                    TransactionType = x.Lookup.LookupName,
//                    Description1 = x.Ledger.Ledger.Description,
//                    Description2 = x.Ledger.Ledger.Description2,
//                    DateInvoiced = x.Ledger.Ledger.DateInvoiced,
//                    Nett = x.Ledger.Ledger.Nett,
//                    Vat = x.Ledger.Ledger.Vat,
//                    Cost = x.Ledger.Ledger.Cost
//                })
//                .AsQueryable();
//        }

//        public void GetLedger(int id)
//        {
//            LedgerTypeList = SelectListViewModel.GetLookupTypeList("LedgerType");

//            Ledger = _ctx.Ledgers
//                .Include(x => x.provider)
//                .Include(x => x.Order.Contact.Account)
//                .Where(x => x.LedgerId == id)
//                .FirstOrDefault();  
//        }

//        public void GetLedgerLines(int id)
//        {
//            List<Lookup> LookupLedgerLineTransType = new List<Lookup>();
//            LookupLedgerLineTransType = DataCache.GetNgmdLookups(x => x.LookupType.LookupTypeName == "LedgerLineTransType");

//            List<Lookup> LookupLedgerType = new List<Lookup>();
//            LookupLedgerType = DataCache.GetNgmdLookups(x => x.LookupType.LookupTypeName == "LedgerType");

//            LedgerLineList = _ctx.LedgerLines
//                .Include(x => x.product)
//                .Where(x => x.LedgerFk == id)
//                .Select(x => new TelerikLedgerLine
//                {
//                    Id = x.LedgerLineId,
//                    PartNo = x.product.partNo,
//                    Year = x.Year,
//                    Period = x.Period,
//                    LedgerLineTransTypeFk = x.LedgerLineTransTypeFk,
//                    LedgerTypeFk = x.LedgerTypeFk,
//                    Description = x.Description,
//                    DateInvoiced = x.DateInvoiced,
//                    QuantityOrdered = x.QuantityOrdered,
//                    QuantityCredited = x.QuantityCredited,
//                    QuantityOutstanding = x.QuantityOutstanding,
//                    UnitPrice = x.UnitPrice,
//                    Vat = x.Vat,
//                    Nett = x.Nett,
//                })
//                .ToList();

//            foreach(TelerikLedgerLine x in LedgerLineList)
//            {
//                x.LedgerLineTransType = LookupLedgerLineTransType.Find(y => y.LookupId == x.LedgerLineTransTypeFk).LookupName;
//                x.LedgerType = LookupLedgerType.Find(y => y.LookupId == x.LedgerTypeFk).LookupName;
//            }
//        }

//        public void GetLedgerLine(int id)
//        {
//            LedgerLine = _ctx.LedgerLines
//                .Include(x => x.productGroup)
//                .Include(x => x.salesAreaGroup)
//                .Where(x => x.LedgerLineId == id)
//                .FirstOrDefault();

//            LedgerLine.LedgerLineTransType = _ctx.Lookup
//                .Where(x => x.LookupType.LookupTypeName == "LedgerLineTransType")
//                .Where(x => x.LookupId == LedgerLine.LedgerLineTransTypeFk)
//                .Select(x => x.LookupName)
//                .FirstOrDefault();
//        }

//        public void GetVariousLookups()
//        {
//            //We already have Ledger populated
//            List<Lookup> LookupList = new List<Lookup>();
//            LookupList = _ctx.Lookup
//                .Include(x => x.LookupType)
//                .ToList();

//            Ledger.LedgerType = LookupList
//                .Where(x => x.LookupType.LookupTypeName == "LedgerType")
//                .Where(x => x.LookupId == Ledger.LedgerTypeFk)
//                .Select(x => x.LookupName)
//                .FirstOrDefault();
//            Ledger.LedgerTransType = LookupList
//                .Where(x => x.LookupType.LookupTypeName == "LedgerTransType")
//                .Where(x => x.LookupId == Ledger.LedgerTransTypeFk)
//                .Select(x => x.LookupName)
//                .FirstOrDefault();
//            Ledger.CustomerGroup = LookupList
//                .Where(x => x.LookupType.LookupTypeName == "CustomerGroup")
//                .Where(x => x.LookupId == Ledger.CustomerGroupFk)
//                .Select(x => x.LookupName)
//                .FirstOrDefault();
//            Ledger.SupplierGroup = LookupList
//                .Where(x => x.LookupType.LookupTypeName == "SupplierGroup")
//                .Where(x => x.LookupId == Ledger.SupplierGroupFk)
//                .Select(x => x.LookupName)
//                .FirstOrDefault();

//        }

//        public void SaveLedger(LedgerViewModel model)
//        {
//            using (ngmdEntities db = new ngmdEntities())
//            {
//                Ledger LedgerInDB = db.Ledgers.FirstOrDefault(x => x.LedgerId == model.Ledger.LedgerId);
//                LedgerInDB.LedgerTypeFk = model.Ledger.LedgerTypeFk;

//                db.SaveChanges();
//            }
//        }

//        public void SaveLedgerLine(LedgerViewModel model)
//        {
//            using (ngmdEntities db = new ngmdEntities())
//            {
//                LedgerLine LedgerLineInDB = db.LedgerLines.FirstOrDefault(x => x.LedgerLineId == model.LedgerLine.LedgerLineId);
//                LedgerLineInDB.Description = model.LedgerLine.Description;

//                db.SaveChanges();
//            }
//        }

//        public class TelerikLedger
//        {
//            public int Id { get; set; }
//            public int? OrderId { get; set; }
//            public int? AccountId { get; set; }
//            public string AccountShortName { get; set; }
//            public int? SupplierId { get; set; }
//            public string ProviderName { get; set; }
//            public int Year { get; set; }
//            public int Period { get; set; }
//            public int? PurchaseOrderId { get; set; }
//            public string Type { get; set; }
//            public string TransactionType { get; set; }
//            public string Description1 { get; set; }
//            public string Description2 { get; set; }
//            public DateTime DateInvoiced { get; set; }
//            public double Nett { get; set; }
//            public double Vat { get; set; }
//            public double Cost { get; set; }
//        }

//        public class TelerikLedgerLine
//        {
//            public long Id { get; set; }
//            public string PartNo { get; set; }
//            public int Year { get; set; }
//            public int Period { get; set; }
//            public int LedgerLineTransTypeFk { get; set; }
//            public string LedgerLineTransType { get; set; }
//            public int? LedgerTypeFk { get; set; }
//            public string LedgerType { get; set; }
//            public string Description { get; set; }
//            public DateTime DateInvoiced { get; set; }
//            public int QuantityOrdered { get; set; }
//            public int? QuantityCredited { get; set; }
//            public int QuantityOutstanding { get; set; }
//            public double UnitPrice { get; set; }
//            public double Vat { get; set; }
//            public double? Nett { get; set; }
//        }
//    }
//}


