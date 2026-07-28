using Google.Apis.Util;
using MailChimp.Net;
using MailChimp.Net.Core;
using Nest;
using netGiant.Intranet.DataLayer.CustomerData;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using Newtonsoft.Json;
using ngBatchProcesses.BusinessObjects.Shared;
using NGBP.DataAccessLayer.DataUtilities;
using Org.BouncyCastle.Bcpg.OpenPgp;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using MCNM = MailChimp.Net.Models;
using Status = MailChimp.Net.Models.Status;

namespace ngBatchProcesses.BusinessObjects.DataFeeds
{
    public class MailChimpFeed
    {
        public MailChimpFeed(Dictionary<string, string> parms)
        {
            Parms = parms;
            Type = Parms["type"];
            SubType = Parms["subtype"];
            WebsiteId = Int32.Parse(Parms["websiteid"]);
            ErrorHasOccurred = false;
            ConnName = "netgiantMasterData";
            StandardFunctions.WriteProcessStarted();
            Settings = Properties.Settings.Default;
            ErrorCount = 0;
            SuccessCount = 0;

            ApiKey = EntityFunctions.GetNgmdCMSEntry(WebsiteId, "MiscData", "MailChimpApiKey");
            StoreId = (Properties.Settings.Default.Environment == "Live" ? "Live_" : "Dev_") + EntityFunctions.GetNgmdCMSEntry(WebsiteId, "CommonData", "ShortSiteName");
            Mcm = new MailChimpManager(ApiKey);            
        }

        public bool ErrorHasOccurred { get; set; }
        public string ConnName { get; set; }
        Properties.Settings Settings { get; }
        public string Type { get; set; }
        public string SubType { get; set; }
        public int WebsiteId { get; set; }
        private string ActivityLogFileName { get; set; }
        public Dictionary<string, string> Parms { get; set; }
        public int ErrorCount { get; set; }
        public int SuccessCount { get; set; }

        public string ApiKey { get; set; }
        public string StoreId { get; set; }
        public string ListId { get; set; }
        public MailChimpManager Mcm { get; set; }
        public string[] WorkFlowId { get; set; } = new string[3];
        public string[] QueueId { get; set; } = new string[3];
        public string JourneyId { get; set; }
        public string StepId { get; set; }
        public MCNM.Product Prd { get; set; }

        public void ProcessFeed()
        {
            switch (SubType)
            {
                case "product":
                {
                    Task t = MaintainProductsAsync();
                    t.Wait();
                    break;
                }
                case "customer":
                {
                    Task t = MaintainCustomersAsync();
                    t.Wait();
                    break;
                }
                case "order":
                {
                    MaintainOrders();
                    break;
                }
                case "cart":
                {
                    Task t = MaintainCartsAsync();
                    t.Wait();
                    break;
                }
                case "list":
                {
                    Task t = MaintainListAsync();
                    t.Wait();
                    break;
                }
            }

            //Log in activity log
            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Success Count: " + SuccessCount + ", Error Count: " + ErrorCount });
            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
        }

