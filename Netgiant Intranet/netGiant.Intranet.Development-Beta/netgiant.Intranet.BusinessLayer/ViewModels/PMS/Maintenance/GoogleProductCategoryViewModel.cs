using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance
{
    public class GoogleProductCategoryViewModel : CommonViewModel
    {
        private ngmdEntities _ctx;

        public GoogleProductCategoryViewModel()
        {
            _ctx = new ngmdEntities();
        }

        public IQueryable<TelerikGoogleProductCategory> GoogleProductCategoryList { get; set; }
        public googleProductCategory GoogleProductCategory { get; set; }

        public GoogleProductCategoryViewModel Get()
        {
            GoogleProductCategoryList = _ctx.googleProductCategory
                                            .Select(x => new TelerikGoogleProductCategory
                                            {
                                                Id = x.googleProductCategoryID,
                                                ProductCode = x.googleProductCategoryNo,
                                                Description = x.googleProductCategoryDescription
                                            })
                                            .AsQueryable();
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

        public SaveReturn Delete(int id)
        {
            var sr = new SaveReturn();
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
                    sr.IsSuccess = true;
                }
            }
            catch (Exception ex)
            {
                sr.IsSuccess = false;
                sr.Message = ex.Message;
            }
            return sr;
        }
    }

    public class TelerikGoogleProductCategory
    {
        public int Id { get; set; }
        public int ProductCode { get; set; }
        public string Description { get; set; }
    }
}
