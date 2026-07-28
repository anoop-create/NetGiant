using DP001BusinessLogic.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using DP001DataAccess.Entities;
using System.IO;
using static System.Net.Mime.MediaTypeNames;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;
using Microsoft.AspNet.Identity;
using DP001DataAccess.Utilities;
using DP001Website.Models;
using DP001BusinessLogic;
using System.Web.Security;

namespace DP001Website.Controllers
{
    [Authorize]
    public class ReportsController : ApplicationController
    {
        private ReportsViewModel model;

        public ActionResult Comparison(int? id)
        {
            var tenantId = GetTenant().TenantID;
            var model = new ReportsViewModel(GetChannelId());
            model.InitializeReport(id, User.Identity.GetUserId(), tenantId);

            if (model.RequestedReportTenantId != null)
            {
                if (model.RequestedReportTenantId != tenantId)
                {
                    RouteData.Values.Remove("id");
                    return RedirectToAction("Comparison");
                }
            }

            return View(model);
        }

        public ActionResult Comparison_Read([DataSourceRequest]DataSourceRequest request)
        {
            var model = new ReportsViewModel(GetChannelId());
            model.GetComparison();

            var result = model.TelerikProducts.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        public ActionResult ComparisonStaging()
        {
            var model = new ReportsViewModel(GetChannelId());
            model.InitializeReport(null, User.Identity.GetUserId(), GetTenant().TenantID);

            return View(model);
        }

        public ActionResult GetBrands([DataSourceRequest]DataSourceRequest data)
        {
            var model = new ReportsViewModel(GetChannelId());
            model.GetComparison();

            var result = model.TelerikProducts.ToDataSourceResult(data);
            var brandList = ((IEnumerable<ProductViewModel.Telerik>)result.Data)
                .OrderBy(x => x.BrandName)
                .Select(x => x.BrandName)
                .Distinct()
                .ToList();

            return Json(brandList, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetCategories([DataSourceRequest]DataSourceRequest request)
        {
            var model = new ReportsViewModel(GetChannelId());
            model.GetComparison();

            var result = model.TelerikProducts.ToDataSourceResult(request);
            var categoryList = ((IEnumerable<ProductViewModel.Telerik>)result.Data)
                .OrderBy(x => x.CategoryName)
                .Select(x => x.CategoryName)
                .Distinct()
                .ToList();

            return Json(categoryList, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetOutcomes([DataSourceRequest]DataSourceRequest request)
        {
            var model = new ReportsViewModel(GetChannelId());
            model.GetComparison();

            var result = model.TelerikProducts.ToDataSourceResult(request);
            var outcomeList = ((IEnumerable<ProductViewModel.Telerik>)result.Data)
                .OrderBy(x => x.CalculationOutcome)
                .Where(x => x.CalculationOutcome != null)
                .Select(x => x.CalculationOutcome)
                .Distinct()
                .ToList();

            return Json(outcomeList, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetRuleNames([DataSourceRequest]DataSourceRequest request)
        {
            var model = new ReportsViewModel(GetChannelId());
            model.GetComparison();

            var result = model.TelerikProducts.ToDataSourceResult(request);
            var ruleNamesList = ((IEnumerable<ProductViewModel.Telerik>)result.Data)
                .Where(x => x.RuleName != null)
                .OrderBy(x => x.RuleName)
                .Select(x => x.RuleName)
                .Distinct()
                .ToList();

            ruleNamesList.Insert(0, "None");

            return Json(ruleNamesList, JsonRequestBehavior.AllowGet);
        }

        public ActionResult KeyLines()
        {
            model = new ReportsViewModel(GetChannelId());
            model.InitializeReport(null, User.Identity.GetUserId(), GetTenant().TenantID);

            return View(model);
        }

        public ActionResult KeyLines_Read([DataSourceRequest]DataSourceRequest request)
        {
            var model = new ReportsViewModel(GetChannelId());
            model.GetKeyLinesInventory();

            var result = model.TelerikProducts.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }
        
        public FileResult Export([DataSourceRequest]DataSourceRequest request, string columns)
        {
            var model = new ReportsViewModel(GetChannelId());
            model.GetComparison();

            var results = new List<ProductViewModel.Telerik>(model.TelerikProducts.ToDataSourceResult(request).Data as IEnumerable<ProductViewModel.Telerik>);
            var visibleColumns = columns.Split(',');

            var output = new MemoryStream();
            var writer = new Csv.CsvFileWriter(output, ',');
            var firstRow = new Csv.CsvRow();

            foreach (var col in visibleColumns)
            {
                var colNames = col.Split('|');
                firstRow.Add(colNames[1]);
            }
            writer.WriteRow(firstRow);

            foreach (var item in results)
            {
                var newRow = new Csv.CsvRow();

                foreach (var col in visibleColumns)
                {
                    var colNames = col.Split('|');
                    newRow.Add(typeof(ProductViewModel.Telerik).GetProperty(colNames[0]).GetValue(item, null).ToSafeString());
                }

                writer.WriteRow(newRow);
            }

            writer.Flush();
            output.Position = 0;

            return File(output, "text/csv", "Export.csv");
        }

        public FileResult ExportKeyLines([DataSourceRequest]DataSourceRequest request, string columns)
        {
            var model = new ReportsViewModel(GetChannelId());
            model.GetKeyLinesInventory();

            var results = new List<ProductViewModel.Telerik>(model.TelerikProducts.ToDataSourceResult(request).Data as IEnumerable<ProductViewModel.Telerik>);
            var visibleColumns = columns.Split(',');

            var output = new MemoryStream();
            var writer = new Csv.CsvFileWriter(output, ',');
            var firstRow = new Csv.CsvRow();

            foreach (var col in visibleColumns)
            {
                var colNames = col.Split('|');
                firstRow.Add(colNames[1]);
            }
            writer.WriteRow(firstRow);

            foreach (var item in results)
            {
                var newRow = new Csv.CsvRow();

                foreach (var col in visibleColumns)
                {
                    var colNames = col.Split('|');
                    newRow.Add(typeof(ProductViewModel.Telerik).GetProperty(colNames[0]).GetValue(item, null).ToSafeString());
                }

                writer.WriteRow(newRow);
            }

            writer.Flush();
            output.Position = 0;

            return File(output, "text/csv", "Export.csv");
        }

        public ActionResult TooltipDataPriceRule(int id)
        {
            var crud = new CrudPriceRule();
            var channelFk = GetChannelId();
            var priceRule = crud.Read(x => x.PriceRuleID == id && x.ChannelFK == channelFk).FirstOrDefault();

            return PartialView(priceRule);
        }

        public ActionResult Manage()
        {
            var model = new ReportConfigurationViewModel(GetChannelId());
            model.GetReportLinks(GetTenant().TenantID, User.Identity.GetUserId());
            model.CurrentUserEmail = UserManager.GetEmail(User.Identity.GetUserId());

            foreach (var report in model.ReportConfigList)
            {
                if (!string.IsNullOrEmpty(report.UserId))
                {
                    report.Owner = UserManager.FindById(report.UserId).Email;
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateInput(false)]
        [ValidateAntiForgeryToken]
        public JsonResult CreateReportConfiguration(
            string configurationValue, 
            string reportName, 
            string baseUrl,
            string area,
            int security,
            string extendedConfiguration)
        {
            var crudLookup = new CrudLookup();
            var model = new ReportConfigurationViewModel(GetChannelId());
            model.ReportConfig = new ReportConfiguration
            {
                ConfigurationValue = configurationValue,
                Name = reportName,
                BaseUrl = baseUrl,
                UserId = User.Identity.GetUserId(),
                TenantFk = GetTenant().TenantID,
                AreaFk = crudLookup.Read(x => x.LookupType.LookupTypeName == "ReportArea" && x.LookupName == area).First().LookupID,
                SecurityFk = security,
                ExtendedConfiguration = extendedConfiguration
            };

            dynamic saveReturn = model.Create();
            return Json(new { success = saveReturn.IsSuccess, reportConfigId = saveReturn.ReturnData.ReportConfigId });
        }

        [HttpPost]
        public ActionResult EditReportConfig(int id)
        {
            var model = new ReportConfigurationViewModel(GetChannelId());
            var userId = User.Identity.GetUserId();
            model.Edit(id);

            if (userId == model.ReportConfig.UserId)
            {
                return PartialView(model);
            }
            else
            {
                return PartialView(new ReportConfigurationViewModel(GetChannelId()));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult UpdateReportConfig(ReportConfigurationViewModel model)
        {
            model.ReportConfig.UserId = User.Identity.GetUserId();
            var origUserId = CrudReportConfiguration.Read(x => x.ReportConfigurationId == model.ReportConfig.ReportConfigurationId).FirstOrDefault().UserId;

            if (TryValidateModel(model.ReportConfig) && origUserId == model.ReportConfig.UserId)
            {
                var saveReturn = model.Update();
                var newModel = model.Edit(model.ReportConfig.ReportConfigurationId);
                newModel.ReportConfig.Owner = UserManager.FindById(model.ReportConfig.UserId).Email;

                return Json(new
                {
                    IsSuccess = saveReturn.IsSuccess,
                    id = model.ReportConfig.ReportConfigurationId,
                    html = RenderPartialViewToString("~/Views/Reports/ReportConfigRow.cshtml", newModel)
                },
                JsonRequestBehavior.AllowGet);
            }

            return Json(new { IsSuccess = false });
        }

        [HttpDelete]
        public JsonResult DeleteReportConfig(int id)
        {
            var model = new ReportConfigurationViewModel(GetChannelId());
            model.Edit(id);

            if (model.ReportConfig.UserId == User.Identity.GetUserId())
            {
                var sr = model.Delete();
                if (sr.IsSuccess)
                {
                    return Json(new { isSuccess = true, id = id, action = "Delete", html = "", msg = "" }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(new { isSuccess = false, id = id, action = "Delete", msg = sr.Message }, JsonRequestBehavior.AllowGet);
                }
            }

            return Json(new { isSuccess = false, id = id, action = "Delete", msg = "No Permission" }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateInput(false)]
        [ValidateAntiForgeryToken]
        public JsonResult UpdateReportConfigValue(int id, string configValue, string extendedConfiguration)
        {
            var model = new ReportConfigurationViewModel(GetChannelId());
            model.Edit(id);

            if (model.ReportConfig.UserId == User.Identity.GetUserId())
            {
                model.ReportConfig.ConfigurationValue = configValue;
                model.ReportConfig.ExtendedConfiguration = extendedConfiguration;
                var saveReturn = model.Update();
                if (saveReturn.IsSuccess)
                {
                    return Json(new { IsSuccess = true });
                }
                else
                {
                    return Json(new { IsSuccess = false });
                }
            }

            return Json(new { IsSuccess = false });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (model != null)
                    model.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}

