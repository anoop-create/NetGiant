using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace netGiant.Intranet.DataLayer
{
    [MetadataType(typeof(AxisFieldsAdditionalMetaData))]
    public partial class AxisFieldsAdditional
    {
        
    }

    public class AxisFieldsAdditionalMetaData
    {
        [AllowHtml]
        public string stockNoteDesc { get; set; }
    }
}
