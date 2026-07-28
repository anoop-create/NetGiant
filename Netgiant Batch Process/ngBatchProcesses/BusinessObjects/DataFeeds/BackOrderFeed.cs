using netGiant.Intranet.DataLayer.NetgiantMasterData;
using ngBatchProcesses.BusinessObjects.Shared;
using NGBP.DataAccessLayer.DataUtilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Text;

namespace ngBatchProcesses.BusinessObjects.DataFeeds
{
    public class BackOrderFeed
    {
        public BackOrderFeed(Dictionary<string, string> parms)
        {
            StandardFunctions.WriteProcessStarted();
            Parms = parms;
            if (Parms.ContainsKey("subtype"))
            {
                SubType = Parms["subtype"];
            }
            if (Parms.ContainsKey("action"))
            {
                Action = Parms["action"];
            }
            BackOrderStatusList = EntityFunctions.GetNgmdLookup(x => x.LookupType.LookupTypeName == "Back Order Status");

            RootDirectory = Properties.Settings.Default.LocalDirectory;
            WorkingPath = RootDirectory + (string)Properties.Settings.Default["BackOrdersFilePath"] + "Working\\";
            ArchivePath = RootDirectory + (string)Properties.Settings.Default["BackOrdersFilePath"] + "Archive\\";
        }

        public Dictionary<string, string> Parms { get; set; }
        public string SubType { get; set; } = "";
        public string Action { get; set; } = "";
        public string RootDirectory { get; set; }
        public string WorkingPath { get; set; }
        public string ArchivePath { get; set; }
        public List<Lookup> BackOrderStatusList { get; set; }
        private DateTime FtpFileDate { get; set; }

