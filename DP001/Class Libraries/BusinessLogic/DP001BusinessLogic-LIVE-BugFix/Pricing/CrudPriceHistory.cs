using DP001DataAccess.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DP001BusinessLogic
{
    public class CrudPriceHistory
    {
        public List<PriceHistory> Read(Expression<Func<PriceHistory, bool>> where)
        {
            using (DP001Entities db = new DP001Entities())
            {
                var query = db.PriceHistories.AsQueryable();
                query = query.Where(where);

                return query.ToList();
            }
        }
    }
}
