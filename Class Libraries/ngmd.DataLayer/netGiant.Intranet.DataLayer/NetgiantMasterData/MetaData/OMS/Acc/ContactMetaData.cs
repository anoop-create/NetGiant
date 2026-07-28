using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace netGiant.Intranet.DataLayer.NetgiantMasterData
{
    public partial class Contact
    {
        public string ContactStatus { get; set; }
    }

    [MetadataType(typeof(ContactMetaData))]
    public partial class Contact{ };

    public class ContactMetaData
    {
        [StringLength(25, ErrorMessage = "Maximum of 25 characters")]
        public string Title { get; set; }


        [Required]
        public string FirstName { get; set; }


        [Required]
        public string LastName { get; set; }


        [StringLength(20, ErrorMessage = "Maximum of 20 characters")]
        public string TelephoneNumber { get; set; }


        [StringLength(20, ErrorMessage = "Maximum of 20 characters")]
        public string FaxNumber { get; set; }


        [StringLength(100, ErrorMessage = "Maximum of 100 characters")]
        public string Email { get; set; }
    }
}

