using netGiant.Intranet.BusinessLayer.Utilities;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace netGiant.Intranet.BusinessLayer.ViewModels.Orders
{
    public class BackOrderViewModel : CommonViewModel
    {
        private ngmdEntities _ctx;

        public BackOrderViewModel()
        {
            _ctx = new ngmdEntities();
        }

        public IQueryable<TelerikBackOrder> BackOrderList { get; set; }
        public IQueryable<TelerikBackOrderItem> BackOrderItemList { get; set; }
        public List<BackOrderItem> BackOrderForExport { get; set; }
        public string LocalDirectory { get; set; }
        public string FilePath { get; set; }
        public BackOrder BackOrderEntry { get; set; }
        public int BackOrderId { get; set; } = 0;

        public BackOrderViewModel GetBackOrder()
        {
            BackOrderList = _ctx.BackOrder
                .Include(x => x.provider)
                .Include(x => x.BackOrderItem)
                .Include(x => x.Lookup)
                //.Include(x => x.provider)
                .OrderByDescending(x => x.OrderDate)
                .Select(x => new TelerikBackOrder
                {
                    Id = x.BackOrderId,
                    OrderReference = x.OrderReferenceNumber,
                    PurchaseOrderNumber = x.PurchaseOrderNumber,
                    Provider = x.provider.providerName,
                    SupplierOrderNumber = x.SupplierOrderNumber,
                    OrderDate = x.OrderDate,
                    Status = x.Lookup.LookupName,
                    CustomerName = x.CustomerName,
                    CostValue = x.BackOrderItem.Sum(y => y.CostPrice * (y.QuantityOrdered - y.QuantitySupplied)),
                    SellValue = x.BackOrderItem.Sum(y => y.SellPrice * (y.QuantityOrdered - y.QuantitySupplied))

                })
                .AsQueryable();

            return this;
        }

        public BackOrderViewModel GetBackOrderEntry(int id)
        {
            BackOrderEntry = _ctx.BackOrder.Find(id);

            return this;
        }

        //public SaveReturn SaveBatchLog()
        //{
        //    var saveReturn = new SaveReturn();

        //    try
        //    {
        //        _ctx.Entry(BatchLogEntry).State = EntityState.Modified;
        //        _ctx.SaveChanges();

        //        saveReturn.IsSuccess = true;
        //    }
        //    catch (Exception ex)
        //    {
        //        saveReturn.IsSuccess = false;
        //        saveReturn.Message = ex.Message;
        //    }

        //    return saveReturn;
        //}

        //public SaveReturn DeleteBatchLog(int id)
        //{
        //    SaveReturn sr = new SaveReturn();

        //    try
        //    {
        //        if (id > 0)
        //        {
        //            using (ngmdEntities db = new ngmdEntities())
        //            {
        //                BatchLog e = db.BatchLog.Where(x => x.BatchLogId == id).FirstOrDefault();
        //                db.Entry(e).State = EntityState.Deleted;
        //                db.SaveChanges();
        //                sr.IsSuccess = true;
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        sr.IsSuccess = false;
        //        sr.Message = ex.Message;
        //    }
        //    return sr;
        //}

        public BackOrderViewModel GetBackOrderItem()
        {
            BackOrderItemList = _ctx.BackOrderItem
                .Include(x => x.BackOrder)
                .Where(x => x.BackOrderFK == BackOrderId)
                .OrderBy(x => x.BackOrderItemId)
                .Select(x => new TelerikBackOrderItem
                {
                    Id = x.BackOrderItemId,
                    ItemReference = x.ItemReference,
                    SupplierItemReference = x.SupplierItemReference,
                    Description = x.Description,
                    StockReplenishmentDate = x.StockReplenishmentDate,
                    QuantityOrdered = x.QuantityOrdered,
                    QuantitySupplied = x.QuantitySupplied,
                    CostValue = x.CostPrice * (x.QuantityOrdered - x.QuantitySupplied),
                    SellValue = x.SellPrice * (x.QuantityOrdered - x.QuantitySupplied),
                    Status = x.Lookup.LookupName,
                })
                .AsQueryable();

            return this;
        }

        public BackOrderViewModel GetFullBackOrder()
        {
            //DateTime dt = DateTime.Now.AddMonths(-3);
            BackOrderForExport = _ctx.BackOrderItem
                .Include(x => x.BackOrder)
                .Where(x => x.BackOrder.Lookup.LookupName == "Open")
                .ToList();

            return this;
        }

        public void CreateBackOrderCSVFile(List<BackOrderItem> backOrderList)
        {
            FilePath = LocalDirectory + "\\PMSTempData\\BatchLogExport_" + DateTime.Now.ToString("dd_MM_yyyy_H_mm_ss") + ".csv";

            using (CsvFileWriter writer = new CsvFileWriter(FilePath, ','))
            {
                SetColumnHeadings(writer);

                foreach (BackOrderItem item in backOrderList)
                {
                    InsertCSVData(writer, item);
                }
            }
        }

        private void InsertCSVData(CsvFileWriter writer, BackOrderItem item)
        {
            CsvRow newRow = new CsvRow();
            newRow.Add(item.BackOrder.BackOrderId.ToString());
            newRow.Add(item.BackOrder.provider.providerName);
            newRow.Add(item.BackOrder.OrderReferenceNumber);
            newRow.Add(item.BackOrder.PurchaseOrderNumber);
            newRow.Add(item.BackOrder.SupplierOrderNumber);
            newRow.Add(item.BackOrder.OrderDate.ToString("dd/MM/yyyy"));
            newRow.Add(item.BackOrder.Lookup.LookupName);
            newRow.Add(item.BackOrder.CustomerName);
            newRow.Add(item.BackOrder.CustomerEmailAddress);
            newRow.Add(item.BackOrderItemId.ToString());
            newRow.Add(item.ItemReference);
            newRow.Add(item.SupplierItemReference);
            newRow.Add(item.QuantityOrdered.ToString());
            newRow.Add(item.QuantitySupplied.ToString());
            newRow.Add(item.CostPrice.ToString());
            newRow.Add(item.SellPrice.ToString());
            newRow.Add((item.CostPrice * (item.QuantityOrdered - item.QuantitySupplied)).ToString());
            newRow.Add((item.SellPrice * (item.QuantityOrdered - item.QuantitySupplied)).ToString());
            newRow.Add(item.Description);
            newRow.Add(item.StockReplenishmentDate != null ? item.StockReplenishmentDate.Value.ToString("dd/MM/yyyy") : "");
            newRow.Add(item.Lookup.LookupName);

            writer.WriteRow(newRow);
        }
        private void SetColumnHeadings(CsvFileWriter writer)
        {
            CsvRow firstRow = new CsvRow();
            firstRow.Add("BackOrderId");
            firstRow.Add("Provider");
            firstRow.Add("OrderReferenceNumber");
            firstRow.Add("PurchaseOrderNumber");
            firstRow.Add("SupplierOrderNumber");
            firstRow.Add("OrderDate");
            firstRow.Add("Status");
            firstRow.Add("CustomerName");
            firstRow.Add("CustomerEmailAddress");
            firstRow.Add("BackOrderItemId");
            firstRow.Add("ItemReference");
            firstRow.Add("SupplierItemReference");
            firstRow.Add("QuantityOrdered");
            firstRow.Add("QuantitySupplied");
            firstRow.Add("UnitCost");
            firstRow.Add("UnitNett");
            firstRow.Add("TotalCost");
            firstRow.Add("TotalNett");
            firstRow.Add("Description");
            firstRow.Add("StockReplenishmentDate");
            firstRow.Add("Status");

            writer.WriteRow(firstRow);
        }

        public SaveReturn SwitchStatus(int id)
        {
            SaveReturn sr = new SaveReturn();

            try
            {
                if (id > 0)
                {
                    using (ngmdEntities db = new ngmdEntities())
                    {
                        List<Lookup> llu = db.Lookup.Where(x => x.LookupType.LookupTypeName == "Back Order Status").ToList();
                        BackOrder bo = db.BackOrder.Where(x => x.BackOrderId == id).FirstOrDefault();
                        if (bo.Lookup.LookupName == "Open")
                        {
                            bo.StatusFK = llu.Where(x => x.LookupName == "Closed").FirstOrDefault().LookupId;
                        }
                        else
                        {
                            bo.StatusFK = llu.Where(x => x.LookupName == "Open").FirstOrDefault().LookupId;
                        }
                        db.Entry(bo).State = EntityState.Modified;
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

        public SaveReturn SwitchLineStatus(int id)
        {
            SaveReturn sr = new SaveReturn();

            try
            {
                if (id > 0)
                {
                    using (ngmdEntities db = new ngmdEntities())
                    {
                        List<Lookup> llu = db.Lookup.Where(x => x.LookupType.LookupTypeName == "Back Order Status").ToList();
                        BackOrderItem boi = db.BackOrderItem
                            .Include(x => x.BackOrder)
                            .Where(x => x.BackOrderItemId == id)
                            .FirstOrDefault();
                        List<BackOrderItem> lboi = db.BackOrderItem
                            .Where(x => x.BackOrder.BackOrderId == boi.BackOrder.BackOrderId && x.Lookup.LookupName == "Open")
                            .ToList();

                        if (boi.Lookup.LookupName == "Open")
                        {
                            boi.StatusFK = llu.Where(x => x.LookupName == "Closed").FirstOrDefault().LookupId;
                            boi.QuantitySupplied = boi.QuantityOrdered;
                            if (lboi.Count == 1)
                            {
                                boi.BackOrder.StatusFK = llu.Where(x => x.LookupName == "Closed").FirstOrDefault().LookupId;
                            }
                        }
                        else
                        {
                            boi.StatusFK = llu.Where(x => x.LookupName == "Open").FirstOrDefault().LookupId;
                            boi.QuantitySupplied = boi.QuantityOrdered - 1;
                            boi.BackOrder.StatusFK = llu.Where(x => x.LookupName == "Open").FirstOrDefault().LookupId;
                        }
                        db.Entry(boi).State = EntityState.Modified;
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

        //public void CreateBackOrderCSVFile(List<BackOrderItem> backOrderList)
        //{
        //    FilePath = LocalDirectory + "\\PMSTempData\\BatchLogExport_" + DateTime.Now.ToString("dd_MM_yyyy_H_mm_ss") + ".csv";

        //    using (CsvFileWriter writer = new CsvFileWriter(FilePath, ','))
        //    {
        //        SetColumnHeadings(writer);

        //        foreach (BackOrderItem item in backOrderList)
        //        {
        //            InsertCSVData(writer, item);
        //        }
        //    }
        //}

        //private void InsertCSVData(CsvFileWriter writer, BackOrderItem item)
        //{
        //    CsvRow newRow = new CsvRow();
        //    newRow.Add(item.BatchLog.BatchLogId.ToString());
        //    newRow.Add(item.BatchLog.Command);
        //    newRow.Add(item.BatchLog.Type == null ? "" : log.BatchLog.Type);
        //    newRow.Add(item.BatchLog.SubType == null ? "" : log.BatchLog.SubType);
        //    newRow.Add(item.BatchLog.Website == null ? "All" : log.BatchLog.Website.FriendlyName);
        //    newRow.Add(item.BatchLog.DateTime.ToString("dd/MM/yyyy"));
        //    newRow.Add(item.DateTime.ToString("dd/MM/yyyy"));
        //    newRow.Add(item.Message == null ? "" : log.Message);
        //    newRow.Add(item.ErrorCode == null ? "" : log.ErrorCode);
        //    newRow.Add(item.BatchLog.Comments == null ? "" : log.BatchLog.Comments);

        //    writer.WriteRow(newRow);
        //}
        //private void SetColumnHeadings(CsvFileWriter writer)
        //{
        //    CsvRow firstRow = new CsvRow();
        //    firstRow.Add("BatchLogId");
        //    firstRow.Add("Command");
        //    firstRow.Add("Type");
        //    firstRow.Add("SubType");
        //    firstRow.Add("Website");
        //    firstRow.Add("StartTime");
        //    firstRow.Add("LogTime");
        //    firstRow.Add("Message");
        //    firstRow.Add("ErrorCode");
        //    firstRow.Add("Comments");

        //    writer.WriteRow(firstRow);
        //}
    }

    public class TelerikBackOrder
    {
        public int Id { get; set; }
        public string OrderReference { get; set; }
        public string PurchaseOrderNumber { get; set; }
        public string Provider { get; set; }
        public string SupplierOrderNumber { get; set; }
        public DateTime OrderDate { get; set; }
        public string Status { get; set; }
        public string CustomerName { get; set; }
        public double CostValue { get; set; }
        public double SellValue { get; set; }
        public bool HasError { get; set; }
    }

    public class TelerikBackOrderItem
    {
        public int Id { get; set; }
        public string ItemReference { get; set; }
        public string SupplierItemReference { get; set; }
        public string Description { get; set; }
        public DateTime? StockReplenishmentDate { get; set; }
        public int QuantityOrdered { get; set; }
        public int QuantitySupplied { get; set; }
        public double CostValue { get; set; }
        public double SellValue { get; set; }
        public string Status { get; set; }
    }

}
