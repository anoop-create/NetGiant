using DP001DataAccess.Entities;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DP001Website.Models
{
    public class ExternalLoginConfirmationViewModel
    {
        [Required]
        [Display(Name = "Email")]
        public string Email { get; set; }
    }

    public class ExternalLoginListViewModel
    {
        public string ReturnUrl { get; set; }
    }

    public class SendCodeViewModel
    {
        public string SelectedProvider { get; set; }
        public ICollection<System.Web.Mvc.SelectListItem> Providers { get; set; }
        public string ReturnUrl { get; set; }
        public bool RememberMe { get; set; }
    }

    public class VerifyCodeViewModel
    {
        [Required]
        public string Provider { get; set; }

        [Required]
        [Display(Name = "Code")]
        public string Code { get; set; }
        public string ReturnUrl { get; set; }

        [Display(Name = "Remember this browser?")]
        public bool RememberBrowser { get; set; }

        public bool RememberMe { get; set; }
    }

    public class ForgotViewModel
    {
        [Required]
        [Display(Name = "Email")]
        public string Email { get; set; }
    }

    public class LoginViewModel
    {
        [Required]
        [Display(Name = "Email")]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }

    public class RegisterViewModel
    {
        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Required]
        [Display(Name = "Company Name")]
        public string CompanyName { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        //[Required]
        [Display(Name = "Title")]
        public string BillingTitle { get; set; }

        //[Required]
        [Display(Name = "Billing First Name")]
        public string BillingFirstName { get; set; }

        //[Required]
        [Display(Name = "Billing Last Name")]
        public string BillingLastName { get; set; }

        //[Required]
        [Display(Name = "Billing Country")]
        public string BillingCountry { get; set; }

        //[Required]
        [Display(Name = "Billing Address Line 1")]
        public string BillingAddress1 { get; set; }

        //[Required]
        [Display(Name = "Billing Address Line 2")]
        public string BillingAddress2 { get; set; }

        //[Required]
        [Display(Name = "City")]
        public string BillingCity { get; set; }

        //[Required]
        [Display(Name = "State/Province")]
        public string BillingStateProvince { get; set; }

        //[Required]
        [Display(Name = "Postal Code")]
        public string BillingPostalCode { get; set; }

        //[Required]
        [Display(Name = "Phone Number")]
        public string BillingPhoneNumber { get; set; }

        //[Required]
        [Display(Name = "Card Type")]
        public string CardType { get; set; }

        //[Required]
        [Display(Name = "Card Number")]
        public string CardNumber { get; set; }

       // [Required]
        [Display(Name = "Expiry Date")]
        public string CardExpiryDate { get; set; }

        //[Required]
        [Display(Name = "CSV")]
        public string CardCSVNumber { get; set; }

        //[Required]
        [Display(Name = "Contract Type")]
        public string ContractType { get; set; }

        public int? StageIndicator { get; set; }
        public string ValidateFields { get; set; }

        [Required]
        public int TenantID { get; set; }

        public List<System.Web.Mvc.SelectListItem> TenantList { get; set; }
    }

    public class ResetPasswordViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        public string Code { get; set; }
    }

    public class ResetMyPasswordViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        public string Code { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "CurrentPassword")]
        public string CurrentPassword { get; set; } 
    }

    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }
    }
}
