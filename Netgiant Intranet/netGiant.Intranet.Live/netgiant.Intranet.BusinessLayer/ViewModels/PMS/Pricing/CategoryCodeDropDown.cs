using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Pricing
{
    public class CategoryCodeDropDown
    {
        public string Text { get; set; }
        public int Value { get; set; }
        public bool NoCategoryFallback { get; set; }
        public int ProductCount { get; set; }
    }
}
