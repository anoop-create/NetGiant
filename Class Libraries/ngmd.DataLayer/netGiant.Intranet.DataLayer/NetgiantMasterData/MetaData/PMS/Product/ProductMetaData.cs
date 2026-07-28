using System.ComponentModel.DataAnnotations;

namespace netGiant.Intranet.DataLayer.NetgiantMasterData
{
    [MetadataType(typeof(ProductMetaData))]
    public partial class product
    {
        public string UNSPSC { get; set; }
    }

    public class ProductMetaData
    {
        [Key]
        public int productID { get; set; }
        [Required(ErrorMessage = "Product Name is required")]
        public string productName { get; set; }
        [Required(ErrorMessage = "Part No is required")]
        public string partNo { get; set; }
        [Required(ErrorMessage = "Product Status is required")]
        public int productStatusFK { get; set; }
        [Required(ErrorMessage = "Data Supplier is required")]
        public int dataSupplierFK { get; set; }
        public string UNSPSCCode { get; set; }
        [Required(ErrorMessage = "Item Type is required")]
        public byte productItemTypeFK { get; set; }
    }
}
