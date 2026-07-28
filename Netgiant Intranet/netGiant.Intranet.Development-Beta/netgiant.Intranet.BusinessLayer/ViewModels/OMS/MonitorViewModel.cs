//using System;
//using System.Linq;
//using netGiant.Intranet.DataLayer.NetgiantMasterData;
//using System.Data.Entity;
//using System.Web.Mvc;
//using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
//using PagedList;
//using System.Linq.Expressions;
//using System.Collections.Generic;
//using System.Data.Entity.SqlServer;
//using System.Data.SqlClient;
//using netGiant.Intranet.BusinessLayer.Utilities;
//using System.Data;

//namespace netGiant.Intranet.BusinessLayer.ViewModels.OMS
//{
//    public class MonitorViewModel : CommonViewModel
//    {

//        private ngmdEntities _ctx;

//        public MonitorViewModel()
//        {
//            _ctx = new ngmdEntities();
//        }

//        public IQueryable<TelerikSO> SOList { get; set; }
//        public IQueryable<TelerikSOP> SOPList { get; set; }
//        public IQueryable<TelerikOOP> OOPList { get; set; }
//        //public List<ProductData> ProductDetailsList { get; set; }
//        public DataTable ProductDetails { get; set; }
//        public DataTable SupplierDetails { get; set; }


//        public void GetSO()
//        {
//            SOList = _ctx.Orders
//                .Where(x => !x.IsApproved)
//                .Include(x => x.OrderLines)
//                .Include(x => x.Contact.Account.Addresses)
//                .Include(x => x.Address)
//                .Select(x => new TelerikSO
//                {
//                    OrderId = x.OrderId,
//                    HasStock = true,
//                    CustCreditHold = x.Contact.Account.IsOnHold,
//                    CustCreditExceed = x.Contact.Account.IsOnHold,
//                    CustTermsExceed = x.Contact.Account.IsOnHold,
//                    AccountId = x.Contact.AccountFk,
//                    AxisAccount = x.Contact.Account.AxisNo,
//                    Name = x.Contact.Account.Name,
//                    ShortName = x.Contact.Account.ShortName,
//                    CurrentBalance = x.Contact.Account.CreditAccounts.FirstOrDefault().CurrentBalance,
//                    CreditLimit = x.Contact.Account.CreditAccounts.FirstOrDefault().CreditLimit,
//                    OrderDate = x.DateOrdered,
//                    Last3Orders = (
//                        _ctx.Orders
//                            .Where(y => y.Contact.Account == x.Contact.Account && y.OrderId != x.OrderId)
//                            .OrderByDescending(y => y.DateOrdered)
//                            .Take(3)
//                            .Select(y => new OrderSnapshot
//                            {
//                                OrderId = y.OrderId,
//                                Date = SqlFunctions.DatePart("day", y.DateOrdered).ToString() + "/"
//                                    + SqlFunctions.DatePart("month", y.DateOrdered).ToString() + "/"
//                                    + SqlFunctions.DatePart("year", y.DateOrdered).ToString(),
//                                Nett = y.Nett
//                            })                            
//                    )
//                    .AsQueryable()
//                })
//                .AsQueryable();
//        }

//        public void GetSOP()
//        {
//            SOPList = _ctx.Orders
//                .Where(x => x.IsApproved && !x.PurchaseOrders.Any())
//                .Include(x => x.OrderLines)
//                .Include(x => x.Contact.Account.Addresses)
//                .Include(x => x.Address)
//                .Include(x => x.PurchaseOrders)
//                .Select(x => new TelerikSOP
//                {
//                    OrderId = x.OrderId,
//                    AccountId = x.Contact.AccountFk,
//                    AxisAccount = x.Contact.Account.AxisNo,
//                    Name = x.Contact.Account.Name,
//                    ShortName = x.Contact.Account.ShortName,
//                    Nett = x.Nett,
//                    OrderDate = x.DateOrdered
//                })
//                .AsQueryable();
//        }

