using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DP001DataAccess.Entities
{
    [MetadataType(typeof(CompetitorInventoryExtensions))]
    public partial class CompetitorInventory
    {
        public string BrandName { get; set; }
        public string CompetitorName { get; set; }
    }

    public class CompetitorInventoryExtensions
    {
        [DisplayFormat(NullDisplayText = "0", DataFormatString = "{0:N2}")]
        public decimal Price { get; set; }
    }
}
