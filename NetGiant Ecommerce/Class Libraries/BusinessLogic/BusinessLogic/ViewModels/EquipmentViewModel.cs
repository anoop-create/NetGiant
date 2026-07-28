using System.Collections.Generic;
using System.Linq;
using DataAccess.EntityFramework;
using System.Configuration;
using System.Globalization;
using System.Web.Mvc;

namespace BusinessLogic.ViewModels
{
    public class EquipmentViewModel : WizardViewModel
    {
        public EquipmentViewModel()
        {
            EquipmentData = DataCache.GetSectionData("EquipmentData");
            if (IsCompatibleSaleActive || IsOEMSaleActive || IsStationerySaleActive)
            {
                EquipmentData["FeatureBackground"] = Utilities.GetItemFromDict(SaleData, "FeatureBackground");
            }
        }

        public void SetupWizard(string typename = null, string manuname = null, string familyname = null)
        {
            if (familyname != null)
            {
                Family = EntityAccess.ReadFamily(x => x.description.Contains(familyname.Replace("-", " "))).FirstOrDefault();
            }

            int typeId = 0;
            CartridgeTypeName = "";
            if (typename != null)
            {
                typename = typename.Replace('-', ' ');
                GetCartridgeTypes();
                CartridgeType = CartridgeTypes.Find(x => x.LookupName.ToLower() == typename);
                if (CartridgeType != null)
                {
                    typeId = CartridgeType.AltLookupId.Value;
                    CartridgeTypeName = CartridgeType.LookupName;
                }
            }
            if (manuname != null)
            {
                Manufacturer = EntityAccess.ReadManufacturer(x => x.manufacturerName == manuname.Replace("-", " ")).FirstOrDefault();
                if (Manufacturer != null)
                {
                    int w = int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString());
                    Manufacturer.manufacturerNotes = Manufacturer.manufacturerNotes
                        .Where(x => x.eqCartridgeTypeFK == typeId && x.websiteFK == w).ToList();
                }
            }            
        }

        public Dictionary<string, string> EquipmentData { get; set; }
        public List<SelectListItem> FrequencyList { get; set; }
        public int PagesPrintedPerDay = 50;
        public int PageCoverage = 5;
        public int Frequency = 1;
        public int TotalPagesPrinted = 0;
        public int TotalEndurance = 0;

        public void GetMeta(LookupNgmd type, manufacturer manu, eqFamily family)
        {
            MetaData = new Dictionary<string, string>();
            int w = int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString());

            string metatitle = "";
            string metadesc = "";
            if (manu != null)
            {
                var manuNote = manu.manufacturerNotes.FirstOrDefault(x => x.eqCartridgeTypeFK == type.AltLookupId.Value && x.websiteFK == w);
                if (manuNote != null)
                    metatitle = manuNote.metaTitle;

                if (manuNote != null)
                    metadesc = manuNote.metaDescription;

                if (family != null)
                {                 
                    if (family.description == manu.manufacturerName)
                    {
                        metatitle = metatitle.Replace(manu.manufacturerName, family.description + " Family");
                        metadesc = metadesc.Replace(manu.manufacturerName, family.description + " Family");
                    }
                    else
                    {
                        metatitle = metatitle.Replace(manu.manufacturerName, family.description);
                        metadesc = metadesc.Replace(manu.manufacturerName, family.description);
                    }
                }
            }
            else
            {
                manufacturerNote note = EntityAccess.ReadManufacturerNotes(x => x.eqCartridgeTypeFK == type.AltLookupId.Value && x.manufacturerFK == null).FirstOrDefault();
                metatitle = note.metaTitle;
                metadesc = note.metaDescription;
            }

            MetaData.Add("Title", metatitle);
            MetaData.Add("Description", metadesc);
        }

        public List<SelectListItem> BuildFrequencyList()
        {
            List<SelectListItem> frequencies = new List<SelectListItem>();

            frequencies.Add(new SelectListItem() { Text = "Per Day", Value = "1" });
            frequencies.Add(new SelectListItem() { Text = "Per Week", Value = "2" });
            frequencies.Add(new SelectListItem() { Text = "Per Month", Value = "3" });
            frequencies.Add(new SelectListItem() { Text = "Per Year", Value = "4" });

            return frequencies;
        }
    }
}
