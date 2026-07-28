using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace netGiant.Intranet.DataLayer.NetgiantMasterData
{
    [MetadataType(typeof(CmsSectionMetaData))]
    public partial class cmsSeries
    {

    }

    public class CmsSectionMetaData
    {
        [Key]
        public int cmsSectionID { get; set; }

        [Required(ErrorMessage = "Website is a required field")]
        public int websiteFK { get; set; }

        [Required(ErrorMessage = "Section Name is a required field")]
        public int sectionName { get; set; }

        [AllowHtml]
        public string headerContent { get; set; }
    }
}
