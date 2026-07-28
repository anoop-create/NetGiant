using System;
using System.Collections.Generic;
using System.Data;
using NGBP.DataAccessLayer.DataUtilities;
using ngBatchProcesses.BusinessObjects.Shared;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System.Linq;

namespace ngBatchProcesses.BusinessObjects.DataFeeds
{
    public class SkuuudleFeed
    {
        /// <summary>
        /// Sends products to Skuuudle
        /// </summary>
        /// <param name="parms"></param>
        public static void ProcessFeed(Dictionary<string, string> parms)
        {
            StandardFunctions.WriteProcessStarted();
            Properties.Settings settings = Properties.Settings.Default;
            bool errorHasOccurred = false;
            
            int websiteId = Int32.Parse(parms["subtype"]);

            char outputDelim = ',';

            string connName = "netgiantMasterData";
            var sqlParams = new List<KeyValuePair<string, string>>();
            sqlParams.Add(new KeyValuePair<string, string>("websiteID", websiteId.ToString()));
            DataTable eqResults;
            try
            {
                eqResults = SQLUtilities.ExecuteStoredProcedureQuery(connName, "ngmd.GetSkuuudleFeed", sqlParams);
            }
            catch (Exception ex)
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR Executing stored procedure ngmd.GetSkuuudleFeed", ErrorCode = "ERROR" });
                StandardFunctions.WriteException(ex);
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
                return;
            }

            //Write the CSV file
            using (CsvFileWriter writer = new CsvFileWriter(parms["output"], outputDelim))
            {
                try
                {
                    //Write the headings, the first row in the CSV
                    CsvRow firstRow = new CsvRow();

                    //Write details
                    foreach (DataColumn dc in eqResults.Columns)
                    {
                        firstRow.Add(dc.ColumnName);
                    }
                    writer.WriteRow(firstRow);

                    //Loop through the datatable and write a row in the csv file for each
                    foreach (DataRow dr in eqResults.Rows)
                    {
                        CsvRow newRow = new CsvRow();
                        foreach (DataColumn dc in eqResults.Columns)
                        {
                            newRow.Add(dr[dc.ColumnName].ToString());
                        }
                        writer.WriteRow(newRow);
                    }
                }
                catch (Exception ex)
                {
                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR Writing Skuuudle Feed CSV File: " + parms["output"] + ": " + ex.Message, ErrorCode = "ERROR" });
                    StandardFunctions.WriteException(ex);
                    errorHasOccurred = true;
                }
            }
            if (!errorHasOccurred)
            {
                //FTP output file
                if (parms.ContainsKey("ftpsite"))
                {
                    string[] fileParts = parms["output"].Split('\\');
                    var finalFileName = fileParts[fileParts.Length - 1];

                    Tuple<bool, string> rtn = FtpUtilities.UploadFTPFile(parms["output"],
                            parms["ftpsite"],
                            parms["ftpusername"],
                            parms["ftppassword"],
                            parms["ftppath"] + finalFileName,
                            true);
                    if (rtn.Item1)
                    {
                        StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "FTP Successful" });
                    }
                    else
                    {
                        StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR Attempting to FTP File: " + parms["output"], ErrorCode = "ERROR" });
                    }
                }
            }

            //Log in activity log
            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
        }
    }
}
