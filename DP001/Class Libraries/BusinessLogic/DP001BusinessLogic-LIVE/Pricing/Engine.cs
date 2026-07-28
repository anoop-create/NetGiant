using DP001DataAccess.Entities;
using System.Data.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.SqlClient;
using DP001BusinessLogic.Shared;
using System.Data;
using DP001DataAccess.Utilities;
using System.IO;
using DP001BusinessLogic.CustomRoutines;

namespace DP001BusinessLogic.Pricing
{
    public class Engine
    {
        public Engine(Dictionary<string, string> parms)
        {
            _suppliedParams = parms;
            InitializeTenant();
            _finalRulesList = new List<PriceRuleDetail>();
            _calculationOutcome = GetCalculationOutcomes();
        }

        private readonly Dictionary<string, string> _suppliedParams;
        private Channel _channel;
        private Tenant _tenant;
        private static List<PriceRuleDetail> _finalRulesList;
        private static List<Lookup> _calculationOutcome;
        private static bool _testMode;
        private static List<CustomField> _customFields;

        private enum CalculationMethod
        {
            CostBase,
            RelatedProductBase,
            FixedPrice
        }

        public void Calculate()
        {
            CommonDataFunctions.CreateLogEntry(_channel, "START CalculatePrices", "Information");

            ClearDownPriceStaging();
            CleanupStagingTables();

            try
            {
                var costBaseRules = GetPriceRules(CalculationMethod.CostBase);
                if (costBaseRules.Count > 0)
                {
                    ProcessRules(costBaseRules);
                    UpdatePrices(costBaseRules);
                }

                var relatedProductBaseRules = GetPriceRules(CalculationMethod.RelatedProductBase);
                if (relatedProductBaseRules.Count > 0)
                {
                    ProcessRules(relatedProductBaseRules);
                    UpdatePrices(relatedProductBaseRules);
                }

                var fixedPriceRules = GetPriceRules(CalculationMethod.FixedPrice);
                if (fixedPriceRules.Count > 0)
                {
                    ProcessRules(fixedPriceRules);
                    UpdatePrices(fixedPriceRules);
                }

                foreach (var method in GetCustomMethods(_channel))
                {
                    var customPriceRules = GetCustomPriceRules(method);
                    if (customPriceRules.Count <= 0) continue;
                    ProcessRules(customPriceRules);
                    UpdatePrices(customPriceRules);
                }

                var newFileStream = _tenant.CreateCsv(_finalRulesList, _channel);
                _tenant.OutputPrices(_channel, newFileStream);
                OutputPricesToEmail(_channel, newFileStream);

                UpdateProductCounts(_channel);
            }
            catch (Exception ex)
            {
                CommonDataFunctions.CreateLogEntry(_channel, "ERROR: " + ex.Message, "Error");
            }

            CommonDataFunctions.CreateLogEntry(_channel, "END CalculatePrices", "Information");
        }

        private void ProcessRules(IEnumerable<PriceRuleDetail> rules)
        {
            CommonDataFunctions.CreateLogEntry(_channel, "START ProcessRules", "Information");

            var priceRuleList = rules as IList<PriceRuleDetail> ?? rules.ToList();

            foreach (var priceRule in priceRuleList.OrderBy(x => x.Product.VariantOf))
            {
                if (!ProductHasCostPrice(priceRule)) continue;

                var competitors = GetCompetitorPrices(priceRule.Rule);
                var totalCompetitors = competitors.Count;
                var numberToBeat = GetNumberOfCompetitorsToBeat(totalCompetitors, priceRule.Rule.BeatRate);
                var costUpliftPrice = GetCostUplift(priceRule.Rule);

                priceRule.Components = new FinalPriceComponents
                {
                    Nudge = GetNudgePrice(numberToBeat, competitors, priceRule.Rule),
                    MinMargin = CalculateMargin(costUpliftPrice, priceRule.Rule.MinMargin),
                    MaxMargin = CalculateMargin(costUpliftPrice, priceRule.Rule.MaxMargin),
                    DesiredMargin = CalculateMargin(costUpliftPrice, priceRule.Rule.DesiredMargin),
                    CompetitorCount = totalCompetitors,
                    CostUpliftPrice = costUpliftPrice
                };
                priceRule.Components.CheapestCompetitorPrice = SetCheapestCompPrice(priceRule.Components, competitors);
                priceRule.Components.BeatenCompetitorPrice = GetBeatenCompetitorPrice(numberToBeat, competitors);
                priceRule.Components.BeatRateNumber = numberToBeat;
                priceRule.Components.PreviousPrice = priceRule.Product.Price;

                SetFinalPrice(priceRule);
                SetAltPrices(priceRule);
                DoRounding(priceRule);

                if (!ProductIsVariant(priceRule)) continue;
                var variantParentRule = priceRuleList.FirstOrDefault(x => x.Product.ClientProductID == priceRule.Rule.VariantOf);

                if (variantParentRule != null)
                {
                    DoVariantPrice(priceRule, variantParentRule);
                }
            }

            CommonDataFunctions.CreateLogEntry(_channel, "END ProcessRules", "Information");
        }

