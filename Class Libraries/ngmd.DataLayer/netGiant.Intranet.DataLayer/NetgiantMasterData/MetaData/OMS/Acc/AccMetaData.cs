using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace netGiant.Intranet.DataLayer.NetgiantMasterData
{ 
    public partial class Account
    {
        public string AccountType { get; set; }
        public string AccountStatus { get; set; }
        public string PaymentMethod { get; set; }
        public string WebsiteName { get; set; }
        public string CustomerGroup { get; set; }
        public string CreditStatus { get; set; }
        public string OrderSource { get; set; }
    }

    [MetadataType(typeof(AccMetaData))]
    public partial class Account { };

    public class AccMetaData
    {
        [StringLength(10, ErrorMessage ="Maximum of 10 characters"), Required]
        public string ShortName { get; set; }
      
    }
}
