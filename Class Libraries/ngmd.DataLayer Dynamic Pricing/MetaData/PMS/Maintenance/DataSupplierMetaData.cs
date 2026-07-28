using System.ComponentModel.DataAnnotations;

namespace netGiant.Intranet.DataLayer
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