        private static bool ProductHasCostPrice(PriceRuleDetail priceRule)
        {
            if (priceRule.Rule.CostPrice != 0) return true;

            priceRule.Product.AltPrice1 = 0;
            priceRule.Product.AltPrice2 = 0;
            priceRule.Product.AltPrice3 = 0;
            priceRule.Product.AltPrice4 = 0;
            priceRule.Product.AltPrice5 = 0;
            priceRule.Product.AltPrice6 = 0;
            priceRule.Product.AltPrice7 = 0;
            priceRule.Product.AltPrice8 = 0;
            priceRule.Product.AltPrice9 = 0;
            priceRule.Product.AltPrice10 = 0;
            priceRule.Product.CheapestCostPrice = 0;
            priceRule.Product.CheapestCompetitorPrice = 0;
            priceRule.Product.GrossMarginPercent = 0;
            priceRule.Product.GrossMarginValue = 0;
            priceRule.Product.CompetitorDifference = 0;
            priceRule.Product.MaximumPrice = 0;
            priceRule.Product.MinimumPrice = 0;
            priceRule.Components = new FinalPriceComponents
            {
                CalculationOutcome = _calculationOutcome.Find(x => x.LookupName == "No Cost Price").LookupID
            };

            return false;
        }

        private static bool ProductIsVariant(PriceRuleDetail priceRule)
        {
            var returnValue = !string.IsNullOrEmpty(priceRule.Product.VariantOf);

            return returnValue;
        }

        private List<PriceRuleDetail> GetPriceRules(CalculationMethod method)
        {
            string methodName;

            switch (method)
            {
                case CalculationMethod.CostBase:
                    methodName = "Cost Base";
                    break;
                case CalculationMethod.RelatedProductBase:
                    methodName = "Related Product Base";
                    break;
                case CalculationMethod.FixedPrice:
                    methodName = "Fixed Price";
                    break;
                default:
                    methodName = "Cost Base";
                    break;
            }

            CommonDataFunctions.CreateLogEntry(_channel, "START GetPriceRules - " + methodName, "Information");

            using (var db = new DP001Entities())
            {
                var products = db.ProductInventories.Where(x => x.ChannelFK == _channel.ChannelID).Include("Brand").Include("ProductCategory").Include("Channel");
                var results = (from x in db.GetProductPriceRules(_channel.ChannelID, methodName, _testMode)
                               join y in products on x.ProductInventoryID equals y.ProductInventoryID
                               select new PriceRuleDetail()
                               {
                                   Rule = x,
                                   Product = y
                               }).ToList();

                CommonDataFunctions.CreateLogEntry(_channel, "END GetPriceRules - " + methodName, "Information");

                return results;
            }
        }

        private List<PriceRuleDetail> GetCustomPriceRules(string method)
        {
            var methodName = method;
            var operatorField = _tenant.CustomMethodOperatorField?.Invoke(method, _channel);

            CommonDataFunctions.CreateLogEntry(_channel, "START GetPriceRules - " + methodName, "Information");

            using (var db = new DP001Entities())
            {
                var products = db.ProductInventories.Where(x => x.ChannelFK == _channel.ChannelID).Include("Brand").Include("ProductCategory").Include("Channel");
                var results = (from x in db.GetCustomProductPriceRules(_channel.ChannelID, _testMode, methodName, operatorField)
                               join y in products on x.ProductInventoryID equals y.ProductInventoryID
                               select new PriceRuleDetail
                               {
                                   Rule = x,
                                   Product = y
                               }).ToList();

                CommonDataFunctions.CreateLogEntry(_channel, "END GetPriceRules - " + methodName, "Information");

                return results;
            }
        }

        private static List<decimal> GetCompetitorPrices(GetProductPriceRules_Result rule)
        {
            var compList = new List<decimal>();

            if (string.IsNullOrEmpty(rule.Competitors)) return compList;

            var compsArray = rule.Competitors.Split(',').ToList();
            compList.AddRange(from item in compsArray where item.Length > 0 select item.Split('#') into itemArray select Convert.ToDecimal(itemArray[1]));

            return compList.OrderByDescending(x => x).ToList();
        }

        private static int GetNumberOfCompetitorsToBeat(int totalCompetitors, decimal competitorsToBeatPercent)
        {
            int beat;

            switch (totalCompetitors)
            {
                case 0:
                    beat = 0;
                    break;
                case 1:
                    beat = 1;
                    break;
                default:
                    beat = Convert.ToInt32(Math.Floor(totalCompetitors * competitorsToBeatPercent));
                    break;
            }

            return beat;
        }

        private static decimal GetCostUplift(GetProductPriceRules_Result rule)
        {
            decimal? costUplift;

            if (rule.CostUpliftIsPercent)
            {
                costUplift = rule.CostPrice + (rule.CostPrice * (rule.CostUplift / 100));
            }
            else
            {
                costUplift = rule.CostPrice + rule.CostUplift;
            }

            return (decimal)costUplift;
        }

        private static decimal GetNudgePrice(int competitorsToBeat, IEnumerable<decimal> competitors, GetProductPriceRules_Result rule)
        {
            decimal nudgePrice = 0;

            if (competitorsToBeat <= 0) return nudgePrice;

            var competitorsLowestPrice = competitors.Take(competitorsToBeat).Select(x => x).Last();
            nudgePrice = competitorsLowestPrice - (competitorsLowestPrice * rule.Nudge);

            return nudgePrice;
        }

        private static decimal GetBeatenCompetitorPrice(int competitorsToBeat, IEnumerable<decimal> competitors)
        {
            decimal beatenPrice = 0;

            if (competitorsToBeat <= 0) return beatenPrice;

            beatenPrice = competitors.Take(competitorsToBeat).Select(x => x).Last();

            return beatenPrice;
        }

        internal static decimal CalculateMargin(decimal price, decimal margin)
        {
            var calculatedMargin = price / (1 - margin);

            return calculatedMargin;
        }

        private static decimal SetCheapestCompPrice(FinalPriceComponents components, List<decimal> compsList)
        {
            components.CheapestCompetitorPrice = compsList.Any() ? compsList.Last() : 0;

            return components.CheapestCompetitorPrice;
        }

