using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace netGiant.Intranet.DataLayer
{
    [MetadataType(typeof(CategoryCodeMetaData))]
    public partial class categoryCode
    {
        public string parentCategoryCodeName { get; set; }
        public List<categoryCode> Children { get; set; }
        public int ProductCount { get; set; }
    }
    
    public class CategoryCodeMetaData
    {
        [Required(ErrorMessage = "Category Code Name is required")]
        public string categoryCodeName { get; set; }
        [Required(ErrorMessage="Website is required")]
        public int websiteFK { get; set; }
    }
}