        /// <summary>
        /// Retrieve all products from MailChimp, retrieve all products from the PMS
        /// </summary>
        /// <returns></returns>
        private async Task MaintainProductsAsync()
        {
            // Retrieve all MailChimp Product Id's
            //List<string> idlist = new List<string>();
            Dictionary<string, Decimal?> idlist = new Dictionary<string, Decimal?>();
            try
            {
                int offset = 0;
                bool moreAvailable = true;
                while (moreAvailable)
                {
                    var products = await Mcm.ECommerceStores.Products(StoreId).GetAllAsync(new QueryableBaseRequest
                        {
                            Limit = 1000,
                            Offset = offset,
                            FieldsToInclude = "products.id,products.variants.price"
                        })
                        .ConfigureAwait(false);

                    if (products.Count() > 0)
                    {
                        foreach (MCNM.Product p in products)
                        {
                            idlist.Add(p.Id, p.Variants[0].Price);
                        }
                        //idlist = products.ToDictionary(x => x.Id, x => x.Variants[0].Price);
                    }

                    if (products.Count() == 1000)
                    {
                        offset += 1000;
                    }
                    else
                    {
                        moreAvailable = false;
                    }
                }
            }
            catch (MailChimpException e)
            {
                StandardFunctions.WriteException(e);
            }
            catch (Exception e)
            {
                StandardFunctions.WriteException(e);
            }
            
            if (Parms["action"] == "delete")
            {
                // Deletion process
                foreach (KeyValuePair<string, Decimal?> entry in idlist)
                {
                    try
                    {
                        await Mcm.ECommerceStores.Products(StoreId).DeleteAsync(entry.Key).ConfigureAwait(false);
                    }
                    catch (MailChimpException e)
                    {
                        StandardFunctions.WriteException(e);
                        StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
                        return;
                    }
                    catch (Exception e)
                    {
                        StandardFunctions.WriteException(e);
                        StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
                        return;
                    }
                }
                return;
            }

            // Add process
            List<SqlParameter> sqlParms = new List<SqlParameter>();
            SqlParameter sqlParm = new SqlParameter("@WebsiteId", SqlDbType.Int);
            sqlParm.Value = WebsiteId;
            sqlParms.Add(sqlParm);
            DataTable prResults;
            try
            {
                prResults = SQLUtilities.ExecuteReadStoredProcedure("netgiantmasterdata", "ngmd.GetMailChimpFeed_Product",
                    sqlParms, "googledata").Tables[0];
            }
            catch (Exception e)
            {
                StandardFunctions.WriteException(e);
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
                return;
            }

            MCNM.Product product;
            int counteradd = 0;
            int counterupd = 0;
            string id = "";
            
            foreach (DataRow dr in prResults.Rows)
            {
                if (id == dr["id"].ToString())
                {
                    continue;
                }
                id = dr["id"].ToString();
                bool isNew = false;
                KeyValuePair<string, decimal?> kvp = idlist.FirstOrDefault(x => x.Key == id);
                if (!idlist.ContainsKey(id))
                {
                    isNew = true;
                }

                MCNM.Variant v = new MCNM.Variant
                {
                    Id = id,                                        // Stock Reference
                    Title = dr["description"].ToString(),           // PMS Description/Title
                    Sku = dr["mpn"].ToString(),                     // PMS Part Number
                    Url = dr["link"].ToString(),                    // Website URL
                    ImageUrl = dr["image_link"].ToString(),         // Website Image URL
                    Price = Decimal.Parse(dr["price"].ToString())   // PMS Price
                };
                IList<MCNM.Variant> variant = new List<MCNM.Variant>();
                variant.Add(v);

                MCNM.Product p = new MCNM.Product
                {
                    Id = id,                                        // Stock Reference
                    Title = dr["description"].ToString(),           // PMS Description/Title
                    Url = dr["link"].ToString(),                    // Website URL
                    Description = dr["description"].ToString(),     // PMS Description/Title
                    ImageUrl = dr["image_link"].ToString(),         // Website Image URL
                    Type = dr["product_type"].ToString(),           // Google Product Type
                    Vendor = dr["brand"].ToString(),                // Manufacturer
                    Variants = variant                              // Variants
                };

                try
                {
                    if (isNew)
                    {
                        product = await Mcm.ECommerceStores.Products(StoreId).AddAsync(p).ConfigureAwait(false);
                        counteradd += 1;
                    }
                    else
                    {
                        if (kvp.Value != v.Price)
                        {
                            product = await Mcm.ECommerceStores.Products(StoreId).UpdateAsync(id, p).ConfigureAwait(false);
                            counterupd += 1;
                        }
                    }
                }
                catch (MailChimpException e)
                {
                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR processing product id: " + id, ErrorCode = "ERROR" });
                    StandardFunctions.WriteException(e);
                    continue;
                }
                catch (Exception e)
                {
                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR processing product id: " + id, ErrorCode = "ERROR" });
                    StandardFunctions.WriteException(e);
                    continue;
                }
            }
            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Total products added: " + counteradd });
            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Total products updated: " + counterupd });
            SuccessCount = counteradd + counterupd;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private async Task MaintainCustomersAsync()
        {
            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Action: " + Parms["action"] });
            if (Parms["action"] == "delete")
            {
                string emailFilter = "";
                if (Parms.ContainsKey("where"))
                {
                    emailFilter = Parms["where"];
                }
                // Retrieve Customers
                List<string> idlist = new List<string>();
                try
                {
                    int offset = 0;
                    bool moreAvailable = true;
                    while (moreAvailable)
                    {
                        var customers = await Mcm.ECommerceStores.Customers(StoreId).GetAllAsync(new OrderRequest()
                            {
                                Limit = 1000,
                                Offset = offset
                            })
                            .ConfigureAwait(false);

                        if (customers.Count() > 0)
                        {
                            idlist.AddRange(customers
                                .Where(x => x.EmailAddress == emailFilter || emailFilter == "")
                                .Select(x => x.Id));
                        }

                        if (customers.Count() == 1000)
                        {
                            offset += 1000;
                        }
                        else
                        {
                            moreAvailable = false;
                        }
                    }
                }
                catch (MailChimpException e)
                {
                    StandardFunctions.WriteException(e);
                }
                catch (Exception e)
                {
                    StandardFunctions.WriteException(e);
                }
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "MailChimp retrieval OK, number retrieved: " + idlist.Count });

                // Deletion process
                foreach (string customerid in idlist)
                {
                    try
                    {
                        await Mcm.ECommerceStores.Customers(StoreId).DeleteAsync(HttpUtility.HtmlEncode(customerid)).ConfigureAwait(false);
                    }
                    catch (MailChimpException e)
                    {
                        if (e.Status == 400)
                        {
                            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Error deleting customer: customer id: " + customerid + ". Processing continues." });
                            ErrorCount += 1;
                        }
                        else
                        {
                            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "**ERROR** processing customer id: " + customerid + ". Processing stopped." });
                            StandardFunctions.WriteException(e);
                            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
                            return;
                        }
                    }
                    catch (Exception e)
                    {
                        StandardFunctions.WriteException(e);
                        StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
                        return;
                    }
                }