        private void SetFinalPrice(PriceRuleDetail priceRule)
        {
            switch (priceRule.Rule.MethodName)
            {
                case "Cost Base":

                    DoCostBase(priceRule.Components);
                    break;

                case "Related Product Base":

                    DoRelatedProductBase(priceRule);
                    break;

                case "Fixed Price":

                    DoFixedPrice(priceRule.Components, priceRule);
                    break;

                default:

                    DoCustomMethod(priceRule);
                    break;
            }

            priceRule.Product.Price = priceRule.Components.FinalPrice;
        }

        private static void DoVariantPrice(PriceRuleDetail variantRule, PriceRuleDetail variantRuleParent)
        {
            variantRule.Product.Price = variantRuleParent.Product.Price;
            variantRule.Product.AltPrice1 = variantRuleParent.Product.AltPrice1;
            variantRule.Product.AltPrice2 = variantRuleParent.Product.AltPrice2;
            variantRule.Product.AltPrice3 = variantRuleParent.Product.AltPrice3;
            variantRule.Product.AltPrice4 = variantRuleParent.Product.AltPrice4;
            variantRule.Product.AltPrice5 = variantRuleParent.Product.AltPrice5;
            variantRule.Product.AltPrice6 = variantRuleParent.Product.AltPrice6;
            variantRule.Product.AltPrice7 = variantRuleParent.Product.AltPrice7;
            variantRule.Product.AltPrice8 = variantRuleParent.Product.AltPrice8;
            variantRule.Product.AltPrice9 = variantRuleParent.Product.AltPrice9;
            variantRule.Product.AltPrice10 = variantRuleParent.Product.AltPrice10;

            if (variantRuleParent.Components != null)
            {
                variantRule.Components.CalculationOutcome = variantRuleParent.Components.CalculationOutcome;
            }
        }

        private void DoCustomMethod(PriceRuleDetail priceRule)
        {
            RunCustomRoutine(TargetUser.NetGiant, "Assembly",
                new
                {
                    priceRuleDetail = priceRule,
                    calculationOutcomes = _calculationOutcome
                });
        }

        private static void DoFixedPrice(FinalPriceComponents components, PriceRuleDetail priceRule)
        {
            components.FinalPrice = priceRule.Rule.FixedPriceOverride;
            components.CalculationOutcome = _calculationOutcome.Find(x => x.LookupName == "Fixed Price").LookupID;
        }

        private void DoRelatedProductBase(PriceRuleDetail priceRule)
        {
            priceRule.Components.FinalPrice = CalculatePercentDiscount(priceRule.Rule.BasePrice, priceRule.Rule.CompatDiscount ?? 0);
            priceRule.Components.CalculationOutcome = _calculationOutcome.Find(x => x.LookupName == "Relational Discount").LookupID;

            RunCustomRoutine(TargetUser.NetGiant, "RelatedDiscount",
                new
                {
                    priceRuleDetail = priceRule,
                    calculationOutcomes = _calculationOutcome
                });
        }

        private static void DoCostBase(FinalPriceComponents components)
        {
            if (components.Nudge > 0)
            {
                if (components.Nudge > components.DesiredMargin)
                {
                    if (components.Nudge > components.MaxMargin)
                    {
                        components.FinalPrice = Math.Round(components.MaxMargin, 2);
                        components.CalculationOutcome = _calculationOutcome.Find(x => x.LookupName == "Maximum").LookupID;
                    }
                    else
                    {
                        components.FinalPrice = Math.Round(components.Nudge, 2);
                        components.PricingRule = "Nudge Up";
                        components.CalculationOutcome = _calculationOutcome.Find(x => x.LookupName == "Nudge Up").LookupID;
                    }
                }
                else if (components.Nudge < components.DesiredMargin)
                {
                    if (components.Nudge < components.MinMargin)
                    {
                        components.FinalPrice = Math.Round(components.MinMargin, 2);
                        components.CalculationOutcome = _calculationOutcome.Find(x => x.LookupName == "Minimum").LookupID;
                    }
                    else
                    {
                        components.FinalPrice = Math.Round(components.Nudge, 2);
                        components.CalculationOutcome = _calculationOutcome.Find(x => x.LookupName == "Nudge Down").LookupID;
                    }
                }
                else
                {
                    components.FinalPrice = Math.Round(components.DesiredMargin, 2);
                    components.CalculationOutcome = _calculationOutcome.Find(x => x.LookupName == "Desired").LookupID;
                }
            }
            else
            {
                components.FinalPrice = Math.Round(components.DesiredMargin, 2);
                components.CalculationOutcome = _calculationOutcome.Find(x => x.LookupName == "Desired").LookupID;
            }
        }

