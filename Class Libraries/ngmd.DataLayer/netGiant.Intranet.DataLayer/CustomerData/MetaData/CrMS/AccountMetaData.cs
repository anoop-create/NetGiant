using System;
using System.ComponentModel.DataAnnotations;
using ExpressiveAnnotations.Attributes;

namespace netGiant.Intranet.DataLayer.CustomerData
{
    [MetadataType(typeof(AccountMetaData))]
    public partial class Account
    {
        public int CustomerTypeId { get; set; }
    }

    public class AccountMetaData
    {
        [Key]
        public int AccountId { get; set; }

        [Required(ErrorMessage = "Customer is a required field")]
        public int CustomerFk { get; set; }

        public int StatusId { get; set; }

        [Required(ErrorMessage = "Organisation type is required")]
        public int OrganisationTypeId { get; set; }

        [Required(ErrorMessage = "Sector is required")]
        public int SectorId { get; set; }

        [Required(ErrorMessage = "Trading or organisation name is required")]
        public string TradingName { get; set; }

        [Required(ErrorMessage = "Contact full name is required")]
        public string ContactName { get; set; }

        [Required(ErrorMessage = "Contact email address is required")]
        [RegularExpression(@"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{2,20}|[0-9]{1,3})(\]?)$", ErrorMessage = "Please enter a valid email address")]
        public string ContactEmailAddress { get; set; }

        [Required(ErrorMessage = "Contact telephone number is required")]
        [RegularExpression(@"^(?:(?:\(?(?:0(?:0|11)\)?[\s-]?\(?|\+)44\)?[\s-]?(?:\(?0\)?[\s-]?)?)|(?:\(?0))(?:(?:\d{5}\)?[\s-]?\d{4,5})|(?:\d{4}\)?[\s-]?(?:\d{5}|\d{3}[\s-]?\d{3}))|(?:\d{3}\)?[\s-]?\d{3}[\s-]?\d{3,4})|(?:\d{2}\)?[\s-]?\d{4}[\s-]?\d{4}))(?:[\s-]?(?:x|ext\.?|\#)\d{3,4})?$", ErrorMessage = "Please enter a valid telephone number.")]
        public string ContactTelephoneNo { get; set; }

        [Required(ErrorMessage = "Number of staff is required")]
        public short TotalStaffCountId { get; set; }

        [Required(ErrorMessage = "Number of staff ordering is required")]
        public short OrderStaffCountId { get; set; }

        [Required(ErrorMessage = "Estimated monthly spend is required")]
        [DisplayFormat(DataFormatString = "{0:N}", ApplyFormatInEditMode = false)]
        public decimal EstMonthlySpend { get; set; }

        [RequiredIf("StatusId == 5", ErrorMessage = "Please enter a Credit Limit for the Approved account")]
        [DisplayFormat(DataFormatString = "{0:N}", ApplyFormatInEditMode = false)]
        public decimal CreditLimit { get; set; }


        [RequiredIf("CustomerTypeId != 3", ErrorMessage = "Company registration number is required")]
        public string CompanyRegNo { get; set; }

        public string CompanyVatNo { get; set; }

        public bool? AcceptStandardTerms { get; set; } = true;

        public bool? AcceptCreditTerms { get; set; } = true;

        [DataType(DataType.Date)]
        public DateTime DateOfApplication { get; set; }

        [DataType(DataType.Date)]
        public DateTime DateLastUpdated { get; set; }
    }
}

