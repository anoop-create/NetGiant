using DP001BusinessLogic.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DP001DataAccess.Entities;
using System.Data.Entity;
using System.Linq.Expressions;
using PagedList;
using MoreLinq;

namespace DP001BusinessLogic
{
    public class CrudSkuMapping
    {
        public SKUMapping Create(SKUMapping obj)
        {
            using (DP001Entities db = new DP001Entities())
            {
                var recordExists = db.SKUMappings
                    .Where(x => x.ChannelFK == obj.ChannelFK &&
                        x.SKUMapFrom == obj.SKUMapFrom &&
                        x.SKUMapTo == obj.SKUMapTo &&
                        x.InventoryFK == obj.InventoryFK)
                    .ToList().Count > 0;

                if (!recordExists)
                {
                    db.Entry(obj).State = EntityState.Added;
                    db.SaveChanges();
                }

                return obj;
            }
        }

        public SkuMappingsDisplayModel Read(int id)
        {
            using (DP001Entities db = new DP001Entities())
            {
                return (from sk in db.SKUMappings
                        where sk.SKUMappingID == id
                        join si in db.SupplierInventories
                            on new { A = sk.SKUMapFrom, B = sk.BrandFK } equals new { A = si.ManufacturerPartNo, B = si.BrandFK }
                        join pi in db.ProductInventories
                            on new { A = sk.SKUMapTo, B = sk.BrandFK } equals new { A = pi.ManufacturerPartNo, B = pi.BrandFK }
                        join br in db.Brands on pi.BrandFK equals br.BrandID
                        select new SkuMappingsDisplayModel
                        {
                            Supp = si,
                            Prod = pi,
                            Type = "Supplier",
                            Brnd = br,
                            SkuMappingId = sk.SKUMappingID
                        }
                        ).FirstOrDefault();
            }
        }

        public SkuMappingEditModel ReadSkuMap(int id, int channelId)
        {
            using (DP001Entities db = new DP001Entities())
            {
                //var sku = db.SKUMappings.Find(id);
                var sku = db.SKUMappings
                    .Include(x => x.Brand)
                    .Include(x => x.Lookup)
                    .FirstOrDefault(x => x.SKUMappingID == id && x.ChannelFK == channelId);

                SkuMappingEditModel details = null;

                if (sku != null)
                {
                    details = new SkuMappingEditModel()
                    {
                        Mapping = sku,
                        Product = db.ProductInventories
                        .Where(x => x.ChannelFK == channelId && x.ManufacturerPartNo == sku.SKUMapTo)
                        .FirstOrDefault(),
                        Supplier = db.SupplierInventories
                        .Where(x => x.ChannelFK == channelId && x.ManufacturerPartNo == sku.SKUMapFrom)
                        .FirstOrDefault(),
                        Competitor = db.CompetitorInventories
                        .Where(x => x.ChannelFK == channelId && x.ManufacturerPartNo == sku.SKUMapFrom)
                        .FirstOrDefault()
                    };
                }

                return details;
            }
        }

