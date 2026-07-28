using DP001BusinessLogic.Shared;
using DP001DataAccess.Entities;
using DP001DataAccess.Utilities;
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
    public class CrudChannel : ICRUD<Channel>
    {
        public Channel Create(Channel obj)
        {
            using (DP001Entities db = new DP001Entities())
            {
                var channel = db.Channels.Where(x => x.ChannelName == obj.ChannelName && x.TenantFK == obj.TenantFK).FirstOrDefault();

                if (channel == null)
                {
                    channel = obj;
                    db.Entry(obj).State = EntityState.Added;
                    db.SaveChanges();
                }

                return channel;
            }
        }

        public Channel Read(int id)
        {
            using (DP001Entities db = new DP001Entities())
            {
                if (id == 0)
                {
                    var newChannel = new Channel();
                    newChannel.TenantSetting = new TenantSetting();

                    return newChannel;
                }
                else
                {
                    return db.Channels
                    .Include("TenantSetting")
                    .Where(x => x.ChannelID == id)
                    .FirstOrDefault();
                }
            }
        }

        public List<Channel> Read(Expression<Func<Channel, bool>> where, int take = 0, int skip = 0)
        {
            using (DP001Entities db = new DP001Entities())
            {
                var query = db.Channels
                    .OrderBy(x => x.ChannelName)
                    .AsQueryable();

                query = query.Where(where);

                if (take > 0)
                {
                    query = query.Take(take).Skip(skip);
                }

                return query.ToList();
            }
        }

        public void Update(Channel obj)
        {
            using (DP001Entities db = new DP001Entities())
            {
                db.Entry(obj).State = EntityState.Modified;
                db.SaveChanges();
            }
        }

        public void Delete(Channel obj)
        {
            //using (DP001Entities db = new DP001Entities())
            //{
            //    db.Entry(obj).State = EntityState.Deleted;
            //    db.SaveChanges();
            //}
        }

        public void Delete(Dictionary<string, string> parms)
        {
            List<SqlParameter> sqlParms = new List<SqlParameter>();
            SqlParameter sqlParm1 = new SqlParameter("@ChannelID", SqlDbType.Int);
            sqlParm1.Value = Int32.Parse(parms["channelid"]);
            sqlParms.Add(sqlParm1);
            if (SQL.ExecuteStoredProcedure("DP001", "DeleteChannel", sqlParms, Int32.Parse(parms["channelid"])))
            {
                CommonDataFunctions.CreateLogEntry(Int32.Parse(parms["tenantid"]), Int32.Parse(parms["channelid"]), "Channel successfully deleted", "Information", true);
            }
            else
            {
                CommonDataFunctions.CreateLogEntry(Int32.Parse(parms["tenantid"]), Int32.Parse(parms["channelid"]), "Channel could not be deleted", "Error", true);
            }
        }
    }
}
