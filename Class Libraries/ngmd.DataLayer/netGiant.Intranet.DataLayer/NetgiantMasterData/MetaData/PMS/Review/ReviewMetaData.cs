using System.ComponentModel.DataAnnotations;

namespace netGiant.Intranet.DataLayer.NetgiantMasterData
{
    [MetadataType(typeof(ReviewMetaData))]
    public partial class feefoFeedback
    {
        public string ProductName { get;set; }
        public string WebsiteName { get; set; }
    }

    public class ReviewMetaData
    {

    }
}
