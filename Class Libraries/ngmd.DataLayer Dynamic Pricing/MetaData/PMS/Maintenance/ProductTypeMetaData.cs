using System.ComponentModel.DataAnnotations;

namespace netGiant.Intranet.DataLayer
{
    [MetadataType(typeof(ProductTypeMetaData))]
    public partial class productType
    {

    }
    
    public class ProductTypeMetaData
    {
        [Required(ErrorMessage="Product Type Name is required")]
        public string productTypeName { get; set; }
        [Required(ErrorMessage = "Product Type No is required")]
        public string productTypeNo { get; set; }
    }
}
