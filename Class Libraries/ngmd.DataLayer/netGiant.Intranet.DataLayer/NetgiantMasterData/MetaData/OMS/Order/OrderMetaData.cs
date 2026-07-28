using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace netGiant.Intranet.DataLayer.NetgiantMasterData
{
    public partial class Order
    {
        public string OrderStatus { get; set; }
        public string PaymentMethod { get; set; }
        public string CustomerGroup { get; set; }
        public string DeliveryAddress { get; set; }
    }

    [MetadataType(typeof(OrderMetaData))]
    public partial class Order { };

    public class OrderMetaData
    {
        [
            StringLength(6, ErrorMessage = "Maximum of 6 characters"),
            Required
        ]
        public string InternalOrderNo { get; set; }
    }
}
