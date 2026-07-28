using System.ComponentModel.DataAnnotations;

namespace netGiant.Intranet.DataLayer.NetgiantMasterData
{
    [MetadataType(typeof(actionLinkMetaData))]
    public partial class actionLink
    {
        public string parentLevelText { get; set; }
        public string topParent { get; set; }
    }
    
    public class actionLinkMetaData
    {
        [Required(ErrorMessage = "Title is required")]
        public string actionLinkDesc { get; set; }
    }
}
