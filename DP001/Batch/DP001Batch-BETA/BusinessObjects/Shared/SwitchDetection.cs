using DP001Batch.BusinessObjects.FileSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using DP001DataAccess.Utilities;
using DP001BusinessLogic;
using DP001BusinessLogic.Pricing;
using System.Data;
using DP001BusinessLogic.Shared;
using System.Diagnostics;
using System.Threading;

namespace DP001Batch.BusinessObjects.Shared
{
    public class SwitchDetection
    {
        private readonly List<string> _notificationsList = new List<string>();

        internal void DetectSwitch(Dictionary<string, string> parms)
        {
            bool success = false;

            //// Use the following when remote debugging on beta / live
            //Console.WriteLine("Waiting for debugger to attach");
            //while (!Debugger.IsAttached)
            //{
            //    Thread.Sleep(100);
            //}
            //Console.WriteLine("Debugger attached");
            //// End debugging
            ///

            switch (parms["type"])
            {
                //In alphabetical order, please

                case "calculateprices":
                    // Calculates prices based on existing inventories and price rules
                    // Example: t$calculateprices,tt$9,ch$39,dbg$1
                    CalculatePrices(parms);
                    break;
                case "copypricerules":
                    // Copy specific price rules from TG to CM and NG
                    // Example: t$copypricerules,tt$18,ch$83,o$84|85
                    CopyPriceRules(parms);
                    break;
                case "deletechannel":
                    // Deletes a channel
                    // Example: t$deletechannel,tt$9,ch$39
                    CrudChannel crud1 = new CrudChannel();
                    crud1.Delete(parms);
                    break;
                case "deletetenant":
                    // Deletes a tenant
                    // Example: t$deletetenant,tt$5,ch$0
                    CrudTenant crud3 = new CrudTenant();
                    crud3.Delete(parms);
                    break;
                case "dummy":
                    // For testing purposes
                    // Example: t$dummy,tt$5,ch$0
                    CommonDataFunctions.CreateLogEntry(0, 0, "START dummy action ", "Information", true);
                    break;
                case "generalmaintenance":
                    // Performs general maintenance on the system
                    // Example: t$generalmaintenance,tt$0,ch$0,p$m   monthly
                    // Example: t$generalmaintenance,tt$0,ch$0,p$d   daily
                    // Example: t$generalmaintenance,tt$0,ch$0,p$w  weekly   
                    StandardFunctions.GeneralMaintenance(parms);
                    break;
                case "loadcalculationhistory":
                    // Load Calculation Outcome History
                    // Example: t$loadcalculationhistory,tt$19,ch$85
                    var calculationHistory = new LoadCalculationHistory(parms);
                    success = calculationHistory.Load();
                    break;
                case "loadinventories":
                    // Load the product, supplier and competitor inventories and creates any brands/competitor/category records 
                    // Example: t$loadinventories,tt$9,ch$39,dbg$1
                    LoadInventories(parms);
                    break;
                case "loadinventoriesonly":
                    // Load the product, supplier and competitor inventories and creates any brands/competitor/category records 
                    // Example: t$loadinventoriesonly,tt$9,ch$39
                    LoadInventoriesOnly(parms);
                    break;
                case "loadsaleshistory":
                    // Load Sales History from a feed
                    // Example: t$loadsaleshistory,tt$19,ch$85
                    var salesHistory = new LoadSalesHistory(parms);
                    success = salesHistory.Load();
                    break;
                case "runadhocschedule":
                    // Runs the schedule to pick up request from tenants
                    // Example: t$runadhocschedule,tt$0,ch$0
                    Scheduling.RunAdHocSchedule(parms);
                    break;
                case "runschedule":
                    // Runs the schedule to detect which tenants/channels require the pricing operation to trigger
                    // Example: t$runschedule,tt$0,ch$0,p$15  **p is in minutes
                    Scheduling.RunSchedule(Int32.Parse(parms["period"]));
                    WriteScheduleInfoLog(parms);
                    break;
                case "runsp":
                    // Runs a stored procedure
                    // Example: t$runsp,tt$0,ch$0
                    RunSP.ExecSPNoParms(parms);
                    break;
                case "truncatetable":
                    // Removes old records from a table
                    // Example: t$truncatetable,tt$0,ch$0,tbl$Log,fld$DateTime,p$-180      **p is in days
                    StandardFunctions.TruncateTable(parms["tablename"], parms["fieldname"], Int32.Parse(parms["period"]), Int32.Parse(parms["debug"]));
                    break;
                case "verifyprices":
                    // Verify that price changes are 'reasonable'
                    // Example: t$verifyprices,tt$18,ch$83
                    CommonDataFunctions.CreateLogEntry(0, 0, "START SwitchDetection " + parms["channelid"], "Information", true);
                    VerifyPrices(parms);
                    CommonDataFunctions.CreateLogEntry(0, 0, "END SwitchDetection " + parms["channelid"], "Information", true);
                    break;
                case "watchdirectory":
                    // Watches a folder and triggers any batch processes
                    // Example: t$watchdirectory,tt$0,ch$0,i$E:\web_sites\DP001TaskWatcher
                    Watcher.WatchFolder(parms["input"]);
                    break;
                default:
                    CommonDataFunctions.CreateLogEntry(0, 0, "ERROR: Invalid Switch detected: " + parms["type"], "Error", true);
                    break;
            }

            ProcessNotifications(parms);
        }

