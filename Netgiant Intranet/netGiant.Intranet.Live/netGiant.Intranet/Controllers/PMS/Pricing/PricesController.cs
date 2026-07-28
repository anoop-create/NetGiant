using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Pricing;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Product;
using netGiant.Intranet.Models;
using netGiant.Intranet.DataLayer;

namespace netGiant.Intranet.Controllers.PMS.Pricing
{
    [Authorize]
    public class PricesController : ApplicationController
    {
        // GET: Prices
        public ActionResult PriceComparison()
        {
            PricesViewModel pvm = new PricesViewModel();
            pvm.SelectedWebsiteFK = 1;
            return View("~/Views/PMS/Pricing/PriceComparison.cshtml", pvm.GetPriceComparison(null, null, null, null,
                                                                                            null, null, null, null,
                                                                                            true, null, null, null,
                                                                                            1, null));
        }

        [HttpPost]
        public ActionResult PriceComparisonData(List<string> optionsArray)
        {
            PricesViewModel pvm = new PricesViewModel();
            int manuFK = Convert.ToInt32(optionsArray[0]);
            int categoryCodeFK = Convert.ToInt32(optionsArray[1]);
            string searchBy = optionsArray[2];
            string searchTerm = optionsArray[3];

            double? priceFrom = Convert.ToDouble(optionsArray[4]);
            double? priceTo = Convert.ToDouble(optionsArray[5]);

            if (priceFrom == 0)
            {
                priceFrom = null;
            }
            if (priceTo == 0)
            {
                priceTo = null;
            }

            string orderBy = optionsArray[6];
            bool inStock = Convert.ToBoolean(optionsArray[7]);
            int compKey = Convert.ToInt32(optionsArray[8]);
            int bestKey = Convert.ToInt32(optionsArray[9]);
            int itemTypeFK = Convert.ToInt32(optionsArray[10]);
            int websiteFK = Convert.ToInt32(optionsArray[11]);
            int pageNumber = Convert.ToInt32(optionsArray[13]);
            int productGroupFK = Convert.ToInt32(optionsArray[12]);

            pvm.GetPriceComparison(pageNumber, manuFK, categoryCodeFK,
                priceFrom, priceTo, orderBy, searchBy, searchTerm, inStock,
                compKey, bestKey, itemTypeFK, websiteFK, productGroupFK);

            return GetJson(pvm);
        }

        [ChildActionOnly]
        public ActionResult PriceComparisonList(List<websiteInventory> Model)
        {
            return PartialView("~/Views/PMS/Pricing/PriceComparisonData.cshtml", Model);
        }

        public ActionResult PriceComparisonBreaks()
        {
            PricesViewModel pvm = new PricesViewModel();
            pvm.SelectedWebsiteFK = 1;
            return View("~/Views/PMS/Pricing/PriceComparisonBreaks.cshtml", pvm.GetPriceComparison(null, null, null, null,
                                                                                            null, null, null, null,
                                                                                            true, null, null, null,
                                                                                            1, null));
        }

        [HttpPost]
        public ActionResult PriceComparisonBreaksData(List<string> optionsArray)
        {
            PricesViewModel pvm = new PricesViewModel();
            int manuFK = Convert.ToInt32(optionsArray[0]);
            int categoryCodeFK = Convert.ToInt32(optionsArray[1]);
            string searchBy = optionsArray[2];
            string searchTerm = optionsArray[3];

            double? priceFrom = Convert.ToDouble(optionsArray[4]);
            double? priceTo = Convert.ToDouble(optionsArray[5]);

            if (priceFrom == 0)
            {
                priceFrom = null;
            }
            if (priceTo == 0)
            {
                priceTo = null;
            }

            string orderBy = optionsArray[6];
            bool inStock = Convert.ToBoolean(optionsArray[7]);
            int compKey = Convert.ToInt32(optionsArray[8]);
            int bestKey = Convert.ToInt32(optionsArray[9]);
            int itemTypeFK = Convert.ToInt32(optionsArray[10]);
            int websiteFK = Convert.ToInt32(optionsArray[11]);
            int pageNumber = Convert.ToInt32(optionsArray[13]);
            int productGroupFK = Convert.ToInt32(optionsArray[12]);

            pvm.GetPriceComparison(pageNumber, manuFK, categoryCodeFK,
                priceFrom, priceTo, orderBy, searchBy, searchTerm, inStock,
                compKey, bestKey, itemTypeFK, websiteFK, productGroupFK);

            return GetJsonBreaks(pvm);
        }

