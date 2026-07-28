using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NGBP.DataAccessLayer.SCOM.Services;
using NGBP.DataAccessLayer.SCOM.SimpleEntities;
using System.Data;
using System.IO;
using ngBatchProcesses.BusinessObjects.Shared;

namespace ngBatchProcesses.BusinessObjects.PricingEngine
{
    public class PriceRule
    {
        static PriceRule()
        {
            hasErrorOccured = false;
            stnFunc = new StandardFunctions();
            dtFinalPrices = CreateDataTable();
            validVatRate = decimal.TryParse(StandardFunctions.GetConfigurationSetting("Pricing", "Vat"), out vatRate);
            validSalesFrequency = int.TryParse(StandardFunctions.GetConfigurationSetting("Pricing", "SalesFrequency"), out salesFrequencyLimit);
        }

        public enum productType
        {
            OEM,
            NonOEM,
            Assembly
        }

        enum senseCheck
        {
            OK,
            Fail
        }

        static StandardFunctions stnFunc;
        static DataTable dtFinalPrices;
        static bool hasErrorOccured;
        static bool validVatRate;
        static decimal vatRate = 0;
        static bool validSalesFrequency;
        static int salesFrequencyLimit = 0;

        public static void ProcessPricingRules()
        {
            stnFunc.AddToActivityLog("Started Batch Program with switch: calculateprices" + Environment.NewLine);

            List<PriceRuleSE> priceRuleList = GetAllPriceRules("ngmd.GetPricingRulesOEM");
            ProcessProducts(priceRuleList, productType.OEM);

            priceRuleList = GetAllPriceRules("ngmd.GetPricingRulesNonOEM");
            ProcessProducts(priceRuleList, productType.NonOEM);

            priceRuleList = GetAllPriceRules("ngmd.GetPricingRulesAssemblies");
            ProcessProducts(priceRuleList, productType.Assembly);

            SaveAndSendLog();
        }

