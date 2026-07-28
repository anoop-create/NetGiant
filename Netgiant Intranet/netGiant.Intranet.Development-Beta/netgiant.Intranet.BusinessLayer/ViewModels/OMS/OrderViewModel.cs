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
//    public class OrderViewModel : CommonViewModel
//    {

//        private ngmdEntities _ctx;

//        public OrderViewModel()
//        {
//            _ctx = new ngmdEntities();
//        }




//        public IQueryable<TelerikOrder> OrderList { get; set; }
//        public List<TelerikOrderLine> OrderLineList { get; set; }
//        public Order Order { get; set; }
//        public OrderLine OrderLine { get; set; }
//        public IQueryable<SelectListItem> PaymentMethodList { get; set; }
//        public IQueryable<SelectListItem> DeliveryServiceList { get; set; }
//        public IQueryable<SelectListItem> OrderSourceList { get; set; }
//        public IQueryable<SelectListItem> OrderStatusList { get; set; }
//        public IQueryable<SelectListItem> CustomerGroupList { get; set; }

//        public void GetOrders(string SearchString, Int32 AccountNumber)
//        {
//            OrderList = _ctx.Orders
//                .Include(x => x.Contact.Account)
//                .Include(x => x.Address)
//                .Where(x => AccountNumber == 0 ? true : x.Contact.AccountFk == AccountNumber)
//                .Where(
//                    x => string.IsNullOrEmpty(SearchString) ? true : 
//                        x.OrderId.ToString().Contains(SearchString)
//                        ||
//                        x.CustomerOrderNo.Contains(SearchString)
//                        ||
//                        x.InternalOrderNo.Contains(SearchString)
//                        ||
//                        x.Contact.Account.ShortName.Contains(SearchString)
//                        ||
//                        x.Contact.Account.Name.Contains(SearchString)
//                        ||
//                        x.Contact.Account.AxisNo.Contains(SearchString)
//                        ||
//                        x.Address.Line1.Contains(SearchString)
//                        ||
//                        x.Address.Line2.Contains(SearchString)
//                        ||
//                        x.Address.Postcode.Contains(SearchString)
//                        ||
//                        x.Contact.Email.Contains(SearchString)
//                        ||
//                        x.Contact.Account.AxisNo.Contains(SearchString)
//                        ||
//                        x.Contact.TelephoneNumber.Contains(SearchString)
//                ) 
//                .Select(x => new TelerikOrder
//                {
//                    Id = x.OrderId,
//                    CustOrderNo = x.CustomerOrderNo,
//                    IntOrderNo = x.InternalOrderNo,
//                    OrderedBy = x.Contact.FirstName + " " + x.Contact.LastName,
//                    OrderedFor = x.Contact.Account.Name,
//                    DateOrdered = x.DateOrdered,
//                    DateReceived = x.DateReceived,
//                    DueDate = x.DueDate,
//                    DateCompleted = x.DateCompleted,
//                    Nett = x.Nett,
//                    Cost = x.Cost,
//                    AccountId = x.Contact.Account.AccountId
//                })
//                .AsQueryable();
//        }

//        public void GetOrder(int id)
//        {
//            PaymentMethodList = SelectListViewModel.GetLookupTypeList("PaymentMethod");

//            DeliveryServiceList = _ctx.deliveryService
//                .Select(x => new SelectListItem 
//                { 
//                    Value = x.DeliveryServiceId.ToString(),
//                    Text = x.ServiceName
//                });

//            OrderSourceList = _ctx.OrderSources
//                .Select(x => new SelectListItem
//                {
//                    Value = x.OrderSourceId.ToString(),
//                    Text = x.Description,
//                });

//            OrderStatusList = SelectListViewModel.GetLookupTypeList("OMSStatus");

//            CustomerGroupList = SelectListViewModel.GetLookupTypeList("CustomerGroup");

//            Order = _ctx.Orders
//                .Include(x => x.deliveryService)
//                .Where(x => x.OrderId == id)
//                .FirstOrDefault()
//                ;
//        }

