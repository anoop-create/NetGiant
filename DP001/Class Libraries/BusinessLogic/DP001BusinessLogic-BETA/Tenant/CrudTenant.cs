using DP001BusinessLogic.Shared;
using DP001DataAccess.Entities;
using DP001DataAccess.Utilities;
using PagedList;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DP001BusinessLogic
{
    public class CrudTenant : ICRUD<TenantSetting>
    {
        public TenantSetting Create(TenantSetting obj)
        {
            using (DP001Entities db = new DP001Entities())
            {
                if (obj.TenantID == 0)
                {
                    db.Entry(obj).State = EntityState.Added;
                    db.SaveChanges();
                }

                return obj;
            }
        }

        public TenantSetting Read(int tenantID)
        {
            using (DP001Entities db = new DP001Entities())
            {
                TenantSetting ts = db.TenantSettings
                    .Include(x => x.Lookup)
                    .Include("Channels.Schedules.Lookup")
                    .Include("Channels.FTPSettings")
                    .Include("Channels.FTPSettings.Lookup")
                    .Include("Channels.FTPSettings.FieldMapping")
                    .Include("Channels.FTPSettings.Suppliers")
                    .Include("Channels.PriceRules")
                    .Include("Channels.CustomFields")
                    .Include("Channels.CustomFields.Lookup")
                    .Include(x => x.SapASettings)
                    .Include(x => x.Contract)
                    .FirstOrDefault(x => x.TenantID == tenantID);
                if (ts.Channels.Count > 0)
                {
                    ts.Channels.OrderBy(c => c.ChannelID).FirstOrDefault().IsDefault = true;
                }
                return ts;
            }
        }

        public List<TenantSetting> Read(Expression<Func<TenantSetting, bool>> where)
        {
            using (DP001Entities db = new DP001Entities())
            {
                var query = db.TenantSettings
                    .Include(x => x.Lookup)
                    .Include(x => x.Channels)
                    .Include(x => x.SapASettings)
                    .Include(x => x.Contract)
                    .AsQueryable();

                query = query.Where(where);

                return query.ToList();
            }
        }

        public TenantSetting GetTenantForChannel(int channelId)
        {
            using (DP001Entities db = new DP001Entities())
            {
                var query = db.TenantSettings
                    .Include(x => x.SapASettings)
                    .Include(x => x.Contract)
                    .Where(x => x.Channels.Any(c => c.ChannelID == channelId))
                    .FirstOrDefault();

                return query;
            }
        }

        //public List<Channel> ReadChannel(Expression<Func<Channel, bool>> where)
        //{
        //    using (DP001Entities db = new DP001Entities())
        //    {
        //        var query = db.Channels
        //            .AsQueryable();

        //        query = query.Where(where);

        //        return query.ToList();
        //    }
        //}

        public void Update(TenantSetting obj)
        {
            using (DP001Entities db = new DP001Entities())
            {
                db.Entry(obj).State = EntityState.Modified;
                try
                {
                    db.SaveChanges();
                }
                catch (Exception e)
                {
                    string x = e.Message;
                }
                
            }
        }

        public void Delete(TenantSetting obj)
        {
            using (DP001Entities db = new DP001Entities())
            {
                db.Entry(obj).State = EntityState.Deleted;
                db.SaveChanges();
            }
        }

        public void Delete(Dictionary<string, string> parms)
        {
            List<SqlParameter> sqlParms = new List<SqlParameter>();
            SqlParameter sqlParm1 = new SqlParameter("@TenantID", SqlDbType.Int);
            sqlParm1.Value = Int32.Parse(parms["tenantid"]);
            sqlParms.Add(sqlParm1);
            if (SQL.ExecuteStoredProcedure("DP001", "DeleteTenant", sqlParms, Int32.Parse(parms["channelid"])))
            {
                CommonDataFunctions.CreateLogEntry(Int32.Parse(parms["tenantid"]), 0, "Tenant successfully deleted", "Information", true);
            }
            else
            {
                CommonDataFunctions.CreateLogEntry(Int32.Parse(parms["tenantid"]), 0, "Tenant could not be deleted", "Error", true);
            }
        }

        //public Channel CreateChannel(Channel obj)
        //{
        //    using (DP001Entities db = new DP001Entities())
        //    {
        //        if (obj.ChannelID == 0)
        //        {
        //            db.Entry(obj).State = EntityState.Added;
        //            db.SaveChanges();
        //        }

        //        return obj;
        //    }
        //}

        //public void UpdateChannel(Channel obj)
        //{
        //    using (DP001Entities db = new DP001Entities())
        //    {
        //        db.Entry(obj).State = EntityState.Modified;
        //        db.SaveChanges();

        //    }
        //}

        //public void DeleteChannel(Channel obj)
        //{
        //    using (DP001Entities db = new DP001Entities())
        //    {
        //        db.Entry(obj).State = EntityState.Deleted;
        //        db.SaveChanges();
        //    }
        //}
    }
}
