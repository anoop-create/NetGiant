using System.ComponentModel.DataAnnotations;
using ExpressiveAnnotations.Attributes;
using System;

namespace netGiant.Intranet.DataLayer.NetgiantMasterData
{
    [MetadataType(typeof(DeliverySupplierCodeMetaData))]
    public partial class deliverySupplierCode
    {
    }

    public partial class DeliverySupplierCodeMetaData
    {
        [Required(ErrorMessage = "Please provide a code")]
        public string providerItemCode { get; set; }

    }

  
}
