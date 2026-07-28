using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DP001DataAccess.Entities
{
    [MetadataType(typeof(ProductInventoryExtensions))]
    public partial class ProductInventory
    {
        public string BrandName { get; set; }
        public string LnKdBrandName { get; set; }
        public string ProductCategoryName { get; set; }

        [Display(Name = "Gross Margin")]
        [DisplayFormat(DataFormatString = "{0:N2}")]
        public decimal? GrossMarginPercentMod { get { return GrossMarginPercent * 100; } set { GrossMarginPercent = value / 100; } }
    }

    public class ProductInventoryExtensions
    {
        [DisplayFormat(NullDisplayText = "None")]
        public string ManufacturerPartNo { get; set; }

        [DisplayFormat(NullDisplayText = "0")]
        public decimal Price { get; set; }

        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy (HH:mm)}")]
        public DateTime DateLastUpdated { get; set; }

    }
}