        private void SetAltPrices(PriceRuleDetail pr)
        {
            pr.Product.AltPrice1 = CalculateAltPrice(pr.Product, pr.Rule.AltPriceAdj1, pr.Rule.AdjMinMargin1, pr.Rule.AdjMaxMargin1, pr.Components.CostUpliftPrice);
            pr.Product.AltPrice2 = CalculateAltPrice(pr.Product, pr.Rule.AltPriceAdj2, pr.Rule.AdjMinMargin2, pr.Rule.AdjMaxMargin2, pr.Components.CostUpliftPrice);
            pr.Product.AltPrice3 = CalculateAltPrice(pr.Product, pr.Rule.AltPriceAdj3, pr.Rule.AdjMinMargin3, pr.Rule.AdjMaxMargin3, pr.Components.CostUpliftPrice);
            pr.Product.AltPrice4 = CalculateAltPrice(pr.Product, pr.Rule.AltPriceAdj4, pr.Rule.AdjMinMargin4, pr.Rule.AdjMaxMargin4, pr.Components.CostUpliftPrice);
            pr.Product.AltPrice5 = CalculateAltPrice(pr.Product, pr.Rule.AltPriceAdj5, pr.Rule.AdjMinMargin5, pr.Rule.AdjMaxMargin5, pr.Components.CostUpliftPrice);
            pr.Product.AltPrice6 = CalculateAltPrice(pr.Product, pr.Rule.AltPriceAdj6, pr.Rule.AdjMinMargin6, pr.Rule.AdjMaxMargin6, pr.Components.CostUpliftPrice);
            pr.Product.AltPrice7 = CalculateAltPrice(pr.Product, pr.Rule.AltPriceAdj7, pr.Rule.AdjMinMargin7, pr.Rule.AdjMaxMargin7, pr.Components.CostUpliftPrice);
            pr.Product.AltPrice8 = CalculateAltPrice(pr.Product, pr.Rule.AltPriceAdj8, pr.Rule.AdjMinMargin8, pr.Rule.AdjMaxMargin8, pr.Components.CostUpliftPrice);
            pr.Product.AltPrice9 = CalculateAltPrice(pr.Product, pr.Rule.AltPriceAdj9, pr.Rule.AdjMinMargin9, pr.Rule.AdjMaxMargin9, pr.Components.CostUpliftPrice);
            pr.Product.AltPrice10 = CalculateAltPrice(pr.Product, pr.Rule.AltPriceAdj10, pr.Rule.AdjMinMargin10, pr.Rule.AdjMaxMargin10, pr.Components.CostUpliftPrice);

            RunCustomRoutine(TargetUser.NetGiant, "BreakMargins",
                new
                {
                    priceRuleDetail = pr,
                    costUpliftPrice = pr.Components.CostUpliftPrice
                });
        }

        private static decimal CalculateAltPrice(ProductInventory product,
            decimal altPriceAdj,
            decimal altPriceMinMar,
            decimal altPriceMaxMar,
            decimal costupliftPrice)
        {
            decimal returnPrice = 0;

            if (altPriceAdj == 0) return returnPrice;

            var minAdjMargin = CalculateMargin(costupliftPrice, altPriceMinMar);
            var maxAdjMargin = CalculateMargin(costupliftPrice, altPriceMaxMar);
            var desAltPrice = returnPrice = CalculatePercentDiscount(product.Price, altPriceAdj);

            if (desAltPrice < minAdjMargin)
            {
                returnPrice = minAdjMargin;
            }

            if (desAltPrice > maxAdjMargin)
                returnPrice = maxAdjMargin;

            return Math.Round(returnPrice, 2);
        }

        internal static decimal CalculatePercentDiscount(decimal price, decimal altPriceAdj)
        {
            decimal discountedPrice = 0;

            if (price > 0)
            {
                discountedPrice = Math.Round(price + (price * altPriceAdj), 2);
            }

            return discountedPrice;
        }

        private void UpdatePrices(List<PriceRuleDetail> rules)
        {
            CommonDataFunctions.CreateLogEntry(_channel, "START UpdatePrices", "Information");

            MergeToFinalList(rules);

            if (!_testMode)
            {
                SQL.SQLBulkInsert(SetupProductDataTable(rules), "DP001");

                var sqlParms = new List<SqlParameter>();
                var sqlParm1 = new SqlParameter("@ChannelFK", SqlDbType.Int) { Value = _channel.ChannelID };
                sqlParms.Add(sqlParm1);
                var sqlParm2 = new SqlParameter("@deleteUnmatched", SqlDbType.Bit) { Value = false };
                sqlParms.Add(sqlParm2);
                var isSuccess = SQL.ExecuteStoredProcedure("DP001", "CreateUpdateProductInventory", sqlParms, _channel.ChannelID);

                if (!isSuccess)
                    CommonDataFunctions.CreateLogEntry(_channel, "Unable to complete price calculations due to errors found. Please contact support.", "Notification");
            }
            else
            {
                InsertStagingPrices(rules);
            }

            CommonDataFunctions.CreateLogEntry(_channel, "END UpdatePrices", "Information");
        }

        private void ClearDownPriceStaging()
        {
            CommonDataFunctions.CreateLogEntry(_channel, "START ClearDownPriceStaging", "Information");

            using (DP001Entities db = new DP001Entities())
            {
                db.DeletePriceStagingEntries(_channel.ChannelID);
            }

            CommonDataFunctions.CreateLogEntry(_channel, "END ClearDownPriceStaging", "Information");
        }

