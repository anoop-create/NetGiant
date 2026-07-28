using DP001DataAccess.Entities;
using DP001DataAccess.Utilities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace DP001BusinessLogic.Pricing
{
    public class CopyPriceRule
    {
        private int _copyFromChannel;
        private List<int> _copyToChannels;
        private List<PriceRule> _originalRules;
        private List<PriceRule> _newPriceRules;

        public CopyPriceRule(Dictionary<string, string> parms)
        {
            _copyFromChannel = Convert.ToInt32(parms["channelid"]);
            _copyToChannels = parms["output"].Split('|').Select(int.Parse).ToList();
            _originalRules = new List<PriceRule>();
            _newPriceRules = new List<PriceRule>();
        }

        public void Copy()
        {
            List<PriceRule> priceRules = GetPriceRulesToCopy();

            if (priceRules.Count < 1)
                return;

            ResetPriceRuleCopyValue(priceRules);

            for (int i = 0; i < priceRules.Count; i++)
            {
                if (priceRules[i].IsBanding)
                {
                    bool exists = _originalRules.Where(x => x.RuleName.Contains(priceRules[i].RuleName)).Count() > 0;

                    if (exists)
                        continue;
                }

                GetOriginalPriceRules(priceRules[i]);
            }

            for (int i = 0; i < priceRules.Count; i++)
            {
                GetNewPriceRules(priceRules[i]);
            }

            if (_originalRules.Count > 0)
            {
                DeleteOriginalRules();
            }

            CreateNewRules();
        }

        private List<PriceRule> GetPriceRulesToCopy()
        {
            using (var db = new DP001Entities())
            {
                return db.PriceRules
                             .Where(x => db.PriceRules
                                           .Where(p => p.ChannelFK == _copyFromChannel && p.CustomRuleField7 == 1)
                                           .Select(p => p.RuleName)
                                           .Distinct().Contains(x.RuleName) && x.ChannelFK == _copyFromChannel)
                             .ToList();
            }
        }

        private void ResetPriceRuleCopyValue(List<PriceRule> priceRules)
        {
            using (var db = new DP001Entities())
            {
                for (int i = 0; i < priceRules.Count; i++)
                {
                    priceRules[i].CustomRuleField7 = 0;
                    db.Entry(priceRules[i]).State = EntityState.Modified;
                }

                db.SaveChanges();
            }
        }

        private void GetOriginalPriceRules(PriceRule rule)
        {
            using (var db = new DP001Entities())
            {
                for (int i = 0; i < _copyToChannels.Count; i++)
                {
                    int channel = _copyToChannels[i];

                    var originalRule = db.PriceRules
                                         .Where(x =>
                                                   x.RuleName == rule.RuleName &&
                                                   x.ChannelFK == channel
                                               ).ToList();

                    if (originalRule.Count > 0)
                    {
                        for (int j = 0; j < originalRule.Count; j++)
                        {
                            _originalRules.Add(originalRule[j]);
                        }
                    }
                }
            }
        }

        private void GetNewPriceRules(PriceRule priceRule)
        {
            using (var db = new DP001Entities())
            {
                for (int i= 0; i < _copyToChannels.Count; i++)
                {
                    int? brandFK = null;
                    long? productCategoryFK = null;

                    if (priceRule.BrandFK != null)
                    {
                        brandFK = GetBrandFK(db, _copyToChannels[i], (int)priceRule.BrandFK);

                        if (brandFK == null)
                        {
                            RemovePriceRuleFromCopy(db, _copyToChannels[i], priceRule, "brandFK: " + priceRule.BrandFK);
                            continue;
                        }
                    }

                    if (priceRule.ProductCategoryFK != null)
                    {
                        productCategoryFK = GetProductCategoryFK(db, _copyToChannels[i], priceRule);

                        if (productCategoryFK == null)
                        {
                            RemovePriceRuleFromCopy(db, _copyToChannels[i], priceRule, "productCategoryFK: " + priceRule.ProductCategoryFK);
                            continue;
                        }
                    }

                    PriceRule newPriceRule = PopulatePriceRuleProperties(priceRule, _copyToChannels[i], brandFK, productCategoryFK);
                    _newPriceRules.Add(newPriceRule);
                }
            }
        }

        private void RemovePriceRuleFromCopy(DP001Entities db, int channel, PriceRule priceRule, string reason)
        {
            PriceRule original = _originalRules
                                    .Where(w => w.ChannelFK == channel && w.RuleName == priceRule.RuleName && w.BandName == priceRule.BandName)
                                    .FirstOrDefault();

            if (original != null)
            {
                _originalRules.Remove(original);
            }

            if (priceRule.CustomRuleField7 == 0)
            {
                priceRule.CustomRuleField7 = 1;
                db.Entry(priceRule).State = EntityState.Modified;
                db.SaveChanges();
            }

            var crudChannel = new CrudChannel();
            var channelFrom = crudChannel.Read(_copyFromChannel);
            var channelTo = crudChannel.Read(channel);
            var msg = "Could not copy ";
            msg += priceRule.IsBanding ? "Banding rule " : "rule ";
            msg += priceRule.RuleName + " to " + channelTo.ChannelName;
            CommonDataFunctions.CreateLogEntry(channelFrom, msg, "Notification");
            CommonDataFunctions.CreateLogEntry(channelFrom, msg + ", no matching " + reason, "Error");

        }

        private int? GetBrandFK(DP001Entities db, int channel, int brandFK)
        {
            var query = db.Brands
                        .Where(x => x.BrandName == db.Brands
                                                     .Where(y => y.BrandID == brandFK)
                                                     .Select(y => y.BrandName)
                                                     .FirstOrDefault()
                                    && x.ChannelFK == channel)
                        .Select(x => x.BrandID)
                        .ToList();

            if (query.Count == 1)
            {
                return query[0];
            }
            else
            {
                return null;
            }
        }

        private long? GetProductCategoryFK(DP001Entities db, int channel, PriceRule rule)
        {
            ProductCategory category = db.ProductCategories.Where(x => x.ProductCategoryID == rule.ProductCategoryFK).FirstOrDefault();

            string ngName = category.CategoryName.Replace("Toner", "Toner Cartridges")
                                                 .Replace("Ink", "Inkjet Cartridges");

            var query = db.ProductCategories
                         .Where(x => (x.CategoryName == category.CategoryName || x.CategoryName == ngName) && x.ChannelFK == channel)
                         .Select(s => s.ProductCategoryID)
                         .ToList();

            if (query.Count == 1)
            {
                return query[0];
            } 
            else
            {
                return null;
            }
        }

        private PriceRule PopulatePriceRuleProperties(PriceRule priceRule, int copyToChannel, int? brandFK, long? productCategoryFK)
        {
            return new PriceRule
            {
                ChannelFK = copyToChannel,
                RuleName = priceRule.RuleName,
                RuleTypeFK = priceRule.RuleTypeFK,
                BrandFK = brandFK,
                ProductInventoryFK = priceRule.ProductInventoryFK,
                ProductCategoryFK = productCategoryFK,
                MethodFK = priceRule.MethodFK,
                IsBanding = priceRule.IsBanding,
                IsActive = priceRule.IsActive,
                IsTest = priceRule.IsTest,
                BandName = priceRule.BandName,
                BandStart = priceRule.BandStart,
                BandEnd = priceRule.BandEnd,
                UpliftIsPc = priceRule.UpliftIsPc,
                CostUplift = priceRule.CostUplift,
                MarginsArePc = priceRule.MarginsArePc,
                DesiredMargin = priceRule.DesiredMargin,
                MinMargin = priceRule.MinMargin,
                MaxMargin = priceRule.MaxMargin,
                BeatRate = priceRule.BeatRate,
                Nudge = priceRule.Nudge,
                FixedPriceOverride = priceRule.FixedPriceOverride,
                AltPriceAdj1 = priceRule.AltPriceAdj1,
                AltPriceAdj2 = priceRule.AltPriceAdj2,
                AltPriceAdj3 = priceRule.AltPriceAdj3,
                AltPriceAdj4 = priceRule.AltPriceAdj4,
                AltPriceAdj5 = priceRule.AltPriceAdj5,
                AltPriceAdj6 = priceRule.AltPriceAdj6,
                AltPriceAdj7 = priceRule.AltPriceAdj7,
                AltPriceAdj8 = priceRule.AltPriceAdj8,
                AltPriceAdj9 = priceRule.AltPriceAdj9,
                AltPriceAdj10 = priceRule.AltPriceAdj10,
                CompatDiscount = priceRule.CompatDiscount,
                ProductCount = priceRule.ProductCount,
                AboveCounter = priceRule.AboveCounter,
                BelowCounter = priceRule.BelowCounter,
                MaxCounter = priceRule.MaxCounter,
                MinCounter = priceRule.MinCounter,
                RoundingGroupFK = priceRule.RoundingGroupFK,
                AdjMinMargin1 = priceRule.AdjMinMargin1,
                AdjMaxMargin1 = priceRule.AdjMaxMargin1,
                AdjMinMargin2 = priceRule.AdjMinMargin2,
                AdjMaxMargin2 = priceRule.AdjMaxMargin2,
                AdjMinMargin3 = priceRule.AdjMinMargin3,
                AdjMaxMargin3 = priceRule.AdjMaxMargin3,
                AdjMinMargin4 = priceRule.AdjMinMargin4,
                AdjMaxMargin4 = priceRule.AdjMaxMargin4,
                AdjMinMargin5 = priceRule.AdjMinMargin5,
                AdjMaxMargin5 = priceRule.AdjMaxMargin5,
                AdjMinMargin6 = priceRule.AdjMinMargin6,
                AdjMaxMargin6 = priceRule.AdjMaxMargin6,
                AdjMinMargin7 = priceRule.AdjMinMargin7,
                AdjMaxMargin7 = priceRule.AdjMaxMargin7,
                AdjMinMargin8 = priceRule.AdjMinMargin8,
                AdjMaxMargin8 = priceRule.AdjMaxMargin8,
                AdjMinMargin9 = priceRule.AdjMinMargin9,
                AdjMaxMargin9 = priceRule.AdjMaxMargin9,
                AdjMinMargin10 = priceRule.AdjMinMargin10,
                AdjMaxMargin10 = priceRule.AdjMaxMargin10,
                CustomRuleField1 = priceRule.CustomRuleField1,
                CustomRuleField2 = priceRule.CustomRuleField2,
                CustomRuleField3 = priceRule.CustomRuleField3,
                CustomRuleField4 = priceRule.CustomRuleField4,
                CustomRuleField5 = priceRule.CustomRuleField5,
                CustomRuleField6 = priceRule.CustomRuleField6,
                CustomRuleField7 = priceRule.CustomRuleField7,
                CustomRuleField8 = priceRule.CustomRuleField8,
                CustomRuleField9 = priceRule.CustomRuleField9,
                CustomRuleField10 = priceRule.CustomRuleField10,
            };
        }

        private void DeleteOriginalRules()
        {
            using (var db = new DP001Entities())
            {
                for (int i = 0; i < _originalRules.Count; i++)
                {
                    PriceRule pr = db.PriceRules.Find(_originalRules[i].PriceRuleID);
                    db.PriceRules.Remove(pr);

                    TenantAudit audit = CreateAudit(_originalRules[i].ChannelFK, 'D', _originalRules[i].RuleName, _originalRules[i], null);
                    db.TenantAudits.Add(audit);
                }
                db.SaveChanges();
            }
        }

        private void CreateNewRules()
        {
            using (var db = new DP001Entities())
            {
                for (int i = 0; i < _newPriceRules.Count; i++)
                {
                    db.PriceRules.Add(_newPriceRules[i]);

                    TenantAudit audit = CreateAudit(_newPriceRules[i].ChannelFK, 'A', _newPriceRules[i].RuleName, null, _newPriceRules[i]);
                    db.TenantAudits.Add(audit);
                }

                db.SaveChanges();
            }
        }

        private TenantAudit CreateAudit(int channel, char type, string ruleName, PriceRule oldRule, PriceRule newRule)
        {
            var obj = new CrudPriceRule();

            return new TenantAudit
            {
                ChannelFK = channel,
                Timestamp = DateTime.Now,
                UserName = "Batch - Copy Price Rule",
                Type = Convert.ToString(type),
                ObjectName = "Price Rule - " + ruleName,
                OldValues = oldRule != null ? obj.BuildAuditString(oldRule) : "",
                NewValues = newRule != null ? obj.BuildAuditString(newRule) : "",
            };
        }
    }
}