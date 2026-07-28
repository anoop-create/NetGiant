using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace netGiant.Intranet.DataLayer.NetgiantMasterData
{
    [MetadataType(typeof(EventMetaData))]
    public partial class Event
    {

    }

    public partial class EventMetaData
    {
        [Required(ErrorMessage = "Event Name is required")]
        public string EventName { get; set; }
        [DisplayFormat(ConvertEmptyStringToNull = false)]
        public string Description { get; set; }
    }
}
