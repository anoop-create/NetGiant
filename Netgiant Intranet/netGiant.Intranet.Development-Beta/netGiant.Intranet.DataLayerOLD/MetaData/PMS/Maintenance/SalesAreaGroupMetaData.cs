using System.ComponentModel.DataAnnotations;

namespace netGiant.Intranet.DataLayer
{
    [MetadataType(typeof(SalesAreaGroupMetaData))]
    public partial class salesAreaGroup
    {
        
    }
    
    public class SalesAreaGroupMetaData
    {        
        [Required(ErrorMessage="Sales Area Group Name is required")]
        public string salesAreaGroupName { get; set; }
    }
}
