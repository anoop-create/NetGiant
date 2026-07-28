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
    public class CrudProviderExclusion
    {
        public ProviderExclusion Create(ProviderExclusion obj)
        {
            using (DP001Entities db = new DP001Entities())
            {
                if (obj.ProviderExclusionID == 0)
                {
                    db.Entry(obj).State = EntityState.Added;
                    db.SaveChanges();
                }

                return obj;
            }
        }

        public List<ProviderExclusion> Read(int channelId)
        {
            using (DP001Entities db = new DP001Entities())
            {
                return db.ProviderExclusions
                    .Include(x => x.Lookup)
                    .Where(x => x.ChannelFK == channelId).ToList();
            }
        }

        public List<ProviderExclusion> Read(Expression<Func<ProviderExclusion, bool>> where)
        {
            using (DP001Entities db = new DP001Entities())
            {
                var query = db.ProviderExclusions
                    .Include(x => x.Lookup)
                    .AsQueryable();

                query = query.Where(where);

                return query.ToList();
            }
        }

        public void Update(ProviderExclusion obj)
        {
            using (DP001Entities db = new DP001Entities())
            {
                db.Entry(obj).State = EntityState.Modified;
                db.SaveChanges();
            }
        }

        public void Delete(ProviderExclusion obj)
        {
            using (DP001Entities db = new DP001Entities())
            {
                db.Entry(obj).State = EntityState.Deleted;
                db.SaveChanges();
            }
        }
    }
}

