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
    public class ObsoleteItemsViewModel
    {
        public List<obsoleteItem> ObsoleteItemsList { get; set; }
        public int ObsoleteItemsListCount { get; set; }
        public IQueryable<SelectListItem> WebsiteNameList { get; set; }
        public obsoleteItem ObsoleteItem { get; set; }

        public ObsoleteItemsViewModel GetObsoleteItems()
        {
            return GetObsoleteItems(null, null, null, null, 1);
        }

        public ObsoleteItemsViewModel GetObsoleteItems(string orderBy, string searchTerm, string searchBy, int? websiteID, int? block)
        {
            int blockSize = 50;
            int blockNumber = (block ?? 1);

            using (ngmdEntities db = new ngmdEntities())
            {
                IQueryable<obsoleteItem> query = db.obsoleteItem
                    .Include(x => x.Website);

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    switch (searchBy)
                    {
                        case "stockref":
                            query = query.Where(x => x.stockReference.ToString().Contains(searchTerm.Trim()));
                            break;
                        case "equip":
                            query = query.Where(x => x.equipmentName.ToString().Contains(searchTerm.Trim().ToLower()));
                            break;
                        case "url":
                            query = query.Where(x => x.URL.ToLower().Contains(searchTerm.Trim().ToLower()));
                            break;
                        default:
                            break;
                    }
                }

                if (websiteID != null && websiteID > 0)
                {
                    query = query.Where(x => x.websiteFK == websiteID);
                }

                switch (orderBy)
                {
                    case "stockrefAsc":
                        query = query.OrderBy(x => x.stockReference);
                        break;
                    case "stockrefDesc":
                        query = query.OrderByDescending(x => x.stockReference);
                        break;
                    case "equipAsc":
                        query = query.OrderBy(x => x.equipmentName);
                        break;
                    case "equipDesc":
                        query = query.OrderByDescending(x => x.equipmentName);
                        break;
                    case "websiteAsc":
                        query = query.OrderBy(x => x.Website.WebsiteName);
                        break;
                    case "websiteDesc":
                        query = query.OrderByDescending(x => x.Website.WebsiteName);
                        break;
                    case "urlAsc":
                        query = query.OrderBy(x => x.URL);
                        break;
                    case "urlDesc":
                        query = query.OrderByDescending(x => x.URL);
                        break;
                    default:
                        query = query.OrderBy(x => x.Website.WebsiteName)
                            .ThenBy(x => x.stockReference)
                            .ThenBy(x => x.equipmentName);
                        break;
                }

                ObsoleteItemsListCount = query.Count();
                ObsoleteItemsList = query
                    .Skip((blockNumber - 1) * blockSize)
                    .Take(blockSize)
                    .ToList();

                SetupSelectLists();
            }
            return this;
        }

        public void SetupSelectLists()
        {
            WebsiteNameList = GetWebsiteNames();
        }

        private IQueryable<SelectListItem> GetWebsiteNames()
        {
            IQueryable<SelectListItem> query;

            using (ngmdEntities db = new ngmdEntities())
            {
                query = db.Websites.OrderBy(x => x.WebsiteName).Select(x => new SelectListItem
                {
                    Value = x.WebsiteID.ToString(),
                    Text = x.WebsiteName.ToString()
                }).ToList().AsQueryable();
            }
            return query;
        }

        public ObsoleteItemsViewModel Create(int id)
        {
            if (id > 0)
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    ObsoleteItem = db.obsoleteItem.Where(x => x.obsoleteItemID == id).FirstOrDefault();
                }
            }
            else
            {
                ObsoleteItem = new obsoleteItem();
            }
            SetupSelectLists();

            return this;
        }

        public void Save()
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    if (ObsoleteItem.obsoleteItemID > 0)
                    {
                        db.Entry(ObsoleteItem).State = EntityState.Modified;
                    }
                    else
                    {
                        //Check if a Seo Text already exists for specified criteria
                        CheckObsoleteItemExists(db);
                        db.Entry(ObsoleteItem).State = EntityState.Added;
                    }

                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException(ex.Message + ex.StackTrace);
            }
        }

        private void CheckObsoleteItemExists(ngmdEntities db)
        {
            obsoleteItem pst = new obsoleteItem();

            pst = db.obsoleteItem.Where(x => x.websiteFK == ObsoleteItem.websiteFK &&
                x.stockReference == ObsoleteItem.stockReference &&
                x.equipmentName == ObsoleteItem.equipmentName).FirstOrDefault();

            if (pst != null)
                throw new Exception("Obsolete Item already exists.");
        }

        public void Delete(int id)
        {
            try
            {
                if (id > 0)
                {
                    using (ngmdEntities db = new ngmdEntities())
                    {
                        obsoleteItem en = db.obsoleteItem.Where(x => x.obsoleteItemID == id).FirstOrDefault();
                        db.Entry(en).State = EntityState.Deleted;
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
