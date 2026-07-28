using System.ComponentModel.DataAnnotations;

namespace netGiant.Intranet.DataLayer
{
    [MetadataType(typeof(WebsiteInventoryMetaData))]
    public partial class websiteInventory
    {

    }
    
    public class WebsiteInventoryMetaData
    {
        [Required(ErrorMessage="Website is required")]
        public int websiteFK { get; set; }
        [Required(ErrorMessage="Product is required")]
        public int productFK { get; set; }
        //[Required(ErrorMessage="Category Code is required")]
        //public int categoryCodeFK { get; set; }
    }
}
