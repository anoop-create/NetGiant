using System.ComponentModel.DataAnnotations;

namespace netGiant.Intranet.DataLayer.NetgiantMasterData
{
    [MetadataType(typeof(CreditAccountMetaData))]
    public partial class CreditAccount
    {

    }

    public class CreditAccountMetaData
    {
        public string VatNumber { get; set; }


        [StringLength(10, ErrorMessage = "Maximum of 10 characters")]
        public string SageNominalAccount { get; set; }
    }

}
