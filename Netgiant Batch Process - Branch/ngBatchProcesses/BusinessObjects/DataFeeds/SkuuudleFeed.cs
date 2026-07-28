using System;
using System.Collections.Generic;
using System.Data;
using NGBP.DataAccessLayer.DataUtilities;
using ngBatchProcesses.BusinessObjects.Shared;

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
            StandardFunctions stnFunc = new StandardFunctions();
            stnFunc.AddToActivityLog(parms["type"] + " " + parms["subtype"] + " Process Started");
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
                stnFunc.AddToActivityLog("**ERROR** Executing stored procedure ngmd.GetSkuuudleFeed");
                stnFunc.ProcessException(ex);
                stnFunc.LogActivity();
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
                    stnFunc.AddToActivityLog("**Error** Writing Skuuudle Feed CSV File: " + parms["output"] + ": " + ex.Message);
                    stnFunc.AddToActivityLog("Stack Trace: " + ex.StackTrace);
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

                    try
                    {
                        FtpUtilities.UploadFTPFile(parms["output"],
                            parms["ftpsite"] + "/" + parms["ftppath"] + finalFileName,
                            parms["ftpusername"],
                            parms["ftppassword"]);
                    }
                    catch (Exception ex)
                    {
                        stnFunc.AddToActivityLog("**Error** Attempting to FTP File: " + parms["output"] + ": " + ex.Message);
                        stnFunc.AddToActivityLog("Stack Trace: " + ex.StackTrace);
                    }
                }
            }

            //Log in activity log
            stnFunc.AddToActivityLog(parms["type"] + " Process Finished");
            string activityLogFileName = stnFunc.LogActivity(parms["type"]);

            if (errorHasOccurred && settings.Environment == "Live")
            {
                List<string> additionalEmails = new List<string>();
                additionalEmails.Add("Daniel.whittaker@netgiant.com");
                additionalEmails.Add("stuart.deavall@netgiant.com");
                stnFunc.SendSimpleEmail(parms["type"], activityLogFileName, additionalEmails);
            }
            stnFunc = null;
        }
    }
}
