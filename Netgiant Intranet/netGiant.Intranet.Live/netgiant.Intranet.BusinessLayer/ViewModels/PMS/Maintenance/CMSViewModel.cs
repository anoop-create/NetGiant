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
    public class CMSViewModel
    {
        public cmsSection CmsSection { get; set; }
        public List<cmsSection> CmsSectionList { get; set; }
        public int CmsSectionListCount { get; set; }
        public cmsEntry CmsEntry { get; set; }
        public List<cmsEntry> CmsEntryList { get; set; }
        public int CmsEntryListCount { get; set; }
        public IQueryable<SelectListItem> WebsiteNameList { get; set; }
        public List<SelectListItem> SectionNameList { get; set; }
        public List<SelectListItem> EntryNameList { get; set; }

        public CMSViewModel GetCmsSection()
        {
            return GetCmsSection(null, null, null, null, 1);
        }

        public CMSViewModel GetCmsSection(string orderBy, string searchTerm, string searchBy, int? websiteID, int? block)
        {
            int blockSize = 50;
            int blockNumber = (block ?? 1);

            using (ngmdEntities db = new ngmdEntities())
            {
                IQueryable<cmsSection> query = db.cmsSection
                    .Include(x => x.Website);

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    switch (searchBy)
                    {
                        case "sectionname":
                            query = query.Where(x => x.sectionName.ToString().Contains(searchTerm.Trim()));
                            break;
                        //case "equip":
                        //    query = query.Where(x => x.equipmentName.ToString().Contains(searchTerm.Trim().ToLower()));
                        //    break;
                        //case "url":
                        //    query = query.Where(x => x.URL.ToLower().Contains(searchTerm.Trim().ToLower()));
                        //    break;
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
                    case "sectionnameAsc":
                        query = query.OrderBy(x => x.sectionName);
                        break;
                    case "sectionnameDesc":
                        query = query.OrderByDescending(x => x.sectionName);
                        break;
                    case "websiteAsc":
                        query = query.OrderBy(x => x.Website.WebsiteName);
                        break;
                    case "websiteDesc":
                        query = query.OrderByDescending(x => x.Website.WebsiteName);
                        break;
                    default:
                        query = query.OrderBy(x => x.Website.WebsiteName)
                            .ThenBy(x => x.sectionName);
                        break;
                }

                CmsSectionListCount = query.Count();
                CmsSectionList = query
                    .Skip((blockNumber - 1) * blockSize)
                    .Take(blockSize)
                    .ToList();

                SetupSelectLists();
            }
            return this;
        }

        public CMSViewModel GetCmsEntries()
        {
            return GetCmsEntries(null, null, null, null, 1);
        }

        public CMSViewModel GetCmsEntries(string orderBy, string searchTerm, string searchBy, int? websiteId, int? block)
        {
            int blockSize = 50;
            int blockNumber = (block ?? 1);

            using (ngmdEntities db = new ngmdEntities())
            {
                IQueryable<cmsEntry> query = db.cmsEntry
                    .Include(x => x.cmsSection)
                    .Include(x => x.cmsSection.Website);

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    switch (searchBy)
                    {
                        case "entryname":
                            query = query.Where(x => x.entryName.ToString().Contains(searchTerm.Trim()));
                            break;
                        case "sectionname":
                            query = query.Where(x => x.cmsSection.sectionName.ToString().Contains(searchTerm.Trim()));
                            break;
                        default:
                            break;
                    }
                }

                if (websiteId != null && websiteId > 0)
                {
                    query = query.Where(x => x.cmsSection.websiteFK == websiteId);
                }

                switch (orderBy)
                {
                    case "sectionnameAsc":
                        query = query.OrderBy(x => x.cmsSection.sectionName);
                        break;
                    case "sectionnameDesc":
                        query = query.OrderByDescending(x => x.cmsSection.sectionName);
                        break;
                    case "entrynameAsc":
                        query = query.OrderBy(x => x.entryName);
                        break;
                    case "entrynameDesc":
                        query = query.OrderByDescending(x => x.entryName);
                        break;
                    case "websiteAsc":
                        query = query.OrderBy(x => x.cmsSection.Website.WebsiteName);
                        break;
                    case "websiteDesc":
                        query = query.OrderByDescending(x => x.cmsSection.Website.WebsiteName);
                        break;
                    default:
                        query = query.OrderBy(x => x.cmsSection.Website.WebsiteName)
                            .ThenBy(x => x.cmsSection.sectionName)
                            .ThenBy(x => x.entryName);
                        break;
                }

                CmsEntryListCount = query.Count();
                CmsEntryList = query
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
            SectionNameList = GetSectionNames();
            EntryNameList = GetEntryNames(CmsEntry != null ? CmsEntry.cmsSectionFK : 0);
        }
        
        public CMSViewModel CreateSection(int id)
        {
            if (id > 0)
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    CmsSection = db.cmsSection.Where(x => x.cmsSectionID == id).FirstOrDefault();
                }
            }
            else
            {
                CmsSection = new cmsSection();
            }
            SetupSelectLists();

            return this;
        }

        public bool SaveSection()
        {
            bool success = true;
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    if (CmsSection.cmsSectionID > 0)
                    {
                        db.Entry(CmsSection).State = EntityState.Modified;
                    }
                    else
                    {
                        //Check if a Seo Text already exists for specified criteria
                        CheckCmsSectionExists(db);
                        db.Entry(CmsSection).State = EntityState.Added;
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

        public void DeleteSection(int id)
        {
            try
            {
                if (id > 0)
                {
                    using (ngmdEntities db = new ngmdEntities())
                    {
                        cmsSection s = db.cmsSection.Where(x => x.cmsSectionID == id).FirstOrDefault();
                        db.Entry(s).State = EntityState.Deleted;
                        db.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException(ex.Message + ex.StackTrace);
            }
        }

        public CMSViewModel CreateEntry(int id)
        {
            if (id > 0)
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    CmsEntry = db.cmsEntry
                        .Include(x => x.cmsSection)
                        .Where(x => x.cmsEntryID == id).FirstOrDefault();
                }
            }
            else
            {
                CmsEntry = new cmsEntry();
            }
            SetupSelectLists();

            return this;
        }

        public bool SaveEntry()
        {
            bool success = true;
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    if (CmsEntry.cmsEntryID > 0)
                    {
                        db.Entry(CmsEntry).State = EntityState.Modified;
                    }
                    else
                    {
                        //Check if a Seo Text already exists for specified criteria
                        CheckCmsEntryExists(db);
                        db.Entry(CmsEntry).State = EntityState.Added;
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

        public void DeleteEntry(int id)
        {
            try
            {
                if (id > 0)
                {
                    using (ngmdEntities db = new ngmdEntities())
                    {
                        cmsEntry e = db.cmsEntry.Where(x => x.cmsEntryID == id).FirstOrDefault();
                        db.Entry(e).State = EntityState.Deleted;
                        db.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException(ex.Message + ex.StackTrace);
            }
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

        public List<SelectListItem> GetSectionNames(int? websiteId = null)
        {
            List<SelectListItem> oList;

            using (ngmdEntities db = new ngmdEntities())
            {
                if (websiteId == null)
                {
                    oList = db.cmsSection
                    .OrderBy(x => x.sectionName).Select(x => new SelectListItem
                    {
                        Value = x.cmsSectionID.ToString(),
                        Text = x.sectionName.ToString()
                    }).ToList();
                }
                else
                {
                    oList = db.cmsSection
                    .Where(x => x.websiteFK == websiteId)
                    .OrderBy(x => x.sectionName).Select(x => new SelectListItem
                    {
                        Value = x.cmsSectionID.ToString(),
                        Text = x.sectionName.ToString()
                    }).ToList();
                }
            }
            return oList;
        }

        public List<SelectListItem> GetEntryNames(int? sectionId = null)
        {
            List<SelectListItem> oList = new List<SelectListItem>();

            using (ngmdEntities db = new ngmdEntities())
            {
                if (sectionId != null)
                {
                    oList = db.cmsEntry
                        .Where(x => x.cmsSectionFK == sectionId)
                        .OrderBy(x => x.entryName).Select(x => new SelectListItem
                        {
                            Value = x.cmsEntryID.ToString(),
                            Text = x.entryName.ToString()
                        }).ToList();
                }
            }
            return oList;
        }

        private void CheckCmsSectionExists(ngmdEntities db)
        {
            cmsSection pst = new cmsSection();

            pst = db.cmsSection.Where(x => x.websiteFK == CmsSection.websiteFK &&
                x.sectionName == CmsSection.sectionName).FirstOrDefault();

            if (pst != null)
                throw new Exception("CMS Section already exists.");
        }

        private void CheckCmsEntryExists(ngmdEntities db)
        {
            cmsEntry pst = new cmsEntry();

            pst = db.cmsEntry.Where(x => x.cmsSectionFK == CmsEntry.cmsSectionFK &&
                x.entryName == CmsEntry.entryName).FirstOrDefault();

            if (pst != null)
                throw new Exception("CMS Entry already exists.");
        }
    }
}
