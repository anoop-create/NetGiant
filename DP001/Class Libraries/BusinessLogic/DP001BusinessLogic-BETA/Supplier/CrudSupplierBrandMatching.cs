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
    public class CrudSupplierBrandMatching //: ICRUD<SupplierBrandMatching>
    {
        //public SupplierBrandMatching Create(SupplierBrandMatching obj, List<Supplier> suppliers)
        public SupplierBrandMatching Create(SupplierBrandMatching obj, int channelId)
        {
            using (DP001Entities db = new DP001Entities())
            {
                var supplierBrandMatching = db.SupplierBrandMatchings.Where(x => x.Reference.ToLower() == obj.Reference.ToLower() && x.Supplier.Channel.ChannelID == channelId).FirstOrDefault();

                if (supplierBrandMatching == null)
                {
                    supplierBrandMatching = obj;
                    db.Entry(obj).State = EntityState.Added;
                    db.SaveChanges();
                }

                return supplierBrandMatching;
            }

        }

        public void Delete(SupplierBrandMatching obj)
        {
            using (DP001Entities db = new DP001Entities())
            {
                db.Entry(obj).State = EntityState.Deleted;
                db.SaveChanges();
            }
        }

        public SupplierBrandMatching Read(int id)
        {
            throw new NotImplementedException();
        }

        public List<SupplierBrandMatching> Read(Expression<Func<SupplierBrandMatching, bool>> where)
        {
            using (DP001Entities db = new DP001Entities())
            {
                var query = db.SupplierBrandMatchings
                    .OrderBy(x => x.BrandName)
                    .AsQueryable();

                query = query.Where(where);

                return query.ToList();
            }
        }

        public IQueryable<SupplierBrandMatching> ReadQuery(
            Expression<Func<SupplierBrandMatching, bool>> where,
            DP001Entities ctx)
        {
            var query = ctx.SupplierBrandMatchings
                    .OrderBy(x => x.BrandName)
                    .AsQueryable();

            query = query.Where(where);

            return query;
        }

        public void Update(SupplierBrandMatching obj)
        {
            using (DP001Entities db = new DP001Entities())
            {
                db.Entry(obj).State = EntityState.Modified;
                db.SaveChanges();
            }
        }
    }
}
