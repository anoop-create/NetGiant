using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace netGiant.Intranet.DataLayer.NetgiantMasterData
{
    [MetadataType(typeof(CmsEntryMetaData))]
    public partial class cmsEntry
    {

    }

    public class CmsEntryMetaData
    {
        [Key]
        public int cmsEntryID { get; set; }

        [Required(ErrorMessage = "Series is a required field")]
        public int cmsSectionFK { get; set; }

        [Required(ErrorMessage = "Entry Name is a required field")]
        public int entryName { get; set; }

        [AllowHtml]
        public string cmsContent { get; set; }

        public string metaData { get; set; }
    }
}
