using PagedList;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using netGiant.Intranet.DataLayer;
using System.Web.Mvc;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using System.Data.Entity;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Pricing
{
    public class PricingRulesViewModel
    {
        public List<priceRule> priceRuleList { get; set; }
        public PagedList.IPagedList<priceRuleBand> priceRuleBandList { get; set; }
        public IQueryable<SelectListItem> allCategories { get; set; }
        public IQueryable<SelectListItem> allWebsites { get; set; }
        public List<SelectListItem> allManufacturers { get; set; }
        public IQueryable<SelectListItem> allRuleTypes { get; set; }
        public IQueryable<SelectListItem> allproducts { get; set; }
        public IQueryable<SelectListItem> allProductGroups { get; set; }
        public IQueryable<SelectListItem> AllProductItemTypes { get; set; }
        public priceRule priceRuleSingle { get; set; }
        public priceRuleBand priceRuleBandSingle { get; set; }
        public int selectedWebsiteFK { get; set; }
        public int selectedCategoryCodeFK { get; set; }
        public int selectedRuleTypeFK { get; set; }
        public int? selectedPageNumber { get; set; }
        public string selectedOrderBy { get; set; }
        public List<priceRuleBand> priceRuleBandsEditList { get; set; }
        public bool isCostUpliftPercentage { get; set; }
        public string priceRuleBandsToDelete { get; set; }
        public int PriceRulesCount { get; set; }

        public PricingRulesViewModel Get(int? block, string orderBy, int? websiteFK, int? categoryCodeFK,
            int? ruleTypeFK)
        {
            int blockSize = 50;
            int blockNumber = (block ?? 1);

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    IQueryable<priceRule> list = db.priceRule.Include("CategoryCode.website").Include("manufacturer")
                                                    .Include("product").Include("priceRuleType")
                                                    .Include("productGroup")
                                                    .OrderBy(x => x.priceRuleID);

                    if (websiteFK != null && websiteFK != 0)
                    {
                        list = list.Where(x => x.categoryCode.websiteFK == websiteFK);
                    }

                    if (categoryCodeFK != null && categoryCodeFK != 0)
                    {
                        list = list.Where(x => x.categoryCodeFK == categoryCodeFK);
                    }

                    if (ruleTypeFK != null && ruleTypeFK > 0)
                    {
                        list = list.Where(x => x.ruleType == ruleTypeFK);
                    }

                    switch (orderBy)
                    {
                        case "categoryCodeAsc":
                            list = list.OrderBy(x => x.categoryCode.categoryCodeName);
                            break;
                        case "categoryCodeDesc":
                            list = list.OrderByDescending(x => x.categoryCode.categoryCodeName);
                            break;
                        case "ruleTypeAsc":
                            list = list.OrderBy(x => x.priceRuleType.ruleName);
                            break;
                        case "ruleTypeDesc":
                            list = list.OrderByDescending(x => x.priceRuleType.ruleName);
                            break;
                        case "manufacturerAsc":
                            list = list.OrderBy(x => x.manufacturer.manufacturerName);
                            break;
                        case "manufacturerDesc":
                            list = list.OrderByDescending(x => x.manufacturer.manufacturerName);
                            break;
                        case "productAsc":
                            list = list.OrderBy(x => x.product.productName);
                            break;
                        case "productDesc":
                            list = list.OrderByDescending(x => x.product.productName);
                            break;
                        case "bandingAsc":
                            list = list.OrderBy(x => x.useBanding);
                            break;
                        case "bandingDesc":
                            list = list.OrderByDescending(x => x.useBanding);
                            break;
                        case "costUpliftAsc":
                            list = list.OrderBy(x => x.costUplift);
                            break;
                        case "costUpliftDesc":
                            list = list.OrderByDescending(x => x.costUplift);
                            break;
                        case "desiredMarginAsc":
                            list = list.OrderBy(x => x.desiredMargin);
                            break;
                        case "desiredMarginDesc":
                            list = list.OrderByDescending(x => x.desiredMargin);
                            break;
                        case "minMarginAsc":
                            list = list.OrderBy(x => x.minMargin);
                            break;
                        case "minMarginDesc":
                            list = list.OrderByDescending(x => x.minMargin);
                            break;
                        case "maxMarginAsc":
                            list = list.OrderBy(x => x.maxMargin);
                            break;
                        case "maxMarginDesc":
                            list = list.OrderByDescending(x => x.maxMargin);
                            break;
                        case "compsToBeatAsc":
                            list = list.OrderBy(x => x.competitorsToBeat);
                            break;
                        case "compsToBeatDesc":
                            list = list.OrderByDescending(x => x.competitorsToBeat);
                            break;
                        case "nudgeAsc":
                            list = list.OrderBy(x => x.nudge);
                            break;
                        case "nudgeDesc":
                            list = list.OrderByDescending(x => x.nudge);
                            break;
                        case "descriptionAsc":
                            list = list.OrderBy(x => x.description);
                            break;
                        case "descriptionDesc":
                            list = list.OrderByDescending(x => x.description);
                            break;
                        case "websiteAsc":
                            list = list.OrderBy(x => x.categoryCode.Website.FriendlyName);
                            break;
                        case "websiteDesc":
                            list = list.OrderByDescending(x => x.categoryCode.Website.FriendlyName);
                            break;
                        default:
                            list = list.OrderBy(x => x.description);
                            break;
                    }

                    NoLockInterceptor.ApplyNoLock = true;

                    PriceRulesCount = list.Count();
                    priceRuleList = list.Skip((blockNumber - 1) * blockSize).Take(blockSize).ToList();
                    allCategories = SelectListViewModel.AllCategoryCodes(null, true);
                    allWebsites = SelectListViewModel.AllWebsites();
                    allRuleTypes = SelectListViewModel.AllRuleTypes();
                }
            }
            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace, e.InnerException);
            }

            return this;
        }

        public PricingRulesViewModel GetPricingBands(int priceRuleID)
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    IQueryable<priceRuleBand> list = db.priceRuleBand.OrderBy(x => x.priceRuleBandID).Where(x => x.priceRuleFK == priceRuleID);
                    priceRuleBandList = list.ToPagedList(1, 24);
                    isCostUpliftPercentage = db.priceRule.Find(priceRuleID).costUpliftIsPercent;
                }
            }
            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace, e.InnerException);
            }

            return this;
        }

        public static PricingRulesViewModel CreatePricingRule(int id)
        {
            PricingRulesViewModel model = new PricingRulesViewModel();

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    if (id > 0)
                    {
                        model.priceRuleSingle = db.priceRule.Include("priceRuleBand").Include("categoryCode")
                                            .Include("CategoryCode.website").Include("product")
                                            .Include("productGroup")
                                            .SingleOrDefault(m => m.priceRuleID == id);
                    }
                    else
                    {
                        model.priceRuleSingle = new priceRule();
                    }

                    model.allManufacturers = SelectListViewModel.AllManufacturers();
                    model.allCategories = SelectListViewModel.AllCategoryCodes(null, true);
                    model.allRuleTypes = SelectListViewModel.AllRuleTypes();
                    model.allproducts = SelectListViewModel.AllProductsPartNoDesc();
                    model.allWebsites = SelectListViewModel.AllWebsites();
                    model.allProductGroups = SelectListViewModel.AllProductGroups();
                    model.priceRuleBandsEditList = db.priceRuleBand.Where(x => x.priceRuleFK == id).ToList();
                }
            }

            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace, e.InnerException);
            }

            return model;
        }

        public int SavePricingRule()
        {
            int newID = 0;

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    
                    if (priceRuleSingle.costUpliftIsPercent)
                    {
                        priceRuleSingle.costUplift = priceRuleSingle.costUplift / 100;
                    }

                    priceRuleSingle.desiredMargin = priceRuleSingle.desiredMargin / 100;
                    priceRuleSingle.minMargin = priceRuleSingle.minMargin / 100;
                    priceRuleSingle.maxMargin = priceRuleSingle.maxMargin / 100;
                    priceRuleSingle.competitorsToBeat = priceRuleSingle.competitorsToBeat / 100;
                    priceRuleSingle.nudge = priceRuleSingle.nudge / 100;
                    priceRuleSingle.productFK = priceRuleSingle.productFK == 0 ? null : priceRuleSingle.productFK;
                    priceRuleSingle.breakPrice1 = priceRuleSingle.breakPrice1 / 100;
                    priceRuleSingle.breakPrice2 = priceRuleSingle.breakPrice2 / 100;
                    priceRuleSingle.breakPrice3 = priceRuleSingle.breakPrice3 / 100;
                    priceRuleSingle.breakPrice4 = priceRuleSingle.breakPrice4 / 100;
                    priceRuleSingle.breakPrice5 = priceRuleSingle.breakPrice5 / 100;
                    priceRuleSingle.compatDiscount = priceRuleSingle.compatDiscount / 100;
                    priceRuleSingle.compatOverrideMargin = priceRuleSingle.compatOverrideMargin / 100;
                    priceRuleSingle.packDiscount = priceRuleSingle.packDiscount / 100;
                    priceRuleSingle.finalBreakMinimumMarginStock = priceRuleSingle.finalBreakMinimumMarginStock / 100;
                    priceRuleSingle.finalBreakMinimumMarginAssemblies = priceRuleSingle.finalBreakMinimumMarginAssemblies / 100;

                    if (priceRuleSingle.priceRuleID > 0)
                    {
                        foreach (var band in priceRuleSingle.priceRuleBand)
                        {
                            SetBands(db, band);
                        }

                        db.SaveChanges();

                        List<int> newBandIds = priceRuleSingle.priceRuleBand.Select(x => x.priceRuleBandID).ToList();
                        List<int> oldBandIds = db.priceRuleBand.Where(x => x.priceRuleFK == priceRuleSingle.priceRuleID)
                            .Select(x => x.priceRuleBandID).ToList();

                        foreach (int id in oldBandIds)
                        {
                            if (!newBandIds.Contains(id))
                            {
                                db.Entry(db.priceRuleBand.Find(id)).State = EntityState.Deleted;
                            }
                        }

                        db.Entry(priceRuleSingle).State = EntityState.Modified;
                    }
                    else
                    {
                        //Check if a pricing rule already exists for specified criteria
                        //CheckRuleExists(db);
                        foreach (var band in priceRuleSingle.priceRuleBand)
                        {
                            SetBands(db, band);
                        }
                        db.Entry(priceRuleSingle).State = EntityState.Added;
                    }

                    db.SaveChanges();
                }

                newID = priceRuleSingle.priceRuleID;
            }
            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace, e.InnerException);
            }

            return newID;
        }

        private void SetBands(ngmdEntities db, priceRuleBand band)
        {
            band.priceRuleFK = priceRuleSingle.priceRuleID;
            band.desiredMargin = band.desiredMargin / 100;
            band.minMargin = band.minMargin / 100;
            band.maxMargin = band.maxMargin / 100;
            band.competitorsToBeat = band.competitorsToBeat / 100;
            band.nudge = band.nudge / 100;
            band.breakPrice1 = band.breakPrice1 / 100;
            band.breakPrice2 = band.breakPrice2 / 100;
            band.breakPrice3 = band.breakPrice3 / 100;
            band.breakPrice4 = band.breakPrice4 / 100;
            band.breakPrice5 = band.breakPrice5 / 100;
            band.compatDiscount = band.compatDiscount / 100;
            band.compatOverrideMargin = band.compatOverrideMargin / 100;
            band.packDiscount = band.packDiscount / 100;
            band.finalBreakMinimumMarginStock = band.finalBreakMinimumMarginStock / 100;
            band.finalBreakMinimumMarginAssemblies = band.finalBreakMinimumMarginAssemblies / 100;

            if (band.priceRuleBandID > 0)
            {
                db.Entry(band).State = EntityState.Modified;
            }
            else
            {
                db.Entry(band).State = EntityState.Added;
            }
        }

        private void CheckRuleExists(ngmdEntities db)
        {
            priceRule pr = new priceRule();

            switch (priceRuleSingle.ruleType)
            {
                case 1:
                    pr = db.priceRule.Where(x => x.ruleType == 1 &&
                        x.categoryCodeFK == priceRuleSingle.categoryCodeFK &&
                        x.manufacturerFK == null &&
                        x.productFK == null && x.productGroupFK == null).FirstOrDefault();
                    break;
                case 2:
                    pr = db.priceRule.Where(x => x.ruleType == 2 &&
                        x.categoryCodeFK == priceRuleSingle.categoryCodeFK &&
                        x.manufacturerFK == priceRuleSingle.manufacturerFK &&
                        x.productFK == null &&
                        x.productGroupFK == null).FirstOrDefault();
                    break;
                case 3:
                    pr = db.priceRule.Where(x => x.ruleType == 3 &&
                        x.categoryCodeFK == priceRuleSingle.categoryCodeFK &&
                        (x.manufacturerFK == null || x.manufacturerFK == priceRuleSingle.manufacturerFK) &&
                        x.productFK == priceRuleSingle.productFK &&
                        (x.productGroupFK == null || x.productGroupFK == priceRuleSingle.productGroupFK)).FirstOrDefault();
                    break;
                default:
                    break;
            }

            if (pr != null)
                throw new Exception("Price Rule already exists for specified criteria.");
        }

        //public void SavePricingRuleBandings(int priceRuleID)
        //{
        //    using (ngmdEntities db = new ngmdEntities())
        //    {
        //        try
        //        {
        //            if (priceRuleSingle.useBanding)
        //            {
        //                if (priceRuleBandsToDelete != null)
        //                {
        //                    string[] bandsToDelete = priceRuleBandsToDelete.Split(',');
        //                    foreach (string band in bandsToDelete)
        //                    {
        //                        if (band != "")
        //                        {
        //                            db.priceRuleBand.Remove(db.priceRuleBand.Find(Convert.ToInt32(band)));
        //                        }
        //                    }
        //                }

        //                if (priceRuleBandsEditList != null)
        //                {
        //                    foreach (var item in priceRuleBandsEditList)
        //                    {
        //                        if (priceRuleSingle.costUpliftIsPercent)
        //                        {
        //                            item.costUplift = item.costUplift / 100;
        //                        }

        //                        item.desiredMargin = item.desiredMargin / 100;
        //                        item.minMargin = item.minMargin / 100;
        //                        item.maxMargin = item.maxMargin / 100;
        //                        item.competitorsToBeat = item.competitorsToBeat / 100;
        //                        item.nudge = item.nudge / 100;
        //                        item.breakPrice1 = item.breakPrice1 / 100;
        //                        item.breakPrice2 = item.breakPrice2 / 100;
        //                        item.breakPrice3 = item.breakPrice3 / 100;
        //                        item.breakPrice4 = item.breakPrice4 / 100;
        //                        item.breakPrice5 = item.breakPrice5 / 100;
        //                        item.packDiscount = item.packDiscount / 100;
        //                        item.compatDiscount = item.compatDiscount / 100;
        //                        item.compatOverrideMargin = item.compatOverrideMargin / 100;

        //                        item.priceRuleFK = priceRuleID;

        //                        if (item.priceRuleBandID > 0)
        //                        {
        //                            db.Entry(item).State = EntityState.Modified;
        //                        }
        //                        else
        //                        {
        //                            db.Entry(item).State = EntityState.Added;
        //                        }
        //                    }
        //                }
        //            }
        //            else
        //            {
        //                db.priceRuleBand.RemoveRange(db.priceRuleBand.Where(x => x.priceRuleFK == priceRuleSingle.priceRuleID));
        //            }

        //            db.SaveChanges();
        //        }
        //        catch (Exception e)
        //        {
        //            throw new ApplicationException(e.Message + e.StackTrace, e.InnerException);
        //        }
        //    }
        //}

        public void DeletePriceRule(int id)
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    db.priceRuleBand.RemoveRange(db.priceRuleBand.Where(x => x.priceRuleFK == id));
                    db.priceRule.Remove(db.priceRule.Find(id));
                    db.SaveChanges();
                }
            }

            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace, e.InnerException);
            }
        }

        public priceRuleBand CreatePriceRuleBand(int id)
        {
            try
            {
                priceRuleBand band = new priceRuleBand();

                if (id > 0)
                {
                    using (ngmdEntities db = new ngmdEntities())
                    {
                        band = db.priceRuleBand.Find(id);
                    }
                }

                return band;
            }
            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace, e.InnerException);
            }
        }
    }
}
