using DP001DataAccess.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using DP001BusinessLogic;
using DP001DataAccess.Utilities;

namespace DP001Website.Models
{
    public class CommonModel
    {
        public TenantSetting GetTenant()
        {
            var tenant = new TenantSetting();

            if (HttpContext.Current.Session["Tenant"] != null)
            {
                tenant = (TenantSetting)HttpContext.Current.Session["Tenant"];
            }
            else
            {
                tenant = RefreshTenantSession();
                if (tenant.Channels.Count > 0)
                {
                    HttpContext.Current.Session["CurrentChannel"] = tenant.LastUsedChannelId.ToString();
                }
            }

            if (tenant.Channels.Count == 0)
            {
                //Create default Channel
                var channelSetting = new CrudChannel();

                Channel ch = new Channel();
                ch.ChannelName = "Default Channel";
                ch.TenantFK = tenant.TenantID;
                ch.IsActive = false;
                ch.IsDefault = true;
                var crudLookup = new CrudLookup();
                ch.SLActiveTypeFK = crudLookup.Read(x => x.LookupType.LookupTypeName == "SkuudleLiteActiveType" &&
                    x.LookupName == "SL None").FirstOrDefault().LookupID;
                ch.RoundingGroupFK = crudLookup.Read(x => x.LookupType.LookupTypeName == "RoundingGroup" &&
                    x.LookupName == "No Rounding").FirstOrDefault().LookupID;
                channelSetting.Create(ch);

                tenant = RefreshTenantSession();
                tenant.LastUsedChannelId = tenant.Channels.FirstOrDefault().ChannelID;
                HttpContext.Current.Session["CurrentChannel"] = tenant.LastUsedChannelId.ToString();

                var crudTenant = new CrudTenant();
                crudTenant.Update(tenant);
            }

            return tenant;
        }

        public TenantSetting RefreshTenantSession()
        {
            var user = UserManager.FindById(HttpContext.Current.User.Identity.GetUserId());
            var tenantSetting = new CrudTenant();

            TenantSetting ts = tenantSetting.Read(user.TenantID);
            HttpContext.Current.Session["Tenant"] = ts;

            RefreshLogSummaryCount(ts.LastUsedChannelId);

            return ts;
        }

        public void RefreshLogSummaryCount(int? channelId)
        {
            DateTime dt = CommonDataFunctions.GetCurrentDateTime().AddDays(-2);
            CrudLog crud = new CrudLog();
            HttpContext.Current.Session["SummaryLogCount"] = crud.Read(x => (x.Lookup.LookupName == "Notification" || x.Lookup.LookupName == "Suggestion" || x.Lookup.LookupName == "ScheduleInfo") && x.DateTime > dt && x.ChannelFK == channelId).Count;
        }

        public void ChangeUserTenant(int tenantID)
        {
            var user = UserManager.FindById(HttpContext.Current.User.Identity.GetUserId());
            user.TenantID = tenantID;
            UserManager.Update(user);

            var tenantSession = RefreshTenantSession();
            HttpContext.Current.Session["Tenant"] = tenantSession;

            if (tenantSession.LastUsedChannelId > 0)
            {
                HttpContext.Current.Session["CurrentChannel"] = tenantSession.LastUsedChannelId.ToString();
            }
            else
            {
                HttpContext.Current.Session["CurrentChannel"] = tenantSession.Channels.FirstOrDefault().ChannelID.ToString();
            }
        }

        public Channel GetChannel()
        {
            var channel = new Channel();

            TenantSetting tenant = (TenantSetting)HttpContext.Current.Session["Tenant"];
            int channelId = Int32.Parse(HttpContext.Current.Session["CurrentChannel"].ToString());
            return tenant.Channels
                .Where(x => x.ChannelID == channelId)
                .FirstOrDefault();
        }

        public int GetChannelId()
        {
            var tenant = new TenantSetting();

            if (HttpContext.Current.Session["Tenant"] != null)
            {
                tenant = (TenantSetting)HttpContext.Current.Session["Tenant"];
            }
            else
            {
                HttpContext.Current.Session["Tenant"] = tenant = RefreshTenantSession();
            }

            if (HttpContext.Current.Session["CurrentChannel"] == null)
            {
                HttpContext.Current.Session["CurrentChannel"] = tenant.LastUsedChannelId.ToString();
            }

            return Int32.Parse(HttpContext.Current.Session["CurrentChannel"].ToString());
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        private ApplicationUserManager _userManager;
    }
}