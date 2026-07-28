using System.ComponentModel.DataAnnotations;

namespace netGiant.Intranet.DataLayer.NetgiantMasterData
{
    [MetadataType(typeof(ProductPriceMetaData))]
    public partial class productPrice
    {

    }

    public class ProductPriceMetaData
    {
        [DisplayFormat(DataFormatString = "{0:C}")]
        public float price { get; set; }
        [DisplayFormat(DataFormatString = "{0:C}")]
        public float cheapestCostPrice { get; set; }
    }
}