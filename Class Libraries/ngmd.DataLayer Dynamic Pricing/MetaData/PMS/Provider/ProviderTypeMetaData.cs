using System.ComponentModel.DataAnnotations;

namespace netGiant.Intranet.DataLayer
{
    [MetadataType(typeof(ProviderTypeMetaData))]
    public partial class providerType
    {
        
    }
    
    public class ProviderTypeMetaData
    {
        [Required(ErrorMessage="Provider type name is required")]
        [MaxLength(45, ErrorMessage="Maximum length is 45 characters")]
        public string providerTypeName { get; set; }
    }
}
