using System.ComponentModel.DataAnnotations;

namespace netGiant.Intranet.DataLayer.NetgiantMasterData
{
    [MetadataType(typeof(DeliveryZoneMetaData))]
    public partial class deliveryZone
    {

    }

    public class DeliveryZoneMetaData
    {
        [Required(ErrorMessage = "Website is required")]
        public string WebsiteFK { get; set; }

        [Required(ErrorMessage = "Zone Name is required")]
        public string ZoneName { get; set; }
    }

    [MetadataType(typeof(DeliveryServiceMetaData))]
    public partial class deliveryService
    {

    }

    public class DeliveryServiceMetaData
    {
        [Required(ErrorMessage = "Website is required")]
        public string WebsiteFK { get; set; }

        [Required(ErrorMessage = "Service Name is required")]
        public string ServiceName { get; set; }

        [Required(ErrorMessage = "Stock Ref is required")]
        public string StockRef { get; set; }

        [Required(ErrorMessage = "Price is required")]
        public string Price { get; set; }

        [Required(ErrorMessage = "Info Message is required")]
        public string InfoMessage { get; set; }
    }
}
