using System.ComponentModel.DataAnnotations;
using ExpressiveAnnotations.Attributes;
using System;

namespace netGiant.Intranet.DataLayer.NetgiantMasterData
{
    [MetadataType(typeof(FaqMetaData))]
    public partial class Faq
    {
    }

    public partial class FaqMetaData
    {
        [Range(0, 100, ErrorMessage = "Priority must be in the range 0 to 100")]
        public string Priority { get; set; }

    }
}
