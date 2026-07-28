using DP001BusinessLogic.Shared;
using DP001DataAccess.Entities;
using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.IO;
using System.Linq;
using System.Web.Mvc;

namespace DP001BusinessLogic.ViewModels
{
    public class PriceRuleViewModel
    {
        public PriceRuleViewModel(int channelId)
        {
            _channelId = channelId;
            _ctx = new DP001Entities();
        }

        public PriceRuleViewModel()
        {

        }

        public IQueryable<PriceRuleInfo> PriceRulesList { get; set; }
        public List<PriceRule> BandList { get; set; }
        public PriceRule PriceRuleEntry { get; set; }
        public List<SelectListItem> Categories { get; set; }
        public List<SelectListItem> Brands { get; set; }
        public List<SelectListItem> RuleTypes { get; set; }
        public List<SelectListItem> MethodTypes { get; set; }
        public List<SelectListItem> RoundingGroups { get; set; }
        private int _channelId;
        public Channel Channel { get; set; }
        public TenantSetting Tenant { get; set; }

        private DP001Entities _ctx;

        public PriceRuleViewModel GetRules()
        {
            var crud = new CrudPriceRule();
            //PriceRulesList = crud.ReadPriceRulesQuery(x => x.ChannelFK == _channelId, _ctx);
            PriceRulesList = crud.ReadPriceRulesQuery(x => x.ChannelFK == _channelId, _ctx);

            return this;
        }

        public PriceRuleViewModel Edit(int id)
        {
            var crud = new CrudPriceRule();
            PriceRuleEntry = FormatForDisplay(crud.Read(x => x.ChannelFK == _channelId && x.PriceRuleID == id).FirstOrDefault());

            if (PriceRuleEntry != null)
            {
                BandList = crud.GetRelatedBands(PriceRuleEntry, _channelId);
                Categories = SharedViewModel.GetCategoryList(_channelId, PriceRuleEntry.BrandFK ?? 0);
                Brands = SharedViewModel.GetBrandList(_channelId);
                RuleTypes = SharedViewModel.GetLookupList("PriceRuleType");
                MethodTypes = SharedViewModel.GetLookupList("Method");
                RoundingGroups = SharedViewModel.GetLookupList("RoundingGroup");
            }

            return this;
        }

        public PriceRuleViewModel New()
        {
            PriceRuleEntry = new PriceRule();
            Categories = SharedViewModel.GetCategoryList(_channelId);
            Brands = SharedViewModel.GetBrandList(_channelId);
            RuleTypes = SharedViewModel.GetLookupList("PriceRuleType");
            MethodTypes = SharedViewModel.GetLookupList("Method");
            RoundingGroups = SharedViewModel.GetLookupList("RoundingGroup");

            var crudChannel = new CrudChannel();
            PriceRuleEntry.RoundingGroupFK = crudChannel.Read(x => x.ChannelID == _channelId).FirstOrDefault().RoundingGroupFK;

            return this;
        }