        private static void ProcessProducts(List<PriceRuleSE> prSE, productType type)
        {
            foreach (PriceRuleSE rule in prSE)
            {
                try
                {
                    switch (type)
                    {
                        case productType.OEM:
                            ProcessOEM(rule, type);
                            break;
                        case productType.NonOEM:
                            ProcessNonOEM(rule, type);
                            break;
                        case productType.Assembly:
                            ProcessAssembly(rule, type);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    stnFunc.AddToActivityLog("**Error** Occured Calculating price for product FK: " + rule.ProductFK + Environment.NewLine);
                    stnFunc.AddToActivityLog("**Error Message**: " + ex.Message + Environment.NewLine);
                    stnFunc.AddToActivityLog("**Error Stack Trace**: " + ex.StackTrace + Environment.NewLine);
                    hasErrorOccured = true;
                }
            }

            CreateCSVFile();
            InsertNewPrices();
            dtFinalPrices.Rows.Clear();
        }

        private static void InsertNewPrices()
        {
            string filePath = Properties.Settings.Default.SQLServerLocalDirectory + "PMSPrices\\updatepricesNEW.csv";

            try
            {
                PriceRuleServices.UpdateNewPrices(filePath);
                stnFunc.AddToActivityLog("Successfully executed stored procedure ngmd.InsertProductPricesNEW");
            }
            catch (Exception ex)
            {
                stnFunc.AddToActivityLog("**Error** Executing stored procedure ngmd.InsertProductPricesNEW: " + ex.Message);
                stnFunc.AddToActivityLog("Stack Trace: " + ex.StackTrace);
            }
        }

        private static void SaveAndSendLog()
        {
            stnFunc.AddToActivityLog("Finished Batch Program with switch: calculateprices" + Environment.NewLine);
            string acitivityLogFileName = stnFunc.LogActivity("calculateprices");
            if (hasErrorOccured && Properties.Settings.Default.Environment == "Live")
            {
                stnFunc.SendSimpleEmail("calculateprices", acitivityLogFileName);
            }
        }

        private static List<PriceRuleSE> GetAllPriceRules(string spName)
        {
            List<PriceRuleSE> prSE = new List<PriceRuleSE>();

            try
            {
                PriceRuleServices prServices = new PriceRuleServices();
                prSE = prServices.GetAllPriceRules(spName);
            }
            catch (Exception ex)
            {
                stnFunc.AddToActivityLog("**Error** Occurred getting Price rules" + Environment.NewLine);
                stnFunc.AddToActivityLog("**Error Message**: " + ex.Message + Environment.NewLine);
                stnFunc.AddToActivityLog("**Error Stack Trace**: " + ex.StackTrace + Environment.NewLine);
                hasErrorOccured = true;
            }

            return prSE;
        }

        private static DataTable CreateDataTable()
        {
            DataTable dtFinalPrices = new DataTable();
            dtFinalPrices.Columns.Add("productFK", typeof(int));
            dtFinalPrices.Columns.Add("websiteInventoryFK", typeof(int));
            dtFinalPrices.Columns.Add("partNo", typeof(string));
            dtFinalPrices.Columns.Add("finalPrice", typeof(decimal));
            dtFinalPrices.Columns.Add("cheapestCostPrice", typeof(decimal));
            dtFinalPrices.Columns.Add("cheapestCompetitorPrice", typeof(decimal));
            dtFinalPrices.Columns.Add("competitorCount", typeof(int));
            dtFinalPrices.Columns.Add("pricingRule", typeof(string));
            dtFinalPrices.Columns.Add("breakPrice1", typeof(decimal));
            dtFinalPrices.Columns.Add("breakPrice2", typeof(decimal));
            dtFinalPrices.Columns.Add("breakPrice3", typeof(decimal));
            dtFinalPrices.Columns.Add("breakPrice4", typeof(decimal));
            dtFinalPrices.Columns.Add("breakPrice5", typeof(decimal));
            dtFinalPrices.Columns.Add("finalPriceIncVat", typeof(decimal));
            dtFinalPrices.Columns.Add("breakPrice1IncVat", typeof(decimal));
            dtFinalPrices.Columns.Add("breakPrice2IncVat", typeof(decimal));
            dtFinalPrices.Columns.Add("breakPrice3IncVat", typeof(decimal));
            dtFinalPrices.Columns.Add("breakPrice4IncVat", typeof(decimal));
            dtFinalPrices.Columns.Add("breakPrice5IncVat", typeof(decimal));
            dtFinalPrices.Columns.Add("basePrice", typeof(decimal));
            dtFinalPrices.Columns.Add("breakPricingRule", typeof(string));
            return dtFinalPrices;
        }

        private static void ProcessOEM(PriceRuleSE rule, productType type)
        {
            IEnumerable<competitor> compsListSortedDesc = GetSortedCompetitors(rule, "DESC");
            int totalCompetitors = compsListSortedDesc.Count();
            int numberOfCompsToBeat = GetNumberOfCompetitorsToBeat(totalCompetitors, rule.CompetitorsToBeat);
            decimal costUpliftPrice = GetCostUplift(rule);

            FinalPriceComponents components = new FinalPriceComponents();
            components.Nudge = GetNudgePrice(numberOfCompsToBeat, compsListSortedDesc, rule);
            components.MinMargin = CalculateMargin(costUpliftPrice, rule.MinMarginPercent);
            components.MaxMargin = CalculateMargin(costUpliftPrice, rule.MaxMarginPercent);
            components.DesiredMargin = CalculateMargin(costUpliftPrice, rule.DesiredMargin);
            components.CompetitorCount = totalCompetitors;
            components.CheapestCompetitorPrice = SetCheapestCompPrice(components, compsListSortedDesc);
            components.ProductType = type;

            SetFinalPriceOEM(components, rule);
            CalculateBreakPricing(components, rule, type);
            SetBreakPricingIncVat(components);
            AddToDataTable(rule, components);
        }

        private static void ProcessNonOEM(PriceRuleSE rule, productType type)
        {
            FinalPriceComponents components = new FinalPriceComponents();
            components.NonOEMDiscount = CalculateNonOEMDiscount(rule);
            components.ProductType = type;
            SetFinalPriceNonOEM(components, rule);
            CalculateBreakPricing(components, rule, type);
            SetBreakPricingIncVat(components);
            AddToDataTable(rule, components);
        }

        private static void ProcessAssembly(PriceRuleSE rule, productType type)
        {
            IEnumerable<competitor> compsListSortedDesc = GetSortedCompetitors(rule, "DESC");

            FinalPriceComponents components = new FinalPriceComponents();
            components.CheapestCompetitorPrice = SetCheapestCompPrice(components, compsListSortedDesc);
            CalculateBreakPricing(components, rule, type);
            CalculateFinalPriceIncVat(components);
            SetBreakPricingIncVat(components);
            AddToDataTable(rule, components);
        }

        private static void CalculateBreakPricing(FinalPriceComponents components, PriceRuleSE rule, productType type)
        {
            decimal price = 0;
            decimal finalBreakMinMargin = 0;
            decimal break1DesiredPrice = 0;
            decimal break2DesiredPrice = 0;

            if(type == productType.Assembly)
            {
                price = rule.FixedPriceOverride == 0 ? 
                    Math.Round(rule.BasePrice - (rule.BasePrice * rule.PackDiscount), 2) : (decimal)rule.FixedPriceOverride;
                finalBreakMinMargin = Math.Round(CalculateMargin(rule.CostPrice, rule.FinalBreakMinimumMarginAssemblies), 2);
            }
            else
            {
                price = components.FinalPrice;
                finalBreakMinMargin = CalculateMargin(rule.CostPrice, rule.FinalBreakMinimumMarginStock);
            }

            break1DesiredPrice = CalculateDiscount(price, rule.BreakPrice1);
            break2DesiredPrice = CalculateDiscount(price, rule.BreakPrice2);

            //Break pricing Safety Net.
            if (break2DesiredPrice > finalBreakMinMargin)
            {
                //Use Desired
                components.BreakPrice1 = break1DesiredPrice;
                components.BreakPrice2 = break2DesiredPrice;

                if (type == productType.Assembly)
                {
                    components.FinalPrice = price;
                    SetBreakPricing(components, rule);

                    if (rule.FixedPriceOverride == 0)
                    {
                        components.PricingRule = type == productType.Assembly ?
                            "Disc. " + Math.Round(rule.PackDiscount * 100, 2) + "%" : components.PricingRule;
                    }
                    else
                    {
                        components.PricingRule = "Fixed Price";
                    }
                }

                components.BreakPricingRule = "Desired";
            }
            else
            {
                //Use Minimum
                if (type == productType.Assembly)
                {
                    decimal increment = (rule.BasePrice - finalBreakMinMargin) / 3;
                    components.BreakPrice2 = finalBreakMinMargin;
                    components.BreakPrice1 = Math.Round(finalBreakMinMargin + increment, 2);
                    components.FinalPrice = Math.Round(components.BreakPrice1 + increment, 2);
                    components.PricingRule = "Custom Disc. Breaks";
                    components.BreakPricingRule = "Minimum";
                }
                else
                {
                    components.BreakPrice2 = finalBreakMinMargin;
                    components.BreakPrice1 = Math.Round(components.BreakPrice2 + ((price - finalBreakMinMargin) / 2), 2);
                    components.BreakPricingRule = "Minimum";
                }

                if (components.BreakPrice1 > components.FinalPrice || components.BreakPrice2 > components.FinalPrice)
                {
                    components.BreakPrice1 = price;
                    components.BreakPrice2 = price;
                    components.FinalPrice = price;
                    components.BreakPricingRule = "Maximum";
                }
            }
        }

        private static decimal CalculateDiscount(decimal price, decimal discount)
        {
            return Math.Round(price - (price * discount), 2);
        }

        private static decimal CalculateNonOEMDiscount(PriceRuleSE rule)
        {
            decimal discountPrice = 0;

            if (rule.BasePrice > 0)
            {
                discountPrice = Math.Round(rule.BasePrice - (rule.BasePrice * rule.CompatDiscount), 2);
            }
            else
            {
                discountPrice = Math.Round(rule.CostPrice / (decimal)0.6, 2);
            }

            return discountPrice;
        }

        private static decimal SetCheapestCompPrice(FinalPriceComponents components, IEnumerable<competitor> compsList)
        {
            if (compsList.Count() > 0)
            {
                components.CheapestCompetitorPrice = compsList.Last().price;
            }
            else
            {
                components.CheapestCompetitorPrice = 0;
            }

            return components.CheapestCompetitorPrice;
        }

        private static decimal CalculateMargin(decimal costUpliftPrice, decimal margin)
        {
            decimal calculatedMargin = 0;

            calculatedMargin = costUpliftPrice / (1 - margin);

            return calculatedMargin;
        }

        private static decimal GetCostUplift(PriceRuleSE priceRule)
        {
            decimal costUplift = 0;

            if (priceRule.CostUpliftIsPercent)
            {
                costUplift = priceRule.CostPrice + (priceRule.CostPrice * priceRule.CostUplift);
            }
            else
            {
                costUplift = priceRule.CostPrice + priceRule.CostUplift;
            }

            return costUplift;
        }

        private static int GetNumberOfCompetitorsToBeat(int totalCompetitors, decimal competitorsToBeatPercent)
        {
            int beat = 0;

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

        private static decimal GetNudgePrice(int competitorsToBeat, IEnumerable<competitor> competitorsList, PriceRuleSE rule)
        {
            decimal nudgePrice = 0;

            if (competitorsToBeat > 0)
            {
                decimal competitorsLowestPrice = competitorsList.Take(competitorsToBeat).Select(x => x.price).Last();
                nudgePrice = competitorsLowestPrice - (competitorsLowestPrice * rule.Nudge);
            }
            else
            {
                stnFunc.AddToActivityLog("**Warning** This product has no competitor pricing: " + rule.PartNo);
                stnFunc.AddToActivityLog("Using desired margin and cost price to calculate price" + Environment.NewLine);
            }

            return nudgePrice;
        }

        private static void SetFinalPriceOEM(FinalPriceComponents components, PriceRuleSE rule)
        {
            if (rule.FixedPriceOverride == 0)
            {
                if (components.Nudge > 0)
                {
                    if (components.Nudge > components.DesiredMargin)
                    {
                        if (components.Nudge > components.MaxMargin)
                        {
                            components.FinalPrice = Math.Round(components.MaxMargin, 2);
                            components.PricingRule = "Maximum";
                        }
                        else
                        {
                            components.FinalPrice = Math.Round(components.Nudge, 2);
                            components.PricingRule = "Nudge Up";
                        }
                    }
                    else if (components.Nudge < components.DesiredMargin)
                    {
                        if (components.Nudge < components.MinMargin)
                        {
                            components.FinalPrice = Math.Round(components.MinMargin, 2);
                            components.PricingRule = "Minimum";
                        }
                        else
                        {
                            components.FinalPrice = Math.Round(components.Nudge, 2);
                            components.PricingRule = "Nudge Down";
                        }
                    }
                    else
                    {
                        components.FinalPrice = Math.Round(components.DesiredMargin, 2);
                        components.PricingRule = "Desired";
                    }
                }
                else
                {
                    components.FinalPrice = Math.Round(components.DesiredMargin, 2);
                    components.PricingRule = "Desired";
                }
            }
            else
            {
                components.FinalPrice = (decimal)rule.FixedPriceOverride;
                components.PricingRule = "Fixed Price";
            }

            CalculateFinalPriceIncVat(components);
        }

        private static void SetFinalPriceNonOEM(FinalPriceComponents components, PriceRuleSE rule)
        {
            if (rule.FixedPriceOverride == 0)
            {
                decimal actualMarginPercent = 0;

                if (components.NonOEMDiscount > 0)
                {
                    actualMarginPercent = (components.NonOEMDiscount - rule.CostPrice) / components.NonOEMDiscount;
                }

                decimal actualMarginValue = components.NonOEMDiscount - rule.CostPrice;
                components.FinalPrice = ApplyAdjustmentsNonOEM(components, rule, actualMarginValue, actualMarginPercent);
                components.FinalPrice = ApplySalesYtdAdjustmentsNonOEM(components, rule) ?? components.FinalPrice;
                components.FinalPrice = Math.Round(components.FinalPrice, 2);
            }
            else
            {
                components.FinalPrice = (decimal)rule.FixedPriceOverride;
                components.PricingRule = "Fixed Price";
            }

            CalculateFinalPriceIncVat(components);
        }

        private static decimal? ApplySalesYtdAdjustmentsNonOEM(FinalPriceComponents components, PriceRuleSE rule)
        {
            decimal? returnPrice = null;

            if (validSalesFrequency)
            {
                if (rule.SalesYearToDate < salesFrequencyLimit)
                {
                    decimal marginOverrideValue = rule.CostPrice + rule.CompatOverrideValue;
                    decimal marginOverridePercent = rule.CostPrice / (1 - rule.CompatOverrideMargin);
                    returnPrice = Math.Max(marginOverrideValue, marginOverridePercent);
                    components.PricingRule = "Sales < " + salesFrequencyLimit;
                }
            }

            return returnPrice;
        }

        private static decimal ApplyAdjustmentsNonOEM(FinalPriceComponents components, PriceRuleSE rule,
            decimal actualMarginValue, decimal actualMarginPercent)
        {
            decimal returnPrice = 0;

            senseCheck maxMarginValueCheck = CheckMaxMarginValueNonOEM(actualMarginValue, rule);
            senseCheck maxMarginPercentCheck = CheckMaxMarginPercentNonOEM(actualMarginPercent, rule);
            senseCheck minMarginValueCheck = CheckMinMarginValueNonOEM(actualMarginValue, rule);

            if (maxMarginValueCheck == senseCheck.OK &&
                maxMarginPercentCheck == senseCheck.OK &&
                minMarginValueCheck == senseCheck.OK)
            {
                returnPrice = components.NonOEMDiscount;
                components.PricingRule = rule.BasePrice > 0 ? "OEM Discount" : "40% Margin";
            }
            else if (maxMarginPercentCheck == senseCheck.Fail &&
                maxMarginValueCheck == senseCheck.Fail)
            {
                decimal maxMarginValueCalc = rule.CostPrice + rule.MaxMarginValue;
                decimal maxMarginPercentCalc = rule.CostPrice / (1 - rule.MaxMarginPercent);
                returnPrice = Math.Min(maxMarginValueCalc, maxMarginPercentCalc);

                if (maxMarginValueCalc == returnPrice)
                {
                    components.PricingRule = "Max Margin Value";
                }
                else
                {
                    components.PricingRule = "Max Margin Percent";
                }
            }

            if ((returnPrice - rule.CostPrice) < rule.MinMarginValue)
            {
                returnPrice = rule.CostPrice + rule.MinMarginValue;
                components.PricingRule = "Min Margin Value";
            }

            if (returnPrice > 0)
            {
                if ((returnPrice - rule.CostPrice) / returnPrice < (decimal)0.3)
                {
                    returnPrice = rule.CostPrice / (decimal)0.7;
                    components.PricingRule = "30% Margin";
                }
            }

            return returnPrice;
        }

        private static senseCheck CheckMinMarginValueNonOEM(decimal actualMarginValue, PriceRuleSE rule)
        {
            senseCheck returnValue;

            if (actualMarginValue < rule.MinMarginValue)
            {
                returnValue = senseCheck.Fail;
            }
            else
            {
                returnValue = senseCheck.OK;
            }

            return returnValue;
        }

        private static senseCheck CheckMaxMarginPercentNonOEM(decimal actualMarginPercent, PriceRuleSE rule)
        {
            senseCheck returnValue;

            if ((actualMarginPercent * 100) > (rule.MaxMarginPercent * 100))
            {
                returnValue = senseCheck.Fail;
            }
            else
            {
                returnValue = senseCheck.OK;
            }

            return returnValue;
        }

        private static senseCheck CheckMaxMarginValueNonOEM(decimal actualMarginValue, PriceRuleSE rule)
        {
            senseCheck returnValue;

            if (actualMarginValue > rule.MaxMarginValue)
            {
                returnValue = senseCheck.Fail;
            }
            else
            {
                returnValue = senseCheck.OK;
            }

            return returnValue;
        }

        private static IEnumerable<competitor> GetSortedCompetitors(PriceRuleSE rule, string sortDirection)
        {
            List<competitor> compList = new List<competitor>();

            if (rule.CompPrices.Length > 0)
            {
                List<string> compsArray = rule.CompPrices.Split(',').ToList();

                foreach (string item in compsArray)
                {
                    competitor comp = new competitor();
                    string[] itemArray = item.Split('#');
                    comp.providerFK = Convert.ToInt32(itemArray[0]);
                    comp.price = Convert.ToDecimal(itemArray[1]);
                    compList.Add(comp);
                }

                compsArray = null;

                return sortDirection == "ASC" ? compList.OrderBy(x => x.price) : compList.OrderByDescending(x => x.price);
            }
            else
            {
                return compList;
            }
        }

        private static void SetBreakPricing(FinalPriceComponents components, PriceRuleSE rule)
        {
            components.BreakPrice1 = Math.Round(components.FinalPrice * (1 - rule.BreakPrice1), 2);
            components.BreakPrice2 = Math.Round(components.FinalPrice * (1 - rule.BreakPrice2), 2);
            components.BreakPrice3 = Math.Round(components.FinalPrice * (1 - rule.BreakPrice3), 2);
            components.BreakPrice4 = Math.Round(components.FinalPrice * (1 - rule.BreakPrice4), 2);
            components.BreakPrice5 = Math.Round(components.FinalPrice * (1 - rule.BreakPrice5), 2);
        }

        private static void SetBreakPricingIncVat(FinalPriceComponents components)
        {
            components.BreakPrice1IncVat = Math.Round(components.BreakPrice1 * (1 + vatRate), 2);
            components.BreakPrice2IncVat = Math.Round(components.BreakPrice2 * (1 + vatRate), 2);
            components.BreakPrice3IncVat = Math.Round(components.BreakPrice3 * (1 + vatRate), 2);
            components.BreakPrice4IncVat = Math.Round(components.BreakPrice4 * (1 + vatRate), 2);
            components.BreakPrice5IncVat = Math.Round(components.BreakPrice5 * (1 + vatRate), 2);
        }

        private static void CalculateFinalPriceIncVat(FinalPriceComponents components)
        {
            if (validVatRate)
                components.FinalPriceIncVat = Math.Round(components.FinalPrice * (1 + vatRate), 2);
        }

        private static void AddToDataTable(PriceRuleSE rule, FinalPriceComponents components)
        {
            DataRow dr = dtFinalPrices.NewRow();
            dr["productFK"] = rule.ProductFK;
            dr["websiteInventoryFK"] = rule.WebsiteInventoryFK;
            dr["partNo"] = rule.PartNo;
            dr["finalPrice"] = components.FinalPrice;
            dr["cheapestCostPrice"] = rule.CostPrice;
            dr["cheapestCompetitorPrice"] = components.CheapestCompetitorPrice;
            dr["competitorCount"] = components.CompetitorCount;
            dr["pricingRule"] = components.PricingRule;
            dr["breakPrice1"] = components.BreakPrice1;
            dr["breakPrice2"] = components.BreakPrice2;
            dr["breakPrice3"] = components.BreakPrice3;
            dr["breakPrice4"] = components.BreakPrice4;
            dr["breakPrice5"] = components.BreakPrice5;
            dr["finalPriceIncVat"] = components.FinalPriceIncVat;
            dr["breakPrice1IncVat"] = components.BreakPrice1IncVat;
            dr["breakPrice2IncVat"] = components.BreakPrice2IncVat;
            dr["breakPrice3IncVat"] = components.BreakPrice3IncVat;
            dr["breakPrice4IncVat"] = components.BreakPrice4IncVat;
            dr["breakPrice5IncVat"] = components.BreakPrice5IncVat;
            dr["basePrice"] = rule.BasePrice;
            dr["breakPricingRule"] = components.BreakPricingRule;
            dtFinalPrices.Rows.Add(dr);
        }

        private static string CreateCSVFile()
        {
            string sqlFilePath = Properties.Settings.Default.SQLServerFilePath;
            string csvFilename = "updatepricesNEW.csv";
            string csvFilepath = sqlFilePath + "PMSPrices\\" + csvFilename;

            try
            {
                StringBuilder sb = new StringBuilder();

                IEnumerable<string> columnNames = dtFinalPrices.Columns.Cast<DataColumn>().Select(column => column.ColumnName);
                sb.AppendLine(string.Join(",", columnNames));

                foreach (DataRow row in dtFinalPrices.Rows)
                {
                    IEnumerable<string> fields = row.ItemArray.Select(field => field.ToString());
                    sb.AppendLine(string.Join(",", fields));
                }

                File.WriteAllText(csvFilepath, sb.ToString());
                stnFunc.AddToActivityLog("Successfully created csv file: " + csvFilepath + "");
            }
            catch (Exception ex)
            {
                stnFunc.AddToActivityLog("Unable to create " + csvFilename + ".csv file.\r\nFailed to create " + csvFilename + ".csv" + ex.Message);
                hasErrorOccured = true;
            }

            return csvFilepath;
        }

    }
}