        [ChildActionOnly]
        public ActionResult PriceComparisonBreaksList(List<websiteInventory> model)
        {
            return PartialView("~/Views/PMS/Pricing/PriceComparisonBreaksData.cshtml", model);
        }

        public string GetCategoryCodes(int id)
        {
            PricesViewModel model = new PricesViewModel();

            System.Web.Script.Serialization.JavaScriptSerializer jSearializer =
                   new System.Web.Script.Serialization.JavaScriptSerializer();
            return jSearializer.Serialize(model.GetAllCategoryCodes(id, true));

            //return model.GetAllCategoryCodes(id);
        }

        public ActionResult CompetitorPricingData(string partNo)
        {
            ProductFieldViewModel model = new ProductFieldViewModel();
            model.SelectedPartNo = partNo;
            return PartialView("~/Views/PMS/Product/ProductFields/ProductCompetitorData.cshtml",
                                model.GetProductCompetitors().productCompetitors);
        }

        private ActionResult GetJson(PricesViewModel model)
        {
            JsonModel jsonModel = new JsonModel();
            jsonModel.NoMoreData = model.PriceComparison.Count < 50;
            jsonModel.Count = model.PriceComparisonCount;
            jsonModel.HTMLString = base.RenderPartialViewToString("~/Views/PMS/Pricing/PriceComparisonData.cshtml",
                model.PriceComparison);
            jsonModel.InfoBoxMessage = TempData["InformationBoxFlag"] != null ? TempData["InformationBoxFlag"].ToString() : "";
            return Json(jsonModel);
        }

        private ActionResult GetJsonBreaks(PricesViewModel model)
        {
            JsonModel jsonModel = new JsonModel();
            jsonModel.NoMoreData = model.PriceComparison.Count < 50;
            jsonModel.Count = model.PriceComparisonCount;
            jsonModel.HTMLString = base.RenderPartialViewToString("~/Views/PMS/Pricing/PriceComparisonBreaksData.cshtml",
                model.PriceComparison);
            jsonModel.InfoBoxMessage = TempData["InformationBoxFlag"] != null ? TempData["InformationBoxFlag"].ToString() : "";
            return Json(jsonModel);
        }

        public ActionResult PMSAxisPriceDifference()
        {
            PricesViewModel model = new PricesViewModel();
            model.SelectedWebsiteFK = 1;
            int websiteFK = 1;
            return View("~/Views/PMS/Pricing/PMSAxisPriceDiff.cshtml", 
                model.GetPMSAxisPriceComparison(null, websiteFK, null, null, null, null, null));
        }

        [ChildActionOnly]
        public ActionResult AxisPriceDifferenceList(List<PMSAxisPriceDifference> model)
        {
            return PartialView("~/Views/PMS/Pricing/PMSAxisPriceDiffData.cshtml", model);
        }

        public ActionResult AxisPriceDifferenceData(string[] optionsArray)
        {
            PricesViewModel model = new PricesViewModel();
            model.GetPMSAxisPriceComparison(Convert.ToInt32(optionsArray[6]), 
                Convert.ToInt32(optionsArray[0]),
                optionsArray[1],
                Convert.ToInt32(optionsArray[2]),
                optionsArray[3],
                Convert.ToInt32(optionsArray[4]),
                Convert.ToInt32(optionsArray[5]));

            JsonModel jsonModel = new JsonModel();
            jsonModel.NoMoreData = model.PMSAxisPriceDifferenceList.Count < 50;
            jsonModel.Count = model.PMSAxisPriceDifferenceCount;
            jsonModel.HTMLString = base.RenderPartialViewToString("~/Views/PMS/Pricing/PMSAxisPriceDiffData.cshtml",
                model.PMSAxisPriceDifferenceList);
            jsonModel.InfoBoxMessage = TempData["InformationBoxFlag"] != null ? TempData["InformationBoxFlag"].ToString() : "";
            return Json(jsonModel);
        }
    }
}