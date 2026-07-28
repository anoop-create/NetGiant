using DP001Batch.BusinessObjects.Shared;
using DP001BusinessLogic;
using DP001BusinessLogic.Pricing;
using DP001BusinessLogic.Shared;
using DP001DataAccess.Entities;
using DP001DataAccess.Utilities;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DP001Batch.BusinessObjects.FileSystem
{
    public class Scheduling
    {
        public Scheduling()
        {

        }

        public static void RunSchedule(int slotSize)
        {
            //Derive the current time slot
            var ct1 = CommonDataFunctions.GetCurrentDateTime();
            var day = (int)ct1.DayOfWeek;
            var ts1 = new TimeSpan(0, 0, 0);
            var ts2 = new TimeSpan(0, 0, 0);

            var noRuns = 60 / slotSize;
            for (var i = 0; i < noRuns; i++)
            {
                var j = i * slotSize;
                if (ct1.Minute >= j && ct1.Minute <= j + slotSize - 1)
                {
                    ts1 = new TimeSpan(ct1.Hour, j, 0);
                    ts2 = new TimeSpan(ct1.Hour, j + slotSize - 1, 59);
                }
            }

            //Get a list of Tenant/Channels scheduled for this slot
            var crud = new CrudSchedule();
            var schedules = crud.Read(x => x.Channel.TenantSetting.IsActive && x.Channel.IsActive && x.IsActive && (x.Lookup1.LookupName == "Daily" && x.Time >= ts1 && x.Time <= ts2) || (x.Lookup1.LookupName == "Weekly" && x.DayOfWeek == day && x.Time >= ts1 && x.Time <= ts2));

            //For each Channel run the data load/price calculation process
            foreach (var sch in schedules)
            {
                var t = new Tenant();
                var tenant = t.GetTenantFromChannel(sch.ChannelFK);
                AzureFunctions.WriteToAzureStorageQueue("t$loadinventories,tt$" + tenant.TenantID + ",ch$" + sch.ChannelFK);
            }
        }

        public static void RunAdHocSchedule(Dictionary<string, string> parms)
        {
            var platform = ConfigurationManager.AppSettings["Platform"];

            if (platform == "Server")
            {
                string folderPath = CommonFunctions.GetMachineAppSetting("AdHocBatchLocation");
                foreach (string file in Directory.EnumerateFiles(folderPath, "*.txt"))
                {
                    //string contents = File.ReadAllText(file);
                    while (CommonFunctions.IsFileInUse(new FileInfo(file)))
                    {
                        Thread.Sleep(500);
                    }

                    try
                    {
                        var fileText = File.ReadAllText(file);
                        string[] args = fileText.Split(null);
                        var dictionary = SwitchDetection.loadParms(args);
                        var switchDetect = new SwitchDetection();
                        switchDetect.DetectSwitch(dictionary);

                        CommonFunctions.DeleteFile(file);
                    }
                    catch (Exception)
                    {
                        CommonDataFunctions.CreateLogEntry(new Channel(), "Unable to run adhoc routine", "Error", true);
                    }
                }
            }
            else if (platform == "Azure")
            {
                var files = AzureFunctions.ListBlobContainerFilesAndContent("adhocschedule");
                var filesToDelete = new List<string>();

                foreach (var file in files)
                {
                    try
                    {
                        string[] args = file.Value.Split(null);
                        var dictionary = SwitchDetection.loadParms(args);
                        var switchDetect = new SwitchDetection();
                        switchDetect.DetectSwitch(dictionary);
                        filesToDelete.Add(file.Key);
                    }
                    catch (Exception)
                    {
                        CommonDataFunctions.CreateLogEntry(new Channel(), "Unable to run adhoc routine", "Error", true);
                    }
                }

                AzureFunctions.DeleteFilesInBlobContianer("adhocschedule", filesToDelete);
            }
        }
    }
}
