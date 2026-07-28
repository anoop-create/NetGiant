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

        public IQueryable<Telerik> PriceRulesList { get; set; }
        public List<PriceRule> BandList { get; set; }
        public PriceRule PriceRuleEntry { get; set; }
        public List<SelectListItem> Categories { get; set; }
        public List<SelectListItem> Brands { get; set; }
        public List<SelectListItem> RuleTypes { get; set; }
        public List<SelectListItem> MethodTypes { get; set; }
        public List<SelectListItem> RoundingGroups { get; set; }
        public List<CustomField> CustomFieldList { get; set; }
        public List<CustomField> AdjFieldList { get; set; }
        public List<SharedViewModel.SelectListItemExtended> ReportConfigList { get; set; }
        public List<SelectListItem> ReportSecurityList { get; set; }
        public ReportConfiguration ReportConfiguration { get; set; }
        public bool UserCanModify { get; set; }
        private int _channelId;
        public Channel Channel { get; set; }
        public TenantSetting Tenant { get; set; }
        public int? RequestedReportTenantId { get; set; }

        private DP001Entities _ctx;

        public PriceRuleViewModel InitializeReport(int? reportConfigId, string userId, int? tenantFk)
        {
            ReportSecurityList = SharedViewModel.GetLookupList("ReportSecurity");
            ReportConfigList = SharedViewModel.GetReportConfigSelectList(tenantFk, userId, "Price Rules");

            if (reportConfigId != null)
            {
                var config = CrudReportConfiguration.Read(x => x.ReportConfigurationId == reportConfigId).FirstOrDefault();

                if (config != null)
                {
                    RequestedReportTenantId = config.TenantFk;

                    if (config.Lookup.LookupName == "Private")
                    {
                        if (config.UserId != userId)
                            return this;
                    }

                    if (config.Lookup.LookupName == "Shared")
                    {
                        if (config.TenantFk != tenantFk)
                            return this;
                    }

                    ReportConfiguration = config;

                    if (ReportConfiguration.UserId == userId)
                        UserCanModify = true;
                }
            }

            return this;
        }

        public PriceRuleViewModel GetRules()
        {
            var crud = new CrudPriceRule();
            PriceRulesList = crud.ReadPriceRulesQuery(x => x.ChannelFK == _channelId, _ctx).AsTelerikViewModel();

            return this;
        }

        public PriceRuleViewModel GetRule(int id)
        {
            var crud = new CrudPriceRule();
            PriceRuleEntry = crud.Read(id);

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
                MethodTypes = SharedViewModel.GetMethodList(Channel.TenantFK);
                RoundingGroups = SharedViewModel.GetRoundingList(Channel.TenantFK);
                if (RoundingGroups.Count == 1)
                {
                    RoundingGroups.First().Selected = true;
                }
            }

            return this;
        }

        public PriceRuleViewModel New()
        {
            PriceRuleEntry = new PriceRule();
            Categories = SharedViewModel.GetCategoryList(_channelId);
            Brands = SharedViewModel.GetBrandList(_channelId);
            RuleTypes = SharedViewModel.GetLookupList("PriceRuleType");
            MethodTypes = SharedViewModel.GetMethodList(Channel.TenantFK);
            RoundingGroups = SharedViewModel.GetRoundingList(Channel.TenantFK);
            if (RoundingGroups.Count == 1)
            {
                RoundingGroups.First().Selected = true;
            }

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

                    bool firstBand = true;
                    foreach (PriceRule pr in bandedRules)
                    {
                        if (priceRule.PriceRuleID == pr.PriceRuleID)
                        {
                            //Check first band starts at 0 
                            if (firstBand && priceRule.BandStart > 0)
                            {
                                saveReturn.Message = "The first banded rule in a banded ruleset must start at 0";
                                isValid = false;
                            }
                            continue;
                        }
                        if (firstBand)
                        {
                            firstBand = false;
                        }

                        //Check for overlaps 
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

        public void Dispose()
        {
            _ctx.Dispose();
        }

        public class Telerik
        {
            public int PriceRuleId { get; set; }
            public string RuleName { get; set; }
            public string RuleType { get; set; }
            public bool Banding { get; set; }
            public string Method { get; set; }
            public string CategoryName { get; set; }
            public string BrandName { get; set; }
            public string ProductName { get; set; }
            public string ProductPartNo { get; set; }
            public long? ProductId { get; set; }
            public int ProductCount { get; set; }
            public bool IsTest { get; set; }
            public bool Active { get; set; }
        }
    }

    public static class PriceRuleExtensions
    {
        public static IQueryable<PriceRuleViewModel.Telerik> AsTelerikViewModel(this IQueryable<PriceRuleInfo> priceRuleQuery)
        {
            return priceRuleQuery.Select(o => new PriceRuleViewModel.Telerik
            {
                PriceRuleId = o.Rule.PriceRuleID,
                RuleName = o.Rule.RuleName,
                RuleType = o.Rule.Lookup1.LookupName,
                Banding = o.Rule.IsBanding,
                Method = o.Rule.Lookup.LookupName,
                CategoryName = o.Rule.ProductCategory.CategoryName,
                BrandName = o.Rule.Brand.BrandName,
                ProductName = o.Rule.ProductInventory.Description,
                ProductPartNo = o.Rule.ProductInventory.ManufacturerPartNo,
                ProductId = o.Rule.ProductInventory.ProductInventoryID,
                ProductCount = o.ProductCount,
                IsTest = o.Rule.IsTest,
                Active = o.Rule.IsActive
            });
        }
    }
}