        private static void InsertStagingPrices(IEnumerable<PriceRuleDetail> rules)
        {
            var stagingList = new List<PriceStaging>();

            foreach (var rule in rules)
            {
                stagingList.Add(new PriceStaging
                {
                    ProductInventoryFK = rule.Product.ProductInventoryID,
                    ChannelFK = rule.Product.ChannelFK,
                    Price = rule.Product.Price,
                    AltPrice1 = rule.Product.AltPrice1,
                    AltPrice2 = rule.Product.AltPrice2,
                    AltPrice3 = rule.Product.AltPrice3,
                    AltPrice4 = rule.Product.AltPrice4,
                    AltPrice5 = rule.Product.AltPrice5,
                    AltPrice6 = rule.Product.AltPrice6,
                    AltPrice7 = rule.Product.AltPrice7,
                    AltPrice8 = rule.Product.AltPrice8,
                    AltPrice9 = rule.Product.AltPrice9,
                    AltPrice10 = rule.Product.AltPrice10,
                    PriceRuleFK = rule.Product.PriceRuleFK,
                    CalculationOutcome = rule.Components.CalculationOutcome,
                    BeatRateNumber = rule.Components.BeatRateNumber,
                    StockQuantity = rule.Rule.StockQuantity,
                    CheapestCostPrice = rule.Rule.CostPrice,
                    CheapestCompetitorPrice = rule.Components.CheapestCompetitorPrice,
                    GrossMarginPercent = Math.Round((rule.Product.Price - rule.Rule.CostPrice) / rule.Product.Price * 100, 2) / 100,
                    GrossMarginValue = Math.Round(rule.Product.Price - rule.Rule.CostPrice, 2),
                    CompetitorDifference = Math.Round(Math.Abs(rule.Components.BeatenCompetitorPrice - rule.Product.Price), 2),
                    CurrentPriceDifference = Math.Round(Math.Abs(rule.Product.Price - rule.Components.PreviousPrice), 2),
                    BeatenCompetitorPrice = rule.Components.BeatenCompetitorPrice
                });
            }

            var crudPriceStaging = new CrudPriceStaging();
            crudPriceStaging.Create(stagingList);
        }

        private static void MergeToFinalList(IEnumerable<PriceRuleDetail> rules)
        {
            foreach (var rule in rules)
            {
                var orig = _finalRulesList.Find(x => x.Product.ProductInventoryID == rule.Product.ProductInventoryID);

                if (orig != null)
                {
                    _finalRulesList.Remove(orig);
                }

                _finalRulesList.Add(rule);
            }
        }

