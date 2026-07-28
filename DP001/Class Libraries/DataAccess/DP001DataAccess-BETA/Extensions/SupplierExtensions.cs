using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExpressiveAnnotations.Attributes;

namespace DP001DataAccess.Entities
{
    [MetadataType(typeof(SupplierExtensions))]
    public partial class Supplier
    {
    }

    public class SupplierExtensions
    {
        public Nullable<int> FTPSettingsFK { get; set; }

        [Required(ErrorMessage = "Supplier Name is required")]
        [Display(Name = "Supplier Name")]
        public string SupplierName { get; set; }

    }
}
