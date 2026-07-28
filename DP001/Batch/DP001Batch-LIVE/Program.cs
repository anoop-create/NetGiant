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

namespace DP001Batch
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (ConfigurationManager.AppSettings["Platform"] == "Azure")
            {
                var config = new JobHostConfiguration();
                config.UseTimers();
                config.Queues.MaxDequeueCount = 1;
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
        public static void TriggerFunction([QueueTrigger("dp001batchqueue")] CloudQueueMessage message)
        {
            StartProcess(message.AsString);
        }

        // This is on a timer schedule every 15 minutes
        public static void RunSchedule([TimerTrigger("0 0,15,30,45 * * * *")] TimerInfo timernInfo)
        {
            CommonDataFunctions.CreateLogEntry(0, 0, "Auto Run Schedule - Azure Webjob", "ScheduleInfo", true);

            const string args = "t$runschedule,tt$0,ch$0,p$15";
            var parms = SwitchDetection.loadParms(new[] { args });
            var switchDetect = new SwitchDetection();
            switchDetect.DetectSwitch(parms);
        }

        // This is on a timer schedule 6:07am and 6:07pm every day
        public static void RunAdHocSchedule([TimerTrigger("0 7 6,18 * * *")] TimerInfo timerInfo)
        {
            CommonDataFunctions.CreateLogEntry(0, 0, "Auto Run AdHoc Schedule - Azure Webjob", "ScheduleInfo", true);

            const string args = "t$runadhocschedule,tt$0,ch$0";
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

            const string args = "t$generalmaintenance,tt$0,ch$0,p$w";
            var parms = SwitchDetection.loadParms(new[] { args });
            var switchDetect = new SwitchDetection();
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
            CommonDataFunctions.CreateLogEntry(Int32.Parse(parms["tenantid"]), Int32.Parse(parms["channelid"]), "START JOB: " + CommonFunctions.DictToString(parms, null), "Information", true);
            AppDomain.CurrentDomain.UnhandledException += (sender, e) => UnhandledExceptionTrapper(sender, e, parms);
            var switchDetect = new SwitchDetection();
            switchDetect.DetectSwitch(parms);
            CommonDataFunctions.CreateLogEntry(Int32.Parse(parms["tenantid"]), Int32.Parse(parms["channelid"]), "END JOB: " + CommonFunctions.DictToString(parms, null), "Information", true);

            tc.Flush();
            Thread.Sleep(1000);
        }

        private static void RegisterApplicationInsights()
        {
            if (ConfigurationManager.AppSettings["Environment"] == "Live")
            {
                TelemetryConfiguration.Active.InstrumentationKey = "c2f32249-db57-486b-afef-554c693e38bf";
            }
        }

        private static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e, Dictionary<string, string> parms)
        {
            Exception ex = (Exception)e.ExceptionObject;
            CustomErrorHandling errorEvent = new CustomErrorHandling();
            errorEvent.LogError(ex, parms);

            if (ConfigurationManager.AppSettings["Environment"] == "Live")
            {
                var tc = new TelemetryClient();
                tc.TrackException(ex);
                tc.Flush();
                Thread.Sleep(1000);
            }
        }

    }
}
