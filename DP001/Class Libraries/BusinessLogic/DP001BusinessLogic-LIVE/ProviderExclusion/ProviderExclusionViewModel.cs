using DP001DataAccess.Entities;
using DP001BusinessLogic.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using DP001DataAccess.Utilities;

namespace DP001BusinessLogic.ViewModels
{
    public class ProviderExclusionViewModel
    {
        public ProviderExclusionViewModel()
        {

        }

        public ProviderExclusionViewModel(int channelId)
        {
            _channelId = channelId;
        }

        public List<ProviderExclusion> ProviderExclusionList { get; set; }
        public ProviderExclusion ProviderExclusionEntry { get; set; }
        public List<SelectListItem> AllProviders { get; set; }

        private int _channelId;
        public Channel Channel { get; set; }
        public TenantSetting Tenant { get; set; }
        public int InventoryId { get; set; }

        public ProviderExclusionViewModel GetExclusions()
        {
            var crud = new CrudProviderExclusion();
            ProviderExclusionList = crud.Read(_channelId);

            return this;
        }

        public ProviderExclusionViewModel New()
        {
            ProviderExclusionEntry = new ProviderExclusion();
            ProviderExclusionEntry.Lookup = new Lookup();

            AllProviders = SharedViewModel.GetLookupList("FileType");

            return this;
        }

        //public ProviderExclusionViewModel Edit(int id)
        //{
        //    var crud = new CrudProviderExclusion();

        //    ProviderExclusionEntry = crud.Read(x => x.ChannelFK == _channelId
        //        && x.ProviderExclusionID == id)
        //        .FirstOrDefault();

        //    AllProviders = SharedViewModel.GetLookupList("FileType");

        //    return this;
        //}

        public SaveReturn Create()
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = false;
            bool isValid = true;

            //Validation Checks Go Here
            var crud = new CrudProviderExclusion();
            ProviderExclusion pe = crud.Read(x => x.ChannelFK == ProviderExclusionEntry.ChannelFK && 
                x.FileTypeFK == ProviderExclusionEntry.FileTypeFK && 
                x.ProviderFK == ProviderExclusionEntry.ProviderFK && 
                x.BrandName == ProviderExclusionEntry.BrandName && 
                x.ManufacturerPartNo == ProviderExclusionEntry.ManufacturerPartNo && 
                x.ClientProductID == ProviderExclusionEntry.ClientProductID).FirstOrDefault();
            if (pe != null)
            {
                isValid = false;
                sr.Message = "This record has already been excluded";
            }
            if (!isValid)
            {
                sr.IsSuccess = false;
                return sr;
            }

            try
            {
                crud.Create(ProviderExclusionEntry);

                CrudCompetitorInventory crudCe = new CrudCompetitorInventory();
                CrudLookup crudLookup  = new CrudLookup();
                int dormant = crudLookup.Read(x => x.LookupType.LookupTypeName == "Status" && x.LookupName == "Dormant").FirstOrDefault().LookupID;

                CompetitorInventory ci = crudCe.Read(x => x.ChannelFK == ProviderExclusionEntry.ChannelFK
                                        && x.CompetitorInventoryID == InventoryId)
                                        .FirstOrDefault();
                if (ci != null)
                {
                    ci.StatusFK = dormant;
                    crudCe.Update(ci);
                }               

                sr.IsSuccess = true;
            }
            catch (Exception e)
            {
                sr.Message = e.Message;
            }

            return sr;
        }

        public SaveReturn Update(ProviderExclusion providerExclusionEntry)
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = false;
            bool isValid = true;

            //Validation Checks Go Here

            if (!isValid)
            {
                sr.IsSuccess = false;
                return sr;
            }

            try
            {
                var crud = new CrudProviderExclusion();

                var isFound = crud.Read(x => x.ChannelFK == providerExclusionEntry.ChannelFK
                    && x.ProviderExclusionID == providerExclusionEntry.ProviderExclusionID).Count > 0;

                if (isFound)
                {
                    crud.Update(providerExclusionEntry);
                    sr.IsSuccess = true;
                }
                else
                {
                    sr.Message = "Record does not exist or you do not have persmission to change it";
                }
            }
            catch (Exception e)
            {
                sr.Message = e.Message;
            }

            return sr;
        }

        public SaveReturn Delete(int id)
        {
            var saveReturn = new SaveReturn();
            var crud = new CrudProviderExclusion();

            var deleteRecord = crud.Read(x => x.ChannelFK == _channelId
                && x.ProviderExclusionID == id).FirstOrDefault();

            if (deleteRecord != null)
            {
                crud.Delete(deleteRecord);

                saveReturn.IsSuccess = true;
            }
            else
            {
                saveReturn.IsSuccess = false;
                saveReturn.Message = "Exclusion not found or you do not have permission to delete it";
            }

            return saveReturn;
        }
    }
}

