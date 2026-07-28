using System.ComponentModel.DataAnnotations;

namespace netGiant.Intranet.DataLayer.NetgiantMasterData
{
    [MetadataType(typeof(ProductGroupMetaData))]
    public partial class productGroup
    {

    }
    
    public class ProductGroupMetaData
    {
        [Required(ErrorMessage = "Product Group Name is required")]
        public string productGroupName { get; set; }
        [Required(ErrorMessage = "Product Group No is required")]
        public string productGroupNo { get; set; }
        [Required(ErrorMessage="Product Type is required")]
        public int productTypeFK { get; set; }
    }
}
