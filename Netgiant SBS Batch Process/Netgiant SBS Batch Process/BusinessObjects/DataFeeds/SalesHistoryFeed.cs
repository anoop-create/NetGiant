using NGSBP.DataAccessLayer.DataUtilities;
using ngSBSBatchProcesses.BusinessObjects.Shared;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ngSBSBatchProcesses.BusinessObjects.DataFeeds
{
    public class SalesHistoryFeed
    {
        public SalesHistoryFeed(Dictionary<string, string> parms)
        {
            suppliedParms = parms;
            stnFunc = new StandardFunctions();
        }

        const string connName = "axisdiplomat";
        private Dictionary<string, string> suppliedParms;
        private StandardFunctions stnFunc;

        public void Generate(string timespan, string period)
        {
            stnFunc.AddToActivityLog(suppliedParms["type"] + " " + " Started");
            var results = GetSalesHistoryData(timespan, period);
            CreateFeedFile(results);
            FtpOutputFile();
        }

        private DataTable GetSalesHistoryData(string timespan, string period)
        {
            var parameters = new List<KeyValuePair<string, string>>();
            parameters.Add(new KeyValuePair<string, string>("timespan", timespan));
            parameters.Add(new KeyValuePair<string, string>("period", period));

            return SQLUtilities.ExecuteStoredProcedureQuery(connName, ".ng_SalesHistory", parameters);
        }

        private void CreateFeedFile(DataTable data)
        {
            var outputDelim = new char();
            outputDelim = ',';
            var writer = new CsvFileWriter(suppliedParms["output"], outputDelim);

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

        private void FtpOutputFile()
        {
            if (suppliedParms.ContainsKey("ftpsite"))
            {
                FtpUtilities.UploadFTPFile(suppliedParms["output"],
                    suppliedParms["ftpsite"] + "/" + suppliedParms["ftppath"] + Path.GetFileName(suppliedParms["output"]),
                    suppliedParms["ftpusername"],
                    suppliedParms["ftppassword"]);
            }
        }
    }
}
