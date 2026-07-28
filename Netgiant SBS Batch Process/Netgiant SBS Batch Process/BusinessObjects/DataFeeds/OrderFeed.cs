using NGSBP.DataAccessLayer.DataUtilities;
using ngSBSBatchProcesses.BusinessObjects.Shared;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ngSBSBatchProcesses.BusinessObjects.DataFeeds
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
            stnFunc.AddToActivityLog(suppliedParms["type"] + " " + " Started");
            DoOrders();
            DoOrderLines();
        }

        private void DoOrders()
        {
            csvFilename = suppliedParms["output"] + "orderHistory_orderTable.csv";
            DataTable results = SQLUtilities.ExecuteStoredProcedureQuery(connName, "ng_GetOrders");
            WriteCSV(results);
            FTPFile();
            LogActivity(stnFunc);
        }

        private void DoOrderLines()
        {
            csvFilename = suppliedParms["output"] + "orderHistory_orderLines.csv";
            DataTable results = SQLUtilities.ExecuteStoredProcedureQuery(connName, "ng_GetOrderLines");
            WriteCSV(results);
            FTPFile();
            LogActivity(stnFunc);
        }

        private void WriteCSV(DataTable results)
        {
            char outputDelim = new char();
            outputDelim = ',';
            CsvFileWriter writer = new CsvFileWriter(csvFilename, outputDelim);

            foreach (DataRow dr in results.Rows)
            {
                CsvRow newRow = new CsvRow();
                foreach (DataColumn dc in results.Columns)
                {
                    newRow.Add(dr[dc.ColumnName].ToString());
                }
                writer.WriteRow(newRow);
            }

            writer.Close();
            results.Dispose();
        }

        private void FTPFile()
        {
            if (suppliedParms.ContainsKey("ftpsite"))
            {
                var settings = Properties.Settings.Default;
                string finalFileName = "";
                string[] fileParts = csvFilename.Split('\\');
                finalFileName = fileParts[fileParts.Length - 1];
                FtpUtilities.UploadFTPFile(csvFilename,
                    suppliedParms["ftpsite"] + "/" + suppliedParms["ftppath"] + finalFileName,
                    suppliedParms["ftpusername"],
                    suppliedParms["ftppassword"]);
            }
        }

        private void LogActivity(StandardFunctions stnFunc)
        {
            stnFunc.AddToActivityLog(suppliedParms["type"] + " Process Finished");
            string activityLogFileName = stnFunc.LogActivity();
        }
    }
}
