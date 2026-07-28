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
using Microsoft.VisualBasic.FileIO;

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

        private Dictionary<string, string> _suppliedParams;
        private Channel _channel;
        private Tenant _tenant;
        private static List<PriceRuleDetail> _finalRulesList;
        private static List<Lookup> _calculationOutcome;
        private static bool _testMode;

        private enum CalculationMethod
        {
            CostBase,
            RelatedProductBase,
            FixedPrice
        }

        public void Calculate()
        {
            CommonDataFunctions.CreateLogEntry(_channel, "START CalculatePrices", "Information");

            var ten = new Tenant();
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

                var newFileStream = _tenant.CreateCsv(_finalRulesList, _channel);
                _tenant.OutputPrices(_channel, newFileStream);
                OutputPricesToEmail(_channel, newFileStream);
                newFileStream = null;

                UpdateProductCounts(_channel);
            }
            catch (Exception ex)
            {
                CommonDataFunctions.CreateLogEntry(_channel, "ERROR: " + ex.Message, "Error");
            }

            CommonDataFunctions.CreateLogEntry(_channel, "END CalculatePrices", "Information");
        }

        private void ProcessRules(List<PriceRuleDetail> rules)
        {
            CommonDataFunctions.CreateLogEntry(_channel, "START ProcessRules", "Information");

            foreach (var priceRule in rules)
            {
                var competitors = GetCompetitorPrices(priceRule.Rule);
                var totalCompetitors = competitors.Count;
                var numberToBeat = GetNumberOfCompetitorsToBeat(totalCompetitors, priceRule.Rule.BeatRate);
                var costUpliftPrice = GetCostUplift(priceRule.Rule);

                priceRule.Components = new FinalPriceComponents();
                priceRule.Components.Nudge = GetNudgePrice(numberToBeat, competitors, priceRule.Rule);
                priceRule.Components.MinMargin = CalculateMargin(costUpliftPrice, priceRule.Rule.MinMargin);
                priceRule.Components.MaxMargin = CalculateMargin(costUpliftPrice, priceRule.Rule.MaxMargin);
                priceRule.Components.DesiredMargin = CalculateMargin(costUpliftPrice, priceRule.Rule.DesiredMargin);
                priceRule.Components.CompetitorCount = totalCompetitors;
                priceRule.Components.CheapestCompetitorPrice = SetCheapestCompPrice(priceRule.Components, competitors);
                priceRule.Components.BeatRateNumber = numberToBeat;
                priceRule.Components.PreviousPrice = priceRule.Product.Price;

                // This line is only here to test out the Custom Routine framework. Doesn't do anything at present.
                RunCustomRoutine(TargetUser.NetGiant, "BreakMargins", _channel, priceRule);

                SetFinalPrice(priceRule.Components, priceRule);
                SetAltPrices(priceRule);
                DoRounding(priceRule);
            }

            CommonDataFunctions.CreateLogEntry(_channel, "END ProcessRules", "Information");
        }

        private List<PriceRuleDetail> GetPriceRules(CalculationMethod method)
        {
            var methodName = "";

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

            using (DP001Entities db = new DP001Entities())
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

        private static List<decimal> GetCompetitorPrices(GetProductPriceRules_Result rule)
        {
            var compList = new List<decimal>();

            if (!String.IsNullOrEmpty(rule.Competitors))
            {
                var compsArray = rule.Competitors.Split(',').ToList();

                foreach (string item in compsArray)
                {
                    if (item.Length > 0)
                    {
                        string[] itemArray = item.Split('#');
                        compList.Add(Convert.ToDecimal(itemArray[1]));
                    }
                }

                return compList.OrderByDescending(x => x).ToList();
            }
            else
            {
                return compList;
            }
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
                    beat = Convert.ToInt32(Math.Floor(totalCompetitors * (competitorsToBeatPercent)));
                    break;
            }

            return beat;
        }

        private static decimal GetCostUplift(GetProductPriceRules_Result rule)
        {
            decimal? costUplift = 0;

            if (rule.CostUpliftIsPercent)
            {
                costUplift = rule.BasePrice + (rule.BasePrice * (rule.CostUplift / 100));
            }
            else
            {
                costUplift = rule.BasePrice + rule.CostUplift;
            }

            return costUplift ?? rule.BasePrice;
        }

        private static decimal GetNudgePrice(int competitorsToBeat, List<decimal> competitors, GetProductPriceRules_Result rule)
        {
            decimal nudgePrice = 0;

            if (competitorsToBeat > 0)
            {
                decimal competitorsLowestPrice = competitors.Take(competitorsToBeat).Select(x => x).Last();
                nudgePrice = competitorsLowestPrice - (competitorsLowestPrice * rule.Nudge);
            }
            else
            {
                // Log Warning about no competitors
            }

            return nudgePrice;
        }

        private static decimal CalculateMargin(decimal price, decimal margin)
        {
            decimal calculatedMargin = 0;

            calculatedMargin = price / (1 - margin);

            return calculatedMargin;
        }

        private static decimal SetCheapestCompPrice(FinalPriceComponents components, List<decimal> compsList)
        {
            if (compsList.Count() > 0)
            {
                components.CheapestCompetitorPrice = compsList.Last();
            }
            else
            {
                components.CheapestCompetitorPrice = 0;
            }

            return components.CheapestCompetitorPrice;
        }

        private static void SetFinalPrice(FinalPriceComponents components, PriceRuleDetail priceRule)
        {
            switch (priceRule.Rule.MethodName)
            {
                case "Cost Base":

                    DoCostBase(components);
                    break;

                case "Related Product Base":

                    DoRelatedProductBase(components, priceRule);
                    break;

                case "Fixed Price":

                    DoFixedPrice(components, priceRule);
                    break;

                default:

                    DoCostBase(components);
                    break;
            }

            priceRule.Product.Price = components.FinalPrice;
        }

        private static void DoFixedPrice(FinalPriceComponents components, PriceRuleDetail priceRule)
        {
            components.FinalPrice = (decimal)priceRule.Rule.FixedPriceOverride;
            components.CalculationOutcome = _calculationOutcome.Find(x => x.LookupName == "Fixed Price").LookupID;
        }

        private static void DoRelatedProductBase(FinalPriceComponents components, PriceRuleDetail priceRule)
        {
            components.FinalPrice = CalculatePercentDiscount(priceRule.Rule.BasePrice, priceRule.Rule.CompatDiscount ?? 0);
            components.CalculationOutcome = _calculationOutcome.Find(x => x.LookupName == "Relational Discount").LookupID;
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

        private void SetAltPrices(PriceRuleDetail priceRule)
        {
            priceRule.Product.AltPrice1 = CalculateAltPrice(priceRule.Product, priceRule.Rule.AltPriceAdj1);
            priceRule.Product.AltPrice2 = CalculateAltPrice(priceRule.Product, priceRule.Rule.AltPriceAdj2);
            priceRule.Product.AltPrice3 = CalculateAltPrice(priceRule.Product, priceRule.Rule.AltPriceAdj3);
            priceRule.Product.AltPrice4 = CalculateAltPrice(priceRule.Product, priceRule.Rule.AltPriceAdj4);
            priceRule.Product.AltPrice5 = CalculateAltPrice(priceRule.Product, priceRule.Rule.AltPriceAdj5);
            priceRule.Product.AltPrice6 = CalculateAltPrice(priceRule.Product, priceRule.Rule.AltPriceAdj6);
            priceRule.Product.AltPrice7 = CalculateAltPrice(priceRule.Product, priceRule.Rule.AltPriceAdj7);
            priceRule.Product.AltPrice8 = CalculateAltPrice(priceRule.Product, priceRule.Rule.AltPriceAdj8);
            priceRule.Product.AltPrice9 = CalculateAltPrice(priceRule.Product, priceRule.Rule.AltPriceAdj9);
            priceRule.Product.AltPrice10 = CalculateAltPrice(priceRule.Product, priceRule.Rule.AltPriceAdj10);
        }

        private decimal CalculateAltPrice(ProductInventory product, decimal altPriceAdj)
        {
            decimal finalAltPrice = 0;

            if (altPriceAdj > 0)
            {
                finalAltPrice = CalculatePercentDiscount(product.Price, altPriceAdj);
            }

            return finalAltPrice;
        }

        private static decimal CalculatePercentDiscount(decimal price, decimal altPriceAdj)
        {
            decimal discountedPrice = 0;

            if (price > 0)
            {
                discountedPrice = Math.Round(price - (price * altPriceAdj), 2);
            }

            return discountedPrice;
        }

        private void UpdatePrices(List<PriceRuleDetail> rules)
        {
            MergeToFinalList(rules);

            if (!_testMode)
            {
                CommonDataFunctions.CreateLogEntry(_channel, "START UpdatePrices", "Information");

                SQL.SQLBulkInsert(SetupProductDataTable(rules), "DP001");

                List<SqlParameter> sqlParms = new List<SqlParameter>();
                SqlParameter sqlParm1 = new SqlParameter("@ChannelFK", SqlDbType.Int);
                sqlParm1.Value = _channel.ChannelID;
                sqlParms.Add(sqlParm1);
                SqlParameter sqlParm2 = new SqlParameter("@deleteUnmatched", SqlDbType.Bit);
                sqlParm2.Value = false;
                sqlParms.Add(sqlParm2);
                SQL.ExecuteStoredProcedure("DP001", "CreateUpdateProductInventory", sqlParms, _channel.ChannelID);

                CommonDataFunctions.CreateLogEntry(_channel, "END UpdatePrices", "Information");
            }
            else
            {
                InsertStagingPrices(rules);
            }
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

        private void InsertStagingPrices(List<PriceRuleDetail> rules)
        {
            var stagingList = new List<PriceStaging>();

            foreach (var rule in rules)
            {
                stagingList.Add(new PriceStaging()
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
                    CheapestCostPrice = rule.Rule.BasePrice,
                    CheapestCompetitorPrice = rule.Components.CheapestCompetitorPrice,
                    GrossMarginPercent = (Math.Round(((rule.Product.Price - rule.Rule.BasePrice) / rule.Product.Price) * 100, 2)) / 100,
                    GrossMarginValue = Math.Round((rule.Product.Price - rule.Rule.BasePrice), 2),
                    CompetitorDifference = Math.Round(Math.Abs(rule.Components.CheapestCompetitorPrice - rule.Product.Price), 2),
                    CurrentPriceDifference = Math.Round(Math.Abs(rule.Product.Price - rule.Components.PreviousPrice), 2)
                });
            }

            var crudPriceStaging = new CrudPriceStaging();
            crudPriceStaging.Create(stagingList);
        }

        private static void MergeToFinalList(List<PriceRuleDetail> rules)
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

        private static DataTable SetupProductDataTable(List<PriceRuleDetail> rules)
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
                newRow["LnkdBrandFK"] = rule.Product.LnkdBrandFK == null ? (object)DBNull.Value : rule.Product.LnkdBrandFK;
                newRow["LnkdManufacturerPartNo"] = rule.Product.LnkdManufacturerPartNo == null ? (object)DBNull.Value : rule.Product.LnkdManufacturerPartNo;
                newRow["ProductCategoryFK"] = rule.Product.ProductCategoryFK;
                newRow["Price"] = rule.Product.Price;
                newRow["AltPrice1"] = rule.Product.AltPrice1;
                newRow["AltPrice2"] = rule.Product.AltPrice2;
                newRow["AltPrice3"] = rule.Product.AltPrice3;
                newRow["AltPrice4"] = rule.Product.AltPrice4;
                newRow["AltPrice5"] = rule.Product.AltPrice5;
                newRow["AltPrice6"] = rule.Product.AltPrice6;
                newRow["AltPrice7"] = rule.Product.AltPrice7;
                newRow["AltPrice8"] = rule.Product.AltPrice8;
                newRow["AltPrice9"] = rule.Product.AltPrice9;
                newRow["AltPrice10"] = rule.Product.AltPrice10;
                newRow["PriceRuleFK"] = rule.Rule.PriceRuleFK;
                newRow["CalculationOutcome"] = rule.Components.CalculationOutcome;
                newRow["BeatRateNumber"] = rule.Components.BeatRateNumber;
                newRow["StockQuantity"] = rule.Rule.StockQuantity;
                newRow["CheapestCostPrice"] = rule.Rule.BasePrice;
                newRow["CheapestCompetitorPrice"] = rule.Components.CheapestCompetitorPrice;
                newRow["GrossMarginPercent"] = Math.Round(((rule.Product.Price - GetCostUplift(rule.Rule)) / rule.Product.Price) * 100, 2);
                newRow["GrossMarginValue"] = Math.Round((rule.Product.Price - rule.Rule.BasePrice), 2);
                newRow["CompetitorDifference"] = Math.Round(Math.Abs(rule.Components.CheapestCompetitorPrice - rule.Product.Price), 2);
                newRow["MaximumMargin"] = Math.Round(rule.Components.MaxMargin, 2);
                newRow["MinimumMargin"] = Math.Round(rule.Components.MinMargin, 2);
                newRow["DesiredMargin"] = Math.Round(rule.Components.DesiredMargin, 2);
                newRow["IsKeyLine"] = rule.Product.IsKeyLine;

                dt.Rows.Add(newRow);
            }

            return dt;
        }

        private void InitializeTenant()
        {
            _tenant = new Tenant();
            _channel = _tenant.GetChannelRecord(Convert.ToInt32(_suppliedParams["channelid"]));
            _tenant.SetupTenantDelegates(_channel);

            _testMode = false;
            if (_suppliedParams["debug"] == "1")
            {
                _testMode = true;
            }
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
                if (!string.IsNullOrEmpty(channel.OutputFileEmailAddress))
                {
                    CommonDataFunctions.CreateLogEntry(channel, "START OutputPricesToEmail", "Information");
                    CommonDataFunctions.CreateLogEntry(channel, "Sending Email to: " + channel.OutputFileEmailAddress, "Information");

                    var subject = "Priceology file for channel: " + channel.ChannelName;
                    var body = "Please find attached your pricing file for the channel: " + channel.ChannelName;
                    var emailTo = new List<string>()
                        {
                            channel.OutputFileEmailAddress
                        };

                    Email.SendEmail(body, subject, emailTo, "noreply@priceology.io", csv, channel.ChannelName);
                    CommonDataFunctions.CreateLogEntry(channel, "END OutputPricesToFtp", "Information");
                }
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

                    ftpDetail.FTPPath = !string.IsNullOrEmpty(ftpDetail.FTPPath) ?
                        string.Format("//{0}//", ftpDetail.FTPPath) : string.Empty;

                    string ftpFolderPath = "ftp://" + ftpDetail.FTPServer + ftpDetail.FTPPath + "/" + ftpDetail.FTPFileName;

                    var hostDetails = new Ftp.FtpHostDetails()
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
                CommonDataFunctions.CreateLogEntry(channel, "Could not output prices to FTP '" +
                    ftpDetail.FTPServer + "'. Error: " + e.Message + " Stack: " + e.StackTrace, "Notification", true);
            }
        }

        public static MemoryStream CreateInMemoryCsv(List<PriceRuleDetail> priceRules, Channel channel)
        {
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
            Csv.CsvRow firstRow = new Csv.CsvRow();
            firstRow.Add("Brand");
            firstRow.Add("ManufacturerPartNo");
            firstRow.Add("ClientProductID");
            firstRow.Add("Description");
            firstRow.Add("Category");
            firstRow.Add("Price");
            firstRow.Add("AltPrice1");
            firstRow.Add("AltPrice2");
            firstRow.Add("AltPrice3");
            firstRow.Add("AltPrice4");
            firstRow.Add("AltPrice5");
            firstRow.Add("AltPrice6");
            firstRow.Add("AltPrice7");
            firstRow.Add("AltPrice8");
            firstRow.Add("AltPrice9");
            firstRow.Add("AltPrice10");
            writer.WriteRow(firstRow);
        }

        private static void CreateCsvData(Csv.CsvFileWriter writer)
        {
            foreach (var pr in _finalRulesList)
            {
                Csv.CsvRow newRow = new Csv.CsvRow();
                newRow.Add(pr.Product.Brand.BrandName);
                newRow.Add(pr.Product.ManufacturerPartNo);
                newRow.Add(pr.Product.ClientProductID);
                newRow.Add(pr.Product.Description);
                newRow.Add(pr.Product.ProductCategory.CategoryName);
                newRow.Add(pr.Product.Price.ToString());
                newRow.Add(pr.Product.AltPrice1.ToString());
                newRow.Add(pr.Product.AltPrice2.ToString());
                newRow.Add(pr.Product.AltPrice3.ToString());
                newRow.Add(pr.Product.AltPrice4.ToString());
                newRow.Add(pr.Product.AltPrice5.ToString());
                newRow.Add(pr.Product.AltPrice6.ToString());
                newRow.Add(pr.Product.AltPrice7.ToString());
                newRow.Add(pr.Product.AltPrice8.ToString());
                newRow.Add(pr.Product.AltPrice9.ToString());
                newRow.Add(pr.Product.AltPrice10.ToString());
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

        private static string GetRowFieldData(string[] row, int columnIndex)
        {
            string fieldData = "";

            if (columnIndex != -1)
            {
                fieldData = string.IsNullOrEmpty(row[columnIndex]) ? "" : row[columnIndex];
            }

            return fieldData;
        }

        private List<Lookup> GetCalculationOutcomes()
        {
            var crud = new CrudLookup();
            return crud.Read(x => x.LookupType.LookupTypeName == "CalculationOutcome");
        }

        private void DoRounding(PriceRuleDetail priceRule)
        {
            switch (priceRule.Rule.RoundingGroup)
            {
                case "Rounding Rule 1":

                    RoundingGroups.SetGroup1Prices(priceRule.Product);

                    break;
                default:
                    break;
            }
        }

        private void UpdateProductCounts(Channel _channel)
        {
            try
            {
                CommonDataFunctions.CreateLogEntry(_channel, "START UpdateProductCounts", "Information");

                using (DP001Entities db = new DP001Entities())
                {
                    db.SetPriceRuleProductCounts(_channel.ChannelID);
                }

                CommonDataFunctions.CreateLogEntry(_channel, "END UpdateProductCounts", "Information");
            }
            catch (Exception e)
            {
                CommonDataFunctions.CreateLogEntry(_channel, "**ERROR Updating Product Counts** - " + e.Message +
                    " Stack: " + e.StackTrace, "Information");
            }
        }

        private void CleanupStagingTables()
        {
            CommonDataFunctions.CreateLogEntry(_channel, "START CleanupStaging Tables", "Information");

            List<SqlParameter> sqlParms = new List<SqlParameter>();
            SqlParameter sqlParm1 = new SqlParameter("@ChannelFK", SqlDbType.Int);
            sqlParm1.Value = _channel.ChannelID;
            sqlParms.Add(sqlParm1);
            SQL.ExecuteStoredProcedure("DP001", "DeleteStagingTableEntries", sqlParms, _channel.ChannelID);

            CommonDataFunctions.CreateLogEntry(_channel, "END CleanupStaging Tables", "Information");
        }

        private void RunCustomRoutine(
            TargetUser targetUser, 
            string targetFunction, 
            Channel channel, 
            object extras = null)
        {
            _tenant.CustomRoutines?.Invoke(targetUser, "BreakMargins", _channel, extras);
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
    }
}
