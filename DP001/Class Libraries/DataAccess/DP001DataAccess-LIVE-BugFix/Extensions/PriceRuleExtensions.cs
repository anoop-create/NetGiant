using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExpressiveAnnotations.Attributes;

namespace DP001DataAccess.Entities
{
    [MetadataType(typeof(PriceRuleExtensions))]
    public partial class PriceRule
    {
        public string RuleTypeName { get; set; }
        public string RuleMethodName { get; set; }

        [RequiredIf("RuleMethodName == 'Cost Base'", ErrorMessage = "Desired Margin is required")]
        [Range(0, 1000, ErrorMessage = "Desired Margin is a percentage and should be between 0 and 1000")]
        [Display(Name = "Desired Margin")]
        [DisplayFormat(DataFormatString = "{0:N2}")]
        public decimal DesiredMarginMod { get { return DesiredMargin * 100; } set { DesiredMargin = value / 100; } }

        [RequiredIf("RuleMethodName == 'Cost Base'", ErrorMessage = "Minimum Margin is required")]
        [Range(0, 1000, ErrorMessage = "Minimum Margin is a percentage and should be between 0 and 1000")]
        [Display(Name = "Minimum Margin")]
        [DisplayFormat(DataFormatString = "{0:N2}")]
        public decimal MinMarginMod { get { return MinMargin * 100; } set { MinMargin = value / 100; } }

        [RequiredIf("RuleMethodName == 'Cost Base'", ErrorMessage = "Maximum Margin is required")]
        [Range(0, 1000, ErrorMessage = "Maximum Margin is a percentage and should be between 0 and 1000")]
        [Display(Name = "Maximum Margin")]
        [DisplayFormat(DataFormatString = "{0:N2}")]
        public decimal MaxMarginMod { get { return MaxMargin * 100; } set { MaxMargin = value / 100; } }

        [RequiredIf("RuleMethodName == 'Cost Base'", ErrorMessage = "Beat Rate is required, but can be 0")]
        [Range(0, 100, ErrorMessage = "Beat Rate is a percentage and should be between 0 and 100")]
        [Display(Name = "Beat Rate")]
        [DisplayFormat(DataFormatString = "{0:N2}")]
        public decimal BeatRateMod { get { return BeatRate * 100; } set { BeatRate = value / 100; } }

        [RequiredIf("RuleMethodName == 'Cost Base'", ErrorMessage = "Nudge Amount is required, but can be 0")]
        [Range(-100, 100, ErrorMessage = "Nudge Amount is a percentage and should be between -100 and 100")]
        [Display(Name = "Nudge Amount")]
        [DisplayFormat(DataFormatString = "{0:N2}")]
        public decimal NudgeMod { get { return Nudge * 100; } set { Nudge = value / 100; } }

        [Range(0, 1000, ErrorMessage = "Alt Price Adjustment 1 is a percentage and should be between 0 and 1000")]
        [Display(Name = "Alt Price Adjustment 1")]
        [DisplayFormat(DataFormatString = "{0:N2}")]
        public decimal AltPriceAdj1Mod { get { return AltPriceAdj1 * 100; } set { AltPriceAdj1 = value / 100; } }

        [Range(0, 1000, ErrorMessage = "Alt Price Adjustment 2 is a percentage and should be between 0 and 1000")]
        [Display(Name = "Alt Price Adjustment 2")]
        [DisplayFormat(DataFormatString = "{0:N2}")]
        public decimal AltPriceAdj2Mod { get { return AltPriceAdj2 * 100; } set { AltPriceAdj2 = value / 100; } }
    }

    public class PriceRuleExtensions
    {
        [Required(ErrorMessage = "Rule Name is required")]
        [Display(Name = "Rule Name")]
        public string RuleName { get; set; }

        [Required(ErrorMessage = "Rule Type is required")]
        [Display(Name = "Rule Type")]
        public string RuleTypeFK { get; set; }

        [RequiredIf("RuleTypeName == 'Brand'", ErrorMessage = "Brand is required")]
        [Display(Name = "Brand")]
        public Nullable<int> BrandFK { get; set; }

        [RequiredIf("RuleTypeName == 'Category'", ErrorMessage = "Category is required")]
        [Display(Name = "Product Category")]
        public Nullable<long> ProductCategoryFK { get; set; }

        [RequiredIf("RuleTypeName == 'Product'", ErrorMessage = "Product is required")]
        [Display(Name = "Product Name")]
        public Nullable<long> ProductInventoryFK { get; set; }

