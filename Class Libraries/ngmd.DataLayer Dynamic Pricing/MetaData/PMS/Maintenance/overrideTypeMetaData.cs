using System.ComponentModel.DataAnnotations;

namespace netGiant.Intranet.DataLayer
{
    [MetadataType(typeof(overrideTypeMetaData))]   
    public partial class overrideType
    {

    }
    
    public class overrideTypeMetaData
    {
        [Required(ErrorMessage="Override Type Name is required")]
        public string overrideTypeName { get; set; }
    }
}
