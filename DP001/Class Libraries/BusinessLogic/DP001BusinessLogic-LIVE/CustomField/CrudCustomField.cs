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
    public class CrudCustomField
    {
        public CustomField Create(CustomField obj)
        {
            using (DP001Entities db = new DP001Entities())
            {
                if (obj.CustomFieldID == 0)
                {
                    db.Entry(obj).State = EntityState.Added;
                    db.SaveChanges();
                }

                return obj;
            }
        }

        public List<CustomField> Read(int channelId)
        {
            using (DP001Entities db = new DP001Entities())
            {
                return db.CustomFields
                    .Include(x => x.Lookup)
                    .Where(x => x.ChannelFK == channelId)
                    .ToList();
            }
        }

        public List<CustomField> Read(Expression<Func<CustomField, bool>> where)
        {
            using (DP001Entities db = new DP001Entities())
            {
                var query = db.CustomFields
                    .Include(x => x.Lookup)
                    .AsQueryable();

                query = query.Where(where);

                return query.ToList();
            }
        }

        public void Update(CustomField obj)
        {
            using (DP001Entities db = new DP001Entities())
            {
                db.Entry(obj).State = EntityState.Modified;
                db.SaveChanges();
            }
        }

        public void Delete(CustomField obj)
        {
            using (DP001Entities db = new DP001Entities())
            {
                db.Entry(obj).State = EntityState.Deleted;
                db.SaveChanges();
            }
        }
    }
}
