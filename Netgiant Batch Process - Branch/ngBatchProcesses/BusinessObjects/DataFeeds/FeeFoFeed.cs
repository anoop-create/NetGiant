using Microsoft.VisualBasic;
using Microsoft.VisualBasic.FileIO;
using ngBatchProcesses.BusinessObjects.Shared;
using NGBP.DataAccessLayer.DataUtilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Policy;

namespace ngBatchProcesses.BusinessObjects.DataFeeds
{
    public static class FeeFoFeed
    {
        public static void LoadData(Dictionary<string, string> parms)
        {
            //Start Log
            StandardFunctions stnFunc = new StandardFunctions();
            stnFunc.AddToActivityLog(parms["type"] + " website:" + parms["websiteid"] + " Started");
            Properties.Settings settings = Properties.Settings.Default;
            bool errorHasOccurred = false;

            //Construct URL
            string merchantid = "";
            int months = int.Parse(parms["period"]) * -1;
            string feedbackFrom = DateTime.Now.AddMonths(months).ToString("yyyy-MM-dd");
            int websiteID = int.Parse(parms["websiteid"]);
            string apikey = StandardFunctions.GetConfigurationSetting("BatchProgram", "FeeFoAPIKey", websiteID);
            
            switch (websiteID)
            {
                case 1:
                    merchantid = "toner-giant";
                    break;
                case 2:
                    merchantid = "cartridge-monkey";
                    break;
                case 3:
                    merchantid = "netgiant-ltd";
                    break;
            }

            string url = "http://ww2.feefo.com/api/download-feedback?merchantidentifier=" + merchantid + "&apikey=" + apikey + "&updatedsince=" + feedbackFrom;

            //Retrieve data via API call
            int fileCount = 0;
            MemoryStream ms = new MemoryStream();
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            using (Stream s = request.GetResponse().GetResponseStream())
            {
                s.CopyTo(ms);
                ms.Position = 0;
            }
            using (TextFieldParser csvReader = new TextFieldParser(ms))
            {
                //Process data
                string uniqueRef = "";
                string orderRef = "";
                
                try
                {
                    csvReader.SetDelimiters(new string[] { ControlChars.Tab.ToString() });
                    csvReader.TrimWhiteSpace = true;

                    var firstLine = new string[] { };
                    firstLine = csvReader.ReadFields();

                    List<int> indexList = new List<int>();
                    int feedbackDateColumn = LookupFieldIndex(firstLine, "Feedback Date", indexList);
                    int partNoColumn = LookupFieldIndex(firstLine, "Product Ref", indexList);
                    int orderRefColumn = LookupFieldIndex(firstLine, "Order Ref", indexList);
                    int productRatingColumn = LookupFieldIndex(firstLine, "Product Feedback", indexList);
                    int productCommentColumn = LookupFieldIndex(firstLine, "Product Comment", indexList);
                    int vendorReplyColumn = LookupFieldIndex(firstLine, "Product Vendor Reply", indexList);

                    if (CheckValidColumns(indexList))
                    {
                        while (!csvReader.EndOfData)
                        {
                            string[] rowData;
                            try
                            {
                                rowData = csvReader.ReadFields();
                            }
                            catch (Exception ex)
                            {
                                stnFunc.AddToActivityLog("**Error** Unable to read line, previous OrderRef: " + orderRef + ", " + ex.Message);
                                continue;
                            }
                            if (rowData.Length < indexList.Max())
                            {
                                stnFunc.AddToActivityLog("**Error** Unable to read line, previous OrderRef: " + orderRef);
                                continue;
                            }
                            var feedbackDate = GetRowFieldData(rowData, feedbackDateColumn);
                            var partNo = GetRowFieldData(rowData, partNoColumn);
                            orderRef = GetRowFieldData(rowData, orderRefColumn);
                            uniqueRef = orderRef + "-" + partNo;
                            var productComment = GetRowFieldData(rowData, productCommentColumn);
                            var vendorReply = GetRowFieldData(rowData, vendorReplyColumn);

                            int productRating = 3;
                            var temp = GetRowFieldData(rowData, productRatingColumn);
                            //if (orderRef == "T3MBO")
                            //{
                            //    bool stophere = true;
                            //}
                            switch (GetRowFieldData(rowData, productRatingColumn))
                            {
                                case "--":
                                    productRating = 1;
                                    break;
                                case "-":
                                    productRating = 2;
                                    break;
                                case "+":
                                    productRating = 4;
                                    break;
                                case "++":
                                    productRating = 5;
                                    break;
                                case "nyt":
                                    continue;
                                case "":
                                    continue;
                                default:
                                    productRating = 3;
                                    break;
                            }

                            List<SqlParameter> sqlParms = new List<SqlParameter>();
                            SqlParameter sqlParm1 = new SqlParameter("@FeedbackDate", SqlDbType.DateTime);
                            sqlParm1.Value = feedbackDate;
                            sqlParms.Add(sqlParm1);
                            SqlParameter sqlParm2 = new SqlParameter("@WebsiteFK", SqlDbType.Int);
                            sqlParm2.Value = websiteID;
                            sqlParms.Add(sqlParm2);
                            SqlParameter sqlParm3 = new SqlParameter("@PartNo", SqlDbType.VarChar);
                            sqlParm3.Value = partNo;
                            sqlParms.Add(sqlParm3);
                            SqlParameter sqlParm4 = new SqlParameter("@UniqueRef", SqlDbType.VarChar);
                            sqlParm4.Value = uniqueRef;
                            sqlParms.Add(sqlParm4);
                            SqlParameter sqlParm5 = new SqlParameter("@ProductRating", SqlDbType.Int);
                            sqlParm5.Value = productRating;
                            sqlParms.Add(sqlParm5);
                            SqlParameter sqlParm6 = new SqlParameter("@ProductComment", SqlDbType.VarChar);
                            sqlParm6.Value = productComment;
                            sqlParms.Add(sqlParm6);
                            SqlParameter sqlParm7 = new SqlParameter("@VendorReply", SqlDbType.VarChar);
                            sqlParm7.Value = vendorReply;
                            sqlParms.Add(sqlParm7);

                            try
                            {
                                SQLUtilities.ExecuteStoredProcedure("netgiantmasterdata", "ngmd.InsFeeFoFeedback",
                                    sqlParms);
                                fileCount += 1;
                            }
                            catch (Exception ex)
                            {
                                stnFunc.AddToActivityLog("**ERROR** Executing stored procedure ngmd.InsFeeFoFeedback");
                                stnFunc.ProcessException(ex);
                                errorHasOccurred = true;
                            }
                        }
                    }
                    else
                    {
                        stnFunc.AddToActivityLog("**Error** LoadFeeFoData.LoadData: Unmatched column name");
                        errorHasOccurred = true;
                    }
                }
                catch (Exception ex)
                {
                    stnFunc.AddToActivityLog("**Error** while processing feefo file, OrderRef: " + orderRef + ", " + ex.Message);
                    stnFunc.AddToActivityLog("Stack Trace: " + ex.StackTrace);
                    errorHasOccurred = true;
                }
                finally
                {
                    ms.Close();
                    ms.Dispose();
                }
            }

            //Log in activity log
            stnFunc.AddToActivityLog(parms["type"] + " " + fileCount + " feedbacks added. Process Finished");
            string activityLogFileName = stnFunc.LogActivity(parms["type"]);

            if (errorHasOccurred && settings.Environment == "Live")
            {
                List<string> additionalEmails = new List<string>();
                additionalEmails.Add("Daniel.whittaker@netgiant.com");
                additionalEmails.Add("stuart.deavall@netgiant.com");
                stnFunc.SendSimpleEmail(parms["type"], activityLogFileName, additionalEmails);
            }
        }

