using System.ComponentModel.DataAnnotations;

namespace netGiant.Intranet.DataLayer
{
    [MetadataType(typeof(productSubStatusMetaData))]
    public partial class productSubStatus
    {
        
    }

    public class productSubStatusMetaData
    {
        //[Required(ErrorMessage="Product Sub Status Name is required")]
        //public string productSubStatusName { get; set; }
    }
}
