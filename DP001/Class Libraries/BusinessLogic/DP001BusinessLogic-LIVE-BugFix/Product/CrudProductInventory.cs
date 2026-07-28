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
    public class CrudProductInventory
    {
        public ProductInventory Create(ProductInventory obj)
        {
            using (DP001Entities db = new DP001Entities())
            {
                if (obj.ProductInventoryID == 0)
                {
                    db.Entry(obj).State = EntityState.Added;
                    db.SaveChanges();
                }

                return obj;
            }
        }

        public void Create(List<ProductInventory> products, int channelFK)
        {
            DataTable dt = new DataTable("StagingProductInventory");

            using (DP001Entities db = new DP001Entities())
            {
                dt.Columns.Add(new DataColumn("ProductInventoryFK", typeof(int)));
                dt.Columns.Add(new DataColumn("ChannelFK", typeof(int)));
                dt.Columns.Add(new DataColumn("BrandFK", typeof(int)));
                dt.Columns.Add(new DataColumn("ManufacturerPartNo", typeof(string)));
                dt.Columns.Add(new DataColumn("Description", typeof(string)));
                dt.Columns.Add(new DataColumn("ClientProductID", typeof(string)));
                dt.Columns.Add(new DataColumn("LnkdBrandFK", typeof(int)));
                dt.Columns.Add(new DataColumn("LnkdBrandManufacturerPartNo", typeof(string)));
                dt.Columns.Add(new DataColumn("ProductCategoryFK", typeof(long)));
                dt.Columns.Add(new DataColumn("Price", typeof(decimal)));
                dt.Columns.Add(new DataColumn("AltPrice1", typeof(decimal)));
                dt.Columns.Add(new DataColumn("AltPrice2", typeof(decimal)));
                dt.Columns.Add(new DataColumn("AltPrice3", typeof(decimal)));
                dt.Columns.Add(new DataColumn("AltPrice4", typeof(decimal)));
                dt.Columns.Add(new DataColumn("AltPrice5", typeof(decimal)));
                dt.Columns.Add(new DataColumn("AltPrice6", typeof(decimal)));
                dt.Columns.Add(new DataColumn("AltPrice7", typeof(decimal)));
                dt.Columns.Add(new DataColumn("AltPrice8", typeof(decimal)));
                dt.Columns.Add(new DataColumn("AltPrice9", typeof(decimal)));
                dt.Columns.Add(new DataColumn("AltPrice10", typeof(decimal)));
                dt.Columns.Add(new DataColumn("PriceRuleFK", typeof(int)));
                dt.Columns.Add(new DataColumn("CalculationOutcome", typeof(int)));
                dt.Columns.Add(new DataColumn("BeatRateNumber", typeof(int)));
                dt.Columns.Add(new DataColumn("StockQuantity", typeof(int)));
                dt.Columns.Add(new DataColumn("CheapestCostPrice", typeof(decimal)));
                dt.Columns.Add(new DataColumn("CheapestCompetitorPrice", typeof(decimal)));
                dt.Columns.Add(new DataColumn("GrossMarginPercent", typeof(decimal)));
                dt.Columns.Add(new DataColumn("GrossMarginValue", typeof(decimal)));
                dt.Columns.Add(new DataColumn("CompetitorDifference", typeof(decimal)));
                dt.Columns.Add(new DataColumn("MaximumMargin", typeof(decimal)));
                dt.Columns.Add(new DataColumn("MinimumMargin", typeof(decimal)));
                dt.Columns.Add(new DataColumn("DesiredMargin", typeof(decimal)));
                dt.Columns.Add(new DataColumn("IsKeyLine", typeof(bool)));

                foreach (ProductInventory pi in products)
                {
                    dt.Rows.Add(0, pi.ChannelFK, pi.BrandFK, pi.ManufacturerPartNo.Truncate(45), pi.Description.Truncate(200), pi.ClientProductID.Truncate(45), pi.LnkdBrandFK, 
                        pi.LnkdManufacturerPartNo.Truncate(45), pi.ProductCategoryFK, pi.Price, pi.AltPrice1, pi.AltPrice2, pi.AltPrice3, pi.AltPrice4, pi.AltPrice5,
                        pi.AltPrice6, pi.AltPrice7, pi.AltPrice8, pi.AltPrice9, pi.AltPrice10, pi.PriceRuleFK, pi.CalculationOutcome,
                        pi.BeatRateNumber, pi.StockQuantity, pi.CheapestCostPrice, pi.CheapestCompetitorPrice, pi.GrossMarginPercent,
                        pi.GrossMarginValue, pi.CompetitorDifference, pi.MaximumPrice, pi.MinimumPrice, pi.DesiredPrice,
                        pi.IsKeyLine);
                }
            }

            SQL.SQLBulkInsert(dt, "DP001");

            List<SqlParameter> sqlParms = new List<SqlParameter>();
            SqlParameter sqlParm1 = new SqlParameter("@ChannelFK", SqlDbType.Int);
            sqlParm1.Value = channelFK;
            sqlParms.Add(sqlParm1);
            SqlParameter sqlParm2 = new SqlParameter("@deleteUnmatched", SqlDbType.Bit);
            sqlParm2.Value = true;
            sqlParms.Add(sqlParm2);
            SQL.ExecuteStoredProcedure("DP001", "CreateUpdateProductInventory", sqlParms, channelFK);
        }

        public ProductInventory Read(long id)
        {
            using (DP001Entities db = new DP001Entities())
            {
                return db.ProductInventories
                    .Include("Brand")
                    .Where(x => x.ProductInventoryID == id)
                    .FirstOrDefault();
            }
        }

        public List<ProductInventory> Read(Expression<Func<ProductInventory, bool>> where, int take = 0, int skip = 0)
        {
            using (DP001Entities db = new DP001Entities())
            {
                var query = db.ProductInventories
                    .Include("Brand")
                    .Include("SupplierInventories.Supplier")
                    .Include("CompetitorInventories.Competitor")
                    .Include("PriceRule.Lookup")
                    .Include("PriceRule.Lookup1")
                    .Include("PriceRule.Brand")
                    .Include("PriceRule.ProductCategory")
                    .Include("PriceRule.ProductInventory")
                    .Include("ProductCategory")
                    .Include("Lookup")
                    .OrderBy(x => x.Description)
                    .AsQueryable();

                query = query.Where(where);

                if (take > 0)
                {
                    query = query.Take(take).Skip(skip);
                }

                return query.ToList();
            }
        }

        public ProductInventory Read(string mfpn, int channelId, string manufacturer = null)
        {
            using (DP001Entities db = new DP001Entities())
            {
                var query = db.ProductInventories
                    .Where(x => x.ManufacturerPartNo.ToLower() == mfpn.ToLower() &&
                    x.ChannelFK == channelId);

                if (manufacturer != null)
                    query = query.Where(x => x.Brand.BrandName.ToLower() == manufacturer.ToLower());

                return query.FirstOrDefault();
            }
        }

        public IPagedList<ProductInventory> ReadPagedList(Expression<Func<ProductInventory, bool>> where, int pageNumber, string sortField, string sortDir)
        {
            using (DP001Entities db = new DP001Entities())
            {
                var query = db.ProductInventories
                    .Include("Brand")
                    .Include("SupplierInventories")
                    .Include("CompetitorInventories")
                    .Include("ProductCategory")
                    .AsQueryable();

                query = query.OrderBy(sortField + " " + sortDir.ToUpper());
                query = query.Where(where);

                return query.ToPagedList(pageNumber, 200);
            }
        }

        public IQueryable<ProductInventory> ReadProductsQuery(
            Expression<Func<ProductInventory, bool>> where, 
            DP001Entities ctx)
        {
            var query = ctx.ProductInventories
                .Include("Brand")
                .Include("SupplierInventories")
                .Include("CompetitorInventories")
                .Include("ProductCategory")
                .OrderBy(x => x.Description)
                .AsQueryable();

            query = query.Where(where);

            NoLockInterceptor.ApplyNoLock = true;

            return query;
        }

        

        public List<ProductInventory> ReadAll()
        {
            using (DP001Entities db = new DP001Entities())
            {
                return db.ProductInventories.Take(10).ToList();
            }
        }

        public List<ProductInventory> Read(int take, int skip, Func<ProductInventory, bool> query = null)
        {
            using (DP001Entities db = new DP001Entities())
            {
                IQueryable<ProductInventory> productList = db.ProductInventories.OrderBy(x => x.Description);

                if (query != null)
                {
                    productList = productList.Where(query).AsQueryable();
                }

                return productList.Take(take).Skip(skip).ToList();
            }
        }

        public int ReadCount(int channelId)
        {
            using (DP001Entities db = new DP001Entities())
            {
                NoLockInterceptor.ApplyNoLock = true;
                return db.ProductInventories.Where(x => x.ChannelFK == channelId).Count();
            }
        }

        public int ReadSupplierExceptionCount(int channelId, int matchLimit)
        {
            using (DP001Entities db = new DP001Entities())
            {
                NoLockInterceptor.ApplyNoLock = true;
                var dd = db.ProductInventories.Where(x => x.ChannelFK == channelId &&
                    x.SupplierInventories.Count <= matchLimit);
                var ddd = dd.ToList().Count;

                return db.ProductInventories.Where(x => x.ChannelFK == channelId && 
                    x.SupplierInventories.Count <= matchLimit).ToList().Count;
            }
        }

        public int ReadCompetitorExceptionCount(int channelId, int matchLimit)
        {
            using (DP001Entities db = new DP001Entities())
            {
                NoLockInterceptor.ApplyNoLock = true;
                return db.ProductInventories.Where(x => x.ChannelFK == channelId &&
                    x.CompetitorInventories.Count <= matchLimit).ToList().Count;
            }
        }

        public void Update(ProductInventory obj)
        {
            using (DP001Entities db = new DP001Entities())
            {
                db.Entry(obj).State = EntityState.Modified;
                db.SaveChanges();
            }
        }

        public void Update(List<ProductInventory> products)
        {
            using (DP001Entities db = new DP001Entities())
            {
                products.ForEach(x => db.Entry(x).State = EntityState.Modified);
                db.SaveChanges();
            }
        }

        public void Delete(ProductInventory obj)
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
        //        db.ProductInventories.RemoveRange(db.ProductInventories.Where(x => x.TenantFK == setting.TenantID));
        //        db.SaveChanges();
        //    }
        //}
    }
}
