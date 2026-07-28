using System;
using ngBatchProcesses.BusinessObjects.Shared;
using NGBP.DataAccessLayer.DataUtilities;
using System.Collections.Generic;
using System.Data;

namespace ngBatchProcesses.BusinessObjects.DataFeeds
{
    public class OrderFeed
    {
        public OrderFeed(Dictionary<string, string> parms)
        {
            suppliedParms = parms;
            stnFunc = new StandardFunctions();
        }

        Dictionary<string, string> suppliedParms;
        StandardFunctions stnFunc;
        const string connName = "axisdiplomat";
        string csvFilename = "";

        public void Generate()
        {
            DoOrders();
            DoOrderLines();
        }

        private void DoOrders()
        {
            stnFunc.AddToActivityLog(suppliedParms["type"] + " Orders - Started");
            csvFilename = suppliedParms["output"] + "orderHistory_orderTable.csv";
            DataTable results;
            try
            {
                results = SQLUtilities.ExecuteStoredProcedureQuery(connName, "ng_GetOrders");
            }
            catch (Exception ex)
            {
                stnFunc.AddToActivityLog("**ERROR** Executing stored procedure ng_GetOrders");
                stnFunc.ProcessException(ex);
                stnFunc.LogActivity();
                return;
            }

            WriteCSV(results);
            LogActivity(stnFunc);
        }

        private void DoOrderLines()
        {
            stnFunc.AddToActivityLog(suppliedParms["type"] + " Order Lines - Started");
            csvFilename = $"{suppliedParms["output"]}orderHistory_orderLines.csv";
            DataTable results;
            try
            {
                results = SQLUtilities.ExecuteStoredProcedureQuery(connName, "ng_GetOrderLines");
            }
            catch (Exception ex)
            {
                stnFunc.AddToActivityLog("**ERROR** Executing stored procedure ng_GetOrderLines");
                stnFunc.ProcessException(ex);
                stnFunc.LogActivity();
                return;
            }

            WriteCSV(results);
            LogActivity(stnFunc);
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
                stnFunc.AddToActivityLog("**ERROR** Executing OrderFeed.WriteCSV - filename: " + csvFilename);
                stnFunc.ProcessException(ex);
            }
        }

        private void LogActivity(StandardFunctions stnFunc)
        {
            stnFunc.AddToActivityLog(suppliedParms["type"] + " Process Finished");
            stnFunc.LogActivity(suppliedParms["type"]);
        }
    }
}
