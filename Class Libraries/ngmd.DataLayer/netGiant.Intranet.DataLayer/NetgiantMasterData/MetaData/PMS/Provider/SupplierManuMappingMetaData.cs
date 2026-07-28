using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System.ComponentModel.DataAnnotations;

namespace netGiant.Intranet.DataLayer.NetgiantMasterData
{
    [MetadataType(typeof(SupplierManuMappingMetaData))]
    public partial class supplierManuMapping
    {
    }

    public class SupplierManuMappingMetaData
    {
        [Required(ErrorMessage = "Sup Manu Ref is required")]
        public string supplierManuRef { get; set; }
        [Required(ErrorMessage = "Manufacturer is required")]
        public int manufacturerFK { get; set; }
        [Required(ErrorMessage = "Provider is required")]
        public int providerFK { get; set; }
    }
}
