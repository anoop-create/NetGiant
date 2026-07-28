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

        public IQueryable<TenantAudit> TenantAuditList { get; set; }
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
            TenantAuditList = crud.ReadTenantAuditQuery(x => x.ChannelFK == _channelId, _ctx);

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
    }
}
