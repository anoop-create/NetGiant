using DP001BusinessLogic.Shared;
using DP001DataAccess.Entities;
using PagedList;
using System.Linq.Dynamic;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using System.Data;
using DP001DataAccess.Utilities;
using System.Linq.Expressions;

namespace DP001BusinessLogic
{
    public class CrudCompetitorInventory : ICRUD<CompetitorInventory>
    {
        public CompetitorInventory Create(CompetitorInventory obj)
        {
            using (DP001Entities db = new DP001Entities())
            {
                if (obj.CompetitorInventoryID == 0)
                {
                    db.Entry(obj).State = EntityState.Added;
                    db.SaveChanges();
                }

                return obj;
            }
        }

        public void Create(List<CompetitorInventory> competitors, int channelFK)
        {
            DataTable dt = new DataTable("StagingCompetitorInventory");
            
            using (DP001Entities db = new DP001Entities())
            {
                dt.Columns.Add(new DataColumn("ChannelFK", typeof(int)));
                dt.Columns.Add(new DataColumn("CompetitorFK", typeof(int)));
                dt.Columns.Add(new DataColumn("BrandFK", typeof(int)));
                dt.Columns.Add(new DataColumn("ManufacturerPartNo", typeof(string)));
                dt.Columns.Add(new DataColumn("Price", typeof(decimal)));
                dt.Columns.Add(new DataColumn("OriginalBrand", typeof(string)));
                dt.Columns.Add(new DataColumn("ClientProductID", typeof(string)));

                foreach (CompetitorInventory ci in competitors)
                {
                    dt.Rows.Add(ci.ChannelFK, ci.CompetitorFK, ci.BrandFK, ci.ManufacturerPartNo.Truncate(45), ci.Price, ci.OriginalBrand.Truncate(100), ci.ClientProductID.Truncate(45));
                }
            }
            
            SQL.SQLBulkInsert(dt, "DP001");

            List<SqlParameter> sqlParms = new List<SqlParameter>();
            SqlParameter sqlParm1 = new SqlParameter("@ChannelFK", SqlDbType.Int);
            sqlParm1.Value = channelFK;
            sqlParms.Add(sqlParm1);
            SQL.ExecuteStoredProcedure("DP001", "CreateUpdateCompetitorInventory", sqlParms, channelFK);
        }

        public CompetitorInventory Read(long id)
        {
            using (DP001Entities db = new DP001Entities())
            {
                return db.CompetitorInventories.Find(id);
            }
        }

        public CompetitorInventory Read(int id)
        {
            return Read((long)id);
        }

        public List<CompetitorInventory> Read(Expression<Func<CompetitorInventory, bool>> where, int take = 0, int skip = 0)
        {
            using (DP001Entities db = new DP001Entities())
            {
                var query = db.CompetitorInventories
                    .Include("Brand")
                    .Include("Competitor")
                    .Include("ProductInventory")
                    .OrderBy(x => x.ManufacturerPartNo)
                    .AsQueryable();

                query = query.Where(where);

                if (take > 0)
                {
                    query = query.Take(take).Skip(skip);
                }

                return query.ToList();
            }
        }

        public List<CompetitorInventory> ReadOnly(Expression<Func<CompetitorInventory, bool>> where, int take = 0, int skip = 0)
        {
            using (DP001Entities db = new DP001Entities())
            {
                var query = db.CompetitorInventories
                    .OrderBy(x => x.ManufacturerPartNo)
                    .AsQueryable();

                query = query.Where(where);

                if (take > 0)
                {
                    query = query.Take(take).Skip(skip);
                }

                return query.ToList();
            }
        }

        public IQueryable<CompetitorInventory> ReadCompetitorInventoryQuery(
            Expression<Func<CompetitorInventory, bool>> where,
            DP001Entities ctx)
        {
            var query = ctx.CompetitorInventories
                .Include("Brand")
                .OrderBy(x => x.Competitor.CompetitorName)
                .AsQueryable();

            query = query.Where(where);

            NoLockInterceptor.ApplyNoLock = true;

            return query;
        }

        public IPagedList<CompetitorInventory> ReadPagedList(Expression<Func<CompetitorInventory, bool>> where, int pageNumber, string sortField, string sortDir)
        {
            using (DP001Entities db = new DP001Entities())
            {
                var query = db.CompetitorInventories
                    .Include("Competitor")
                    .Include("Brand")
                    .OrderBy(x => x.ManufacturerPartNo)
                    .AsQueryable();

                query = query.OrderBy(sortField + " " + sortDir.ToUpper());
                query = query.Where(where);

                return query.ToPagedList(pageNumber, 200);
            }
        }

        public void Update(CompetitorInventory obj)
        {
            using (DP001Entities db = new DP001Entities())
            {
                db.Entry(obj).State = EntityState.Modified;
                db.SaveChanges();
            }
        }

        public void Delete(CompetitorInventory obj)
        {
            using (DP001Entities db = new DP001Entities())
            {
                db.Entry(obj).State = EntityState.Deleted;
                db.SaveChanges();
            }
        }

        public void UpdateList(List<CompetitorInventory> competitorInventories)
        {
            using (DP001Entities db = new DP001Entities())
            {
                foreach (var item in competitorInventories)
                {
                    db.Entry(item).State = EntityState.Modified;
                }

                db.SaveChanges();
            }
        }
    }
}
