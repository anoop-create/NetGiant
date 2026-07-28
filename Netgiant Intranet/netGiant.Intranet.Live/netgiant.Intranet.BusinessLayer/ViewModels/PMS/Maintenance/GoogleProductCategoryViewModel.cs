using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using netGiant.Intranet.DataLayer;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance
{
    public class GoogleProductCategoryViewModel
    {
        public List<googleProductCategory> GoogleProductCategoryList { get; set; }
        public int GoogleProductCategoryListCount { get; set; }
        public googleProductCategory GoogleProductCategory { get; set; }

        public GoogleProductCategoryViewModel GetGoogleProductCategory()
        {
            return GetGoogleProductCategory(null, null, null, 1);
        }

        public GoogleProductCategoryViewModel GetGoogleProductCategory(string orderBy, string searchTerm, string searchBy, int? block)
        {
            int blockSize = 50;
            int blockNumber = (block ?? 1);

            using (ngmdEntities db = new ngmdEntities())
            {
                IQueryable<googleProductCategory> query = db.googleProductCategory;

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    switch (searchBy)
                    {
                        case "number":
                            query = query.Where(x => x.googleProductCategoryNo.ToString().Contains(searchTerm.Trim()));
                            break;
                        case "description":
                            query = query.Where(x => x.googleProductCategoryDescription.ToString().Contains(searchTerm.Trim().ToLower()));
                            break;
                        default:
                            break;
                    }
                }

                switch (orderBy)
                {
                    case "noAsc":
                        query = query.OrderBy(x => x.googleProductCategoryNo);
                        break;
                    case "noDesc":
                        query = query.OrderByDescending(x => x.googleProductCategoryNo);
                        break;
                    case "descriptionAsc":
                        query = query.OrderBy(x => x.googleProductCategoryDescription);
                        break;
                    case "descriptionDesc":
                        query = query.OrderByDescending(x => x.googleProductCategoryDescription);
                        break;
                    default:
                        query = query.OrderBy(x => x.googleProductCategoryID);
                        break;
                }

                GoogleProductCategoryListCount = query.Count();
                GoogleProductCategoryList = query
                    .Skip((blockNumber - 1) * blockSize)
                    .Take(blockSize)
                    .ToList();
            }
            return this;
        }

        public GoogleProductCategoryViewModel Create(int id)
        {
            if (id > 0)
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    GoogleProductCategory = db.googleProductCategory.Where(x => x.googleProductCategoryID == id).FirstOrDefault();
                }
            }
            else
            {
                GoogleProductCategory = new googleProductCategory();
            }

            return this;
        }

        public void Save()
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    if (GoogleProductCategory.googleProductCategoryID > 0)
                    {
                        db.Entry(GoogleProductCategory).State = EntityState.Modified;
                    }
                    else
                    {
                        db.Entry(GoogleProductCategory).State = EntityState.Added;
                    }

                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException(ex.Message + ex.StackTrace);
            }
        }

        public void Delete(int id)
        {
            try
            {
                if (id > 0)
                {
                    using (ngmdEntities db = new ngmdEntities())
                    {
                        googleProductCategory gpc = db.googleProductCategory.Where(x => x.googleProductCategoryID == id).FirstOrDefault();
                        db.Entry(gpc).State = EntityState.Deleted;
                        db.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException(ex.Message + ex.StackTrace);
            }
        }
    }
}
