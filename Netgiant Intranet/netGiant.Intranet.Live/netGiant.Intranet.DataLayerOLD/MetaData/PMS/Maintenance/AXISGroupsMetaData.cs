using System.ComponentModel.DataAnnotations;

namespace netGiant.Intranet.DataLayer
{
    [MetadataType(typeof(AXISGroupsMetaData))]
    public partial class AXISGroups
    {
        
    }
    
    public class AXISGroupsMetaData
    {
        [Required(ErrorMessage="AXIS Group Name is required")]
        public string AXISGroupName { get; set; }
        [Required(ErrorMessage="AXIS Group No is required")]
        public string AXISGroupNo { get; set; }
        [Required(ErrorMessage = "P-eBusiness Group is required")]
        public string p_eBusinessGroup { get; set; }
        [Required(ErrorMessage = "S-eBusiness Group is required")]
        public string s_eBusinessGroup { get; set; }
        [Required(ErrorMessage="Website is required")]
        public int websiteFK { get; set; }
        [Required(ErrorMessage = "Category Code is required")]
        public int categoryCodeFK { get; set; }
    }
}
