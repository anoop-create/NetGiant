using System;
using ngBatchProcesses.BusinessObjects.Shared;
using NGBP.DataAccessLayer.DataUtilities;
using System.Collections.Generic;
using System.Data;
using netGiant.Intranet.DataLayer.NetgiantMasterData;

namespace ngBatchProcesses.BusinessObjects.DataFeeds
{
    public class SalesHistoryFeed
    {
        public SalesHistoryFeed(Dictionary<string, string> parms)
        {
            _params = parms;
        }

        const string connName = "axisdiplomat";
        private readonly Dictionary<string, string> _params;

        public void Generate()
        {
            StandardFunctions.WriteProcessStarted();
            DataTable results = GetSalesHistoryData(_params["subtype"], _params["input"]);

            if (results.Rows.Count > 0)
            {
                CreateFeedFile(results);
            }

            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
        }

        private DataTable GetSalesHistoryData(string timespan, string period)
        {
            var parameters = new List<KeyValuePair<string, string>>();
            parameters.Add(new KeyValuePair<string, string>("timespan", timespan));
            parameters.Add(new KeyValuePair<string, string>("period", period));

            DataTable dt = new DataTable();

            try
            {
                dt = SQLUtilities.ExecuteStoredProcedureQuery(connName, "ng_SalesHistory", parameters);
            }
            catch (Exception ex)
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR Executing stored procedure ngmd.ng_GetProductIds", ErrorCode = "ERROR" });
                StandardFunctions.WriteException(ex);
            }

            return dt;
        }

        private void CreateFeedFile(DataTable data)
        {
            var writer = new CsvFileWriter(_params["output"], ',');

            var firstRow = new CsvRow
            {
                "Date",
                "AverageCostPrice",
                "Quantity",
                "AverageSellPrice",
                "StockReference",
                "PartNo",
                "TgWebsiteInventoryId",
                "CmWebsiteInventoryId",
                "NgWebsiteInventoryId",
                "Period"
            };

            writer.WriteRow(firstRow);

            foreach (DataRow row in data.Rows)
            {
                var newRow = new CsvRow();
                foreach (DataColumn column in data.Columns)
                {
                    newRow.Add(row[column.ColumnName].ToString());
                }
                writer.WriteRow(newRow);
            }

            writer.Close();
            data.Dispose();
        }
    }
}
