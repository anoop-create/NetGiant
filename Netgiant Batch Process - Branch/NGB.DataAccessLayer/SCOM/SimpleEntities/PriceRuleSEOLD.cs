using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGBP.DataAccessLayer.SCOM.SimpleEntities
{
    [Serializable]
    public class PriceRuleSEOLD
    {
        public int WebsiteInventoryFK { get; set; }
        public int ProductFK { get; set; }
        public string PartNo { get; set; }
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
        public decimal MinMargin { get; set; }
        public decimal MaxMargin { get; set; }
        public decimal CompetitorsToBeat { get; set; }
        public decimal Nudge { get; set; }
        public string CompPrices { get; set; }
    }
}
