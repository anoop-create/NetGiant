using DP001BusinessLogic.ViewModels;
using DP001DataAccess.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DP001Website.Models;
using static System.Net.Mime.MediaTypeNames;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;
using Microsoft.AspNet.Identity;
using System.IO;
using DP001DataAccess.Utilities;

namespace DP001Website.Controllers
{
    [Authorize]
    public class PriceRulesController : ApplicationController
    {
        private PriceRuleViewModel model;

        // GET: PriceRules
        public ActionResult Index(int? id)
        {
            var tenantId = GetTenant().TenantID;
            int channelId = GetChannelId();
            model = new PriceRuleViewModel(channelId);
            model.InitializeReport(id, User.Identity.GetUserId(), tenantId);

            if (model.RequestedReportTenantId != null)
            {
                if (model.RequestedReportTenantId != tenantId)
                {
                    RouteData.Values.Remove("id");
                    return RedirectToAction("Index");
                }
            }

            ViewBag.tenantId = tenantId;
            ViewBag.channelId = channelId;

            return View(model);
        }

        public ActionResult Index_Read([DataSourceRequest]DataSourceRequest request)
        {
            var model = new PriceRuleViewModel(GetChannelId());
            model.GetRules();

            var result = model.PriceRulesList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
        }

        [SetCulture]
        public ActionResult Edit(int id)
        {
            int channelId = GetChannelId();
            var model = new PriceRuleViewModel(channelId);
            model.Channel = GetChannel();
            model.CustomFieldList = model.Channel.CustomFields.Where(x => x.Lookup.LookupName == "Price Rule Field").ToList();
            model.AdjFieldList = model.Channel.CustomFields.Where(x => x.Lookup.LookupName == "Price Adjustment Field").ToList();
            ViewBag.tenant = GetTenant();
            model.Edit(id);

            if (model.PriceRuleEntry == null)
            {
                return RedirectToAction("Index");
            }
            else
            {
                return View(model);
            }
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        [ValidateAntiForgeryToken]
        [MultipleButton(Name = "action", Argument = "Update")]
        public JsonResult Update(PriceRuleViewModel model, bool BandingChange)
        {
            //bool isBandingChange = Request.QueryString["BandingChange"];
            model.PriceRuleEntry.ChannelFK = GetChannelId();
            model.Channel = GetChannel();

            var saveReturn = model.Update(model.PriceRuleEntry, BandingChange);
            if (saveReturn.IsSuccess)
            {
                CommonModel cm = new CommonModel();
                cm.RefreshTenantSession();

                return Json(new {
                    IsSuccess = true,
                    Id = model.PriceRuleEntry.PriceRuleID,
                    Action = "Save",
                    Msg = ""
                }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new {
                    IsSuccess = false,
                    Id = model.PriceRuleEntry.PriceRuleID,
                    Action = "Save",
                    Msg = saveReturn.Message
                }, JsonRequestBehavior.AllowGet);
            }

            //return RedirectToAction("Edit", new { id = model.PriceRuleEntry.PriceRuleID });
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        [ValidateAntiForgeryToken]
        [MultipleButton(Name = "action", Argument = "Save")]
        public JsonResult Create(PriceRuleViewModel model)
        {
            model.PriceRuleEntry.ChannelFK = GetChannelId();
            model.Channel = GetChannel();
            var saveReturn = model.Create(model.PriceRuleEntry);

            if (saveReturn.IsSuccess)
            {
                CommonModel cm = new CommonModel();
                cm.RefreshTenantSession();

                //return RedirectToAction("Edit", new { id = model.PriceRuleEntry.PriceRuleID });
                return Json(new {
                    IsSuccess = true,
                    Id = model.PriceRuleEntry.PriceRuleID,
                    Action = "Save",
                    Msg = "" }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                var newModel = new PriceRuleViewModel(model.PriceRuleEntry.ChannelFK);
                newModel.Channel = GetChannel();
                newModel.New();
                newModel.PriceRuleEntry = model.PriceRuleEntry;

                //ModelState.AddModelError("", saveReturn.EntityValidationError);
                //ModelState.AddModelError("", saveReturn.Message);

                //return View("New", newModel);
                return Json(new
                {
                    IsSuccess = false,
                    Id = model.PriceRuleEntry.PriceRuleID,
                    Action = "Save",
                    Msg = saveReturn.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        [ValidateAntiForgeryToken]
        [MultipleButton(Name = "action", Argument = "UpdateBand")]
        public JsonResult UpdateBand([Bind(Prefix = "Band")] PriceRuleViewModel model)
        {
            model.PriceRuleEntry.ChannelFK = GetChannelId();
            model.Channel = GetChannel();

            var pvHtml = RenderPartialViewToString("BandRow", model.PriceRuleEntry);
            var ruleId = model.PriceRuleEntry.PriceRuleID;
            var saveReturn = model.Update(model.PriceRuleEntry, false);

            CommonModel cm = new CommonModel();
            cm.RefreshTenantSession();

            return Json(new
            {
                Html = pvHtml,
                Id = ruleId,
                IsSuccess = saveReturn.IsSuccess,
                Msg = saveReturn.Message,
                Exception = saveReturn.InnerException
            });
        }

        [SetCulture]
        public ActionResult EditBand(int id)
        {
            int channelId = GetChannelId();
            var model = new PriceRuleViewModel(channelId);
            model.Channel = GetChannel();
            model.CustomFieldList = model.Channel.CustomFields.Where(x => x.Lookup.LookupName == "Price Rule Field").ToList();
            model.AdjFieldList = model.Channel.CustomFields.Where(x => x.Lookup.LookupName == "Price Adjustment Field").ToList();
            ViewBag.tenant = GetTenant();
            model.Edit(id);

            ViewData.TemplateInfo.HtmlFieldPrefix = "Band";

            return PartialView(model);
            //return PartialView("~/Views/PriceRules/Test.cshtml", model);
        }

        [SetCulture]
        public ActionResult NewBand(int id)
        {
            //var tenant = GetTenant();
            int channelId = GetChannelId();
            var model = new PriceRuleViewModel(channelId);
            model.Channel = GetChannel();
            model.CustomFieldList = model.Channel.CustomFields.Where(x => x.Lookup.LookupName == "Price Rule Field").ToList();
            model.AdjFieldList = model.Channel.CustomFields.Where(x => x.Lookup.LookupName == "Price Adjustment Field").ToList();
            ViewBag.tenant = GetTenant();
            var savedData = model.Edit(id).PriceRuleEntry;
            model.PriceRuleEntry = new PriceRule();
            model.PriceRuleEntry.RuleName = savedData.RuleName;
            model.PriceRuleEntry.RuleTypeFK = savedData.RuleTypeFK;
            model.PriceRuleEntry.BrandFK = savedData.BrandFK;
            model.PriceRuleEntry.ProductCategoryFK = savedData.ProductCategoryFK;
            model.PriceRuleEntry.MethodFK = savedData.MethodFK;
            model.PriceRuleEntry.IsBanding = savedData.IsBanding;
            model.PriceRuleEntry.Lookup = savedData.Lookup;
            model.PriceRuleEntry.Lookup1 = savedData.Lookup1;
            model.PriceRuleEntry.RoundingGroupFK = GetChannel().RoundingGroupFK;
            model.PriceRuleEntry.BandStart = 0;
            model.PriceRuleEntry.BandEnd = 0;

            ViewData.TemplateInfo.HtmlFieldPrefix = "Band";
            return PartialView(model);
        }

        [SetCulture]
        public ActionResult New()
        {
            int channelId = GetChannelId();
            var model = new PriceRuleViewModel(channelId);
            model.Channel = GetChannel();
            model.CustomFieldList = model.Channel.CustomFields.Where(x => x.Lookup.LookupName == "Price Rule Field").ToList();
            model.AdjFieldList = model.Channel.CustomFields.Where(x => x.Lookup.LookupName == "Price Adjustment Field").ToList();
            ViewBag.tenant = GetTenant();
            model.New();

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        [ValidateAntiForgeryToken]
        [MultipleButton(Name = "action", Argument = "SaveBand")]
        public JsonResult CreateBand([Bind(Prefix = "Band")] PriceRuleViewModel model)
        {
            model.PriceRuleEntry.ChannelFK = GetChannelId();
            model.Channel = GetChannel();

            var saveReturn = model.Create(model.PriceRuleEntry);
            var ruleId = model.PriceRuleEntry.PriceRuleID;
            var pvHtml = RenderPartialViewToString("BandRow", model.PriceRuleEntry);

            CommonModel cm = new CommonModel();
            cm.RefreshTenantSession();

            return Json(new
            {
                Html = pvHtml,
                Id = ruleId,
                IsSuccess = saveReturn.IsSuccess,
                Msg = saveReturn.Message,
                Exception = saveReturn.InnerException
            });
        }

        [HttpDelete]
        [Authorize(Roles = "Administrator")]
        public JsonResult Delete(int id)
        {
            int channelId = GetChannelId();
            var model = new PriceRuleViewModel(channelId);
            var saveReturn = model.Delete(id);

            CommonModel cm = new CommonModel();
            cm.RefreshTenantSession();

            return Json(new
            {
                IsSuccess = saveReturn.IsSuccess,
                Msg = saveReturn.Message,
                Exception = saveReturn.InnerException,
                Id = id

            }
            , JsonRequestBehavior.AllowGet);
        }

        public string PriceRuleTooltipData(int id)
        {
            string html = "";

            try
            {
                var model = new PriceRuleViewModel(GetChannelId());
                var results = model.GetRule(id);

                if (model.PriceRuleEntry != null)
                {
                    html = RenderPartialViewToString("PriceRuleTooltipData", model);
                }
            }
            catch (Exception e)
            {
                html = e.Message;
            }

            return html;
        }

        public FileResult Export([DataSourceRequest]DataSourceRequest request, string columns)
        {
            var model = new PriceRuleViewModel(GetChannelId());
            model.GetRules();

            var results = new List<PriceRuleViewModel.Telerik>(model.PriceRulesList.ToDataSourceResult(request).Data as IEnumerable<PriceRuleViewModel.Telerik>);
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
                    newRow.Add(typeof(PriceRuleViewModel.Telerik).GetProperty(colNames[0]).GetValue(item, null).ToSafeString());
                }

                writer.WriteRow(newRow);
            }

            writer.Flush();
            output.Position = 0;

            return File(output, "text/csv", "Export.csv");
        }

        public ActionResult GetBrands([DataSourceRequest]DataSourceRequest data)
        {
            var model = new PriceRuleViewModel(GetChannelId());
            model.GetRules();

            var result = model.PriceRulesList.ToDataSourceResult(data);
            var brandList = ((IEnumerable<PriceRuleViewModel.Telerik>)result.Data)
                .Where(x => x.BrandName != null)
                .OrderBy(x => x.BrandName)
                .Select(x => x.BrandName)
                .Distinct()
                .ToList();

            return Json(brandList, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetCategories([DataSourceRequest]DataSourceRequest request)
        {
            var model = new PriceRuleViewModel(GetChannelId());
            model.GetRules();

            var result = model.PriceRulesList.ToDataSourceResult(request);
            var categoryList = ((IEnumerable<PriceRuleViewModel.Telerik>)result.Data)
                .Where(x => x.CategoryName != null)
                .OrderBy(x => x.CategoryName)
                .Select(x => x.CategoryName)
                .Distinct()
                .ToList();

            return Json(categoryList, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetMethods([DataSourceRequest]DataSourceRequest request)
        {
            var model = new PriceRuleViewModel(GetChannelId());
            model.GetRules();

            var result = model.PriceRulesList.ToDataSourceResult(request);
            var methodList = ((IEnumerable<PriceRuleViewModel.Telerik>)result.Data)
                .Where(x => x.Method != null)
                .OrderBy(x => x.Method)
                .Select(x => x.Method)
                .Distinct()
                .ToList();

            return Json(methodList, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetRuleTypes([DataSourceRequest]DataSourceRequest request)
        {
            var model = new PriceRuleViewModel(GetChannelId());
            model.GetRules();

            var result = model.PriceRulesList.ToDataSourceResult(request);
            var ruleTypeList = ((IEnumerable<PriceRuleViewModel.Telerik>)result.Data)
                .Where(x => x.RuleType != null)
                .OrderBy(x => x.RuleType)
                .Select(x => x.RuleType)
                .Distinct()
                .ToList();

            return Json(ruleTypeList, JsonRequestBehavior.AllowGet);
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
