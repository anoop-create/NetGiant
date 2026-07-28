using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using netGiant.Intranet.DataLayer;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Import
{
    public class ImportPriceRuleViewModel
    {
        public ImportPriceRuleViewModel()
        {
            AllWebsites = SelectListViewModel.AllWebsites();
        }

        public IQueryable<SelectListItem> AllWebsites { get; set; }
        public string FilePath { get; set; }
        public int WebsiteFK { get; set; }
        public List<string> Warnings { get; set; }

        public void Import(string filePath)
        {
            DataTable dt = SharedFunctions.ReadTextFile(filePath);
            dt = GetAcceptedFields(dt);
            ProcessRows(dt);
        }

        private DataTable GetAcceptedFields(DataTable csvData)
        {
            DataColumnCollection columns = csvData.Columns;

            try
            {
                Dictionary<int, string> mappedColumns = new Dictionary<int, string>();
                int colIndex = 0;

                foreach (string col in PriceRuleAcceptedFields.Fields)
                {
                    if (columns.Contains(col))
                    {
                        mappedColumns.Add(colIndex, col);
                        colIndex++;
                    }
                }

                csvData = new DataView(csvData).ToTable(false, mappedColumns.Select(x => x.Value).ToArray());
            }
            catch (Exception ex)
            {
                throw new ApplicationException(ex.Message);
            }

            return csvData;
        }

        private void ProcessRows(DataTable finalDt)
        {
            int currentRow = 1;
            List<PriceRuleImportFields> validPriceRuleList = new List<PriceRuleImportFields>();

            foreach (DataRow row in finalDt.Rows)
            {
                try
                {
                    PriceRuleImportFields priceRuleFields = null;
                    priceRuleFields = ExtractPriceRule(row, currentRow);
                    ValidatePriceRule(priceRuleFields);
                    validPriceRuleList.Add(priceRuleFields);
                    currentRow++;
                }
                catch (Exception ex)
                {
                    string message = LogErrorString(currentRow, ex);
                    throw new ApplicationException(message);
                }
            }

            finalDt = null;
            SaveRecords(validPriceRuleList);
        }

        private void SaveRecords(List<PriceRuleImportFields> validPriceRuleList)
        {
            foreach (PriceRuleImportFields pr in validPriceRuleList)
            {
                if (pr.IsNew)
                {
                    CreateNewPriceRule(pr);
                }
                else
                {
                    UpdatePriceRule(pr);
                }
            }
        }

        private void UpdatePriceRule(PriceRuleImportFields pr)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                priceRule existingRule = null;

                switch (pr.RuleTypeFK)
                {
                    case 1:
                        existingRule = db.priceRule.Where(x => x.categoryCodeFK == pr.CategoryCodeFK &&
                            pr.ManufacturerFK == null && x.productFK == null).FirstOrDefault();
                        break;
                    case 2:
                        existingRule = db.priceRule.Where(x => x.categoryCodeFK == pr.CategoryCodeFK &&
                            pr.ManufacturerFK == pr.ManufacturerFK && x.productFK == null).FirstOrDefault();
                        break;
                    case 3:
                        existingRule = db.priceRule.Where(x => x.productFK == pr.ProductFK &&
                            x.categoryCodeFK == pr.CategoryCodeFK).FirstOrDefault();
                        break;
                    default:
                        break;
                }

                existingRule.breakPrice1 = pr.BreakPrice1;
                existingRule.breakPrice2 = pr.BreakPrice2;
                existingRule.breakPrice3 = pr.BreakPrice3;
                existingRule.breakPrice4 = pr.BreakPrice4;
                existingRule.breakPrice5 = pr.BreakPrice5;
                existingRule.categoryCodeFK = pr.CategoryCodeFK;
                existingRule.compatDiscount = pr.CompatibleDiscount;
                existingRule.compatOverrideMargin = pr.CompatibleOverrideMargin;
                existingRule.compatOverrideValue = pr.CompatibleOverrideValue;
                existingRule.competitorsToBeat = pr.CompetitorsToBeat;
                existingRule.costUpliftIsPercent = pr.CostUpliftIsPercent;
                existingRule.costUplift = pr.CostUplift;
                existingRule.description = pr.Description;
                existingRule.desiredMargin = pr.DesiredMargin;
                existingRule.fixedPriceOverride = pr.FixedPriceOverride;
                existingRule.manufacturerFK = pr.ManufacturerFK;
                existingRule.maxMargin = pr.MaxMargin;
                existingRule.maxMarginValue = pr.MaxMarginValue;
                existingRule.minMargin = pr.MinMargin;
                existingRule.minMarginValue = pr.MinMarginValue;
                existingRule.nudge = pr.Nudge;
                existingRule.packDiscount = pr.PackDiscount;
                existingRule.productFK = pr.ProductFK;
                existingRule.productGroupFK = pr.ProductGroupFK;
                existingRule.ruleType = pr.RuleTypeFK;
                existingRule.useBanding = pr.UseBanding;

                db.Entry(existingRule).State = EntityState.Modified;
                db.SaveChanges();
            }
        }

        private void CreateNewPriceRule(PriceRuleImportFields pr)
        {
            try
            {
                priceRule newRule = new priceRule();
                newRule.breakPrice1 = pr.BreakPrice1;
                newRule.breakPrice2 = pr.BreakPrice2;
                newRule.breakPrice3 = pr.BreakPrice3;
                newRule.breakPrice4 = pr.BreakPrice4;
                newRule.breakPrice5 = pr.BreakPrice5;
                newRule.categoryCodeFK = pr.CategoryCodeFK;
                newRule.compatDiscount = pr.CompatibleDiscount;
                newRule.compatOverrideMargin = pr.CompatibleOverrideMargin;
                newRule.compatOverrideValue = pr.CompatibleOverrideValue;
                newRule.competitorsToBeat = pr.CompetitorsToBeat;
                newRule.costUpliftIsPercent = pr.CostUpliftIsPercent;
                newRule.costUplift = pr.CostUplift;
                newRule.description = pr.Description;
                newRule.desiredMargin = pr.DesiredMargin;
                newRule.fixedPriceOverride = pr.FixedPriceOverride;
                newRule.manufacturerFK = pr.ManufacturerFK;
                newRule.maxMargin = pr.MaxMargin;
                newRule.maxMarginValue = pr.MaxMarginValue;
                newRule.minMargin = pr.MinMargin;
                newRule.minMarginValue = pr.MinMarginValue;
                newRule.nudge = pr.Nudge;
                newRule.packDiscount = pr.PackDiscount;
                newRule.productFK = pr.ProductFK;
                newRule.productGroupFK = pr.ProductGroupFK;
                newRule.ruleType = pr.RuleTypeFK;
                newRule.useBanding = pr.UseBanding;

                using (ngmdEntities db = new ngmdEntities())
                {
                    db.Entry(newRule).State = EntityState.Added;
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Could not save new price rule - " + ex.Message, ex.InnerException);
            }
        }

        private string LogErrorString(int currentRow, Exception ex)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("Error importing row " + currentRow + ".");
            sb.Append(" Error Message - " + ex.Message + ".");
            sb.Append(" File Processing Ended Due to Errors in the File.");
            sb.Append(" Re-Upload a Valid File.");

            return sb.ToString();
        }

        private void ValidatePriceRule(PriceRuleImportFields priceRuleFields)
        {
            if ((priceRuleFields.DesiredMargin > 0) && (priceRuleFields.DesiredMargin < priceRuleFields.MinMargin))
                throw new ApplicationException("Invalid Rule - Desired Margin is less than Min Margin.");
            if ((priceRuleFields.DesiredMargin > 0) && (priceRuleFields.DesiredMargin > priceRuleFields.MaxMargin))
                throw new ApplicationException("Invalid Rule - Desired Margin is greater than Max Margin.");
        }

        private PriceRuleImportFields ExtractPriceRule(DataRow row, int csvRow)
        {
            PriceRuleImportFields fields = new PriceRuleImportFields();

            ExtractDescription(row, fields);
            ExtractCategory(row, fields);
            ExtractManufacturer(row, fields);
            ExtractPartNo(row, fields);
            ExtractBanding(row, fields);
            ExtractCostUplift(row, fields);
            ExtractMargins(row, fields);
            ExtractCompetitorInfo(row, fields);
            ExtractProductGroup(row, fields);
            ExtractBreakPrices(row, fields);
            ExtractCompatibleFields(row, fields);
            ExtractPackDiscount(row, fields);
            ExtractFixedPriceOverride(row, fields);
            DetermineRuleType(row, fields);
            CheckPriceRuleIsNew(fields);

            return fields;
        }

        private void CheckPriceRuleIsNew(PriceRuleImportFields fields)
        {
            fields.IsNew = true;

            using (ngmdEntities db = new ngmdEntities())
            {
                if (fields.RuleTypeFK == 1)
                {
                    priceRule pr = db.priceRule.Where(x => x.categoryCodeFK == fields.CategoryCodeFK).FirstOrDefault();
                    if (pr != null)
                        fields.IsNew = false;
                }

                if (fields.RuleTypeFK == 2)
                {
                    priceRule pr = db.priceRule.Where(x => x.categoryCodeFK == fields.CategoryCodeFK &&
                        x.manufacturerFK == fields.ManufacturerFK).FirstOrDefault();
                    if (pr != null)
                        fields.IsNew = false;
                }

                if (fields.RuleTypeFK == 3)
                {
                    priceRule pr = db.priceRule.Where(x => x.productFK == fields.ProductFK &&
                        x.categoryCodeFK == fields.CategoryCodeFK).FirstOrDefault();
                    if (pr != null)
                        fields.IsNew = false;
                }
            }
        }

        private void DetermineRuleType(DataRow row, PriceRuleImportFields fields)
        {
            if (fields.ManufacturerFK == null && fields.ProductFK == null)
                fields.RuleTypeFK = 1;
            if (fields.ProductFK == null && fields.ManufacturerFK != null)
                fields.RuleTypeFK = 2;
            if (fields.ProductFK != null)
                fields.RuleTypeFK = 3;
        }

        private void ExtractFixedPriceOverride(DataRow row, PriceRuleImportFields fields)
        {
            string fixedPrice = DataTableColExists(row, "Fixed Price Override") == true ? row["Fixed Price Override"].ToString() : null;

            if (fixedPrice != null)
            {
                decimal dFixedPrice = 0;
                bool validFixedPrice = decimal.TryParse(fixedPrice, out dFixedPrice);

                if (validFixedPrice)
                {
                    fields.FixedPriceOverride = dFixedPrice;
                }
                else
                {
                    throw new ApplicationException("Invalid Fixed Price Specified.");
                }
            }

        }

        private void ExtractPackDiscount(DataRow row, PriceRuleImportFields fields)
        {
            string packDiscount = DataTableColExists(row, "Pack Discount") == true ? row["Pack Discount"].ToString() : null;

            if (packDiscount != null)
            {
                decimal dPackDiscount = 0;
                bool validPackDiscount = decimal.TryParse(packDiscount, out dPackDiscount);

                if (validPackDiscount)
                {
                    fields.PackDiscount = dPackDiscount / 100;
                }
                else
                {
                    throw new ApplicationException("Invalid Pack Discount Specified.");
                }
            }

        }

        private void ExtractCompatibleFields(DataRow row, PriceRuleImportFields fields)
        {
            string compatDiscount = DataTableColExists(row, "Compatible Discount") == true ? row["Compatible Discount"].ToString() : null;
            string compatOverrideValue = DataTableColExists(row, "Compatible Override Value") == true ? row["Compatible Override Value"].ToString() : null;
            string compatOverrideMargin = DataTableColExists(row, "Compatible Override Margin") == true ? row["Compatible Override Margin"].ToString() : null;
            
            if (compatDiscount != null)
            {
                decimal dCompatDiscount = 0;
                bool validCompatDiscount = decimal.TryParse(compatDiscount, out dCompatDiscount);

                if (validCompatDiscount)
                {
                    fields.CompatibleDiscount = dCompatDiscount / 100;
                }
                else
                {
                    throw new ApplicationException("Invalid Compatible Discount Specified.");
                }
            }

            if (compatOverrideValue != null)
            {
                decimal dCompatOverrideValue = 0;
                bool validCompatOverrideValue = decimal.TryParse(compatOverrideValue, out dCompatOverrideValue);

                if (validCompatOverrideValue)
                {
                    fields.CompatibleOverrideValue = dCompatOverrideValue;
                }
                else
                {
                    throw new ApplicationException("Invalid Compatible Override Value Specified.");
                }
            }

            if (compatOverrideMargin != null)
            {
                decimal dCompatOverrideMargin = 0;
                bool validCompatOverrideMargin = decimal.TryParse(compatOverrideMargin, out dCompatOverrideMargin);

                if (validCompatOverrideMargin)
                {
                    fields.CompatibleOverrideMargin = dCompatOverrideMargin / 100;
                }
                else
                {
                    throw new ApplicationException("Invalid Compatible Override Margin Specified.");
                }
            }

        }

        private void ExtractBreakPrices(DataRow row, PriceRuleImportFields fields)
        {
            string break1 = DataTableColExists(row, "Break1") == true ? row["Break1"].ToString() : null;
            string break2 = DataTableColExists(row, "Break2") == true ? row["Break2"].ToString() : null;
            string break3 = DataTableColExists(row, "Break3") == true ? row["Break3"].ToString() : null;
            string break4 = DataTableColExists(row, "Break4") == true ? row["Break4"].ToString() : null;
            string break5 = DataTableColExists(row, "Break5") == true ? row["Break5"].ToString() : null;

            if (break1 != null)
            {
                decimal dBreak1 = 0;
                bool validBreak1 = decimal.TryParse(break1, out dBreak1);

                if (validBreak1)
                {
                    fields.BreakPrice1 = dBreak1 / 100;
                }
                else
                {
                    throw new ApplicationException("Invalid Break 1 Specified.");
                }
            }

            if (break2 != null)
            {
                decimal dBreak2 = 0;
                bool validBreak2 = decimal.TryParse(break2, out dBreak2);

                if (validBreak2)
                {
                    fields.BreakPrice2 = dBreak2 / 100;
                }
                else
                {
                    throw new ApplicationException("Invalid Break 2 Specified.");
                }
            }

            if (break1 != null)
            {
                decimal dBreak3 = 0;
                bool validBreak3 = decimal.TryParse(break3, out dBreak3);

                if (validBreak3)
                {
                    fields.BreakPrice3 = dBreak3 / 100;
                }
                else
                {
                    throw new ApplicationException("Invalid Break 3 Specified.");
                }
            }

            if (break1 != null)
            {
                decimal dBreak4 = 0;
                bool validBreak4 = decimal.TryParse(break4, out dBreak4);

                if (validBreak4)
                {
                    fields.BreakPrice4 = dBreak4 / 100;
                }
                else
                {
                    throw new ApplicationException("Invalid Break 4 Specified.");
                }
            }

            if (break1 != null)
            {
                decimal dBreak5 = 0;
                bool validBreak5 = decimal.TryParse(break5, out dBreak5);

                if (validBreak5)
                {
                    fields.BreakPrice5 = dBreak5 / 100;
                }
                else
                {
                    throw new ApplicationException("Invalid Break 5 Specified.");
                }
            }

        }

        private void ExtractProductGroup(DataRow row, PriceRuleImportFields fields)
        {
            string grp = DataTableColExists(row, "Product Group") == true ? row["Product Group"].ToString() : null;

            if (grp != null)
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    productGroup pg = db.productGroup.Where(x => x.productGroupName.Trim().ToLower() ==
                        grp.Trim().ToLower()).FirstOrDefault();

                    if (pg != null)
                    {
                        fields.ProductGroupFK = pg.productGroupID;
                    }
                    else
                    {
                        throw new ApplicationException("Invalid Product Group Specified.");
                    }
                }
            }

        }

        private void ExtractCompetitorInfo(DataRow row, PriceRuleImportFields fields)
        {
            string compsToBeat = DataTableColExists(row, "Competitors To Beat") == true ? row["Competitors To Beat"].ToString() : null;
            string nudge = DataTableColExists(row, "Nudge") == true ? row["Nudge"].ToString() : null;

            if (compsToBeat != null)
            {
                decimal dCompsToBeat = 0;
                bool validCompsToBeat = decimal.TryParse(compsToBeat, out dCompsToBeat);

                if (validCompsToBeat)
                {
                    fields.CompetitorsToBeat = dCompsToBeat / 100;
                }
                else
                {
                    throw new ApplicationException("Invalid Competitors to Beat Specified.");
                }
            }

            if (nudge != null)
            {
                decimal dNudge = 0;
                bool validNudge = decimal.TryParse(nudge, out dNudge);

                if (validNudge)
                {
                    fields.Nudge = dNudge / 100;
                }
                else
                {
                    throw new ApplicationException("Invalid Nudge Specified.");
                }
            }

        }

        private void ExtractMargins(DataRow row, PriceRuleImportFields fields)
        {
            string desiredMargin = DataTableColExists(row, "Desired Margin") == true ? row["Desired Margin"].ToString() : null;
            string minMargin = DataTableColExists(row, "Min Margin") == true ? row["Min Margin"].ToString() : null;
            string maxMargin = DataTableColExists(row, "Max Margin") == true ? row["Max Margin"].ToString() : null;
            string minMarginValue = DataTableColExists(row, "Min Margin Value") == true ? row["Min Margin Value"].ToString() : null;
            string maxMarginValue = DataTableColExists(row, "Max Margin Value") == true ? row["Max Margin Value"].ToString() : null;

            using (ngmdEntities db = new ngmdEntities())
            {
                if (desiredMargin != null)
                {
                    decimal dDesiredMargin = 0;
                    bool validDesiredMargin = decimal.TryParse(desiredMargin, out dDesiredMargin);

                    if (validDesiredMargin)
                    {
                        fields.DesiredMargin = dDesiredMargin / 100;
                    }
                    else
                    {
                        throw new ApplicationException("Invalid Desired Margin specified.");
                    }
                }

                if (minMargin != null)
                {
                    decimal dMinMargin = 0;
                    bool validMinMargin = decimal.TryParse(minMargin, out dMinMargin);

                    if (validMinMargin)
                    {
                        fields.MinMargin = dMinMargin / 100;
                    }
                    else
                    {
                        throw new ApplicationException("Invalid Min Margin specified.");
                    }
                }

                if (maxMargin != null)
                {
                    decimal dMaxMargin = 0;
                    bool validMaxMargin = decimal.TryParse(maxMargin, out dMaxMargin);

                    if (validMaxMargin)
                    {
                        fields.MaxMargin = dMaxMargin / 100;
                    }
                    else
                    {
                        throw new ApplicationException("Invalid Max Margin specified.");
                    }
                }

                if (minMarginValue != null)
                {
                    decimal dMinMarginValue = 0;
                    bool validMinMarginValue = decimal.TryParse(minMarginValue, out dMinMarginValue);

                    if (validMinMarginValue)
                    {
                        fields.MinMarginValue = dMinMarginValue;
                    }
                    else
                    {
                        throw new ApplicationException("Invalid Min Margin Value specified.");
                    }
                }

                if (maxMarginValue != null)
                {
                    decimal dMaxMarginValue = 0;
                    bool validMaxMarginValue = decimal.TryParse(maxMarginValue, out dMaxMarginValue);

                    if (validMaxMarginValue)
                    {
                        fields.MaxMarginValue = dMaxMarginValue;
                    }
                    else
                    {
                        throw new ApplicationException("Invalid Max Margin Value specified.");
                    }
                }
            }
        }

        private void ExtractCostUplift(DataRow row, PriceRuleImportFields fields)
        {
            string costUpliftIsPercent = DataTableColExists(row, "Cost Uplift is Percent") == true ? 
                row["Cost Uplift is Percent"].ToString() : null;

            string costUplift = DataTableColExists(row, "Cost Uplift") == true ?
                row["Cost Uplift"].ToString() : null;

            if (costUpliftIsPercent != null)
            {
                fields.CostUpliftIsPercent = costUpliftIsPercent.ToLower() == "y" ? true : false;
            }

            if (costUplift != null)
            {
                decimal dCostUplift = 0;
                bool validCostUplift = decimal.TryParse(costUplift, out dCostUplift);

                if (validCostUplift)
                {
                    switch (fields.CostUpliftIsPercent)
                    {
                        case true:
                            fields.CostUplift = dCostUplift / 100;
                            break;
                        case false:
                            fields.CostUplift = dCostUplift;
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    throw new ApplicationException("Invalid Cost Uplift Value");
                }
            }

        }

        private void ExtractBanding(DataRow row, PriceRuleImportFields fields)
        {
            string banding = DataTableColExists(row, "Banding") == true ? row["Banding"].ToString() : null;

            if (banding != null)
            {
                fields.UseBanding = banding.ToLower() == "y" ? true : false;
            }
        }

        private void ExtractPartNo(DataRow row, PriceRuleImportFields fields)
        {
            string partNo = DataTableColExists(row, "Alt Ref") == true ? row["Alt Ref"].ToString() : null;

            if (partNo != null)
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    product prd = db.product.Where(x => x.partNo == partNo).FirstOrDefault();

                    if (prd != null)
                    {
                        fields.ProductFK = prd.productID;
                    }
                }
            }
        }

        private void ExtractManufacturer(DataRow row, PriceRuleImportFields fields)
        {
            string manufacturer = DataTableColExists(row, "Manufacturer") == true ? row["Manufacturer"].ToString() : null;

            if (manufacturer != null)
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    manufacturer man = db.manufacturer.Where(x => x.manufacturerName.Trim().ToLower() ==
                        manufacturer.Trim().ToLower()).FirstOrDefault();

                    if (man != null)
                    {
                        fields.ManufacturerFK = man.manufacturerID;
                    }
                    else
                    {
                        throw new ApplicationException("Invalid Manufacturer specified.");
                    }
                }
            }
        }

        private void ExtractCategory(DataRow row, PriceRuleImportFields fields)
        {
            string category = DataTableColExists(row, "Category") == true ? row["Category"].ToString() : null;

            if (category != null)
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    categoryCode cc = db.categoryCode.Where(x => x.categoryCodeName.Trim().ToLower() ==
                        category.Trim().ToLower() && x.websiteFK == WebsiteFK).FirstOrDefault();

                    if (cc != null)
                    {
                        fields.CategoryCodeFK = cc.categoryCodeID;
                    }
                    else
                    {
                        throw new ApplicationException("Invalid Category specified.");
                    }
                }
            }
            else
            {
                throw new ApplicationException("No Category specified.");
            }

        }

        private static void ExtractDescription(DataRow row, PriceRuleImportFields fields)
        {
            fields.Description = DataTableColExists(row, "Description") == true ? row["Description"].ToString() : null;
        }

        public static bool DataTableColExists(DataRow dr, string colName)
        {
            return dr.Table.Columns.Contains(colName);
        }

        private static bool? SetBoolean(string value)
        {
            bool? returnValue = null;

            if (value != null)
            {
                switch (value.ToLower())
                {
                    case "y":
                        returnValue = true;
                        break;
                    case "n":
                        returnValue = false;
                        break;
                    default:
                        returnValue = null;
                        break;
                }
            }

            return returnValue;
        }
    }
}
