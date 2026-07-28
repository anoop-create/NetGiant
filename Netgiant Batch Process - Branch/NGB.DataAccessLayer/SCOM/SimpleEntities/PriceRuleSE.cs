using System;

namespace NGBP.DataAccessLayer.SCOM.SimpleEntities
{
    [Serializable]
    public class PriceRuleSE
    {
        public int WebsiteInventoryFK { get; set; }
        public int ProductFK { get; set; }
        public string PartNo { get; set; }
        public decimal BasePrice { get; set; }
        public decimal CostPrice { get; set; }
        public int PriceRuleID { get; set; }
        public int CategoryCodeFK { get; set; }
        public string description { get; set; }
        public int RuleTypeFK { get; set; }
        public int ManufacturerFK { get; set; }
        public bool UseBanding { get; set; }
        public decimal CostUplift { get; set; }
        public bool CostUpliftIsPercent { get; set; }
        public decimal DesiredMargin { get; set; }
        public decimal MinMarginPercent { get; set; }
        public decimal MaxMarginPercent { get; set; }
        public decimal MinMarginValue { get; set; }
        public decimal MaxMarginValue { get; set; }
        public decimal CompetitorsToBeat { get; set; }
        public decimal Nudge { get; set; }
        public string CompPrices { get; set; }
        public decimal BreakPrice1 { get; set; }
        public decimal BreakPrice2 { get; set; }
        public decimal BreakPrice3 { get; set; }
        public decimal BreakPrice4 { get; set; }
        public decimal BreakPrice5 { get; set; }
        public decimal PackDiscount { get; set; }
        public decimal CompatDiscount { get; set; }
        public decimal CompatOverrideMargin { get; set; }
        public decimal CompatOverrideValue { get; set; }
        public int SalesYearToDate { get; set; }
        public decimal? FixedPriceOverride { get; set; }
        public decimal FinalBreakMinimumMarginStock { get; set; }
        public decimal FinalBreakMinimumMarginAssemblies { get; set; }
    }
}