        private static DataTable SetupProductDataTable(IEnumerable<PriceRuleDetail> rules)
        {
            var dt = new DataTable("StagingProductInventory");

            dt.Columns.Add(new DataColumn("ProductInventoryFK", typeof(int)));
            dt.Columns.Add(new DataColumn("ChannelFK", typeof(int)));
            dt.Columns.Add(new DataColumn("BrandFK", typeof(int)));
            dt.Columns.Add(new DataColumn("ManufacturerPartNo", typeof(string)));
            dt.Columns.Add(new DataColumn("Description", typeof(string)));
            dt.Columns.Add(new DataColumn("ClientProductID", typeof(string)));
            dt.Columns.Add(new DataColumn("LnkdBrandFK", typeof(int)));
            dt.Columns.Add(new DataColumn("LnkdManufacturerPartNo", typeof(string)));
            dt.Columns.Add(new DataColumn("ProductCategoryFK", typeof(long)));
            dt.Columns.Add(new DataColumn("Price", typeof(decimal)));
            dt.Columns.Add(new DataColumn("AltPrice1", typeof(decimal)));
            dt.Columns.Add(new DataColumn("AltPrice2", typeof(decimal)));
            dt.Columns.Add(new DataColumn("AltPrice3", typeof(decimal)));
            dt.Columns.Add(new DataColumn("AltPrice4", typeof(decimal)));
            dt.Columns.Add(new DataColumn("AltPrice5", typeof(decimal)));
            dt.Columns.Add(new DataColumn("AltPrice6", typeof(decimal)));
            dt.Columns.Add(new DataColumn("AltPrice7", typeof(decimal)));
            dt.Columns.Add(new DataColumn("AltPrice8", typeof(decimal)));
            dt.Columns.Add(new DataColumn("AltPrice9", typeof(decimal)));
            dt.Columns.Add(new DataColumn("AltPrice10", typeof(decimal)));
            dt.Columns.Add(new DataColumn("PriceRuleFK", typeof(int)));
            dt.Columns.Add(new DataColumn("CalculationOutcome", typeof(int)));
            dt.Columns.Add(new DataColumn("BeatRateNumber", typeof(int)));
            dt.Columns.Add(new DataColumn("StockQuantity", typeof(int)));
            dt.Columns.Add(new DataColumn("CheapestCostPrice", typeof(decimal)));
            dt.Columns.Add(new DataColumn("CheapestCompetitorPrice", typeof(decimal)));
            dt.Columns.Add(new DataColumn("GrossMarginPercent", typeof(decimal)));
            dt.Columns.Add(new DataColumn("GrossMarginValue", typeof(decimal)));
            dt.Columns.Add(new DataColumn("CompetitorDifference", typeof(decimal)));
            dt.Columns.Add(new DataColumn("MaximumMargin", typeof(decimal)));
            dt.Columns.Add(new DataColumn("MinimumMargin", typeof(decimal)));
            dt.Columns.Add(new DataColumn("DesiredMargin", typeof(decimal)));
            dt.Columns.Add(new DataColumn("IsKeyLine", typeof(bool)));
            dt.Columns.Add(new DataColumn("CustomProductField1", typeof(string)));
            dt.Columns.Add(new DataColumn("CustomProductField2", typeof(string)));
            dt.Columns.Add(new DataColumn("CustomProductField3", typeof(string)));
            dt.Columns.Add(new DataColumn("CustomProductField4", typeof(string)));
            dt.Columns.Add(new DataColumn("CustomProductField5", typeof(string)));
            dt.Columns.Add(new DataColumn("CustomProductField6", typeof(string)));
            dt.Columns.Add(new DataColumn("CustomProductField7", typeof(string)));
            dt.Columns.Add(new DataColumn("CustomProductField8", typeof(string)));
            dt.Columns.Add(new DataColumn("CustomProductField9", typeof(string)));
            dt.Columns.Add(new DataColumn("CustomProductField10", typeof(string)));
            dt.Columns.Add(new DataColumn("VariantOf", typeof(string)));
            dt.Columns.Add(new DataColumn("BeatenCompetitorPrice", typeof(decimal)));
            dt.Columns.Add(new DataColumn("TargetMarginPercent", typeof(decimal)));

            foreach (var rule in rules)
            {
                var newRow = dt.NewRow();
                newRow["ProductInventoryFK"] = rule.Product.ProductInventoryID;
                newRow["ChannelFK"] = rule.Product.ChannelFK;
                newRow["BrandFK"] = rule.Product.BrandFK;
                newRow["Price"] = rule.Product.Price;
                newRow["ManufacturerPartNo"] = rule.Product.ManufacturerPartNo;
                newRow["Description"] = rule.Product.Description;
                newRow["ClientProductID"] = rule.Product.ClientProductID;
                newRow["LnkdBrandFK"] = rule.Product.LnkdBrandFK ?? (object)DBNull.Value;
                newRow["LnkdManufacturerPartNo"] = rule.Product.LnkdManufacturerPartNo ?? (object)DBNull.Value;
                newRow["ProductCategoryFK"] = rule.Product.ProductCategoryFK;
                newRow["Price"] = rule.Product.Price;
                newRow["AltPrice1"] = rule.Product.AltPrice1 ?? (object)DBNull.Value;
                newRow["AltPrice2"] = rule.Product.AltPrice2 ?? (object)DBNull.Value;
                newRow["AltPrice3"] = rule.Product.AltPrice3 ?? (object)DBNull.Value;
                newRow["AltPrice4"] = rule.Product.AltPrice4 ?? (object)DBNull.Value;
                newRow["AltPrice5"] = rule.Product.AltPrice5 ?? (object)DBNull.Value;
                newRow["AltPrice6"] = rule.Product.AltPrice6 ?? (object)DBNull.Value;
                newRow["AltPrice7"] = rule.Product.AltPrice7 ?? (object)DBNull.Value;
                newRow["AltPrice8"] = rule.Product.AltPrice8 ?? (object)DBNull.Value;
                newRow["AltPrice9"] = rule.Product.AltPrice9 ?? (object)DBNull.Value;
                newRow["AltPrice10"] = rule.Product.AltPrice10 ?? (object)DBNull.Value;
                newRow["PriceRuleFK"] = rule.Rule.PriceRuleFK;
                newRow["CalculationOutcome"] = rule.Components.CalculationOutcome;
                newRow["BeatRateNumber"] = rule.Components.BeatRateNumber;
                newRow["StockQuantity"] = rule.Rule.StockQuantity ?? (object)DBNull.Value;
                newRow["CheapestCostPrice"] = rule.Rule.CostPrice;
                newRow["CheapestCompetitorPrice"] = rule.Components.CheapestCompetitorPrice;

                if (rule.Product.Price > 0)
                {
                    newRow["GrossMarginPercent"] = Math.Round(((rule.Product.Price - GetCostUplift(rule.Rule)) / rule.Product.Price) * 100, 2);
                }
                else
                {
                    newRow["GrossMarginPercent"] = 0;
                }

                newRow["GrossMarginValue"] = Math.Round(rule.Product.Price - rule.Rule.CostPrice, 2);
                newRow["CompetitorDifference"] = Math.Round(Math.Abs(rule.Components.CheapestCompetitorPrice - rule.Product.Price), 2);
                newRow["MaximumMargin"] = Math.Round(rule.Components.MaxMargin, 2);
                newRow["MinimumMargin"] = Math.Round(rule.Components.MinMargin, 2);
                newRow["DesiredMargin"] = Math.Round(rule.Components.DesiredMargin, 2);
                newRow["IsKeyLine"] = rule.Product.IsKeyLine;
                newRow["CustomProductField1"] = rule.Product.CustomProductField1;
                newRow["CustomProductField2"] = rule.Product.CustomProductField2;
                newRow["CustomProductField3"] = rule.Product.CustomProductField3;
                newRow["CustomProductField4"] = rule.Product.CustomProductField4;
                newRow["CustomProductField5"] = rule.Product.CustomProductField5;
                newRow["CustomProductField6"] = rule.Product.CustomProductField6;
                newRow["CustomProductField7"] = rule.Product.CustomProductField7;
                newRow["CustomProductField8"] = rule.Product.CustomProductField8;
                newRow["CustomProductField9"] = rule.Product.CustomProductField9;
                newRow["CustomProductField10"] = rule.Product.CustomProductField10;
                newRow["VariantOf"] = rule.Product.VariantOf;
                newRow["BeatenCompetitorPrice"] = rule.Components.BeatenCompetitorPrice;               

                decimal? tm = null;
                if (rule.Components.BeatenCompetitorPrice != 0)
                {
                    tm = ((rule.Components.BeatenCompetitorPrice - GetCostUplift(rule.Rule)) * 100 / rule.Components.BeatenCompetitorPrice);
                }
                newRow["TargetMarginPercent"] = tm ?? (object)DBNull.Value;

                dt.Rows.Add(newRow);
            }

            return dt;
        }

        private void InitializeTenant()
        {
            _tenant = new Tenant();
            _channel = _tenant.GetChannelRecord(Convert.ToInt32(_suppliedParams["channelid"]));
            _tenant.SetupTenantDelegates(_channel);

            _testMode = _suppliedParams["debug"] == "1";
        }

        public static void OutputPrices(Channel channel, MemoryStream stream)
        {
            stream.Position = 0;
            OutputPricesToFtp(channel, stream);
        }

