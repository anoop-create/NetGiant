using System.ComponentModel.DataAnnotations;

namespace netGiant.Intranet.DataLayer.NetgiantMasterData
{
    [MetadataType(typeof(AxisFieldsMetaData))]
    public partial class AxisFields
    {
        public string Attribute1Description { get; set; }
        public string Attribute2Description { get; set; }
        public string Attribute3Description { get; set; }
        public string Attribute4Description { get; set; }
        public string Attribute5Description { get; set; }
        public string Attribute6Description { get; set; }
        public string Attribute7Description { get; set; }
        public string Attribute8Description { get; set; }
        public string Attribute9Description { get; set; }
        public string Attribute10Description { get; set; }
    }

    public class AxisFieldsMetaData
    {
        
    }
}
