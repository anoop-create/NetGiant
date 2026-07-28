using NGBP.DataAccessLayer.DataUtilities;
using NGBP.DataAccessLayer.SCOM.SimpleEntities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.EnterpriseServices;

namespace NGBP.DataAccessLayer.SCOM.Services
{
    [Serializable]
    public class PriceRuleServices
    {
 
        public List<PriceRuleSE> GetAllPriceRules(string spName)
        {
            DataTable dtPriceRules = SQLUtilities.ExecuteStoredProcedureQuery("netgiantmasterdata", spName, 3000);
            List<PriceRuleSE> prList = new List<PriceRuleSE>();

            foreach (DataRow row in dtPriceRules.Rows)
            {
                PriceRuleSE prSE = new PriceRuleSE();
                prSE.WebsiteInventoryFK = string.IsNullOrEmpty(row["websiteInventoryFK"].ToString()) ? 0 : Convert.ToInt32(row["websiteInventoryFK"]);
                prSE.ProductFK = string.IsNullOrEmpty(row["productFK"].ToString()) ? 0 : Convert.ToInt32(row["productFK"]);
                prSE.PartNo = row["partNo"].ToString();
                prSE.BasePrice = string.IsNullOrEmpty(row["basePrice"].ToString()) ? 0 : Convert.ToDecimal(row["basePrice"]);
                prSE.CostPrice = string.IsNullOrEmpty(row["costPrice"].ToString()) ? 0 : Convert.ToDecimal(row["costPrice"]);
                prSE.PriceRuleID = string.IsNullOrEmpty(row["priceRuleID"].ToString()) ? 0 : Convert.ToInt32(row["priceRuleID"]);
                prSE.CategoryCodeFK = string.IsNullOrEmpty(row["categoryCodeFK"].ToString()) ? 0 : Convert.ToInt32(row["categoryCodeFK"]);
                prSE.description = row["description"].ToString();
                prSE.RuleTypeFK = string.IsNullOrEmpty(row["ruleTypeFK"].ToString()) ? 0 : Convert.ToInt32(row["ruleTypeFK"]);
                prSE.ManufacturerFK = string.IsNullOrEmpty(row["manufacturerFK"].ToString()) ? 0 : Convert.ToInt32(row["manufacturerFK"]);
                prSE.UseBanding = string.IsNullOrEmpty(row["useBanding"].ToString()) ? false : Convert.ToBoolean(row["useBanding"]);
                prSE.CostUplift = string.IsNullOrEmpty(row["costUplift"].ToString()) ? 0 : Convert.ToDecimal(row["costUplift"]);
                prSE.CostUpliftIsPercent = string.IsNullOrEmpty(row["costUpliftIsPercent"].ToString()) ? false : Convert.ToBoolean(row["costUpliftIsPercent"]);
                prSE.DesiredMargin = string.IsNullOrEmpty(row["desiredMargin"].ToString()) ? 0 : Convert.ToDecimal(row["desiredMargin"]);
                prSE.MinMarginPercent = string.IsNullOrEmpty(row["minMargin"].ToString()) ? 0 : Convert.ToDecimal(row["minMargin"]);
                prSE.MaxMarginPercent = string.IsNullOrEmpty(row["maxMargin"].ToString()) ? 0 : Convert.ToDecimal(row["maxMargin"]);
                prSE.MinMarginValue = string.IsNullOrEmpty(row["minMarginValue"].ToString()) ? 0 : Convert.ToDecimal(row["minMarginValue"]);
                prSE.MaxMarginValue = string.IsNullOrEmpty(row["maxMarginValue"].ToString()) ? 0 : Convert.ToDecimal(row["maxMarginValue"]);
                prSE.CompetitorsToBeat = string.IsNullOrEmpty(row["competitorsToBeat"].ToString()) ? 0 : Convert.ToDecimal(row["competitorsToBeat"]);
                prSE.Nudge = string.IsNullOrEmpty(row["nudge"].ToString()) ? 0 : Convert.ToDecimal(row["nudge"]);
                prSE.CompPrices = row["compPrices"].ToString();
                prSE.BreakPrice1 = string.IsNullOrEmpty(row["breakPrice1"].ToString()) ? 0 : Convert.ToDecimal(row["breakPrice1"]);
                prSE.BreakPrice2 = string.IsNullOrEmpty(row["breakPrice2"].ToString()) ? 0 : Convert.ToDecimal(row["breakPrice2"]);
                prSE.BreakPrice3 = string.IsNullOrEmpty(row["breakPrice3"].ToString()) ? 0 : Convert.ToDecimal(row["breakPrice3"]);
                prSE.BreakPrice4 = string.IsNullOrEmpty(row["breakPrice4"].ToString()) ? 0 : Convert.ToDecimal(row["breakPrice4"]);
                prSE.BreakPrice5 = string.IsNullOrEmpty(row["breakPrice5"].ToString()) ? 0 : Convert.ToDecimal(row["breakPrice5"]);
                prSE.PackDiscount = string.IsNullOrEmpty(row["packDiscount"].ToString()) ? 0 : Convert.ToDecimal(row["packDiscount"]);
                prSE.CompatDiscount = string.IsNullOrEmpty(row["compatDiscount"].ToString()) ? 0 : Convert.ToDecimal(row["compatDiscount"]);
                prSE.CompatOverrideMargin = string.IsNullOrEmpty(row["compatOverrideMargin"].ToString()) ? 0 : Convert.ToDecimal(row["compatOverrideMargin"]);
                prSE.CompatOverrideValue = string.IsNullOrEmpty(row["compatOverrideValue"].ToString()) ? 0 : Convert.ToDecimal(row["compatOverrideValue"]);
                prSE.SalesYearToDate = string.IsNullOrEmpty(row["salesYearToDate"].ToString()) ? 0 : Convert.ToInt32(row["salesYearToDate"]);
                prSE.FixedPriceOverride = string.IsNullOrEmpty(row["fixedPriceOverride"].ToString()) ? 0 : Convert.ToDecimal(row["fixedPriceOverride"]);
                prSE.FinalBreakMinimumMarginStock = string.IsNullOrEmpty(row["finalBreakMinimumMarginStock"].ToString()) ? 0 : Convert.ToDecimal(row["finalBreakMinimumMarginStock"]);
                prSE.FinalBreakMinimumMarginAssemblies = string.IsNullOrEmpty(row["finalBreakMinimumMarginAssemblies"].ToString()) ? 0 : Convert.ToDecimal(row["finalBreakMinimumMarginAssemblies"]);

                prList.Add(prSE);
            } 

            return prList;
        }

        [AutoComplete]
        public static void UpdateNewPrices(string csvFilePath)
        {
            string connectionString = SQLUtilities.GetMachineConnectionString("netgiantmasterdata");
            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "ngmd.InsertProductPricesNEW";
                cmd.CommandTimeout = 1000;

                cmd.Parameters.Add(new SqlParameter(
                    "@pricesCSV", SqlDbType.VarChar, 8000, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Current, csvFilePath));

                if (conn.State == ConnectionState.Closed) conn.Open();

                try
                {
                    cmd.ExecuteNonQuery();
                    conn.Close();
                }

                catch (Exception ex)
                {
                    throw new ApplicationException(ex.Message);
                }
            }
        }

    }
}
