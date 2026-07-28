using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using netGiant.Intranet.DataLayer;
using PagedList;
using System.Data.Entity;
using System.Web.Mvc;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance
{
    public class WebsiteViewModel
    {
        public Website web { get; set; }
        public PagedList.IPagedList<Website> websites { get; set; }

        public WebsiteViewModel Get()
        {
            return Get(null, "", "", "");
        }

        public WebsiteViewModel Get(int? page, string searchTerm, string searchBy, string orderBy)
        {
            int pageSize = 24;
            int pageNumber = (page ?? 1);

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    IQueryable<Website> list = db.Websites;

                    if (!string.IsNullOrEmpty(searchTerm))
                    {
                        searchTerm = searchTerm.ToLower().Trim();
                        
                        switch (searchBy)
                        {
                            case "name":
                                list = list.Where(x => x.WebsiteName.ToLower().Contains(searchTerm));
                                break;
                            case "url":
                                list = list.Where(x => x.WebURL.ToLower().Contains(searchTerm));
                                break;
                            case "friendlyName":
                                list = list.Where(x => x.FriendlyName.ToLower().Contains(searchTerm));
                                break;
                            default:
                                break;
                        }
                    }

                    //Sorting
                    switch (orderBy)
                    {
                        case "websiteNameAsc":
                            list = list.OrderBy(x => x.WebsiteName);
                            break;
                        case "websiteNameDesc":
                            list = list.OrderByDescending(x => x.WebsiteName);
                            break;
                        case "websiteURLAsc":
                            list = list.OrderBy(x => x.WebURL);
                            break;
                        case "websiteURLDesc":
                            list = list.OrderByDescending(x => x.WebURL);
                            break;
                        case "websiteFriendlyNameAsc":
                            list = list.OrderBy(x => x.FriendlyName);
                            break;
                        case "websiteFriendlyNameDesc":
                            list = list.OrderByDescending(x => x.FriendlyName);
                            break;
                        case "dateLastUpdatedAsc":
                            list = list.OrderBy(x => x.dateLastUpdate);
                            break;
                        case "dateLastUpdatedDesc":
                            list = list.OrderByDescending(x => x.dateLastUpdate);
                            break;
                        default:
                            list = list.OrderBy(x => x.WebsiteID);
                            break;
                    }

                    websites = list.ToPagedList(pageNumber, pageSize);
                }
            }

            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }

            return this;
        }

        public static WebsiteViewModel Create(int id)
        {
            WebsiteViewModel model = new WebsiteViewModel();

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    if (id > 0)
                    {
                        model.web = db.Websites.Find(id);
                    }
                    else
                    {
                        model.web = new Website();
                    }
                }
            }

            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }

            return model;
        }

        public void Save()
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    web.dateLastUpdate = DateTime.Now;

                    if (web.WebsiteID > 0)
                    {
                        db.Entry(web).State = EntityState.Modified;
                    }
                    else
                    {
                        db.Websites.Add(web);
                    }

                    db.SaveChanges();
                }
            }

            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }
        }

        public void Delete(int id)
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    Website w = db.Websites.Find(id);
                    db.Websites.Remove(w);
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }
        }
    }
}
