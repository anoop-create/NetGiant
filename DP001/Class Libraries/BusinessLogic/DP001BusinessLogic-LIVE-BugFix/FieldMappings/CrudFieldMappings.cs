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
    public class CrudFieldMappings : ICRUD<FieldMapping>
    {
        public FieldMapping Create(FieldMapping obj)
        {
            using (DP001Entities db = new DP001Entities())
            {
                db.Entry(obj).State = EntityState.Added;
                db.SaveChanges();

                return obj;
            }
        }

        public FieldMapping Read(int id)
        {
            using (DP001Entities db = new DP001Entities())
            {
                return db.FieldMappings.Find(id);
            }
        }

        public void Update(FieldMapping obj)
        {
            using (DP001Entities db = new DP001Entities())
            {
                db.Entry(obj).State = EntityState.Modified;
                db.SaveChanges();
            }
        }

        public void Delete(FieldMapping obj)
        {
            using (DP001Entities db = new DP001Entities())
            {
                db.Entry(obj).State = EntityState.Deleted;
                db.SaveChanges();
            }
        }
    }
}
