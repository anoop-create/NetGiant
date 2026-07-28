using System;
using System.Web.Mvc;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using netGiant.Intranet.BusinessLayer.ViewModels.Admin.BatchProgram;
using System.Configuration;
using System.Net.Mime;
using netGiant.Intranet.Areas.PMS.Export;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;

namespace netGiant.Intranet.Controllers.Admin
{
    public class BatchProgramController : Controller
    {
        public ActionResult Jobs()
        {
            var model = new ScheduledTasksViewModel();
            return View("ListBatchJobs", model.GetTasks());
        }

        public bool RunJob(string jobName, string jobArguments)
        {
            var model = new ScheduledTasksViewModel();
            return model.RunTask(jobName, jobArguments);   
        }
        public ActionResult BatchLog()
        {
            return View(new BatchProgramViewModel());
        }

        public ActionResult BatchLogDetail(int id)
        {
            var model = new BatchProgramViewModel();
            model.BatchLogId = id;
            model.GetBatchLogEntry(id);
            return View(model);
        }

        public ActionResult BatchLog_Read([DataSourceRequest]DataSourceRequest request)
        {
            var model = new BatchProgramViewModel().GetBatchLog();

            var result = model.BatchLogList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        public ActionResult BatchLogDetail_Read([DataSourceRequest]DataSourceRequest request, int id)
        {
            var model = new BatchProgramViewModel();
            model.BatchLogId = id;
            model.GetBatchLogDetail();

            var result = model.BatchLogDetailList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult SaveBatchLog(BatchProgramViewModel model)
        {
            string comment = model.BatchLogEntry.Comments;
            BatchProgramViewModel m = new BatchProgramViewModel();
            m.GetBatchLogEntry(model.BatchLogEntry.BatchLogId);
            m.BatchLogEntry.Comments = comment;
            var sr = m.SaveBatchLog();

            return Json(new
            {
                saveReturn = sr
            });
        }

        [HttpPost]
        public ActionResult DeleteBatchLog(int id)
        {
            BatchProgramViewModel model = new BatchProgramViewModel();
            SaveReturn sr = model.DeleteBatchLog(id);

            return Json(new { saveReturn = sr });
        }

        [HttpPost]
        [DeleteFile]
        [ValidateAntiForgeryToken]
        public FileResult ExportBatchLog(string kendoData)
        {
            var model = new BatchProgramViewModel();
            model.GetFullBatchLog();

            Configuration machineConfig = ConfigurationManager.OpenMachineConfiguration();
            model.LocalDirectory = machineConfig.AppSettings.Settings["LocalDirectory"].Value.ToString();

            model.CreateBatchLogCSVFile(model.BatchLogForExport);

            return File(model.FilePath, MediaTypeNames.Application.Octet, "PMSExport_" + DateTime.Now.ToString("dd_MM_yyyy_H_mm_ss") + ".csv");
        }

        public ActionResult SalesforceBatchLog()
        {
            return View(new BatchProgramViewModel());
        }

        public ActionResult SfBatchLog_Read([DataSourceRequest]DataSourceRequest request)
        {
            var model = new BatchProgramViewModel().GetSfBatchLog();

            var result = model.SfBatchLogList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }
    }

    
}