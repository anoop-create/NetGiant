using DP001BusinessLogic.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using static System.Net.Mime.MediaTypeNames;

namespace DP001Website.Controllers
{
    [Authorize]
    public class CompetitorsController : ApplicationController
    {
        private CompetitorViewModel model;

        public ActionResult Index()
        {
            int channelId = GetChannelId();
            model = new CompetitorViewModel(channelId);
            model.GetCompetitorList();

            ViewBag.tenantId = GetTenant().TenantID; 
            ViewBag.channelId = channelId;

            return View(model);
        }

        public ActionResult Inventory()
        {
            int channelId = GetChannelId();
            model = new CompetitorViewModel(channelId);
            model.GetInventory();

            return View(model);
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

        [DeleteFile]
        public FileResult ExportToExcel()
        {
            model = new CompetitorViewModel(GetChannelId());
            model.GetInventory();

            return File(model.CreateExportFile(), Application.Octet, "PriceologyExport.csv");
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
    }
}