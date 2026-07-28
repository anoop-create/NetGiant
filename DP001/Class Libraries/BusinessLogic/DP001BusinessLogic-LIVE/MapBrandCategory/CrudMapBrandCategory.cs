using DP001BusinessLogic.Shared;
using DP001DataAccess.Entities;
using DP001DataAccess.Utilities;
using PagedList;
using System.Linq.Dynamic;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;

namespace DP001BusinessLogic
{
    public class CrudMapBrandCategory : ICRUD<MapBrandCategory>
    {
        public MapBrandCategory Create(MapBrandCategory obj)
        {
            using (DP001Entities db = new DP001Entities())
            {
                var brandcategory = db.MapBrandCategories.Where(x => x.BrandFK == obj.BrandFK && x.ProductCategoryFK == obj.ProductCategoryFK).FirstOrDefault();

                if (brandcategory == null)
                {
                    brandcategory = obj;
                    db.Entry(obj).State = EntityState.Added;
                    db.SaveChanges();
                }

                return brandcategory;
            }
        }

        public void Delete(MapBrandCategory obj)
        {
            using (DP001Entities db = new DP001Entities())
            {
                db.Entry(obj).State = EntityState.Deleted;
                db.SaveChanges();
            }
        }

        public MapBrandCategory Read(int id)
        {
            using (DP001Entities db = new DP001Entities())
            {
                return db.MapBrandCategories
                    .Include("Brand")
                    .Include("Category")
                    .Where(x => x.MapBrandCategoryID == id)
                    .FirstOrDefault();
            }
        }

        public List<MapBrandCategory> GetCategories(int brandFK)
        {
            using (DP001Entities db = new DP001Entities())
            {
                return db.MapBrandCategories
                    .Include("Brand")
                    .Include("ProductCategory")
                    .Where(x => x.BrandFK == brandFK)
                    .ToList();
            }
        }

        public void Update(MapBrandCategory obj)
        {
            using (DP001Entities db = new DP001Entities())
            {
                db.Entry(obj).State = EntityState.Modified;
                db.SaveChanges();
            }
        }
    }
}
