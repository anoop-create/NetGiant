using System;
using System.Linq;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using PagedList;
using System.Data.Entity;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance
{
    public class WebsiteViewModel : CommonViewModel
    {
        private ngmdEntities _ctx;

        public WebsiteViewModel()
        {
            _ctx = new ngmdEntities();
        }

        public IQueryable<TelerikWebsite> WebsiteList { get; set; }
        public Website websiteSingle { get; set; }

        public WebsiteViewModel Get()
        {
            WebsiteList = _ctx.Website
                                   .Select(x => new TelerikWebsite
                                   {
                                       Id = x.WebsiteID,
                                       Name = x.WebsiteName,
                                       FriendlyName = x.FriendlyName,
                                       Abbreviation = x.Abbreviation,
                                       URL = x.WebURL,
                                       DateLastUpdated = x.dateLastUpdate
                                   })
                                   .AsQueryable();
            return this;
        }

        public WebsiteViewModel Create(int id)
        {
            using (ngmdEntities db = new ngmdEntities())
            {

                if (id > 0)
                {
                    websiteSingle = db.Website.Find(id);
                }
                else
                {
                    websiteSingle = new Website();
                }
            }

            return this;
        }

        public bool Save(WebsiteViewModel wVm)
        {
            bool success = true;

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    wVm.websiteSingle.dateLastUpdate = DateTime.Now;

                    if (wVm.websiteSingle.WebsiteID > 0)
                    {
                        db.Entry(wVm.websiteSingle).State = System.Data.Entity.EntityState.Modified;
                    }
                    else
                    {
                        db.Website.Add(wVm.websiteSingle);
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

        public SaveReturn Delete(int id)
        {
            var sr = new SaveReturn();

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    Website w = db.Website.Find(id);
                    db.Website.Remove(w);
                    db.SaveChanges();
                    sr.IsSuccess = true;
                }
            }
            catch (Exception ex)
            {
                sr.IsSuccess = false;
                throw new ApplicationException(ex.Message + ex.StackTrace);
            }

            return sr;
        }
    }

    public class TelerikWebsite
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string FriendlyName { get; set; }
        public string URL { get; set; }
        public string Abbreviation { get; set; }
        public DateTime DateLastUpdated { get; set; }
    }
}
