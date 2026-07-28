using System.ComponentModel.DataAnnotations;

namespace netGiant.Intranet.DataLayer.NetgiantMasterData
{
    [MetadataType(typeof(FtpDetailMetaData))]
    public partial class ftpDetails
    {
                
    }
    
    public class FtpDetailMetaData
    {
        [Required(ErrorMessage="Ftp Host is required")]
        public string ftpHost { get; set; }
        [Required(ErrorMessage = "Ftp User is required")]
        public string ftpUser { get; set; }
        [Required(ErrorMessage = "Ftp Password is required")]
        public string ftpPassword { get; set; }
        [Required(ErrorMessage = "Ftp file name is required")]
        public string ftpFilename { get; set; }
        [Required(ErrorMessage = "Provider is required")]
        public int providerFK { get; set; }
    }
}
