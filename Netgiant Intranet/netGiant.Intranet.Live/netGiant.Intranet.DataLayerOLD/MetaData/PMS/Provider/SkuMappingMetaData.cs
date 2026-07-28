using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using netGiant.Intranet.DataLayer;
using System.ComponentModel.DataAnnotations;

namespace netGiant.Intranet.DataLayer
{
    [MetadataType(typeof(SkuMappingMetaData))]
    public partial class skuMapping
    {
    }

    public class SkuMappingMetaData
    {
        [Required(ErrorMessage = "Provider is required")]
        public int providerFK { get; set; }
        [Required(ErrorMessage = "Provider Part No is required")]
        public string providerPartNo { get; set; }
        [Required(ErrorMessage = "Our Part No is required")]
        public string altRef { get; set; }
    }
}
