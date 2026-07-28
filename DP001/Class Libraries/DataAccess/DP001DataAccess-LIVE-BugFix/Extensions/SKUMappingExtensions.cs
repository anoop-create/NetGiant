using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExpressiveAnnotations.Attributes;

namespace DP001DataAccess.Entities
{
    [MetadataType(typeof(SKUMappingExtensions))]
    public partial class SKUMapping
    {
        public int ProductInventoryFK { get; set; }
    }

    public class SKUMappingExtensions
    {
    }
}
