using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Entities
{
    public class ProductEntity
    {
        public string ProductUrl { get; set; }
        public string ImageUrl { get; set; }
        public string PartNo { get; set; }
        public string ManuRef { get; set; }
        public string ProductName { get; set; }
        public string Brand { get; set; }
        public int Availability { get; set; }
        public int AttribValue4 { get; set; }
        public int AttribValue5 { get; set; }
        public int AttribValue6 { get; set; }
        public int AttribValue7 { get; set; }
        public int AttribValue8 { get; set; }
        public int AttribValue9 { get; set; }
        public int AxisBrandNo { get; set; }
        public string AttribDesc6 { get; set; }
        public string AttribDesc9 { get; set; }
        public string SpecLine1 { get; set; }
        public string SpecLine2 { get; set; }
        public string SpecLine4 { get; set; }
        public string SpecLine6 { get; set; }
        public int ProductItemType { get; set; }
        public int ProductReference { get; set; }
        public int ProductId { get; set; }
        public int BrandFlag { get; set; }
        public decimal PriceRetail { get; set; }
        public decimal PriceTrade { get; set; }
        public decimal BreakPrice2 { get; set; }
        public decimal BreakPrice3 { get; set; }
        public int BreakQuantity2 { get; set; }
        public int BreakQuantity3 { get; set; }
        public int PageYield { get; set; }
        public double AssemblySaving { get; set; }
        public int AssemblyCount { get; set; }
        public string ProductNotes { get; set; }
        public int ProductGroup { get; set; }
        public string CategoryCodeName { get; set; }
        public int ProductTypeId { get; set; }
        public string ProductType { get; set; }
        public string ProductVideoUrl { get; set; }
        public string CrossSellProductUrl { get; set; }
        public int CrossSellProductId { get; set; }
        public string CrossSellDescription { get; set; }
        public int CrossSellStatus { get; set; }
        public decimal CrossSellPrice { get; set; }
        public string CrossSellImageUrl { get; set; }
        public string CrossSellRef { get; set; }
        public string DsNotes { get; set; }
        public int DsSuppress { get; set; }
        public string MetaTitle { get; set; }
        public string MetaDesc { get; set; }
        public string MetaKeywords { get; set; }
    }
}