        public SaveReturn Update(PriceRule priceRule, bool isBandingChange)
        {
            var saveReturn = new SaveReturn();
            var crud = new CrudPriceRule();
            int add1 = 0;
            bool isValid = true;

            var hasPermission = crud.Read(x => x.ChannelFK == Channel.ChannelID && x.PriceRuleID == priceRule.PriceRuleID).Count > 0;
            if (hasPermission)
            {

                //Uniques Rule name check
                foreach (PriceRule pr in Channel.PriceRules)
                {
                    if (pr.PriceRuleID == priceRule.PriceRuleID)
                    {
                        continue;
                    }

                    //Unique Rule name check
                    if (pr.RuleName.ToLower() == priceRule.RuleName.ToLower() && !priceRule.IsBanding)
                    {
                        saveReturn.Message = "You cannot add a price rule with the same name as an existing rule";
                        isValid = false;
                    }
                    //Unique Signature check
                    if (pr.RuleTypeFK == priceRule.RuleTypeFK
                        && pr.BrandFK == priceRule.BrandFK
                        && pr.ProductCategoryFK == priceRule.ProductCategoryFK
                        && pr.ProductInventoryFK == priceRule.ProductInventoryFK
                        && pr.IsBanding == priceRule.IsBanding
                        && pr.IsBanding == false
                        && pr.IsTest == priceRule.IsTest)
                    {
                        saveReturn.Message = "You cannot add a price rule with the same key fields as an existing rule";
                        isValid = false;
                    }
                    //Banded Rule check
                    if (pr.RuleTypeFK == priceRule.RuleTypeFK
                        && pr.BrandFK == priceRule.BrandFK
                        && pr.ProductCategoryFK == priceRule.ProductCategoryFK
                        && pr.ProductInventoryFK == priceRule.ProductInventoryFK
                        && pr.IsBanding && priceRule.IsBanding == false
                        && pr.IsActive == priceRule.IsActive)
                    {
                        saveReturn.Message = "A banded rule already exists with the same key fields";
                        isValid = false;
                    }
                    if (pr.PriceRuleID == priceRule.PriceRuleID
                        && priceRule.IsActive
                        && pr.IsActive == false)
                    {
                        add1 = 1;
                    }
                }
                //Banded Rules Must not be in Test mode
                if (priceRule.IsBanding && priceRule.IsTest)
                {
                    saveReturn.Message = "Test mode cannot be used for banded rules";
                    isValid = false;
                }
                //Price Rule limit check
                if (priceRule.IsActive)
                {
                    if (activeRuleCountExceeded(add1))
                    {
                        saveReturn.Message = "Unable to activate price rule as activating will exceed your price rule limit";
                        isValid = false;
                    }
                }
                //Banded Rule Range check
                if (priceRule.IsBanding)
                {
                    List<PriceRule> bandedRules = Channel.PriceRules.Where(x => x.RuleTypeFK == priceRule.RuleTypeFK 
                                                                && x.BrandFK == priceRule.BrandFK 
                                                                && x.ProductCategoryFK == priceRule.ProductCategoryFK 
                                                                && x.ProductInventoryFK == priceRule.ProductInventoryFK)
                                                                .OrderBy(x => x.BandStart)
                                                                .ToList();

                    foreach (PriceRule pr in bandedRules)
                    {
                        if (priceRule.PriceRuleID == pr.PriceRuleID)
                        {
                            continue;
                        }
                        if ((priceRule.BandStart >= pr.BandStart && priceRule.BandStart <= pr.BandEnd) || (priceRule.BandEnd >= pr.BandStart && priceRule.BandEnd <= pr.BandEnd))
                        {
                            saveReturn.Message = "The change you have made has resulted in an overlapping band";
                            isValid = false;
                        }
                    }
                }
                //Banding change channel count check
                if (isBandingChange)
                {
                    if (!priceRule.IsBanding)
                    {
                        //Check banding group count
                        int bandingCount = crud.Read(x => x.ChannelFK == Channel.ChannelID && x.RuleName == priceRule.RuleName && x.IsBanding).Count;
                        if (bandingCount > 1)
                        {
                            saveReturn.Message = "There are more than 1 bands associated with this banding group. You can only remove banding when there is one band";
                            isValid = false;
                        }
                    }
                }

                if (!isValid)
                {
                    saveReturn.IsSuccess = false;
                    return saveReturn;
                }

                try
                {
                    crud.Update(priceRule);
                    saveReturn.IsSuccess = true;
                }
                catch (Exception e)
                {
                    saveReturn.IsSuccess = false;
                    saveReturn.Message = e.Message;
                    saveReturn.InnerException = e.InnerException != null ? e.InnerException.Message : "";
                    //saveReturn.InnerException = e.InnerException?.Message;
                }
            }
            else
            {
                saveReturn.IsSuccess = false;
                saveReturn.Message = "You do not have permission to change this rule";
            }

            return saveReturn;
        }