        /// <summary>
        /// Builds the parameter dictionary
        /// </summary>
        /// <param name="args">The arguments passed in</param>
        public static Dictionary<string, string> loadParms(string[] args)
        {
            //Alphabetical order please

            // ch = channel
            // db = dbname
            // dbg = debug 0=false, 1=true
            // i = input
            // id = unique id
            // fld = fieldname
            // fs = ftp site name
            // fu = ftp username
            // fpw = ftp password
            // fp = ftp additional path
            // o = output 
            // p = period         
            // s = run sub type       
            // t = run type
            // tbl = tablename
            // tt = tenant id (a number)

            Dictionary<string, string> parms = new Dictionary<string, string>();

            var arguments = args[0].Split(',').Select(i => i.Split('$')).ToArray();

            foreach (var arg in arguments)
            {
                switch (arg[0])
                {
                    case "ch":
                        parms.Add("channelid", arg[1]);
                        break;
                    case "db":
                        parms.Add("dbname", arg[1]);
                        break;
                    case "dbg":
                        parms.Add("debug", arg[1]);
                        break;
                    case "fld":
                        parms.Add("fieldname", arg[1]);
                        break;
                    case "fp":
                        parms.Add("ftppath", arg[1]);
                        parms.Add("filepath", arg[1]);
                        break;
                    case "fprod":
                        parms.Add("prodfile", arg[1]);
                        break;
                    case "fpw":
                        parms.Add("ftppassword", arg[1]);
                        break;
                    case "fs":
                        parms.Add("ftpsite", arg[1]);
                        break;
                    case "fu":
                        parms.Add("ftpusername", arg[1]);
                        break;
                    case "i":
                        parms.Add("input", arg[1]);
                        break;
                    case "id":
                        parms.Add("id", arg[1]);
                        break;
                    case "o":
                        parms.Add("output", arg[1]);
                        break;
                    case "p":
                        parms.Add("period", arg[1]);
                        break;
                    case "s":
                        parms.Add("subtype", arg[1].ToLower());
                        break;
                    case "t":
                        parms.Add("type", arg[1].ToLower());
                        break;
                    case "tbl":
                        parms.Add("tablename", arg[1]);
                        break;
                    case "tt":
                        parms.Add("tenantid", arg[1]);
                        break;
                    default:
                        // unknown parameter
                        break;
                }
            }
            if (!parms.ContainsKey("debug"))
            {
                parms.Add("debug", "0");
            }
            return parms;
        }

        private void CalculatePrices(Dictionary<string, string> parms)
        {
            CommonDataFunctions.NotificationEvent += NotificationRaised;

            var tenant = new Tenant();

            try
            {
                Engine engine = new Engine(parms);
                if (!engine.Calculate())
                {
                    var crudChannel = new CrudChannel();
                    var channel = crudChannel.Read(Convert.ToInt32(parms["channelid"]));
                    tenant.SetJobInProgress(Convert.ToInt32(parms["channelid"]), false);
                    CommonDataFunctions.CreateLogEntry(channel, "Price Calculations not run due to errors during the Calculate process.", "Notification", true);
                }
                tenant.SetJobInProgress(Convert.ToInt32(parms["channelid"]), false);
            }
            catch (Exception e)
            {
                var crudChannel = new CrudChannel();
                var channel = crudChannel.Read(Convert.ToInt32(parms["channelid"]));
                tenant.SetJobInProgress(Convert.ToInt32(parms["channelid"]), false);
                CommonDataFunctions.CreateLogEntry(channel, e.Message + " - " + e.StackTrace, "Error");
            }
        }