//        public void GetOOP()
//        {
//            OOPList = _ctx.PurchaseOrders
//                .Where(x => x.IsClosed == false && x.IsCompleted == false)
//                .Include(x => x.Order.Contact)
//                .Include(x => x.Order.Address)
//                .Include(x => x.provider)
//                .Include(x => x.PurchaseOrderLines)
//                .Include(x => x.Ledgers)
//                .Select(x => new TelerikOOP
//                {
//                    AxisPo = x.AxisPONo,
//                    Id = x.PurchaseOrderId,
//                    OrderId = x.Order.OrderId,
//                    IsApproved = x.Order.IsApproved,
//                    IsExcluded = false,
//                    IsSent = x.IsSent,
//                    IsAcknowledged = x.IsAcknowledged,
//                    IsReceived = x.PurchaseOrderLines.All(y => y.IsReceived) ? 1 : x.PurchaseOrderLines.Any(y => y.IsReceived) ? 2 : 3,
//                    IsInvoiced = x.PurchaseOrderLines.All(y => y.IsInvoiced) ? 1 : x.PurchaseOrderLines.Any(y => y.IsInvoiced) ? 2 : 3,
//                    SupplierName = x.provider.providerName,
//                    OrderDate = x.Order.DateOrdered,
//                    DueDate = x.Order.DueDate,
//                    DelName = x.Order.Contact.FirstName + " " + x.Order.Contact.LastName,
//                    DelPostcode = x.Order.Address.Postcode,
//                    Nett = x.Order.Nett,
//                    Outstanding = x.Nett - (x.Ledgers.Sum(y => (double?)y.Nett) ?? 0)
//                })
//                .AsQueryable();
//        }

//        public void GetSupplierOptions(int orderId)
//        {
//            List<SqlParameter> sqlParms = new List<SqlParameter>();
//            SqlParameter sqlParm = new SqlParameter("@OrderId", SqlDbType.Int);
//            sqlParm.Value = orderId;
//            sqlParms.Add(sqlParm);

//            DataSet ds = SQLUtilities.ExecuteReadStoredProcedure("netgiantMasterData", "ngmd.GetSupplierOptions", sqlParms, "SupplierOptions");
//            ProductDetails = ds.Tables[0];
//            SupplierDetails = ds.Tables[1];
//        }

//        public class TelerikSO
//        {
//            public int OrderId { get; set; }
//            public bool HasStock { get; set; }
//            public bool CustCreditHold { get; set; }
//            public bool CustCreditExceed { get; set; }
//            public bool CustTermsExceed { get; set; }
//            public int AccountId { get; set; }
//            public string AxisAccount { get; set; }
//            public string Name { get; set; }
//            public string ShortName { get; set; }
//            public decimal CurrentBalance { get; set; }
//            public decimal CreditLimit { get; set; }
//            public DateTime OrderDate { get; set; }
//            public IQueryable<OrderSnapshot> Last3Orders { get; set; }
//        }

//        public class OrderSnapshot
//        {
//            public int OrderId { get; set; }
//            public string Date { get; set; }
//            public double Nett { get; set; }
//        }

//        public class TelerikSOP
//        {
//            public int OrderId { get; set; }
//            public int AccountId { get; set; }
//            public string Name { get; set; }
//            public string AxisAccount { get; set; }
//            public string ShortName { get; set; }
//            public double Nett { get; set; }
//            public DateTime OrderDate { get; set; }
//        }

//        public class TelerikOOP
//        {
//            public int Id { get; set; }
//            public int OrderId { get; set; }
//            public bool IsApproved { get; set; }
//            public bool IsExcluded { get; set; }
//            public bool IsSent { get; set; }
//            public bool IsAcknowledged { get; set; }
//            public int IsReceived { get; set; }
//            public int IsInvoiced { get; set; }
//            public string SupplierName { get; set; }
//            public string AxisPo { get; set; }
//            public DateTime OrderDate { get; set; }
//            public DateTime DueDate { get; set; }
//            public string DelName { get; set; }
//            public string DelPostcode { get; set; }
//            public double Nett { get; set; }
//            public double Outstanding { get; set; }
//        }

//     }

//    public class SupplierData
//    {
//        public string SupplierName { get; set; }
//        public string SupplierShortName { get; set; }
//        public int SupplierId { get; set; }        
//        public decimal SupplierPrice { get; set; }
//        public int SupplierStock { get; set; }
//    }

//    //public class ProductData
//    //{
//    //    public int ProductId { get; set; }
//    //    public string Description { get; set; }
//    //    public int TotalStock { get; set; }
//    //    public int QuantityOrdered { get; set; }
//    //    public List<SupplierData> SupplierDetailsList { get; set; }
//    //}

//}



