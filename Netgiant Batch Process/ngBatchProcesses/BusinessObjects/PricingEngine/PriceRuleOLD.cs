using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NGBP.DataAccessLayer.SCOM.Services;
using NGBP.DataAccessLayer.SCOM.SimpleEntities;
using System.Data;
using System.IO;
using ngBatchProcesses.BusinessObjects.Shared;

namespace ngBatchProcesses.BusinessObjects.PricingEngine
{
    public class PriceRuleOLD
    {
        static PriceRuleOLD()
        {
            hasErrorOccured = false;
            stnFunc = new StandardFunctions();
            dtFinalPrices = CreateDataTable();
        }

        static StandardFunctions stnFunc;
        static DataTable dtFinalPrices;
        static bool hasErrorOccured;

        public static void ProcessPricingRules()
        {
            stnFunc.AddToActivityLog("Started Batch Program with switch: calculateprices" + System.Environment.NewLine);
            List<PriceRuleSEOLD> prSE = GetAllPriceRules();

            foreach (PriceRuleSEOLD rule in prSE)
            {
                try
                {
                    ProcessPriceRule(rule);
                }
                catch (Exception e)
                {
                    stnFunc.AddToActivityLog("**Error** Occured Calculating price for product FK: " + rule.ProductFK + Environment.NewLine);
                    stnFunc.AddToActivityLog("**Error Message**: " + e.Message + Environment.NewLine);
                    stnFunc.AddToActivityLog("**Error Stack Trace**: " + e.StackTrace + Environment.NewLine);
                    hasErrorOccured = true;
                }
            }

            string filePath = CreateCSVFile();
            InsertNewPrices(Properties.Settings.Default.SQLServerLocalDirectory + "PMSPrices\\updateprices.csv");

            SaveAndSendLog();
        }

        private static void InsertNewPrices(string filePath)
        {
            try
            {
                PriceRuleServices.UpdateNewPrices(filePath);
                stnFunc.AddToActivityLog("Successfully executed stored procedure ngmd.InsertProductPrices");
            }
            catch (Exception e)
            {
                stnFunc.AddToActivityLog("**Error** Executing stored procedure ngmd.InsertProductPrices: " + e.Message);
                stnFunc.AddToActivityLog("Stack Trace: " + e.StackTrace);
            }
        }

        private static void SaveAndSendLog()
        {
            stnFunc.AddToActivityLog("Finished Batch Program with switch: calculateprices" + System.Environment.NewLine);
            string acitivityLogFileName = stnFunc.LogActivity();
            if (hasErrorOccured && Properties.Settings.Default.Environment == "Live")
            {
                stnFunc.SendSimpleEmail("calculateprices", acitivityLogFileName);
            }
        }

        private static List<PriceRuleSEOLD> GetAllPriceRules()
        {
            List<PriceRuleSEOLD> prSE = new List<PriceRuleSEOLD>();

            try
            {
                PriceRuleServicesOLD prServices = new PriceRuleServicesOLD();
                prSE = prServices.GetAllPriceRules();
            }
            catch (Exception e)
            {
                stnFunc.AddToActivityLog("**Error** Occurred getting Price rules" + Environment.NewLine);
                stnFunc.AddToActivityLog("**Error Message**: " + e.Message + Environment.NewLine);
                stnFunc.AddToActivityLog("**Error Stack Trace**: " + e.StackTrace + Environment.NewLine);
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
            return dtFinalPrices;
        }

        private static void ProcessPriceRule(PriceRuleSEOLD rule)
        {
            IEnumerable<competitor> compsListSortedDesc = GetSortedCompetitors(rule, "DESC");
            int totalCompetitors = compsListSortedDesc.Count();
            int numberOfCompsToBeat = GetNumberOfCompetitorsToBeat(totalCompetitors, rule.CompetitorsToBeat);
            decimal costUpliftPrice = GetCostUplift(rule);

            FinalPriceComponents components = new FinalPriceComponents();
            components.Nudge = GetNudgePrice(numberOfCompsToBeat, compsListSortedDesc, rule);
            components.MinMargin = CalculateMargin(costUpliftPrice, rule.MinMargin);
            components.MaxMargin = CalculateMargin(costUpliftPrice, rule.MaxMargin);
            components.DesiredMargin = CalculateMargin(costUpliftPrice, rule.DesiredMargin);
            components.CompetitorCount = totalCompetitors;
            if (compsListSortedDesc.Count() > 0)
            {
                components.CheapestCompetitorPrice = compsListSortedDesc.Last().price;
            }
            else
            {
                components.CheapestCompetitorPrice = 0;
            }

            AddToDataTable(rule, GetFinalPrice(components), components);
        }

        private static decimal CalculateMargin(decimal costUpliftPrice, decimal margin)
        {
            decimal calculatedMargin = 0;

            calculatedMargin = costUpliftPrice / (1 - margin);

            return calculatedMargin;
        }

        private static decimal GetCostUplift(PriceRuleSEOLD priceRule)
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

        private static decimal GetNudgePrice(int competitorsToBeat, IEnumerable<competitor> competitorsList, PriceRuleSEOLD rule)
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

        private static decimal GetFinalPrice(FinalPriceComponents components)
        {
            decimal finalPrice = 0;

            if (components.Nudge > 0)
            {
                if (components.Nudge > components.DesiredMargin)
                {
                    if (components.Nudge > components.MaxMargin)
                    {
                        finalPrice = components.MaxMargin;
                        components.PricingRule = "Maximum";
                    }
                    else
                    {
                        finalPrice = components.Nudge;
                        components.PricingRule = "Nudge Up";
                    }
                }
                else if (components.Nudge < components.DesiredMargin)
                {
                    if (components.Nudge < components.MinMargin)
                    {
                        finalPrice = components.MinMargin;
                        components.PricingRule = "Minimum";
                    }
                    else
                    {
                        finalPrice = components.Nudge;
                        components.PricingRule = "Nudge Down";
                    }
                }
                else
                {
                    finalPrice = components.DesiredMargin;
                    components.PricingRule = "Desired";
                }
            }
            else
            {
                finalPrice = components.DesiredMargin;
                components.PricingRule = "Desired";
            }

            return Math.Round(finalPrice, 2);
        }

        private static IEnumerable<competitor> GetSortedCompetitors(PriceRuleSEOLD rule, string sortDirection)
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

        private static void AddToDataTable(PriceRuleSEOLD rule, decimal finalPrice, FinalPriceComponents components)
        {
            DataRow dr = dtFinalPrices.NewRow();
            dr["productFK"] = rule.ProductFK;
            dr["websiteInventoryFK"] = rule.WebsiteInventoryFK;
            dr["partNo"] = rule.PartNo;
            dr["finalPrice"] = finalPrice;
            dr["cheapestCostPrice"] = rule.CostPrice;
            dr["cheapestCompetitorPrice"] = components.CheapestCompetitorPrice;
            dr["competitorCount"] = components.CompetitorCount;
            dr["pricingRule"] = components.PricingRule;
            dtFinalPrices.Rows.Add(dr);
        }

        private static string CreateCSVFile()
        {
            Properties.Settings settings = Properties.Settings.Default;
            string sqlFilePath = settings.SQLServerFilePath;
            string csvFilename = "updateprices.csv";
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
            catch (Exception e)
            {
                stnFunc.AddToActivityLog("Unable to create " + csvFilename + ".csv file.\r\nFailed to create " + csvFilename + ".csv" + e.Message);
                hasErrorOccured = true;
            }

            return csvFilepath;
        }

    }
}
