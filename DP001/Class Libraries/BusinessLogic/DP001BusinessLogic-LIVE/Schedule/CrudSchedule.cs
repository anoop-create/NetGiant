using DP001BusinessLogic.Shared;
using DP001DataAccess.Entities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DP001BusinessLogic
{
    public class CrudSchedule
    {
        public Schedule Create(Schedule obj)
        {
            using (DP001Entities db = new DP001Entities())
            {
                if (obj.ScheduleID == 0)
                {
                    db.Entry(obj).State = EntityState.Added;
                    db.SaveChanges();
                }

                return obj;
            }
        }

        public List<Schedule> Read(int channelId)
        {
            using (DP001Entities db = new DP001Entities())
            {
                return db.Schedules
                    .Include(x => x.Lookup)
                    .Include(x => x.Lookup1)
                    .Where(x => x.ChannelFK == channelId).ToList();
            }
        }

        public List<Schedule> Read(Expression<Func<Schedule, bool>> where)
        {
            using (DP001Entities db = new DP001Entities())
            {
                var query = db.Schedules
                    .Include(x => x.Lookup)
                    .Include(x => x.Lookup1)
                    .AsQueryable();

                query = query.Where(where);

                return query.ToList();
            }
        }

        public int GetTenantScheduleCount(int tenantID)
        {
            using (DP001Entities db = new DP001Entities())
            {
                return db.Schedules
                    .Where(x => x.Channel.TenantFK == tenantID && x.IsActive)
                    .Count();
            }
        }

        public void Update(Schedule obj)
        {
            using (DP001Entities db = new DP001Entities())
            {
                db.Entry(obj).State = EntityState.Modified;
                db.SaveChanges();
            }
        }

        public void Delete(Schedule obj)
        {
            using (DP001Entities db = new DP001Entities())
            {
                db.Entry(obj).State = EntityState.Deleted;
                db.SaveChanges();
            }
        }
    }
}
