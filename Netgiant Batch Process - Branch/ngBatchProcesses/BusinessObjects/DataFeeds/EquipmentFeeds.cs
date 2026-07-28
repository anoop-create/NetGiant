using System;
using System.Collections.Generic;
using System.Data;
using NGBP.DataAccessLayer.DataUtilities;
using ngBatchProcesses.BusinessObjects.Shared;

namespace ngBatchProcesses.BusinessObjects.DataFeeds
{
    class EquipmentFeeds
    {
        public static void ProcessFeed(Dictionary<string, string> parms)
        {
            StandardFunctions stnFunc = new StandardFunctions();
            stnFunc.AddToActivityLog(parms["type"] + " Process Started");
            Properties.Settings settings = Properties.Settings.Default;
            bool errorHasOccurred = false;

            //Execute Stored Procedure to return the equipment result in a datatable
            DataTable results;

            try
            {
                results = SQLUtilities.ExecuteStoredProcedureQuery("tonergiant", "ng_GenerateEquipmentURLs");
            }
            catch (Exception ex)
            {
                stnFunc.AddToActivityLog("**ERROR** Executing stored procedure ng_GenerateEquipmentURLs");
                stnFunc.ProcessException(ex);
                stnFunc.LogActivity();
                return;
            }

            //Setup the CSV file and the delim to use, 
            char outputDelim = ',';

            using (CsvFileWriter writer = new CsvFileWriter(parms["output"], outputDelim))
            {
                try
                {
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
                }
                catch (Exception ex)
                {
                    stnFunc.AddToActivityLog("**Error** Writing CSV File: " + parms["output"] + ": " + ex.Message);
                    stnFunc.AddToActivityLog("Stack Trace: " + ex.StackTrace);
                    errorHasOccurred = true;
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
