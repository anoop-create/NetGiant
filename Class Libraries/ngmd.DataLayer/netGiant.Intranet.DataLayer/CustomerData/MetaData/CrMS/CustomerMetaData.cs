using System.ComponentModel.DataAnnotations;
using ExpressiveAnnotations.Attributes;

namespace netGiant.Intranet.DataLayer.CustomerData
{
    [MetadataType(typeof(CustomerMetaData))]
    public partial class Customer
    {
        public int AccountStatusId { get; set; }
    }

    public class CustomerMetaData
    {
        [Key]
        public int CustomerId { get; set; }

        public int WebsiteFk { get; set; }

        [AssertThat("AccountStatusId == 5 ? AccountNumber != '@' : true", ErrorMessage = "Account Number must be valid for approved accounts")]
        [RegularExpression(@"^(@|01/[0-9]+)$", ErrorMessage = "Account Number must be in the format 01/NNNNNN")]
        public string AccountNumber { get; set; }

        public int CustomerTypeId { get; set; }

        [Required(ErrorMessage = "Contact email address is required")]
        [RegularExpression(@"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{2,20}|[0-9]{1,3})(\]?)$", ErrorMessage = "Please enter a valid email address")]
        public string OriginalEmailAddress { get; set; }

}
}
