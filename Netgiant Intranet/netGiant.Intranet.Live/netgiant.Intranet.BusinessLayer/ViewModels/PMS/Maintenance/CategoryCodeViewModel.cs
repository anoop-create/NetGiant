using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PagedList;
using System.Threading.Tasks;
using netGiant.Intranet.DataLayer;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using System.Web.Mvc;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance
{
    public class CategoryCodeViewModel
    {
        public List<categoryCode> categoryCodesList { get; set; }
        public categoryCode catCode { get; set; }
        public int categoryCodeCount { get; set; }
        public IPagedList<categoryAttribute> categoryAttributesList { get; set; }
        public IQueryable<SelectListItem> AllWebsites { get; set; }
        public List<SelectListItem> AllCategories { get; set; }
        public IQueryable<SelectListItem> AllDataSuppliers { get; set; }
        public IQueryable<SelectListItem> AllFilterableAtts { get; set; }
        public IQueryable<SelectListItem> GoogleCategoryList { get; set; }
        public int filterableAttributeFK { get; set; }
        public int categoryCodeID { get; set; }
        public bool IsPartOfSale { get; set; }

        public CategoryCodeViewModel Get(int? page, string searchTerm, string searchBy,
                                            int? websiteID, int? dataSupplierID, string orderBy)
        {
            int blockSize = 50;
            int blockNumber = (page ?? 1);

            using (ngmdEntities db = new ngmdEntities())
            {
                IQueryable<categoryCode> list = db.categoryCode
                    .Include("website")
                    .Include("googleProductCategory");

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    switch (searchBy)
                    {
                        case "name":
                            list = list.Where(x => x.categoryCodeName.ToLower().Contains(searchTerm.Trim().ToLower()));
                            break;
                        case "parentName":
                            list = list.Where(x => x.categoryCode2.categoryCodeName.ToLower().Contains(searchTerm.Trim().ToLower()));
                            break;
                        case "axisGroupNo":
                            list = list.Where(x => x.AXISGroupNo.ToLower().Contains(searchTerm.Trim().ToLower()));
                            break;
                        default:
                            break;
                    }
                }

                if (websiteID != null && websiteID > 0)
                {
                    list = list.Where(x => x.websiteFK == websiteID);
                }

                switch (orderBy)
                {
                    case "categoryCodeNameAsc":
                        list = list.OrderBy(x => x.categoryCodeName);
                        break;
                    case "categoryCodeNameDesc":
                        list = list.OrderByDescending(x => x.categoryCodeName);
                        break;
                    case "parentCategoryNameAsc":
                        list = list.OrderBy(x => x.categoryCode2.categoryCodeName);
                        break;
                    case "parentCategoryNameDesc":
                        list = list.OrderByDescending(x => x.categoryCode2.categoryCodeName);
                        break;
                    case "websiteAsc":
                        list = list.OrderBy(x => x.Website.WebsiteName);
                        break;
                    case "websiteDesc":
                        list = list.OrderByDescending(x => x.Website.WebsiteName);
                        break;
                    case "axisGroupNoAsc":
                        list = list.OrderBy(x => x.AXISGroupNo);
                        break;
                    case "axisGroupNoDesc":
                        list = list.OrderByDescending(x => x.AXISGroupNo);
                        break;
                    case "googleCategoryAsc":
                        list = list.OrderBy(x => x.googleProductCategory.googleProductCategoryDescription);
                        break;
                    case "googleCategoryDesc":
                        list = list.OrderByDescending(x => x.googleProductCategory.googleProductCategoryDescription);
                        break;
                    case "saleAsc":
                        list = list.OrderBy(x => x.isPartOfSale);
                        break;
                    case "saleDesc":
                        list = list.OrderByDescending(x => x.isPartOfSale);
                        break;
                    case "sitemapAsc":
                        list = list.OrderBy(x => x.isInSitemap);
                        break;
                    case "sitemapDesc":
                        list = list.OrderByDescending(x => x.isInSitemap);
                        break;
                    default:
                        list = list.OrderBy(x => x.categoryCodeName);
                        break;
                }

                categoryCodeCount = list.Count();
                categoryCodesList = list.Skip((blockNumber - 1) * blockSize).Take(blockSize).ToList();
                AllWebsites = SelectListViewModel.AllWebsites();
                AllDataSuppliers = SelectListViewModel.AllDataSuppliers();
                GoogleCategoryList = GetGoogleCategoryNames();
            }

            return this;
        }

        public CategoryCodeViewModel Create(int id)
        {
            using (ngmdEntities db = new ngmdEntities())
            {

                if (id > 0)
                {
                    catCode = db.categoryCode.Find(id);
                    if (catCode != null) IsPartOfSale = catCode.isPartOfSale ?? false;
                }
                else
                {
                    catCode = new categoryCode();
                }
            }

            AllWebsites = SelectListViewModel.AllWebsites();
            AllCategories = SelectListViewModel.AllCategoryCodes().ToList();
            AllDataSuppliers = SelectListViewModel.AllDataSuppliers();
            GoogleCategoryList = GetGoogleCategoryNames();

            return this;
        }

        public bool Save(CategoryCodeViewModel catVm)
        {
            bool success = true;

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    catVm.catCode.dateLastUpdate = DateTime.Now;
                    catVm.catCode.isPartOfSale = IsPartOfSale;

                    if (catVm.catCode.categoryCodeID > 0)
                    {
                        db.Entry(catVm.catCode).State = System.Data.Entity.EntityState.Modified;
                    }
                    else
                    {
                        db.categoryCode.Add(catVm.catCode);
                    }

                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                success = false;
                throw new ApplicationException(ex.Message + ex.StackTrace);
            }

            return success;
        }

        public bool Delete(int id)
        {
            bool success = true;

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    categoryCode cCode = db.categoryCode.Find(id);
                    db.categoryCode.Remove(cCode);
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                success = false;
                throw new ApplicationException(ex.Message + ex.StackTrace);
            }

            return success;
        }

        public CategoryCodeViewModel GetCategoryAttributes(int id, int? page, string orderBy)
        {
            int pageSize = 21;
            int pageNumber = (page ?? 1);

            using (ngmdEntities db = new ngmdEntities())
            {
                IQueryable<categoryAttribute> categoryAttrQuery = db.categoryAttribute.Include("filterableAttribute");

                switch (orderBy)
                {
                    case "attributeNameAsc":
                        categoryAttrQuery = categoryAttrQuery.OrderBy(x => x.filterableAttribute.attributeName);
                        break;
                    case "attributeNameDesc":
                        categoryAttrQuery = categoryAttrQuery.OrderByDescending(x => x.filterableAttribute.attributeName);
                        break;
                    default:
                        categoryAttrQuery = categoryAttrQuery.OrderBy(x => x.filterableAttribute.attributeName);
                        break;
                }

                if (id > 0)
                {
                   categoryAttrQuery = categoryAttrQuery.Where(x => x.categoryCodeFK == id);
                }

                categoryAttributesList = categoryAttrQuery.ToPagedList(pageNumber, pageSize);
            }

            return this;
        }

        public CategoryCodeViewModel CreateCategoryAttributes(int id)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                AllFilterableAtts = SelectListViewModel.AllFilterableAtts();
                List<categoryAttribute> existingCatAttributes = db.categoryAttribute.Where(x => x.categoryCodeFK == id).ToList();

                foreach (categoryAttribute cAtt in existingCatAttributes)
                {
                    AllFilterableAtts = AllFilterableAtts.Where(x => Convert.ToInt32(x.Value) != cAtt.filterableAttributeFK);
                }

                categoryCodeID = id;
            }

            return this;
        }

        public bool SaveCategoryAttribute(CategoryCodeViewModel catVm)
        {
            bool success = true;

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    categoryAttribute ca = new categoryAttribute();
                    ca.categoryCodeFK = catVm.categoryCodeID;
                    ca.filterableAttributeFK = catVm.filterableAttributeFK;

                    db.categoryAttribute.Add(ca);
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                success = false;
                throw new ApplicationException(ex.Message + ex.StackTrace);
            }

            return success;
        }

        public bool DeleteCategoryAttribute(int id)
        {
            bool success = true;

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    categoryAttribute cAtt = db.categoryAttribute.Find(id);
                    db.categoryAttribute.Remove(cAtt);
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                success = false;
                throw new ApplicationException(ex.Message + ex.StackTrace);
            }

            return success;
        }

        public string GetCategoryCodeName(int id)
        {
            string cCodeName = "";

            using (ngmdEntities db = new ngmdEntities())
            {
                cCodeName = db.categoryCode.Where(x => x.categoryCodeID == id).Select(x => x.categoryCodeName).FirstOrDefault();
            }

            return cCodeName;
        }

        private IQueryable<SelectListItem> GetGoogleCategoryNames()
        {
            IQueryable<SelectListItem> query;

            using (ngmdEntities db = new ngmdEntities())
            {
                query = db.googleProductCategory.OrderBy(x => x.googleProductCategoryDescription).Select(x => new SelectListItem
                {
                    Value = x.googleProductCategoryID.ToString(),
                    Text = x.googleProductCategoryDescription.ToString()
                }).ToList().AsQueryable();
            }
            return query;
        }

    }
}