        private void CopyPriceRules(Dictionary<string, string> parms)
        {
            CommonDataFunctions.NotificationEvent += NotificationRaised;

            var tenant = new Tenant();

            try
            {
                CopyPriceRule priceRule = new CopyPriceRule(parms);

                priceRule.Copy();

                tenant.SetJobInProgress(Convert.ToInt32(parms["channelid"]), false);
            }
            catch(Exception e)
            {
                var crudChannel = new CrudChannel();
                var channel = crudChannel.Read(Convert.ToInt32(parms["channelid"]));
                tenant.SetJobInProgress(Convert.ToInt32(parms["channelid"]), false);
                CommonDataFunctions.CreateLogEntry(channel, e.Message + " - " + e.StackTrace, "Error");
            }
        }

        private void LoadInventories(Dictionary<string, string> parms)
        {
            CommonDataFunctions.NotificationEvent += NotificationRaised;

            var tenant = new Tenant();

            try
            {
                Inventories ili = new Inventories(parms);
                if (ili.Populate())
                {
                    Engine epp1 = new Engine(parms);
                    if (!epp1.Calculate())
                    {
                        var crudChannel = new CrudChannel();
                        var channel = crudChannel.Read(Convert.ToInt32(parms["channelid"]));
                        tenant.SetJobInProgress(Convert.ToInt32(parms["channelid"]), false);
                        CommonDataFunctions.CreateLogEntry(channel, "Price Calculations not run due to errors during the Calculate process.", "Notification", true);
                    }
                }
                else
                {
                    var crudChannel = new CrudChannel();
                    var channel = crudChannel.Read(Convert.ToInt32(parms["channelid"]));
                    tenant.SetJobInProgress(Convert.ToInt32(parms["channelid"]), false);
                    CommonDataFunctions.CreateLogEntry(channel, "Price Calculations not run due to errors loading the feed files.", "Notification", true);
                }

                tenant.SetJobInProgress(Convert.ToInt32(parms["channelid"]), false);
            }
            catch (Exception e)
            {
                var crudChannel = new CrudChannel();
                var channel = crudChannel.Read(Convert.ToInt32(parms["channelid"]));
                tenant.SetJobInProgress(Convert.ToInt32(parms["channelid"]), false);
                CommonDataFunctions.CreateLogEntry(channel, e.Message + " - " + e.StackTrace, "Error");
            }

            WriteScheduleInfoLog(parms);
        }

        private void LoadInventoriesOnly(Dictionary<string, string> parms)
        {
            CommonDataFunctions.NotificationEvent += NotificationRaised;

            var tenant = new Tenant();

            try
            {
                Inventories ili = new Inventories(parms);
                ili.Populate();

                tenant.SetJobInProgress(Convert.ToInt32(parms["channelid"]), false);
            }
            catch (Exception e)
            {
                var crudChannel = new CrudChannel();
                var channel = crudChannel.Read(Convert.ToInt32(parms["channelid"]));
                tenant.SetJobInProgress(Convert.ToInt32(parms["channelid"]), false);
                CommonDataFunctions.CreateLogEntry(channel, e.Message + " - " + e.StackTrace, "Error");
            }

            WriteScheduleInfoLog(parms);
        }

        private void NotificationRaised(EventArgs e, string notification)
        {
            _notificationsList.Add(notification);
        }

        private void ProcessNotifications(Dictionary<string, string> parms)
        {
            if (_notificationsList?.Count > 0)
            {
                CommonFunctions.EmailNotifications(parms, _notificationsList);
                _notificationsList.Clear();
            }
        }

        private void WriteScheduleInfoLog(Dictionary<string, string> parms)
        {
            var crudChannel = new CrudChannel();
            var channel = crudChannel.Read(Convert.ToInt32(parms["channelid"]));

            if (_notificationsList?.Count > 0)
            {
                CommonDataFunctions.CreateLogEntry(channel, "Schedule Ran with " +
                    _notificationsList.Count + " Notifications", "ScheduleInfo", true);
            }
            else
            {
                CommonDataFunctions.CreateLogEntry(channel, "Schedule Ran Successfully", "ScheduleInfo", true);
            }
        }

        private void VerifyPrices(Dictionary<string, string> parms)
        {
            CommonDataFunctions.NotificationEvent += NotificationRaised;

            CommonDataFunctions.CreateLogEntry(0, 0, "START1 Constructor " + parms["channelid"], "Information", true);
            var tenant = new Tenant();

            try
            {
                VerifyPrices vp = new VerifyPrices(parms);
                vp.Verify();
            }
            catch (Exception e)
            {
                var crudChannel = new CrudChannel();
                var channel = crudChannel.Read(Convert.ToInt32(parms["channelid"]));
                tenant.SetJobInProgress(Convert.ToInt32(parms["channelid"]), false);
                CommonDataFunctions.CreateLogEntry(channel, e.Message + " - " + e.StackTrace, "Error");
            }
        }
    }
}
