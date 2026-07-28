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
        //public IPagedList<ProductInventory> ReadProductsPagedList(
        //    Expression<Func<ProductInventory, bool>> where,
        //    int pageNumber,
        //    string sortOrder,
        //    string searchTerm)
        //{
        //    using (DP001Entities db = new DP001Entities())
        //    {
        //        var query = db.ProductInventories
        //            .Include("Brand")
        //            .Include("SupplierInventories")
        //            .Include("CompetitorInventories")
        //            .Include("ProductCategory")
        //            .Include("Lookup")
        //            .Include("PriceRule")
        //            .AsQueryable();

        //        query = query.Where(where);

        //        if (searchTerm != null)
        //        {
        //            query = query.Where(x => x.Description.Contains(searchTerm) ||
        //                x.ManufacturerPartNo.Contains(searchTerm) ||
        //                x.Brand.BrandName.Contains(searchTerm) ||
        //                x.ProductCategory.CategoryName.Contains(searchTerm));
        //        }

        //        //query = SortQuery(query, sortOrder);

        //        return query.ToPagedList(pageNumber, 200);
        //    }
        //}

        public IQueryable<ProductInventory> ReadProductsQuery(
            Expression<Func<ProductInventory, bool>> where,
            DP001Entities db)
        {
            var query = db.ProductInventories
                .Include("Brand")
                .Include("SupplierInventories")
                .Include("CompetitorInventories")
                .Include("ProductCategory")
                .Include("Lookup")
                .Include("PriceRule")
                .OrderBy(x => x.Description)
                .AsQueryable();

            query = query.Where(where);

            NoLockInterceptor.ApplyNoLock = true;

            return query;
        }

        public IQueryable<ProductInventory> ReadProductsStagingPricesQuery(
            Expression<Func<ProductInventory, bool>> where,
            DP001Entities db)
        {
            var query = db.ProductInventories
                .Include("Brand")
                .Include("SupplierInventories")
                .Include("CompetitorInventories")
                .Include("ProductCategory")
                .Include("Lookup")
                .Include("PriceRule")
                .Include("PriceStaging")
                .OrderBy(x => x.Description)
                .AsQueryable();

            query = query.Where(where);

            NoLockInterceptor.ApplyNoLock = true;

            return query;
        }
    }
}
