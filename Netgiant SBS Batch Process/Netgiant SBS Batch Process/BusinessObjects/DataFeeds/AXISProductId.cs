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
    class AXISProductId
    {
        public static void WriteProductIds(Dictionary<string, string> parms)
        {
            StandardFunctions stnFunc = new StandardFunctions();
            stnFunc.AddToActivityLog(parms["type"] + " " + " Started");

            string connName = "axisdiplomat";

            //Execute Stored Procedure to return the equipment result in a datatable
            DataTable results = SQLUtilities.ExecuteStoredProcedureQuery(connName, "ng_GetProductIds");

            //Setup the CSV file and the delim to use, 
            char outputDelim = new char();
            outputDelim = ',';
            CsvFileWriter writer = new CsvFileWriter(parms["output"], outputDelim);

            //Write the headings, the first row in the CSV
            CsvRow firstRow = new CsvRow();
            foreach (DataColumn dc in results.Columns)
            {
                firstRow.Add(dc.ColumnName);
            }
            writer.WriteRow(firstRow);

            //Loop through the datatable and write a row in the csv file for each
            foreach (DataRow dr in results.Rows)
            {
                CsvRow newRow = new CsvRow();
                foreach (DataColumn dc in results.Columns)
                {
                    newRow.Add(dr[dc.ColumnName].ToString());
                }
                writer.WriteRow(newRow);
            }

            //Close the writer and dispose of the datatable containing the results
            writer.Close();
            results.Dispose();

            ///FTP output file
            if (parms.ContainsKey("ftpsite"))
            {
                var settings = Properties.Settings.Default;
                string finalFileName = "";
                string[] fileParts = parms["output"].Split('\\');
                finalFileName = fileParts[fileParts.Length - 1];
                FtpUtilities.UploadFTPFile(parms["output"],
                    parms["ftpsite"] + "/" + parms["ftppath"] + finalFileName,
                    parms["ftpusername"],
                    parms["ftppassword"],
                    false);
            }

            //Log in activity log
            stnFunc.AddToActivityLog(parms["type"] + " Process Finished");
            string activityLogFileName = stnFunc.LogActivity();
            stnFunc = null;
        }
    }
}
