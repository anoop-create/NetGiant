using System;
using System.Collections.Generic;
using System.Linq;
using PagedList;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using System.Web.Mvc;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance
{
    public class CategoryCodeViewModel : CommonViewModel
    {
        private ngmdEntities _ctx;

        public CategoryCodeViewModel()
        {
            _ctx = new ngmdEntities();
        }

        public IQueryable<Telerik> CategoryCodeList { get; set; }
        public List<categoryCode> categoryCodesList { get; set; }
        public categoryCode catCode { get; set; }
        public int categoryCodeCount { get; set; }
        public IPagedList<categoryAttribute> CategoryAttributesList { get; set; }
        public IQueryable<SelectListItem> AllWebsites { get; set; }
        public IQueryable<SelectListItem> AllCategories { get; set; }
        public IQueryable<SelectListItem> AllDataSuppliers { get; set; }
        public IQueryable<SelectListItem> AllFilterableAtts { get; set; }
        public IQueryable<SelectListItem> GoogleCategoryList { get; set; }
        public int filterableAttributeFK { get; set; }
        public int categoryCodeID { get; set; }
        public bool IsPartOfSale { get; set; }
        //public IQueryable<TelerikOpenRangeAttribute> OpenRangeAttributeList { get; set; }

        public CategoryCodeViewModel GetCategoryCodes()
        {
            var list = _ctx.categoryCode
                .Include("website")
                .Include("googleProductCategory")
                .Include("categoryCode2")
                .Select(o => new CategoryCodeViewModel.Telerik
                {
                    Id = o.categoryCodeID,
                    AxisGroupNo = o.AXISGroupNo,
                    CategoryName = o.categoryCodeName,
                    ParentCategory = o.categoryCode2.categoryCodeName,
                    Website = o.Website.WebsiteName,
                    GoogleCategory = o.googleProductCategory.googleProductCategoryDescription,
                    IsPrimary = o.isPrimary,
                    InSale = o.isPartOfSale ?? false,
                    InSitemap = o.isInSitemap,
                    HasMeta = (!String.IsNullOrEmpty(o.metaTitle) || !String.IsNullOrEmpty(o.metaDescription))
                })
                .ToList();

            CategoryCodeList = list.AsQueryable();

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

            AllWebsites = SelectListViewModel.GetAllWebsites();
            AllCategories = SelectListViewModel.GetAllCategoryCodes();
            AllDataSuppliers = SelectListViewModel.GetAllDataSuppliers();
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

        //public void GetOpenRangeRemovables(int id)
        //{
        //    OpenRangeAttributeList =
        //        (from a in _ctx.or_removables
        //         where a.CategoryId == id
        //         join s in _ctx.or_searchables on a.NameId equals s.nameID
        //         group a by new { s.name, a.RemovableId } into g
        //         select new TelerikOpenRangeAttribute
        //         {
        //             Id = g.Key.RemovableId,
        //             Name = g.Key.name
        //         });
        //}

        //public SaveReturn GetOpenRangeAttributes(int id)
        //{
        //    var sr = new SaveReturn();

        //    using (ngmdEntities db = new ngmdEntities())
        //    {
        //        try
        //        {
        //            var query = (from s in _ctx.or_searchables
        //                         where !_ctx.or_removables
        //                             .Where(r => r.CategoryId == id)
        //                             .Select(r => r.NameId)
        //                             .Contains(s.nameID)
        //                         group s by new { s.nameID, s.name } into g
        //                         select new
        //                         {
        //                             Id = (int)g.Key.nameID,
        //                             Name = g.Key.name
        //                         }).OrderBy(o => o.Name);

        //            sr.IsSuccess = true;
        //            sr.ReturnData = query;
        //        }
        //        catch (Exception ex)
        //        {
        //            sr.IsSuccess = false;
        //            sr.Message = ex.Message;
        //        }
        //    }

        //    return sr;
        //}

        //public SaveReturn CreateOpenRangeRemovable(int nameId, int categoryId)
        //{
        //    var sr = new SaveReturn();

        //    using(ngmdEntities db = new ngmdEntities())
        //    {
        //        try
        //        {
        //            var attr = db.Set<or_removables>();
        //            attr.Add(new or_removables
        //            {
        //                NameId = nameId,
        //                CategoryId = categoryId
        //            });

        //            db.SaveChanges();
        //            sr.IsSuccess = true;
        //        }
        //        catch(Exception ex)
        //        {
        //            sr.IsSuccess = false;
        //            sr.Message = ex.Message;
        //        }
        //    }

        //    return sr;
        //}

        //public SaveReturn DeleteOpenRangeRemovable(int id)
        //{
        //    var sr = new SaveReturn();

        //    using(ngmdEntities db = new ngmdEntities())
        //    {
        //        try
        //        {
        //            var row = db.or_removables.First(x => x.RemovableId == id);
        //            db.or_removables.Remove(row);
        //            db.SaveChanges();

        //            sr.IsSuccess = true;
        //        }
        //        catch(Exception ex)
        //        {
        //            sr.IsSuccess = false;
        //            sr.Message = ex.Message;
        //        }
        //    }

        //    return sr;
        //}

        public SaveReturn Delete(int id)
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = true;

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
                sr.IsSuccess = false;
                throw new ApplicationException(ex.Message + ex.StackTrace);
            }

            return sr;
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

                CategoryAttributesList = categoryAttrQuery.ToPagedList(pageNumber, pageSize);
            }

            return this;
        }

        public CategoryCodeViewModel CreateCategoryAttributes(int id)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                AllFilterableAtts = SelectListViewModel.GetAllFilterableAtts();
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

        public class Telerik
        {
            public int Id { get; set; }
            public string AxisGroupNo { get; set; }
            public string CategoryName { get; set; }
            public string ParentCategory { get; set; }
            public string Website { get; set; }
            public string GoogleCategory { get; set; }
            public bool IsPrimary { get; set; }
            public bool InSale { get; set; }
            public bool InSitemap { get; set; }
            public bool HasMeta { get; set; }
        }
    }

    //public class TelerikOpenRangeAttribute
    //{
    //    public int Id { get; set; }
    //    public string Name { get; set; }
    //}
}




