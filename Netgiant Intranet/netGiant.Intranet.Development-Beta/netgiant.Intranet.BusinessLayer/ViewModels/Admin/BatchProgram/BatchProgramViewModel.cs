using netGiant.Intranet.BusinessLayer.Utilities;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace netGiant.Intranet.BusinessLayer.ViewModels.Admin.BatchProgram
{
    public class BatchProgramViewModel : CommonViewModel
    {
        private ngmdEntities _ctx;

        public BatchProgramViewModel()
        {
            _ctx = new ngmdEntities();
        }

        public cmsSection CMSSection { get; set; }
        public cmsEntry CMSEntry { get; set; }
        public IQueryable<TelerikBatchLog> BatchLogList { get; set; }
        public IQueryable<TelerikBatchLogDetail> BatchLogDetailList { get; set; }
        public List<BatchLogDetail> BatchLogForExport { get; set; }
        public string LocalDirectory { get; set; }
        public string FilePath { get; set; }
        public BatchLog BatchLogEntry { get; set; }
        public int BatchLogId { get; set; } = 0;
        public IQueryable<TelerikSfBatchLog> SfBatchLogList { get; set; }

        public BatchProgramViewModel GetBatchLog()
        {
            BatchLogList = _ctx.BatchLog
                .Include(x => x.Website)
                .Include(x => x.BatchLogDetail)
                .OrderByDescending(x => x.DateTime)
                .Select(x => new TelerikBatchLog
                {
                    Id = x.BatchLogId,
                    Command = x.Command,
                    Type = x.Type,
                    SubType = x.SubType,
                    Website = x.Website == null ? "Non-Specific" : x.Website.FriendlyName,
                    DateTime = x.DateTime,
                    Comments = x.Comments,
                    Status = x.BatchLogDetail.Any(y => y.ErrorCode == "ERROR") ? 1
                        : x.BatchLogDetail.Any(y => y.ErrorCode == "WARNING") ? 2
                        : x.BatchLogDetail.Any(y => y.Message == "Process completed") ? 0
                        : 1
                })                    
                .AsQueryable();

            return this;
        }

        public BatchProgramViewModel GetBatchLogEntry(int id)
        {
            BatchLogEntry = _ctx.BatchLog.Find(id);

            return this;
        }

        public SaveReturn SaveBatchLog()
        {
            var saveReturn = new SaveReturn();

            try
            {
                _ctx.Entry(BatchLogEntry).State = EntityState.Modified;
                _ctx.SaveChanges();

                saveReturn.IsSuccess = true;
            }
            catch (Exception ex)
            {
                saveReturn.IsSuccess = false;
                saveReturn.Message = ex.Message;
            }

            return saveReturn;
        }

        public SaveReturn DeleteBatchLog(int id)
        {
            SaveReturn sr = new SaveReturn();

            try
            {
                if (id > 0)
                {
                    using (ngmdEntities db = new ngmdEntities())
                    {
                        BatchLog e = db.BatchLog.Where(x => x.BatchLogId == id).FirstOrDefault();
                        db.Entry(e).State = EntityState.Deleted;
                        db.SaveChanges();
                        sr.IsSuccess = true;
                    }
                }
            }
            catch (Exception ex)
            {
                sr.IsSuccess = false;
                sr.Message = ex.Message;
            }
            return sr;
        }

        public BatchProgramViewModel GetBatchLogDetail()
        {
            BatchLogDetailList = _ctx.BatchLogDetail
                .Include(x => x.BatchLog)
                .Where(x => x.BatchLogFk == BatchLogId)
                .OrderByDescending(x => x.DateTime)
                .Select(x => new TelerikBatchLogDetail
                {
                    Id = x.BatchLogDetailId,
                    DateTime = x.DateTime,
                    Message = x.Message,
                    ErrorCode = x.ErrorCode
                })
                .AsQueryable();

            return this;
        }

        public BatchProgramViewModel GetFullBatchLog()
        {
            DateTime dt = DateTime.Now.AddMonths(-3);
            BatchLogForExport = _ctx.BatchLogDetail
                .Include(x => x.BatchLog.Website)
                .Where(x => x.BatchLog.DateTime > dt)
                .ToList();

            return this;
        }

        public void CreateBatchLogCSVFile(List<BatchLogDetail> logList)
        {
            FilePath = LocalDirectory + "\\PMSTempData\\BatchLogExport_" + DateTime.Now.ToString("dd_MM_yyyy_H_mm_ss") + ".csv";

            using (CsvFileWriter writer = new CsvFileWriter(FilePath, ','))
            {
                SetColumnHeadings(writer);

                foreach (BatchLogDetail log in logList)
                {
                    InsertCSVData(writer, log);
                }
            }
        }

        private void InsertCSVData(CsvFileWriter writer, BatchLogDetail log)
        {
            CsvRow newRow = new CsvRow();
            newRow.Add(log.BatchLog.BatchLogId.ToString());
            newRow.Add(log.BatchLog.Command);
            newRow.Add(log.BatchLog.Type == null ? "" : log.BatchLog.Type);
            newRow.Add(log.BatchLog.SubType == null ? "" : log.BatchLog.SubType);
            newRow.Add(log.BatchLog.Website == null ? "All" : log.BatchLog.Website.FriendlyName);
            newRow.Add(log.BatchLog.DateTime.ToString("dd/MM/yyyy"));
            newRow.Add(log.DateTime.ToString("dd/MM/yyyy"));
            newRow.Add(log.Message == null ? "" : log.Message);
            newRow.Add(log.ErrorCode == null ? "" : log.ErrorCode);
            newRow.Add(log.BatchLog.Comments == null ? "" : log.BatchLog.Comments);

            writer.WriteRow(newRow);
        }
        private void SetColumnHeadings(CsvFileWriter writer)
        {
            CsvRow firstRow = new CsvRow();
            firstRow.Add("BatchLogId");
            firstRow.Add("Command");
            firstRow.Add("Type");
            firstRow.Add("SubType");
            firstRow.Add("Website");
            firstRow.Add("StartTime");
            firstRow.Add("LogTime");
            firstRow.Add("Message");
            firstRow.Add("ErrorCode");
            firstRow.Add("Comments");

            writer.WriteRow(firstRow);
        }
        public void DeleteFile(string filePath)
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }

        public BatchProgramViewModel GetSfBatchLog()
        {
            SfBatchLogList = _ctx.SalesforceBatchJob
                .OrderByDescending(x => x.DateCreated)
                .Select(x => new TelerikSfBatchLog
                {
                    JobId = x.JobId,
                    DateCreated = x.DateCreated,
                    Status = x.Status,
                    Object = x.Object,
                    Operation = x.Operation,
                    BatchesCompleted = x.BatchesCompleted,
                    BatchesFailed = x.BatchesFailed,
                    RecordsProcessed = x.RecordsProcessed,
                    RecordsFailed = x.RecordsFailed,
                    HasError = x.Status == "Closed" && (x.RecordsFailed > 0 || x.BatchesFailed > 0) ? true : false
                })
                .AsQueryable();

            return this;
        }

        private int GetBatchLogStatus(ICollection<BatchLogDetail> details)
        {
            if (details.Any(y => y.ErrorCode == "ERROR"))
            {
                return 1;
            }
            if (details.Any(y => y.ErrorCode == "WARNING"))
            {
                return 2;
            }
            return 0;
        }
    }

    public class TelerikBatchLog
    {
        public int Id { get; set; }
        public string Command { get; set; }
        public string Type { get; set; }
        public string SubType { get; set; }
        public string Website { get; set; }
        public DateTime DateTime { get; set; }
        public string Comments { get; set; }
        public int Status { get; set; }
    }

    public class TelerikBatchLogDetail
    {
        public int Id { get; set; }
        public DateTime DateTime { get; set; }
        public string Message { get; set; }
        public string ErrorCode { get; set; }
    }

    public class TelerikSfBatchLog
    {
        public string JobId { get; set; }
        public DateTime DateCreated { get; set; }
        public string Status { get; set; }
        public string Object { get; set; }
        public string Operation { get; set; }
        public int? BatchesCompleted { get; set; }
        public int? BatchesFailed { get; set; }
        public int? RecordsProcessed { get; set; }
        public int? RecordsFailed { get; set; }
        public bool HasError { get; set; }
    }
}
