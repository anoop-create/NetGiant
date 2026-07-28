using System;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Validation;
using System.Collections.Generic;
using System.Configuration;
using System.Web.Mvc;
using netGiant.Intranet.Areas.PMS.Export;
using System.Net.Mime;
using static netGiant.Intranet.BusinessLayer.ViewModels.PMS.Validation.ImageCheckViewModel;
using System.Web;
using Newtonsoft.Json;

namespace netGiant.Intranet.Areas.PMS.Controllers.Validation
{
    public class ValidationController : Controller
    {
        public ActionResult ImageCheckIndex()
        {
            return View(new ImageCheckViewModel());
        }

        public ActionResult ImageCheck_Read([DataSourceRequest]DataSourceRequest request)
        {
            Session["ImageCheck_Read_DataSourceRequest"] = request;

            var model = new ImageCheckViewModel();
            model.GetImageCheckList();

            var result = model.ImageCheckList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        public JsonResult SetExcludedImage(int websiteInventoryId, int equipmentId)
        {
            var model = new ImageCheckViewModel();
            var sr = model.SetExcludedImage(websiteInventoryId, equipmentId);

            return Json(new
            {
                saveReturn = sr
            });
        }

        [HttpPost]
        [DeleteFile]
        [ValidateAntiForgeryToken]
        public FileResult ExportImageCheck()
        {
            DataSourceRequest dataSourceRequest = Session["ImageCheck_Read_DataSourceRequest"] as DataSourceRequest; //get the stored filters from the last kendo request
            dataSourceRequest.PageSize = int.MaxValue; //get ALL the rows 

            var model = new ImageCheckViewModel();
            model.GetImageCheckList(); //gets all the rows from the DB
            DataSourceResult result = model.ImageCheckList.ToDataSourceResult(dataSourceRequest); //kendo is filtering the results from the stored filter

            List<TelerikImageCheck> telerikImageChecks = new List<TelerikImageCheck>(); //createCSV takes a list, we build the list from the results
            
            foreach(TelerikImageCheck row in result.Data)
            {
                telerikImageChecks.Add(row);
            }

            Configuration machineConfig = ConfigurationManager.OpenMachineConfiguration();
            model.LocalDirectory = machineConfig.AppSettings.Settings["LocalDirectory"].Value.ToString();

            model.CreateImageCheckCSVFile(telerikImageChecks);

            return File(model.FilePath, MediaTypeNames.Application.Octet, "PMSExport_" + DateTime.Now.ToString("dd_MM_yyyy_H_mm_ss") + ".csv");
        }
    }
}