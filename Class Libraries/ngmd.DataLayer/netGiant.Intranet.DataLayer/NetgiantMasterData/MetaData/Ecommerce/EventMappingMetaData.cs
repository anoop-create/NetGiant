using System.ComponentModel.DataAnnotations;

namespace netGiant.Intranet.DataLayer.NetgiantMasterData
{
    [MetadataType(typeof(EventMappingMetaData))]
    public partial class EventMapping
    {

    }

    public partial class EventMappingMetaData
    {
        [Required(ErrorMessage = "Please select Event Name")]
        public int EventFk  { get; set; }

        [Required(ErrorMessage = "Please select Mapped CMS Entry")]
        public int MappedCmsEntryFk { get; set; }
    }
}