        private static int LookupFieldIndex(
            string[] headings,
            string lookupField,
            List<int> columnIndexes,
            bool requiredField = true)
        {
            int colIndex;
            bool result = int.TryParse(lookupField, out colIndex);

            if (result)
            {
                colIndex--;
                if (requiredField)
                    columnIndexes.Add(colIndex);

            }
            else
            {
                colIndex = Array.FindIndex(headings, t => t.Equals(lookupField, StringComparison.InvariantCultureIgnoreCase));
                if (requiredField)
                    columnIndexes.Add(colIndex);
            }

            return colIndex;
        }

        private static bool CheckValidColumns(List<int> indexList)
        {
            var valid = true;

            foreach (var col in indexList)
            {
                if (col < 0)
                {
                    valid = false;
                    break;
                }
            }

            return valid;
        }

        private static string GetRowFieldData(string[] row, int columnIndex)
        {
            string fieldData = "";

            if (columnIndex != -1)
            {
                fieldData = string.IsNullOrEmpty(row[columnIndex]) ? "" : row[columnIndex];
            }

            return fieldData;
        }

        public static void ProcessFeeFo(Dictionary<string, string> parms)
        {
            StandardFunctions stnFunc = new StandardFunctions();
            stnFunc.AddToActivityLog(parms["type"] + " " + parms["subtype"] + " Started");

            string connName = "axisdiplomat";

            //Execute Stored Procedure to return the equipment result in a datatable
            var sqlParams = new List<KeyValuePair<string, string>>();
            sqlParams.Add(new KeyValuePair<string, string>("website", parms["subtype"].ToUpper()));

            DataTable results;
            try
            {
                results = SQLUtilities.ExecuteStoredProcedureQuery(connName, "ng_GetFeeFoData", sqlParams);
            }
            catch (Exception ex)
            {
                stnFunc.AddToActivityLog("**ERROR** Executing stored procedure ng_GetFeeFoData");
                stnFunc.ProcessException(ex);
                stnFunc.LogActivity();
                return;
            }

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

            // FTP output file
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
            string activityLogFileName = stnFunc.LogActivity(parms["type"]);
            stnFunc = null;
        }
    }
}
