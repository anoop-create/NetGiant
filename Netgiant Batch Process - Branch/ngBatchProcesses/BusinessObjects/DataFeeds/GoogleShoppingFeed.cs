using System;
using System.Collections.Generic;
using System.Data;
using NGBP.DataAccessLayer.DataUtilities;
using ngBatchProcesses.BusinessObjects.Shared;
using System.Data.SqlClient;

namespace ngBatchProcesses.BusinessObjects.DataFeeds
{
    class GoogleShoppingFeed
    {
        public static void ProcessFeed(Dictionary<string, string> parms)
        {
            StandardFunctions stnFunc = new StandardFunctions();
            stnFunc.AddToActivityLog(parms["type"] + " " + parms["subtype"] + " Process Started");
            Properties.Settings settings = Properties.Settings.Default;
            bool errorHasOccurred = false;

            string connName = "";
            connName = "netgiantMasterData";

            //Set Up
            // subtype is set to 1,2,3 (for Google Feed) or 11,12,13 (for Kenshoo Feed)
            int subtype = Int32.Parse(parms["subtype"]);
            int websiteId;
            bool feedForKenshoo;
            if (subtype > 10)
            {
                websiteId = subtype - 10;
                feedForKenshoo = true;
            }
            else
            {
                websiteId = subtype;
                feedForKenshoo = false;
            }

            string fileExtension = "";
            char outputDelim = new char();
            outputDelim = '\t';

            // Equipment Feed File
            if (!feedForKenshoo)
            {
                fileExtension = "-Equip.txt";
                var sqlParams = new List<KeyValuePair<string, string>>();
                sqlParams.Add(new KeyValuePair<string, string>("websiteID", websiteId.ToString()));

                //Execute Stored Procedure to return the EQUIPMENT result in a datatable
                DataTable eqResults;
                try
                {
                    eqResults = SQLUtilities.ExecuteStoredProcedureQuery(connName, "ngmd.GetGoogleFeed_Equip",
                        sqlParams);
                }
                catch (Exception ex)
                {
                    stnFunc.AddToActivityLog("**ERROR** Executing stored procedure ngmd.GetGoogleFeed_Equip");
                    stnFunc.ProcessException(ex);
                    stnFunc.LogActivity();
                    return;
                }

                //Write the CSV file
                using (CsvFileWriter writer = new CsvFileWriter(parms["output"] + fileExtension, outputDelim))
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
                            //Exclude records with blank images
                            if (dr["image_link"].ToString() != "")
                            {
                                CsvRow newRow = new CsvRow();
                                foreach (DataColumn dc in eqResults.Columns)
                                {
                                    newRow.Add(dr[dc.ColumnName].ToString());
                                }
                                writer.WriteRow(newRow);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        stnFunc.AddToActivityLog("**Error** Writing Equipment CSV File: " + parms["output"] + fileExtension + ": " + ex.Message);
                        stnFunc.AddToActivityLog("Stack Trace: " + ex.StackTrace);
                        errorHasOccurred = true;
                    }
                }

                //FTP output file
                if (parms.ContainsKey("ftpsite"))
                {
                    string finalFileName = "";
                    string[] fileParts = parms["output"].Split('\\');
                    finalFileName = fileParts[fileParts.Length - 1] + fileExtension;

                    try
                    {
                        FtpUtilities.UploadFTPFile(parms["output"] + fileExtension,
                                        parms["ftpsite"] + "/" + parms["ftppath"] + finalFileName,
                                        parms["ftpusername"],
                                        parms["ftppassword"]);
                    }
                    catch (Exception ex)
                    {
                        stnFunc.AddToActivityLog("**Error** Attempting to FTP File: " + parms["output"] + fileExtension + ": " + ex.Message);
                        stnFunc.AddToActivityLog("Stack Trace: " + ex.StackTrace);
                        errorHasOccurred = true;
                    }

                }
            }


            // Product Feed File
            if (!feedForKenshoo)
            {
                fileExtension = "-Product.txt";
            }
            else
            {
                outputDelim = ',';
            }

            List<SqlParameter> sqlParms = new List<SqlParameter>();
            SqlParameter sqlParm = new SqlParameter("@WebsiteId", SqlDbType.Int);
            sqlParm.Value = websiteId;
            sqlParms.Add(sqlParm);
            sqlParm = new SqlParameter("@feedForKenshoo", SqlDbType.Bit);
            sqlParm.Value = feedForKenshoo;
            sqlParms.Add(sqlParm);
            DataTable prResults;
            try
            {
                prResults = SQLUtilities.ExecuteReadStoredProcedure("netgiantmasterdata", "ngmd.GetGoogleFeed_Product",
                    sqlParms, "googledata").Tables[0];
            }
            catch (Exception ex)
            {
                stnFunc.AddToActivityLog("**ERROR** Executing stored procedure ngmd.GetGoogleFeed_Product");
                stnFunc.ProcessException(ex);
                stnFunc.LogActivity();
                return;
            }

            //Write the CSV file
            using (CsvFileWriter writer = new CsvFileWriter(parms["output"] + fileExtension, outputDelim))
            {
                try
                {
                    //Write the headings, the first row in the CSV
                    CsvRow firstRow = new CsvRow();

                    //Write details
                    foreach (DataColumn dc in prResults.Columns)
                    {
                        firstRow.Add(dc.ColumnName);
                    }
                    writer.WriteRow(firstRow);

                    //Loop through the datatable and write a row in the csv file for each
                    foreach (DataRow dr in prResults.Rows)
                    {
                        //Exclude records with blank images
                        if (dr["image_link"].ToString() != "")
                        {
                            CsvRow newRow = new CsvRow();
                            foreach (DataColumn dc in prResults.Columns)
                            {
                                newRow.Add(dr[dc.ColumnName].ToString());
                            }
                            writer.WriteRow(newRow);
                        }
                    }
                }
                catch (Exception ex)
                {
                    stnFunc.AddToActivityLog("**Error** Writing CSV File: " + parms["output"] + fileExtension + ": " + ex.Message);
                    stnFunc.AddToActivityLog("Stack Trace: " + ex.StackTrace);
                    errorHasOccurred = true;
                }
            }

            //FTP output file
            if (parms.ContainsKey("ftpsite"))
            {
                string finalFileName = "";
                string[] fileParts = parms["output"].Split('\\');
                finalFileName = fileParts[fileParts.Length - 1] + fileExtension;

                try
                {
                    FtpUtilities.UploadFTPFile(parms["output"] + fileExtension,
                                    parms["ftpsite"] + "/" + parms["ftppath"] + finalFileName,
                                    parms["ftpusername"],
                                    parms["ftppassword"]);
                }
                catch (Exception ex)
                {
                    stnFunc.AddToActivityLog("**Error** Attempting to FTP File: " + parms["output"] + fileExtension + ": " + ex.Message);
                    stnFunc.AddToActivityLog("Stack Trace: " + ex.StackTrace);
                    errorHasOccurred = true;
                }

            }


            //Log in activity log
            stnFunc.AddToActivityLog(parms["type"] + " " + parms["subtype"] + " Process Finished");
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
