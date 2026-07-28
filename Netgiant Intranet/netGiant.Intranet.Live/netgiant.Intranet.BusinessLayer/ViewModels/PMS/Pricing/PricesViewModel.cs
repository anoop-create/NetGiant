using netGiant.Intranet.DataLayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PagedList;
using System.Web.Mvc;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using System.Linq.Expressions;
using System.Data.Entity;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Pricing
{
    public class PricesViewModel
    {
        public PricesViewModel()
        {
            AllManufacturers = SelectListViewModel.AllManufacturers();
            AllCategoryCodes = SelectListViewModel.AllCategoryCodes(1, true);
        }

        public List<websiteInventory> PriceComparison { get; set; }
        public List<SelectListItem> AllManufacturers { get; set; }
        public IQueryable<SelectListItem> AllCategoryCodes { get; set; }
        public int selectedManufacturerID { get; set; }
        public int selectedCategoryCodeID { get; set; }
        public int SelectedWebsiteFK { get; set; }
        public List<CategoryCodeDropDown> CategoryCodeDropDowns { get; set; }
        public List<providerInventory> CompetitorPrices { get; set; }
        public IQueryable<SelectListItem> AllProductItemType { get; set; }
        public IQueryable<SelectListItem> AllWebsites { get; set; }
        public int SelectProductItemTypeFK { get; set; }
        public int PriceComparisonCount { get; set; }
        public List<PMSAxisPriceDifference> PMSAxisPriceDifferenceList { get; set; }
        public int PMSAxisPriceDifferenceCount { get; set; }
        public IQueryable<SelectListItem> AllProductStatus { get; set; }
        public IQueryable<SelectListItem> AllProductGroups { get; set; }
        public int SelectedProductStatusFK { get; set; }
        public int SelectedProductGroupFK { get; set; }

        public PricesViewModel GetPriceComparison(int? block, int? manufacturerFK, int? categoryCodeFK,
                                                    double? priceFrom, double? priceTo,
                                                    string orderBy, string searchBy, string searchTerm,
                                                    bool inStock, int? compKey, int? bestKey, int? productItemTypeFK,
                                                    int? websiteFK, int? productGroupFK)
        {
            int blockSize = 50;
            int blockNumber = (block ?? 1);

            using (ngmdEntities db = new ngmdEntities())
            {
                IQueryable<websiteInventory> list = db.websiteInventory.Include(x => x.product.skuMapping)
                    .Include(x => x.categoryCode).Include(x => x.product.manufacturer)
                    .Include(x => x.productPrice).Include(x => x.Website);

                list = setWhere(list, searchBy, searchTerm, manufacturerFK, categoryCodeFK, priceFrom,
                        priceTo, inStock, compKey, bestKey, productItemTypeFK, websiteFK, productGroupFK);
                list = setOrderBy(list, orderBy);

                NoLockInterceptor.ApplyNoLock = true;

                PriceComparisonCount = list.Count();
                PriceComparison = list.Skip((blockNumber - 1) * blockSize).Take(blockSize).ToList();
                AllProductItemType = SelectListViewModel.GetAllProductItemType();
                AllWebsites = SelectListViewModel.AllWebsites();
                AllProductGroups = SelectListViewModel.AllProductGroups();
            }

            return this;
        }

        IQueryable<websiteInventory> setOrderBy(IQueryable<websiteInventory> list, string orderBy)
        {
            switch (orderBy)
            {
                case "partNoAsc":
                    list = list.OrderBy(m => m.product.partNo);
                    break;
                case "partNoDesc":
                    list = list.OrderByDescending(m => m.product.partNo);
                    break;
                case "productNameAsc":
                    list = list.OrderBy(m => m.product.productName);
                    break;
                case "productNameDesc":
                    list = list.OrderByDescending(m => m.product.productName);
                    break;
                case "manufacturerAsc":
                    list = list.OrderBy(m => m.product.manufacturer.manufacturerID);
                    break;
                case "manufacturerDesc":
                    list = list.OrderByDescending(m => m.product.manufacturer.manufacturerID);
                    break;
                case "pmsPriceAsc":
                    list = list.OrderBy(m => m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().price);
                    break;
                case "pmsPriceDesc":
                    list = list.OrderByDescending(m => m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().price);
                    break;
                case "differenceAsc":
                    list = list.OrderBy(m => m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().price -
                        m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().cheapestCompetitorPrice);
                    break;
                case "differenceDesc":
                    list = list.OrderByDescending(m => m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().price -
                        m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().cheapestCompetitorPrice);
                    break;
                case "stockAsc":
                    list = list.OrderBy(m => m.product.supplierStock);
                    break;
                case "stockDesc":
                    list = list.OrderByDescending(m => m.product.supplierStock);
                    break;
                case "categoryAsc":
                    list = list.OrderBy(m => m.categoryCode.categoryCodeName);
                    break;
                case "categoryDesc":
                    list = list.OrderByDescending(m => m.categoryCode.categoryCodeName);
                    break;
                case "costPriceAsc":
                    list = list.OrderBy(m => m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().cheapestCostPrice);
                    break;
                case "costPriceDesc":
                    list = list.OrderByDescending(m => m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().cheapestCostPrice);
                    break;
                case "priceDiffAsc":
                    list = list.OrderBy(m => m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().price -
                        m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().cheapestCostPrice);
                    break;
                case "priceDiffDesc":
                    list = list.OrderByDescending(m => m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().price -
                        m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().cheapestCostPrice);
                    break;
                case "break1DiffAsc":
                    list = list.OrderBy(m => m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().breakPrice1 -
                        m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().cheapestCostPrice);
                    break;
                case "break1DiffDesc":
                    list = list.OrderByDescending(m => m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().breakPrice1 -
                        m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().cheapestCostPrice);
                    break;
                case "break2DiffAsc":
                    list = list.OrderBy(m => m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().breakPrice2 -
                        m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().cheapestCostPrice);
                    break;
                case "break2DiffDesc":
                    list = list.OrderByDescending(m => m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().breakPrice2 -
                        m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().cheapestCostPrice);
                    break;
                case "priceMarginAsc":
                    list = list.OrderBy(m => ((m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().price -
                        m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().cheapestCostPrice) /
                        (m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault() == null ||
                        m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().price == 0 ? 1 :
                        m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().price) * 100));
                    break;
                case "priceMarginDesc":
                    list = list.OrderByDescending(m => ((m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().price -
                        m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().cheapestCostPrice) /
                        (m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault() == null ||
                        m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().price == 0 ? 1 :
                        m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().price) * 100));
                    break;
                case "break1MarginAsc":
                    list = list.OrderBy(m => ((m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().breakPrice1 -
                        m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().cheapestCostPrice) /
                        (m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault() == null ||
                        m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().breakPrice1 == 0 ? 1 :
                        m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().breakPrice1) * 100));
                    break;
                case "break1MarginDesc":
                    list = list.OrderByDescending(m => ((m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().breakPrice1 -
                        m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().cheapestCostPrice) /
                        (m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault() == null ||
                        m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().breakPrice1 == 0 ? 1 :
                        m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().breakPrice1) * 100));
                    break;
                case "break2MarginAsc":
                    list = list.OrderBy(m => ((m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().breakPrice2 -
                        m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().cheapestCostPrice) /
                        (m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault() == null ||
                        m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().breakPrice2 == 0 ? 1 :
                        m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().breakPrice2) * 100));
                    break;
                case "break2MarginDesc":
                    list = list.OrderByDescending(m => ((m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().breakPrice2 -
                        m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().cheapestCostPrice) /
                        (m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault() == null ||
                        m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().breakPrice2 == 0 ? 1 :
                        m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().breakPrice2) * 100));
                    break;
                case "competitorCountAsc":
                    list = list.OrderBy(m => m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().competitorCount);
                    break;
                case "competitorCountDesc":
                    list = list.OrderByDescending(m => m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().competitorCount);
                    break;
                case "cheapestCompetitorPriceAsc":
                    list = list.OrderBy(m => m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().cheapestCompetitorPrice);
                    break;
                case "cheapestCompetitorPriceDesc":
                    list = list.OrderByDescending(m => m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().cheapestCompetitorPrice);
                    break;
                case "pricingRuleAsc":
                    list = list.OrderBy(m => m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().pricingRule);
                    break;
                case "pricingRuleDesc":
                    list = list.OrderByDescending(m => m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().pricingRule);
                    break;
                case "basePriceAsc":
                    list = list.OrderBy(m => m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().basePrice);
                    break;
                case "basePriceDesc":
                    list = list.OrderByDescending(m => m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().basePrice);
                    break;
                case "websiteAsc":
                    list = list.OrderBy(m => m.websiteFK);
                    break;
                case "websiteDesc":
                    list = list.OrderByDescending(m => m.websiteFK);
                    break;
                case "breakPricingRuleAsc":
                    list = list.OrderBy(m => m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().breakPricingRule);
                    break;
                case "breakPricingRuleDesc":
                    list = list.OrderByDescending(m => m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().breakPricingRule);
                    break;
                default:
                    list = list.OrderByDescending(m => m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().price -
                        m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().cheapestCompetitorPrice);
                    break;
            }

            return list;
        }


        IQueryable<websiteInventory> setWhere(IQueryable<websiteInventory> list, string searchBy, string searchTerm,
                                            int? manufacturerFK, int? categoryCodeFK, double? priceFrom, double? priceTo,
                                            bool inStock, int? compKey, int? bestKey, int? productItemTypeFK,
                                            int? websiteFK, int? productGroupFK)
        {
            if (manufacturerFK != null && manufacturerFK > 0)
            {
                list = list.Where(m => m.product.manufacturerFK == manufacturerFK);
            }

            if (categoryCodeFK != null && categoryCodeFK > 0)
            {
                list = list.Where(m => m.categoryCodeFK == categoryCodeFK);
            }

            if (priceFrom != null)
            {
                list = list.Where(m => m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().cheapestCostPrice > priceFrom);
            }

            if (priceTo != null)
            {
                list = list.Where(m => m.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().cheapestCostPrice < priceTo);
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                switch (searchBy)
                {
                    case "name":
                        list = list.Where(m => m.product.productName.Contains(searchTerm));
                        break;
                    case "partNo":
                        list = list.Where(m => m.product.partNo.Contains(searchTerm));
                        break;
                    default:
                        break;
                }
            }

            if (inStock)
            {
                list = list.Where(x => x.product.supplierStock > 0);
            }

            if (compKey != null)
            {
                switch (compKey)
                {
                    case 0:
                        list = list.Where(x => x.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().competitorCount < 0);
                        break;
                    case 1:
                        list = list.Where(x => x.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().competitorCount == 0);
                        break;
                    case 10:
                        list = list.Where(x => x.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().competitorCount == 1);
                        break;
                    case 11:
                        list = list.Where(x => x.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().competitorCount < 2);
                        break;
                    case 100:
                        list = list.Where(x => x.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().competitorCount > 1);
                        break;
                    case 101:
                        list = list.Where(x => x.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().competitorCount == 0 ||
                            x.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().competitorCount > 1);
                        break;
                    case 110:
                        list = list.Where(x => x.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().competitorCount > 0);
                        break;
                    //case 111:
                        //All records
                }
            }

            if (bestKey != null)
            {
                switch (bestKey)
                {
                    case 0:
                        list = list.Where(x => x.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().competitorCount < 0);
                        break;
                    case 1:
                        list = list.Where(x => x.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().price <
                            x.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().cheapestCompetitorPrice);
                        break;
                    case 10:
                        list = list.Where(x => x.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().cheapestCompetitorPrice <
                            x.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault().price);
                        break;
                    //case 11:
                    //All records
                }
            }

            if (productItemTypeFK != null && productItemTypeFK > 0)
                list = list.Where(x => x.product.productItemTypeFK == productItemTypeFK);

            if (websiteFK != null && websiteFK > 0)
                list = list.Where(x => x.websiteFK == websiteFK);

            if (productGroupFK != null && productGroupFK > 0)
                list = list.Where(x => x.product.productGroupFK == productGroupFK);

            return list;
        }

        public List<CategoryCodeDropDown> GetAllCategoryCodes(int? websiteID = null, bool primaryOnly = false)
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    IQueryable<categoryCode> list = db.categoryCode.Include("priceRule")
                                                                    .Include("websiteInventory")
                                                                    .OrderBy(x => x.categoryCodeName);

                    if (websiteID != null)
                    {
                        list = list.Where(x => x.websiteFK == websiteID);
                    }

                    if (primaryOnly)
                        list = list.Where(x => x.isPrimary == true);

                    list = list.Where(x => !x.categoryCodeName.Contains("_OLD"));

                    List<CategoryCodeDropDown> ccdList = new List<CategoryCodeDropDown>();

                    foreach (categoryCode cc in list)
                    {
                        CategoryCodeDropDown ccd = new CategoryCodeDropDown();
                        ccd.Text = cc.categoryCodeName.ToString();
                        ccd.Value = cc.categoryCodeID;

                        if (cc.priceRule == null)
                        {
                            ccd.NoCategoryFallback = true;
                        }
                        else
                        {
                            priceRule fallback = cc.priceRule.Where(x => x.manufacturerFK == null && x.productFK == null).FirstOrDefault();
                            if (fallback == null)
                            {
                                ccd.NoCategoryFallback = true;
                            }
                            else
                            {
                                ccd.NoCategoryFallback = false;
                            }
                        }

                        ccd.ProductCount = cc.websiteInventory.Count();
                        ccdList.Add(ccd);
                    }

                    CategoryCodeDropDowns = ccdList;
                }
            }
            catch (InvalidOperationException e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }

            return CategoryCodeDropDowns;
        }

        public PricesViewModel GetPMSAxisPriceComparison(int? block, int? websiteFK, string orderBy,
            int? manufacturerFK, string searchTerm, int? productItemTypeFK, int? productStatusFK)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                int blockSize = 50;
                int blockNumber = (block ?? 1);

                IQueryable<PMSAxisPriceDifference> query = db.PMSAxisPriceDifference;

                switch (orderBy)
                {
                    case "priceDiffAsc":
                        query = query.OrderBy(x => x.priceDiff);
                        break;
                    case "priceDiffDesc":
                        query = query.OrderByDescending(x => x.priceDiff);
                        break;
                    default:
                        query = query.OrderByDescending(x => x.priceDiff);
                        break;
                }

                if (websiteFK != null && websiteFK > 0)
                    query = query.Where(x => x.websiteFK == websiteFK);

                if (manufacturerFK != null  && manufacturerFK > 0)
                    query = query.Where(x => x.manufacturerFK == manufacturerFK);

                if (!string.IsNullOrEmpty(searchTerm))
                    query = query.Where(x => x.partNo.ToLower().Contains(searchTerm.ToLower()));

                if (productItemTypeFK != null && productItemTypeFK > 0)
                    query = query.Where(x => x.productItemTypeFK == productItemTypeFK);

                if (productStatusFK != null && productStatusFK > 0)
                    query = query.Where(x => x.productStatusFK == productStatusFK);

                //NoLockInterceptor.ApplyNoLock = true;

                PMSAxisPriceDifferenceCount = query.Count();
                PMSAxisPriceDifferenceList = query.Skip((blockNumber - 1) * blockSize).Take(blockSize).ToList();
                AllWebsites = SelectListViewModel.AllWebsites();
                AllManufacturers = SelectListViewModel.AllManufacturers();
                AllProductItemType = SelectListViewModel.GetAllProductItemType();
                AllProductStatus = SelectListViewModel.AllProductStatuses();
            }

            return this;
        }
    }
}
