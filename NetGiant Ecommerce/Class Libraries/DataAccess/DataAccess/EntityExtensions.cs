using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DataAccess.EntityFramework
{
    [MetadataType(typeof(SagePayTokenExtensions))]
    public partial class SagePayToken
    {
        public string CardName { get; set; }
    }

    public class SagePayTokenExtensions
    {
    }

    public partial class VoucherPromo
    {
        public List<int> Groups { get; set; } = new List<int>();
        public List<int> Categories { get; set; } = new List<int>();
        public bool SendEmail { get; set; } = false;
        public string Email { get; set; } = "";
        public string VoucherTypeName { get; set; }
    }
}
