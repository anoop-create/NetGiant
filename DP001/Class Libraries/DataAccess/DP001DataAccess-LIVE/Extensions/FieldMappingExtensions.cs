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
        [RequiredIf("FileTypeName != 'Output Inventory' && FileTypeName != 'Additional Inventory'", ErrorMessage = "Brand is required")]
        [Display(Name = "Brand")]
        public string Brand { get; set; }

        [RequiredIf("FileTypeName != 'Output Inventory' && FileTypeName != 'Additional Inventory'", ErrorMessage = "Manufacturer Part No is required")]
        [Display(Name = "Manufacturer Part No")]
        public string ManufacturerPartNo { get; set; }

        [RequiredIf("FileTypeName == 'Supplier Inventory'", ErrorMessage = "Stock Quantity is required")]
        [Display(Name = "Stock Quantity")]
        public string StockQuantity { get; set; }

        [RequiredIf("FileTypeName == 'Supplier Inventory'", ErrorMessage = "Price is required")]
        [RequiredIf("FileTypeName == 'Competitor Inventory'", ErrorMessage = "Price is required")]
        [Display(Name = "Price")]
        public string Price { get; set; }

        [RequiredIf("FileTypeName != 'Output Inventory' && FileTypeName != 'Competitor Inventory' && FileTypeName != 'Additional Inventory'", ErrorMessage = "Description is required")]
        [Display(Name = "Description")]
        public string Description { get; set; }

        [RequiredIf("FileTypeName == 'Product Inventory'", ErrorMessage = "Product ID is required")]
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
    }
}
