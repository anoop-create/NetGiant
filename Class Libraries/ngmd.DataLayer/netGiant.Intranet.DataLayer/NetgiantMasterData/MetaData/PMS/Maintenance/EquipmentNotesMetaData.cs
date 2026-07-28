using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace netGiant.Intranet.DataLayer.NetgiantMasterData
{
    [MetadataType(typeof(EquipmentNotesMetaData))]
    public partial class equipmentNotes
    {

    }

    public class EquipmentNotesMetaData
    {
        [Key]
        public int equipmentNotesID { get; set; }
        [Required(ErrorMessage = "Website is a required field")]
        public int websiteFK { get; set; }
        [Required(ErrorMessage = "Equipment is a required field")]
        public int eqEquipmentFK { get; set; }
        [AllowHtml]
        public string note { get; set; }
    }
}