        public SaveReturn Create(PriceRule priceRule)
        {
            var saveReturn = new SaveReturn();
            var crud = new CrudPriceRule();
            bool isValid = true;

            foreach (PriceRule pr in Channel.PriceRules)
            {
                //Unique Rule name check
                if (pr.RuleName.ToLower() == priceRule.RuleName.ToLower() && !priceRule.IsBanding)
                {
                    saveReturn.Message = "You cannot add a price rule with the same name as an existing rule";
                    isValid = false;
                }
                //Unique Signature check
                if (pr.RuleTypeFK == priceRule.RuleTypeFK
                    && pr.BrandFK == priceRule.BrandFK
                    && pr.ProductCategoryFK == priceRule.ProductCategoryFK
                    && pr.ProductInventoryFK == priceRule.ProductInventoryFK
                    && pr.IsBanding == priceRule.IsBanding
                    && pr.IsBanding == false
                    && pr.IsTest == priceRule.IsTest)
                {
                    saveReturn.Message = "You cannot add a price rule with the same key fields as an existing rule";
                    isValid = false;
                }
                //Banded Rule check
                if (pr.RuleTypeFK == priceRule.RuleTypeFK
                    && pr.BrandFK == priceRule.BrandFK
                    && pr.ProductCategoryFK == priceRule.ProductCategoryFK
                    && pr.ProductInventoryFK == priceRule.ProductInventoryFK
                    && pr.IsBanding && priceRule.IsBanding == false)
                {
                    saveReturn.Message = "A banded rule already exists with the same key fields";
                    isValid = false;
                }
            }
            //Banded Rules Must not be in Test mode
            if (priceRule.IsBanding && priceRule.IsTest)
            {
                saveReturn.Message = "Test mode cannot be used for banded rules";
                isValid = false;
            }
            //Price Rule limit check
            if (priceRule.IsActive)
            {
                if (activeRuleCountExceeded(1))
                {
                    saveReturn.Message = "The price rule cannot be added as adding it will exceed your price rule limit";
                    isValid = false;
                }
            }
            //Banded Rule Range check
            if (priceRule.IsBanding)
            {
                List<PriceRule> bandedRules = Channel.PriceRules.Where(x => x.RuleTypeFK == priceRule.RuleTypeFK
                                                            && x.BrandFK == priceRule.BrandFK
                                                            && x.ProductCategoryFK == priceRule.ProductCategoryFK
                                                            && x.ProductInventoryFK == priceRule.ProductInventoryFK)
                                                            .OrderBy(x => x.BandStart)
                                                            .ToList();

                foreach (PriceRule pr in bandedRules)
                {
                    if (priceRule.PriceRuleID == pr.PriceRuleID)
                    {
                        continue;
                    }
                    if ((priceRule.BandStart >= pr.BandStart && priceRule.BandStart <= pr.BandEnd) || (priceRule.BandEnd >= pr.BandStart && priceRule.BandEnd <= pr.BandEnd))
                    {
                        saveReturn.Message = "The change you have made has resulted in an overlapping band";
                        isValid = false;
                    }
                }
            }

            if (!isValid)
            {
                saveReturn.IsSuccess = false;
                return saveReturn;
            }

            try
            {
                crud.Create(priceRule);
                saveReturn.IsSuccess = true;
            }
            catch (Exception e)
            {
                saveReturn.IsSuccess = false;
                saveReturn.Message = e.Message;
                saveReturn.InnerException = e.InnerException != null ? e.InnerException.ToString() : "";

                if (e is DbEntityValidationException)
                {
                    var entityException = (DbEntityValidationException)e;
                    var errorMessages = entityException.EntityValidationErrors
                    .SelectMany(x => x.ValidationErrors)
                    .Select(x => x.ErrorMessage);

                    saveReturn.EntityValidationError = string.Join("; ", errorMessages);
                }
            }

            return saveReturn;
        }

