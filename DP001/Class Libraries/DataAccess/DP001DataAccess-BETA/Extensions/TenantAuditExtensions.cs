using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DP001DataAccess.Entities
{
    [MetadataType(typeof(TenantAuditExtensions))]
    public partial class TenantAudit
    {
        public string TypeName { get; set; }
        public string TenantName { get; set; }
    }

    public class TenantAuditExtensions
    {
        public int TenantAuditID { get; set; }

        public int ChannelFK { get; set; }

        public System.DateTime Timestamp { get; set; }

        public string ObjectName { get; set; }

        public string OldValues { get; set; }

        public string NewValues { get; set; }

        public string Type { get; set; }

        public string UserName { get; set; }

    }
}
