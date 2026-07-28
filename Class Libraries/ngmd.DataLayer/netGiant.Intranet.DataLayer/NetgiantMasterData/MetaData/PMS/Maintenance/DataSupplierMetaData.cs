using System.ComponentModel.DataAnnotations;

namespace netGiant.Intranet.DataLayer.NetgiantMasterData
{
    [MetadataType(typeof(DataSupplierMetaData))]
    public partial class dataSupplier
    {
        
    }
    
    public class DataSupplierMetaData
    {
        [Required(ErrorMessage="Data Supplier's Name is requird")]
        public string dataSupplierName { get; set; }
    }
}
