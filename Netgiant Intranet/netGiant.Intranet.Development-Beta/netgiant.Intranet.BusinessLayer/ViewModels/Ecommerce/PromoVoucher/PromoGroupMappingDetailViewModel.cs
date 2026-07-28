using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;

namespace netGiant.Intranet.BusinessLayer.ViewModels.Ecommerce.PromoVoucher
{
    public class PromoGroupMappingDetailViewModel : CommonViewModel
    {
        public class TelerikCategoryTreeItem
        {
            public int CategoryId;
            public string CategoryName;
            public List<TelerikCategoryTreeItem> SubCategories;
        }

        public List<TelerikCategoryTreeItem> CategoriesHierarchyTopLevel { get; set; }

        public PromoGroupMappingDetailViewModel()
        {
            _ctx = new ngmdEntities();
        }

        private ngmdEntities _ctx;
        public VoucherPromoGroup PromoGroup { get; set; }

        public IQueryable<TelerikGroupMappings> PromoGroupMappingsList { get; set; }

        public class TelerikGroupMappings
        {
            public int CategoryId { get; set; }
            public string CategoryName { get; set; }
            public int VoucherPromoGroupMappingId { get; set; }
        }

        public void PopulateCategoryTree(int promoVoucherGroupFK)
        {
            PromoGroup = _ctx.VoucherPromoGroup.Where(x => x.VoucherPromoGroupId == promoVoucherGroupFK).FirstOrDefault();
            var webSiteFK = PromoGroup.WebsiteFk;

            BuildCategoryCodeHierarchy(webSiteFK);
        }

        IQueryable<TelerikGroupMappings> PromoGroupMappingsListTest { get; set; }
        public PromoGroupMappingDetailViewModel GetPromoGroupMappingDetailViewModel(int promoVoucherGroupFK) // voucher group id, not a mapping
        {
            // 1 promo group = many category codes
            PromoGroup = _ctx.VoucherPromoGroup.Where(x => x.VoucherPromoGroupId == promoVoucherGroupFK).FirstOrDefault();

            // 1 promo group to many category codes
            PromoGroupMappingsList = _ctx.VoucherPromoGroupMapping
            .Include(x => x.categoryCode)
            .Where(x => x.VoucherPromoGroupFk == promoVoucherGroupFK)
            .OrderBy(x => x.VoucherPromoGroup.GroupName)
            .Select(x => new TelerikGroupMappings
            {
                CategoryId = x.CategoryCodeFk,
                CategoryName = x.categoryCode.categoryCodeName,
                VoucherPromoGroupMappingId = x.VoucherPromoGroupMappingId
            })
            .AsQueryable();

            return this;
        }

        public SaveReturn CreatePromoGroupMapping(int voucherGroupId, string categoryIds)
        {
            var sr = new SaveReturn();

            var categories = categoryIds.Split(',');

            using (ngmdEntities db = new ngmdEntities())
            {
                try
                {
                    // Do there exist any categories in the DB for current voucher group? If so, don't add duplicate any
                    foreach (var category in categories)
                    {
                        var categoryCode = int.Parse(category);

                        var categoriesLinkedToVoucherGroup = db.VoucherPromoGroupMapping
                        .Include(x => x.categoryCode)
                        .Where(x => x.VoucherPromoGroupFk == voucherGroupId)
                        .Where(x => x.CategoryCodeFk == categoryCode);

                        if (categoriesLinkedToVoucherGroup.Count() > 0)
                        {
                            sr.IsSuccess = false;
                            sr.Message = "Category " + category + " is already mapped to the voucher group";
                            return sr;
                        }
                    }

                    foreach (var category in categories)
                    {
                        var pg = db.Set<VoucherPromoGroupMapping>();
                        pg.Add(
                            new VoucherPromoGroupMapping
                            {
                                VoucherPromoGroupFk = voucherGroupId,
                                CategoryCodeFk = int.Parse(category)
                            });

                        db.SaveChanges();
                    }

                    sr.IsSuccess = true;
                }
                catch (Exception ex)
                {
                    sr.IsSuccess = false;
                    sr.Message = ex.Message;
                }
            }

            return sr;
        }

        public static PromoGroupMappingDetailViewModel Create(int promoVoucherGroupFK) // voucher group id, not a mapping
        {
            PromoGroupMappingDetailViewModel model = new PromoGroupMappingDetailViewModel();

            try
            {
                if (promoVoucherGroupFK > 0)
                    model.PopulateCategoryTree(promoVoucherGroupFK);
                // GetPromoGroupMappingDetailViewModel will be called by the Telerik grid
            }

            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }

            return model;
        }

        public SaveReturn Delete(int id)
        {
            var sr = new SaveReturn();

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    VoucherPromoGroupMapping pgrpmapping = db.VoucherPromoGroupMapping.Find(id);
                    db.VoucherPromoGroupMapping.Remove(pgrpmapping);
                    db.SaveChanges();

                    sr.IsSuccess = true;
                }
            }
            catch (Exception e)
            {
                sr.IsSuccess = false;
                sr.Message = e.Message;
                throw new ApplicationException(e.Message + e.StackTrace);
            }

            return sr;
        }

        private void BuildCategoryCodeStructureRecursive(IQueryable<categoryCode> parentDBQuery, List<TelerikCategoryTreeItem> parentViewLevel)
        {
            foreach (var code in parentDBQuery)
            {
                var TelerikCategoryTreeItem = new TelerikCategoryTreeItem
                {
                    CategoryId = code.categoryCodeID,
                    CategoryName = code.isPrimary ? code.categoryCodeName + " (P)" : code.categoryCodeName + " (S)",
                    SubCategories = new List<TelerikCategoryTreeItem>()
                };
                parentViewLevel.Add(TelerikCategoryTreeItem);

                BuildCategoryCodeStructureRecursive(GetCategoryCodeChildren(code).AsQueryable(), TelerikCategoryTreeItem.SubCategories);
            }
        }

        private void BuildCategoryCodeHierarchy(int websiteID = 0)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                IQueryable<categoryCode> query = db.categoryCode;
                CategoriesHierarchyTopLevel = new List<TelerikCategoryTreeItem>();

                // no parent = 1st level categories 
                if (websiteID > 0)
                {
                    query = query.Where(x => x.websiteFK == websiteID && x.parentCategoryCodeID == null).OrderBy(x => x.categoryCodeName);
                }
                else
                {
                    // all websites
                    query = query.Where(x => x.parentCategoryCodeID == null).OrderBy(x => x.categoryCodeName);
                }

                BuildCategoryCodeStructureRecursive(query, CategoriesHierarchyTopLevel);
            }
        }

        private List<categoryCode> GetCategoryCodeChildren(categoryCode code)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                return db.categoryCode.Where(x => x.parentCategoryCodeID == code.categoryCodeID).OrderBy(x => x.categoryCodeName).ToList();
            }
        }

    }
}
