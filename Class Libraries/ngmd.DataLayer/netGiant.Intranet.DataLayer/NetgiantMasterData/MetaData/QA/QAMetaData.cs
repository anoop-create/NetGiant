using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace netGiant.Intranet.DataLayer.NetgiantMasterData
{
    [MetadataType(typeof(QAMetaData))]
    public partial class qa_Main
    {
        public Website SourceWebsite { get; set; }
    }
    
    public class QAMetaData
    {
        [DataType(DataType.MultilineText)]
        [Required(ErrorMessage="Question is required")]
        public string Question { get; set; }

        [AllowHtml]
        [Required(ErrorMessage="Answer is required")]
        public string Answer { get; set; }

        [Required(ErrorMessage="Granularity is required")]
        public int GranularityFK { get; set; }

        [Required(ErrorMessage = "Select at least one website")]
        public virtual ICollection<qa_WebsiteMapping> qa_WebsiteMapping { get; set; }
    }
}
