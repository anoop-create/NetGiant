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
    public class EquipmentNotesViewModel
    {
        public List<equipmentNotes> EquipmentNotesList { get; set; }
        public int EquipmentNotesListCount { get; set; }
        public equipmentNotes EquipmentNotes { get; set; }
        public IQueryable<SelectListItem> WebsiteNameList { get; set; }
        public IQueryable<SelectListItem> EquipmentNameList { get; set; }

        public EquipmentNotesViewModel GetEquipmentNotes()
        {
            return GetEquipmentNotes(null, null, null, null, null, 1);
        }

        public EquipmentNotesViewModel GetEquipmentNotes(string orderBy, string searchTerm, string searchBy, int? websiteID, int? EquipmentID, int? block)
        {
            int blockSize = 50;
            int blockNumber = (block ?? 1);

             using (ngmdEntities db = new ngmdEntities())
             {
                 IQueryable<equipmentNotes> query = db.equipmentNotes
                     .Include(x => x.eqEquipment)
                     .Include(x => x.Website);

                 if (!string.IsNullOrEmpty(searchTerm))
                 {
                     switch (searchBy)
                     {
                         case "website":
                             query = query.Where(x => x.Website.WebsiteName.ToString().Contains(searchTerm.Trim()));
                             break;
                         case "equipment":
                             query = query.Where(x => x.eqEquipment.description.ToString().Contains(searchTerm.Trim().ToLower()));
                             break;
                         case "note":
                             query = query.Where(x => x.note.ToLower().Contains(searchTerm.Trim().ToLower()));
                             break;
                        default:
                             break;
                     }
                 }

                 if (websiteID != null && websiteID > 0)
                 {
                     query = query.Where(x => x.websiteFK == websiteID);
                 }

                 if (EquipmentID != null && EquipmentID > 0)
                 {
                     query = query.Where(x => x.eqEquipmentFK == EquipmentID);
                 }

                 switch (orderBy)
                 {
                     case "websiteAsc":
                         query = query.OrderBy(x => x.Website.WebsiteName);
                         break;
                     case "websiteDesc":
                         query = query.OrderByDescending(x => x.Website.WebsiteName);
                         break;
                     case "equipmentAsc":
                         query = query.OrderBy(x => x.eqEquipment.description);
                         break;
                     case "equipmentDesc":
                         query = query.OrderByDescending(x => x.eqEquipment.description);
                         break;
                     case "notesAsc":
                         query = query.OrderBy(x => x.note);
                         break;
                     case "notesDesc":
                         query = query.OrderByDescending(x => x.note);
                         break;
                    case "isDetailAsc":
                        query = query.OrderBy(x => x.isDetail);
                        break;
                    case "isDetailDesc":
                        query = query.OrderByDescending(x => x.isDetail);
                        break;
                    default:
                         query = query.OrderBy(x => x.equipmentNotesID);
                         break;
                 }

                 EquipmentNotesListCount = query.Count();
                 EquipmentNotesList = query
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

        private IQueryable<SelectListItem> GetEquipmentNames()
        {
            IQueryable<SelectListItem> query;

            using (ngmdEntities db = new ngmdEntities())
            {
                query = db.eqEquipment.OrderBy(x => x.description).Select(x => new SelectListItem
                {
                    Value = x.eqEquipmentID.ToString(),
                    Text = x.description.ToString()
                }).ToList().AsQueryable();
            }
            return query;
        }

        public EquipmentNotesViewModel Create(int id)
        {
            if (id > 0)
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    EquipmentNotes = db.equipmentNotes.Where(x => x.equipmentNotesID == id).FirstOrDefault();
                }
            }
            else
            {
                EquipmentNotes = new equipmentNotes();
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
                    if (EquipmentNotes.equipmentNotesID > 0)
                    {
                        db.Entry(EquipmentNotes).State = EntityState.Modified;
                    }
                    else
                    {
                        //Check if a Equipment Note already exists for specified criteria
                        CheckEquipmentNotesExists(db);
                        db.Entry(EquipmentNotes).State = EntityState.Added;
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
                        equipmentNotes en = db.equipmentNotes.Where(x => x.equipmentNotesID == id).FirstOrDefault();
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

        private void CheckEquipmentNotesExists(ngmdEntities db)
        {
            equipmentNotes equipNote = new equipmentNotes();

            equipNote = db.equipmentNotes.Where(x => x.websiteFK == EquipmentNotes.websiteFK &&
                x.eqEquipmentFK == EquipmentNotes.eqEquipmentFK && x.isDetail == EquipmentNotes.isDetail).FirstOrDefault();

            if (equipNote != null)
                throw new Exception("Equipment Note already exists for specified criteria.");
        }

        public void SetupSelectLists()
        {
            WebsiteNameList = GetWebsiteNames();
            EquipmentNameList = GetEquipmentNames();
        }
    }
}
