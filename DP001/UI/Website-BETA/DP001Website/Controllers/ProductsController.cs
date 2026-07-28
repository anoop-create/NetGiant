using DP001BusinessLogic;
using DP001Website.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DP001BusinessLogic.ViewModels;
using static System.Net.Mime.MediaTypeNames;
using Kendo.Mvc.UI;
using DP001DataAccess.Entities;
using Kendo.Mvc.Extensions;
//using System.Reflection;
using Microsoft.AspNet.Identity;
using System.IO;
using DP001DataAccess.Utilities;

namespace DP001Website.Controllers
{
    [Authorize]
    public class ProductsController : ApplicationController
    {
        private ProductViewModel model;

        public ActionResult Index(int? id)
        {
            var tenantId = GetTenant().TenantID;
            int channelId = GetChannelId();
            model = new ProductViewModel(channelId);
            model.InitializeReport(id, User.Identity.GetUserId(), tenantId);

            if (model.RequestedReportTenantId != null)
            {
                if (model.RequestedReportTenantId != tenantId)
                {
                    RouteData.Values.Remove("id");
                    return RedirectToAction("Index");
                }
            }

            return View(model);
        }
        
        public ActionResult Index_Read([DataSourceRequest]DataSourceRequest request)
        {
            var model = new ProductViewModel(GetChannelId());
            model.GetInventory();

            var result = model.InventoryList.ToDataSourceResult(request);
            var jsonResult = Json(result);
            jsonResult.MaxJsonLength = int.MaxValue;
            return jsonResult;
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

