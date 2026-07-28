using DP001BusinessLogic.Shared;
using DP001DataAccess.Entities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DP001BusinessLogic
{
    public class CrudProductCategory : ICRUD<ProductCategory>
    {
        public ProductCategory Create(ProductCategory obj)
        {
            using (DP001Entities db = new DP001Entities())
            {
                var categ = db.ProductCategories.Where(x => x.CategoryName == obj.CategoryName && x.ChannelFK == obj.ChannelFK).FirstOrDefault();

                if (categ == null)
                {
                    categ = obj;
                    db.Entry(categ).State = EntityState.Added;
                    db.SaveChanges();
                }

                return categ;
            }
        }

        public ProductCategory Read(int id)
        {
            using (DP001Entities db = new DP001Entities())
            {
                return db.ProductCategories.Find(id);
            }
        }

        public List<ProductCategory> Read(Func<ProductCategory, bool> query)
        {
            using (DP001Entities db = new DP001Entities())
            {
                return db.ProductCategories
                    .OrderBy(x => x.CategoryName)
                    .Where(query)
                    .ToList();
            }
        }

        public void Update(ProductCategory obj)
        {
            using (DP001Entities db = new DP001Entities())
            {
                db.Entry(obj).State = EntityState.Modified;
                db.SaveChanges();
            }
        }

        public void Delete(ProductCategory obj)
        {
            using (DP001Entities db = new DP001Entities())
            {
                db.Entry(obj).State = EntityState.Deleted;
                db.SaveChanges();
            }
        }

        public List<ProductCategory> GetCategories(int channelFK)
        {
            using (DP001Entities db = new DP001Entities())
            {
                return db.ProductCategories
                    .Where(x => x.ChannelFK == channelFK)
                    .ToList();
            }
        }
    }
}
