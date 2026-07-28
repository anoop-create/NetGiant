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
    public class ManufacturerNotesViewModel
    {
        public List<manufacturerNotes> ManufacturerNotesList { get; set; }
        public int ManufacturerNotesListCount { get; set; }
        public manufacturerNotes ManufacturerNotes { get; set; }
        public IQueryable<SelectListItem> WebsiteNameList { get; set; }
        public IQueryable<SelectListItem> ManufacturerNameList { get; set; }
        public IQueryable<SelectListItem> CartridgeTypeNameList { get; set; }

        public ManufacturerNotesViewModel GetManufacturerNotes()
        {
            return GetManufacturerNotes(null, null, null, null, null, null, 1);
        }

        public ManufacturerNotesViewModel GetManufacturerNotes(string orderBy, string searchTerm, string searchBy, int? websiteID, 
            int? ManufacturerID, int? CartridgetypeID, int? block)
        {
            int blockSize = 50;
            int blockNumber = (block ?? 1);

            using (ngmdEntities db = new ngmdEntities())
            {
                IQueryable<manufacturerNotes> query = db.manufacturerNotes
                     .Include(x => x.manufacturer)
                     .Include(x => x.eqCartridgeType)
                     .Include(x => x.Website);

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    switch (searchBy)
                    {
                        case "website":
                            query = query.Where(x => x.Website.WebsiteName.ToString().Contains(searchTerm.Trim()));
                            break;
                        case "manufacturer":
                            query = query.Where(x => x.manufacturer.manufacturerName.ToString().Contains(searchTerm.Trim().ToLower()));
                            break;
                        case "cartridge":
                            query = query.Where(x => x.eqCartridgeType.eqCartridgeTypeName.ToLower().Contains(searchTerm.Trim().ToLower()));
                            break;
                        case "note":
                            query = query.Where(x => x.note.ToLower().Contains(searchTerm.Trim().ToLower()));
                            break;
                        case "title":
                            query = query.Where(x => x.metaTitle.ToLower().Contains(searchTerm.Trim().ToLower()));
                            break;
                        case "description":
                            query = query.Where(x => x.metaDescription.ToLower().Contains(searchTerm.Trim().ToLower()));
                            break;
                        default:
                            break;
                    }
                }

                if (websiteID != null && websiteID > 0)
                {
                    query = query.Where(x => x.websiteFK == websiteID);
                }

                if (ManufacturerID == -2)
                {
                    query = query.Where(x => x.manufacturerFK == null);
                }
                else if (ManufacturerID != null && ManufacturerID > 0)
                {
                    query = query.Where(x => x.manufacturerFK == ManufacturerID);
                }

                if (CartridgetypeID != null && CartridgetypeID > 0)
                {
                    query = query.Where(x => x.eqCartridgeTypeFK == CartridgetypeID);
                }

                switch (orderBy)
                {
                    case "websiteAsc":
                        query = query.OrderBy(x => x.Website.WebsiteName);
                        break;
                    case "websiteDesc":
                        query = query.OrderByDescending(x => x.Website.WebsiteName);
                        break;
                    case "manufacturerAsc":
                        query = query.OrderBy(x => x.manufacturer.manufacturerName);
                        break;
                    case "manufacturerDesc":
                        query = query.OrderByDescending(x => x.manufacturer.manufacturerName);
                        break;
                    case "cartridgeAsc":
                        query = query.OrderBy(x => x.eqCartridgeType.eqCartridgeTypeName);
                        break;
                    case "cartridgeDesc":
                        query = query.OrderByDescending(x => x.eqCartridgeType.eqCartridgeTypeName);
                        break;
                    case "noteAsc":
                        query = query.OrderBy(x => x.note);
                        break;
                    case "noteDesc":
                        query = query.OrderByDescending(x => x.note);
                        break;
                    case "titleAsc":
                        query = query.OrderBy(x => x.metaTitle);
                        break;
                    case "titleDesc":
                        query = query.OrderByDescending(x => x.metaTitle);
                        break;
                    case "descriptionAsc":
                        query = query.OrderBy(x => x.metaDescription);
                        break;
                    case "descriptionDesc":
                        query = query.OrderByDescending(x => x.metaDescription);
                        break;
                    default:
                        query = query.OrderBy(x => x.manufacturerNotesID);
                        break;
                }

                ManufacturerNotesListCount = query.Count();
                ManufacturerNotesList = query
                    .Skip((blockNumber - 1) * blockSize)
                    .Take(blockSize)
                    .ToList();
                SetupSelectLists();

            }
            return this;
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

        private IQueryable<SelectListItem> GetManufacturerNames()
        {
            IQueryable<SelectListItem> query;

            using (ngmdEntities db = new ngmdEntities())
            {
                query = db.manufacturer.OrderBy(x => x.manufacturerName).Select(x => new SelectListItem
                {
                    Value = x.manufacturerID.ToString(),
                    Text = x.manufacturerName.ToString()
                }).ToList().AsQueryable();
            }
            return query;
        }

        private IQueryable<SelectListItem> GetCartridgeTypeNames()
        {
            IQueryable<SelectListItem> query;

            using (ngmdEntities db = new ngmdEntities())
            {
                query = db.eqCartridgeType.OrderBy(x => x.eqCartridgeTypeName).Select(x => new SelectListItem
                {
                    Value = x.eqCartridgeTypeID.ToString(),
                    Text = x.eqCartridgeTypeName.ToString()
                }).ToList().AsQueryable();
            }
            return query;
        }

        public ManufacturerNotesViewModel Create(int id)
        {
            if (id > 0)
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    ManufacturerNotes = db.manufacturerNotes.Where(x => x.manufacturerNotesID == id).FirstOrDefault();
                }
            }
            else
            {
                ManufacturerNotes = new manufacturerNotes();
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
                    if (ManufacturerNotes.manufacturerNotesID > 0)
                    {
                        db.Entry(ManufacturerNotes).State = EntityState.Modified;
                    }
                    else
                    {
                        //Check if a Manufacturer Note already exists for specified criteria
                        CheckManufacturerNoteExists(db);
                        db.Entry(ManufacturerNotes).State = EntityState.Added;
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
                        manufacturerNotes en = db.manufacturerNotes.Where(x => x.manufacturerNotesID == id).FirstOrDefault();
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

        private void CheckManufacturerNoteExists(ngmdEntities db)
        {
            manufacturerNotes manuNote = new manufacturerNotes();

            manuNote = db.manufacturerNotes.Where(x => x.websiteFK == ManufacturerNotes.websiteFK &&
                x.manufacturerFK == ManufacturerNotes.manufacturerFK &&
                x.eqCartridgeTypeFK == ManufacturerNotes.eqCartridgeTypeFK).FirstOrDefault();

            if (manuNote != null)
                throw new Exception("Manufacturer Note already exists for specified criteria.");
        }

        public void SetupSelectLists()
        {
            WebsiteNameList = GetWebsiteNames();
            ManufacturerNameList = GetManufacturerNames();
            CartridgeTypeNameList = GetCartridgeTypeNames();
        }
    }
}
