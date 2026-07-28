using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Provider;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Web.Mvc;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance
{
    public class CMSViewModel : CommonViewModel
    {
        private ngmdEntities _ctx;

        public CMSViewModel()
        {
            _ctx = new ngmdEntities();
        }

        public cmsSection CMSSection { get; set; }
        public cmsEntry CMSEntry { get; set; }
        public Event Event { get; set; }
        public EventMapping EventMapping { get; set; }
        public List<SelectListItem> SectionNameList { get; set; }
        public List<SelectListItem> GroupNameList { get; set; }
        public List<SelectListItem> EntryNameList { get; set; }
        public List<SelectListItem> EventNameList { get; set; }
        public List<SelectListItem> EventGroupList { get; set; }
        public List<SelectListItem> EventCmsEntryList { get; set; }
        public IQueryable<SelectListItem> WebsiteNameList { get; set; }
        public IQueryable<TelerikSection> SectionList { get; set; }
        public IQueryable<TelerikEntry> EntryList { get; set; }
        public IQueryable<TelerikEventData> EventDataList { get; set; }
        public List<TelerikEventMapping> EventMappingList { get; set; }
        public List<TelerikEvent> EventList { get; set; }
        public string CmsEntryName { get; set; }
        #region CMS Entries
        public void GetEntryList()
        {
            EntryList = _ctx.cmsEntry
                .Select(x => new TelerikEntry
                {
                    Id = x.cmsEntryID,
                    Website = x.cmsSection.Website.FriendlyName,
                    Section = x.cmsSection.sectionName,
                    Group = (_ctx.Lookup
                    .Where(y => y.LookupType.LookupTypeName == "CMS Group" && y.LookupId == x.cmsGroupFK)
                    .AsQueryable()
                    .FirstOrDefault()
                    .LookupName),
                    Name = x.entryName,
                    Content = x.cmsContent.Length < 30 ? x.cmsContent : x.cmsContent.Substring(0, 30) + "...",
                    Notes = x.notes
                })
            .AsQueryable();
        }

        public CMSViewModel CreateEntry(int id)
        {
            if (id > 0)
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    CMSEntry = db.cmsEntry
                        .Include(x => x.cmsSection.Website)
                        .Where(x => x.cmsEntryID == id).FirstOrDefault();
                }
            }
            else
            {
                CMSEntry = new cmsEntry();
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
                    if (CMSEntry.cmsEntryID > 0)
                    {
                        db.Entry(CMSEntry).State = EntityState.Modified;
                    }
                    else
                    {
                        //Check if a Seo Text already exists for specified criteria
                        CheckCMSEntryExists(db);
                        db.Entry(CMSEntry).State = EntityState.Added;
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

        private void CheckCMSEntryExists(ngmdEntities db)
        {
            cmsEntry pst = new cmsEntry();

            pst = db.cmsEntry.Where(x => x.cmsSectionFK == CMSEntry.cmsSectionFK &&
                x.entryName == CMSEntry.entryName).FirstOrDefault();

            if (pst != null)
                throw new Exception("CMS Entry already exists.");
        }

        public SaveReturn DeleteEntry(int id)
        {
            SaveReturn sr = new SaveReturn();

            try
            {
                if (id > 0)
                {
                    using (ngmdEntities db = new ngmdEntities())
                    {
                        cmsEntry e = db.cmsEntry.Where(x => x.cmsEntryID == id).FirstOrDefault();
                        db.Entry(e).State = EntityState.Deleted;
                        db.SaveChanges();
                        sr.IsSuccess = true;
                    }
                }
            }
            catch (Exception ex)
            {
                sr.IsSuccess = false;
                sr.Message = ex.Message;
            }

            return sr;
        }        
        #endregion
        
        #region CMS Sections
        public void GetSectionList()
        {
            SectionList = _ctx.cmsSection.Select(x => new TelerikSection
            {
                Id = x.cmsSectionID,
                Website = x.Website.FriendlyName,
                Name = x.sectionName
            })
            .AsQueryable();
        }

        public CMSViewModel CreateSection(int id)
        {
            if (id > 0)
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    CMSSection = db.cmsSection.Where(x => x.cmsSectionID == id).FirstOrDefault();
                }
            }
            else
            {
                CMSSection = new cmsSection();
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
                    if (CMSSection.cmsSectionID > 0)
                    {
                        db.Entry(CMSSection).State = EntityState.Modified;
                    }
                    else
                    {
                        //Check if a Seo Text already exists for specified criteria
                        CheckCMSSectionExists(db);
                        db.Entry(CMSSection).State = EntityState.Added;
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

        private void CheckCMSSectionExists(ngmdEntities db)
        {
            cmsSection pst = new cmsSection();

            pst = db.cmsSection.Where(x => x.websiteFK == CMSSection.websiteFK &&
                x.sectionName == CMSSection.sectionName).FirstOrDefault();

            if (pst != null)
                throw new Exception("CMS Section already exists.");
        }

        public SaveReturn DeleteSection(int id)
        {
            SaveReturn sr = new SaveReturn();
            try
            {
                if (id > 0)
                {
                    using (ngmdEntities db = new ngmdEntities())
                    {
                        cmsSection s = db.cmsSection.Where(x => x.cmsSectionID == id).FirstOrDefault();
                        db.Entry(s).State = EntityState.Deleted;
                        db.SaveChanges();
                        sr.IsSuccess = true;
                    }
                }
            }
            catch (Exception ex)
            {
                sr.IsSuccess = false;
                sr.Message = ex.Message;
            }

            return sr;
        }
        #endregion
        
        #region CMS Events
        public CMSViewModel GetEventData()
        {
            EventDataList = _ctx.EventMapping
                .Include("Event.Website")
                .Include("cmsEntry1")
                .Include("cmsEntry2")
                .Where(x => true)
                .Select(x => new TelerikEventData
                {
                    Id = x.EventMappingId,
                    Website = x.Event.Website.FriendlyName,
                    Event = x.Event.EventName + "|" + x.Event.EventId.ToString(),
                    CmsEntryId = x.cmsEntry.cmsEntryID,
                    DefaultCmsSection = x.cmsEntry.cmsSection.sectionName,
                    DefaultCmsEntry = x.cmsEntry.entryName,
                    MappedCmsEntryId = x.cmsEntry1.cmsEntryID,
                    MappedCmsEntry = x.cmsEntry1.entryName,
                    DateActivate = x.Event.DateActive,
                    DateInactive = x.Event.DateInactive,
                    IsActive = x.Event.IsActive.ToString()
                })
                .AsQueryable();

            return this;
        }        

        public CMSViewModel GetEvents()
        {
            EventList = _ctx.Event
                .Select(x => new TelerikEvent
                {
                    Id = x.EventId,
                    Website = x.Website.FriendlyName,
                    Event = x.EventName,
                    DateActivate = x.DateActive,
                    DateInactive = x.DateInactive,
                    IsActive = x.IsActive.ToString()
                })
            .ToList();

            return this;
        }

        public CMSViewModel CreateEvent(int id)
        {
            if (id > 0)
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    Event = db.Event
                        .Include(x => x.Website)
                        .Where(x => x.EventId == id).FirstOrDefault();
                }
            }
            else
            {
                Event = new Event()
                {
                    DateActive = DateTime.Now.Date,
                    DateInactive = DateTime.Now.Date
                };
            }
            WebsiteNameList = GetWebsiteNames();

            return this;
        }

        public SaveReturn SaveEvent()
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = true;
            try
            {
                
                using (ngmdEntities db = new ngmdEntities())
                {
                    // Validation:
                    // End Date is after the Start Date
                    // For all Mappings within this Event check if there is > 1 Mapping for the CMSEntry
                    // For these Mappings check if the Dates for the Events associated with them overlap
                    if (Event.DateActive >= Event.DateInactive)
                    {
                        sr.Message += "<div class=\"g-m-b-5\">End Date is not after the Start Date!</div>";
                        sr.IsSuccess = false;
                        return sr;
                    }
                    List<cmsEntry> lCe = db.EventMapping
                        .Include(x => x.cmsEntry)
                        .Where(x => x.EventFk == Event.EventId && x.cmsEntry.EventMapping.Count > 1)
                        .Select(x => x.cmsEntry)
                        .OrderBy(x => x.cmsEntryID)
                        .ToList();

                    foreach (cmsEntry ce in lCe)
                    {
                        List<Event> lE = new List<Event>();
                        foreach (EventMapping em in ce.EventMapping)
                        {
                            // Don't add the Event that is being saved
                            if (em.EventFk != Event.EventId)
                            {
                                lE.Add(em.Event);
                            }
                        }
                        foreach (Event e in lE)
                        {
                            if ((Event.DateActive >= e.DateActive && Event.DateActive <= e.DateInactive)
                                || (Event.DateInactive <= e.DateInactive && Event.DateInactive >= e.DateActive))
                            {
                                // Problem
                                sr.Message += "<div class=\"g-m-b-5\">Date conflict with Event <span class=\"g-fc-o\">'" + e.EventName + "'</span><br />- CMS Entry <span class=\"g-fc-o\">'" + ce.entryName + "'</span>.</div>";
                                sr.IsSuccess = false;
                            }
                        }
                    }
                }
                if (!sr.IsSuccess)
                {
                    sr.Message = "<div class=\"g-m-b-5\">Save was unsuccessful.</div>" + sr.Message;
                    return sr;
                }
                using (ngmdEntities db = new ngmdEntities())
                {                 

                    if (Event.EventId > 0)
                    {
                        db.Entry(Event).State = EntityState.Modified;
                    }
                    else
                    {
                        db.Entry(Event).State = EntityState.Added;
                    }

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

        public SaveReturn DeleteEvent(int id)
        {
            SaveReturn sr = new SaveReturn();

            try
            {
                if (id > 0)
                {
                    using (ngmdEntities db = new ngmdEntities())
                    {
                        // Check for Event Mappings
                        List<EventMapping> lem = db.EventMapping.Where(x => x.EventFk == id).ToList();
                        foreach (EventMapping em in lem)
                        {
                            db.Entry(em).State = EntityState.Deleted;
                        }

                        // Now delete the Event
                        Event e = db.Event.Where(x => x.EventId == id).FirstOrDefault();
                        db.Entry(e).State = EntityState.Deleted;

                        db.SaveChanges();
                        sr.IsSuccess = true;
                    }
                }
            }
            catch (Exception ex)
            {
                sr.IsSuccess = false;
                sr.Message = ex.Message;
            }

            return sr;
        }
        #endregion

        #region CMS Event Mappings
        public CMSViewModel GetEventMappings(int id = 0)
        {
            if (id > 0)
            {
                EventMappingList = _ctx.EventMapping
                    .Where(x => x.DefaultCmsEntryFk == id)
                    .Select(x => new TelerikEventMapping
                    {
                        Id = x.EventMappingId,
                        CmsEntryId = id,
                        DefaultCmsEntry = x.cmsEntry.entryName,
                        MappedCmsEntryId = x.cmsEntry1.cmsEntryID,
                        MappedCmsEntry = x.cmsEntry1.entryName,
                        Event = x.Event.EventName,
                        DateActivate = x.Event.DateActive,
                        DateInactive = x.Event.DateInactive,
                        IsActive = x.Event.IsActive.ToString()
                    })
                .ToList();
            }
            else
            {
                EventMappingList = new List<TelerikEventMapping>();
            }

            return this;
        }
        public CMSViewModel CreateEventMapping(int id, int cmsEntryId)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                CMSEntry = db.cmsEntry
                    .Include(x => x.cmsSection.Website)
                    .Where(x => x.cmsEntryID == cmsEntryId).FirstOrDefault();
            }
            if (id > 0)
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    EventMapping = db.EventMapping
                        .Include(x => x.Event.Website)
                        .Include(x => x.cmsEntry)
                        .Include(x => x.cmsEntry1)
                        .Where(x => x.EventMappingId == id).FirstOrDefault();
                }
            }
            else
            {
                EventMapping = new EventMapping()
                {
                    DefaultCmsEntryFk = cmsEntryId
                };
            }
            EventNameList = GetEventNames(CMSEntry.cmsSection.websiteFK);
            EventCmsEntryList = GetEntryNames(null, "EventData", CMSEntry.cmsSection.websiteFK);
            WebsiteNameList = GetWebsiteNames();

            return this;
        }

        public SaveReturn SaveEventMapping()
        {
            SaveReturn sr = new SaveReturn();
            sr.IsSuccess = true;
            try
            {
                // Validation:
                // Has this CMS Entry already been mapped for this Event
                // Check if there is > 1 Mapping for this CMSEntry
                // For these Mappings check if the Dates for the Events associated with them overlap
                if (EventMapping.EventMappingId == 0)
                {
                    using (ngmdEntities db = new ngmdEntities())
                    {
                        // Get the CMS Entry (and it's Event Mappings) for the Default CMS Entry (provided there are some Event Mappings)
                        cmsEntry entry = db.cmsEntry
                            .Include(x => x.EventMapping)
                            .Where(x => x.cmsEntryID == EventMapping.DefaultCmsEntryFk && x.EventMapping.Count > 0)
                            .FirstOrDefault();
                        // Get the Event for this Event Mapping
                        Event ev = db.Event
                            .Where(x => x.EventId == EventMapping.EventFk)
                            .FirstOrDefault();

                        // Provided we found a CMS Entry, check if there are any Event Mappings that have the same Event as the one being created
                        if (entry != null)
                        {
                            if (entry.EventMapping.Where(x => x.EventFk == EventMapping.EventFk).Count() > 0)
                            {
                                sr.Message += "<div class=\"g-m-b-5\">There is already a mapping for this CMS Entry for this Event!</div>";
                                sr.IsSuccess = false;
                                return sr;
                            }
                        }
                        List<Event> lE = new List<Event>();
                        if (entry != null)
                        {
                            foreach (EventMapping em in entry.EventMapping)
                            {
                                lE.Add(em.Event);
                            }
                        }
                        foreach (Event e in lE)
                        {
                            if ((ev.DateActive >= e.DateActive && ev.DateActive <= e.DateInactive)
                                || (ev.DateInactive <= e.DateInactive && ev.DateInactive >= e.DateActive))
                            {
                                // Problem
                                sr.Message += "<div class=\"g-m-b-5\">Date conflict with Event <span class=\"g-fc-o\">'" + e.EventName + "'</span><br />- CMS Entry <span class=\"g-fc-o\">'" + entry.entryName + "'</span>.</div>";
                                sr.IsSuccess = false;
                            }
                        }
                    }
                }
                if (!sr.IsSuccess)
                {
                    sr.Message = "<div class=\"g-m-b-5\">Save was unsuccessful.</div>" + sr.Message;
                    return sr;
                }
                using (ngmdEntities db = new ngmdEntities())
                {
                    if (EventMapping.EventMappingId > 0)
                    {
                        db.Entry(EventMapping).State = EntityState.Modified;
                    }
                    else
                    {
                        db.Entry(EventMapping).State = EntityState.Added;
                    }

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

        public SaveReturn DeleteEventMapping(int id)
        {
            SaveReturn sr = new SaveReturn();

            try
            {
                if (id > 0)
                {
                    using (ngmdEntities db = new ngmdEntities())
                    {
                        EventMapping e = db.EventMapping.Where(x => x.EventMappingId == id).FirstOrDefault();
                        db.Entry(e).State = EntityState.Deleted;
                        db.SaveChanges();
                        sr.IsSuccess = true;
                    }
                }
            }
            catch (Exception ex)
            {
                sr.IsSuccess = false;
                sr.Message = ex.Message;
            }

            return sr;
        }
        #endregion

        #region Utilities
        public void SetupSelectLists()
        {
            WebsiteNameList = GetWebsiteNames();
            SectionNameList = GetSectionNames();
            GroupNameList = GetGroupNames();
            EntryNameList = GetEntryNames(CMSEntry != null ? CMSEntry.cmsSectionFK : 0);
        }

        public IQueryable<SelectListItem> GetWebsiteNames()
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

        public List<SelectListItem> GetEventNames(int websiteId)
        {
            List<SelectListItem> oList;

            using (ngmdEntities db = new ngmdEntities())
            {
                oList = db.Event
                    .Where(x =>x.WebsiteFk == websiteId)                   
                    .OrderBy(x => x.EventName).Select(x => new SelectListItem
                    {
                        Value = x.EventId.ToString(),
                        Text = x.EventName.ToString()
                    }).ToList();

            }
            return oList;
        }

        public List<SelectListItem> GetGroupNames()
        {
            List<SelectListItem> oList;

            using (ngmdEntities db = new ngmdEntities())
            {
                int lookupTypeId = db.LookupType.FirstOrDefault(x => x.LookupTypeName == "CMS Group").LookupTypeId;

                oList = db.Lookup
                    .Where(x => x.LookupTypeFk == lookupTypeId)
                    .OrderBy(x => x.LookupName).Select(x => new SelectListItem
                    {
                        Value = x.LookupId.ToString(),
                        Text = x.LookupName.ToString()
                    })
                    .ToList();
            }
            return oList;
        }

        public List<SelectListItem> GetEntryNames(int? sectionId = null, string sectionName = "", int websiteId = 0)
        {
            List<SelectListItem> oList = new List<SelectListItem>();

            Expression<Func<cmsEntry, bool>> where;
            if (string.IsNullOrEmpty(sectionName))
            {
                where = x => x.cmsSectionFK == sectionId;
            }
            else
            {
                where = x => x.cmsSection.sectionName == sectionName && x.cmsSection.websiteFK == websiteId;
            }

            using (ngmdEntities db = new ngmdEntities())
            {
                oList = db.cmsEntry
                    .Where(where)
                    .OrderBy(x => x.entryName).Select(x => new SelectListItem
                    {
                        Value = x.cmsEntryID.ToString(),
                        Text = x.entryName.ToString()
                    }).ToList();
            }
            return oList;
        }
        #endregion

        #region Telerik Classes

        public class TelerikEntry
        {
            public int Id { get; set; }
            public string Website { get; set; }
            public string Group { get; set; }
            public string Section { get; set; }
            public string Name { get; set; }
            public string Content { get; set; }
            public string Notes { get; set; }
        }

        public class TelerikSection
        {
            public int Id { get; set; }
            public string Website { get; set; }
            public string Name { get; set; }
        }

        public class TelerikEventData
        {
            public int Id { get; set; }
            public string Website { get; set; }
            public string EventGroup { get; set; }
            public string Event { get; set; }
            public int CmsEntryId { get; set; }
            public string DefaultCmsSection { get; set; }
            public string DefaultCmsEntry { get; set; }
            public int MappedCmsEntryId { get; set; }
            public string MappedCmsEntry { get; set; }            
            public DateTime DateActivate { get; set; }
            public DateTime DateInactive { get; set; }
            public string IsActive { get; set; }
        }

        public class TelerikEventMapping
        {
            public int Id { get; set; }
            public string Event { get; set; }
            public int CmsEntryId { get; set; }
            public string DefaultCmsEntry { get; set; }
            public int MappedCmsEntryId { get; set; }
            public string MappedCmsEntry { get; set; }
            public DateTime DateActivate { get; set; }
            public DateTime DateInactive { get; set; }
            public string IsActive { get; set; }
        }

        public class TelerikEvent
        {
            public int Id { get; set; }
            public string Website { get; set; }
            public string Event { get; set; }
            public DateTime DateActivate { get; set; }
            public DateTime DateInactive { get; set; }
            public string IsActive { get; set; }
        }
        #endregion
    }
}
