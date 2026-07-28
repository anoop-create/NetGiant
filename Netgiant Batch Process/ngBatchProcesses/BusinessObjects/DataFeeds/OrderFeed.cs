using System;
using ngBatchProcesses.BusinessObjects.Shared;
using NGBP.DataAccessLayer.DataUtilities;
using System.Collections.Generic;
using System.Data;
using netGiant.Intranet.DataLayer.NetgiantMasterData;

namespace ngBatchProcesses.BusinessObjects.DataFeeds
{
    public class OrderFeed
    {
        public OrderFeed(Dictionary<string, string> parms)
        {
            suppliedParms = parms;
        }

        Dictionary<string, string> suppliedParms;
        const string connName = "axisdiplomat";
        string csvFilename = "";

        public void Generate()
        {
            StandardFunctions.WriteProcessStarted();
            DoOrders();
            DoOrderLines();
            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
        }

        private void DoOrders()
        {
            csvFilename = suppliedParms["output"] + "orderHistory_orderTable.csv";
            DataTable results;
            try
            {
                results = SQLUtilities.ExecuteStoredProcedureQuery(connName, "ng_GetOrders");
            }
            catch (Exception ex)
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR Executing stored procedure ng_GetOrders", ErrorCode = "ERROR" });
                StandardFunctions.WriteException(ex);
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
                return;
            }

            WriteCSV(results);
        }

        private void DoOrderLines()
        {
            csvFilename = $"{suppliedParms["output"]}orderHistory_orderLines.csv";
            DataTable results;
            try
            {
                results = SQLUtilities.ExecuteStoredProcedureQuery(connName, "ng_GetOrderLines");
            }
            catch (Exception ex)
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR Executing stored procedure ng_GetOrderLines", ErrorCode = "ERROR" });
                StandardFunctions.WriteException(ex);
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
                return;
            }

            WriteCSV(results);
        }

        private void WriteCSV(DataTable results)
        {
            try
            {
                using (CsvFileWriter writer = new CsvFileWriter(csvFilename, ','))
                {
                    foreach (DataRow dr in results.Rows)
                    {
                        CsvRow newRow = new CsvRow();
                        foreach (DataColumn dc in results.Columns)
                        {
                            newRow.Add(dr[dc.ColumnName].ToString());
                        }

                        writer.WriteRow(newRow);
                    }
                }
            }
            catch (Exception ex)
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR Executing OrderFeed.WriteCSV - filename: " + csvFilename, ErrorCode = "ERROR" });
                StandardFunctions.WriteException(ex);
            }
        }
    }
}
