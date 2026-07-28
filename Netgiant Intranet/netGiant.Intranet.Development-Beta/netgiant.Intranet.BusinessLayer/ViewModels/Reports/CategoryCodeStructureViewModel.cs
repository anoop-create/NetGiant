using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace netGiant.Intranet.BusinessLayer.ViewModels.Reports
{
    public class CategoryCodeStructureViewModel : CommonViewModel
    {
        public List<categoryCode> CatList { get; set; }
        public IQueryable<SelectListItem> WebsiteNameList { get; set; }
        public categoryCode CategoryCode { get; set; }
        public List<product> ProductList { get; set; }

        public void BuildCategoryCodeStructure()
        {
            BuildCategoryCodeStructure(null);
        }

        public void BuildCategoryCodeStructure(int? websiteID)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                IQueryable<categoryCode> query = db.categoryCode
                    .Include("Website");

                if (websiteID != null && websiteID > 0)
                {
                    query = query.Where(x => x.websiteFK == websiteID && x.parentCategoryCodeID == null).OrderBy(x => x.categoryCodeName);
                }
                else
                {
                    query = query.Where(x => x.parentCategoryCodeID == null);
                }

                foreach (var code in query)
                {
                    code.Children = GetChildren(code);
                    code.ProductCount = db.websiteInventory.Where(x => x.websiteFK == websiteID && x.categoryCodeFK == code.categoryCodeID).Count();
                    code.ProductCount += db.secondaryCategoryLookup.Where(x => x.categoryCodeFK == code.categoryCodeID).Count();

                    if (code.Children.Count > 0)
                    {
                        foreach (var code2 in code.Children)
                        {
                            code2.Children = GetChildren(code2);
                            code2.ProductCount = db.websiteInventory.Where(x => x.websiteFK == websiteID && x.categoryCodeFK == code2.categoryCodeID).Count();
                            code2.ProductCount += db.secondaryCategoryLookup.Where(x => x.categoryCodeFK == code2.categoryCodeID).Count();

                            foreach (var code3 in code2.Children)
                            {
                                code3.Children = GetChildren(code3);
                                code3.ProductCount = db.websiteInventory.Where(x => x.websiteFK == websiteID && x.categoryCodeFK == code3.categoryCodeID).Count();
                                code3.ProductCount += db.secondaryCategoryLookup.Where(x => x.categoryCodeFK == code3.categoryCodeID).Count();

                                foreach (var code4 in code3.Children)
                                {
                                    code4.Children = GetChildren(code4);
                                    code4.ProductCount = db.websiteInventory.Where(x => x.websiteFK == websiteID && x.categoryCodeFK == code4.categoryCodeID).Count();
                                    code4.ProductCount += db.secondaryCategoryLookup.Where(x => x.categoryCodeFK == code4.categoryCodeID).Count();

                                    foreach (var code5 in code4.Children)
                                    {
                                        code5.Children = GetChildren(code5);
                                        code5.ProductCount = db.websiteInventory.Where(x => x.websiteFK == websiteID && x.categoryCodeFK == code5.categoryCodeID).Count();
                                        code5.ProductCount += db.secondaryCategoryLookup.Where(x => x.categoryCodeFK == code5.categoryCodeID).Count();

                                        foreach (var code6 in code5.Children)
                                        {
                                            code6.Children = GetChildren(code6);
                                            code6.ProductCount = db.websiteInventory.Where(x => x.websiteFK == websiteID && x.categoryCodeFK == code6.categoryCodeID).Count();
                                            code6.ProductCount += db.secondaryCategoryLookup.Where(x => x.categoryCodeFK == code6.categoryCodeID).Count();
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                WebsiteNameList = GetWebsiteNames();
                CatList = query.ToList();
            }
        }

        private List<categoryCode> GetChildren(categoryCode code)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                return db.categoryCode.Where(x => x.parentCategoryCodeID == code.categoryCodeID).OrderBy(x => x.categoryCodeName).ToList();
            }
        }

        private IQueryable<SelectListItem> GetWebsiteNames()
        {
            IQueryable<SelectListItem> query;

            using (ngmdEntities db = new ngmdEntities())
            {
                query = db.Website.OrderBy(x => x.WebsiteName).Select(x => new SelectListItem
                {
                    Value = x.WebsiteID.ToString(),
                    Text = x.FriendlyName.ToString()
                }).ToList().AsQueryable();
            }
            return query;
        }

        public CategoryCodeStructureViewModel GetCategoryProducts(int categoryCodeID)
        {
            ProductList = new List<product>();

            using (ngmdEntities db = new ngmdEntities())
            {
                var category = db.categoryCode
                    .Include("websiteInventory.product")
                    .Include("websiteInventory.secondaryCategoryLookup.websiteInventory.product")
                    .Where(x => x.categoryCodeID == categoryCodeID)
                    .FirstOrDefault();

                category.websiteInventory
                    .ToList()
                    .ForEach(x => ProductList.Add(x.product));

                category.secondaryCategoryLookup
                    .ToList()
                    .ForEach(x => ProductList.Add(x.websiteInventory.product));
            }

            return this;
        }
    }
}




