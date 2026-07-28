using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ngSBSBatchProcesses.BusinessObjects.Shared;
using NGSBP.DataAccessLayer.DataUtilities;

namespace ngSBSBatchProcesses.BusinessObjects.DataFeeds
{
    public class FreshRelevanceFeeds
    {
        public void GenerateFeeds(Dictionary<string, string> parms)
        {
            var stnFunc = new StandardFunctions();
            stnFunc.AddToActivityLog(parms["type"] + " " + " Process Started");
            Properties.Settings settings = Properties.Settings.Default;
            WriteAndUploadFile(parms, stnFunc, "person_import.txt", "ng_FreshRelevancePersonal");
            WriteAndUploadFile(parms, stnFunc, "transaction_import_" + DateTime.Today.ToString("yyyyMMdd") + ".txt", "ng_FreshRelevanceTrans");

            //Log in activity log
            stnFunc.AddToActivityLog(parms["type"] + " " + " Process Finished");
            stnFunc.LogActivity();
        }
        
        private static void WriteAndUploadFile(Dictionary<string, string> parms, StandardFunctions stnFunc, string fileExtension, string spName)
        {
            var outputDelim = '\t';
            var results = SQLUtilities.ExecuteStoredProcedureQuery("axisdiplomat", spName);

            //Write the Text file
            using (var writer = new CsvFileWriter(parms["output"] + "\\" + fileExtension, outputDelim))
            {
                try
                {
                    //Write the headings, the first row in the CSV
                    var firstRow = new CsvRow();

                    //Write details
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
                catch (Exception e)
                {
                    stnFunc.AddToActivityLog("**Error** Writing Equipment CSV File: " + parms["output"] + ": " +
                                             e.Message);
                    stnFunc.AddToActivityLog("Stack Trace: " + e.StackTrace);
                }
            }

            //FTP output file
            if (parms.ContainsKey("ftpsite"))
            {
                string finalFileName = "";
                finalFileName = fileExtension;

                try
                {
                    FtpUtilities.UploadFTPFile(parms["output"] + "\\" + fileExtension,
                        parms["ftpsite"] + "/" + parms["ftppath"] + finalFileName,
                        parms["ftpusername"],
                        parms["ftppassword"],
                        true);
                }
                catch (Exception e)
                {
                    stnFunc.AddToActivityLog("**Error** Attempting to FTP File: " + parms["output"] + fileExtension + ": " + e.Message);
                    stnFunc.AddToActivityLog("Stack Trace: " + e.StackTrace);
                }
            }
        }
    }
}
