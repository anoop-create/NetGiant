using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System.ComponentModel.DataAnnotations;

namespace netGiant.Intranet.DataLayer.NetgiantMasterData
{
    [MetadataType(typeof(MfpnExtensionsMetaData))]
    public partial class mfpnExtensions
    {
    }

    public class MfpnExtensionsMetaData
    {
        [Required(ErrorMessage = "Manufacturer is required")]
        public string manuID { get; set; }
        [Required(ErrorMessage = "Extension is required")]
        public int extension { get; set; }
    }
}
