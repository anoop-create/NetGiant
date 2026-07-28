using DP001BusinessLogic.Shared;
using DP001DataAccess.Entities;
using DP001DataAccess.Utilities;
using PagedList;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Dynamic;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DP001BusinessLogic
{
    public class CrudLog
    {
        public List<Log> Read(Expression<Func<Log, bool>> where, int take = 0)
        {
            using (DP001Entities db = new DP001Entities())
            {
                var query = db.Logs
                    .Include("Lookup")
                    .OrderByDescending(x => x.DateTime)
                    .AsQueryable();

                query = query.Where(where);

                if (take > 0)
                {
                    query = query.Take(take);
                }

                return query.ToList();
            }
        }

        public IQueryable<Log> ReadLogsQuery(
            Expression<Func<Log, bool>> where,
            DP001Entities ctx)
        {
            var query = ctx.Logs
                .OrderByDescending(x => x.DateTime)
                .AsQueryable();

            query = query.Where(where);

            NoLockInterceptor.ApplyNoLock = true;

            return query;
        }
    }
}
