using System.ComponentModel.DataAnnotations;

namespace netGiant.Intranet.DataLayer.NetgiantMasterData
{
    [MetadataType(typeof(CrossSellingLinkMetaData))]
    public partial class crossSellingLink
    {
        
    }

    public class CrossSellingLinkMetaData
    {
        [Required(ErrorMessage = "Please select a valid product")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid product")]
        public int aProductFK { get; set; }

        [Required(ErrorMessage = "Please select a valid product")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid product")]
        public int bProductFK { get; set; }

        [Required(ErrorMessage = "Please select a type")]
        public int crossSellingLinkTypeFK { get; set; }
    }
}
