using DP001BusinessLogic.Shared;
using DP001DataAccess.Entities;
using PagedList;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DP001BusinessLogic
{
    public class CrudReports
    {
        public IQueryable<ProductInventoryDisplayModel> ReadProductsQuery(
            Expression<Func<ProductInventoryDisplayModel, bool>> where,
            DP001Entities db)
        {

            var query = from pi in db.ProductInventories
                            .Include("Brand")
                            .Include("SupplierInventories")
                            .Include("CompetitorInventories")
                            .Include("ProductCategory")
                            .Include("Lookup")
                            .Include("PriceRule")
                        join vi in db.ProductInventories on new { ch = pi.ChannelFK, cpid = pi.VariantOf } equals new { ch = vi.ChannelFK, cpid = vi.ClientProductID } into vis
                        from vi in vis.DefaultIfEmpty()
                        select new ProductInventoryDisplayModel
                        {
                            Pi = pi,
                            Vi = vi
                        };

            query = query.Where(where)
                .Distinct()
                .OrderBy(x => x.Pi.Description);

            NoLockInterceptor.ApplyNoLock = true;

            return query;
        }

        public IQueryable<ProductInventoryDisplayModel> ReadProductsStagingPricesQuery(
            Expression<Func<ProductInventoryDisplayModel, bool>> where,
            DP001Entities db)
        {

            var query = from pi in db.ProductInventories
                            .Include("Brand")
                            .Include("SupplierInventories")
                            .Include("CompetitorInventories")
                            .Include("ProductCategory")
                            .Include("Lookup")
                            .Include("PriceRule")
                            .Include("PriceStaging")
                        join vi in db.ProductInventories on new { ch = pi.ChannelFK, cpid = pi.VariantOf } equals new { ch = vi.ChannelFK, cpid = vi.ClientProductID } into vis
                        from vi in vis.DefaultIfEmpty()
                        select new ProductInventoryDisplayModel
                        {
                            Pi = pi,
                            Vi = vi
                        };

            query = query.Where(where)
                .Distinct()
                .OrderBy(x => x.Pi.Description);

            NoLockInterceptor.ApplyNoLock = true;

            return query;
        }

        //public IQueryable<ProductInventory> ReadProductsStagingPricesQuery(
        //    Expression<Func<ProductInventory, bool>> where,
        //    DP001Entities db)
        //{
        //    var query = db.ProductInventories
        //        .Include("Brand")
        //        .Include("SupplierInventories")
        //        .Include("CompetitorInventories")
        //        .Include("ProductCategory")
        //        .Include("Lookup")
        //        .Include("PriceRule")
        //        .Include("PriceStaging")
        //        .OrderBy(x => x.Description)
        //        .AsQueryable();

        //    query = query.Where(where);

        //    NoLockInterceptor.ApplyNoLock = true;

        //    return query;
        //}

        public class ProductInventoryDisplayModel
        {
            public ProductInventory Pi { get; set; }
            public ProductInventory Vi { get; set; }
        }
    }
}
