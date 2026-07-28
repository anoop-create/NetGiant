using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance
{
    public class EquipmentNotesViewModel : CommonViewModel
    {
        private ngmdEntities _ctx;

        public EquipmentNotesViewModel()
        {
            _ctx = new ngmdEntities();
        }

        public IQueryable<TelerikEquipmentNotes> EquipmentNotesList { get; set; }
        public equipmentNotes EquipmentNotes { get; set; }
        public IQueryable<SelectListItem> WebsiteNameList { get; set; }
        public IQueryable<SelectListItem> EquipmentNameList { get; set; }
        public IQueryable<SelectListItem> ManufacturerNameList { get; set; }
        public int ManufacturerId { get; set; } = 0;
        public EquipmentNotesViewModel Get()
        {
            EquipmentNotesList = _ctx.equipmentNotes
                                     .Select(x => new TelerikEquipmentNotes
                                     {
                                         Id = x.equipmentNotesID,
                                         Website = x.Website.FriendlyName,
                                         Equipment = x.eqEquipment.description,
                                         Detail = x.isDetail,
                                         Notes = x.note
                                     })
                                     .AsQueryable();
            return this;
        }
        
        private IQueryable<SelectListItem> GetWebsiteNames()
        {
            IQueryable<SelectListItem> query;

            using (ngmdEntities db = new ngmdEntities())
            {
                query = db.Website
                    .OrderBy(x => x.WebsiteName)
                    .Select(x => new SelectListItem
                {
                    Value = x.WebsiteID.ToString(),
                    Text = x.FriendlyName.ToString()
                }).ToList().AsQueryable();
            }
            return query;
        }

        private IQueryable<SelectListItem> GetManufacturerNames()
        {
            IQueryable<SelectListItem> query;

            using (ngmdEntities db = new ngmdEntities())
            {
                int active = db.Lookup.Where(x => x.LookupType.LookupTypeName == "EquipmentStatus" && x.LookupName == "Active").FirstOrDefault().AltLookupId ?? 0;
                query = db.eqEquipment
                    .Include(x => x.manufacturer)
                    .Where(x => x.statusFK == active)                    
                    .Select(x => new SelectListItem
                    {
                        Value = x.manufacturer.manufacturerID.ToString(),
                        Text = x.manufacturer.manufacturerName.ToString()
                    }).Distinct().ToList().AsQueryable();
            }
            return query;
        }

        private int GetManufacturerFromEquipment(int id)
        {
            eqEquipment e = new eqEquipment();
                
            using (ngmdEntities db = new ngmdEntities())
            {
                e = db.eqEquipment
                    .Where(x => x.eqEquipmentID == id)
                    .FirstOrDefault();
            }
            return e != null ? e.manufacturerFK : 0;
        }
        
        public IQueryable<SelectListItem> GetEquipmentNames(int manufacturerId)
        {
            IQueryable<SelectListItem> query;

            using (ngmdEntities db = new ngmdEntities())
            {
                int active = db.Lookup.Where(x => x.LookupType.LookupTypeName == "EquipmentStatus" && x.LookupName == "Active").FirstOrDefault().AltLookupId ?? 0;
                query = db.eqEquipment
                    .Where(x => x.manufacturerFK == manufacturerId && x.statusFK == active)
                    .OrderBy(x => x.description)
                    .Select(x => new SelectListItem
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
                    EquipmentNotes = db.equipmentNotes
                                       .Include(x => x.Website)
                                       .Include(x => x.eqEquipment)
                                       .Where(x => x.equipmentNotesID == id).FirstOrDefault();
                }
            }
            else
            {
                EquipmentNotes = new equipmentNotes();
            }
            SetupSelectLists(EquipmentNotes.eqEquipmentFK);

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

        public SaveReturn Delete(int id)
        {
            var sr = new SaveReturn();
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
                sr.IsSuccess = true;
            }
            catch (Exception ex)
            {
                sr.IsSuccess = false;
                sr.Message = ex.Message;
            }
            return sr;
        }

        private void CheckEquipmentNotesExists(ngmdEntities db)
        {
            equipmentNotes equipNote = new equipmentNotes();

            equipNote = db.equipmentNotes.Where(x => x.websiteFK == EquipmentNotes.websiteFK &&
                x.eqEquipmentFK == EquipmentNotes.eqEquipmentFK && x.isDetail == EquipmentNotes.isDetail).FirstOrDefault();

            if (equipNote != null)
                throw new Exception("Equipment Note already exists for specified criteria.");
        }

        public void SetupSelectLists(int equipmentId)
        {
            WebsiteNameList = GetWebsiteNames();
            ManufacturerNameList = GetManufacturerNames();
            ManufacturerId = GetManufacturerFromEquipment(equipmentId);
            EquipmentNameList = GetEquipmentNames(ManufacturerId);
        }
    }

    public class TelerikEquipmentNotes
    {
        public int Id { get; set; }
        public string Website { get; set; }
        public string Equipment { get; set; }
        public bool Detail { get; set; }
        public string Notes { get; set; }
    }
}
