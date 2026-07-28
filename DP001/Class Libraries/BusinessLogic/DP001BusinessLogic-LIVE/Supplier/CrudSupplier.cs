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
    public class CrudSupplier : ICRUD<Supplier>
    {
        public Supplier Create(Supplier obj)
        {
            using (DP001Entities db = new DP001Entities())
            {
                var supplier = db.Suppliers.Where(x => x.SupplierName == obj.SupplierName).FirstOrDefault();

                if (supplier == null)
                {
                    supplier = obj;
                    db.Entry(obj).State = EntityState.Added;
                    db.SaveChanges();
                }

                return supplier;
            }
        }

        public Supplier Read(int id)
        {
            using (DP001Entities db = new DP001Entities())
            {
                return db.Suppliers.Find(id);
            }
        }

        public List<Supplier> Read(Expression<Func<Supplier, bool>> where, int take = 0, int skip = 0)
        {
            using (DP001Entities db = new DP001Entities())
            {
                var query = db.Suppliers
                    .Include(x => x.FTPSetting.FieldMapping)
                    .OrderBy(x => x.SupplierName)
                    .AsQueryable();

                query = query.Where(where);

                if (take > 0)
                {
                    query = query.Take(take).Skip(skip);
                }

                return query.ToList();
            }
        }

        public void Update(Supplier obj)
        {
            using (DP001Entities db = new DP001Entities())
            {
                db.Entry(obj).State = EntityState.Modified;
                db.SaveChanges();
            }
        }

        public void Delete(Supplier obj)
        {
            using (DP001Entities db = new DP001Entities())
            {
                db.Entry(obj).State = EntityState.Deleted;
                db.SaveChanges();
            }
        }
    }
}