        [RequiredIf("RuleMethodName == 'Cost Base'", ErrorMessage = "Cost Uplift is required, but can be 0")]
        [Display(Name = "Cost Uplift")]
        public decimal CostUplift { get; set; }

        //[RequiredIf("RuleMethodName == 'Cost Base'", ErrorMessage = "Desired Margin is required")]
        //[Range(0, 100, ErrorMessage = "Desired Margin is a percentage and should be between 0 and 100")]
        //[Display(Name = "Desired Margin")]
        //[DisplayFormat(DataFormatString = "{0:F1}")]
        public decimal DesiredMargin { get; set; }
        //public decimal DesiredMargin { get; set; }

        //[RequiredIf("RuleMethodName == 'Cost Base'", ErrorMessage = "Minimum Margin is required")]
        //[Range(0, 100, ErrorMessage = "Minimum Margin is a percentage and should be between 0 and 100")]
        //[Display(Name = "Minimum Margin")]
        public decimal MinMargin { get; set; }

        //[RequiredIf("RuleMethodName == 'Cost Base'", ErrorMessage = "Maximum Margin is required")]
        //[Range(0, 100, ErrorMessage = "Maximum Margin is a percentage and should be between 0 and 100")]
        //[Display(Name = "Maximum Margin")]
        public decimal MaxMargin { get; set; }

        //[RequiredIf("RuleMethodName == 'Cost Base'", ErrorMessage = "Beat Rate is required, but can be 0")]
        //[Range(0, 100, ErrorMessage = "Beat Rate is a percentage and should be between 0 and 100")]
        //[Display(Name = "Beat Rate")]
        public decimal BeatRate { get; set; }

        //[RequiredIf("RuleMethodName == 'Cost Base'", ErrorMessage = "Nudge Amount is required, but can be 0")]
        //[Range(0, 100, ErrorMessage = "Nudge Amount is a percentage and should be between 0 and 100")]
        //[Display(Name = "Nudge Amount")]
        public decimal Nudge { get; set; }

        [Required(ErrorMessage = "Method is required")]
        [Display(Name = "Method")]
        public int MethodFK { get; set; }

        [RequiredIf("IsBanding == true", ErrorMessage = "Band Name is required")]
        public string BandName { get; set; }

        [RequiredIf("IsBanding == true", ErrorMessage = "Band Start is required")]
        [DisplayFormat(DataFormatString = "{0:n2}")]
        [Display(Name = "Band Start")]
        public Nullable<decimal> BandStart { get; set; }

        [RequiredIf("IsBanding == true", ErrorMessage = "Band End is required")]
        [DisplayFormat(DataFormatString = "{0:n2}")]
        [Display(Name = "Band End")]
        public Nullable<decimal> BandEnd { get; set; }

        //public bool UpliftIsPc { get; set; }
        //public bool MarginsArePc { get; set; }

        [RequiredIf("RuleMethodName == 'Fixed Price'", ErrorMessage = "Fixed Price Override is required")]
        [Display(Name = "Fixed Price Override")]
        public decimal FixedPriceOverride { get; set; }

        [Display(Name = "Alt Price Adjustment 1")]
        public decimal AltPriceAdj1 { get; set; }

        [Display(Name = "Alt Price Adjustment 2")]
        public decimal AltPriceAdj2 { get; set; }

        [Display(Name = "Alt Price Adjustment 3")]
        public decimal AltPriceAdj3 { get; set; }

        [Display(Name = "Alt Price Adjustment 4")]
        public decimal AltPriceAdj4 { get; set; }

        [Display(Name = "Alt Price Adjustment 5")]
        public decimal AltPriceAdj5 { get; set; }

        [Display(Name = "Alt Price Adjustment 6")]
        public decimal AltPriceAdj6 { get; set; }

        [Display(Name = "Alt Price Adjustment 7")]
        public decimal AltPriceAdj7 { get; set; }

        [Display(Name = "Alt Price Adjustment 8")]
        public decimal AltPriceAdj8 { get; set; }

        [Display(Name = "Alt Price Adjustment 9")]
        public decimal AltPriceAdj9 { get; set; }

        [Display(Name = "Alt Price Adjustment 10")]
        public decimal AltPriceAdj10 { get; set; }

        [RequiredIf("RuleMethodName == 'Related Product Base'", ErrorMessage = "Related Product Discount is required")]
        [Range(0, 100, ErrorMessage = "Related Product Discount is a percentage and should be between 0 and 100")]
        [Display(Name = "Related Product Discount")]
        public decimal CompatDiscount { get; set; }

        [Display(Name = "Rounding Rule")]
        public int RoundingGroupFK { get; set; }
    }
}
