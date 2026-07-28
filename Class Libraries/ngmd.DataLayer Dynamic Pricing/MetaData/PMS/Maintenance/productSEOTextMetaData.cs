using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace netGiant.Intranet.DataLayer
{
    [MetadataType(typeof(productSEOTextMetaData))]
    public partial class productSEOText
    {
        
    }

    public class productSEOTextMetaData
    {
        [Key]
        public int productSEOTextID { get; set; }
        [Range(1, 5)]
        [Required(ErrorMessage = "ParaGraph Number is required")]
        public int paragraphNo { get; set; }
        [Range(1, 5)]
        [Required(ErrorMessage = "Entry Number is required")]
        public int entryNo { get; set; }
        [Required(ErrorMessage = "Website is required")]
        public int websiteFK { get; set; }
        [Required(ErrorMessage = "Product Type is required")]
        public int productTypeFK { get; set; }
        [Required(ErrorMessage = "ParaGraph Text is required")]
        public string paragraphText { get; set; }
    }
}
