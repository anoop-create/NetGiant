using DP001DataAccess.Entities;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DP001DataAccess.Utilities
{
    public enum PlatformType
    {
        Server,
        Azure
    }

    public static class Global
    {
        public static int JobId = new Int32();

        public static void AssignJobId()
        {
            using (DP001Entities db = new DP001Entities())
            {
                Log l = db.Logs
                            .OrderByDescending(x => x.JobID)
                            .FirstOrDefault();
                JobId = l.JobID.Value + 1;                            
            }
        }
    }

    public class CommonDataFunctions
    {
        public static event NotificationHandler NotificationEvent;
        public delegate void NotificationHandler(EventArgs e, string notification);

        public static void CreateLogEntry(Channel channel, string entry, string messageType, bool alwaysOutput = false)
        {
            if (alwaysOutput || channel.TenantSetting.VerboseLogging)
            {
                LogEntry(channel.TenantSetting.TenantID, channel.ChannelID, messageType, entry);
            }
        }

        public static void CreateLogEntry(int tenantid, int channelid, string entry, string messageType, bool alwaysOutput = false)
        {
            if (alwaysOutput)
            {
                LogEntry(tenantid, channelid, messageType, entry);
            }
        }

        private static void LogEntry(int tenantid, int channelid, string messageType, string entry)
        {
            using (DP001Entities db = new DP001Entities())
            {
                Log log = new Log();
                log.JobID = Global.JobId;
                log.TenantFK = tenantid;
                log.ChannelFK = channelid;
                log.DateTime = GetCurrentDateTime();
                log.Entry = entry;
                int lookupLogTypeId = db.LookupTypes
                    .Where(x => x.LookupTypeName == "LogType")
                    .Select(x => x.LookupTypeID).FirstOrDefault();
                log.LogTypeFK = db.Lookups
                    .Where(x => x.LookupName == messageType && x.LookupTypeFK == lookupLogTypeId)
                    .Select(x => x.LookupID).FirstOrDefault();
            
                db.Entry(log).State = EntityState.Added;
                db.SaveChanges();

                if (messageType == "Notification")
                    NotificationEvent?.Invoke(new EventArgs(), entry);

                if (messageType == "Error")
                    LogApplicationInsightsException(new Exception(entry + " TenantID: " + tenantid + " ChannelID: " + channelid));
            }
        }

        public static PlatformType GetPlatformType()
        {
            var platform = ConfigurationManager.AppSettings["Platform"];

            if (platform == "Server")
            {
                return PlatformType.Server;
            }
            else if (platform == "Azure")
            {
                return PlatformType.Azure;
            }
            else
            {
                return PlatformType.Server;
            }
        }

        public static DateTime GetCurrentDateTime(bool forSQLServer = false)
        {
            DateTime currentDateTime = new DateTime();
            if (forSQLServer)
            {
                using (DP001Entities db = new DP001Entities())
                {
                    var dateQuery = db.Database.SqlQuery<DateTime>("SELECT getdate()");
                    currentDateTime = dateQuery.AsEnumerable().First();
                }
            }
            else
            {
                var timeZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
                currentDateTime = TimeZoneInfo.ConvertTime(DateTime.Now, timeZone);
            }

            return currentDateTime;
        }

        public static DateTimeOffset ConvertDateTimeToCurrentTimeZone(DateTimeOffset dateTime)
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
            var currentDateTime = TimeZoneInfo.ConvertTime(dateTime, timeZone);

            return currentDateTime;
        }

        public static DateTimeOffset GetGmtTime(DateTimeOffset dateTime)
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
            var currentDateTime = TimeZoneInfo.ConvertTime(dateTime, timeZone);

            if (timeZone.IsDaylightSavingTime(dateTime))
                currentDateTime = currentDateTime.AddHours(-1);

            return currentDateTime;
        }
        
        public static void LogApplicationInsightsException(Exception e)
        {
            if (ConfigurationManager.AppSettings["Environment"] == "Live" && ConfigurationManager.AppSettings["Platform"] == "Azure")
            {
                TelemetryConfiguration.Active.InstrumentationKey = ConfigurationManager.AppSettings["APPINSIGHTS_INSTRUMENTATIONKEY"];

                var telemClient = new TelemetryClient();
                telemClient.TrackException(e);
                telemClient.Flush();
                Thread.Sleep(1000);
            }
        }
    }
}
