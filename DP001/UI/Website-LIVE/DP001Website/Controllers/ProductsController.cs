using DP001BusinessLogic;
using DP001Website.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DP001BusinessLogic.ViewModels;
using static System.Net.Mime.MediaTypeNames;
//using System.Reflection;

namespace DP001Website.Controllers
{
    [Authorize]
    public class ProductsController : ApplicationController
    {
        private ProductViewModel model;

        public ActionResult Index(int page = 1, int fseq = 1, string sdir = "asc")
        {
            int channelId = GetChannelId();
            model = new ProductViewModel(channelId);
            model.GetInventory();

            //List<PropertyInfo> prop = model.InventoryList.GetType().GetProperties().ToList();
            //foreach (var prop in model.InventoryList.GetType().GetProperties())
            //{

            //}

            return View(model);
        }

        public JsonResult Search(string term, int brandFK = 0)
        {
            var model = new ProductViewModel(GetChannelId());
            var results = model.Search(term, brandFK).SearchResults
                .Select(x => new
                {
                    Des = x.Description,
                    Id = x.ProductInventoryID,
                    Pn = x.ManufacturerPartNo
                });

            return Json(results, JsonRequestBehavior.AllowGet);
        }

        public string PriceHistoryTooltipData(int id)
        {
            string html = "";

            try
            {
                var model = new ProductViewModel(GetChannelId());
                var results = model.GetPriceHistory(id, true);

                if (model.PriceHistory.Count > 0)
                {
                    html = RenderPartialViewToString("PriceHistoryTooltipData", model);
                }
            }
            catch (Exception e)
            {
                html = e.Message;
            }

            return html;
        }

        public FileResult ExportToExcel()
        {
            model = new ProductViewModel(GetChannelId());
            model.GetInventory();

            return File(model.CreateExportFile(), Application.Octet, "PriceologyExport.csv");
        }

        public ActionResult ProductDetail(int id)
        {
            var model = new ProductViewModel(GetChannelId());
            model.Channel = GetChannel();
            model.CustomFieldList = model.Channel.CustomFields.Where(x => x.Lookup.LookupName == "Product Inventory Field").ToList();
            model.CustomAdjustmentList = model.Channel.CustomFields.Where(x => x.Lookup.LookupName == "Price Adjustment Field").ToList();

            try
            {
                model.GetProductDetail(id);
            }
            catch (Exception)
            {
                return RedirectToAction("Index");
            }

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