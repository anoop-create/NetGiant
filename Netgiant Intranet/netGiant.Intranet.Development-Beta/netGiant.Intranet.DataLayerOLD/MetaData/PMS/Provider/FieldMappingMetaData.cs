using System.ComponentModel.DataAnnotations;

namespace netGiant.Intranet.DataLayer
{
    [MetadataType(typeof(FieldMappingMetaData))]
    public partial class fieldMapping
    {
        
    }
    
    public class FieldMappingMetaData
    {
        [Required(ErrorMessage="Our PMS field is required")]
        public string fieldMappingTo { get; set; }
        [Required(ErrorMessage = "Provider's field is required")]
        public string fieldMappingWith { get; set; }
        [Required(ErrorMessage = "Provider is required")]
        public int providerFK { get; set; }
    }
}
