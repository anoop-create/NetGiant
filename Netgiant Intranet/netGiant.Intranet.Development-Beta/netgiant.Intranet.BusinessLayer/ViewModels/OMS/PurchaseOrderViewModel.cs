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
//    public class PurchaseOrderViewModel : CommonViewModel
//    {

//        private ngmdEntities _ctx;

//        public PurchaseOrderViewModel()
//        {
//            _ctx = new ngmdEntities();
//        }

//        public IQueryable<TelerikPurchaseOrder> PurchaseOrderList { get; set; }
//        public IQueryable<SelectListItem> DeliveryServiceList { get; set; }
//        public List<TelerikPurchaseOrderLine> PurchaseOrderLineList { get; set; }
//        public int PurchaseOrderId { get; set; }
//        public int PurchaseOrderLineCount { get; set; }
//        public PurchaseOrder PurchaseOrder { get; set; }
//        public PurchaseOrderLine PurchaseOrderLine { get; set; }

//        public void GetPurchaseOrders(string SearchString, Int32 PurchaseOrderOrderFk)
//        {
//            PurchaseOrderList = _ctx.PurchaseOrders
//                .Include(x => x.provider)
//                .Where(x => PurchaseOrderOrderFk == 0 ? true : x.OrderFk == PurchaseOrderOrderFk)
//                .Where(
//                    x => string.IsNullOrEmpty(SearchString) ? true :
//                        x.PurchaseOrderId.ToString().Contains(SearchString)
//                        ||
//                        x.provider.providerName.Contains(SearchString)
//                )
//                .Select(x => new TelerikPurchaseOrder
//                {
//                    Id = x.PurchaseOrderId,
//                    OrderId = x.OrderFk,
//                    Supplier = x.provider.providerName,
//                    AxisPONo = x.AxisPONo,
//                    LastUpdatedBy = x.LastUpdatedBy,
//                    DueDate = x.DueDate,
//                    DateClosed = x.DateClosed,
//                    IsCompleted = x.IsCompleted,
//                    Nett = x.Nett
//                })
//                .AsQueryable();
//        }

//        public void GetPurchaseOrder(int id)
//        {
//            DeliveryServiceList = _ctx.deliveryService
//                .Select(x => new SelectListItem
//                {
//                    Value = x.DeliveryServiceId.ToString(),
//                    Text = x.ServiceName
//                });

//            PurchaseOrder = _ctx.PurchaseOrders
//                .Include(x => x.provider)
//                .Where(x => x.PurchaseOrderId == id)
//                .FirstOrDefault();
//        }

//        public void GetPurchaseOrderLines(int id)
//        {
//            PurchaseOrderLineList = _ctx.PurchaseOrderLines
//                .Where(x => x.PurchaseOrderFk == id)
//                .Select(x => new TelerikPurchaseOrderLine
//                {
//                    Id = x.PurchaseOrderLineId,
//                    ProductId = x.ProductFk,
//                    ProductGroup = x.ProductGroupFk,
//                    SupplierStockReference = x.SupplierStockReference,
//                    Description = x.Description,
//                    UnitPrice = x.UnitPrice,
//                    Nett = x.Nett,
//                    QuantityOrdered = x.QuantityOrdered,
//                    QuantityReceived = x.QuantityReceived,
//                    IsReturned = x.IsReturned,
//                    IsComplete = x.IsCompleted
//                })
//                .ToList();
//        }

//        public void GetPurchaseOrderLine(int id)
//        {
//            PurchaseOrderLine = _ctx.PurchaseOrderLines
//                .Where(x => x.PurchaseOrderLineId == id)
//                .FirstOrDefault();
//        }

//        public void SavePurchaseOrder(PurchaseOrderViewModel model)
//        {
//            using (ngmdEntities db = new ngmdEntities())
//            {
//                PurchaseOrder PurchaseOrderInDB = db.PurchaseOrders.FirstOrDefault(x => x.PurchaseOrderId == model.PurchaseOrder.PurchaseOrderId);
//                PurchaseOrderInDB.DeliveryServiceFk = model.PurchaseOrder.DeliveryServiceFk;

//                db.SaveChanges();
//            }
//        }

//        public void SavePurchaseOrderLine(PurchaseOrderViewModel model)
//        {
//            using (ngmdEntities db = new ngmdEntities())
//            {
//                PurchaseOrderLine PurchaseOrderLineInDB = db.PurchaseOrderLines.FirstOrDefault(x => x.PurchaseOrderLineId == model.PurchaseOrderLine.PurchaseOrderLineId);
//                PurchaseOrderLineInDB.Description = model.PurchaseOrderLine.Description;

//                db.SaveChanges();
//            }
//        }

//        public class TelerikPurchaseOrder
//        {
//            public int Id { get; set; }
//            public int OrderId { get; set; }
//            public string Supplier { get; set; }
//            public string AxisPONo { get; set; }
//            public string LastUpdatedBy { get; set; }
//            public decimal CreditLimit { get; set; }
//            public DateTime DueDate { get; set; }
//            public DateTime? DateClosed { get; set; }
//            public bool IsCompleted { get; set; }
//            public double Nett { get; set; }
//        }

//        public class TelerikPurchaseOrderLine
//        {
//            public long Id { get; set; }
//            public int? ProductId { get; set; }
//            public int? ProductGroup { get; set; }
//            public string SupplierStockReference { get; set; }
//            public string Description { get; set; }
//            public double UnitPrice { get; set; }
//            public double Nett { get; set; }
//            public int QuantityOrdered { get; set; }
//            public int QuantityReceived { get; set; }
//            public bool IsReturned { get; set; }
//            public bool IsComplete { get; set; }

//        }
//    }
//}


