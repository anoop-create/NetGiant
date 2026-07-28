using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExpressiveAnnotations.Attributes;

namespace DP001DataAccess.Entities
{
    [MetadataType(typeof(FieldMappingExtensions))]
    public partial class FieldMapping
    {
        public string FileTypeName { get; set; }
    }

    public class FieldMappingExtensions
    {
        [RequiredIf("FileTypeName != 'Output Inventory' && FileTypeName != 'Sales History Inventory' && FileTypeName != 'Additional Inventory'", ErrorMessage = "Brand is required")]
        [Display(Name = "Brand")]
        public string Brand { get; set; }

        [RequiredIf("FileTypeName != 'Output Inventory' && FileTypeName != 'Sales History Inventory' && FileTypeName != 'Additional Inventory'", ErrorMessage = "Manufacturer Part No is required")]
        [Display(Name = "Manufacturer Part No")]
        public string ManufacturerPartNo { get; set; }

        [RequiredIf("FileTypeName == 'Supplier Inventory'", ErrorMessage = "Stock Quantity is required")]
        [Display(Name = "Stock Quantity")]
        public string StockQuantity { get; set; }

        [RequiredIf("FileTypeName == 'Supplier Inventory'", ErrorMessage = "Price is required")]
        [RequiredIf("FileTypeName == 'Competitor Inventory'", ErrorMessage = "Price is required")]
        [RequiredIf("FileTypeName == 'Sales History Inventory'", ErrorMessage = "Average Sell Price is required")]
        [Display(Name = "Price")]
        public string Price { get; set; }

        [RequiredIf("FileTypeName != 'Output Inventory' && FileTypeName != 'Sales History Inventory' && FileTypeName != 'Competitor Inventory' && FileTypeName != 'Additional Inventory'", ErrorMessage = "Description is required")]
        [Display(Name = "Description")]
        public string Description { get; set; }

        [RequiredIf("FileTypeName == 'Product Inventory'", ErrorMessage = "Product ID is required")]
        [RequiredIf("FileTypeName == 'Sales History Inventory'", ErrorMessage = "Client product ID is required")]
        [Display(Name = "Client Product ID")]
        public string ClientProductID { get; set; }

        [Display(Name = "Linked Manufacturer")]
        public string LnKdManufacturer { get; set; }

        [Display(Name = "Linked Manufacturer Part No")]
        public string LnKdManufacturerPartNo { get; set; }

        [RequiredIf("FileTypeName == 'Product Inventory'", ErrorMessage = "Product Category is required")]
        [Display(Name = "Product Category")]
        public string ProductCategory { get; set; }

        [RequiredIf("FileTypeName == 'Competitor Inventory'", ErrorMessage = "Competitor is required")]
        [Display(Name = "Competitor")]
        public string Competitor { get; set; }

        [Display(Name = "Key Line")]
        public bool IsKeyLine { get; set; }

        [Display(Name = "Variant Of")]
        public string VariantOf { get; set; }

        [RequiredIf("FileTypeName == 'Sales History Inventory'", ErrorMessage = "Quantity is required")]
        [Display(Name = "Quantity")]
        public string Quantity { get; set; }

        [RequiredIf("FileTypeName == 'Sales History Inventory'", ErrorMessage = "Period is required")]
        [Display(Name = "Period")]
        public string Period { get; set; }

        [RequiredIf("FileTypeName == 'Sales History Inventory'", ErrorMessage = "Date is required")]
        [Display(Name = "Date")]
        public string Date { get; set; }

        [RequiredIf("FileTypeName == 'Sales History Inventory'", ErrorMessage = "Average Cost Price is required")]
        [Display(Name = "Average Cost Price")]
        public string Price2 { get; set; }
    }
}
