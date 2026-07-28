using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.IO;
using NGSBP.DataAccessLayer.DataUtilities;
using ngSBSBatchProcesses.BusinessObjects.Shared;

namespace ngSBSBatchProcesses.BusinessObjects.DataFeeds
{
    class FeeFoFeed
    {
        public static void ProcessFeeFo(Dictionary<string, string> parms)
        {
            StandardFunctions stnFunc = new StandardFunctions();
            stnFunc.AddToActivityLog(parms["type"] + " " + parms["subtype"] + " Started");

            string connName = "axisdiplomat";

            //Execute Stored Procedure to return the equipment result in a datatable
            var sqlParams = new List<KeyValuePair<string, string>>();
            sqlParams.Add(new KeyValuePair<string, string>("website", parms["subtype"].ToUpper()));
            DataTable results = SQLUtilities.ExecuteStoredProcedureQuery(connName, "ng_GetFeeFoData", sqlParams);

            //Setup the CSV file and the delim to use, 
            char outputDelim = new char();
            outputDelim = '\t';
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
                finalFileName = fileParts[fileParts.Length - 1]; //Kenshoo final Path Part
                FtpUtilities.UploadFTPFile(parms["output"],
                    parms["ftpsite"] + "/" + parms["ftppath"] + finalFileName,
                    parms["ftpusername"],
                    parms["ftppassword"]);
            }

            //Log in activity log
            stnFunc.AddToActivityLog(parms["type"] + " Process Finished");
            string activityLogFileName = stnFunc.LogActivity();
            stnFunc = null;
        }
    }
}
