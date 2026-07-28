using DP001BusinessLogic.Shared;
using DP001DataAccess.Entities;
using PagedList;
using System.Linq.Dynamic;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DP001DataAccess.Utilities;
using System.Linq.Expressions;
using System.Data.SqlClient;
using System.Transactions;
using MoreLinq;

namespace DP001BusinessLogic
{
    public class CrudSupplierInventory
    {
        public SupplierInventory Create(SupplierInventory obj)
        {
            using (DP001Entities db = new DP001Entities())
            {
                if (obj.SupplierInventoryID == 0)
                {
                    db.Entry(obj).State = EntityState.Added;
                    db.SaveChanges();
                }

                return obj;
            }
        }

        public void Create(List<SupplierInventory> suppliers, int channelFK)
        {
            DataTable dt = new DataTable("StagingSupplierInventory");

            using (DP001Entities db = new DP001Entities())
            {
                dt.Columns.Add(new DataColumn("ChannelFK", typeof(int)));
                dt.Columns.Add(new DataColumn("SupplierFK", typeof(int)));
                dt.Columns.Add(new DataColumn("BrandFK", typeof(int)));
                dt.Columns.Add(new DataColumn("ManufacturerPartNo", typeof(string)));
                dt.Columns.Add(new DataColumn("StockQuantity", typeof(int)));
                dt.Columns.Add(new DataColumn("Price", typeof(decimal)));
                dt.Columns.Add(new DataColumn("ProductInventoryFK", typeof(int)));
                dt.Columns.Add(new DataColumn("Description", typeof(string)));
                dt.Columns.Add(new DataColumn("Dismiss", typeof(bool)));
                dt.Columns.Add(new DataColumn("DateLastUpdated", typeof(DateTime)));
                dt.Columns.Add(new DataColumn("OriginalBrand", typeof(string)));
                dt.Columns.Add(new DataColumn("ClientProductID", typeof(string)));

                foreach (SupplierInventory si in suppliers)
                {
                    dt.Rows.Add(si.ChannelFK, si.SupplierFK, si.BrandFK,
                        string.IsNullOrEmpty(si.ManufacturerPartNo) ? "?" : si.ManufacturerPartNo.Truncate(45),
                        si.StockQuantity, si.Price, 1, si.Description.Truncate(200), false, CommonDataFunctions.GetCurrentDateTime(),
                        si.OriginalBrand.Truncate(100), si.ClientProductID.Truncate(45));
                }
            }

            SQL.SQLBulkInsert(dt, "DP001");

            List<SqlParameter> sqlParms = new List<SqlParameter>();
            SqlParameter sqlParm1 = new SqlParameter("@ChannelFK", SqlDbType.Int);
            sqlParm1.Value = channelFK;
            sqlParms.Add(sqlParm1);
            SQL.ExecuteStoredProcedure("DP001", "CreateUpdateSupplierInventory", sqlParms, channelFK);
        }

        public SupplierInventory Read(long id)
        {
            using (DP001Entities db = new DP001Entities())
            {
                return db.SupplierInventories.Find(id);
            }
        }

        public List<SupplierInventory> Read(Expression<Func<SupplierInventory, bool>> where, int take = 0, int skip = 0)
        {
            using (DP001Entities db = new DP001Entities())
            {
                var query = db.SupplierInventories
                    .Include("Brand")
                    .Include("Supplier")
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

        public List<string> ReadSuppBrands(int channelFk, int take = 0, int skip = 0)
        {
            using (DP001Entities db = new DP001Entities())
            {
                var query = db.SupplierInventories
                    .Where(x => x.ChannelFK == channelFk)
                    .OrderBy(x => x.OriginalBrand)
                    .DistinctBy(x => x.OriginalBrand)
                    .Select(x => x.OriginalBrand)
                    .AsQueryable();

                if (take > 0)
                {
                    query = query.Take(take).Skip(skip);
                }

                return query.ToList();
            }
        }

        public List<SupplierInventory> ReadOnly(Expression<Func<SupplierInventory, bool>> where, int take = 0, int skip = 0)
        {
            using (DP001Entities db = new DP001Entities())
            {
                var query = db.SupplierInventories
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

        public IPagedList<SupplierInventory> ReadPagedList(Expression<Func<SupplierInventory, bool>> where, int pageNumber, string sortField, string sortDir)
        {
            using (DP001Entities db = new DP001Entities())
            {
                var query = db.SupplierInventories
                    .Include("Brand")
                    .Include("ProductInventory")
                    .Include("Supplier")
                    .AsQueryable();

                query = query.OrderBy(sortField + " " + sortDir.ToUpper());
                query = query.Where(where);

                return query.ToPagedList(pageNumber, 200);
            }
        }

        public IQueryable<SupplierInventory> ReadSupplierInventoryQuery(
            Expression<Func<SupplierInventory, bool>> where,
            DP001Entities ctx)
        {
            var query = ctx.SupplierInventories
            .Include("Brand")
            .Include("ProductInventory")
            .Include("Supplier")
            .OrderBy(x => x.Description)
            .AsQueryable();

            query = query.Where(where);

            NoLockInterceptor.ApplyNoLock = true;

            return query;
        }

        public void Update(SupplierInventory obj)
        {
            using (DP001Entities db = new DP001Entities())
            {
                db.Entry(obj).State = EntityState.Modified;
                db.SaveChanges();
            }
        }

        public void Delete(SupplierInventory obj)
        {
            using (DP001Entities db = new DP001Entities())
            {
                db.Entry(obj).State = EntityState.Deleted;
                db.SaveChanges();
            }
        }

        //public void ClearDownTenant(TenantSetting setting)
        //{
        //    using (DP001Entities db = new DP001Entities())
        //    {
        //        db.SupplierInventories.RemoveRange(db.SupplierInventories.Where(x => x.TenantFK == setting.TenantID));
        //        db.SaveChanges();
        //    }
        //}

        public void UpdateList(List<SupplierInventory> supplierInventories)
        {
            using (DP001Entities db = new DP001Entities())
            {
                foreach (var item in supplierInventories)
                {
                    db.Entry(item).State = EntityState.Modified;
                }

                db.SaveChanges();
            }
        }
    }
}
