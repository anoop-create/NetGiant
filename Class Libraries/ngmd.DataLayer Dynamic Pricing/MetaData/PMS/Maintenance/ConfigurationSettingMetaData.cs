using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace netGiant.Intranet.DataLayer
{
    [MetadataType(typeof(ConfigurationMetaData))]
    public partial class configurationSetting
    {

    }

    public class ConfigurationMetaData
    {
        [Required(ErrorMessage="Setting Name is required")]
        public string settingName { get; set; }
        [Required(ErrorMessage = "Setting Value is required")]
        [AllowHtml]
        public string settingValue { get; set; }
        [Required(ErrorMessage = "Section Name is required")]
        public string sectionName { get; set; }
    }
}
