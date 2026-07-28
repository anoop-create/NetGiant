using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Import
{
    public class JobStatusCommonViewModel : CommonViewModel, IJobStatusCommonViewModel
    {
        protected JobStatus JobStatusRecordWrite;

        public JobStatusCommonViewModel()
        {
            Warnings = new List<string>();
        }

        public List<string> Warnings { get; set; }
        public bool SaveHadErrors { get; set; }

        public void WriteJobStatusRecord(string jobStatus, string htmlNotes, SavingErrorType savingErrorType = SavingErrorType.Saving)
        {
            // the order of these 2 is important, as htmlNotes depends on jobStatus
            htmlNotes = ProcessHtmlNotes(jobStatus, htmlNotes, savingErrorType);

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    if (JobStatusRecordWrite == null)
                    {
                        JobStatusRecordWrite = new JobStatus()
                        {
                            JobStartDate = DateTime.Now,
                            JobStatusString = jobStatus,
                            HtmlNotes = htmlNotes
                        };

                        db.Entry(JobStatusRecordWrite).State = EntityState.Added;
                        db.SaveChanges();
                    }
                    else
                    {
                        JobStatusRecordWrite.JobStatusString = jobStatus;
                        if (!string.IsNullOrEmpty(htmlNotes))
                        {
                            JobStatusRecordWrite.HtmlNotes += htmlNotes;
                        }

                        db.Entry(JobStatusRecordWrite).State = EntityState.Modified;
                        db.SaveChanges();
                    }
                }
            }
            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }
        }

        private string ProcessHtmlNotes(string jobStatus, string htmlNotes, SavingErrorType savingErrorType)
        {
            if (!string.IsNullOrEmpty(htmlNotes))
            {
                if (savingErrorType == SavingErrorType.Validation)
                {
                    htmlNotes = "Validation: " + htmlNotes;
                }
                else if (savingErrorType == SavingErrorType.Saving)
                {
                    htmlNotes = "Saving: " + htmlNotes;
                }

                if (jobStatus.Contains("Working"))
                {
                    htmlNotes = "<font color=\"red\">" + htmlNotes + "</font><br>";
                }
                else if (jobStatus.Contains("Complete"))
                {
                    if (SaveHadErrors)
                    {
                        htmlNotes = "<font color=\"red\">" + htmlNotes + "</font><br>";
                    }
                    else
                    {
                        htmlNotes = "<font color=\"green\">" + htmlNotes + "</font><br>";
                    }
                }
            }

            return htmlNotes;
        }

        public List<Status> GetStatus()
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                return db.JobStatus
                         .OrderByDescending(x => x.JobStartDate)
                         .Take(12)
                         .ToList()
                         .Select(x => new Status
                         {
                             Id = x.JobStatusId,
                             Title = x.JobStatusString,
                             Date = x.JobStartDate.ToString("dd/MM/yyyy HH:mm:ss"),
                             Message = x.HtmlNotes
                         })
                         .ToList();
            }
        }

        protected string ErrorMessage(int currentRow, Exception ex, bool breakout = false)
        {
            var sb = new StringBuilder();

            sb.Append("Error importing row " + currentRow + ".");
            sb.Append(" Error Message - " + ex.Message + ".");
            if (breakout)
            {
                sb.Append(" File Processing Ended Due to Errors in the File.");
                sb.Append(" Re-Upload a Valid File.");
            }
            else
            {
                sb.Append(" Re-Import a Valid Line.");
            }

            return sb.ToString();
        }
    }

    public enum SavingErrorType
    {
        Validation,
        Saving
    }

    public class Status
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Date { get; set; }
        public string Message { get; set; }
    }
}
