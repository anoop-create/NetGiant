using DP001BusinessLogic.Shared;
using DP001DataAccess.Entities;
using DP001DataAccess.Utilities;
using MoreLinq;
using PagedList;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;

namespace DP001BusinessLogic
{
    public class CrudTenantAudit : ICRUD<TenantAudit>
    {
        public TenantAudit Create(TenantAudit obj)
        {
            using (DP001Entities db = new DP001Entities())
            {
                if (obj.TenantAuditID == 0)
                {
                    db.Entry(obj).State = EntityState.Added;
                    db.SaveChanges();
                }

                return obj;
            }
        }

        public void Delete(TenantAudit obj)
        {
            throw new NotImplementedException();
        }

        public TenantAudit Read(int id)
        {
            TenantAudit ta = new TenantAudit();
            using (DP001Entities db = new DP001Entities())
            {
               ta = db.TenantAudits
                    .Include(x => x.Channel)
                    .Where(x => x.TenantAuditID == id)
                    .FirstOrDefault();
            }
            ta.TypeName = GetTypeName(ta.Type);

            return ta;
        }

        public List<TenantAudit> Read(Expression<Func<TenantAudit, bool>> where)
        {
            using (DP001Entities db = new DP001Entities())
            {
                var query = db.TenantAudits
                    .Include(x => x.Channel)
                    .AsQueryable();

                query = query.Where(where);

                return query.ToList();
            }
        }

        public IQueryable<TenantAudit> ReadTenantAuditQuery(
            Expression<Func<TenantAudit, bool>> where,
            DP001Entities ctx)
        {
            var query = ctx.TenantAudits
                .Include(x => x.Channel)
                .OrderByDescending(x => x.Timestamp)
                .AsQueryable();

            query = query.Where(where);

            foreach (TenantAudit ta in query)
            {
                ta.TypeName = GetTypeName(ta.Type);
            }

            return query;
        }

        public void Update(TenantAudit obj)
        {
            throw new NotImplementedException();
        }

        private string GetTypeName(string type)
        {
            string typeName = "";
            switch (type)
            {
                case "A":
                    typeName = "Add";
                    break;
                case "C":
                    typeName = "Change";
                    break;
                case "D":
                    typeName = "Delete";
                    break;
            }
            return typeName;
        }

        public TenantAudit BuildTenantAuditRecord(
            int channelFK,
            string type,
            string objectName,
            string oldValues,
            string newValues
        )
        {
            TenantAudit ta = new TenantAudit();

            ta.ChannelFK = channelFK;
            ta.Type = type;
            ta.Timestamp = CommonDataFunctions.GetCurrentDateTime();
            ta.UserName = Thread.CurrentPrincipal.Identity.Name;
            ta.ObjectName = objectName.Truncate(50);
            ta.OldValues = oldValues;
            ta.NewValues = newValues;

            return ta;
        }
    }
}
