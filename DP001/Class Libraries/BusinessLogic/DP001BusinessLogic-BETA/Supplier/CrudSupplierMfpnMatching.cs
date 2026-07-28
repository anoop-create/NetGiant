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
    public class CrudSupplierMfpnMatching : ICRUD<SupplierMfpnMatching>
    {
        public SupplierMfpnMatching Create(SupplierMfpnMatching obj)
        {
            using (DP001Entities db = new DP001Entities())
            {
                var supplierMfpnMatching = db.SupplierMfpnMatchings.Where(x => x.ChannelFK == obj.ChannelFK && x.BrandName == obj.BrandName && x.MatchTerm == obj.MatchTerm).FirstOrDefault();

                if (supplierMfpnMatching == null)
                {
                    supplierMfpnMatching = obj;
                    db.Entry(obj).State = EntityState.Added;
                    db.SaveChanges();
                }

                return supplierMfpnMatching;
            }
        }

        public void Delete(SupplierMfpnMatching obj)
        {
            using (DP001Entities db = new DP001Entities())
            {
                db.Entry(obj).State = EntityState.Deleted;
                db.SaveChanges();
            }
        }

        public SupplierMfpnMatching Read(int id)
        {
            throw new NotImplementedException();
        }

        public List<SupplierMfpnMatching> Read(Expression<Func<SupplierMfpnMatching, bool>> where)
        {
            using (DP001Entities db = new DP001Entities())
            {
                var query = db.SupplierMfpnMatchings
                    .OrderBy(x => x.BrandName)
                    .AsQueryable();

                query = query.Where(where);

                return query.ToList();
            }
        }

        public IQueryable<SupplierMfpnMatching> ReadQuery(Expression<Func<SupplierMfpnMatching, bool>> where, DP001Entities ctx)
        {
            var query = ctx.SupplierMfpnMatchings
                .OrderBy(x => x.BrandName)
                .AsQueryable();

            query = query.Where(where);

            return query;
        }

        public void Update(SupplierMfpnMatching obj)
        {
            using (DP001Entities db = new DP001Entities())
            {
                db.Entry(obj).State = EntityState.Modified;
                db.SaveChanges();
            }
        }
    }
}
