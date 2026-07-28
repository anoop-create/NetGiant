using System.ComponentModel.DataAnnotations;

namespace netGiant.Intranet.DataLayer.NetgiantMasterData
{
    [MetadataType(typeof(EquipmentMetaData))]
    public partial class eqFamily
    {

    }

    public partial class EquipmentMetaData
    {
        [Required(ErrorMessage = "Description is required")]
        public string description { get; set; }
        [Required(ErrorMessage = "Manufacturer is required")]
        public string manufacturerFK { get; set; }
        
    }
}
