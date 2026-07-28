using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExpressiveAnnotations.Attributes;

namespace DP001DataAccess.Entities
{
    [MetadataType(typeof(ProviderExclusionExtensions))]
    public partial class ProviderExclusion
    {
        //public string ProviderName { get { return GetProvider(FileTypeFK, ProviderFK); } }
        public string ProviderName { get; set; }

        /// <summary>
        /// Retrieve the provider name
        /// </summary>
        /// <param name="providerTypeId"></param>
        /// <param name="providerId"></param>
        /// <returns></returns>
        //public static string GetProvider(int providerTypeId, int providerId)
        //{
        //    //Just doing Competitors for the moment but Suppliers should be added when that functionality is introduced

        //    using (DP001Entities db = new DP001Entities())
        //    {
        //        var comp = db.Competitors.Where(x => x.CompetitorID == providerId).FirstOrDefault();

        //        if (comp == null)
        //        {
        //            return "";
        //        }

        //        return comp.CompetitorName;
        //    }
        //}

        public string ExclusionType { get; set; }
        public string CompetitorDescription { get; set; }
    }

    public class ProviderExclusionExtensions
    {
        [Required(ErrorMessage = "Exclusion type is required")]
        public int ExclusionTypeFk { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Provider is required")]
        [Required(ErrorMessage = "Provider is required")]
        public int ProviderFK { get; set; }

        [RequiredIf("ExclusionType == 'Brand'", ErrorMessage = "Brand Name is required")]
        public string BrandName { get; set; }
    }

    
}
