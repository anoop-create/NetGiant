using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace netGiant.Intranet.DataLayer.NetgiantMasterData
{
    [MetadataType(typeof(AxisFieldsAdditionalMetaData))]
    public partial class AxisFieldsAdditional
    {
        
    }

    public class AxisFieldsAdditionalMetaData
    {
        [AllowHtml]
        public string stockNoteDesc { get; set; }

        [AllowHtml]
        public string priorityNote { get; set; }

        [Range(1, 999999999)]
        public int? breakQuantity1 { get; set; }

        [Range(1, 999999999)]
        public int? breakQuantity2 { get; set; }

        [Range(1, 999999999)]
        public int? breakQuantity3 { get; set; }
    }
}
