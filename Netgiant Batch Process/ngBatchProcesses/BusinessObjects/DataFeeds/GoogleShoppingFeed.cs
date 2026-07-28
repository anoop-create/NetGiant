using System;
using System.Collections.Generic;
using System.Data;
using NGBP.DataAccessLayer.DataUtilities;
using ngBatchProcesses.BusinessObjects.Shared;
using System.Data.SqlClient;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System.Linq;

namespace ngBatchProcesses.BusinessObjects.DataFeeds
{
    class GoogleShoppingFeed
    {
        public static void ProcessFeed(Dictionary<string, string> parms)
        {
            StandardFunctions.WriteProcessStarted();
            Properties.Settings settings = Properties.Settings.Default;

            string connName = "";
            connName = "netgiantMasterData";

            //Set Up
            // subtype is set to 1,2,3 (for Google Feed) or 11,12,13 (for Kenshoo Feed)
            int subtype = Int32.Parse(parms["websiteid"]);
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
                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR Executing stored procedure ngmd.GetGoogleFeed_Equip", ErrorCode = "ERROR" });
                    StandardFunctions.WriteException(ex);
                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
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
                        StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR Writing Equipment CSV File: " + parms["output"] + fileExtension + ": " + ex.Message, ErrorCode = "ERROR" });
                        StandardFunctions.WriteException(ex);
                    }
                }

                //FTP output file
                if (parms.ContainsKey("ftpsite"))
                {
                    string finalFileName = parms["output"] + fileExtension;

                    try
                    {
                        FtpUtilities.UploadSFTPFiles(
                            parms["ftpsite"],
                            parms["ftpusername"],
                            parms["ftppassword"],
                            parms["ftppath"],
                            finalFileName,
                            Int32.Parse(parms["number"]));
                        StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "FTP Successful" });
                    }
                    catch (Exception ex)
                    {
                        StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR Attempting to FTP File: " + finalFileName + ": " + ex.Message, ErrorCode = "ERROR" });
                        StandardFunctions.WriteException(ex);
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
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR Executing stored procedure ngmd.GetGoogleFeed_Product", ErrorCode = "ERROR" });
                StandardFunctions.WriteException(ex);
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
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
                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR Writing CSV File: " + parms["output"] + fileExtension + ": " + ex.Message, ErrorCode = "ERROR" });
                    StandardFunctions.WriteException(ex);
                }
            }

            //FTP output file
            if (parms.ContainsKey("ftpsite"))
            {
                string finalFileName = parms["output"] + fileExtension;

                try
                {
                    FtpUtilities.UploadSFTPFiles(
                            parms["ftpsite"],
                            parms["ftpusername"],
                            parms["ftppassword"],
                            parms["ftppath"],
                            finalFileName, 
                            Int32.Parse(parms["number"]));
                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "FTP Successful" });
                }
                catch (Exception ex)
                {
                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR Attempting to FTP File: " + finalFileName + ": " + ex.Message, ErrorCode = "ERROR" });
                    StandardFunctions.WriteException(ex);
                }
            }

            //Log in activity log
            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
        }
    }
}
