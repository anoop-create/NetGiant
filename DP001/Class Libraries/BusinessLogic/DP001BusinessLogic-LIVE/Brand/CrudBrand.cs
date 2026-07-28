using DP001BusinessLogic.Shared;
using DP001DataAccess.Entities;
using System.Data.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;

namespace DP001BusinessLogic
{
    public class CrudBrand : ICRUD<Brand>
    {
        public Brand Create(Brand obj)
        {
            using (DP001Entities db = new DP001Entities())
            {
                var brand = db.Brands.Where(x => x.BrandName == obj.BrandName && x.ChannelFK == obj.ChannelFK).FirstOrDefault();

                if (brand == null)
                {
                    brand = obj;
                    db.Entry(obj).State = EntityState.Added;
                    db.SaveChanges();
                }

                return brand;
            }
        }

        public Brand Read(int id)
        {
            using (DP001Entities db = new DP001Entities())
            {
                return db.Brands.Find(id);
            }
        }

        public List<Brand> Read(Expression<Func<Brand, bool>> where)
        {
            using (DP001Entities db = new DP001Entities())
            {
                var query = db.Brands
                    .OrderBy(x => x.BrandName)
                    .AsQueryable();

                query = query.Where(where);

                return query.ToList();
            }
        }

        public void Update(Brand obj)
        {
            using (DP001Entities db = new DP001Entities())
            {
                db.Entry(obj).State = EntityState.Modified;
                db.SaveChanges();
            }
        }

        public void Delete(Brand obj)
        {
            using (DP001Entities db = new DP001Entities())
            {
                db.Entry(obj).State = EntityState.Deleted;
                db.SaveChanges();
            }
        }

        public List<Brand> GetBrands(int channelId)
        {
            using (DP001Entities db = new DP001Entities())
            {
                return db.Brands
                    .Where(x => x.ChannelFK == channelId)
                    .OrderBy(x => x.BrandName)
                    .ToList();
            }
        }
    }
}
