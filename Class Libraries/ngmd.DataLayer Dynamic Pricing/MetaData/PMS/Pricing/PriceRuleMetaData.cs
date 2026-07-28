using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using netGiant.Intranet.DataLayer;
using System.ComponentModel.DataAnnotations;

namespace netGiant.Intranet.DataLayer
{
    [MetadataType(typeof(PriceRuleMetaData))]
    public partial class priceRule
    {
    }

    public class PriceRuleMetaData
    {
        //[DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:N1}")]
        //public object desiredMargin { get; set; }
        //[DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:N1}")]
        //public object minMargin { get; set; }
        //[DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:N1}")]
        //public object maxMargin { get; set; }
        //[DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:N1}")]
        //public object competitorsToBeat { get; set; }
        //[DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:N1}")]
        //public object nudge { get; set; }
    }
}
