using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DP001BusinessLogic.Shared
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// Truncate the string to the specified max length. Returns the original string when length is less than the max length.
        /// </summary>
        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }  
    }
}
