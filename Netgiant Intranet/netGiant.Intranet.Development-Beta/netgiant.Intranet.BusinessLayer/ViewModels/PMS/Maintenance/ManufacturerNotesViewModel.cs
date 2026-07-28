using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance
{
    public class ManufacturerNotesViewModel : CommonViewModel
    {
        private ngmdEntities _ctx;

        public ManufacturerNotesViewModel()
        {
            _ctx = new ngmdEntities();
        }

        public IQueryable<TelerikManufacturerNotes> ManufacturerNotesList { get; set; }
        public manufacturerNotes ManufacturerNotes { get; set; }
        public IQueryable<SelectListItem> WebsiteNameList { get; set; }
        public IQueryable<SelectListItem> ManufacturerNameList { get; set; }
        public IQueryable<SelectListItem> CartridgeTypeNameList { get; set; }

       public ManufacturerNotesViewModel Get()
        {
            ManufacturerNotesList = _ctx.manufacturerNotes
                                        .Select(x => new TelerikManufacturerNotes
                                        {
                                            Id = x.manufacturerNotesID,
                                            Website = x.Website.FriendlyName,
                                            Manufacturer = x.manufacturer.manufacturerName,
                                            CartridgeType = (_ctx.Lookup
                                                .Where(y => y.LookupType.LookupTypeName == "CartridgeType" && y.AltLookupId == x.eqCartridgeTypeFK)
                                                .AsQueryable()
                                                .FirstOrDefault()
                                                .LookupName),
                                            Notes = x.note,
                                            PriorityNotes = x.priorityNote,
                                            SecondaryNotes = x.secondaryNote,
                                            MetaTitle = x.metaTitle,
                                            MetaDescription = x.metaDescription
                                        })
                                        .AsQueryable();
            return this;
        }

        private IQueryable<SelectListItem> GetWebsiteNames()
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

        public ManufacturerNotesViewModel Create(int id)
        {
            if (id > 0)
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    ManufacturerNotes = db.manufacturerNotes
                                          .Include(x => x.Website)
                                          .Where(x => x.manufacturerNotesID == id)
                                          .FirstOrDefault();
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

        public SaveReturn Delete(int id)
        {
            var sr = new SaveReturn();
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
                sr.IsSuccess = true;
            }
            catch (Exception ex)
            {
                sr.IsSuccess = false;
                sr.Message = ex.Message;
            }
            return sr;
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
            //CartridgeTypeNameList = GetCartridgeTypeNames();
            CartridgeTypeNameList = SelectListViewModel.GetNgmdLookupSelectList("CartridgeType");
        }
    }

    public class TelerikManufacturerNotes
    {
        public int Id { get; set; }
        public string Website { get; set; }
        public string Manufacturer { get; set; }
        public string CartridgeType { get; set; }
        public string Notes { get; set; }
        public string PriorityNotes { get; set; }
        public string SecondaryNotes { get; set; }
        public string MetaTitle { get; set; }
        public string MetaDescription { get; set; }
    }
}
