using System.ComponentModel.DataAnnotations;

namespace netGiant.Intranet.DataLayer
{
    [MetadataType(typeof(ProductOverrideMetaData))]
    public partial class productOverride
    {

    }
    
    public class ProductOverrideMetaData
    {
        [Required(ErrorMessage = "Original value is required")]
        public string originalValue { get; set; }

        [Required(ErrorMessage = "Override value is required")]
        public string overrideValue { get; set; }

        [Required(ErrorMessage = "Override Type is required")]
        public int overrideTypeFK { get; set; }
    }
}
