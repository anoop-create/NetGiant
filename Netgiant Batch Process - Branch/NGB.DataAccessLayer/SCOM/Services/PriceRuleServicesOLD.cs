using NGBP.DataAccessLayer.DataUtilities;
using NGBP.DataAccessLayer.SCOM.SimpleEntities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.EnterpriseServices;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGBP.DataAccessLayer.SCOM.Services
{
    [Serializable]
    public class PriceRuleServicesOLD
    {

        public List<PriceRuleSEOLD> GetAllPriceRules()
        {
            DataTable dtPriceRules = SQLUtilities.ExecuteStoredProcedureQuery("netgiantmasterdata", "ngmd.GetPricingRules", 3000);
            List<PriceRuleSEOLD> prList = new List<PriceRuleSEOLD>();

            foreach (DataRow row in dtPriceRules.Rows)
            {
                PriceRuleSEOLD prSE = new PriceRuleSEOLD();
                prSE.WebsiteInventoryFK = Convert.ToInt32(row["websiteInventoryFK"]);
                prSE.ProductFK = Convert.ToInt32(row["productFK"]);
                prSE.PartNo = row["partNo"].ToString();
                prSE.CostPrice = Convert.ToDecimal(row["costPrice"]);
                prSE.PriceRuleID = Convert.ToInt32(row["priceRuleID"]);
                prSE.CategoryCodeFK = Convert.ToInt32(row["categoryCodeFK"]);
                prSE.description = row["description"].ToString();
                prSE.RuleTypeFK = Convert.ToInt32(row["ruleTypeFK"]);
                prSE.ManufacturerFK = Convert.ToInt32(row["manufacturerFK"]);
                prSE.UseBanding = Convert.ToBoolean(row["useBanding"]);
                prSE.CostUplift = Convert.ToDecimal(row["costUplift"]);
                prSE.CostUpliftIsPercent = Convert.ToBoolean(row["costUpliftIsPercent"]);
                prSE.DesiredMargin = Convert.ToDecimal(row["desiredMargin"]);
                prSE.MinMargin = Convert.ToDecimal(row["minMargin"]);
                prSE.MaxMargin = Convert.ToDecimal(row["maxMargin"]);
                prSE.CompetitorsToBeat = Convert.ToDecimal(row["competitorsToBeat"]);
                prSE.Nudge = Convert.ToDecimal(row["nudge"]);
                prSE.CompPrices = row["compPrices"].ToString();

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
                cmd.CommandText = "ngmd.InsertProductPrices";
                cmd.CommandTimeout = 1000;

                cmd.Parameters.Add(new SqlParameter(
                    "@pricesCSV", SqlDbType.VarChar, 8000, ParameterDirection.Input, false, 0, 0, "", DataRowVersion.Current, csvFilePath));

                if (conn.State == ConnectionState.Closed) conn.Open();

                try
                {
                    cmd.ExecuteNonQuery();
                    conn.Close();
                }

                catch (Exception e)
                {
                    throw new ApplicationException(e.Message);
                }
            }
        }

    }
}