        private static void OutputPricesToEmail(Channel channel, MemoryStream csv)
        {
            try
            {
                if (string.IsNullOrEmpty(channel.OutputFileEmailAddress)) return;

                CommonDataFunctions.CreateLogEntry(channel, "START OutputPricesToEmail", "Information");
                CommonDataFunctions.CreateLogEntry(channel, "Sending Email to: " + channel.OutputFileEmailAddress, "Information");

                var subject = "Priceology file for channel: " + channel.ChannelName;
                var body = "Please find attached your pricing file for the channel: " + channel.ChannelName;
                var emailTo = new List<string>
                {
                    channel.OutputFileEmailAddress
                };

                Email.SendEmail(body, subject, emailTo, "noreply@priceology.io", csv, channel.ChannelName);
                CommonDataFunctions.CreateLogEntry(channel, "END OutputPricesToFtp", "Information");
            }
            catch (Exception e)
            {
                CommonDataFunctions.CreateLogEntry(channel, "Could not output prices to email address - '" +
                    channel.OutputFileEmailAddress + "' Error: " + e.Message + " Stack: " + e.StackTrace, "Notification", true);
            }
        }

        private static void OutputPricesToFtp(Channel channel, MemoryStream csv)
        {
            var ftpDetail = channel.FTPSettings.FirstOrDefault(x => x.FileTypeFK == 52);

            try
            {
                CommonDataFunctions.CreateLogEntry(channel, "START OutputPricesToFtp", "Information");

                if (ftpDetail != null)
                {
                    CommonDataFunctions.CreateLogEntry(channel, "Uploading to FTP: " + ftpDetail.FTPServer, "Information");

                    ftpDetail.FTPPath = !string.IsNullOrEmpty(ftpDetail.FTPPath) ? $"//{ftpDetail.FTPPath}//" : string.Empty;

                    var hostDetails = new Ftp.FtpHostDetails
                    {
                        FileName = ftpDetail.FTPFileName,
                        FtpHost = ftpDetail.FTPServer,
                        FtpUser = ftpDetail.FTPUser,
                        FtpPassword = ftpDetail.FTPPassword,
                        FolderPath = ftpDetail.FTPPath,
                        Protocol = CommonFunctions.LookupFtpProtocol(ftpDetail.FTPProtocolFK)
                    };

                    Ftp.UploadFTPFile(hostDetails, csv);
                }

                CommonDataFunctions.CreateLogEntry(channel, "START OutputPricesToFtp", "Information");
            }
            catch (Exception e)
            {
                if (ftpDetail != null)
                    CommonDataFunctions.CreateLogEntry(channel, "Could not output prices to FTP '" +
                        ftpDetail.FTPServer + "'. Error: " + e.Message + " Stack: " + e.StackTrace, "Notification", true);
            }
        }

        public static MemoryStream CreateInMemoryCsv(List<PriceRuleDetail> priceRules, Channel channel)
        {
            _customFields = channel.CustomFields.Where(x => x.Lookup.LookupName == "Price Adjustment Field").ToList();
            return CreateCsvFileInMemory();
        }

        private static MemoryStream CreateCsvFileInMemory()
        {
            var memoryStream = new MemoryStream();
            var writer = new Csv.CsvFileWriter(memoryStream, '\t');

            CreateCsvHeader(writer);
            CreateCsvData(writer);
            writer.Flush();

            return memoryStream;
        }

        private static void CreateCsvHeader(Csv.CsvFileWriter writer)
        {
            var firstRow = new Csv.CsvRow
            {
                "Brand",
                "ManufacturerPartNo",
                "ClientProductID",
                "Description",
                "Category",
                "Price"
            };

            firstRow.AddRange(_customFields.Select(cf => cf.UserFieldName));
            writer.WriteRow(firstRow);
        }

        private static void CreateCsvData(Csv.CsvFileWriter writer)
        {
            var nostCostPriceOutcome = _calculationOutcome.Find(x => x.LookupName == "No Cost Price").LookupID;

            foreach (var pr in _finalRulesList)
            {
                if (pr.Components.CalculationOutcome == nostCostPriceOutcome)
                    continue;

                var newRow = new Csv.CsvRow
                {
                    pr.Product.Brand.BrandName,
                    pr.Product.ManufacturerPartNo,
                    pr.Product.ClientProductID,
                    pr.Product.Description,
                    pr.Product.ProductCategory.CategoryName,
                    pr.Product.Price.ToString()
                };

                newRow.AddRange(_customFields.Select(cf => typeof(ProductInventory).GetProperty(cf.DBFieldName.Replace("Adj", ""))).Select(property => property.GetValue(pr.Product, null).ToString()));

                writer.WriteRow(newRow);
            }
        }

        public static void OutputPricesToApi(TenantSetting setting)
        {
            // Code here to send the final prices back to the API, SAP or similar.
        }

        //public static void OutputPricesToSitc(Channel channel, MemoryStream stream)
        //{
        //try
        //{
        //    CommonDataFunctions.CreateLogEntry(channel, "START Upload StockAndPrices file to FTP", "Information");
        //    var ftp = channel.FTPSettings.Where(x => x.Lookup.LookupName == "Output Inventory").FirstOrDefault();

        //    //Rename files in FTP location 2
        //    Ftp.RenameAllFtpFiles(ftp.FTPServer, ftp.FTPUser, ftp.FTPPassword, "old_");

        //    //Code here to copy files from FTP location 1 over to FTP location 2
        //    //Ftp.CopyAllFilesFromFtpToFtp(details);

        //    //Code here to replace the prices file on FTP location 2

        //    if (ftp != null)
        //    {
        //        var result = Ftp.DownloadFTPFile(ftp.FTPServer, ftp.FTPUser, ftp.FTPPassword, "StockAndPrices.csv",
        //            channel.TenantFK.ToString() + "\\" + "StockAndPrices.csv",
        //                Ftp.FTPProtocol.SFTP,
        //                "tenantfolders");

