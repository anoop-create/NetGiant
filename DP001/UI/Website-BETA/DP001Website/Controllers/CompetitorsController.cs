using DP001BusinessLogic;
using DP001BusinessLogic.Shared;
using DP001BusinessLogic.ViewModels;
using DP001DataAccess.Entities;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.AspNet.Identity;
using System.IO;
using DP001DataAccess.Utilities;
using DP001Website.Models;
using Microsoft.Ajax.Utilities;

namespace DP001Website.Controllers
{
    [Authorize]
    public class CompetitorsController : ApplicationController
    {
        private CompetitorViewModel model;

        public ActionResult Index()
        {
            int channelId = GetChannelId();
            //model = new CompetitorViewModel(channelId);
            //model.GetCompetitorList();

            ViewBag.tenantId = GetTenant().TenantID; 
            ViewBag.channelId = channelId;

            return View(model);
        }

        public ActionResult Index_Read([DataSourceRequest]DataSourceRequest request)
        {
            int channelId = GetChannelId();
            model = new CompetitorViewModel(channelId);
            model.GetCompetitorList();

            var result = model.CompetitorList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        public ActionResult Inventory(int? id)
        {
            var tenantId = GetTenant().TenantID;
            int channelId = GetChannelId();
            model = new CompetitorViewModel(channelId);
            model.InitializeReport(id, User.Identity.GetUserId(), tenantId);

            if (model.RequestedReportTenantId != null)
            {
                if (model.RequestedReportTenantId != tenantId)
                {
                    RouteData.Values.Remove("id");
                    return RedirectToAction("Inventory");
                }
            }

            return View(model);
        }

        public ActionResult Inventory_Read([DataSourceRequest]DataSourceRequest request)
        {
            var model = new CompetitorViewModel(GetChannelId());
            model.GetInventory();

            var result = model.InventoryList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        public string CompetitorsTooltipData(int id)
        {
            var channelId = GetChannelId();
            var model = new CompetitorViewModel(channelId);
            model.GetCompetitors(id);

            if (model.CompetitorsList.Count > 0)
            {
                return RenderPartialViewToString("CompetitorsTooltipData", model);
            }
            else
            {
                return "Product competitors not found or you do not have permission to view them";
            }
        }

        public JsonResult SearchInventory(string term, int brandFK)
        {
            if (term.Length < 4)
            {
                return Json("", JsonRequestBehavior.AllowGet);
            }
            var model = new CompetitorViewModel(GetChannelId());
            var results = model.SearchInventory(term, brandFK).SearchResults
                .Select(x => new
                {
                    Id = x.CompetitorInventoryID,
                    Br = x.Brand.BrandName,
                    Pn = x.ManufacturerPartNo,
                    Cn = x.Competitor.CompetitorName
                });

            return Json(results, JsonRequestBehavior.AllowGet);
        }

        public JsonResult Search(string term, int competitorFk)
        {
            model = new CompetitorViewModel(GetChannelId());
            var results = model.Search(term, competitorFk)
                .Select(x => new
                {
                    Des = x.Description,
                    Id = x.CompetitorInventoryID,
                    Pn = x.ManufacturerPartNo,
                    Cn = x.Competitor.CompetitorName
                });

            return Json(results, JsonRequestBehavior.AllowGet);
        }

        public JsonResult SearchBrands([DataSourceRequest]DataSourceRequest request, int competitorFk)
        {
            model = new CompetitorViewModel(GetChannelId());

            var results = new List<CompetitorViewModel.TelerikCompetitorBrand>();

            if (competitorFk > 0)
            {
                results = model.SearchBrands(competitorFk);
            }

            var result = results.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        public JsonResult SearchCompetitorEntries([DataSourceRequest]DataSourceRequest request, int competitorFk)
        {
            model = new CompetitorViewModel(GetChannelId());

            var results = new List<CompetitorViewModel.Telerik>();

            if (competitorFk > 0)
            {
                results = model.Search(competitorFk);
            }

            var result = results.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        public FileResult Export([DataSourceRequest]DataSourceRequest request, string columns)
        {
            var model = new CompetitorViewModel(GetChannelId());
            model.GetInventory();

            var results = new List<CompetitorViewModel.Telerik>(model.InventoryList.ToDataSourceResult(request).Data as IEnumerable<CompetitorViewModel.Telerik>);
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
                    newRow.Add(typeof(CompetitorViewModel.Telerik).GetProperty(colNames[0]).GetValue(item, null).ToSafeString());
                }

                writer.WriteRow(newRow);
            }

            writer.Flush();
            output.Position = 0;

            return File(output, "text/csv", "Export.csv");
        }

        [HttpPost]
        public JsonResult Activate(int competitorId, bool isActive)
        {
            CompetitorViewModel model = new CompetitorViewModel(GetChannelId());
            model.CompetitorEntry = model.GetCompetitor(competitorId);
            model.CompetitorEntry.IsActive = isActive;

            var saveReturn = model.Update(model.CompetitorEntry);
            if (saveReturn.IsSuccess)
            {
                return Json(new { isSuccess = true, id = competitorId, action = "Update", html = "", msg = "" }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new { isSuccess = false, id = competitorId, action = "Update", html = "Unable to update", msg = saveReturn.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult GetBrands([DataSourceRequest]DataSourceRequest data)
        {
            var model = new CompetitorViewModel(GetChannelId());
            model.GetInventory();

            var result = model.InventoryList.ToDataSourceResult(data);
            var brandList = ((IEnumerable<CompetitorViewModel.Telerik>)result.Data)
                .OrderBy(x => x.BrandName)
                .Select(x => x.BrandName)
                .Distinct()
                .ToList();

            return Json(brandList, JsonRequestBehavior.AllowGet);
        }

        public FileResult ExportCompetitors([DataSourceRequest]DataSourceRequest request, string columns)
        {
            var model = new CompetitorViewModel(GetChannelId());
            model.GetCompetitorList();

            var results = new List<CompetitorViewModel.TelerikCompetitor>(model.CompetitorList.ToDataSourceResult(request).Data as IEnumerable<CompetitorViewModel.TelerikCompetitor>);
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
                    newRow.Add(typeof(CompetitorViewModel.TelerikCompetitor).GetProperty(colNames[0]).GetValue(item, null).ToSafeString());
                }

                writer.WriteRow(newRow);
            }

            writer.Flush();
            output.Position = 0;

            return File(output, "text/csv", "Export.csv");
        }
    }
}