        public SaveReturn Delete(int id)
        {
            var sr = new SaveReturn();
            var crud = new CrudPriceRule();

            try 
            {
                var deleteRecord = crud.Read(x => x.ChannelFK == _channelId
                    && x.PriceRuleID == id).FirstOrDefault();

                if (deleteRecord != null)
                {
                    sr.IsSuccess = crud.Delete(deleteRecord);
                    if (!sr.IsSuccess)
                    {
                        sr.Message = "There was a problem when trying to delete the Price Rule, the problem has been reported to technical support";
                    }
                }
                else
                {
                    throw new ArgumentException("You do not have permission to delete this rule");
                }
            }
            catch (Exception e)
            {
                sr.IsSuccess = false;
                sr.Message = e.Message;
                sr.InnerException = e.InnerException != null ? e.InnerException.Message : "";
            }

            return sr;
        }

        private PriceRule FormatForDisplay(PriceRule rule)
        {
            //rule.DesiredMargin = rule.DesiredMargin * 100;

            return rule;
        }

        private bool activeRuleCountExceeded(int add1)
        {
            bool exceeded = true;
            var crud = new CrudPriceRule();

            int ruleCount = crud.GetTenantRuleCount(Channel.TenantFK);
            if (ruleCount + add1 <= Channel.TenantSetting.Contract.PriceRuleCount)
            {
                exceeded = false;
            }

            return exceeded;
        }

        public Stream CreateExportFile()
        {
            var data = PriceRulesList.Select(x => new
            {
                Rule_Name = x.Rule.RuleName,
                Rule_Type = x.Rule.Lookup1 != null ? x.Rule.Lookup1.LookupName : "",
                Banding = x.Rule.IsBanding ? "Yes" : "No",
                Method = x.Rule.Lookup != null ? x.Rule.Lookup.LookupName : "",
                Category = x.Rule.ProductCategory != null ? x.Rule.ProductCategory.CategoryName : "",
                Brand = x.Rule.Brand != null ? x.Rule.Brand.BrandName : "",
                Product = x.Rule.ProductInventory != null ? x.Rule.ProductInventory.ManufacturerPartNo : "",
                Product_Count = x.Rule.ProductInventories.Count,
                Test_Rule = x.Rule.IsTest ? "Yes" : "No",
                Active = x.Rule.IsActive ? "Yes" : "No",
                Band_Name = x.Rule.BandName,
                Band_Start = x.Rule.BandStart,
                Band_End = x.Rule.BandEnd,
                Cost_Uplift_Is_Percent = x.Rule.UpliftIsPc ? "Yes" : "No",
                Cost_Uplift = x.Rule.CostUplift,
                Minimum_Margin = x.Rule.MinMargin * 100,
                Desired_Margin = x.Rule.DesiredMargin * 100,
                Maximum_Margin = x.Rule.MaxMargin * 100,
                Beat_Rate = x.Rule.BeatRate * 100,
                Nudge_Amount = x.Rule.Nudge * 100,
                Fixed_Price_Override = x.Rule.FixedPriceOverride,
                Adjustment_1 = x.Rule.AltPriceAdj1,
                Adjustment_2 = x.Rule.AltPriceAdj2,
                Adjustment_3 = x.Rule.AltPriceAdj3,
                Adjustment_4 = x.Rule.AltPriceAdj4,
                Adjustment_5 = x.Rule.AltPriceAdj5,
                Adjustment_6 = x.Rule.AltPriceAdj6,
                Adjustment_7 = x.Rule.AltPriceAdj7,
                Adjustment_8 = x.Rule.AltPriceAdj8,
                Adjustment_9 = x.Rule.AltPriceAdj9,
                Adjustment_10 = x.Rule.AltPriceAdj10,
                Related_Discount = x.Rule.CompatDiscount,
                Rounding_Group = x.Rule.Lookup2.LookupName
            }).ToList();

            return ExportUtilities.ExportToSpreadsheet(data, true);
        }

        public void Dispose()
        {
            _ctx.Dispose();
        }
    }
}
