using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace netGiant.Intranet.DataLayer
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
        [Required(ErrorMessage="Answer is required")]
        public string Answer { get; set; }
        [Required(ErrorMessage="Granularity is required")]
        public int GranularityFK { get; set; }
        [Required(ErrorMessage = "Select at least one website")]
        public virtual ICollection<qa_WebsiteMapping> qa_WebsiteMapping { get; set; }
    }
}
