using netGiant.Intranet.DataLayer.NetgiantMasterData;
using Newtonsoft.Json.Linq;
using ngBatchProcesses.BusinessObjects.Apis;
using ngBatchProcesses.BusinessObjects.Shared;
using NGBP.DataAccessLayer.DataUtilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ngBatchProcesses.BusinessObjects.DataFeeds
{
    public class SalesforceFeed
    {
        public SalesforceFeed(Dictionary<string, string> parms)
        {
            StandardFunctions.WriteProcessStarted();
            Parms = parms;
            if (Parms.ContainsKey("subtype"))
            {
                SubType = Parms["subtype"];
            }
            if (Parms.ContainsKey("action"))
            {
                Action = Parms["action"];
            }
        }

        public Salesforce Salesforce { get; set; }
        public GoogleAnalytics GoogleAnalytics { get; set; }
        public Dictionary<string, string> Parms { get; set; }
        public string SubType { get; set; }
        public string Action { get; set; }
        //public string DefaultAccount { get; set; }
        public string DefaultRecordTypeId { get; set; }
        public string DefaultAccountId { get; set; }
        public string KeyValue { get; set; }
        public StringBuilder Payload = new StringBuilder();
        private int RecordCount { get; set; }

        public void ProcessFeed()
        {
            Salesforce = new Salesforce();            
            if (!Salesforce.Authenticate())
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Unable to authenticate to Salesforce", ErrorCode = "ERROR" });
                return;
            }
            switch (SubType)
            {
                case "accounts":
                    {
                        if (Action == "load")
                        {
                            LoadAccounts();
                        }
                        break;
                    }
                case "contacts":
                    {
                        if (Action == "load")
                        {
                            LoadContacts();
                        }
                        break;
                    }
                case "orders":
                    {
                        if (Action == "load")
                        {
                            LoadOrders();
                        }
                        break;
                    }
                case "orderlines":
                    {
                        if (Action == "load")
                        {
                            LoadOrderLines();
                        }
                        break;
                    }
                case "products":
                    {
                        if (Action == "load")
                        {
                            LoadProducts();
                        }
                        break;
                    }
                case "all":
                    {
                        if (Action == "load")
                        {
                            LoadAccounts();
                            LoadContacts();
                            LoadOrders();
                            LoadOrderLines();
                        }
                        break;
                    }
                case "updatestats":
                    {
                        UpdateJobStats();
                        break;
                    }
                case "delete":
                    {
                        DeleteObject();
                        break;
                    }
            }
            Salesforce = null;
            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
        }

        private void LoadOrders()
        {
            DateTime endDate = DateTime.Now;
            if (Parms.ContainsKey("date"))
            {
                endDate = Convert.ToDateTime(Parms["date"]);
            }

            // Retrieve orders for the last 2 days
            List<SqlParameter> sqlParms = new List<SqlParameter>();
            SqlParameter sqlParm = new SqlParameter("@endDate", SqlDbType.DateTime);
            sqlParm.Value = endDate;
            sqlParms.Add(sqlParm);
            DataTable orders;
            try
            {
                orders = SQLUtilities.ExecuteReadStoredProcedure("axisdiplomat", "dbo.ng_GetOrdersForSalesforce",
                    sqlParms, "sfdata", 200).Tables[0];
            }
            catch (Exception e)
            {
                StandardFunctions.WriteException(e);
                return;
            }

            orders.Columns.Add("Source", Type.GetType("System.String"));
            orders.Columns.Add("Medium", Type.GetType("System.String"));
            orders.Columns.Add("Campaign", Type.GetType("System.String"));

            // Retrieve Analytics data for the last week
            GoogleAnalytics = new GoogleAnalytics();
            DataTable gaData = GoogleAnalytics.GetTransactions(endDate);
            GoogleAnalytics = null;

            // Merge Order data with Analytics data
            foreach (DataRow order in orders.Rows)
            {
                DataRow gaFound = gaData.AsEnumerable().FirstOrDefault(r => r.Field<string>("OrderNumber") == order.Field<string>("OrderNumber"));
                if (gaFound != null)
                {
                    order.SetField("Source", gaFound.Field<string>("Source"));
                    order.SetField("Medium", gaFound.Field<string>("Medium"));
                    order.SetField("Campaign", gaFound.Field<string>("Campaign"));
                }
            }

            // Loop through items and UPSERT into Salesforce
            if (!Salesforce.CreateJob("Opportunity", "AXIS_ID__c"))
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Unable to create job", ErrorCode = "ERROR" });
                return;
            }
            // Get the default Account Id
            JObject account = JObject.Parse(Salesforce.HttpGet("SELECT+Id+FROM+Account+WHERE+Name='AXIS+Integration'"));
            DefaultAccountId = account["records"][0]["Id"].ToString();

            // Create batches of max 1000 records
            Payload = new StringBuilder();
            RecordCount = 0;
            Salesforce.BatchCount = 0;
            bool firstTime = true;
            foreach (DataRow dr in orders.Rows)
            {
                BuildOpportunity(dr, firstTime);
                firstTime = false;
                RecordCount++;

                if (RecordCount >= 1000)
                {
                    byte[] pushedBytes = Encoding.ASCII.GetBytes(Payload.ToString());
                    if (!Salesforce.AddBatch(pushedBytes))
                    {
                        // Error
                    }
                    RecordCount = 0;
                    Payload = new StringBuilder();
                    firstTime = true;
                }
            }
            if (RecordCount > 0)
            {
                byte[] pushedBytes = Encoding.ASCII.GetBytes(Payload.ToString());
                if (!Salesforce.AddBatch(pushedBytes))
                {
                    // Error
                }
            }
            if (!Salesforce.UpdateJobStatus("Closed"))
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Unable to close job: " + Salesforce.JobId, ErrorCode = "ERROR" });
                return;
            }

            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Record Count: " + orders.Rows.Count + ", Batch Count: " + Salesforce.BatchCount });
        }

        private void LoadOrderLines()
        {
            DateTime endDate = DateTime.Now;
            if (Parms.ContainsKey("date"))
            {
                endDate = Convert.ToDateTime(Parms["date"]);
            }

            // Retrieve orderlines for the last 2 days
            List<SqlParameter> sqlParms = new List<SqlParameter>();
            SqlParameter sqlParm = new SqlParameter("@endDate", SqlDbType.DateTime);
            sqlParm.Value = endDate;
            sqlParms.Add(sqlParm);
            DataTable orderlines;
            try
            {
                orderlines = SQLUtilities.ExecuteReadStoredProcedure("axisdiplomat", "dbo.ng_GetOrderLinesForSalesforce",
                    sqlParms, "sfdata", 60).Tables[0];
            }
            catch (Exception e)
            {
                StandardFunctions.WriteException(e);
                return;
            }

            // Loop through items and UPSERT into Salesforce
            if (!Salesforce.CreateJob("OpportunityLineItem", "External_ID__c"))
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Unable to create job", ErrorCode = "ERROR" });
                return;
            }

            // Create batches of max 1000 records
            Payload = new StringBuilder();
            RecordCount = 0;
            Salesforce.BatchCount = 0;
            bool firstTime = true;
            foreach (DataRow dr in orderlines.Rows)
            {
                if (string.IsNullOrEmpty(dr["OrderNumber"].ToString())
                    || string.IsNullOrEmpty(dr["UniqueLineID"].ToString()))
                {
                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Order Number or Line Number is invalid.", ErrorCode = "ERROR" });
                    continue;
                }

                BuildLineItem(dr, firstTime);
                firstTime = false;
                RecordCount++;

                if (RecordCount >= 1000)
                {
                    byte[] pushedBytes = Encoding.ASCII.GetBytes(Payload.ToString());
                    if (!Salesforce.AddBatch(pushedBytes))
                    {
                        // Error
                    }
                    RecordCount = 0;
                    Payload = new StringBuilder();
                    firstTime = true;
                }
            }
            if (RecordCount > 0)
            {
                byte[] pushedBytes = Encoding.ASCII.GetBytes(Payload.ToString());
                if (!Salesforce.AddBatch(pushedBytes))
                {
                    // Error
                }
            }
            if (!Salesforce.UpdateJobStatus("Closed"))
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Unable to close job: " + Salesforce.JobId, ErrorCode = "ERROR" });
                return;
            }

            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Record Count: " + orderlines.Rows.Count + ", Batch Count: " + Salesforce.BatchCount });
        }

        private void LoadAccounts()
        {
            DateTime endDate = DateTime.Now;
            if (Parms.ContainsKey("date"))
            {
                endDate = Convert.ToDateTime(Parms["date"]);
            }
            bool isCreditAcc = false;
            if (Parms.ContainsKey("id"))
            {
                isCreditAcc = true;
            }

            // Retrieve accounts for the last hour
            List<SqlParameter> sqlParms = new List<SqlParameter>();
            SqlParameter sqlParm = new SqlParameter("@endDate", SqlDbType.DateTime);
            sqlParm.Value = endDate;
            sqlParms.Add(sqlParm);
            sqlParm = new SqlParameter("@isCreditAcc", SqlDbType.Bit);
            sqlParm.Value = isCreditAcc;
            sqlParms.Add(sqlParm);
            DataTable accounts;
            try
            {
                accounts = SQLUtilities.ExecuteReadStoredProcedure("axisdiplomat", "dbo.ng_GetAccountsForSalesforce",
                    sqlParms, "sfdata").Tables[0];
            }
            catch (Exception e)
            {
                StandardFunctions.WriteException(e);
                return;
            }

            // Loop through items and UPSERT into Salesforce
            if (!Salesforce.CreateJob("Account", "AXIS_ID__c"))
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Unable to create job", ErrorCode = "ERROR" });
                return;
            }

            // Get the default Record Type Id
            JObject account = JObject.Parse(Salesforce.HttpGet("SELECT+Id+FROM+RecordType+WHERE+Name='Business'+AND+SobjectType='Account'"));
            DefaultRecordTypeId = account["records"][0]["Id"].ToString();

            // Create batches of max 1000 records
            Payload = new StringBuilder();
            RecordCount = 0;
            Salesforce.BatchCount = 0;
            bool firstTime = true;
            foreach (DataRow dr in accounts.Rows)
            {
                BuildAccount(dr, firstTime);
                firstTime = false;
                RecordCount++;

                if (RecordCount >= 1000)
                {
                    byte[] pushedBytes = Encoding.ASCII.GetBytes(Payload.ToString());
                    if (!Salesforce.AddBatch(pushedBytes))
                    {
                        // Error
                    }
                    RecordCount = 0;
                    Payload = new StringBuilder();
                    firstTime = true;
                }
            }
            if (RecordCount > 0)
            {
                byte[] pushedBytes = Encoding.ASCII.GetBytes(Payload.ToString());
                if (!Salesforce.AddBatch(pushedBytes))
                {
                    // Error
                }
            }
            if (!Salesforce.UpdateJobStatus("Closed"))
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Unable to close job: " + Salesforce.JobId, ErrorCode = "ERROR" });
                return;
            }

            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Record Count: " + accounts.Rows.Count + ", Batch Count: " + Salesforce.BatchCount });
        }

        private void LoadContacts()
        {
            DateTime endDate = DateTime.Now;
            if (Parms.ContainsKey("date"))
            {
                endDate = Convert.ToDateTime(Parms["date"]);
            }

            // Retrieve contacts for the last hour
            List<SqlParameter> sqlParms = new List<SqlParameter>();
            SqlParameter sqlParm = new SqlParameter("@endDate", SqlDbType.DateTime);
            sqlParm.Value = endDate;
            sqlParms.Add(sqlParm);
            DataTable contacts;
            try
            {
                contacts = SQLUtilities.ExecuteReadStoredProcedure("axisdiplomat", "dbo.ng_GetContactsForSalesforce",
                    sqlParms, "sfdata", 60).Tables[0];
            }
            catch (Exception e)
            {
                StandardFunctions.WriteException(e);
                return;
            }

            // Loop through items and UPSERT into Salesforce          
            if (!Salesforce.CreateJob("Contact", "AXIS_ID__c"))
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Unable to create job", ErrorCode = "ERROR" });
                return;
            }

            // Get the default Account Id
            JObject account = JObject.Parse(Salesforce.HttpGet("SELECT+Id+FROM+Account+WHERE+Name='AXIS+Integration'"));
            DefaultAccountId = account["records"][0]["Id"].ToString();

            // Create batches of max 1000 records
            Payload = new StringBuilder();
            RecordCount = 0;
            Salesforce.BatchCount = 0;
            bool firstTime = true;
            foreach (DataRow dr in contacts.Rows)
            {
                BuildContact(dr, firstTime);
                firstTime = false;
                RecordCount++;

                if (RecordCount >= 1000)
                {
                    byte[] pushedBytes = Encoding.ASCII.GetBytes(Payload.ToString());
                    if (!Salesforce.AddBatch(pushedBytes))
                    {
                        // Error
                    }
                    RecordCount = 0;
                    Payload = new StringBuilder();
                    firstTime = true;
                }
            }
            if (RecordCount > 0)
            {
                byte[] pushedBytes = Encoding.ASCII.GetBytes(Payload.ToString());
                if (!Salesforce.AddBatch(pushedBytes))
                {
                    // Error
                }
            }
            if (!Salesforce.UpdateJobStatus("Closed"))
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Unable to close job: " + Salesforce.JobId, ErrorCode = "ERROR" });
                return;
            }

            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Record Count: " + contacts.Rows.Count + ", Batch Count: " + Salesforce.BatchCount });
        }

        private void LoadProducts()
        {
            DateTime endDate = DateTime.Now;
            if (Parms.ContainsKey("date"))
            {
                endDate = Convert.ToDateTime(Parms["date"]);
            }

            // Retrieve products for the last hour
            List<SqlParameter> sqlParms = new List<SqlParameter>();
            SqlParameter sqlParm = new SqlParameter("@endDate", SqlDbType.DateTime);
            sqlParm.Value = endDate;
            sqlParms.Add(sqlParm);
            DataTable products;
            try
            {
                products = SQLUtilities.ExecuteReadStoredProcedure("axisdiplomat", "dbo.ng_GetProductsForSalesforce",
                    sqlParms, "sfdata").Tables[0];
            }
            catch (Exception e)
            {
                StandardFunctions.WriteException(e);
                return;
            }

            // Loop through items and UPSERT into Salesforce          
            if (!Salesforce.CreateJob("Product2", "Axis_Id__c"))
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Unable to create job", ErrorCode = "ERROR" });
                return;
            }

            // Create batches of max 1000 records
            Payload = new StringBuilder();
            RecordCount = 0;
            Salesforce.BatchCount = 0;
            bool firstTime = true;
            foreach (DataRow dr in products.Rows)
            {
                BuildProduct(dr, firstTime);
                firstTime = false;
                RecordCount++;

                if (RecordCount >= 1000)
                {
                    byte[] pushedBytes = Encoding.ASCII.GetBytes(Payload.ToString());
                    if (!Salesforce.AddBatch(pushedBytes))
                    {
                        // Error
                    }
                    RecordCount = 0;
                    Payload = new StringBuilder();
                    firstTime = true;
                }
            }
            if (RecordCount > 0)
            {
                byte[] pushedBytes = Encoding.ASCII.GetBytes(Payload.ToString());
                if (!Salesforce.AddBatch(pushedBytes))
                {
                    // Error
                }
            }
            if (!Salesforce.UpdateJobStatus("Closed"))
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Unable to close job: " + Salesforce.JobId, ErrorCode = "ERROR" });
                return;
            }

            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Record Count: " + products.Rows.Count + ", Batch Count: " + Salesforce.BatchCount });
        }

        private void DeleteObject()
        {           
            Salesforce = new Salesforce();
            if (!Salesforce.Authenticate())
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Unable to authenticate to Salesforce", ErrorCode = "ERROR" });
                return;
            }
            // Delete
            bool isSuccess = Salesforce.HttpDelete(Parms["table"], Parms["id"]);

            Salesforce = null;
        }

        private void BuildOpportunity(DataRow row, bool isHeader)
        {
            if (isHeader)
            {
                string headerLine = "Name," +
                    "AXIS_ID__c," +
                    "Order_Source_Axis__c," +
                    "Account_AXIS_ID__c," +
                    "ReleasedDate__c," +
                    "ReceivedDate__c," +
                    "CloseDate," +
                    "StageName," +
                    "Type," +
                    "Invoice_Address_Line_1__c," +
                    "Invoice_Address_Line_2__c," +
                    "Invoice_Address_Line_3__c," +
                    "Invoice_Address_Line_4__c," +
                    "Invoice_Address_Line_5__c," +
                    "Invoice_Address_Line_6__c," +
                    "Invoice_Address_Line_7__c," +
                    "Delivery_Address_Line_1__c," +
                    "Delivery_Address_Line_2__c," +
                    "Delivery_Address_Line_3__c," +
                    "Delivery_Address_Line_4__c," +
                    "Delivery_Address_Line_5__c," +
                    "Delivery_Address_Line_6__c," +
                    "Delivery_Address_Line_7__c," +
                    "GA_Source2__c," +
                    "Source__c," +
                    "PPC_Campaign__c," +
                    "AccountId," +
                    "Payment_Method__c," +
                    "Suppliers_Involved__c," +
                    "Customer_Order_Count__c," +
                    "Last_Sync_Method__c";
                Payload.AppendLine(headerLine);              
            }

            string detailLine = "\"" + row.Field<string>("OrderNumber") + "\"," +
                "\"" + row.Field<string>("OrderNumber") + "\"," +
                "\"" + row.Field<string>("OrderSource") + "\"," +
                "\"" + row.Field<string>("CustomerReference") + "\"," +
                (row.Field<DateTime?>("ReleasedDate") == null ? "null," : "\"" + row.Field<DateTime>("ReleasedDate").ToString("s") + "\",") +
                (row.Field<DateTime?>("ReceivedDate") == null ? "null," : "\"" + row.Field<DateTime>("ReceivedDate").ToString("s") + "\",") +
                (row.Field<DateTime?>("ReleasedDate") == null ? "null," : "\"" + row.Field<DateTime>("ReleasedDate").ToString("s") + "\",") +
                "\"" + row.Field<string>("OrderStatus") + "\"," +
                "\"" + row.Field<string>("OrderType") + "\"," +
                "\"" + row.Field<string>("InvoiceLine1").Replace("\"", "") + "\"," +
                "\"" + row.Field<string>("InvoiceLine2").Replace("\"", "") + "\"," +
                "\"" + row.Field<string>("InvoiceLine3").Replace("\"", "") + "\"," +
                "\"" + row.Field<string>("InvoiceLine4").Replace("\"", "") + "\"," +
                "\"" + row.Field<string>("InvoiceLine5").Replace("\"", "") + "\"," +
                "\"" + row.Field<string>("InvoiceLine6").Replace("\"", "") + "\"," +
                "\"" + row.Field<string>("InvoiceLine7").Replace("\"", "") + "\"," +
                "\"" + row.Field<string>("DeliveryLine1").Replace("\"", "") + "\"," +
                "\"" + row.Field<string>("DeliveryLine2").Replace("\"", "") + "\"," +
                "\"" + row.Field<string>("DeliveryLine3").Replace("\"", "") + "\"," +
                "\"" + row.Field<string>("DeliveryLine4").Replace("\"", "") + "\"," +
                "\"" + row.Field<string>("DeliveryLine5").Replace("\"", "") + "\"," +
                "\"" + row.Field<string>("DeliveryLine6").Replace("\"", "") + "\"," +
                "\"" + row.Field<string>("DeliveryLine7").Replace("\"", "") + "\"," +
                (row.Field<string>("Source") == null ? "null," : "\"" + row.Field<string>("Source") + "\",") +
                (row.Field<string>("Medium") == null ? "null," : "\"" + row.Field<string>("Medium") + "\",") +
                (row.Field<string>("Campaign") == null ? "null," : "\"" + row.Field<string>("Campaign") + "\",") +
                DefaultAccountId + "," +
                "\"" + row.Field<string>("PaymentMethod") + "\"," +
                "\"" + row.Field<string>("Supplier") + "\"," +
                row.Field<int>("OrderCount") + "," +
                "\"Partial update\"";

            Payload.AppendLine(detailLine);
        }

        private void BuildLineItem(DataRow row, bool isHeader)
        {
            if (isHeader)
            {
                string headerLine = "Cost__c," +
                    "Description," +
                    "External_ID__c," +
                    "Opportunity_AXIS_ID__c," +
                    "Product_AXIS_ID__c," +
                    "Quantity," +
                    "Sales_Group__c," +
                    "Stock_reference__c," +
                    "UnitPrice," +
                    "Last_Sync_Method__c";
                Payload.AppendLine(headerLine);
            }

            string detailLine = row.Field<Decimal>("Cost").ToString() + "," +
            "\"" + row.Field<string>("Description") + "\"," +
            "\"" + row.Field<string>("OrderNumber") + "-" + row.Field<Decimal>("UniqueLineID").ToString() + "\"," +
            "\"" + row.Field<string>("OrderNumber") + "\"," +
            "\"" + row.Field<string>("ProductReference") + "\"," +
            row.Field<Decimal>("Quantity").ToString() + "," +
            "\"" + row.Field<string>("SalesGroupName") + "\"," +
            "\"" + row.Field<string>("StockReference") + "\"," +
            row.Field<Decimal>("UnitPrice").ToString() + "," +
            "\"Partial update\"";

            Payload.AppendLine(detailLine);
        }

        private void BuildAccount(DataRow row, bool isHeader)
        {
            if (isHeader)
            {
                string headerLine = "";
                if (Parms.ContainsKey("id"))
                {
                    headerLine = "AXIS_ID__c," +
                    "Application_Source__c," +
                    "RecordTypeId";
                }
                else
                {
                    headerLine = "AXIS_ID__c," +
                    "Axis_Last_Modified__c," +
                    "BillingPostalCode," +
                    "BillingStreet," +
                    "Credit_Status__c," +
                    "Customer_Group__c," +
                    "Fax," +
                    "Name," +
                    "Order_Source_Name__c," +
                    "Phone," +
                    "Phone_Other__c," +
                    "Primary_Email_address__c," +
                    "RecordTypeId," +
                    "Sage_Account_Number__c," +
                    "Last_Sync_Method__c";
                }
                Payload.AppendLine(headerLine);
            }

            string detailLine = "";
            if (Parms.ContainsKey("id"))
            {
                detailLine = "\"" + row.Field<string>("AXIS_ID") + "\"," +
                    "\"" + row.Field<string>("ApplicationSource") + "\"," +
                    DefaultRecordTypeId;
            }
            else
            {
                string address = (string.IsNullOrEmpty(row.Field<string>("Name")) ? "" : row.Field<string>("Name") + Environment.NewLine) +
                    (string.IsNullOrEmpty(row.Field<string>("Address1")) ? "" : row.Field<string>("Address1") + Environment.NewLine) +
                    (string.IsNullOrEmpty(row.Field<string>("Address2")) ? "" : row.Field<string>("Address2") + Environment.NewLine) +
                    (string.IsNullOrEmpty(row.Field<string>("Address3")) ? "" : row.Field<string>("Address3") + Environment.NewLine) +
                    (string.IsNullOrEmpty(row.Field<string>("Address4")) ? "" : row.Field<string>("Address4") + Environment.NewLine) +
                    (string.IsNullOrEmpty(row.Field<string>("Address5")) ? "" : row.Field<string>("Address5") + Environment.NewLine);

                detailLine = "\"" + row.Field<string>("AXIS_ID") + "\"," +
                    "\"" + DateTime.Now.ToString("s") + "\"," +
                    "\"" + row.Field<string>("Postcode") + "\"," +
                    "\"" + address + "\"," +
                    "\"" + row.Field<string>("CreditStatus") + "\"," +
                    "\"" + row.Field<string>("CustomerGroupName") + "\"," +
                    "\"" + row.Field<string>("Fax") + "\"," +
                    "\"" + row.Field<string>("Name") + "\"," +
                    "\"" + row.Field<string>("OrderSourceName") + "\"," +
                    "\"" + row.Field<string>("TelNo") + "\"," +
                    "\"" + row.Field<string>("AltTelNo") + "\"," +
                    "\"" + row.Field<string>("PrimaryEmail") + "\"," +
                    DefaultRecordTypeId + "," +
                    "\"" + row.Field<string>("SageNumber") + "\"," +
                    "\"Partial update\"";
            }

            Payload.AppendLine(detailLine);
        }

        private void BuildContact(DataRow row, bool isHeader)
        {
            if (isHeader)
            {
                string headerLine = "Account_AXIS_ID__c," +
                    "AccountId," +
                    "Axis_Email_Opt_Out__c," +
                    "AXIS_ID__c," +
                    "Axis_Last_Modified__c," +
                    "Customer_Email__c," +
                    "Customer_Group__c," +
                    "Email," +
                    "FirstName," +
                    "LastName," +
                    "Phone," +
                    "Title," +
                    "Last_Sync_Method__c";
                Payload.AppendLine(headerLine);
            }

            string detailLine = "\"" + row.Field<string>("AccountId") + "\"," +
                DefaultAccountId + "," +
                "" + row.Field<byte>("EmailOptOut").ToString() + "," +
                "\"" + row.Field<string>("AXIS_ID") + "-" + row.Field<string>("ContactNumber") + "\"," +
                DateTime.Now.ToString("s") + "," +
                "\"" + row.Field<string>("Email") + "\"," +
                "\"" + row.Field<string>("CustomerGroupName") + "\"," +
                "\"" + row.Field<string>("MarketingEmail") + "\"," +
                "\"" + row.Field<string>("Firstname") + "\"," +
                "\"" + row.Field<string>("Surname") + "\"," +
                "\"" + row.Field<string>("TelNo") + "\"," +
                "\"" + row.Field<string>("Title") + "\"," +
                "\"Partial update\"";

            Payload.AppendLine(detailLine);
        }

        private void BuildProduct(DataRow row, bool isHeader)
        {
            if (isHeader)
            {
                string headerLine = "Axis_Id__c," +
                    "Axis_Last_Modified__c," +
                    "Cost_Price__c," +
                    "Family," +
                    "IsActive," +
                    "Name," +
                    "Product_Group__c," +
                    "Product_Type__c," +
                    "ProductCode," +
                    "Sell_price__c," +
                    "Last_Sync_Method__c";
                Payload.AppendLine(headerLine);
            }

            string detailLine = "\"" + row.Field<string>("AXIS_ID") + "\"," +
                "\"" + DateTime.Now.ToString("s") + "\"," +
                (row.Field<Decimal?>("Cost") == null ? "0.00," : "\"" + row.Field<Decimal>("Cost").ToString() + "\",") +
                "\"" + row.Field<string>("ProductFamily") + "\"," +
                "True," +
                "\"" + row.Field<string>("Description") + "\"," +
                "\"" + row.Field<string>("ProductGroup") + "\"," +
                "\"" + row.Field<string>("ProductTypeName") + "\"," +
                "\"" + row.Field<string>("ProductCode") + "\"," +
                (row.Field<Decimal?>("Price") == null ? "0.00," : "\"" + row.Field<Decimal>("Price").ToString() + "\",") +
                "\"Partial update\"";

            Payload.AppendLine(detailLine);
        }

        private void UpdateJobStats()
        {
            List<SalesforceBatchJob> lsbj = EntityFunctions.GetSalesForceBatchJob(x => x.Status != "Closed" && x.Status != "Aborted");
            using (ngmdEntities db = new ngmdEntities())
            {
                foreach (SalesforceBatchJob sbj in lsbj)
                {
                    Salesforce.GetJob(sbj);
                    if (sbj.Status == "Closed")
                    {
                        if (sbj.RecordsFailed > 0 || sbj.BatchesFailed > 0)
                        {
                            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Batch Job failure: Job ID: " + sbj.JobId + ", Records Failed: " + sbj.RecordsFailed + ", Batches Failed: " + sbj.BatchesFailed, ErrorCode = "ERROR" });
                        }
                    }
                    db.Entry(sbj).State = EntityState.Modified;
                }
                db.SaveChanges();
            }
        }
    }
}
