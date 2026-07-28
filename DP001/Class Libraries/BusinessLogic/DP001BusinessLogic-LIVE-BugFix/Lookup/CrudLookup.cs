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
    }
}
