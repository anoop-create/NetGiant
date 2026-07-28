namespace ngBatchProcesses.BusinessObjects.PricingEngine
{
    public class FinalPriceComponents
    {
        public decimal Nudge { get; set; }
        public decimal DesiredMargin { get; set; }
        public decimal MaxMargin { get; set; }
        public decimal MinMargin { get; set; }
        public decimal CheapestCompetitorPrice { get; set; }
        public int CompetitorCount { get; set; }
        public string PricingRule { get; set; }
        public decimal NonOEMDiscount { get; set; }
        public decimal FinalPrice { get; set; }
        public decimal BreakPrice1 { get; set; }
        public decimal BreakPrice2 { get; set; }
        public decimal BreakPrice3 { get; set; }
        public decimal BreakPrice4 { get; set; }
        public decimal BreakPrice5 { get; set; }
        public decimal FinalPriceIncVat { get; set; }
        public decimal BreakPrice1IncVat { get; set; }
        public decimal BreakPrice2IncVat { get; set; }
        public decimal BreakPrice3IncVat { get; set; }
        public decimal BreakPrice4IncVat { get; set; }
        public decimal BreakPrice5IncVat { get; set; }
        public PriceRule.productType ProductType { get; set; }
        public string BreakPricingRule { get; set; }
    }
}
