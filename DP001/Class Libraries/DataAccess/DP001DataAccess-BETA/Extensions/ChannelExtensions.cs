using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExpressiveAnnotations.Attributes;

namespace DP001DataAccess.Entities
{
    [MetadataType(typeof(ChannelExtensions))]
    public partial class Channel
    {
        public bool IsDefault { get; set; } = false;
    }

    public class ChannelExtensions
    {
        [Required(ErrorMessage = "Channel Name is required")]
        [Display(Name = "Channel Name")]
        public string ChannelName { get; set; }

        public bool IsActive { get; set; }

        public bool JobInProgress { get; set; }

        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        [Display(Name = "Output File Email Address")]
        public string OutputFileEmailAddress { get; set; }

        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        [Display(Name = "Notifications Email Address")]
        public string NotificationsEmailAddress { get; set; }

        [Required(ErrorMessage = "Rounding Rule is required")]
        [Display(Name = "Rounding Rule")]
        public int RoundingGroupFK { get; set; }
    }
}
