using System.ComponentModel.DataAnnotations;

namespace netGiant.Intranet.DataLayer
{
    [MetadataType(typeof(ProviderMetaData))]
    public partial class provider
    {
    }
    
    public class ProviderMetaData
    {
        [Required(ErrorMessage="Provider's name is required")]
        public string providerName { get; set; }
        [Required(ErrorMessage = "Provider type is required")]
        public int providerTypeFK { get; set; }
    }
}
