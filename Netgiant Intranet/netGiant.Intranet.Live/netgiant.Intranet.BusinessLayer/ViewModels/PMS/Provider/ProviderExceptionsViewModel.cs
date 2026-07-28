using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;
using PagedList;
using netGiant.Intranet.DataLayer;
using System.Web.Mvc;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Provider
{
    public class ProviderExceptionsViewModel
    {
        public ProviderExceptionsViewModel()
        {
            AllWebsites = SelectListViewModel.AllWebsites();
            AllManufacturers = SelectListViewModel.AllManufacturers();
        }

        public IPagedList<CustomInventoryModel> ProviderInventoryExcList { get; set; }
        public IQueryable<SelectListItem> AllWebsites { get; set; }
        public List<SelectListItem> AllManufacturers { get; set; }

        public ProviderExceptionsViewModel GetExceptions()
        {
            return GetExceptions(1, null, true, null, true, null, null, null);
        }

        public ProviderExceptionsViewModel GetExceptions(int pageNumber, string orderBy, bool? inStock,
            string reason, bool hideUnwanted, int? manufacturerFK, int? categoryCodeFK, bool? cheapest)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                var qq = db.providerInventory.Include("skuMapping.product.websiteInventory")
                    .Include("skuMapping.product.manufacturer")
                    .Select(x => new CustomInventoryModel
                    {
                        Categories = x.skuMapping.FirstOrDefault().product.websiteInventory.Select(c => c.categoryCodeFK),
                        ProductFK = x.skuMapping.FirstOrDefault().product.productID,
                        PartNo = x.skuMapping.FirstOrDefault().product.partNo,
                        Manufacturer = x.skuMapping.FirstOrDefault().product.manufacturer.manufacturerName,
                        ManufacturerFK = x.skuMapping.FirstOrDefault().product.manufacturerFK,
                        ProductName = x.skuMapping.FirstOrDefault().product.productName,
                        Stock = x.quantity,
                        ProviderInventoryFK = x.providerInventoryID,
                        ProviderPartNo = x.providerPartNo,
                        Provider = x.provider.providerName,
                        PotentialNew = x.potentialNewProduct,
                        Untrusted = x.untrustedProvider,
                        ProviderTypeFK = x.provider.providerTypeFK,
                        Date = x.dateLastUpdate,
                        LatestPrice = x.providerPrice.OrderByDescending(p => p.providerPriceID).FirstOrDefault().price,
                        PreviousPrice = x.providerPrice.OrderByDescending(p => p.providerPriceID).Skip(1).Take(1).FirstOrDefault().price,
                        Change = x.providerPrice.OrderByDescending(p => p.providerPriceID).FirstOrDefault().price -
                            x.providerPrice.OrderByDescending(p => p.providerPriceID).Skip(1).Take(1).FirstOrDefault().price,

                        ChangePercentage = (x.providerPrice.OrderByDescending(p => p.providerPriceID).FirstOrDefault().price -
                            x.providerPrice.OrderByDescending(p => p.providerPriceID).Skip(1).Take(1).FirstOrDefault().price) /
                            x.providerPrice.OrderByDescending(p => p.providerPriceID).Skip(1).Take(1).FirstOrDefault().price,

                        Reason = x.providerPrice.OrderByDescending(p => p.providerPriceID).FirstOrDefault().price >
                            x.providerPrice.OrderByDescending(p => p.providerPriceID).Skip(1).Take(1).FirstOrDefault().price
                            ? "Price Rise" : "Price Fall",

                        CheapestPrice = db.skuMapping.Where(y => y.productFK == x.skuMapping.FirstOrDefault().productFK).ToList()
                            .OrderBy(p => p.providerInventory.providerPrice.OrderBy(b => b.price).FirstOrDefault().price)
                            .FirstOrDefault().providerInventory.providerPrice.OrderBy(n => n.price).FirstOrDefault().price
                            == x.providerPrice.OrderByDescending(w => w.providerPriceID).FirstOrDefault().price ? true : false
                    });

                qq = SetOrderByClause(qq, orderBy, reason);
                qq = SetWhereClause(qq, inStock, reason, hideUnwanted, manufacturerFK, categoryCodeFK, cheapest);

                NoLockInterceptor.ApplyNoLock = true;

                ProviderInventoryExcList = qq.ToPagedList(pageNumber, 50);

            }

            return this;
        }

        private IQueryable<CustomInventoryModel> SetWhereClause(IQueryable<CustomInventoryModel> q,
            bool? inStock, string reason, bool hideUntrusted, int? manufacturerFK, int? categoryCodeFK, bool? cheapest)
        {
            q = q.Where(x => x.PreviousPrice != null && x.PreviousPrice > 0 &&
                            x.PotentialNew == false &&
                            x.ProviderTypeFK == 2);

            if (inStock == true)
                q = q.Where(x => x.Stock > 0);

            if (hideUntrusted == true)
                q = q.Where(x => x.Untrusted == false);

            if (manufacturerFK != null && manufacturerFK > 0)
                q = q.Where(x => x.ManufacturerFK == manufacturerFK);

            if (categoryCodeFK != null && categoryCodeFK > 0)
                q = q.Where(x => x.Categories.Any(c => c.Value == categoryCodeFK));

            if (cheapest == true)
                q = q.Where(x => x.CheapestPrice);

            switch (reason)
            {
                case "priceRise":
                    q = q.Where(x => x.LatestPrice >= x.PreviousPrice);
                    break;
                case "priceFall":
                    q = q.Where(x => x.LatestPrice <= x.PreviousPrice);
                    break;
            }

            return q;
        }

        private IQueryable<CustomInventoryModel> SetOrderByClause(IQueryable<CustomInventoryModel> query,
            string orderBy, string reason)
        {
            switch (orderBy)
            {
                case "priceChangeAsc":
                    query = query.OrderBy(x => x.LatestPrice - x.PreviousPrice)
                    .ThenByDescending(x => x.Stock);
                    break;
                case "priceChangeDesc":
                    query = query.OrderByDescending(x => x.LatestPrice - x.PreviousPrice)
                    .ThenByDescending(x => x.Stock);
                    break;
                case "priceChangePercentAsc":
                    query = query.OrderBy(x => x.ChangePercentage)
                    .ThenByDescending(x => x.Stock);
                    break;
                case "priceChangePercentDesc":
                    query = query.OrderByDescending(x => x.ChangePercentage)
                    .ThenByDescending(x => x.Stock);
                    break;
                case "previousPriceAsc":
                    query = query.OrderBy(x => x.PreviousPrice)
                        .ThenByDescending(x => x.Stock);
                    break;
                case "previousPriceDesc":
                    query = query.OrderByDescending(x => x.PreviousPrice)
                        .ThenByDescending(x => x.Stock);
                    break;
                case "latestPriceAsc":
                    query = query.OrderBy(x => x.LatestPrice)
                        .ThenByDescending(x => x.Stock);
                    break;
                case "latestPriceDesc":
                    query = query.OrderByDescending(x => x.LatestPrice)
                        .ThenByDescending(x => x.Stock);
                    break;
                case "providerAsc":
                    query = query.OrderBy(x => x.Provider)
                        .ThenByDescending(x => x.Stock);
                    break;
                case "providerDesc":
                    query = query.OrderByDescending(x => x.Provider)
                        .ThenByDescending(x => x.Stock);
                    break;
                case "providerPartNoAsc":
                    query = query.OrderBy(x => x.ProviderPartNo)
                        .ThenByDescending(x => x.Stock);
                    break;
                case "providerPartNoDesc":
                    query = query.OrderByDescending(x => x.ProviderPartNo)
                        .ThenByDescending(x => x.Stock);
                    break;
                case "partNoAsc":
                    query = query.OrderBy(x => x.PartNo)
                        .ThenByDescending(x => x.Stock);
                    break;
                case "partNoDesc":
                    query = query.OrderByDescending(x => x.PartNo)
                        .ThenByDescending(x => x.Stock);
                    break;
                case "productNameAsc":
                    query = query.OrderBy(x => x.ProductName)
                        .ThenByDescending(x => x.Stock);
                    break;
                case "productNameDesc":
                    query = query.OrderByDescending(x => x.ProductName)
                        .ThenByDescending(x => x.Stock);
                    break;
                case "stockAsc":
                    query = query.OrderBy(x => x.Stock)
                        .ThenBy(x => x.PreviousPrice - x.LatestPrice);
                    break;
                case "stockDesc":
                    query = query.OrderByDescending(x => x.Stock)
                        .ThenByDescending(x => x.PreviousPrice - x.LatestPrice);
                    break;
                case "dateAsc":
                    query = query.OrderBy(x => x.ProviderInventoryFK);
                    break;
                case "dateDesc":
                    query = query.OrderByDescending(x => x.ProviderInventoryFK);
                    break;
                default:
                    if (reason == "priceRise")
                    {
                        query = query.OrderByDescending(x => x.ChangePercentage)
                    .ThenByDescending(x => x.Stock);
                    }
                    else
                    {
                        query = query.OrderBy(x => x.ChangePercentage)
                    .ThenByDescending(x => x.Stock);
                    }

                    break;
            }

            return query;
        }
    }

    public class CustomInventoryModel
    {
        public IEnumerable<int?> Categories { get; set; }
        public int ProductFK { get; set; }
        public string PartNo { get; set; }
        public string Manufacturer { get; set; }
        public int? ManufacturerFK { get; set; }
        public int ProviderInventoryFK { get; set; }
        public string ProviderPartNo { get; set; }
        public string Provider { get; set; }
        public bool? PotentialNew { get; set; }
        public bool? Untrusted { get; set; }
        public int ProviderTypeFK { get; set; }
        public string ProductName { get; set; }
        public int? Stock { get; set; }
        public double? LatestPrice { get; set; }
        public double? PreviousPrice { get; set; }
        public double? Change { get; set; }
        public double ChangePercentage { get; set; }
        public DateTime Date { get; set; }
        public string Reason { get; set; }
        public bool CheapestPrice { get; set; }
    }
}
