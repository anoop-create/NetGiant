using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace netGiant.Intranet.BusinessLayer.Utilities
{
    public class HTMLUtilities
    {
        public static string BRForDisplay(string FixThis)
        {
            if (FixThis != null)
            {
                return FixThis.Replace(Environment.NewLine, "<br />");
            }
            return FixThis;
        }
    }
}
