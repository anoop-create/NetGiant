using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExpressiveAnnotations.Attributes;
using System.Web.Mvc;

namespace DP001DataAccess.Entities
{
    [MetadataType(typeof(ReportConfigurationExtensions))]
    public partial class ReportConfiguration
    {
        public string Owner { get; set; }
    }

    public class ReportConfigurationExtensions
    {
        [AllowHtml]
        public string ConfigurationValue { get; set; }

        [Required(ErrorMessage = "Description is required")]
        public string Name { get; set; }

        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
        public DateTime DateLastUpdated { get; set; }
    }
}
