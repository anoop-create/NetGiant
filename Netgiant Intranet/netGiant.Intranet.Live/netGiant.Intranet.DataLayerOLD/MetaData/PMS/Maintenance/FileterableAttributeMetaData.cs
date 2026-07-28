using System.ComponentModel.DataAnnotations;

namespace netGiant.Intranet.DataLayer
{
    [MetadataType(typeof(FileterableAttributeMetaData))]
    public partial class filterableAttribute
    {
        
    }
    
    public class FileterableAttributeMetaData
    {
        //[Required(ErrorMessage = "Category code is required")]
        //public int categoryCodeFK { get; set; }
        [Required(ErrorMessage = "Attribute name is required")]
        public string attributeName { get; set; }
    }
}