        public IQueryable<SkuMappingsDisplayModel> ReadMappingsQuery(
            int channelId,
            DP001Entities ctx)
        {
            NoLockInterceptor.ApplyNoLock = true;

            var crudLookup = new CrudLookup();
            var supplierFileTypeID = crudLookup.Read(x => x.LookupName == "Supplier Inventory").First().LookupID;
            var competitorFileTypeID = crudLookup.Read(x => x.LookupName == "Competitor Inventory").First().LookupID;

            var query = from sk in ctx.SKUMappings
                        join si in ctx.SupplierInventories
                            on new { A = sk.SKUMapFrom, B = sk.BrandFK, C = sk.ChannelFK, D = sk.FileTypeFK } equals new { A = si.ManufacturerPartNo, B = si.BrandFK, C = si.ChannelFK, D = supplierFileTypeID } into sis
                        from si in sis.DefaultIfEmpty()
                        join ci in ctx.CompetitorInventories
                            on new { A = sk.SKUMapFrom, B = sk.BrandFK, C = sk.ChannelFK, D = sk.FileTypeFK } equals new { A = ci.ManufacturerPartNo, B = ci.BrandFK, C = ci.ChannelFK, D = competitorFileTypeID } into cis
                        from ci in cis.DefaultIfEmpty()
                        join pi in ctx.ProductInventories
                            on new { A = sk.SKUMapTo, B = sk.BrandFK, C = sk.ChannelFK } equals new { A = pi.ManufacturerPartNo, B = pi.BrandFK, C = pi.ChannelFK } into pis
                        from pi in pis.DefaultIfEmpty()
                        join br in ctx.Brands
                            on new { A = pi.BrandFK, B = pi.ChannelFK } equals new { A = br.BrandID, B = br.ChannelFK } into brs
                        from br in brs.DefaultIfEmpty()
                        join sup in ctx.Suppliers
                            on new { A = sk.InventoryFK, B = pi.ChannelFK } equals new { A = sup.SupplierID, B = sup.ChannelFK } into sups
                        from sup in sups.DefaultIfEmpty()
                        join look in ctx.Lookups
                            on sk.FileTypeFK equals look.LookupID
                        where sk.ChannelFK == channelId && pi != null
                        select new SkuMappingsDisplayModel
                        {
                            Supp = si,
                            Prod = pi,
                            Type = look.LookupName.Replace(" Inventory", ""),
                            Brnd = br,
                            SkuMappingId = sk.SKUMappingID,
                            SupplierRecord = sup,
                            Comp = ci,
                            SupplierCompetitorName = si != null ? si.Supplier.SupplierName : ci.Competitor.CompetitorName
                        };

            query = query.DistinctBy(x => new { x.Prod.ManufacturerPartNo, c = x.Supp != null ? x.Supp.Supplier.SupplierName : x.Comp != null ? x.Comp.Competitor.CompetitorName : "" }).OrderBy(x => x.Brnd.BrandName).AsQueryable();

            return query.OrderBy(x => x.Prod.ManufacturerPartNo).ThenBy(x => x.Brnd.BrandName);

        }

        public List<string> GetSkuMapSuppliersCompetitorsList(int channelId, DP001Entities ctx)
        {
            var list = new List<string>();
            var crudLookup = new CrudLookup();
            var supplierFileTypeID = crudLookup.Read(x => x.LookupName == "Supplier Inventory").First().LookupID;
            var competitorFileTypeID = crudLookup.Read(x => x.LookupName == "Competitor Inventory").First().LookupID;

            var query = from sk in ctx.SKUMappings
                        join sup in ctx.Suppliers
                            on new { A = sk.InventoryFK, B = channelId } equals new { A = sup.SupplierID, B = sup.ChannelFK } into sups
                        from sup in sups.DefaultIfEmpty()
                        join com in ctx.Competitors
                            on new { A = sk.InventoryFK, B = channelId } equals new { A = com.CompetitorID, B = com.ChannelFK } into coms
                        from com in coms.DefaultIfEmpty()
                        where sk.ChannelFK == channelId
                        select new
                        {
                            SUP = sup,
                            COM = com
                        };

            list.AddRange(query.Where(x => x.SUP != null).ToList().Select(x => x.SUP?.SupplierName));
            list.AddRange(query.Where(x => x.COM != null).ToList().Select(x => x.COM?.CompetitorName));
            list = list.OrderBy(x => x).ToList();
            list.Insert(0, "Please Select...");

            return list.ToList();
        }

        public SKUMapping ReadSingle(int id)
        {
            using (DP001Entities db = new DP001Entities())
            {
                return db.SKUMappings
                    .Where(x => x.SKUMappingID == id).FirstOrDefault();
            }
        }

        public Boolean ReadCheck(int brandId, string partNo)
        {
            bool isFound = false;
            using (DP001Entities db = new DP001Entities())
            {
                SKUMapping skuMap = db.SKUMappings
                    .Where(x => x.BrandFK == brandId && x.SKUMapTo == partNo).FirstOrDefault();
                isFound = (skuMap == null) ? false : true;
            }
            return isFound;
        }

