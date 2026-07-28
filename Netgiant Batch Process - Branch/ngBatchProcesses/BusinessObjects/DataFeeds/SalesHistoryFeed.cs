using System;
using ngBatchProcesses.BusinessObjects.Shared;
using NGBP.DataAccessLayer.DataUtilities;
using System.Collections.Generic;
using System.Data;

namespace ngBatchProcesses.BusinessObjects.DataFeeds
{
    public class SalesHistoryFeed
    {
        public SalesHistoryFeed(Dictionary<string, string> parms)
        {
            _params = parms;
            stnFunc = new StandardFunctions();
        }

        const string connName = "axisdiplomat";
        private readonly Dictionary<string, string> _params;
        private readonly StandardFunctions stnFunc;

        public void Generate()
        {
            stnFunc.AddToActivityLog($"{ _params["type"]} Started");
            DataTable results = GetSalesHistoryData(_params["subtype"], _params["input"]);

            if (results.Rows.Count > 0)
            {
                CreateFeedFile(results);
            }

            stnFunc.AddToActivityLog($"{ _params["type"]} Completed");
            stnFunc.LogActivity(_params["type"]);
        }

        private DataTable GetSalesHistoryData(string timespan, string period)
        {
            var parameters = new List<KeyValuePair<string, string>>();
            parameters.Add(new KeyValuePair<string, string>("timespan", timespan));
            parameters.Add(new KeyValuePair<string, string>("period", period));

            DataTable dt = new DataTable();

            try
            {
                dt = SQLUtilities.ExecuteStoredProcedureQuery(connName, ".ng_SalesHistory", parameters);
            }
            catch (Exception ex)
            {
                stnFunc.AddToActivityLog("**ERROR** Executing stored procedure ng_GetProductIds");
                stnFunc.ProcessException(ex);
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
