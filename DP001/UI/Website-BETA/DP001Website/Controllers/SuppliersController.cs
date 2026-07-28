using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DP001BusinessLogic.ViewModels;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using DP001DataAccess.Entities;
using DP001BusinessLogic;
using static System.Net.Mime.MediaTypeNames;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;
using System.IO;
using DP001DataAccess.Utilities;
using DP001Website.Models;

namespace DP001Website.Controllers
{
    [Authorize]
    public class SuppliersController : ApplicationController
    {
        private SupplierViewModel model;

        public ActionResult Inventory(int? id)
        {
            var tenantId = GetTenant().TenantID;
            int channelId = GetChannelId();
            model = new SupplierViewModel(channelId);
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
            var model = new SupplierViewModel(GetChannelId());
            model.GetInventory();

            var result = model.InventoryList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        public JsonResult SearchInventory(string term, int brandFK)
        {
            if (term.Length < 4)
            {
                return Json("", JsonRequestBehavior.AllowGet);
            }
            var model = new SupplierViewModel(GetChannelId());
            var results = model.SearchInventory(term, brandFK).SearchResults
                .Select(x => new
                {
                    Des = x.Description,
                    Id = x.SupplierInventoryID,
                    Pn = x.ManufacturerPartNo,
                    Sn = x.Supplier.SupplierName
                });

            return Json(results, JsonRequestBehavior.AllowGet);
        }

        public FileResult Export([DataSourceRequest]DataSourceRequest request, string columns)
        {
            var model = new SupplierViewModel(GetChannelId());
            model.GetInventory();

            var results = new List<SupplierViewModel.Telerik>(model.InventoryList.ToDataSourceResult(request).Data as IEnumerable<SupplierViewModel.Telerik>);
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
                    newRow.Add(typeof(SupplierViewModel.Telerik).GetProperty(colNames[0]).GetValue(item, null).ToSafeString());
                }

                writer.WriteRow(newRow);
            }

            writer.Flush();
            output.Position = 0;

            return File(output, "text/csv", "Export.csv");
        }

        //
        //Brand Aliases
        //
        public ActionResult BrandAliases()
        {
            model = new SupplierViewModel(GetChannelId());
            //model.GetBrandAliases();
            //ViewBag.MvcGridAddRow = new HtmlString("<a href=\"/Suppliers/NewBrandAlias\"><button class=\"g-cur-p btn btn-default btn-sm\"><i class=\"fa fa-plus\"></i> Add Brand Alias</button></a>");

            return View(model);
        }

