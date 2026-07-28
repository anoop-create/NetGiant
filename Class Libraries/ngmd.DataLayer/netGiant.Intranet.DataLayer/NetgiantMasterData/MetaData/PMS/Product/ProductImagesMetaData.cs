using System.ComponentModel.DataAnnotations;

namespace netGiant.Intranet.DataLayer.NetgiantMasterData
{
    [MetadataType(typeof(ProductImageMetaData))]
    public partial class productImage
    {
        public string ACDModifier { get; set; }
    }

    public class ProductImageMetaData
    {

    }
}
