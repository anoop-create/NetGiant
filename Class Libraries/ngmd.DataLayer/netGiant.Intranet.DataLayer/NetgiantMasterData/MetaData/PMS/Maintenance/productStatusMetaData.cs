using System.ComponentModel.DataAnnotations;

namespace netGiant.Intranet.DataLayer.NetgiantMasterData
{
    [MetadataType(typeof(productStatusMetaData))]
    public partial class productStatus
    {

    }
    
    public class productStatusMetaData
    {
        [Required(ErrorMessage="Product Status Name is required")]
        public string productStatusName { get; set; }
    }
}
