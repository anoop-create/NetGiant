using DP001DataAccess.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;

namespace DP001BusinessLogic
{
    public class CrudLookup
    {
        public List<Lookup> Read(Expression<Func<Lookup, bool>> where)
        {
            using (DP001Entities db = new DP001Entities())
            {
                return db.Lookups
                    .Where(where)
                    .OrderBy(x => x.LookupName)
                    .ToList();
            }
        }
        public List<LookupType> ReadTypes(Expression<Func<LookupType, bool>> where)
        {
            using (DP001Entities db = new DP001Entities())
            {
                return db.LookupTypes
                    .Where(where)
                    .OrderBy(x => x.LookupTypeName)
                    .ToList();
            }
        }

        public IQueryable<Lookup> ReadLookupsQuery(
            Expression<Func<Lookup, bool>> where,
            DP001Entities ctx)
        {
            var query = ctx.Lookups
                .Include(x => x.LookupType)
                .Where(where)
                .OrderBy(x => x.LookupType.LookupTypeName).ThenBy(x => x.LookupName)
                .AsQueryable();

            query = query.Where(where);
            NoLockInterceptor.ApplyNoLock = true;
            return query;
        }

        public IQueryable<LookupType> ReadLookupTypesQuery(
            Expression<Func<LookupType, bool>> where,
            DP001Entities ctx)
        {
            var query = ctx.LookupTypes
                .Where(where)
                .OrderBy(x => x.LookupTypeName)
                .AsQueryable();

            query = query.Where(where);
            NoLockInterceptor.ApplyNoLock = true;
            return query;
        }
    }
}
