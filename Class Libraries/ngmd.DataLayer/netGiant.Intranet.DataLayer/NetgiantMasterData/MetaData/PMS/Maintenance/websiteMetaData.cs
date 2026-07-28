using System.ComponentModel.DataAnnotations;

namespace netGiant.Intranet.DataLayer.NetgiantMasterData
{
    [MetadataType(typeof(websiteMetaData))]
    public partial class Website
    {
    }
    
    public class websiteMetaData
    {
        [Required(ErrorMessage="Website Name is required")]
        public string WebsiteName { get; set; }
        [Required(ErrorMessage="Website URL is required")]
        public string WebURL { get; set; }
        [Required(ErrorMessage="Friendly Name")]
        public string FriendlyName { get; set; }
    }
}
