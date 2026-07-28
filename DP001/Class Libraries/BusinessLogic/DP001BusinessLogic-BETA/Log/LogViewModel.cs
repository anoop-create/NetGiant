using DP001DataAccess.Entities;
using DP001BusinessLogic.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using DP001DataAccess.Utilities;

namespace DP001BusinessLogic.ViewModels
{
    public class LogViewModel
    {
        public LogViewModel()
        {
            _ctx = new DP001Entities();
        }

        public List<Log> LogList { get; set; }
        public List<Log> ErrorLogList { get; set; }
        public List<Log> SuggestionLogList { get; set; }
        public List<Log> ScheduleLogList { get; set; }
        public IQueryable<Telerik> ErrorList { get; set; }
        private DP001Entities _ctx;

        public LogViewModel GetSummary(int channelId)
        {
            DateTime dt = CommonDataFunctions.GetCurrentDateTime().AddDays(-2);
            CrudLog crud = new CrudLog();
            ErrorLogList = crud.Read(x => x.Lookup.LookupName == "Notification" && x.DateTime > dt && x.ChannelFK == channelId, 5);
            SuggestionLogList = crud.Read(x => x.Lookup.LookupName == "Suggestion" && x.DateTime > dt && x.ChannelFK == channelId, 5);
            ScheduleLogList = crud.Read(x => x.Lookup.LookupName == "ScheduleInfo" && x.DateTime > dt && x.ChannelFK == channelId, 5);

            return this;
        }

        public List<Log> GetNotifications(int channelId, List<string> logTypes)
        {
            CrudLog crud = new CrudLog();
            List<Log> logList = crud.Read(x => logTypes.Contains(x.Lookup.LookupName) && x.ChannelFK == channelId);

            return logList;
        }

        public List<Log> GetNotifications(int channelId, string type)
        {
            CrudLog crud = new CrudLog();
            List<Log> logList = crud.Read(x => x.Lookup.LookupName == type && x.ChannelFK == channelId);

            return logList;
        }

        public LogViewModel GetErrors()
        {
            List<string> logTypes = new List<string> { "Error", "Notification" };
            var crud = new CrudLog();
            ErrorList = crud.ReadLogsQuery(x => logTypes.Contains(x.Lookup.LookupName), _ctx).AsTelerikViewModel();

            return this;
        }

        public string GetLastRunDate(int channelId)
        {
            CrudLog crud = new CrudLog();
            Log lastRun = crud.Read(x => x.Lookup.LookupName == "ScheduleInfo" && x.ChannelFK == channelId).OrderByDescending(x => x.DateTime).FirstOrDefault();
            if (lastRun == null)
            {
                return "No history found";
            }
            else
            {
                return string.Format("{0:dd/MM/yyyy (HH:mm)}", lastRun.DateTime);
            }
        }

        public class Telerik
        {
            public long LogId { get; set; }
            public int? JobId { get; set; }
            public int? TenantId { get; set; }
            public int? ChannelId { get; set; }
            public string LogType { get; set; }

            private DateTime? _logDateTime;
            public DateTime? LogDateTime
            {
                get
                {
                    return _logDateTime;
                }
                set
                {
                    if (value.HasValue)
                        _logDateTime = CommonDataFunctions.GetGmtTime(value.Value).LocalDateTime;
                }
            }

            public string Description { get; set; }
        }
    }

    public static class LogExtensions
    {
        public static IQueryable<LogViewModel.Telerik> AsTelerikViewModel(this IQueryable<Log> query)
        {
            return query.Select(o => new LogViewModel.Telerik
            {
                LogId = o.LogID,
                JobId = o.JobID,
                TenantId = o.TenantFK,
                ChannelId = o.ChannelFK,
                LogType = o.Lookup.LookupName,
                LogDateTime = o.DateTime,
                Description = o.Entry
            });
        }
    }
}
