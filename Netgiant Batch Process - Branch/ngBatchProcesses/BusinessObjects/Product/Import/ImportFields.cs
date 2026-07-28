using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ngBatchProcesses.BusinessObjects.Product.Import
{
    public class ImportFields
    {
        //Product Table
        public bool IsNew { get; set; }
        public string altRef { get; set; }
        public string productName { get; set; }
        public string unspscCode { get; set; }
        public int? manufacturerFK { get; set; }
        public int? productStatusFK { get; set; }
        public int? productGroupFK { get; set; }
        public int? salesAreaGroupFK { get; set; }
        public int? dataSupplierFK { get; set; }

        //Website Inventory Table
        public int? CategoryCodeFK { get; set; }

        //AxisFields Table
        public string partNo { get; set; }
        public string stockRef { get; set; }
        public string spec1 { get; set; }
        public string spec2 { get; set; }
        public string spec3 { get; set; }
        public string spec4 { get; set; }
        public string spec5 { get; set; }
        public string spec6 { get; set; }
        public bool? reSaleable { get; set; }
        public string stockRecordType { get; set; }
        public bool? discontinuedItem { get; set; }
        public int? defaultDeliveryToCust { get; set; }
        public int? attr1 { get; set; }
        public int? attr2 { get; set; }
        public int? attr3 { get; set; }
        public int? attr4 { get; set; }
        public int? attr5 { get; set; }
        public int? attr6 { get; set; }
        public int? attr7 { get; set; }
        public int? attr8 { get; set; }
        public int? attr9 { get; set; }
        public int? attr10 { get; set; }
        public int? eBusinessLanguage { get; set; }
        public bool? published { get; set; }
        public bool? featured { get; set; }
        public bool? bestSeller { get; set; }
        public bool? supressCnetImage { get; set; }
        public bool? supressCnetDesc { get; set; }
        public string additionalInfoUrl { get; set; }

        //AxisFieldsAdditional Table
        public int websiteFK { get; set; }
        public string stockNoteDesc { get; set; }
        public string stockNoteLanguage { get; set; }
        public string metaTitle { get; set; }
        public string metaKeywords { get; set; }
        public string metaDesc { get; set; }
        public string googleFeedSite { get; set; }
        public bool? googleFeedInclude { get; set; }
        public string googleFeedCategory { get; set; }
        public string googleFeedAvailability { get; set; }
        public string googleFeedCondition { get; set; }
        public string bespokeFeedSite { get; set; }
        public bool? bespokeFeedInclude { get; set; }
        public bool? bespokeFeedUseCustomShipCost { get; set; }
        public string bespokeFeedAvailability { get; set; }
        public string bespokeFeedCondition { get; set; }
    }

    public class AcceptedFields
    {
        public static string[] Fields
        {
            get { return acceptedFields; }
        }

        private static string[] acceptedFields = { "Alt Ref", "Stock reference", "Specification 1", 
                                            "Specification 2", "Specification 3", "Specification 4", 
                                            "Specification 5", "Specification 6", "Default delivery days", 
                                            "Discontinued Item", "Re-saleable", "Attribute 1", "Attribute 2",
                                            "Attribute 3", "Attribute 4", "Attribute 5", "Attribute 6", "Attribute 7", 
                                            "Attribute 8", "Attribute 9", "Attribute 10", "English Meta Title",
                                            "French Meta Title", "German Meta Title", "English Meta Keywords",
                                            "French Meta Keywords", "German Meta Keywords", "English Meta Description",
                                            "French Meta Description", "German Meta Description", "Additional Information URL", 
                                            "Suppress cnet image", "Suppress cnet description", "Featured item", "Best seller",
                                            "Published", "Stock Notes", "Stock Record Type", "Google Feed Include",
                                            "Google Feed Category", "Google Feed Availability", "Google Feed Condition",
                                            "Bespoke Feed Include", "Bespoke Feed Custom Shipping Cost", 
                                            "Google Feed Site", "Bespoke Feed Site",
                                            "Bespoke Feed Availability", "Bespoke Feed Condition", "Product Name", "UNSPSC",
                                            "Manufacturer", "Product Status", "Product Group", "Sales Area Group",
                                            "Data Supplier", "Provider Part No", "Provider ID", "Axis Supplier ID",
                                            "Record Type", "Category Code", "eBus", "eBusIsPrimary"};
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
}
