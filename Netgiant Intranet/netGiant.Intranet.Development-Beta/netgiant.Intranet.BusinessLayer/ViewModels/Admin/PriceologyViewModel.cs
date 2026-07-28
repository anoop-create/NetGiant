using netGiant.Intranet.BusinessLayer.Utilities;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using netGiant.Intranet.DataLayer.Priceology;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Log = netGiant.Intranet.DataLayer.Priceology.Log;
using PriceologyLog = netGiant.Intranet.DataLayer.Priceology.Log;

namespace netGiant.Intranet.BusinessLayer.ViewModels.Admin
{
    public class PriceologyViewModel : CommonViewModel
    {
        private netGiant.Intranet.DataLayer.Priceology.PriceologyEntities _ctx;

        public PriceologyViewModel()
        {
            _ctx = new PriceologyEntities();
        }

        public IQueryable<TelerikLog> LogList { get; set; }
        public IQueryable<TelerikLog> LogDetailList { get; set; }
        public List<PriceologyLog> LogForExport { get; set; }
        public string LocalDirectory { get; set; }
        public string FilePath { get; set; }
        public PriceologyLog LogEntry { get; set; }
        public int LogId { get; set; } = 0;
        public int JobId { get; set; } = 0;
        public string Command { get; set; }

        public PriceologyViewModel GetLog()
        {
            LogList = _ctx.Logs
                .Include(x => x.Lookup)
                .Where(x => x.Entry.StartsWith("START JOB:"))
                .OrderByDescending(x => x.DateTime)
                .GroupJoin( _ctx.Channels,
                    Log => Log.ChannelFK,
                    Channel => Channel.ChannelID,
                    (Log, Channel) => new { Log, Channel }
                )
                .GroupJoin(_ctx.TenantSettings,
                    Log => Log.Log.TenantFK,
                    Tenant => Tenant.TenantID,
                    (Log, Tenant) => new { Log, Tenant }
                )
                .Select(x => new
                {
                    LogId = x.Log.Log.LogID,
                    JobId = x.Log.Log.JobID.Value,
                    JobName = x.Log.Log.Entry.Replace("START JOB: ", ""),
                    Channel = x.Log.Channel.FirstOrDefault().ChannelName ?? x.Log.Log.ChannelFK.ToString(),
                    Tenant = x.Tenant.FirstOrDefault().Description ?? x.Log.Log.TenantFK.ToString(),
                    MessageType = x.Log.Log.Lookup.LookupName,
                    Message = x.Log.Log.Entry,
                    DateTime = x.Log.Log.DateTime
                })
                .AsEnumerable()
                .Select(x => new TelerikLog
                {
                    LogId = x.LogId
                    , JobId = x.JobId
                    , JobName = x.JobName.Split(' ')[0].Split('=')[1].Replace("'", "")
                    , Channel = x.Channel
                    , Tenant = x.Tenant
                    , MessageType = x.MessageType
                    , Message = x.Message
                    , DateTime = x.DateTime
                    //, MessageCount = _ctx.Logs.Where(y => y.JobID == x.JobId).Count
                })
                .AsQueryable();

            return this;
        }

        public PriceologyViewModel GetLogEntry(int id)
        {
            LogEntry = _ctx.Logs.Find(id);
            int i = LogEntry.Entry.IndexOf("type=") + 6;
            int j = LogEntry.Entry.IndexOf("'", i);
            Command = LogEntry.Entry.Substring(i, j - i);

            return this;
        }

        public PriceologyViewModel GetLogDetail()
        {
            LogDetailList = _ctx.Logs
            .Include(x => x.Lookup)
            .Where(x => x.JobID == JobId)
            .OrderBy(x => x.DateTime)
            .GroupJoin(_ctx.Channels,
                Log => Log.ChannelFK,
                Channel => Channel.ChannelID,
                (Log, Channel) => new { Log, Channel }
            )
            .GroupJoin(_ctx.TenantSettings,
                Log => Log.Log.TenantFK,
                Tenant => Tenant.TenantID,
                (Log, Tenant) => new { Log, Tenant }
            )
            .Select(x => new 
            {
                LogId = x.Log.Log.LogID,
                JobId = x.Log.Log.JobID.Value,
                Channel = x.Log.Channel.FirstOrDefault().ChannelName ?? x.Log.Log.ChannelFK.ToString(),
                Tenant = x.Tenant.FirstOrDefault().Description ?? x.Log.Log.TenantFK.ToString(),
                MessageType = x.Log.Log.Lookup.LookupName,
                Message = x.Log.Log.Entry,
                DateTime = x.Log.Log.DateTime
            })
            .AsEnumerable()
            .Select(x => new TelerikLog
            {
                LogId = x.LogId,
                JobId = x.JobId,
                Channel = x.Channel,
                Tenant = x.Tenant,
                MessageType = x.MessageType,
                Message = x.Message,
                DateTime = x.DateTime
            })
            .AsQueryable();

            return this;

        }

        public PriceologyViewModel GetFullLog()
        {
            DateTime dt = DateTime.Now.AddMonths(-3);
            LogForExport = _ctx.Logs
                .Include(x => x.Lookup)
                .Where(x => x.DateTime > dt)
                .ToList();

            return this;
        }

        public void CreateLogCSVFile(List<PriceologyLog> logList)
        {
            FilePath = LocalDirectory + "\\PMSTempData\\PriceologyLogExport_" + DateTime.Now.ToString("dd_MM_yyyy_H_mm_ss") + ".csv";

            using (CsvFileWriter writer = new CsvFileWriter(FilePath, ','))
            {
                SetColumnHeadings(writer);

                foreach (PriceologyLog log in logList)
                {
                    InsertCSVData(writer, log);
                }
            }
        }

        private void InsertCSVData(CsvFileWriter writer, PriceologyLog log)
        {
            CsvRow newRow = new CsvRow();
            newRow.Add(log.LogID.ToString());
            newRow.Add(log.JobID.ToString());
            newRow.Add(log.TenantFK.ToString());
            newRow.Add(log.ChannelFK.ToString());
            newRow.Add(log.Lookup.LookupName);
            newRow.Add(log.DateTime == null ? "" : log.DateTime.Value.ToString("dd/MM/yyyy"));
            newRow.Add(log.Entry);

            writer.WriteRow(newRow);
        }
        private void SetColumnHeadings(CsvFileWriter writer)
        {
            CsvRow firstRow = new CsvRow();
            firstRow.Add("LogId");
            firstRow.Add("JobId");
            firstRow.Add("TenantFK");
            firstRow.Add("ChannelFK");
            firstRow.Add("Log Type");
            firstRow.Add("DateTime");
            firstRow.Add("Entry");

            writer.WriteRow(firstRow);
        }

        //private int GetBatchLogStatus(ICollection<BatchLogDetail> details)
        //{
        //    if (details.Any(y => y.ErrorCode == "ERROR"))
        //    {
        //        return 1;
        //    }
        //    if (details.Any(y => y.ErrorCode == "WARNING"))
        //    {
        //        return 2;
        //    }
        //    return 0;
        //}
    }

    public class TelerikLog
    {
        public long LogId { get; set; }
        public int JobId { get; set; }
        public string Tenant { get; set; }
        public string Channel { get; set; }        
        public string JobName { get; set; }
        public string MessageType { get; set; }
        public string Message { get; set; }
        public int MessageCount { get; set; }
        public DateTime? DateTime { get; set; }
    }

    //public class TelerikLogDetail
    //{
    //    public int Id { get; set; }
    //    public DateTime DateTime { get; set; }
    //    public string Message { get; set; }
    //    public string ErrorCode { get; set; }
    //}
}

