using DP001BusinessLogic.Shared;
using DP001DataAccess.Entities;
using PagedList;
using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using System.Web;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace DP001BusinessLogic.ViewModels
{
    public class TenantAuditViewModel
    {
        public TenantAuditViewModel(int channelId)
        {
            _channelId = channelId;
            _ctx = new DP001Entities();
        }

        public TenantAuditViewModel()
        {

        }

        public IQueryable<Telerik> TenantAuditList { get; set; }
        public TenantAudit TenantAuditEntry { get; set; }
        private int _channelId;
        public Channel Channel { get; set; }
        public TenantSetting Tenant { get; set; }
        public List<string> FieldList { get; set; }
        public List<string> FromFieldList { get; set; }
        public List<string> ToFieldList { get; set; }
        private DP001Entities _ctx;


        public TenantAuditViewModel GetEntries()
        {
            var crud = new CrudTenantAudit();
            TenantAuditList = crud.ReadTenantAuditQuery(x => x.ChannelFK == _channelId, _ctx).AsTelerikViewModel();

            return this;
        }

        public TenantAuditViewModel DisplayEntry(int id)
        {
            var crud = new CrudTenantAudit();
            TenantAuditEntry = crud.Read(x => x.ChannelFK == _channelId && x.TenantAuditID == id).FirstOrDefault();

            if (TenantAuditEntry != null)
            {
                if (TenantAuditEntry.OldValues == "")
                {
                    FieldList = TenantAuditEntry.NewValues.Split('#').ToList();
                }
                else
                {
                    FieldList = TenantAuditEntry.OldValues.Split('#').ToList();
                }
                
                FromFieldList = TenantAuditEntry.OldValues.Split('#').ToList();
                ToFieldList = TenantAuditEntry.NewValues.Split('#').ToList();
            }

            return this;
        }

        public class Telerik
        {
            public int TenantAuditId { get; set; }
            public DateTime Date { get; set; }
            public string ChannelName { get; set; }
            public string UserName { get; set; }
            public string Action { get; set; }
            public string Type { get; set; }
            public string ObjectName { get; set; }
        }
    }

    public static class TenantAuditExtensions
    {
        public static IQueryable<TenantAuditViewModel.Telerik> AsTelerikViewModel(this IQueryable<TenantAudit> tenantAuditQuery)
        {
            return tenantAuditQuery.Select(o => new TenantAuditViewModel.Telerik
            {
                TenantAuditId = o.TenantAuditID,
                Date = o.Timestamp,
                ChannelName = o.Channel.ChannelName,
                UserName = o.UserName,
                Type = o.Type,
                ObjectName = o.ObjectName,
                Action = o.Type == "A" ? "Add" : o.Type == "C" ? "Change" : o.Type == "D" ? "Delete" : ""
            });
        }
    }
}
