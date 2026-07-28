using System;
using System.Web.Mvc;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using netGiant.Intranet.BusinessLayer.ViewModels.Admin;
using System.Configuration;
using System.Net.Mime;
using netGiant.Intranet.Areas.PMS.Export;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;

namespace netGiant.Intranet.Controllers.Admin
{
    public class PriceologyController : Controller
    {
        public ActionResult Log()
        {
            return View(new PriceologyViewModel());
        }

        public ActionResult LogDetail(int id)
        {
            var model = new PriceologyViewModel();           
            model.GetLogEntry(id);
            model.JobId = model.LogEntry.JobID.Value;
            return View(model);
        }

        public ActionResult Log_Read([DataSourceRequest] DataSourceRequest request)
        {
            var model = new PriceologyViewModel().GetLog();

            var result = model.LogList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        public ActionResult LogDetail_Read([DataSourceRequest] DataSourceRequest request, int id)
        {
            var model = new PriceologyViewModel();
            model.JobId = id;
            model.GetLogDetail();

            var result = model.LogDetailList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        [HttpPost]
        [DeleteFile]
        [ValidateAntiForgeryToken]
        public FileResult ExportLog(string kendoData)
        {
            var model = new PriceologyViewModel();
            model.GetFullLog();

            Configuration machineConfig = ConfigurationManager.OpenMachineConfiguration();
            model.LocalDirectory = machineConfig.AppSettings.Settings["LocalDirectory"].Value.ToString();

            model.CreateLogCSVFile(model.LogForExport);

            return File(model.FilePath, MediaTypeNames.Application.Octet, "PMSExport_" + DateTime.Now.ToString("dd_MM_yyyy_H_mm_ss") + ".csv");
        }

    }
}