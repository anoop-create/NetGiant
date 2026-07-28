using DP001Batch.BusinessObjects.Shared;
using System;
using System.Collections.Generic;
using DP001BusinessLogic.Shared;
using DP001DataAccess.Utilities;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights;
using System.Threading;
using System.Configuration;

namespace DP001Batch
{
    class Program
    {
        static void Main(string[] args)
        {
            RegisterApplicationInsights();

            var tc = new TelemetryClient();
            Dictionary<string, string> parms = SwitchDetection.loadParms(args);
            CommonDataFunctions.CreateLogEntry(Int32.Parse(parms["tenantid"]), Int32.Parse(parms["channelid"]), "START JOB: " + CommonFunctions.DictToString(parms, null), "Information", true);
            AppDomain.CurrentDomain.UnhandledException += (sender, e) => UnhandledExceptionTrapper(sender, e, parms);
            SwitchDetection.DetectSwitch(parms);
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
