using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace netGiant.Intranet.DataLayer.NetgiantMasterData
{ 
    public partial class Address
    {
        public string AddressType { get; set; }
    }

    [MetadataType(typeof(AddressMetaData))]
    public partial class Address { };

    class AddressMetaData
    {

    }
}
