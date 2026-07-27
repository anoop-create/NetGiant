using DataAccess.EntityFramework;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace BusinessLogic.ViewModels
{
    public class WizardViewModel : CommonViewModel
    {
        public WizardViewModel()
        {
            WizardData = DataCache.GetSectionData("WizardData");
            if (IsCompatibleSaleActive || IsOEMSaleActive || IsStationerySaleActive)
            {
                WizardData["FeatureBackground"] = Utilities.GetItemFromDict(SaleData, "FeatureBackground");
            }
            WizDropDowns = new WizardLists();
        }

        public manufacturer Manufacturer = null;
        public eqFamily Family = null;
        public LookupNgmd CartridgeType = null;
        public string CartridgeTypeName = "";
        public string ManufacturerName = "";
        public Dictionary<string, string> WizardData { get; set; }
        public WizardLists WizDropDowns { get; set; }
        public bool HasAlternateType { get; set; } = false;

        public void GetWizardLists(string typename = "", int manufacturerId = 0, int familyId = 0)
        {
            WizDropDowns.ManufacturerList = DataCache.GetManufacturers(typename);
            WizDropDowns.FamilyList = DataCache.GetFamilies(typename, manufacturerId);
            WizDropDowns.EquipList = DataCache.GetEquipment(typename, manufacturerId, familyId);
            WizDropDowns.CartridgeTypeName = typename;

            //Check if Manufacturer has alternative Ink or Toner Cartridges
            if (manufacturerId != 0)
            {
                string alttypename = "ink cartridges";
                if (typename == "ink cartridges")
                {
                    alttypename = "toner cartridges";
                }
                if (DataCache.GetEquipment(alttypename, manufacturerId, 0).Count > 0)
                {
                    HasAlternateType = true;
                }
            }

            WizDropDowns.ManufacturerList = WizDropDowns.ManufacturerList.Select(x => new SelectListItem
            {
                Text = x.Text,
                Value = x.Value,
                Selected = false
            }).ToList();
            if (manufacturerId != 0)
            {
                var manufacturerOption = WizDropDowns.ManufacturerList.Find(x => x.Value == manufacturerId.ToString());
                if (manufacturerOption != null)
                    manufacturerOption.Selected = true;
            }

            WizDropDowns.FamilyList = WizDropDowns.FamilyList.Select(x => new SelectListItem
            {
                Text = x.Text,
                Value = x.Value,
                Selected = false
            }).ToList();
            if (familyId != 0)
            {
                var familyOption = WizDropDowns.FamilyList.Find(x => x.Value == familyId.ToString());
                if (familyOption != null)
                    familyOption.Selected = true;
            }
        }
    }
}
