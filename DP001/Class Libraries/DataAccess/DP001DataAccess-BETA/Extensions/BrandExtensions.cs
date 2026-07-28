using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DP001DataAccess.Entities
{
    [MetadataType(typeof(BrandExtensions))]
    public partial class Brand
    {
    }

    public class BrandExtensions
    {
        [DisplayFormat(NullDisplayText = "None")]
        public string BrandName { get; set; }
    }
}
