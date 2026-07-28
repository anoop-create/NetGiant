using DP001BusinessLogic.ViewModels;
using DP001DataAccess.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DP001Website.Models;
using static System.Net.Mime.MediaTypeNames;

namespace DP001Website.Controllers
{
    [Authorize]
    public class PriceRulesController : ApplicationController
    {
        private PriceRuleViewModel model;

        // GET: PriceRules
        public ActionResult Index()
        {
            int channelId = GetChannelId();
            model = new PriceRuleViewModel(channelId);
            model.GetRules();
            ViewBag.MvcGridAddRow = new HtmlString("<a href=\"/PriceRules/New\"><button class=\"g-cur-p btn btn-default btn-sm\"><i class=\"fa fa-plus\"></i> Add Price Rule</button></a>");
            ViewBag.tenantId = GetTenant().TenantID;
            ViewBag.channelId = channelId;

            return View(model);
        }

        public ActionResult Edit(int id)
        {
            int channelId = GetChannelId();
            var model = new PriceRuleViewModel(channelId);
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

        public ActionResult EditBand(int id)
        {
            int channelId = GetChannelId();
            var model = new PriceRuleViewModel(channelId);
            ViewBag.tenant = GetTenant();
            model.Edit(id);

            ViewData.TemplateInfo.HtmlFieldPrefix = "Band";

            return PartialView(model);
            //return PartialView("~/Views/PriceRules/Test.cshtml", model);
        }

        public ActionResult NewBand(int id)
        {
            //var tenant = GetTenant();
            int channelId = GetChannelId();
            var model = new PriceRuleViewModel(channelId);
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

            ViewData.TemplateInfo.HtmlFieldPrefix = "Band";
            return PartialView(model);
        }

        public ActionResult New()
        {
            int channelId = GetChannelId();
            var model = new PriceRuleViewModel(channelId);
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

        [DeleteFile]
        public FileResult ExportToExcel()
        {
            model = new PriceRuleViewModel(GetChannelId());
            model.GetRules();

            return File(model.CreateExportFile(), Application.Octet, "PriceologyExport.csv");
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