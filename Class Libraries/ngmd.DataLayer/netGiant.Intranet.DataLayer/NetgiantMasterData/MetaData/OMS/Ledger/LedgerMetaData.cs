using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace netGiant.Intranet.DataLayer.NetgiantMasterData    
{
    public partial class Ledger
    {
        public string LedgerType { get; set; }
        public string LedgerTransType { get; set; }
        public string CustomerGroup { get; set; }
        public string SupplierGroup { get; set; }
    }
}
