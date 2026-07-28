using MailChimp.Net;
using MailChimp.Net.Models;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.FileIO;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ngBatchProcesses.BusinessObjects.Shared;
using NGBP.DataAccessLayer.DataUtilities;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace ngBatchProcesses.BusinessObjects.DataFeeds
{
    public class ReviewRequest
    {
        public ReviewRequest(Dictionary<string, string> parms)
        {
            Parms = parms;
            Type = Parms["type"];
            SubType = Parms.ContainsKey("subtype") ? Parms["subtype"].ToString() : "";
            TargetDate = Parms.ContainsKey("date") ? Convert.ToDateTime(Parms["date"]) : (DateTime?)null;
            WebsiteId = Parms.ContainsKey("websiteid") ? Int32.Parse(Parms["websiteid"]) : 0;
            ErrorHasOccurred = false;
            ConnName = "netgiantMasterData";
            StandardFunctions.WriteProcessStarted();
            Settings = Properties.Settings.Default;
            ErrorCount = 0;
            SuccessCount = 0;
            TpSuccessCount = 0;
        }
        public bool ErrorHasOccurred { get; set; }
        public string ConnName { get; set; }
        Properties.Settings Settings { get; }
        public string Type { get; set; }
        public string SubType { get; set; }
        public DateTime? TargetDate { get; set; }
        public int WebsiteId { get; set; }
        private string ActivityLogFileName { get; set; }
        public Dictionary<string, string> Parms { get; set; }
        public int ErrorCount { get; set; }
        public int SuccessCount { get; set; }
        public int TpSuccessCount { get; set; }

        public string FfApiKey { get; set; }
        public Dictionary<string, string> TpConfig { get; set; }
        public string TpApiToken { get; set; }
        public string McApiKey { get; set; }
        public string McStoreId { get; set; }
        public string McListId { get; set; }
        public MailChimpManager Mcm { get; set; }
        public string McWorkFlowId { get; set; } = "";
        public string McQueueId { get; set; } = "";
        public List<string> TrustPilotSent { get; set; } = new List<string>();
        private static readonly HttpClient Client = new HttpClient();


        public void LoadData()
        {
            //Construct URL
            string merchantid = "";
            int months = int.Parse(Parms["period"]) * -1;
            string feedbackFrom = DateTime.Now.AddMonths(months).ToString("yyyy-MM-dd");
            string apikey = EntityFunctions.GetConfigurationSetting("BatchProgram", "FeeFoAPIKey", WebsiteId);
            
            switch (WebsiteId)
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

            int fileCount = 0;
            MemoryStream ms = new MemoryStream();
            try
            {
                string url = "http://ww2.feefo.com/api/download-feedback?merchantidentifier=" + merchantid + "&apikey=" + apikey + "&updatedsince=" + feedbackFrom;

                //Retrieve data via API call
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                using (Stream s = request.GetResponse().GetResponseStream())
                {
                    s.CopyTo(ms);
                    ms.Position = 0;
                }
            }
            catch (Exception ex)
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR while attempting to retrieve data from the Feefo API for " + merchantid + ", " + ex.Message, ErrorCode = "ERROR" });
                StandardFunctions.WriteException(ex);
                ErrorHasOccurred = true;
            }

            if (!ErrorHasOccurred)
            {
                using (TextFieldParser csvReader = new TextFieldParser(ms))
                {
                    //Process data
                    string uniqueRef = "";
                    string orderRef = "";

                    try
                    {
                        csvReader.SetDelimiters(new string[] {ControlChars.Tab.ToString()});
                        csvReader.TrimWhiteSpace = true;

                        var firstLine = new string[] { };
                        firstLine = csvReader.ReadFields();

                        List<int> indexList = new List<int>();
                        int authorColumn = LookupFieldIndex(firstLine, "Customer Name", indexList);
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
                                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR Unable to read line, previous OrderRef: " + orderRef + ", " + ex.Message, ErrorCode = "ERROR" });
                                    StandardFunctions.WriteException(ex);
                                    continue;
                                }
                                if (rowData.Length < indexList.Max())
                                {
                                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR Unable to read line, previous OrderRef: " + orderRef, ErrorCode = "ERROR" });
                                    continue;
                                }
                                var author = GetRowFieldData(rowData, authorColumn);
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
                                sqlParm2.Value = WebsiteId;
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
                                SqlParameter sqlParm8 = new SqlParameter("@Author", SqlDbType.VarChar);
                                sqlParm8.Value = author;
                                sqlParms.Add(sqlParm8);

                                try
                                {
                                    SQLUtilities.ExecuteStoredProcedure("netgiantmasterdata", "ngmd.InsFeeFoFeedback",
                                        sqlParms);
                                    fileCount += 1;
                                }
                                catch (Exception ex)
                                {
                                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "**ERROR** Executing stored procedure ngmd.InsFeeFoFeedback" });
                                    StandardFunctions.WriteException(ex);
                                    ErrorHasOccurred = true;
                                }
                            }
                        }
                        else
                        {
                            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR LoadFeeFoData.LoadData: Unmatched column name", ErrorCode = "ERROR" });
                            ErrorHasOccurred = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR LoadFeeFoData.LoadData: Unmatched column name", ErrorCode = "ERROR" });
                        StandardFunctions.WriteException(ex);
                        ErrorHasOccurred = true;
                    }
                    finally
                    {
                        ms.Close();
                        ms.Dispose();
                    }
                }
            }

            //Log in activity log
            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = " " + fileCount + " feedbacks added" });
            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
        }

        private int LookupFieldIndex(
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

        private bool CheckValidColumns(List<int> indexList)
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

        private string GetRowFieldData(string[] row, int columnIndex)
        {
            string fieldData = "";

            if (columnIndex != -1)
            {
                fieldData = string.IsNullOrEmpty(row[columnIndex]) ? "" : row[columnIndex];
            }

            return fieldData;
        }

        /// <summary>
        /// Retrieve a list of orders placed today and formulates a request to the customer requesting a review.
        /// Review requests are currently split between TrustPilot (sent via MailChimp API) and FeeFo (sent via FTP file).
        /// </summary>
        public void ProcessReviewRequests()
        {
            //Execute Stored Procedure to return orders in a datatable
            ConnName = "axisdiplomat";

            List<SqlParameter> sqlParms = new List<SqlParameter>();
            SqlParameter sqlParm = new SqlParameter("@website", SqlDbType.VarChar);
            sqlParm.Value = SubType.ToUpper();
            sqlParms.Add(sqlParm);
            if (TargetDate != null)
            {
                sqlParm = new SqlParameter("@targetDate", SqlDbType.DateTime);
                sqlParm.Value = TargetDate;
                sqlParms.Add(sqlParm);
            }
            DataTable results;
            try
            {
                results = SQLUtilities.ExecuteReadStoredProcedure(ConnName, "ng_GetFeeFoData", sqlParms, "feefodata").Tables[0];
            }
            catch (Exception ex)
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR Executing stored procedure ng_GetFeeFoData", ErrorCode = "ERROR" });
                StandardFunctions.WriteException(ex);
                return;
            }

            List<string> exclusions = new List<string>();
            exclusions = EntityFunctions.GetBackOrder(x => x.Lookup.LookupName == "Open")
                            .Select(x => x.OrderReferenceNumber)
                            .Distinct()
                            .ToList();

            // Remove elements from results that are in exclusions
            for (int i = results.Rows.Count - 1; i >= 0; i--)
            {
                if (exclusions.Contains(results.Rows[i]["Order Ref"].ToString()))
                {
                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Order excluded from Review Processing: " + results.Rows[i]["Order Ref"].ToString() });
                    results.Rows.Remove(results.Rows[i]);
                }
            }

            int trustpilotLimit = 0;
            string ids = EntityFunctions.GetConfigurationSetting("BatchProgramSetting", "MailChimpTrustPilotQId", WebsiteId);
            if (ids.Contains("#"))
            {
                McApiKey = EntityFunctions.GetNgmdCMSEntry(WebsiteId, "MiscData", "MailChimpApiKey");
                McStoreId = (Properties.Settings.Default.Environment == "Live" ? "Live_" : "Dev_") + EntityFunctions.GetNgmdCMSEntry(WebsiteId, "CommonData", "ShortSiteName");
                Mcm = new MailChimpManager(McApiKey);
                McWorkFlowId = ids.Split('#')[0];
                McQueueId = ids.Split('#')[1];
                McListId = EntityFunctions.GetNgmdCMSEntry(WebsiteId, "MiscData", "MailChimpListId");

                // Set the number of reviews to be sent to Trustpilot
                trustpilotLimit = int.Parse(EntityFunctions.GetConfigurationSetting("BatchProgram", "TpLimit", WebsiteId));
                //if (results.Rows.Count > 0)
                //{
                //    List<string> ordernos = results
                //        .AsEnumerable()
                //        .Select(x => x.Field<string>("Order Ref"))
                //        .ToList();
                //    trustpilotLimit = ordernos.Distinct().Count() * 40 / 100;
                //}                
            }

            //Setup the CSV file and the delim to use, 
            char outputDelim = new char();
            outputDelim = '\t';
            CsvFileWriter writer = new CsvFileWriter(Parms["output"], outputDelim);

            //Write the headings, the first row in the CSV
            CsvRow firstRow = new CsvRow();
            foreach (DataColumn dc in results.Columns)
            {
                firstRow.Add(dc.ColumnName);
            }
            writer.WriteRow(firstRow);

            if (results.Rows.Count > 0)
            {
                int trustpilotCount = 0;
                string orderref = results.Rows[0]["Order Ref"].ToString();

                if (trustpilotLimit > 0)    // Trustpilot is in use for this website
                {
                    // Send first n to Trustpilot
                    TpData data = new TpData(results.Rows[0], trustpilotCount, null);
                    foreach (DataRow dr in results.Rows)
                    {
                        if (trustpilotCount < trustpilotLimit)
                        {
                            if (dr["Order Ref"].ToString() != orderref)
                            {
                                Task t = CreateTrustpilotInviteAsync(data);
                                t.Wait();
                                orderref = dr["Order Ref"].ToString();
                                trustpilotCount += 1;
                                data = new TpData(dr, trustpilotCount, null);
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                    if (trustpilotCount < trustpilotLimit)
                    {
                        Task t = CreateTrustpilotInviteAsync(data);
                        trustpilotCount += 1;
                    }
                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "TrustPilot Success User Count: " + TpSuccessCount });
                }

                // Send rest to FeeFo
                foreach (DataRow dr in results.Rows)
                {
                    // Skip orders already sent to TrustPilot
                    if (TrustPilotSent.Where(x => x.Contains(dr["Order Ref"].ToString())).FirstOrDefault() != null)
                    {
                        continue;
                    }

                    CsvRow newRow = new CsvRow();
                    foreach (DataColumn dc in results.Columns)
                    {
                        newRow.Add(dr[dc.ColumnName].ToString());
                    }
                    writer.WriteRow(newRow);
                    SuccessCount += 1;
                }
            }

            //Close the writer and dispose of the datatable containing the results
            writer.Close();
            results.Dispose();

            // FTP output file
            if (Parms.ContainsKey("ftpsite"))
            {
                var settings = Properties.Settings.Default;
                string finalFileName = "";
                string[] fileParts = Parms["output"].Split('\\');
                finalFileName = fileParts[fileParts.Length - 1]; //Kenshoo final Path Part
                try
                {
                    FtpUtilities.UploadSFTPFiles(
                        Parms["ftpsite"],
                        Parms["ftpusername"],
                        Parms["ftppassword"],
                        Parms["ftppath"],
                        Parms["output"],
                        2222);
                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Feefo Success Record Count: " + SuccessCount });
                }
                catch (Exception ex)
                {
                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR uploading FTP file to Feefo", ErrorCode = "ERROR" });
                    StandardFunctions.WriteException(ex);
                }
            }

            //Log in activity log
            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
        }

        public async Task CreateTrustpilotInviteAsync(TpData data)
        {            
            try
            {
                // Attempt to update the Order Number in the main Mailing List
                Member m = await Mcm.Members.GetAsync(McListId, data.consumerEmail);
                m.MergeFields["ORDERNO"] = data.orderNumber;
                await Mcm.Members.AddOrUpdateAsync(McListId, m);

                // Subscribe the customer to the Workflow
                await Mcm.AutomationEmailQueues.AddSubscriberAsync(McWorkFlowId, McQueueId, data.consumerEmail);
                TrustPilotSent.Add(data.orderNumber);
                TpSuccessCount += 1;
            }
            catch (Exception e) {}
        }

        private async Task<string> GetTpTokenAsync()
        {
            string tpTokenUri = "https://api.trustpilot.com/v1/oauth/oauth-business-users-for-applications/accesstoken";

            var form = new Dictionary<string, string>
                {
                    {"grant_type", "password"},
                    {"username", TpConfig["TpUser"]},
                    {"password", TpConfig["TpPassword"]},
                };
            
            Client.DefaultRequestHeaders.Accept.Clear();
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(
                    Encoding.ASCII.GetBytes(string.Format("{0}:{1}", TpConfig["TpAPIKey"], TpConfig["TpAPISecret"]))));
            HttpResponseMessage tokenResponse = await Client.PostAsync(tpTokenUri, new FormUrlEncodedContent(form));
            string jsonContent = "";
            if (tokenResponse.StatusCode == HttpStatusCode.OK)
            {
                jsonContent = await tokenResponse.Content.ReadAsStringAsync();
            }
            return jsonContent;
        }

        private DataTable GetTestData()
        {
            string sql = "";
            string union = "";
            string email = "";
            int orderref = 20001;
            for (int i = 0; i < 4; i++)
            {
                switch (i)
                    {
                    case 0:
                        email = "stuart.deavall@tonergiant.co.uk";
                        break;
                    case 1:
                        email = "stuart.deavall@netgiant.comglen.dale@netgiant.com";
                        break;
                    case 2:
                        email = "glen.dale130@gmail.com";
                        break;
                    case 3:
                        email = "service.admin@netgiant.com";
                        break;
                }
                string[] name = email.Split('@')[0].Split('.');
                sql += union + @"
                    SELECT '" + name[0] + @"' AS [Name]
                    , '" + email + @"' AS [Email]
                    , '16/10/2019' AS[Date]
	                , 'Epson T0594 Yellow Ink Cartridge' AS[Description]
	                , 'toner-giant' AS[Merchant Identifier]
	                , 'C13T05944010' AS[Product Search Code]
                    , 'TVUXF' AS[Order Ref]
	                , '16/10/2019' AS[Feedback Date]
	                , '155194' AS[Customer Ref]
                    , 16.50 AS[Amount]
	                , 'GBP' AS[Currency]
                    , 'https://www.tonergiant.co.uk/product/Epson-T0594-Yellow-Ink-Cartridge-C13T05944010-10923/' AS[Product Link]
	                , 'productline=Epson Ink' AS[Tags]
                    UNION ALL 
                    SELECT '" + name[0] + @"' AS [Name]
                    , '" + email + @"' AS [Email]
                    , '16/10/2019' AS[Date]
	                , 'Epson T0599 Light Light Black Ink Cartridge' AS[Description]
	                , 'toner-giant' AS[Merchant Identifier]
	                , 'C13T05994010' AS[Product Search Code]
	                , '" + (orderref + i).ToString() + @"' AS[Order Ref]
	                , '16/10/2019' AS[Feedback Date]
	                , '155194' AS[Customer Ref]
                    , 15.52 AS[Amount]
	                , 'GBP' AS[Currency]
                    , 'https://www.tonergiant.co.uk/product/Epson-T0599-Light-Light-Black-Ink-Cartridge-C13T05994010-10928/' AS[Product Link]
	                , 'productline=Epson Ink' AS[Tags]
                    ";
                if (union == "")
                {
                    union = "UNION ALL ";
                }
            }
            DataSet ds = SQLUtilities.ExecuteReadInline(ConnName, sql, "ds", 60);
            return ds.Tables[0];
        }
    }


    public class TpData
    {
        public TpData()
        {
            serviceReviewInvitation = new TpServReviewInvitation();
        }
        public TpData(DataRow dr, int counter, string reply)
        {
            serviceReviewInvitation = new TpServReviewInvitation();

            consumerEmail = dr["Email"].ToString();
            replyTo = reply;
            referenceNumber = DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + counter.ToString();
            consumerName = dr["Name"].ToString();
            locale = "en-GB";
            senderEmail = "noreply.invitations@trustpilotmail.com";
            senderName = "Trustpilot";
            serviceReviewInvitation.preferredSendTime = DateTime.Now.AddDays(7);
            if (ConfigurationManager.AppSettings["Environment"] != "Live")
            {
                serviceReviewInvitation.preferredSendTime = DateTime.Now.AddMinutes(10);
            }
            orderNumber = dr["Order Ref"].ToString();
        }
        public string consumerEmail { get; set; }
        public string replyTo { get; set; }
        public string referenceNumber { get; set; }
        public string consumerName { get; set; }
        public string locale { get; set; }
        public string senderEmail { get; set; }
        public TpServReviewInvitation serviceReviewInvitation { get; set; }
        public string locationId { get; set; }
        public string senderName { get; set; }
        public string orderNumber { get; set; }
    }

    public class TpServReviewInvitation
    {
        public DateTime preferredSendTime { get; set; }
    }
}