        //        var memoryStream = new MemoryStream();
        //        using (var writer = new Csv.CsvFileWriter(memoryStream, ','))
        //        using (var reader = new TextFieldParser(result.Path))
        //        {
        //            reader.SetDelimiters(new string[] { "," });
        //            reader.TrimWhiteSpace = true;

        //            var headings = reader.ReadFields();

        //            var firstRow = new Csv.CsvRow();
        //            foreach (var field in headings)
        //            {
        //                firstRow.Add(field);
        //            }

        //            writer.WriteRow(firstRow);

        //            while (!reader.EndOfData)
        //            {
        //                var rowData = reader.ReadFields();
        //                var productId = GetRowFieldData(rowData, 0);
        //                var stock = GetRowFieldData(rowData, 1);
        //                var price = _finalRulesList.Find(x => x.Product.ClientProductID == productId).Product.Price;
        //                var cost = GetRowFieldData(rowData, 3);
        //                var distributorId = GetRowFieldData(rowData, 4);

        //                var newRow = new Csv.CsvRow();
        //                newRow.Add(productId);
        //                newRow.Add(stock);
        //                newRow.Add(price.ToString());
        //                newRow.Add(cost);
        //                newRow.Add(distributorId);
        //                writer.WriteRow(newRow);
        //            }

        //            writer.Flush();
        //        }

        //        Ftp.UploadFTPFile(memoryStream, ftp.FTPServer + "/" + ftp.FTPFileName, ftp.FTPUser, ftp.FTPPassword);
        //        //TEST: Ftp.UploadFTPFile(memoryStream, "ftp://10.101.1.50/" + ftp.FTPFileName, "dp001", "test123");

        //        CommonDataFunctions.CreateLogEntry(channel, "END Upload StockAndPrices file to FTP", "Information");
        //    }
        //}
        //catch (Exception ex)
        //{
        //    CommonDataFunctions.CreateLogEntry(channel, "Could not upload StockAndPrices file to SITC. Reason:" + 
        //        ex.Message, "Notification");
        //}

        //using (var csv = CreateCsvFileInMemory())
        //{
        //    OutputPricesToEmail(channel, csv);
        //}
        //}

        private static List<Lookup> GetCalculationOutcomes()
        {
            var crud = new CrudLookup();
            return crud.Read(x => x.LookupType.LookupTypeName == "CalculationOutcome");
        }

        private static void DoRounding(PriceRuleDetail priceRule)
        {
            switch (priceRule.Rule.RoundingGroup)
            {
                case "Rounding Rule 1":

                    RoundingGroups.SetGroup1Prices(priceRule.Product);

                    break;
            }
        }

        private static void UpdateProductCounts(Channel channel)
        {
            try
            {
                CommonDataFunctions.CreateLogEntry(channel, "START UpdateProductCounts", "Information");

                using (var db = new DP001Entities())
                {
                    db.SetPriceRuleProductCounts(channel.ChannelID);
                }

                CommonDataFunctions.CreateLogEntry(channel, "END UpdateProductCounts", "Information");
            }
            catch (Exception e)
            {
                CommonDataFunctions.CreateLogEntry(channel, "**ERROR Updating Product Counts** - " + e.Message +
                    " Stack: " + e.StackTrace, "Information");
            }
        }

        private void CleanupStagingTables()
        {
            CommonDataFunctions.CreateLogEntry(_channel, "START CleanupStaging Tables", "Information");

            var sqlParms = new List<SqlParameter>();
            var sqlParm1 = new SqlParameter("@ChannelFK", SqlDbType.Int) { Value = _channel.ChannelID };
            sqlParms.Add(sqlParm1);
            var isSuccess = SQL.ExecuteStoredProcedure("DP001", "DeleteStagingTableEntries", sqlParms, _channel.ChannelID);

            if (!isSuccess)
                CommonDataFunctions.CreateLogEntry(_channel, "Unable to start process due to errors found. Please contact support.", "Notification");

            CommonDataFunctions.CreateLogEntry(_channel, "END CleanupStaging Tables", "Information");
        }

        private static IEnumerable<string> GetCustomMethods(Channel channel)
        {
            var crud = new CrudLookup();
            return crud.Read(
                x =>
                    x.LookupType.LookupTypeName == "CustomRuleMethod" &&
                    x.TenantLookups.FirstOrDefault().TenantFK == channel.TenantFK).Select(x => x.LookupName);
        }

        private void RunCustomRoutine(
            TargetUser targetUser,
            string targetFunction,
            object extras = null)
        {
            _tenant.CustomRoutines?.Invoke(targetUser, targetFunction, _channel, extras);
        }
    }

    public class PriceRuleDetail
    {
        public GetProductPriceRules_Result Rule { get; set; }
        public ProductInventory Product { get; set; }
        public FinalPriceComponents Components { get; set; }
    }

    public class FinalPriceComponents
    {
        public decimal Nudge { get; set; }
        public decimal DesiredMargin { get; set; }
        public decimal MaxMargin { get; set; }
        public decimal MinMargin { get; set; }
        public decimal CheapestCompetitorPrice { get; set; }
        public int CompetitorCount { get; set; }
        public string PricingRule { get; set; }
        public decimal FinalPrice { get; set; }
        public int CalculationOutcome { get; set; }
        public int BeatRateNumber { get; set; }
        public decimal PreviousPrice { get; set; }
        public decimal CostUpliftPrice { get; set; }
        public decimal BeatenCompetitorPrice { get; set; }
    }
}