                return;
            }
            return;
        }

        /// <summary>
        /// This routine intended for an initial load of pre-existing orders/customers based on orders dating back x months
        /// </summary>
        /// <returns></returns>
        private void MaintainOrders()
        {
            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Action: " + Parms["action"] });
            if (Parms["action"] == "add")
            {
                DateTime fromDate = DateTime.Now.AddDays(-14);

                AddOrders(fromDate);
            }
            if (Parms["action"] == "delete")
            {
                Task t = DeleteOrdersAsync();
                t.Wait();
            }
            if (Parms["action"] == "load")
            {
                string date_string = Parms["period"];
                DateTime fromDate = DateTime.ParseExact(date_string, "yyyy-MM-dd HH:mm:ss", null);

                 AddOrders(fromDate);
            }
            if (Parms["action"] == "lapsed")
            {
                Task t = MaintainLapsedCustomersAsync();
                t.Wait();
            }
            if (Parms["action"] == "predict")
            {
                Task t = MaintainOrderPredictionAsync();
                t.Wait();
            }
        }

        private void AddOrders(DateTime from)
        {
            // Retrieve Orders
            List<SqlParameter> sqlParms = new List<SqlParameter>();
            SqlParameter sqlParm = new SqlParameter("@StartDate", SqlDbType.DateTime);
            sqlParm.Value = from;
            sqlParms.Add(sqlParm);
            sqlParm = new SqlParameter("@WebsiteId", SqlDbType.Int);
            sqlParm.Value = WebsiteId;
            sqlParms.Add(sqlParm);

            DataTable orders;
            try
            {
                orders = SQLUtilities.ExecuteReadStoredProcedure("axisdiplomat", "dbo.ng_GetOrdersForMailChimp",
                    sqlParms, "orders").Tables[0];
            }
            catch (Exception e)
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR Attempting to Run SP: dbo.ng_GetOrdersForMailChimp : " + e.Message, ErrorCode = "ERROR" });
                StandardFunctions.WriteException(e);
                ErrorHasOccurred = true;
                return;
            }

            // Insertion process
            string id = "";
            int orderCount = 0;
            MCNM.Order order = null;
            foreach (DataRow dr in orders.Rows)
            {

                if (id != dr["OrderNumber"].ToString())
                {
                    if (order != null)
                    {
                        Task t = AddOrderAsync(order, orderCount);
                        t.Wait();
                    }

                    id = dr["OrderNumber"].ToString();
                    order = new MCNM.Order
                    {
                        Id = id,
                        CurrencyCode = MailChimp.Net.Core.CurrencyCode.GBP,
                        OrderTotal = Convert.ToDecimal(dr["OrderTotal"].ToString()),
                        ProcessedAtForeign = dr["OrderDate"].ToString(),
                        UpdatedAtForeign = dr["OrderDate"].ToString(),
                        DiscountTotal = Convert.ToDecimal(dr["VoucherAmt"].ToString()),
                        Customer = new MailChimp.Net.Models.Customer
                        {
                            //Id = dr["CustomerAccountNumber"].ToString().Replace("/", "-"),
                            //EmailAddress = Properties.Settings.Default.Environment == "Live"
                            //    ? dr["EmailAddress"].ToString()
                            //    : "stuart.deavall@netgiant.com",
                            EmailAddress = dr["EmailAddress"].ToString(),
                            OptInStatus = false
                        }
                    };
                    StringBuilder sb = new StringBuilder(order.Customer.EmailAddress);
                    order.Customer.Id = sb
                        .Replace("@", "-")
                        .Replace(".", string.Empty)
                        .Replace("_", string.Empty)
                        .Replace("+", string.Empty)
                        .ToString();
                    if (!string.IsNullOrEmpty(dr["CampaignId"].ToString()))
                    {
                        order.CampaignId = dr["CampaignId"].ToString();
                    }
                    List<string> name = dr["Name"].ToString().Split(' ').ToList();
                    order.Customer.FirstName = "";
                    order.Customer.LastName = "";
                    if (name.Count > 0)
                    {
                        order.Customer.FirstName = name[0];
                        if (name.Count > 1)
                        {
                            order.Customer.LastName = name[name.Count - 1];
                        }
                    }

                    orderCount = Int32.Parse(dr["OrderCount"].ToString());
                }

                order.Lines.Insert(0, new MCNM.Line
                {
                    Id = id + "-" + dr["LineId"].ToString(),
                    ProductId = dr["ProductId"].ToString(),
                    ProductVariantId = dr["ProductId"].ToString(),
                    //ProductVariantId = "Dummy",
                    Quantity = Int32.Parse(dr["Quantity"].ToString()),
                    Price = Convert.ToDecimal(dr["Price"].ToString())
                });
            }
            if (order != null)
            {
                Task t = AddOrderAsync(order, orderCount);
                t.Wait();
            }

            return;
        }

        private async Task AddOrderAsync(MCNM.Order order, int orderCount)
        {
            MCNM.Order newOrder;
            //int i = 0;
            //while (i < 2)
            //{
            try
            {
                newOrder = await Mcm.ECommerceStores.Orders(StoreId).AddAsync(order).ConfigureAwait(false);
                SuccessCount += 1;
                //i = 2;

                // Trigger lapsed customer workflows
                //if (Parms["action"] == "add")
                //{
                //    Task t = MaintainLapsedCustomersAsync(order, orderCount);
                //    t.Wait();                        
                //}
            }
            catch (MailChimpException e)
            {
                if (e.Status == 400)
                {
                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Error adding order: Order Number " + order.Id + ". " + e.Message + ". Processing continues", ErrorCode = "WARNING" });
                    ErrorCount += 1;
                    //i += 1;
                    //order.Customer.Id = order.Customer.Id + "-" + DateTime.Now.ToString("yyyyMMddhhmmss");
                }
                else
                {
                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "**ERROR** processing order id: " + order.Id + ". Processing stopped", ErrorCode = "ERROR" });
                    StandardFunctions.WriteException(e);
                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
                    return;
                }
            }
            catch (Exception e)
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "**ERROR** processing order id: " + order.Id + ". Processing stopped" });
                StandardFunctions.WriteException(e);
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
                return;
            }
            //}

            return;
        }

        private async Task DeleteOrdersAsync()
        {
            // Retrieve Orders
            List<string> idlist = new List<string>();
            try
            {
                int offset = 0;
                bool moreAvailable = true;
                while (moreAvailable)
                {
                    var orders = await Mcm.ECommerceStores.Orders(StoreId).GetAllAsync(new OrderRequest()
                        {
                            Limit = 1000,
                            Offset = offset,
                            FieldsToInclude = "orders.id"
                        })
                        .ConfigureAwait(false);

                    if (orders.Count() > 0)
                    {
                        idlist.AddRange(orders.Select(x => x.Id));
                    }

                    if (orders.Count() == 1000)
                    {
                        offset += 1000;
                    }
                    else
                    {
                        moreAvailable = false;
                    }
                }
            }
            catch (MailChimpException e)
            {
                StandardFunctions.WriteException(e);
            }
            catch (Exception e)
            {
                StandardFunctions.WriteException(e);
            }

            // Deletion process
            foreach (string orderid in idlist)
            {
                try
                {
                    await Mcm.ECommerceStores.Orders(StoreId).DeleteAsync(orderid).ConfigureAwait(false);
                }
                catch (MailChimpException e)
                {
                    StandardFunctions.WriteException(e);
                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
                    return;
                }
                catch (Exception e)
                {
                    StandardFunctions.WriteException(e);
                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
                    return;
                }
            }

            return;
        }

        private async Task MaintainLapsedCustomersAsync()
        {
            ListId = EntityFunctions.GetNgmdCMSEntry(WebsiteId, "MiscData", "MailChimpListId");
            for (int i = 0; i < 3; i++)
            {
                string ids = EntityFunctions.GetConfigurationSetting("BatchProgramSetting", "MailChimpLapsedQId" + (i + 1).ToString(), WebsiteId);
                if (ids.Contains("#"))
                {
                    WorkFlowId[i] = ids.Split('#')[0];
                    QueueId[i] = ids.Split('#')[1];
                }
            }
            int countTrigger = Int32.Parse(Parms["where"]);

            // Remove tags set on previous day
            List<SqlParameter> sqlParms = new List<SqlParameter>();
            SqlParameter sqlParm = new SqlParameter("@Days", SqlDbType.Int);
            sqlParm.Value = Int32.Parse(Parms["period"]) + 1;
            sqlParms.Add(sqlParm);
            sqlParm = new SqlParameter("@WebsiteId", SqlDbType.Int);
            sqlParm.Value = WebsiteId;
            sqlParms.Add(sqlParm);
            sqlParm = new SqlParameter("@CountTrigger", SqlDbType.Int);
            sqlParm.Value = countTrigger;
            sqlParms.Add(sqlParm);

            DataTable orders;
            try
            {
                orders = SQLUtilities.ExecuteReadStoredProcedure("axisdiplomat", "dbo.ng_GetLapsedCustomersForMailChimp",
                    sqlParms, "orders").Tables[0];
            }
            catch (Exception e)
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR Attempting to Run SP: dbo.ng_GetLapsedCustomersForMailChimp : " + e.Message, ErrorCode = "ERROR" });
                StandardFunctions.WriteException(e);
                ErrorHasOccurred = true;
                return;
            }

            foreach (DataRow dr in orders.Rows)
            {
                string email = dr["EmailAddress"].ToString();
                if (Properties.Settings.Default.Environment != "Live")
                {
                    email = "stuart.deavall@netgiant.com";
                }
                try
                {
                    MCNM.Member m = await Mcm.Members.GetAsync(ListId, email);
                    if (countTrigger == 3)
                    {
                        int i = m.Tags.FindIndex(x => x.Name == "IsLapsed03");
                        if (i >= 0)
                        {
                            m.Tags.Remove(m.Tags[i]);
                        }
                        await Mcm.Members.AddOrUpdateAsync(ListId, m);
                    }                    
                }
                catch (Exception e) { }
            }

            // Deal with todays lapsed customers
            sqlParms = new List<SqlParameter>();
            sqlParm = new SqlParameter("@Days", SqlDbType.Int);
            sqlParm.Value = Int32.Parse(Parms["period"]);
            sqlParms.Add(sqlParm);
            sqlParm = new SqlParameter("@WebsiteId", SqlDbType.Int);
            sqlParm.Value = WebsiteId;
            sqlParms.Add(sqlParm);
            sqlParm = new SqlParameter("@CountTrigger", SqlDbType.Int);
            sqlParm.Value = Int32.Parse(Parms["where"]);
            sqlParms.Add(sqlParm);

            try
            {
                orders = SQLUtilities.ExecuteReadStoredProcedure("axisdiplomat", "dbo.ng_GetLapsedCustomersForMailChimp",
                    sqlParms, "orders").Tables[0];
            }
            catch (Exception e)
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR Attempting to Run SP: dbo.ng_GetLapsedCustomersForMailChimp : " + e.Message, ErrorCode = "ERROR" });
                StandardFunctions.WriteException(e);
                ErrorHasOccurred = true;
                return;
            }

            foreach (DataRow dr in orders.Rows)
            {
                string email = dr["EmailAddress"].ToString();
                if (Properties.Settings.Default.Environment != "Live")
                {
                    email = "stuart.deavall@netgiant.com";
                }
                try
                {
                    MCNM.Member m = await Mcm.Members.GetAsync(ListId, email);
                    if (countTrigger == 3)
                    {
                        int i = m.Tags.FindIndex(x => x.Name == "IsLapsed03");
                        if (i < 0)
                        {
                            MCNM.Tags tags = new MCNM.Tags();
                            tags.MemberTags.Add(new MCNM.Tag() {
                                Name = "IsLapsed03",
                                Status = "active"
                            });
                            await Mcm.Members.AddTagsAsync(ListId, email, tags);
                        }                        
                    }
                    else
                    {
                        try
                        {
                            await Mcm.AutomationEmailQueues.AddSubscriberAsync(WorkFlowId[countTrigger - 1], QueueId[countTrigger - 1], email);
                            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Added to workflow: " + countTrigger.ToString() + ": " + email });
                        }
                        catch (Exception e) { }
                    }
                }
                catch (Exception e) { }
            }
        }

        private async Task MaintainOrderPredictionAsync()
        {
            ListId = EntityFunctions.GetNgmdCMSEntry(WebsiteId, "MiscData", "MailChimpListId");

            string ids = EntityFunctions.GetConfigurationSetting("BatchProgramSetting", "MailChimpOrderPredictionQId", WebsiteId);
            if (ids.Contains("#"))
            {
                JourneyId = ids.Split('#')[0];
                StepId = ids.Split('#')[1];
            }

            // Deal with customers with a NOD of today
            List<SqlParameter> sqlParms = new List<SqlParameter>();
            SqlParameter sqlParm = new SqlParameter("@DaysOffset", SqlDbType.Int);
            sqlParm.Value = Int32.Parse(Parms["period"]);
            sqlParms.Add(sqlParm);
            sqlParm = new SqlParameter("@WebsiteId", SqlDbType.Int);
            sqlParm.Value = WebsiteId;
            sqlParms.Add(sqlParm);

            DataTable customers;
            try
            {
                customers = SQLUtilities.ExecuteReadStoredProcedure("axisdiplomat", "dbo.ng_GetPredictedNextOrderDate",
                    sqlParms, "orders").Tables[0];
            }
            catch (Exception e)
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR Attempting to Run SP: dbo.ng_GetPredictedNextOrderDate : " + e.Message, ErrorCode = "ERROR" });
                StandardFunctions.WriteException(e);
                ErrorHasOccurred = true;
                return;
            }

            foreach (DataRow dr in customers.Rows)
            {
                string email = dr["EmailAddress"].ToString();
                if (Properties.Settings.Default.Environment != "Live")
                {
                    email = "stuart.deavall@netgiant.com";
                }
                try
                {
                    StandardFunctions.SetTlsVersion();
                    var client = new RestClient("https://us16.api.mailchimp.com/3.0/");
                    var request = new RestRequest("customer-journeys/journeys/" + JourneyId + "/steps/" + StepId + "/actions/trigger")
                    {
                        Authenticator = new HttpBasicAuthenticator("user", ApiKey)
                    }
                        .AddJsonBody("{ \"email_address\": \"" + email + "\" }");
                    var response = await client.ExecuteAsync(request, RestSharp.Method.Post);
                    if (response.IsSuccessful)
                    {
                        SuccessCount += 1;
                    } 
                    else 
                    {
                        StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR Attempting MailChimp POST: " + response.Content, ErrorCode = "WARNING" });
                        ErrorCount += 1;
                    }
                }
                catch (Exception e) 
                {
                    StandardFunctions.WriteException(e);
                }
            }
        }

        /// <summary>
        /// Retrieve all carts from MailChimp and delete those that are older the x days.
        /// </summary>
        /// <returns></returns>
        private async Task MaintainCartsAsync()
        {
            // Retrieve all MailChimp Cart Id's

            List<string> idlist = new List<string>();
            try
            {
                int offset = 0;
                bool moreAvailable = true;
                while (moreAvailable)
                {
                    var carts = await Mcm.ECommerceStores.Carts(StoreId).GetAllAsync(new QueryableBaseRequest
                    {
                        Limit = 1000
                        , Offset = offset
                        , FieldsToInclude = "carts.id"
                    })
                    .ConfigureAwait(false);

                    if (carts.Count() > 0)
                    {
                        idlist.AddRange(carts.Select(x => x.Id));
                    }

                    if (carts.Count() == 1000)
                    {
                        offset += 1000;
                    }
                    else
                    {
                        moreAvailable = false;
                    }
                }
            }
            catch (MailChimpException e)
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "MailChimp error: " + e.Detail + " " + e.StackTrace });
            }
            catch (Exception e)
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "MailChimp error: " + e.Message + " " + e.StackTrace });
            }

            DateTime cutoff = DateTime.Now.AddDays(int.Parse(Parms["period"]) * -1);
            StandardFunctions.SetTlsVersion();
            foreach (string id in idlist)
            {
                try
                {
                    MCNM.Cart cart = await Mcm.ECommerceStores.Carts(StoreId).GetAsync(id).ConfigureAwait(false);
                    if (cart.UpdatedAt < cutoff)
                    {
                        try
                        {
                            await Mcm.ECommerceStores.Carts(StoreId).DeleteAsync(id).ConfigureAwait(false);
                        }
                        catch (Exception e)
                        {
                            StandardFunctions.WriteException(e);
                        }
                    }
                }
                catch (Exception e)
                {
                    StandardFunctions.WriteException(e, "WARNING");
                }
            }
        }

        /// <summary>
        /// Retrieve the MailChimp list for a website and replace the list for that website in the SQL table 
        /// </summary>
        /// <returns></returns>
        private async Task MaintainListAsync()
        {
            // Retrieve the MailChimp List
            string listid = EntityFunctions.GetNgmdCMSEntry(WebsiteId, "MiscData", "MailChimpListId");
            List<StagingMailingList> lml = new List<StagingMailingList>();
            try
            {
                int offset = 0;
                bool moreAvailable = true;
                while (moreAvailable)
                {
                    var members = await Mcm.Members.GetAllAsync(listid, new MemberRequest
                    {
                        Status = Status.Subscribed,
                        Limit = 1000,
                        Offset = offset,
                        FieldsToInclude = "members.email_address"
                    }).ConfigureAwait(false);

                    if (members.Count() > 0)
                    {
                        lml.AddRange(members.Select(x => new StagingMailingList
                        {
                            MailingListId = 0,
                            WebsiteFk = WebsiteId,
                            EmailAddress = x.EmailAddress.ToLower()
                        }));
                    }

                    if (members.Count() == 1000)
                    {
                        offset += 1000;
                    }
                    else
                    {
                        moreAvailable = false;
                    }
                }
            }
            catch (MailChimpException e)
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "MailChimp error: " + e.Detail + " " + e.StackTrace });
            }
            catch (Exception e)
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "MailChimp error: " + e.Message + " " + e.StackTrace });
            }

            // Refresh StagingMailingList table
            if (lml.Count > 0)
            {
                try
                {
                    if (StandardFunctions.BulkInsertMailingList(lml))
                    {
                        List<SqlParameter> sqlParms = new List<SqlParameter>();
                        SqlParameter sqlParm = new SqlParameter("@WebsiteId", SqlDbType.VarChar);
                        sqlParm.Value = WebsiteId;
                        sqlParms.Add(sqlParm);

                        SQLUtilities.ExecuteStoredProcedure("customersqldata", "dbo.UpdateMailingList", sqlParms);
                    }
                    else
                    {
                        StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "DATABASE ERROR: error during insertion loop", ErrorCode = "ERROR" });
                        ErrorHasOccurred = true;
                    }
                }
                catch (Exception ex)
                {
                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "DATABASE ERROR: error during insertion loop", ErrorCode = "ERROR" });
                    StandardFunctions.WriteException(ex);
                    ErrorHasOccurred = true;
                }

                if (!ErrorHasOccurred)
                {
                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = lml.Count + " records successfully added" });
                }
            }
            else
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "DATABASE ERROR: unable to delete old mailing list", ErrorCode = "ERROR" });
                ErrorHasOccurred = true;
            }

        }
    }
}
