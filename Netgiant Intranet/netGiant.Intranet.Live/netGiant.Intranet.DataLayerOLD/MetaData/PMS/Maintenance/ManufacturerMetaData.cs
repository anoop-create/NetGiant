using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace netGiant.Intranet.DataLayer
{
    [MetadataType(typeof(ManufacturerMetaData))]
    public partial class manufacturer
    {

    }
    
    public class ManufacturerMetaData
    {
        [Required(ErrorMessage="Manufacturer's name is required")]
        public string manufacturerName { get; set; }
    }
}