        public void LoadData()
        {
            // Build a list of ftp files to download
            List<provider> lprov = EntityFunctions.GetProviderList(x => x.active, "Back Order Supplier");
            StandardFunctions stnFunc = new StandardFunctions();

            foreach (provider p in lprov)
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Processing Provider: " + p.providerName });

                List<fieldMapping> lfm = p.fieldMapping.ToList();
                DataTable dt = new DataTable();
                dt.Columns.Add("purchaseOrderNumber", typeof(string));
                dt.Columns.Add("supplierOrderNumber", typeof(string));
                dt.Columns.Add("orderDate", typeof(DateTime));
                dt.Columns.Add("supplierItemReference", typeof(string));
                dt.Columns.Add("itemReference", typeof(string));
                dt.Columns.Add("description", typeof(string));
                dt.Columns.Add("quantityOrdered", typeof(int));
                dt.Columns.Add("quantitySupplied", typeof(int));
                dt.Columns.Add("quantityOutstanding", typeof(int));
                dt.Columns.Add("stockReplenishmentDate", typeof(DateTime));

                dt.Columns["purchaseOrderNumber"].DefaultValue = null;
                dt.Columns["supplierOrderNumber"].DefaultValue = null;
                dt.Columns["orderDate"].DefaultValue = null;
                dt.Columns["orderDate"].AllowDBNull = true;
                dt.Columns["supplierItemReference"].DefaultValue = null;
                dt.Columns["description"].DefaultValue = "";
                dt.Columns["itemReference"].DefaultValue = null;
                dt.Columns["quantityOrdered"].DefaultValue = 0;
                dt.Columns["quantitySupplied"].DefaultValue = 0;
                dt.Columns["quantityOutstanding"].DefaultValue = 0;
                dt.Columns["stockReplenishmentDate"].DefaultValue = null;
                dt.Columns["stockReplenishmentDate"].AllowDBNull = true;

                try
                {
                    foreach (ftpDetails ftpd in p.ftpDetails)
                    {
                        // Download the file
                        string ftpFilename = (String.IsNullOrEmpty(ftpd.ftpFolder) ? "" : "/" + ftpd.ftpFolder + "/") + ftpd.ftpFilename;
                        Tuple<bool, string> rtn = FtpUtilities.DownloadFTPFile(
                            ftpd.ftpHost,
                            ftpd.ftpUser,
                            ftpd.ftpPassword,
                            ftpFilename,
                            WorkingPath + ftpd.ftpFilename,
                            false);
                        if (rtn.Item1)
                        {
                            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "FluentFTP Download Successful for: " + ftpFilename });
                        }
                        else
                        {
                            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR FluentFTP unable to Download FTP File: " + ftpFilename + ". " + rtn.Item2, ErrorCode = "ERROR" });
                        }

                        if (File.Exists(WorkingPath + ftpd.ftpFilename))
                        {
                            // Process the file
                            string filetype = ftpd.ftpFilename.Split('.')[1].ToLower();
                            DataTable dtBackOrderReport = new DataTable();
                            switch (filetype)
                            {
                                case "xlsx":
                                case "xls":
                                    {
                                        dtBackOrderReport = ExcelUtilities.LoadWorksheetInDataTable(WorkingPath + ftpd.ftpFilename);
                                        break;
                                    }
                                case "csv":
                                    {
                                        dtBackOrderReport = CsvUtilities.LoadCsvInDataTable(WorkingPath + ftpd.ftpFilename);
                                        break;
                                    }
                                default:
                                    {
                                        break;
                                    }
                            }

                            dt = StandardFunctions.RationaliseTable(dtBackOrderReport, dt, lfm);

                            ftpd.dateLastFeedFile = FtpUtilities.FileLastModifiedDate;
                            if (!EntityFunctions.SaveFtpDetails(ftpd))
                            {
                                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Unable to update FTP Feed File Date", ErrorCode = "WARNING" });
                            }
                        }
                    }
                    // Process DataTable                
                    foreach (DataRow dr in dt.Rows)
                    {
                        string sql = @"SELECT A.email AS [Email],
                        A.cont AS [Name],
                        --I.record AS [HasInvoice],
                        ISNULL(POL2.sir, POL1.sir) AS [StockRef], 
                        POL1.qor AS [QuantityOrdered],
                        PO.pon AS [PONumber],
                        PO.srf AS [SONumber],
                        P.dl1 + ' ' + P.dl2 AS [Description],
                        PO.oso AS [OrderNumber],
                        CASE
		                    WHEN O.csg IN (10,11,12,14,13) THEN 1
		                    WHEN O.csg IN (0,1,3) THEN 2
		                    WHEN O.csg IN (5,6,7) THEN 3
		                    ELSE 1
	                    END AS [WebsiteId],
                        ISNULL(NULLIF(POL1.cup, 0), ISNULL(POL2.cup, 0)) AS [CostPrice],
		                ROUND(ISNULL(OL.upr, 0) * 100 / (100 + ISNULL(OL.vrt, 0)), 2) AS [SellPrice]
                        FROM [AXIS14080CO1].[dbo].accpom00 PO
                        INNER JOIN [AXIS14080CO1].[dbo].accpol00 POL1 ON POL1.doc = PO.pon AND POL1.rtp IN (1,2)
                        LEFT OUTER JOIN [AXIS14080CO1].[dbo].accpol00 POL2 ON POL2.record = POL1.ptr AND POL2.rtp = 3
                        INNER JOIN [AXIS14080CO1].[dbo].accsom00 O ON O.drf = PO.oso
                        LEFT OUTER JOIN [AXIS14080CO1].[dbo].accsol00 OL ON OL.ono = O.drf AND OL.sir = ISNULL(POL2.sir, POL1.sir) AND OL.rtl IN (1,3)
                        LEFT OUTER JOIN [AXIS14080CO1].[dbo].accstk00 P ON P.ref = ISNULL(POL2.sir, POL1.sir)
                        INNER JOIN [AXIS14080CO1].[dbo].accaad01 A ON A.adref = O.cusrf AND A.no = REPLACE(STR(O.conno,4),' ','0')";

                        if (dr["purchaseOrderNumber"] != null && dr["supplierItemReference"] != null)
                        {
                            sql += @"
                            WHERE PO.pon = '" + dr["purchaseOrderNumber"] + @"' AND POL1.ssr = '" + dr["supplierItemReference"] + @"' 
                            AND POL1.qor > 0";
                        }
                        else
                        {
                            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Error on Back Order Report: Purchase Order Number or Supplier Item Reference not supplied", ErrorCode = "ERROR" });
                            // Don't select any records
                            sql += @"
                            WHERE 0 = 1";
                        }

                        DataTable axisData = SQLUtilities.ExecuteReadInline("axisdiplomat", sql, "ds", 60).Tables[0];

                        if (axisData.Rows.Count > 0)
                        {
                            DateTime od = new DateTime();
                            if (!DateTime.TryParse(dr["orderDate"].ToString(), out od))
                            {
                                od = DateTime.Now;
                            }
                            BackOrder bo = new BackOrder
                            {
                                ProviderFK = p.providerID,
                                WebsiteFK = (int)axisData.Rows[0]["WebsiteId"],
                                OrderReferenceNumber = axisData.Rows[0]["OrderNumber"].ToString(),
                                PurchaseOrderNumber = axisData.Rows[0]["PONumber"].ToString(),
                                SupplierOrderNumber = axisData.Rows[0]["SONumber"].ToString(),
                                OrderDate = od,
                                StatusFK = BackOrderStatusList.FirstOrDefault(x => x.LookupName == "Open").LookupId,
                                CustomerName = axisData.Rows[0]["Name"].ToString(),
                                CustomerEmailAddress = axisData.Rows[0]["Email"].ToString()
                            };
                            int boId = SaveBackOrder(bo);

                            if (boId > 0)
                            {
                                int quantityOrdered = dr["quantityOrdered"] != null ? int.Parse(dr["quantityOrdered"].ToString()) : int.Parse(axisData.Rows[0]["OrderNumber"].ToString());
                                int quantitySupplied = dr["quantitySupplied"] != null ? int.Parse(dr["quantitySupplied"].ToString()) : 0;
                                BackOrderItem boi = new BackOrderItem
                                {
                                    BackOrderFK = boId,
                                    ItemReference = axisData.Rows[0]["StockRef"].ToString(),
                                    SupplierItemReference = dr["supplierItemReference"].ToString(),
                                    Description = axisData.Rows[0]["Description"].ToString(),
                                    QuantityOrdered = quantityOrdered,
                                    QuantitySupplied = quantitySupplied,
                                    StockReplenishmentDate = StandardFunctions.ConvertStringToNullableDate(dr["stockReplenishmentDate"].ToString()),
                                    CostPrice = Convert.ToDouble(axisData.Rows[0]["CostPrice"]),
                                    SellPrice = Convert.ToDouble(axisData.Rows[0]["SellPrice"]),
                                    StatusFK = BackOrderStatusList.FirstOrDefault(x => x.LookupName == "Open").LookupId
                                };
                                CheckBackOrderItemExists(boi);
                                SaveBackOrderItem(boi);
                            }
                        }
                        else
                        {
                            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Unable to retrieve data for Purchase Order Number: " + dr["purchaseOrderNumber"].ToString() + ", Supplier Item Reference: " + dr["supplierItemReference"].ToString(), ErrorCode = "ERROR" });
                        }
                    }
                }
                catch (Exception ex)
                {
                    StandardFunctions.WriteException(ex);
                }
                stnFunc.ArchiveFile(WorkingPath, ArchivePath + DateTime.Now.ToString("dd_MM_yyyy_H_mm_ss") + "_" + p.providerID);
            }
            stnFunc.CleanupArchiveLocation(ArchivePath);
            stnFunc = null;

            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
        }

        public void SetStatus()
        {
            if (SubType == "maintain")
            {
                try
                {
                    SQLUtilities.ExecuteSimpleStoredProcedure("netgiantBatchProcesses", "ngmd.MaintainBackOrders", 30);
                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Successfully executed stored procedure to maintain Back Orders" });
                }
                catch (Exception ex)
                {
                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR Unable to maintain Back Orders. Maintain Back Orders failed", ErrorCode = "ERROR" });
                    StandardFunctions.WriteException(ex);
                }
            }
        }

        private int SaveBackOrder(BackOrder backOrder)
        {
            BackOrder bo = EntityFunctions.GetBackOrder(x => x.PurchaseOrderNumber == backOrder.PurchaseOrderNumber).FirstOrDefault();
            if (bo == null)
            {
                if (!EntityFunctions.SaveBackOrder(backOrder))
                {
                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Unable to save Back Order record for PO Number: " + backOrder.PurchaseOrderNumber, ErrorCode = "ERROR" });
                    return 0;
                }
            }
            else
            {
                // Don't update anything if it already exists
                return bo.BackOrderId;
            }
            return backOrder.BackOrderId;
        }

        private bool SaveBackOrderItem(BackOrderItem backOrderItem)
        {
            bool isSuccess = true;
            if (!EntityFunctions.SaveBackOrderItem(backOrderItem))
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Unable to save Back Order Item record for Back Order ID: " + backOrderItem.BackOrderFK.ToString() + ", Stock Reference: " + backOrderItem.ItemReference, ErrorCode = "ERROR" });
                isSuccess = false;
            }
            return isSuccess;
        }

        private void CheckBackOrderItemExists(BackOrderItem backOrderItem)
        {
            BackOrderItem boi = EntityFunctions.GetBackOrderItem(x => x.BackOrderFK == backOrderItem.BackOrderFK && x.ItemReference == backOrderItem.ItemReference).FirstOrDefault();
            if (boi != null)
            {
                backOrderItem.BackOrderItemId = boi.BackOrderItemId;
            }
        }
    }
}
