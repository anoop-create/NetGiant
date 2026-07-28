using ngBatchProcesses.BusinessObjects.Shared;
using NGBP.DataAccessLayer.DataUtilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System.Globalization;
using System.Configuration;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators;
using RestSharp.Serializers.NewtonsoftJson;
using System.Web.Util;
using System.Web.UI.WebControls;
using System.Web.Helpers;

namespace ngBatchProcesses.BusinessObjects.DataFeeds
{
    public class StannpFeed
    {
        public StannpFeed(Dictionary<string, string> parms)
        {
            Parms = parms;
            Type = Parms["type"];
            SubType = Parms["subtype"];
            WebsiteId = Int32.Parse(Parms["websiteid"]);
            ConnName = "netgiantMasterData";
            StandardFunctions.WriteProcessStarted();
            InTestMode = Parms.ContainsKey("testmode");
            if (InTestMode)
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "***** RUNNING IN TEST MODE ******" });
            }
            Settings = Properties.Settings.Default;
            ErrorCount = 0;
            SuccessCount = 0;
            DeletedCount = 0;

            ApiKey = EntityFunctions.GetConfigurationSetting("BatchProgram", "StannpAPIKey", WebsiteId);
        }

        public string ConnName { get; set; }
        Properties.Settings Settings { get; }
        public string Type { get; set; }
        public string SubType { get; set; }
        public int WebsiteId { get; set; }
        public bool InTestMode { get; set; }
        private string ActivityLogFileName { get; set; }
        public Dictionary<string, string> Parms { get; set; }
        public int ErrorCount { get; set; }
        public int SuccessCount { get; set; }
        public int DeletedCount { get; set; }
        public List<long> DeleteList {get;set;}
        public int KeepCount { get; set; }
        private static readonly HttpClient Client = new HttpClient();

        public string ApiKey { get; set; }
        public string ListId { get; set; }

        public void ProcessFeed()
        {
            if (SubType == "maintainlist")
            {
                ListId = Parms["id"];
                Task t = MaintainListAsync();
                t.Wait();

                StandardFunctions.CWrite("End Processing");     // Local only
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Deleted Count: " + DeletedCount.ToString() + ". Keep Count: " + KeepCount.ToString() + "." });
                StandardFunctions.CReadKey();                   // Local only
            }
            else
            {
                Task t = RunProcessAsync();
                t.Wait();

                //Log in activity log
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Success Count: " + SuccessCount + ", Error Count: " + ErrorCount });
            }
            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
        }

        public async Task MaintainListAsync()
        {
            DeleteList = new List<long>();
            DateTime cutOffDate = DateTime.Now.AddDays(-180); // 6 months
            int offset = 0;

            StannpRecipients recipients = RetrieveList(0);
            while (recipients.Items.Count > 0)
            {
                if (recipients.Success)
                {
                    foreach (StannpRecipient r in recipients.Items)
                    {
                        if (r.dateCreated < cutOffDate)
                        {
                            DeleteList.Add(r.Id);
                        }
                        else
                        {
                            KeepCount++;
                        }
                    }
                }
                else
                {
                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Stannp error: Unable to retrieve group list for " + ListId, ErrorCode = "ERROR" });
                }

                offset += 2000;
                recipients = RetrieveList(offset);
            }

            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Attempting to remove " + DeleteList.Count.ToString() + " recipients." });

            var tasks = new List<Task>();
            int count = 0;
            int batchSize = 100;
            foreach (long id in DeleteList)
            {
                // Process 100 firstly and then 300 at a time because Stannp only allow 300 API calls per minute
                if (count == batchSize)
                {
                    Task t = Task.WhenAll(tasks);
                    try
                    {
                        await t;
                    }
                    catch { }

                    tasks = new List<Task>();
                    System.Threading.Thread.Sleep(60000);
                    count = 0;
                    batchSize = 300;
                }
                tasks.Add(DeleteRecipientAsync(id));
                count++;
            }
            Task tt = Task.WhenAll(tasks);
            try
            {
                await tt;
            }
            catch { }
        }

        private async Task RunProcessAsync()
        {
            string sql = BuildSql();
            DataSet ds = SQLUtilities.ExecuteReadInline("axisdiplomat", sql, "ds", 60);
            DataTable dt = ds.Tables[0];

            string rs = GetConfiguration();
            string[] runSettings = rs.Split('#');

            DateTime triggerDate = DateTime.Now.AddDays((double)1);
            DateTime expiryDate = DateTime.Now.AddDays(Convert.ToDouble(runSettings[3]));
            string url = "https://dash.stannp.com/api/v1/recipients/new?limit=300&api_key=" + ApiKey;
            double seconds = Math.Round((DateTime.Now - (new DateTime(2018, 01, 01))).TotalSeconds, 0);

            ServicePointManager.Expect100Continue = true;
            StandardFunctions.SetTlsVersion();

            foreach (DataRow dr in dt.Rows)
            {
                string voucherCode = "";
                VoucherPromo vp;
                if (SubType == "newcustomer2")
                {
                    // Retrieve existing voucher
                    string ac = dr["CustomerReference"].ToString();
                    vp = EntityFunctions.GetVoucher(x => x.AccountNumber == ac);
                    if (vp == null)
                    {
                        ErrorCount += 1;
                        StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Stannp error: Unable to retrieve existing voucher for customer - " + dr["CustomerReference"].ToString(), ErrorCode = "WARNING" });
                        continue;
                    }
                }
                else
                {
                    // Generate a voucher code
                    string secondsstring = (seconds).ToString();
                    seconds += 1;
                    voucherCode = StandardFunctions.GenerateVoucherCode(secondsstring);

                    // Store Voucher
                    vp = new VoucherPromo
                    {
                        WebsiteFk = WebsiteId,
                        VoucherTypeFk = Int32.Parse(runSettings[2]),
                        VoucherPromoGroupFk = Int32.Parse(runSettings[1]),
                        VoucherCode = voucherCode,
                        Description = runSettings[0],
                        ValidFrom = DateTime.Now,
                        ValidTo = expiryDate,
                        StockRef = runSettings[4],
                        MinBasketValue = 0,
                        MinQualValue = 0,
                        Percentage = Convert.ToDecimal(runSettings[5]),
                        AccountNumber = dr["CustomerReference"].ToString(),
                        IsGlobal = true,
                        IsSingleUse = true,
                        IsUsed = false
                    };
                    if (Int32.Parse(dr["PaymentMethod"].ToString()) == 8)
                    {
                        vp.AccountNumber = null;
                    }
                    if (!EntityFunctions.VoucherExists(x => x.VoucherCode == voucherCode && x.WebsiteFk == WebsiteId))
                    {
                        if (!EntityFunctions.SaveVoucher(vp))
                        {
                            ErrorCount += 1;
                            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Stannp error: Unable to save voucher for customer - " + dr["CustomerReference"].ToString(), ErrorCode = "WARNING" });
                        }
                    }
                    else
                    {
                        ErrorCount += 1;
                        StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Stannp error: Voucher Code already exists - " + vp.VoucherCode, ErrorCode = "ERROR" });
                    }
                }

                // Construct Dictionary
                Dictionary<string, string> dict = new Dictionary<string, string>
                {
                    {"title", dr["Title"].ToString() },
                    {"firstname", CultureInfo.CurrentCulture.TextInfo.ToTitleCase(dr["Forename"].ToString()) },
                    {"lastname", CultureInfo.CurrentCulture.TextInfo.ToTitleCase(dr["Surname"].ToString()) },
                    {"full_name", "" },
                    {"company", dr["Company"].ToString() },
                    {"address1", dr["AddressLine1"].ToString() },
                    {"address2", dr["AddressLine2"].ToString() },
                    {"address3", dr["AddressLine3"].ToString() },
                    {"city", dr["TownCity"].ToString() },
                    {"postcode", dr["PostCode"].ToString() },
                    {"country", "GB" },
                    {"group_id", runSettings[6] },
                    {"on_duplicate", "update" },
                    {SubType == "newcustomer2" ? "trigger_date2" : "trigger_date", triggerDate.ToString("yyyy-MM-dd") },
                    {"voucher_code", vp.VoucherCode },
                    {"voucher_expiry", vp.ValidTo.ToString("yyyy-MM-dd") },
                    {"voucher_expiry_text", vp.ValidTo.ToString("dd MMM yyyy") }
                };

                // Validation
                dict["full_name"] = dict["firstname"] + " " + dict["lastname"];
                if (dict["firstname"].Length < 4)
                {
                    dict["firstname"] = dict["firstname"] + " " + dict["lastname"];
                }

                // Post
                if (!InTestMode)
                {
                    var response = await Client.PostAsync(url, new FormUrlEncodedContent(dict));

                    var responseString = await response.Content.ReadAsStringAsync();
                    JObject j = JObject.Parse(responseString);
                    if (Convert.ToBoolean(j["success"]))
                    {
                        if (Convert.ToBoolean(j["data"]["valid"]))
                        {
                            SuccessCount += 1;
                        }
                        else
                        {
                            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Stannp error: Invalid data for - " + dr["CustomerReference"].ToString(), ErrorCode = "ERROR" });
                            ErrorCount += 1;
                        }
                    }
                    else
                    {
                        ErrorCount = +1;
                    }
                }
                else
                {
                    SuccessCount += 1;
                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Stannp letter: " + JsonConvert.SerializeObject(dict) });
                }
            }
        }

        private string GetConfiguration()
        {
            switch (SubType)
            {
                case "newcustomer":
                    {
                        return EntityFunctions.GetConfigurationSetting("BatchProgram", "StannpNewCustomerSettings", WebsiteId);
                    }

                case "newcustomer2":
                    {
                        return EntityFunctions.GetConfigurationSetting("BatchProgram", "StannpNewCustomer2Settings", WebsiteId);
                    }

                case "retention":
                    {
                        return EntityFunctions.GetConfigurationSetting("BatchProgram", "StannpRetentionSettings", WebsiteId);
                    }
            }
            return "";
        }

        private StannpRecipients RetrieveList(int offset)
        {
            RestClient client = new RestClient("https://dash.stannp.com/api/v1/", configureSerialization: s => s.UseNewtonsoftJson());

            var request = new RestRequest("recipients/list/" + ListId + "?limit=2000&offset=" + offset.ToString() + "&api_key=" + ApiKey);
            var response = client.Execute(request, RestSharp.Method.Get);

            StannpRecipients info = JsonConvert.DeserializeObject<StannpRecipients>(response.Content);
            return info;
        }

        private async Task DeleteRecipientAsync(long id)
        {
            StandardFunctions.CWrite("Deleting " + id + ", Count = " + DeletedCount);
            RestClient client = new RestClient("https://dash.stannp.com/api/v1/", configureSerialization: s => s.UseNewtonsoftJson());

            var request = new RestRequest("recipients/delete?api_key=" + ApiKey)
                .AddParameter("id", id);
            RestResponse response = await client.ExecuteAsync(request, RestSharp.Method.Post);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                if (Convert.ToBoolean(JObject.Parse(response.Content)["success"].ToString()))
                {
                    DeletedCount++;
                    return;
                }
            }
            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Unable to delete recipient: " + id.ToString() + " , Status Code: " + response.StatusCode.ToString() });
        }

        private string BuildSql()
        {
            string top = "";
            if (Parms.ContainsKey("number"))
            {
                top = " TOP " + Parms["number"];
            }
            switch (SubType)
            {
                case "newcustomer":
                    {
                        if (ConfigurationManager.AppSettings["Environment"] != "Live")
                        {
                            return @"
                                SELECT '2019-10-04' AS [TransactionDate]
                                , '01/99999' AS [CustomerReference]
                                , 'Stuart Deavall' AS[ContactName]
                                , 8 As [PaymentMethod]
	                            , 'Mr' AS[Title]
	                            , 'Stuart' AS[Forename]
	                            , 'Deavall' AS[Surname]
	                            , 'Netgiant Ltd' AS[Company]
	                            , '61 Gibfield Park Avenue' AS[AddressLine1]
	                            , '' AS[AddressLine2]
                                , '' AS[AddressLine3]
	                            , '' AS[AddressLine4]
                                , '' AS[AddressLine5]
	                            , 'Manchester' AS[TownCity]
                                , '' AS[County]
	                            , 'M46 0SY' AS[PostCode]
                                UNION ALL
                                SELECT '2019-10-04' AS [TransactionDate]
                                , '01/99998' AS [CustomerReference]
                                , 'Louise Murray' AS[ContactName]
                                , 0 As [PaymentMethod]
	                            , 'Miss' AS[Title]
	                            , 'Loise' AS[Forename]
	                            , 'Murray' AS[Surname]
	                            , 'Netgiant Ltd' AS[Company]
	                            , '61 Gibfield Park Avenue' AS[AddressLine1]
	                            , '' AS[AddressLine2]
                                , '' AS[AddressLine3]
	                            , '' AS[AddressLine4]
                                , '' AS[AddressLine5]
	                            , 'Manchester' AS[TownCity]
                                , '' AS[County]
	                            , 'M46 0SY' AS[PostCode]
                            ";
                        }
                        else
                        {
                            return @"
                            DECLARE @startdate DATE
                            DECLARE @enddate DATE
                            DECLARE @newjoincutoff DATE

                            DECLARE @Customers TABLE (
		                            CustomerReference VARCHAR(33)
		                            , TransactionDate DATE
		                            , PaymentMethod TINYINT
	                            )

                            SET @newjoincutoff = DATEADD(dd, DATEDIFF(dd, 0, GETDATE() - 1000), 0)
                            SET @startdate = DATEADD(dd, DATEDIFF(dd, 0, GETDATE() - " + Parms["period"] + @"), 0)
                            SET @enddate = DATEADD(dd, DATEDIFF(dd, 0, GETDATE()), 0)

                            INSERT INTO @Customers
	                            SELECT s.CustomerReference, s.TransactionDate, soi.PaymentMethod
	                            FROM [/acc/Sales] s
	                            OUTER APPLY (SELECT TOP 1 DocumentStatusCode As [PaymentMethod] FROM dbo.[/acc/SalesOrdersIndexed] WHERE CustomerOrderNumber = s.Description) soi
	                            WHERE DocumentReference LIKE '%INV%' 
	                            AND CustomerGroupCode = 10
	                            AND TransactionDate < @enddate
	                            AND TransactionDate >= @startdate
	                            GROUP BY CustomerReference, s.TransactionDate, soi.PaymentMethod
	                            ORDER BY CustomerReference

                            --SELECT * FROM @Customers
                            --ORDER BY CustomerReference

                            DELETE FROM @Customers
                            WHERE CustomerReference IN (
	                            SELECT CustomerReference
		                            FROM [/acc/Sales] s
		                            WHERE DocumentReference LIKE '%INV%' 
		                            AND CustomerReference IN (SELECT CustomerReference FROM @Customers)
		                            AND TransactionDate < @startdate
		                            AND TransactionDate > @newjoincutoff
		                            GROUP BY CustomerReference
		                            ) 

                            SELECT" + top + @" TransactionDate
                                , CustomerReference
                                , Customer.cnm AS[ContactName]
                                , PaymentMethod
	                            , UPPER(Left(Contacts.ftit, 1)) + LOWER(SUBSTRING(Contacts.ftit, 2, 20)) AS[Title]
	                            , UPPER(Left(Contacts.fornm, 1)) + LOWER(SUBSTRING(Contacts.fornm, 2, 30)) AS[Forename]
	                            , UPPER(Left(Contacts.surnm, 1)) + LOWER(SUBSTRING(Contacts.surnm, 2, 30)) AS[Surname]
	                            , FormattedAdress.org AS[Company]
	                            , FormattedAdress.add_0 AS[AddressLine1]
	                            , FormattedAdress.add_1 AS[AddressLine2]
                                , FormattedAdress.add_2 AS[AddressLine3]
	                            , FormattedAdress.add_3 AS[AddressLine4]
                                , FormattedAdress.add_4 AS[AddressLine5]
	                            , FormattedAdress.city AS[TownCity]
                                , FormattedAdress.state AS[County]
	                            , UPPER(FormattedAdress.pcode) AS[PostCode] 
                            FROM @Customers C
                            LEFT JOIN(SELECT cusrf, aduid, cnm FROM dbo.acccus01) Customer on Customer.cusrf = C.CustomerReference
                            LEFT JOIN dbo.accfad00 FormattedAdress ON FormattedAdress.uid = Customer.aduid
                            LEFT JOIN dbo.accadr00 AdditionalDetails ON AdditionalDetails.arn = C.CustomerReference
                            LEFT JOIN dbo.accaad01 Contacts ON Contacts.adref = C.CustomerReference AND Contacts.no = AdditionalDetails.dcon
                            ORDER BY CustomerReference
                            ";
                        }
                    }

                case "newcustomer2":
                    {
                        if (ConfigurationManager.AppSettings["Environment"] != "Live")
                        {
                            return @"
                                SELECT '2019-10-04' AS [TransactionDate]
                                , '01/99999' AS [CustomerReference]
                                , 'Stuart Deavall' AS[ContactName]
                                , 8 As [PaymentMethod]
	                            , 'Mr' AS[Title]
	                            , 'Stuart' AS[Forename]
	                            , 'Deavall' AS[Surname]
	                            , 'Netgiant Ltd' AS[Company]
	                            , '61 Gibfield Park Avenue' AS[AddressLine1]
	                            , '' AS[AddressLine2]
                                , '' AS[AddressLine3]
	                            , '' AS[AddressLine4]
                                , '' AS[AddressLine5]
	                            , 'Manchester' AS[TownCity]
                                , '' AS[County]
	                            , 'M46 0SY' AS[PostCode]
                                UNION ALL
                                SELECT '2019-10-04' AS [TransactionDate]
                                , '01/99998' AS [CustomerReference]
                                , 'Loise Murray' AS[ContactName]
                                , 0 As [PaymentMethod]
	                            , 'Miss' AS[Title]
	                            , 'Louise' AS[Forename]
	                            , 'Murray' AS[Surname]
	                            , 'Netgiant Ltd' AS[Company]
	                            , '61 Gibfield Park Avenue' AS[AddressLine1]
	                            , '' AS[AddressLine2]
                                , '' AS[AddressLine3]
	                            , '' AS[AddressLine4]
                                , '' AS[AddressLine5]
	                            , 'Manchester' AS[TownCity]
                                , '' AS[County]
	                            , 'M46 0SY' AS[PostCode]
                            ";
                        }
                        else
                        {
                            return @"
                            DECLARE @startdate DATE
                            DECLARE @enddate DATE
                            DECLARE @newjoincutoff DATE

                            DECLARE @Customers TABLE (
		                            CustomerReference VARCHAR(33)
		                            , TransactionDate DATE
		                            , PaymentMethod TINYINT
	                            )

                            SET @newjoincutoff = DATEADD(dd, DATEDIFF(dd, 0, GETDATE() - 1090), 0)
                            SET @startdate = DATEADD(dd, DATEDIFF(dd, 0, GETDATE() - 97), 0)
                            SET @enddate = DATEADD(dd, DATEDIFF(dd, 0, GETDATE() - 90), 0)

                            INSERT INTO @Customers
	                            SELECT s.CustomerReference, s.TransactionDate, soi.PaymentMethod
	                            FROM [/acc/Sales] s
	                            OUTER APPLY (SELECT TOP 1 DocumentStatusCode As [PaymentMethod] FROM dbo.[/acc/SalesOrdersIndexed] WHERE CustomerOrderNumber = s.Description) soi
	                            WHERE DocumentReference LIKE '%INV%' 
	                            AND CustomerGroupCode = 10
	                            AND TransactionDate < @enddate
	                            AND TransactionDate >= @startdate
	                            GROUP BY CustomerReference, s.TransactionDate, soi.PaymentMethod
	                            ORDER BY CustomerReference

                            --SELECT * FROM @Customers
                            --ORDER BY CustomerReference

                            DELETE FROM @Customers
                            WHERE CustomerReference IN (
	                            SELECT CustomerReference
		                            FROM [/acc/Sales] s
		                            WHERE DocumentReference LIKE '%INV%' 
		                            AND CustomerReference IN (SELECT CustomerReference FROM @Customers)
		                            AND TransactionDate < @startdate
		                            AND TransactionDate > @newjoincutoff
		                            GROUP BY CustomerReference
		                            )
		
                            DELETE FROM @Customers
                            WHERE CustomerReference IN (
	                            SELECT CustomerReference
		                            FROM [/acc/Sales] s
		                            WHERE DocumentReference LIKE '%INV%' 
		                            AND CustomerReference IN (SELECT CustomerReference FROM @Customers)
		                            AND TransactionDate > @enddate
		                            GROUP BY CustomerReference
		                            )

                            SELECT" + top + @" TransactionDate
                                , CustomerReference
                                , Customer.cnm AS[ContactName]
                                , PaymentMethod
	                            , UPPER(Left(Contacts.ftit, 1)) + LOWER(SUBSTRING(Contacts.ftit, 2, 20)) AS[Title]
	                            , UPPER(Left(Contacts.fornm, 1)) + LOWER(SUBSTRING(Contacts.fornm, 2, 30)) AS[Forename]
	                            , UPPER(Left(Contacts.surnm, 1)) + LOWER(SUBSTRING(Contacts.surnm, 2, 30)) AS[Surname]
	                            , FormattedAdress.org AS[Company]
	                            , FormattedAdress.add_0 AS[AddressLine1]
	                            , FormattedAdress.add_1 AS[AddressLine2]
                                , FormattedAdress.add_2 AS[AddressLine3]
	                            , FormattedAdress.add_3 AS[AddressLine4]
                                , FormattedAdress.add_4 AS[AddressLine5]
	                            , FormattedAdress.city AS[TownCity]
                                , FormattedAdress.state AS[County]
	                            , UPPER(FormattedAdress.pcode) AS[PostCode] 
                            FROM @Customers C
                            LEFT JOIN(SELECT cusrf, aduid, cnm FROM dbo.acccus01) Customer on Customer.cusrf = C.CustomerReference
                            LEFT JOIN dbo.accfad00 FormattedAdress ON FormattedAdress.uid = Customer.aduid
                            LEFT JOIN dbo.accadr00 AdditionalDetails ON AdditionalDetails.arn = C.CustomerReference
                            LEFT JOIN dbo.accaad01 Contacts ON Contacts.adref = C.CustomerReference AND Contacts.no = AdditionalDetails.dcon
                            ORDER BY CustomerReference
                            ";
                        }
                    }

                case "retention":
                    {
                        return @"
                            DECLARE @sdate DATE
                            DECLARE @edate DATE

                            SET @sdate = DATEADD(dd, DATEDIFF(dd, 0, GETDATE() - 90), 0)
                            SET @edate = DATEADD(dd, DATEDIFF(dd, 0, GETDATE() - 30), 0)

                            SELECT" + top + @" BaseData.cusrf AS [CustomerReference]
                                , Customer.cnm AS[ContactName]
	                            , UPPER(Left(Contacts.ftit, 1)) + LOWER(SUBSTRING(Contacts.ftit, 2, 20)) AS[Title]
	                            , UPPER(Left(Contacts.fornm, 1)) + LOWER(SUBSTRING(Contacts.fornm, 2, 30)) AS[Forename]
	                            , UPPER(Left(Contacts.surnm, 1)) + LOWER(SUBSTRING(Contacts.surnm, 2, 30)) AS[Surname]
	                            , FormattedAdress.org AS[Company]
	                            , FormattedAdress.add_0 AS[AddressLine1]
	                            , FormattedAdress.add_1 AS[AddressLine2]
                                , FormattedAdress.add_2 AS[AddressLine3]
	                            , FormattedAdress.add_3 AS[AddressLine4]
                                , FormattedAdress.add_4 AS[AddressLine5]
	                            , FormattedAdress.city AS[TownCity]
                                , FormattedAdress.state AS[County]
	                            ,Upper(FormattedAdress.pcode) AS[PostCode]
                            FROM (SELECT *
	                            FROM (
		                            SELECT Accounts.cusrf
			                            , Accounts.namaa_0
			                            , Accounts.grp
			                            , COUNT(distinct Sales.DocumentReference) as [NoOrders]
			                            , ROUND(sum(Sales.NetValue), 2) as [Turnover]
			                            , ROUND(sum(Sales.NetValue)/ COUNT(distinct Sales.DocumentReference), 2) as [AOV]
			                            , CASE WHEN COUNT(distinct Sales.DocumentReference) > 2 
					                            THEN (DateDIFF(DAY,MIN(Sales.TransactionDate),MAX(Sales.TransactionDate)) / COUNT(distinct Sales.DocumentReference) -1) + MAX(Sales.TransactionDate)
				                            ELSE (MAX(Sales.TransactionDate) + 45)
			                            END AS [Expected Next Order Date]
		                            FROM acccus01 Accounts
		                            INNER JOIN [/acc/Sales] Sales ON Sales.CustomerReference = Accounts.cusrf AND Sales.DocumentReference LIKE 'INV%'
		                            WHERE Accounts.grp IN (10,11,12)
		                            GROUP BY
			                            Accounts.cusrf
			                            , Accounts.namaa_0
			                            , Accounts.grp
		                            ) Customers
	                            WHERE [Expected Next Order Date] >= @sdate
	                            AND [Expected Next Order Date] < @edate
	                            AND Customers.NoOrders > 1
	                            AND Customers.AOV >= 100	
	                            ) BaseData
                            LEFT JOIN(SELECT cusrf, aduid, cnm FROM acccus01) Customer on BaseData.cusrf = Customer.cusrf
                            LEFT JOIN dbo.accfad00 FormattedAdress ON Customer.aduid = FormattedAdress.uid
                            LEFT JOIN dbo.accadr00 AdditionalDetails ON BaseData.cusrf = AdditionalDetails.arn
                            LEFT JOIN dbo.accaad01 Contacts ON adref = BaseData.cusrf AND no = AdditionalDetails.dcon
                            ORDER BY BaseData.Turnover DESC
                        ";
                    }
            }
            return "";
        }

        public partial class StannpRecipients
        {
            [JsonProperty("success")]
            public bool Success { get; set; } = false;

            [JsonProperty("data")]
            public List<StannpRecipient> Items { get; set; }
        }

        public partial class StannpRecipient
        {
            [JsonProperty("id")]
            public long Id { get; set; }

            [JsonProperty("created")]
            public DateTime dateCreated { get; set; }
        }
    }
}
