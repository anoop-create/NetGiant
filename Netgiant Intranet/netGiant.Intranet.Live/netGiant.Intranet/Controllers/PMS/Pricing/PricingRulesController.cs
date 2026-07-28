using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Pricing;
using netGiant.Intranet.DataLayer;
using netGiant.Intranet;
using netGiant.Intranet.Models;
using System.IO;

namespace netGiant.Intranet.Controllers.PMS.Pricing
{
    [Authorize]
    public class PricingRulesController : ApplicationController
    {
        // GET: PriceRule
        public ActionResult PricingRules()
        {
            PricingRulesViewModel prVm = new PricingRulesViewModel();
            GetSessionSearch(prVm);

            return View("~/Views/PMS/Pricing/PricingRules.cshtml", prVm.Get(prVm.selectedPageNumber, prVm.selectedOrderBy, 
                                                        prVm.selectedWebsiteFK, prVm.selectedCategoryCodeFK,
                                                        prVm.selectedRuleTypeFK));
        }

        [ChildActionOnly]
        public ActionResult PricingRuleList(List<priceRule> model)
        {
            return PartialView("~/Views/PMS/Pricing/PricingRulesData.cshtml", model);
        }

        [HttpPost]
        public ActionResult PricingRulesData(List<string> optionsArray)
        {
            PricingRulesViewModel prVm = new PricingRulesViewModel();
            SetSessionSearch(optionsArray, prVm);
            prVm.Get(prVm.selectedPageNumber,
                                prVm.selectedOrderBy, prVm.selectedWebsiteFK,
                                prVm.selectedCategoryCodeFK,
                                prVm.selectedRuleTypeFK);
            return GetJson(prVm);
        }

        private ActionResult GetJson(PricingRulesViewModel model)
        {
            JsonModel jsonModel = new JsonModel();
            jsonModel.NoMoreData = model.priceRuleList.Count < 50;
            jsonModel.Count = model.PriceRulesCount;
            jsonModel.HTMLString = base.RenderPartialViewToString("~/Views/PMS/Pricing/PricingRulesData.cshtml",
                model.priceRuleList);
            jsonModel.InfoBoxMessage = TempData["InformationBoxFlag"] != null ? TempData["InformationBoxFlag"].ToString() : "";
            return Json(jsonModel);
        }

        public ActionResult PricingBandsData(int priceRuleID)
        {
            PricingRulesViewModel prVm = new PricingRulesViewModel();
            return PartialView("~/Views/PMS/Pricing/PricingRuleBandsData.cshtml", prVm.GetPricingBands(priceRuleID));
        }

        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult CreatePricingRule(int id)
        {
            return View("~/Views/PMS/Pricing/CreatePricingRule.cshtml", PricingRulesViewModel.CreatePricingRule(id));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult SavePricingRule(PricingRulesViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    int newID = model.SavePricingRule();
                    //model.SavePricingRuleBandings(newID);
                    TempData["InformationBoxFlag"] = "Price Rule Saved";
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                    return RedirectToAction("CreatePricingRule", new { id = model.priceRuleSingle.priceRuleID });
                }
            }
            
            return RedirectToAction("PricingRules");
        }

        [HttpPost]
        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult DeletePricingRule(List<string> optionsArray)
        {
            PricingRulesViewModel prVm = new PricingRulesViewModel();
            prVm.DeletePriceRule(Convert.ToInt32(optionsArray[3]));
            TempData["InformationBoxFlag"] = "Price Rule Deleted";
            SetSessionSearch(optionsArray, prVm);
            prVm.Get(prVm.selectedPageNumber,
                                prVm.selectedOrderBy, prVm.selectedWebsiteFK,
                                prVm.selectedCategoryCodeFK,
                                prVm.selectedRuleTypeFK);
            return GetJson(prVm);
        }

        [Authorize(Roles="IntranetAdmin, PMSAdmin")]
        public ActionResult StartProcessCalculatePrices()
        {
            string result = StartProcess.StartScheduledTask("Netgiant\\Calculate PMS Prices");
            TempData["InformationBoxFlag"] = result;
            return RedirectToAction("PricingRules");
        }

        private void SetSessionSearch(List<string> optionsArray, PricingRulesViewModel prVm)
        {
            prVm.selectedWebsiteFK = Convert.ToInt32(optionsArray[0]);
            prVm.selectedCategoryCodeFK = Convert.ToInt32(optionsArray[1]);
            prVm.selectedRuleTypeFK = Convert.ToInt32(optionsArray[4]);
            prVm.selectedPageNumber = Convert.ToInt32(optionsArray[5]);
            prVm.selectedOrderBy = optionsArray[2];

            Dictionary<string, object> dState = new Dictionary<string, object>();
            dState.Add("websiteFK", prVm.selectedWebsiteFK);
            dState.Add("categoryCodeFK", prVm.selectedCategoryCodeFK);
            dState.Add("ruleTypeFK", prVm.selectedRuleTypeFK);
            dState.Add("pageNumber", prVm.selectedPageNumber);
            dState.Add("orderBy", prVm.selectedOrderBy);
            
            Session["pricingRulesDictionary"] = dState;
        }

        private void GetSessionSearch(PricingRulesViewModel prVm)
        {
            if (Session["pricingRulesDictionary"] != null)
            {
                Dictionary<string, object> dState = (Dictionary<string, object>)Session["pricingRulesDictionary"];
                prVm.selectedWebsiteFK = (int)dState["websiteFK"];
                prVm.selectedCategoryCodeFK = (int)dState["categoryCodeFK"];
                prVm.selectedRuleTypeFK = (int)dState["ruleTypeFK"];
                prVm.selectedPageNumber = (int)dState["pageNumber"];
                prVm.selectedOrderBy = dState["orderBy"].ToString();
            }
        }

        public ActionResult ResetSearch()
        {
            Session.Remove("pricingRulesDictionary");
            return RedirectToAction("PricingRules");
        }

        [HttpPost]
        public ActionResult GetNewPriceRuleBandTableRow(int priceRuleBandId)
        {
            PricingRulesViewModel model = new PricingRulesViewModel();
            return PartialView("~/Views/PMS/Pricing/CreatePriceRuleBand.cshtml", 
                model.CreatePriceRuleBand(priceRuleBandId));
        }

        public ActionResult GetPriceRuleMoreDetail(int id)
        {
            return PartialView("~/Views/PMS/Pricing/PriceRuleMoreDetail.cshtml", 
                PricingRulesViewModel.CreatePricingRule(id).priceRuleSingle);
        }
    }
}
