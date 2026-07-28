using System.ComponentModel.DataAnnotations;

namespace netGiant.Intranet.DataLayer.CustomerData
{
    [MetadataType(typeof(BillingMetaData))]
    public partial class Billing
    {

    }

    public class BillingMetaData
    {
        [Key]
        public int BillingId { get; set; }

        public int CustomerFk { get; set; }

        [Required(ErrorMessage = "Contact name is required")]
        public string ContactName { get; set; }

        [Required(ErrorMessage = "Contact email address is required")]
        [RegularExpression(@"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{2,20}|[0-9]{1,3})(\]?)$", ErrorMessage = "Please enter a valid email address")]
        public string ContactEmailAddress { get; set; }

        [Required(ErrorMessage = "Contact telephone number is required")]
        [RegularExpression(@"^(?:(?:\(?(?:0(?:0|11)\)?[\s-]?\(?|\+)44\)?[\s-]?(?:\(?0\)?[\s-]?)?)|(?:\(?0))(?:(?:\d{5}\)?[\s-]?\d{4,5})|(?:\d{4}\)?[\s-]?(?:\d{5}|\d{3}[\s-]?\d{3}))|(?:\d{3}\)?[\s-]?\d{3}[\s-]?\d{3,4})|(?:\d{2}\)?[\s-]?\d{4}[\s-]?\d{4}))(?:[\s-]?(?:x|ext\.?|\#)\d{3,4})?$", ErrorMessage = "Please enter a valid telephone number.")]
        public string ContactTelephoneNo { get; set; }

        public string AddressLine1 { get; set; }

        public string AddressLine2 { get; set; }

        public string AddressLine3 { get; set; }

        public string AddressLine4 { get; set; }

        public string AddressLine5 { get; set; }

        [Required(ErrorMessage = "Postcode is required")]
        public string PostCode { get; set; }

        public string Country { get; set; }
    }
}
