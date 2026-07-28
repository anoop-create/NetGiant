using DP001Batch.BusinessObjects.Shared;
using System;
using System.Collections.Generic;
using DP001BusinessLogic.Shared;
using DP001DataAccess.Utilities;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights;
using System.Threading;
using System.Configuration;
using Microsoft.Azure.WebJobs;
using Microsoft.WindowsAzure.Storage.Queue;
using System.Threading.Tasks;
using DP001DataAccess.Entities;
using System.Diagnostics;

namespace DP001Batch
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //// Use the following when remote debugging on beta / live
            //Console.WriteLine("Waiting for debugger to attach");
            //while (!Debugger.IsAttached)
            //{
            //    Thread.Sleep(100);
            //}
            //Console.WriteLine("Debugger attached");
            //// End debugging


            if (ConfigurationManager.AppSettings["Platform"] == "Azure")
            {
                var config = new JobHostConfiguration();
                config.UseTimers();
                config.Queues.MaxDequeueCount = 6;
                config.Queues.BatchSize = 1;
                config.Queues.MaxPollingInterval = TimeSpan.FromSeconds(30);
                var host = new JobHost(config);
                host.RunAndBlock();
            }
            else
            {
                StartProcess(args[0]);
            }
        }

        // This is triggered when a message arrives in the Azure Queue
        [Timeout("00:30:00")]
        public static async Task TriggerFunction([QueueTrigger("dp001batchqueue")] CloudQueueMessage message, CancellationToken cancellationToken)
        {
            var taskA = new Task(() => StartProcess(message.AsString), cancellationToken);
            taskA.Start();
            await taskA;
        }

        // This is on a timer schedule every 15 minutes and pushes jobs into the Queue
        public static void RunSchedule([TimerTrigger("0 0,15,30,45 * * * *")] TimerInfo timernInfo)
        {
            CommonDataFunctions.CreateLogEntry(0, 0, "Auto Run Schedule - Azure Webjob", "ScheduleInfo", true);

            const string args = "t$runschedule,tt$0,ch$0,p$15";
            var parms = SwitchDetection.loadParms(new[] { args });
            var switchDetect = new SwitchDetection();
            switchDetect.DetectSwitch(parms);
        }

        // The following schedules DO NOT write jobs to the Queue
        // This is on a timer schedule 6:07am and 6:07pm every day
        public static void RunAdHocSchedule([TimerTrigger("0 7 6,18 * * *")] TimerInfo timerInfo)
        {
            CommonDataFunctions.CreateLogEntry(0, 0, "Auto Run AdHoc Schedule - Azure Webjob", "ScheduleInfo", true);

            const string args = "t$runadhocschedule,tt$0,ch$0";
            var parms = SwitchDetection.loadParms(new[] { args });
            var switchDetect = new SwitchDetection();
            switchDetect.DetectSwitch(parms);
        }

        // This is on a timer schedule 12:35am every day
        public static void NgOnlyCalculationHistory([TimerTrigger("0 35 0 * * *")] TimerInfo timerInfo)
        {
            CommonDataFunctions.CreateLogEntry(0, 0, "NG Only Load Calculation History - Azure Webjob", "Information", true);

            string args = "t$loadcalculationhistory,tt$18,ch$83";
            var parms = SwitchDetection.loadParms(new[] { args });
            var switchDetect = new SwitchDetection();
            switchDetect.DetectSwitch(parms);

            //args = "t$loadcalculationhistory,tt$18,ch$84";
            //parms = SwitchDetection.loadParms(new[] { args });
            //switchDetect = new SwitchDetection();
            //switchDetect.DetectSwitch(parms);

            //args = "t$loadcalculationhistory,tt$18,ch$85";
            //parms = SwitchDetection.loadParms(new[] { args });
            //switchDetect = new SwitchDetection();
            //switchDetect.DetectSwitch(parms);
        }

        // This is on a timer schedule 9:05am and 12:05pm every day
        public static void NgOnlyVerifyPrices([TimerTrigger("0 5 9,12 * * *")] TimerInfo timerInfo)
        {
            try
            {
                CommonDataFunctions.CreateLogEntry(0, 0, "NG Only Verify Prices - Azure Webjob", "Information", true);

                string args = "t$verifyprices,tt$18,ch$83";
                var parms = SwitchDetection.loadParms(new[] { args });
                var switchDetect = new SwitchDetection();
                switchDetect.DetectSwitch(parms);
            }
            catch
            {
                CommonDataFunctions.CreateLogEntry(18, 83, "Unable to run verifyprices routine", "Error", true);
            }
            //args = "t$verifyprices,tt$18,ch$84";
            //parms = SwitchDetection.loadParms(new[] { args });
            //switchDetect = new SwitchDetection();
            //switchDetect.DetectSwitch(parms);

            //args = "t$verifyprices,tt$18,ch$85";
            //parms = SwitchDetection.loadParms(new[] { args });
            //switchDetect = new SwitchDetection();
            //switchDetect.DetectSwitch(parms);
        }

        // This is a timer schedule 12:00pm and 5:00pm every day
        public static void CopyPriceRules([TimerTrigger("0 7 12,17 * * *")] TimerInfo timerInfo)
        {
            CommonDataFunctions.CreateLogEntry(0, 0, "Auto Run Copy Price Rule TG to CM and NG - Azure Webjob", "Information", true);

            const string args = "t$copypricerules,tt$18,ch$83,o$84|85";
            var parms = SwitchDetection.loadParms(new[] { args });
            var switchDetect = new SwitchDetection();
            switchDetect.DetectSwitch(parms);
        }

        // This is on a timer schedule 1:05am every day
        public static void GeneralMaintenanceDaily([TimerTrigger("0 5 1 * * *")] TimerInfo timerInfo)
        {
            CommonDataFunctions.CreateLogEntry(0, 0, "Auto Run Daily Maintenance - Azure Webjob", "Information", true);

            const string args = "t$generalmaintenance,tt$0,ch$0,p$d";
            var parms = SwitchDetection.loadParms(new[] { args });
            var switchDetect = new SwitchDetection();
            switchDetect.DetectSwitch(parms);
        }

        // This is on a timer schedule 3:00am every sunday
        public static void GeneralMaintenanceWeekly([TimerTrigger("0 0 3 * * 0")] TimerInfo timerInfo)
        {
            CommonDataFunctions.CreateLogEntry(0, 0, "Auto Run Weekly Maintenance - Azure Webjob", "Information", true);

            string args = "t$generalmaintenance,tt$0,ch$0,p$w";
            var parms = SwitchDetection.loadParms(new[] { args });
            var switchDetect = new SwitchDetection();
            switchDetect.DetectSwitch(parms);

            args = "t$truncatetable,tt$0,ch$0,tbl$CalculationHistory,fld$Date,p$-45";
            parms = SwitchDetection.loadParms(new[] { args });
            switchDetect = new SwitchDetection();
            switchDetect.DetectSwitch(parms);
        }

        // This is on a timer schedule 1:35am on 1st day of every month
        public static void GeneralMaintenanceMonthly([TimerTrigger("0 35 1 1 * *")] TimerInfo timerInfo)
        {
            CommonDataFunctions.CreateLogEntry(0, 0, "Auto Run Monthly Maintenance - Azure Webjob", "Information", true);

            const string args = "t$generalmaintenance,tt$0,ch$0,p$m";
            var parms = SwitchDetection.loadParms(new[] { args });
            var switchDetect = new SwitchDetection();
            switchDetect.DetectSwitch(parms);
        }

        private static void StartProcess(string message)
        {
            RegisterApplicationInsights();

            var tc = new TelemetryClient();
            Dictionary<string, string> parms = SwitchDetection.loadParms(new[] { message });
            Global.AssignJobId();
            CommonDataFunctions.CreateLogEntry(Int32.Parse(parms["tenantid"]), Int32.Parse(parms["channelid"]), "START JOB: " + CommonFunctions.DictToString(parms, null) + ". " + ConfigurationManager.AppSettings["CPU"] + ". " + ConfigurationManager.AppSettings["Environment"] + ".", "Information", true);
            AppDomain.CurrentDomain.UnhandledException += (sender, e) => UnhandledExceptionTrapper(sender, e, parms);
            var switchDetect = new SwitchDetection();
            switchDetect.DetectSwitch(parms);
            CommonDataFunctions.CreateLogEntry(Int32.Parse(parms["tenantid"]), Int32.Parse(parms["channelid"]), "END JOB: " + CommonFunctions.DictToString(parms, null), "Information", true);

            tc.Flush();
            Thread.Sleep(1000);
        }

        private static void RegisterApplicationInsights()
        {
            if (ConfigurationManager.AppSettings["Environment"] == "Live" && ConfigurationManager.AppSettings["Platform"] == "Azure")
            {
                TelemetryConfiguration.Active.InstrumentationKey = "c2f32249-db57-486b-afef-554c693e38bf";
            }
        }

        private static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e, Dictionary<string, string> parms)
        {
            Exception ex = (Exception)e.ExceptionObject;
            CustomErrorHandling errorEvent = new CustomErrorHandling();
            errorEvent.LogError(ex, parms);

            if (ConfigurationManager.AppSettings["Environment"] == "Live" && ConfigurationManager.AppSettings["Platform"] == "Azure")
            {
                var tc = new TelemetryClient();
                tc.TrackException(ex);
                tc.Flush();
                Thread.Sleep(1000);
            }
        }

    }
}
