using DP001BusinessLogic.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DP001BusinessLogic;
using DP001DataAccess.Entities;
using System.Drawing;
using System.IO;
using DP001Website.Models;
using DP001BusinessLogic.Shared;
using static System.Net.Mime.MediaTypeNames;

namespace DP001Website.Controllers
{
    [Authorize]
    public class ReportsController : ApplicationController
    {
        private ReportsViewModel model;

        public ActionResult Comparison()
        {
            bool? hasRuleName = null;
            model = new ReportsViewModel(GetChannelId());
            string gridFilter = Request.QueryString["rn"] != null ? Request.QueryString["rn"] : "0";

            if (gridFilter.Contains("yes"))
            {
                hasRuleName = true;
            }
            if (gridFilter.Contains("no"))
            {
                hasRuleName = false;
            }

            Channel channel = GetChannel();
            model.UsesVariantOf = false;
            if (
                !String.IsNullOrEmpty(
                    channel
                        .FTPSettings
                        .FirstOrDefault(x => x.Lookup.LookupName == "Product Inventory")
                        .FieldMapping.VariantOf))
            {
                model.UsesVariantOf = true;
            }

            model.Get(hasRuleName);
            //model.GetVariants();

            return View(model);
        }

        public ActionResult ComparisonStaging()
        {
            model = new ReportsViewModel(GetChannelId());
            Channel channel = GetChannel();
            model.UsesVariantOf = false;
            if (
                !String.IsNullOrEmpty(
                    channel
                        .FTPSettings
                        .FirstOrDefault(x => x.Lookup.LookupName == "Product Inventory")
                        .FieldMapping.VariantOf))
            {
                model.UsesVariantOf = true;
            }

            model.GetStagingPriceComparison();

            return View(model);
        }

        [DeleteFile]
        public FileResult ExportToExcel()
        {
            model = new ReportsViewModel(GetChannelId());
            model.Get();

            return File(model.CreateExportFile(), Application.Octet, "PriceologyExport.csv");
        }

        [DeleteFile]
        public FileResult ExportToExcelTestRules()
        {
            model = new ReportsViewModel(GetChannelId());
            model.GetStagingPriceComparison();

            return File(model.CreateExportFileTestRules(), Application.Octet, "PriceologyExport.csv");
        }

        public ActionResult KeyLines()
        {
            model = new ReportsViewModel(GetChannelId());
            Channel channel = GetChannel();
            model.UsesVariantOf = false;
            if (
                !String.IsNullOrEmpty(
                    channel
                        .FTPSettings
                        .FirstOrDefault(x => x.Lookup.LookupName == "Product Inventory")
                        .FieldMapping.VariantOf))
            {
                model.UsesVariantOf = true;
            }
            model.GetKeyLinesInventory();

            return View(model);
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