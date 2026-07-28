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
    public class CrudCompetitor : ICRUD<Competitor>
    {
        public bool ErrorOccured { get; set; } = false;

        public Competitor Create(Competitor obj)
        {
            using (DP001Entities db = new DP001Entities())
            {
                var comp = db.Competitors.Where(x => x.CompetitorName == obj.CompetitorName && x.ChannelFK == obj.ChannelFK).FirstOrDefault();

                if (comp == null)
                {
                    comp = obj;
                    db.Entry(obj).State = EntityState.Added;
                    db.SaveChanges();
                }

                return comp;
            }
        }

        public List<Competitor> Create(HashSet<Competitor> competitors, int channelFK)
        {
            DataTable dt = new DataTable("StagingCompetitor");

            using (DP001Entities db = new DP001Entities())
            {
                dt.Columns.Add(new DataColumn("ChannelFK", typeof(int)));
                dt.Columns.Add(new DataColumn("CompetitorName", typeof(string)));
                dt.Columns.Add(new DataColumn("ReviewTotal", typeof(int)));
                dt.Columns.Add(new DataColumn("ReviewRating", typeof(decimal)));
                dt.Columns.Add(new DataColumn("IsActive", typeof(bool)));

                foreach (Competitor ci in competitors)
                {
                    dt.Rows.Add(ci.ChannelFK, ci.CompetitorName.Truncate(100), ci.ReviewTotal, ci.ReviewRating, ci.IsActive);
                }
            }

            SQL.SQLBulkInsert(dt, "DP001");

            List<SqlParameter> sqlParms = new List<SqlParameter>();
            SqlParameter sqlParm1 = new SqlParameter("@ChannelFK", SqlDbType.Int);
            sqlParm1.Value = channelFK;
            sqlParms.Add(sqlParm1);
            var isSuccess = SQL.ExecuteStoredProcedure("DP001", "CreateUpdateCompetitor", sqlParms, channelFK);

            if (!isSuccess)
            {
                var crudChannel = new CrudChannel();
                var channel = crudChannel.Read(x => x.ChannelID == channelFK).FirstOrDefault();
                CommonDataFunctions.CreateLogEntry(channel, "Unable to load competitors due to errors found. Please contact support.", "Notification");
                ErrorOccured = true;
            }

            return Read(x => x.ChannelFK == channelFK);
        }

        public Competitor Read(int id)
        {
            using (DP001Entities db = new DP001Entities())
            {
                return db.Competitors.Find(id);
            }
        }

        public List<Competitor> Read(Expression<Func<Competitor, bool>> where)
        {
            using (DP001Entities db = new DP001Entities())
            {
                var query = db.Competitors
                    .AsQueryable();

                query = query.Where(where);

                return query.ToList();
            }
        }

        public HashSet<Competitor> ReadToHashset(Expression<Func<Competitor, bool>> where)
        {
            using (DP001Entities db = new DP001Entities())
            {
                var query = db.Competitors
                    .AsQueryable();

                query = query.Where(where);

                return new HashSet<Competitor>(query);
            }
        }

        public IQueryable<Competitor> ReadCompetitorQuery(
            Expression<Func<Competitor, bool>> where,
            DP001Entities ctx)
        {
            var query = ctx.Competitors
                .Include(x => x.Channel)
                .OrderBy(x => x.CompetitorName)
                .AsQueryable();

            query = query.Where(where);

            return query;
        }

        public void Update(Competitor obj)
        {
            using (DP001Entities db = new DP001Entities())
            {
                db.Entry(obj).State = EntityState.Modified;
                db.SaveChanges();
            }
        }

        public void Delete(Competitor obj)
        {
            using (DP001Entities db = new DP001Entities())
            {
                db.Entry(obj).State = EntityState.Deleted;
                db.SaveChanges();
            }
        }
    }
}
