using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace netGiant.Intranet.DataLayer
{
    [MetadataType(typeof(ManufacturerNotesMetaData))]
    public partial class manufacturerNotes
    {

    }

    public class ManufacturerNotesMetaData
    {
        [Key]
        public int manufacturerNotesID { get; set; }
        [Required(ErrorMessage = "Website is a required field")]
        public int websiteFK { get; set; }
        [Required(ErrorMessage = "Cartridge Type is a required field")]
        public int eqCartridgeTypeFK { get; set; }
        [AllowHtml]
        [Required(ErrorMessage = "Note is a required field")]
        public string note { get; set; }
    }
}
