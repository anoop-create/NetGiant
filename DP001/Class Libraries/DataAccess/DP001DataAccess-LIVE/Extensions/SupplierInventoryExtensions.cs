using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DP001DataAccess.Entities
{
    [MetadataType(typeof(SupplierInventoryExtensions))]
    public partial class SupplierInventory
    {
        public string BrandName { get; set; }
    }

    public class SupplierInventoryExtensions
    {
        [DisplayFormat(DataFormatString = "{0:N2}")]
        public decimal Price { get; set; }
    }
}
