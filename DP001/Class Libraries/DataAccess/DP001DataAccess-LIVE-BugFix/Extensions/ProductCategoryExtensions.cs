using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DP001DataAccess.Entities
{
    [MetadataType(typeof(ProductCategoryExtensions))]
    public partial class ProductCategory
    {
    }

    public class ProductCategoryExtensions
    {
        [DisplayFormat(NullDisplayText = "None")]
        public string CategoryName { get; set; }
    }
}