        public void Update(SKUMapping obj)
        {
            using (DP001Entities db = new DP001Entities())
            {
                db.Entry(obj).State = EntityState.Modified;
                db.SaveChanges();
            }
        }

        public void Delete(SKUMapping obj)
        {
            using (DP001Entities db = new DP001Entities())
            {
                db.Entry(obj).State = EntityState.Deleted;
                db.SaveChanges();
            }
        }

        public IQueryable<SkuMappingsDisplayModel> ReadProductSupplierExceptionsQuery(
            int channelId,
            DP001Entities ctx,
            int supplierMatchLimit)
        {
            var query = from pi in ctx.ProductInventories
                        join si in ctx.SupplierInventories on pi.ProductInventoryID equals si.ProductInventoryFK into sis
                        from si in sis.DefaultIfEmpty()
                        join br in ctx.Brands on pi.BrandFK equals br.BrandID into brs
                        from br in brs.DefaultIfEmpty()
                        join pc in ctx.ProductCategories on pi.ProductCategoryFK equals pc.ProductCategoryID into pcs
                        from pc in pcs.DefaultIfEmpty()
                        where pi.ChannelFK == channelId && pi.SupplierInventories.Count <= supplierMatchLimit
                                && pi.Lookup1.LookupName == "Active"
                        select new SkuMappingsDisplayModel
                        {
                            Supp = si,
                            Prod = pi,
                            Type = "Supplier",
                            Brnd = br
                        };

            query = query
                .Distinct()
                .OrderBy(x => x.Prod.Description);

            return query;
        }

        public IQueryable<SkuMappingsDisplayModel> ReadProductCompetitorExceptionsQuery(
            int channelId,
            DP001Entities ctx,
            int matchLimit)
        {
            var query = from pi in ctx.ProductInventories
                        join ci in ctx.CompetitorInventories on pi.ProductInventoryID equals ci.ProductInventoryFK into cis
                        from ci in cis.DefaultIfEmpty()
                        join br in ctx.Brands on pi.BrandFK equals br.BrandID into brs
                        from br in brs.DefaultIfEmpty()
                        join pc in ctx.ProductCategories on pi.ProductCategoryFK equals pc.ProductCategoryID into pcs
                        from pc in pcs.DefaultIfEmpty()
                        where pi.ChannelFK == channelId && pi.CompetitorInventories.Count <= matchLimit
                                && pi.Lookup1.LookupName == "Active"
                        select new SkuMappingsDisplayModel
                        {
                            Comp = ci,
                            Prod = pi,
                            Type = "Competitor",
                            Brnd = br
                        };

            query = query
                .Distinct()
                .OrderBy(x => x.Prod.Description);

            return query;
        }

        public IPagedList<ProductInventory> ReadProductsPagedList(Expression<Func<ProductInventory, bool>> where, int pageNumber)
        {
            using (DP001Entities db = new DP001Entities())
            {
                var query = db.ProductInventories
                    .Include("Brand")
                    .Include("SupplierInventories")
                    .Include("CompetitorInventories")
                    .Include("ProductCategory")
                    .OrderBy(x => x.Description)
                    .AsQueryable();

                query = query.Where(where);
                query = query.Where(x => x.ManufacturerPartNo == "TX112233");

                return query.ToPagedList(pageNumber, 200);
            }
        }

