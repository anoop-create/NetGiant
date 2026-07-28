using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace netGiant.Intranet.BusinessLayer.ViewModels.Ecommerce
{
    public class LogViewModel : CommonViewModel
    {
        public LogViewModel()
        {
            _ctx = new ngmdEntities();
        }

        public IQueryable<Telerik> LogList { get; set; }
        public Log LogEntry { get; set; }
        private ngmdEntities _ctx;

        public LogViewModel GetLogs()
        {
            LogList = _ctx.Log.Where(x => !x.Deleted).AsQueryable().AsTelerikViewModel();
            return this;
        }

        public LogViewModel GetLogEntry(long id)
        {
            using (var db = new ngmdEntities())
            {
                LogEntry = db.Log.Find(id);
            }

            return this;
        }

        public SaveReturn SaveLogEntry()
        {
            var saveReturn = new SaveReturn();

            try
            {
                using (var db = new ngmdEntities())
                {
                    db.Entry(LogEntry).State = EntityState.Modified;
                    db.SaveChanges();
                }

                saveReturn.IsSuccess = true;
            }
            catch (Exception ex)
            {
                saveReturn.IsSuccess = false;
                saveReturn.Message = ex.Message;
            }

            return saveReturn;
        }

        public SaveReturn SetDeletedFlag(int id, bool deleted)
        {
            var saveReturn = new SaveReturn();

            try
            {
                using (var db = new ngmdEntities())
                {
                    var logEntry = db.Log.Find(id);
                    logEntry.Deleted = deleted;
                    db.Entry(logEntry).State = EntityState.Modified;
                    db.SaveChanges();
                }

                saveReturn.IsSuccess = true;
            }
            catch (Exception e)
            {
                saveReturn.IsSuccess = false;
                saveReturn.Message = e.Message;
            }

            return saveReturn;
        }

        public class Telerik
        {
            public long Id { get; set; }
            public string Website { get; set; }
            public string User { get; set; }
            public int Type { get; set; }
            public DateTime DateTime { get; set; }
            public int? StatusCode { get; set; }
            public string Description { get; set; }
            public string Url { get; set; }
            public string UserAgent { get; set; }
            public string QueryString { get; set; }
            public string FormData { get; set; }
            public string DeveloperComments { get; set; }
            public string LogStatus { get; set; }
        }
    }

    public static class LogViewModeExtensions
    {
        public static IQueryable<LogViewModel.Telerik> AsTelerikViewModel(this IQueryable<Log> logQuery)
        {
            return logQuery.Select(o => new LogViewModel.Telerik
            {
                Id = o.Id,
                Website = o.Website.FriendlyName,
                User = !string.IsNullOrEmpty(o.User) ? o.User : "Unknown",
                Type = o.Type,
                DateTime = o.DateTime,
                StatusCode = o.StatusCode,
                Description = o.Description,
                Url = o.Url,
                UserAgent = o.UserAgent,
                QueryString = o.QueryString,
                FormData = o.FormData,
                DeveloperComments = !string.IsNullOrEmpty(o.DeveloperComments) ? o.DeveloperComments : "",
                //LogStatus = o.Lookup.LookupName
            });
        }
    }
}
