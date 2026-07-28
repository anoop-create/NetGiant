using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Import
{
    public class ProductFields
    {
        //Product Table
        public bool IsNew { get; set; }
        public string AltRef { get; set; }
        public string ProductName { get; set; }
        public string UnspscCode { get; set; }
        public int? ManufacturerFK { get; set; }
        public int? ProductStatusFK { get; set; }
        public int? ProductGroupFK { get; set; }
        public int? SalesAreaGroupFK { get; set; }
        public int? DataSupplierFK { get; set; }
        public int? PageYield { get; set; }
        public string Capacity { get; set; }
        public string Barcode { get; set; }
        public List<int> AssemblyComponents { get; set; }
        public string PrimaryEbusinessGroup { get; set; }
        public List<string> SecondaryEbusinessGroups { get; set; }
        public int? SecondaryCrossSellGroup { get; set; }

        //AxisFields Table
        public string PartNo { get; set; }
        public string StockRef { get; set; }
        public string Spec1 { get; set; }
        public string Spec2 { get; set; }
        public string Spec3 { get; set; }
        public string Spec4 { get; set; }
        public string Spec5 { get; set; }
        public string Spec6 { get; set; }
        public bool? ReSaleable { get; set; }
        public string StockRecordType { get; set; }
        public bool? DiscontinuedItem { get; set; }
        public int? DefaultDeliveryToCust { get; set; }
        public int? Attr1 { get; set; }
        public int? Attr2 { get; set; }
        public int? Attr3 { get; set; }
        public int? Attr4 { get; set; }
        public int? Attr5 { get; set; }
        public int? Attr6 { get; set; }
        public int? Attr7 { get; set; }
        public int? Attr8 { get; set; }
        public int? Attr9 { get; set; }
        public int? Attr10 { get; set; }
        public int? EBusinessLanguage { get; set; }
        public bool? Published { get; set; }
        public bool? Featured { get; set; }
        public bool? BestSeller { get; set; }
        public bool? SupressOpenRangeImage { get; set; }
        public bool? SupressOpenRangeSpec { get; set; }
        public string AdditionalInfoUrl { get; set; }

        //AxisFieldsAdditional Table
        public int WebsiteFK { get; set; }
        public string StockNoteDesc { get; set; }
        public string PriorityNote { get; set; }
        public string StockNoteLanguage { get; set; }
        public string MetaTitle { get; set; }
        public string MetaKeywords { get; set; }
        public string MetaDesc { get; set; }
        public string GoogleFeedSite { get; set; }
        public bool? GoogleFeedInclude { get; set; }
        public string GoogleFeedCategory { get; set; }
        public string GoogleFeedAvailability { get; set; }
        public string GoogleFeedCondition { get; set; }
        public string BespokeFeedSite { get; set; }
        public bool? BespokeFeedInclude { get; set; }
        public bool? BespokeFeedUseCustomShipCost { get; set; }
        public string BespokeFeedAvailability { get; set; }
        public string BespokeFeedCondition { get; set; }
        public string GooglePromotionId { get; set; }
        public int? BreakQuantity1 { get; set; }
        public int? BreakQuantity2 { get; set; }
        public int? BreakQuantity3 { get; set; }
    }

    public class ProductAcceptedFields
    {
        public static string[] Fields = { "Alt Ref", "ACD Modifier", "Stock reference", "Specification 1", 
                                            "Specification 2", "Specification 3", "Specification 4", 
                                            "Specification 5", "Specification 6", "Default delivery days", 
                                            "Discontinued Item", "Re-saleable", "Attribute 1", "Attribute 2",
                                            "Attribute 3", "Attribute 4", "Attribute 5", "Attribute 6", "Attribute 7", 
                                            "Attribute 8", "Attribute 9", "Attribute 10", "English Meta Title",
                                            "French Meta Title", "German Meta Title", "English Meta Keywords",
                                            "French Meta Keywords", "German Meta Keywords", "English Meta Description",
                                            "French Meta Description", "German Meta Description", "Additional Information URL", 
                                            "Suppress open range image", "Suppress open range spec", "Featured item", "Best seller",
                                            "Published", "Stock Notes", "Stock Record Type", "Google Feed Include",
                                            "Google Feed Category", "Google Feed Availability", "Google Feed Condition",
                                            "Bespoke Feed Include", "Bespoke Feed Custom Shipping Cost", 
                                            "Google Feed Site", "Bespoke Feed Site",
                                            "Bespoke Feed Availability", "Bespoke Feed Condition", "Product Name",
                                            "Manufacturer", "Priority Note", "Product Status", "Product Group", "Sales Area Group",
                                            "Data Supplier", "Provider Part No", "Provider ID", "Axis Supplier ID",
                                            "Record Type", "eBus", "eBusIsPrimary", "Google Promotion IDs", "Page Yield", "Capacity", 
                                            "Break Quantity 1", "Break Quantity 2", "Break Quantity 3", "Barcode",
                                            "Assembly Components", "Primary eBusiness Group", "Secondary eBusiness Groups",
                                            "Website", "URL", "IsThumbnail", "IsMain", "Secondary Cross Sell Group", "dateCreated" };
    }

    public class SkuMappingFields
    {
        public string ProviderPartNo { get; set; }
        public int? ProviderFK { get; set; }
        public int? AxisSupplierNo { get; set; }
        public string AltRef { get; set; }
        public int? ProductFK { get; set; }
        public int? ProviderInventoryFK { get; set; }
    }

    public class eBusinessMappingFields
    {
        public int productFK { get; set; }
        public string eBusinessRef { get; set; }
        public bool? isPrimary { get; set; }
    }

    public class CategoryCodeFields
    {
        public string AltRef { get; set; }
        public string Manufacturer { get; set; }
        public int? CategoryCodeTG { get; set; }
        public int? CategoryCodeCM { get; set; }
        public List<int> SecondaryCategoriesTG { get; set; }
        public List<int> SecondaryCategoriesCM { get; set; }
    }

    public class CategoryCodeAcceptedFields
    {
        public static string[] Fields = { "Alt Ref", "Manufacturer", "Category Code TG", "Category Code CM", "Category Code NG",
                                          "Secondary Categories TG", "Secondary Categories CM", "Secondary Categories NG" };
    }

    public class EquipmentAcceptedFields
    {
        public static string[] Fields = { "Equip Description", "Equip Manufacturer", "Equip Cartridge Type",
                                          "Equip Product", "Equip Product Type", "Equip Main URL", "Equip Thumbnail URL",
                                          "Equip Meta Keywords", "Family Description", "Family Manufacturer",
                                          "Equip ID", "Family ID", "Record Type", "Equip Meta Content Type", "Globally Featured", 
                                          "Brand Featured", "Date Created", "Equip Notes ID", "Website ID", "Equip Note", "Is Detail", "Equip Status" };
    }

    public class EquipmentNotesAcceptedFields
    {
        public static string[] Fields = { "Equipment Notes ID", "Equipment ID", "Website ID", "Equipment Note", "Is Detail" };
    }

    public class EquipmentImportFields
    {
        public int EquipID { get; set; }
        public string EquipDescription { get; set; }
        public int EquipManuFK { get; set; }
        public int EquipCartTypeFK { get; set; }
        public int? EquipProductFK { get; set; }
        public int EquipProductTypeFK { get; set; }
        public int EquipStatusFK { get; set; }
        public string EquipMainURL { get; set; }
        public string EquipThumbnailURL { get; set; }
        public string EquipMetaKeywords { get; set; }
        public string EquipMetaTitle { get; set; }
        public string EquipMetaDescription { get; set; }
        public byte EquipMetaContentTypeFK { get; set; } = 1;
        public string FamilyDescription { get; set; }
        public int? FamilyManuFK { get; set; }
        public bool? GloballyFeatured { get; set; }
        public bool? HomeFeatured { get; set; }
        public bool? BrandFeatured { get; set; }
    }

    public class EquipmentNotesImportFields
    {
        public int EquipNotesID { get; set; }
        public int WebsiteID { get; set; }
        public int EquipmentID { get; set; }
        public string EquipNote { get; set; }
        public bool? IsDetail { get; set; }
    }

    public class FamilyImportFields
    {
        public int FamilyID { get; set; }
        public string FamilyDescription { get; set; }
        public int FamilyManuFK { get; set; }
    }

    public class FamilyMappingImportFields
    {
        public int FamilyID { get; set; }
        public int EquipID { get; set; }
    }

    public class EquipmentProductMappingImportFields
    {
        public int EquipID { get; set; }
        public int ProductID { get; set; }
    }

    public class PromotionalGroupsAcceptedFields
    {
        public static string[] Fields = { "Website ID", "Alt Ref", "Promo Name" };
    }

    public class PromotionalGroupImportFields
    {
        public int WebsiteId { get; set; }
        public string AltRef { get; set; }
        public string PromoName { get; set; }
    }

    public class CrossSellingLinksAcceptedFields
    {
        public static string[] Fields = { "Part No A", "Part No B", "Type", "Two Way Link" };
    }

    public class CrossSellingLinkImportFields
    {
        public string PartNoA { get; set; }
        public string PartNoB { get; set; }
        public string Type { get; set; }
        public bool TwoWayLink { get; set; }
    }

    public class ObsoleteItemAcceptedFields
    {
        public static string[] Fields = { "Website ID", "Stock Reference", "Equipment Name", "URL" };
    }

    public class ObsoleteItemImportFields
    {
        public int WebsiteId { get; set; }
        public string StockReference { get; set; }
        public string EquipmentName { get; set; }
        public string URL { get; set; }
    }

    public class ProviderInventoryAcceptedFields
    {
        public static string[] Fields = { "Part No", "Description", "Quantity", "Effective Date", "Provider ID", "Date Last Updated",
                                          "Provider Part No", "Manufacturer ID", "Potential New Product", "Unwanted Product",
                                          "Untrusted Provider", "UNSPSC Code", "UNSPSC Class", "Provider Manu Ref", "Barcode" };
    }

    public class ProviderInventoryImportFields
    {
        [Required]
        [MaxLength(255)]
        public string PartNo { get; set; }
        [MaxLength(900)]
        public string Description { get; set; }
        public int? Quantity { get; set; }
        public DateTime? EffectiveDate { get; set; }
        [Required]
        public int ProviderID { get; set; }
        public DateTime DateLastUpdate { get; set; }
        [MaxLength(255)]
        public string ProviderPartNo { get; set; }
        public int? ManufacturerID { get; set; }
        public bool? PotentialNewProduct { get; set; }
        public bool? UnwantedProduct { get; set; }
        public bool? UntrustedProvider { get; set; }
        [MaxLength(200)]
        public string UNSPSCCode { get; set; }
        [MaxLength(200)]
        public string UNSPSCClass { get; set; }
        [MaxLength(200)]
        public string ProviderManuRef { get; set; }
        [MaxLength(20)]
        public string Barcode { get; set; }
    }

    public class ProductImagesImportFields
    {
        public int websiteInventoryFK { get; set; } // not imported/exported, but useful to us
        public string altRef { get; set; }
        public int manufacturerFK { get; set; }
        public int websiteFK { get; set; }
        public string URL { get; set; }
        public bool isThumbnail { get; set; }
        public bool isMain { get; set; }
        public string ACDModifier { get; set; }
    }

}