//            public void GetOrderLines(int id)
//        {
//            OrderLineList = _ctx.OrderLines
//                .Where(x => x.OrderFk == id)
//                .OrderBy(x => x.LineNo)
//                .Select(x => new TelerikOrderLine
//                {
//                    OrderLineId = x.OrderLineId,
//                    PartNo = "XX123",
//                    LineNo = x.LineNo,
//                    Description = x.Description,
//                    UnitPrice = x.UnitPrice,
//                    Quantity = x.QuantityOrdered,
//                    Nett = x.Nett,
//                    Vat = x.Vat
//                })
//                .ToList();
//        }

//        public void GetOrderLine(int id)
//        {
//            OrderLine = _ctx.OrderLines
//                .Include(x => x.product)
//                .Where(x => x.OrderLineId == id)
//                .FirstOrDefault();

//        }

//        public void GetVariousLookups()
//        {
//            //We already have Order populated
//            List<Lookup> LookupList = new List<Lookup>();
//            LookupList = _ctx.Lookup
//                .Include(x => x.LookupType)
//                .ToList();

//            Order.OrderStatus = LookupList
//                .Where(x => x.LookupType.LookupTypeName == "OMSStatus")
//                .Where(x => x.LookupId == Order.OrderStatusFk)
//                .Select(x => x.LookupName)
//                .FirstOrDefault();
//            Order.PaymentMethod = LookupList
//                .Where(x => x.LookupType.LookupTypeName == "PaymentMethod")
//                .Where(x => x.LookupId == Order.PaymentMethodFk)
//                .Select(x => x.LookupName)
//                .FirstOrDefault();
//            Order.CustomerGroup = LookupList
//                .Where(x => x.LookupType.LookupTypeName == "CustomerGroup")
//                .Where(x => x.LookupId == Order.CustomerGroupFk)
//                .Select(x => x.LookupName)
//                .FirstOrDefault();

//            Order.DeliveryAddress = _ctx.Addresses
//                .Where(x => x.AddressId == Order.DeliveryAddressFk)
//                .Select(x => x.Postcode)
//                .FirstOrDefault();
//        }

//        public void SaveOrder(OrderViewModel model)
//        {
//            using(ngmdEntities db = new ngmdEntities())
//            {
//                Order OrderInDB = db.Orders.FirstOrDefault(x => x.OrderId == model.Order.OrderId);
//                OrderInDB.CustomerOrderNo = model.Order.CustomerOrderNo;
//                OrderInDB.InternalOrderNo = model.Order.InternalOrderNo;
//                OrderInDB.PaymentMethodFk = model.Order.PaymentMethodFk;
//                OrderInDB.DeliveryServiceFk = model.Order.DeliveryServiceFk;
//                OrderInDB.OrderSourceFk = model.Order.OrderSourceFk;
//                OrderInDB.OrderStatusFk = model.Order.OrderStatusFk;
//                OrderInDB.CustomerGroupFk = model.Order.CustomerGroupFk;

//                db.SaveChanges();
//            }
//        }

//        public void SaveOrderLine(OrderViewModel model)
//        {
//            using (ngmdEntities db = new ngmdEntities())
//            {
//                OrderLine OrderLineInDB = db.OrderLines.FirstOrDefault(x => x.OrderLineId == model.OrderLine.OrderLineId);
//                OrderLineInDB.Description = model.OrderLine.Description;

//                db.SaveChanges();
//            }
//        }

//        public class TelerikOrder
//        {
//            public int Id { get; set; }
//            public string CustOrderNo { get; set; }
//            public string IntOrderNo { get; set; }
//            public string OrderedBy { get; set; }
//            public string OrderedFor { get; set; }
//            public string ContactTelNo { get; set; }
//            public string ContactName { get; set; }
//            public decimal CreditLimit { get; set; }
//            public DateTime DateOrdered { get; set; }
//            public DateTime? DateReceived { get; set; }
//            public DateTime DueDate { get; set; }
//            public DateTime? DateCompleted { get; set; }
//            public double Nett { get; set; }
//            public double Cost { get; set; }
//            public int AccountId { get; set; }
//        }

//        public class TelerikOrderLine
//        {
//            public long OrderLineId { get; set; }
//            public string PartNo { get; set; }
//            public int LineNo { get; set; }
//            public string Description { get; set; }
//            public double UnitPrice { get; set; }
//            public double Vat { get; set; }
//            public double Nett { get; set; }
//            public int Quantity { get; set; }
//        }
//    }
//}

