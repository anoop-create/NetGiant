using DP001DataAccess.Entities;
using DP001BusinessLogic.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace DP001BusinessLogic.ViewModels
{
    public class TenantSettingsViewModel
    {
        public TenantSettingsViewModel()
        {

        }

        public TenantSettingsViewModel(TenantSetting tenant)
        {
            Tenant = tenant;
        }

        public TenantSetting Tenant { get; set; }
        public Channel ChannelEntry { get; set; }
        public List<FTPSetting> FtpSettingList { get; set; }
        public List<Schedule> ScheduleList { get; set; }
        public int CurrentChannelID { get; set; }
        public List<SelectListItem> TenantList { get; set; }
        public int SelectedTenantID { get; set; }
        public List<SelectListItem> RoundingGroups { get; set; }

        public SaveReturn Update(TenantSetting tenantSetting)
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = false;

            try
            {
                var crud = new CrudTenant();
                var isValid = crud.Read(x => x.TenantID == tenantSetting.TenantID).Count > 0;

                if (isValid)
                {
                    crud.Update(tenantSetting);
                    sr.IsSuccess = true;
                }
                else
                {
                    sr.Message = "Record does not exist";
                }
            }
            catch (Exception e)
            {
                sr.Message = e.Message;
            }

            return sr;
        }

        public TenantSettingsViewModel NewChannel()
        {
            ChannelEntry = new Channel();

            return this;
        }

        public TenantSettingsViewModel EditChannel(int channelId)
        {
            ChannelEntry = new Channel();
            ChannelEntry = Tenant.Channels.Where(x => x.ChannelID == channelId).FirstOrDefault();

            return this;
        }

        public SaveReturn CreateChannel()
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = false;
            int channelCount = 0;
            bool isValid = true;

            foreach (Channel ch in Tenant.Channels)
            {
                //Unique Channel name check
                if (ch.ChannelName.ToLower() == ChannelEntry.ChannelName.ToLower())
                {
                    sr.Message = "You cannot add a channel with the same name as an existing channel";
                    isValid = false;
                }
                if (ch.IsActive)
                {
                    channelCount += 1;
                }
            }
            //Channel limit check
            if (ChannelEntry.IsActive)
            {
                if (channelCount == Tenant.Contract.ChannelLimit)
                {
                    sr.Message = "The channel cannot be added as adding it will exceed your channel limit";
                    isValid = false;
                }
            }

            if (!isValid)
            {
                sr.IsSuccess = false;
                return sr;
            }

            if (ChannelEntry.SLActiveTypeFK == null)
            {
                var crudLookup = new CrudLookup();
                ChannelEntry.SLActiveTypeFK = crudLookup.Read(x => x.LookupType.LookupTypeName == "SkuudleLiteActiveType" &&
                    x.LookupName == "SL None").FirstOrDefault().LookupID;
            }

            try
            {
                var crud = new CrudChannel();
                crud.Create(ChannelEntry);
                sr.IsSuccess = true;
            }
            catch (Exception e)
            {
                sr.Message = e.Message;
            }

            return sr;
        }

        public SaveReturn UpdateChannel(Channel channelEntry)
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = false;
            int channelCount = 0;
            bool isValid = true;

            foreach (Channel ch in Tenant.Channels)
            {
                if (ch.IsActive)
                {
                    channelCount += 1;
                }

                if (ch.ChannelID == channelEntry.ChannelID)
                {
                    if (channelEntry.IsActive && !ch.IsActive)
                    {
                        channelCount += 1;
                    }
                    continue;
                }

                //Unique Channel name check
                if (ch.ChannelName.ToLower() == ChannelEntry.ChannelName.ToLower())
                {
                    sr.Message = "You cannot add a channel with the same name as an existing channel";
                    isValid = false;
                }

                if (ch.ChannelID == channelEntry.ChannelID
                    && ChannelEntry.IsActive
                    && ch.IsActive == false)
                {
                    channelCount += 1;
                }
            }
            //Channel limit check
            if (channelEntry.IsActive)
            {
                if (channelCount > Tenant.Contract.ChannelLimit)
                {
                    sr.Message = "Unable to activate channel as activating will exceed your channel limit";
                    isValid = false;
                }
            }

            if (!isValid)
            {
                sr.IsSuccess = false;
                return sr;
            }

            try
            {
                var crud = new CrudChannel();

                var isFound = crud.Read(x => x.TenantFK == channelEntry.TenantFK
                    && x.ChannelID == channelEntry.ChannelID).Count > 0;

                if (isFound)
                {
                    crud.Update(channelEntry);
                    sr.IsSuccess = true;
                }
                else
                {
                    sr.Message = "Record does not exist";
                }
            }
            catch (Exception e)
            {
                sr.Message = e.Message;
            }

            return sr;
        }

        public SaveReturn DeleteChannel(Channel channel)
        {
            //Channel deletion also needs to delete the following:
            //  All Price Rules associated with the Channel
            //  All Price History associated with the Channel
            //  All Schedules associated with the Channel
            //  All FieldMappings associated with the Channel
            //  All FTPSettings associated with the Channel
            //  All Competitors associated with the Channel
            //  All Suppliers associated with the Channel
            //  All ProductCategories associated with the Channel
            //  All SKUMappings associated with the Channel
            //  The CompetitorInventory associated with the Channel
            //  The SupplierInventory associated with the Channel
            //  The ProductInventory associated with the Channel

            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = false;
            bool isValid = true;

            var crud = new CrudChannel();

            if (channel.IsDefault)
            {
                sr.Message = "You cannot delete the default channel";
                isValid = false;
            }
            if (!isValid)
            {
                sr.IsSuccess = false;
                return sr;
            }

            try
            {
                if (channel != null)
                {
                    crud.Delete(channel);
                    sr.IsSuccess = true;
                }
                else
                {
                    sr.Message = "Record does not exist";
                }
            }
            catch (Exception e)
            {
                sr.Message = e.Message;
            }

            return sr;
        }

        public TenantSettingsViewModel GetTenantList()
        {
            var crud = new CrudTenant();
            TenantList = crud.Read(x => x.IsActive)
                .OrderBy(x => x.TenantID)
                .Select(y =>
                    new SelectListItem()
                    {
                        Text = y.TenantID + " - " + y.Description,
                        Value = y.TenantID.ToString()
                    })
                .ToList();

            return this;
        }
    }
}
