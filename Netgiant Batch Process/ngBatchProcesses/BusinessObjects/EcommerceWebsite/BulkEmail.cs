using netGiant.Intranet.DataLayer.NetgiantMasterData;
using ngBatchProcesses.BusinessObjects.Shared;
using NGBP.DataAccessLayer.DataUtilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ngBatchProcesses.BusinessObjects.EcommerceWebsite
{
    public class BulkEmail
    {
        public BulkEmail(Dictionary<string, string> parms)
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

        public Dictionary<string, string> Parms { get; set; }
        public string SubType { get; set; }
        public string Action { get; set; }
        public DateTime LastRunDate { get; set; }
        public bool ErrorOccured { get; set; } = false;
        public string[] EmailTemplate { get; set; } = new string[3];

        public void Process()
        {
            switch (SubType)
            {
                // This option no longer in use
                case "heldorders":
                    {
                        ProcessHeldOrders();
                        break;
                    }
                case "backorders":
                    {
                        ProcessBackOrders();
                        break;
                    }
                default:
                    {
                        break;
                    }
            }

            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
        }

        private void ProcessHeldOrders()
        {
            bool isBankHoliday = Convert.ToBoolean(EntityFunctions.GetNgmdCMSEntry(1, "CommonData", "DeliveryDateIsOverridden"));
            DateTime now = DateTime.Now;
            if (now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday || isBankHoliday)
            {
                return;
            }
            DateTime lastRunDate = Convert.ToDateTime((string)Properties.Settings.Default["HeldOrdersEmailLastRun"]);
            string[] emailFrom = new string[4];
            for (int i = 1; i < 4; i++)
            {
                EmailTemplate[i] = EntityFunctions.GetNgmdCMSEntry(i, "EmailData", "HeldOrderEmail");
                emailFrom[i] = EntityFunctions.GetNgmdCMSEntry(i, "CheckoutData", "SalesEmail");
            }
            LastRunDate = lastRunDate.Date.AddHours(17).AddMinutes(30);

            List<SqlParameter> sqlParms = new List<SqlParameter>();
            SqlParameter sqlParm = new SqlParameter("@Type", SqlDbType.VarChar);
            sqlParm.Value = "HO";
            sqlParms.Add(sqlParm);
            if (Parms.ContainsKey("period"))
            {
                sqlParm = new SqlParameter("@EndDate", SqlDbType.DateTime);
                sqlParm.Value = DateTime.Now.AddDays(-1).Date.AddHours(17).AddMinutes(30);
                sqlParms.Add(sqlParm);
                sqlParm = new SqlParameter("@StartDate", SqlDbType.DateTime);
                sqlParm.Value = DateTime.Now.AddDays((int.Parse(Parms["period"]) + 1) * -1).Date.AddHours(17).AddMinutes(30);
                sqlParms.Add(sqlParm);
            }
            else
            {
                sqlParm = new SqlParameter("@EndDate", SqlDbType.DateTime);
                sqlParm.Value = DateTime.Now.Date.AddHours(17).AddMinutes(30);
                sqlParms.Add(sqlParm);
                sqlParm = new SqlParameter("@StartDate", SqlDbType.DateTime);
                sqlParm.Value = LastRunDate;
                sqlParms.Add(sqlParm);
            }

            DataTable result = SQLUtilities.ExecuteReadStoredProcedure("axisdiplomat", "dbo.ng_GetUnfulfilledOrders", sqlParms).Tables[0];

            string lastOrder = "";
            foreach (DataRow dr in result.Rows)
            {
                if (dr["OrderNumber"].ToString() != lastOrder)
                {
                    lastOrder = dr["OrderNumber"].ToString();
                    try
                    {
                        DateTime orderDate = Convert.ToDateTime(dr["OrderDate"].ToString());
                        string emailContent = EmailTemplate[int.Parse(dr["WebsiteId"].ToString())]
                            .Replace("[BillingName]", dr["Name"].ToString())
                            .Replace("[OrderRef]", dr["OrderNumber"].ToString())
                            .Replace("[YourOrderRef]", dr["CustomerOrderNumber"].ToString())
                            .Replace("[OrderDate]", orderDate.ToString("dd MMMM yyyy"))
                            .Replace("[SiteName]", dr["SiteName"].ToString());

                        List<string> toAddresses = new List<string>();
                        toAddresses.Add(dr["Email"].ToString());
                        string subject = "Order Update";

                        Email.SendEmail(toAddresses,
                            emailFrom[int.Parse(dr["WebsiteId"].ToString())],
                            subject,
                            emailContent,
                            true,
                            "",
                            "transactional.emails@netgiant.com");

                        StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Email sent for order " + dr["OrderNumber"].ToString() + " - " + dr["Email"].ToString() });

                    }
                    catch (Exception ex)
                    {
                        StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR processing order " + dr["OrderNumber"].ToString(), ErrorCode = "ERROR" });
                        StandardFunctions.WriteException(ex);
                    }
                }
            }

            // Update Last Run Date
            configurationSetting cs = EntityFunctions.GetConfigurationSettings(x => x.sectionName == "BatchProgramVariable" && x.settingName == "HeldOrdersEmailLastRun").FirstOrDefault();
            cs.settingValue = DateTime.Now.ToString();
            if (!EntityFunctions.SaveConfigurationSetting(cs))
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR. Could not save Last Run Date", ErrorCode = "ERROR" });
            }
        }

        private void ProcessBackOrders()
        {
            // Figure out dates - process doesn't run on Sat/Sun
            int startAdj = -5;
            int endAdj = -5;            
            DayOfWeek today = DateTime.Now.DayOfWeek;            
            if (today == DayOfWeek.Thursday)
            {
                startAdj = -5;
                endAdj = -3;
            }
            if (today == DayOfWeek.Friday)
            {
                startAdj = -3;
                endAdj = -3;
            }
            DateTime endDate = DateTime.Now.AddDays(endAdj).Date.AddHours(23).AddMinutes(59);
            DateTime startDate = DateTime.Now.AddDays(startAdj).Date;

            string[] emailFrom = new string[3];
            for (int i = 1; i < 3; i++)
            {
                EmailTemplate[i] = EntityFunctions.GetNgmdCMSEntry(i, "EmailData", "BackOrderEmail");
                emailFrom[i] = EntityFunctions.GetNgmdCMSEntry(i, "CheckoutData", "SalesEmail");
            }

            List<BackOrder> lbo = new List<BackOrder>();
            lbo = EntityFunctions.GetBackOrder(x => x.OrderDate >= startDate && x.OrderDate <= endDate && x.Lookup.LookupName == "Open");

            foreach (BackOrder bo in lbo)
            {
                try
                {
                    string emailContent = EmailTemplate[bo.WebsiteFK]
                        .Replace("[BillingName]", bo.CustomerName)
                        .Replace("[OrderRef]", bo.OrderReferenceNumber)
                        //.Replace("[YourOrderRef]", dr["CustomerOrderNumber"].ToString())
                        .Replace("[SiteName]", bo.Website.FriendlyName)
                        .Replace("[OrderDate]", bo.OrderDate.ToString("dd MMMM yyyy"))
                        .Replace("[ItemList]", BuildItemList(bo));


                    List<string> toAddresses = new List<string>();
                    toAddresses.Add(bo.CustomerEmailAddress);
                    string subject = bo.Website.FriendlyName + " Order Update - " + bo.OrderReferenceNumber;

                    Email.SendEmail(toAddresses,
                        emailFrom[bo.WebsiteFK],
                        subject,
                        emailContent,
                        true,
                        "",
                        "transactional.emails@netgiant.com");

                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Email sent for order " + bo.OrderReferenceNumber + " - " + bo.CustomerEmailAddress });

                }
                catch (Exception ex)
                {
                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR processing order " + bo.OrderReferenceNumber, ErrorCode = "ERROR" });
                    StandardFunctions.WriteException(ex);
                }
            }
        }

        private string BuildItemList(BackOrder bo)
        {
            StringBuilder sb = new StringBuilder();
            string linebreak = "";
            foreach (BackOrderItem boi in bo.BackOrderItem)
            {
                string replenishmentDate = "";
                if (boi.StockReplenishmentDate == null || boi.StockReplenishmentDate < DateTime.Now || boi.StockReplenishmentDate > DateTime.Now.AddMonths(2))
                {
                    replenishmentDate = "No due date";
                }
                else
                {
                    replenishmentDate = boi.StockReplenishmentDate?.ToString("dd MMMM yyyy");
                }

                sb.Append(linebreak + boi.QuantityOrdered + " x " + boi.Description);
                linebreak = "<br />";
            }            

            return sb.ToString();
        }
    }
}