        public ActionResult BrandAliases_Read([DataSourceRequest]DataSourceRequest request)
        {
            model = new SupplierViewModel(GetChannelId());
            model.GetBrandAliases();

            var result = model.BrandAliases.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        public ActionResult NewBrandAlias()
        {
            var model = new SupplierViewModel(GetChannelId());
            model.NewBrandAlias();

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        [ValidateAntiForgeryToken]
        [MultipleButton(Name = "action", Argument = "Save")]
        public JsonResult CreateBrandAlias(SupplierViewModel model)
        {
            var saveReturn = model.CreateBrandAlias(GetChannelId());
            if (saveReturn.IsSuccess)
            {
                return Json(new { isSuccess = true, id = model.SupplierBrandMatchingEntry.SupplierBrandMatchingID, action = "Save", msg = "" }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new { isSuccess = false, id = model.SupplierBrandMatchingEntry.SupplierBrandMatchingID, action = "Save", msg = saveReturn.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult EditBrandAlias(int id)
        {
            int channelId = GetChannelId();
            var model = new SupplierViewModel(channelId);
            model.EditBrandAlias(id);

            if (model.SupplierBrandMatchingEntry != null)
            {
                return PartialView(model);
            }
            else
            {
                return RedirectToAction("BrandAliases");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult UpdateBrandAlias(SupplierViewModel model)
        {
            model.ChannelID = GetChannelId();
            var saveReturn = model.UpdateBrandAlias(model.SupplierBrandMatchingEntry);
            if (saveReturn.IsSuccess)
            {
                return Json(new { isSuccess = true, id = model.SupplierBrandMatchingEntry.SupplierBrandMatchingID, action = "Save", html = "", msg = "" }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new { isSuccess = false, id = model.SupplierBrandMatchingEntry.SupplierBrandMatchingID, action = "Save", msg = saveReturn.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpDelete]
        public ActionResult DeleteBrandAlias(int id)
        {
            int channelId = GetChannelId();
            var model = new SupplierViewModel(channelId);
            model.DeleteBrandAlias(id);

            return RedirectToAction("BrandAliases");
        }

        //
        //MfPN Aliases
        //
        public ActionResult MfPNAliases()
        {
            //int channelId = GetChannelId();
            //model = new SupplierViewModel(channelId);
            //model.GetMfpnAliases();
            //ViewBag.MvcGridAddRow = new HtmlString("<a href=\"/Suppliers/NewMfpnAlias\"><button class=\"g-cur-p btn btn-default btn-sm\"><i class=\"fa fa-plus\"></i> Add Part No. Alias</button></a>");

            return View(model);
        }

        public ActionResult MfPNAliases_Read([DataSourceRequest]DataSourceRequest request)
        {
            model = new SupplierViewModel(GetChannelId());
            model.GetMfpnAliases();

            var result = model.MfpnAliases.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        public ActionResult NewMfPNAlias()
        {
            var model = new SupplierViewModel(GetChannelId());
            model.NewMfpnAlias();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult CreateMfPNAlias(SupplierViewModel model)
        {
            model.SupplierMfpnMatchingEntry.ChannelFK = GetChannelId();
            var saveReturn = model.CreateMfpnAlias();
            if (saveReturn.IsSuccess)
            {
                return Json(new { isSuccess = true, id = model.SupplierMfpnMatchingEntry.SupplierMfpnMatchingID, action = "Save", msg = "" }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new { isSuccess = false, id = model.SupplierMfpnMatchingEntry.SupplierMfpnMatchingID, action = "Save", msg = saveReturn.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult EditMfPNAlias(int id)
        {
            int channelId = GetChannelId();
            var model = new SupplierViewModel(channelId);
            model.EditMfpnAlias(id);

            if (model.SupplierMfpnMatchingEntry != null)
            {
                return PartialView(model);
            }
            else
            {
                return RedirectToAction("MfpnAliases");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult UpdateMfPNAlias(SupplierViewModel model)
        {
            model.SupplierMfpnMatchingEntry.ChannelFK = GetChannelId();
            var saveReturn = model.UpdateMfpnAlias(model.SupplierMfpnMatchingEntry);
            if (saveReturn.IsSuccess)
            {
                return Json(new { isSuccess = true, id = model.SupplierMfpnMatchingEntry.SupplierMfpnMatchingID, action = "Save", html = "", msg = "" }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new { isSuccess = false, id = model.SupplierMfpnMatchingEntry.SupplierMfpnMatchingID, action = "Save", msg = saveReturn.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpDelete]
        public ActionResult DeleteMfpnAlias(int id)
        {
            var model = new SupplierViewModel(GetChannelId());
            model.DeleteMfpnAlias(id);

            return RedirectToAction("MfpnAliases");
        }

        public ActionResult GetBrands([DataSourceRequest]DataSourceRequest data)
        {
            var model = new SupplierViewModel(GetChannelId());
            model.GetInventory();

            var result = model.InventoryList.ToDataSourceResult(data);
            var brandList = ((IEnumerable<SupplierViewModel.Telerik>)result.Data)
                .OrderBy(x => x.BrandName)
                .Select(x => x.BrandName)
                .Distinct()
                .ToList();

            return Json(brandList, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetSuppliers([DataSourceRequest]DataSourceRequest data)
        {
            var model = new SupplierViewModel(GetChannelId());
            model.GetInventory();

            var result = model.InventoryList.ToDataSourceResult(data);
            var brandList = ((IEnumerable<SupplierViewModel.Telerik>)result.Data)
                .OrderBy(x => x.SupplierName)
                .Select(x => x.SupplierName)
                .Distinct()
                .ToList();

            return Json(brandList, JsonRequestBehavior.AllowGet);
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
