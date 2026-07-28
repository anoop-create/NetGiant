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

        static List<string> notificationsList = new List<string>();

        public static void RunSchedule(int slotSize)
        {
            //Derive the current time slot
            DateTime ct1 = CommonDataFunctions.GetCurrentDateTime();
            int day = (int)ct1.DayOfWeek;
            TimeSpan ts1 = new TimeSpan(0, 0, 0);
            TimeSpan ts2 = new TimeSpan(0, 0, 0);

            int noRuns = 60 / slotSize;
            for (int i = 0; i < noRuns; i++)
            {
                int j = i * slotSize;
                if (ct1.Minute >= j && ct1.Minute <= j + slotSize - 1)
                {
                    ts1 = new TimeSpan(ct1.Hour, j, 0);
                    ts2 = new TimeSpan(ct1.Hour, j + slotSize - 1, 59);
                }
            }

            //Get a list of Tenant/Channels scheduled for this slot
            var crud = new CrudSchedule();
            List<Schedule> schedules = new List<Schedule>();
            schedules = crud.Read(x => x.Channel.TenantSetting.IsActive && x.Channel.IsActive && x.IsActive && (x.Lookup1.LookupName == "Daily" && x.Time >= ts1 && x.Time <= ts2) || (x.Lookup1.LookupName == "Weekly" && x.DayOfWeek == day && x.Time >= ts1 && x.Time <= ts2));

            //For each Channel run the data load/price calculation process
            foreach (Schedule sch in schedules)
            {
                CommonDataFunctions.NotificationEvent += NotificationRaised;

                // The full pricing process - performs all necessary operations to update prices
                TenantSetting tenant = new TenantSetting();
                Tenant t = new Tenant();
                tenant = t.GetTenantFromChannel(sch.ChannelFK);
                Dictionary<string, string> parms = SwitchDetection.loadParms(new string[] { "tt$" + tenant.TenantID.ToString() + ",ch$" + sch.ChannelFK.ToString() + ",i$0" });

                //CommonDataFunctions.CreateLogEntry(t.GetChannelRecord(sch.ChannelFK), "START RunSchedule", "Information", true);

                Inventories ipp = new Inventories(parms);
                if (ipp.Populate())
                {
                    Engine epp = new Engine(parms);
                    epp.Calculate();
                }

                ProcessNotifications(parms);
                WriteScheduleInfoLog(parms);
                notificationsList.Clear();
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
                        SwitchDetection.DetectSwitch(dictionary);

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
                        SwitchDetection.DetectSwitch(dictionary);
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

        private static void NotificationRaised(EventArgs e, string notification)
        {
            notificationsList.Add(notification);
        }

        private static void ProcessNotifications(Dictionary<string, string> parms)
        {
            if (notificationsList.Count > 0)
            {
                CommonFunctions.EmailNotifications(parms, notificationsList);
            }
        }

        private static void WriteScheduleInfoLog(Dictionary<string, string> parms)
        {
            var crudChannel = new CrudChannel();
            var channel = crudChannel.Read(Convert.ToInt32(parms["channelid"]));

            if (notificationsList.Count > 0)
            {
                CommonDataFunctions.CreateLogEntry(channel, "Schedule Ran with " +
                    notificationsList.Count + " Notifications", "ScheduleInfo", true);
            }
            else
            {
                CommonDataFunctions.CreateLogEntry(channel, "Schedule Ran Successfully", "ScheduleInfo", true);
            }
        }
    }
}
