using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExpressiveAnnotations.Attributes;

namespace DP001DataAccess.Entities
{
    [MetadataType(typeof(LogExtensions))]
    public partial class Log
    {
    }

    public class LogExtensions
    {
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy (HH:mm)}")]
        public DateTime DateTime { get; set; }
    }
}
