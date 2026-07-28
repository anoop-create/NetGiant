using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DP001DataAccess.Entities
{
    [MetadataType(typeof(TenantSettingExtensions))]
    public partial class TenantSetting
    {
    }

    public class TenantSettingExtensions
    {
        [Required(ErrorMessage = "Email Address is required")]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        [Display(Name = "Email Address")]
        public string ContactEmail { get; set; }
    }
}