        public List<SupplierInventory> GetSuggestedSupplierMappings(ProductInventory product)
        {
            using (DP001Entities db = new DP001Entities())
            {
                var results = db.GetSuggestedSupplierMappings(product.ChannelFK, product.ManufacturerPartNo, product.BrandFK).ToList();
                var suggestedMappings = new List<SupplierInventory>();

                foreach (var s in results)
                {
                    suggestedMappings.Add(new SupplierInventory()
                    {
                        SupplierInventoryID = s.SupplierInventoryID,
                        ChannelFK = s.ChannelFK,
                        SupplierFK = s.SupplierFK,
                        BrandFK = s.BrandFK,
                        ManufacturerPartNo = s.ManufacturerPartNo,
                        StockQuantity = s.StockQuantity,
                        Price = s.Price,
                        ProductInventoryFK = s.ProductInventoryFK,
                        Description = s.Description,
                        Dismiss = s.Dismiss,
                        Brand = new Brand() { BrandID = s.BrandFK, BrandName = s.BrandName },
                        Supplier = new Supplier() { SupplierID = s.SupplierFK, SupplierName = s.SupplierName }
                    });
                }

                return suggestedMappings;

                //    var suppliers = db.SupplierInventories
                //        .Include(x => x.Brand)
                //        .Include(x => x.Supplier)
                //        .Where(x => (x.ManufacturerPartNo.Contains(CommonFunctions.ReplaceSpecialCharacters(product.ManufacturerPartNo)) ||
                //            product.ManufacturerPartNo.Contains(CommonFunctions.ReplaceSpecialCharacters(x.ManufacturerPartNo))) &&
                //            x.ChannelFK == product.ChannelFK &&
                //            x.BrandFK == product.BrandFK &&
                //            x.ProductInventoryFK != null)
                //        .Select(x => x.SupplierFK)
                //        .ToList();

                //    return db.SupplierInventories
                //        .Include(x => x.Brand)
                //        .Include(x => x.Supplier)
                //        .Where(x => (x.ManufacturerPartNo.Contains(CommonFunctions.ReplaceSpecialCharacters(product.ManufacturerPartNo)) ||
                //            product.ManufacturerPartNo.Contains(CommonFunctions.ReplaceSpecialCharacters(x.ManufacturerPartNo))) &&
                //            x.ChannelFK == product.ChannelFK &&
                //            x.BrandFK == product.BrandFK &&
                //            x.ProductInventoryFK == null &&
                //            !suppliers.Contains(x.SupplierFK) && 
                //            x.Dismiss == false)
                //        .ToList();
            }
        }

        //public List<SKUMapping> ReadSkuMappings(Expression<Func<SKUMapping, bool>> where)
        //{
        //    using (DP001Entities db = new DP001Entities())
        //    {
        //        return db.SKUMappings.Include(x => x.Brand).Where(where).ToList();
        //    }
        //}

        public List<CompetitorInventory> GetSuggestedCompetitorMappings(ProductInventory product)
        {
            using (DP001Entities db = new DP001Entities())
            {
                var competitors = db.CompetitorInventories
                    .Include(x => x.Brand)
                    .Include(x => x.Competitor)
                    .Where(x => (x.ManufacturerPartNo.Contains(product.ManufacturerPartNo) ||
                        product.ManufacturerPartNo.Contains(x.ManufacturerPartNo)) &&
                        x.ChannelFK == product.ChannelFK &&
                        x.BrandFK == product.BrandFK &&
                        x.ProductInventoryFK != null)
                    .Select(x => x.CompetitorFK)
                    .ToList();

                return db.CompetitorInventories
                    .Include(x => x.Brand)
                    .Include(x => x.Competitor)
                    .Where(x => (x.ManufacturerPartNo.Contains(product.ManufacturerPartNo) ||
                        product.ManufacturerPartNo.Contains(x.ManufacturerPartNo)) &&
                        x.ChannelFK == product.ChannelFK &&
                        x.BrandFK == product.BrandFK &&
                        x.ProductInventoryFK == null &&
                        !competitors.Contains(x.CompetitorFK) &&
                        x.Dismiss == false)
                    .ToList();
            }
        }
    }

    public class SkuMappingsDisplayModel
    {
        public string Type { get; set; }
        public SupplierInventory Supp { get; set; }
        public ProductInventory Prod { get; set; }
        public CompetitorInventory Comp { get; set; }
        public Brand Brnd { get; set; }
        public long SkuMappingId { get; set; }
        public Supplier SupplierRecord { get; set; }
        public string SupplierCompetitorName { get; set; }
    }

    public class SkuMappingEditModel
    {
        public SKUMapping Mapping { get; set; }
        public ProductInventory Product { get; set; }
        public SupplierInventory Supplier { get; set; }
        public CompetitorInventory Competitor { get; set; }
    }
}
