using System.ComponentModel.DataAnnotations;

namespace netGiant.Intranet.DataLayer.NetgiantMasterData
{
    [MetadataType(typeof(FileterableAttributeMetaData))]
    public partial class filterableAttribute
    {
        
    }
    
    public class FileterableAttributeMetaData
    {
        [Required(ErrorMessage = "Attribute name is required")]
        public string attributeName { get; set; }
    }
}
