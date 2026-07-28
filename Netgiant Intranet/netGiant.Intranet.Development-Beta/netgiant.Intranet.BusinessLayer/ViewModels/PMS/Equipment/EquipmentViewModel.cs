using System;
using System.Linq;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System.Data.Entity;
using System.Web.Mvc;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using PagedList;
using System.Linq.Expressions;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Equipment
{
    public class EquipmentViewModel : CommonViewModel
    {
        private ngmdEntities _ctx;

        public EquipmentViewModel()
        {
            _ctx = new ngmdEntities();
        }

        public IQueryable<TelerikEquipment> EquipmentList { get; set; }
        public IQueryable<TelerikFamily> FamilyList { get; set; }
        public eqEquipment Equipment { get; set; }
        public eqFamily Family { get; set; }
        public eqFamilyMembership FamilyMembership { get; set; }

        public IQueryable<SelectListItem> AllFamilies { get; set; }
        public IQueryable<SelectListItem> AllProductTypes { get; set; }
        public IQueryable<SelectListItem> AllCartridgeTypes { get; set; }
        public IQueryable<SelectListItem> AllEquipStatus { get; set; }
        public IQueryable<SelectListItem> AllMetaContentTypes { get; set; }
        public IQueryable<SelectListItem> AllEquipManufacturers { get; set; }
        public IQueryable<SelectListItem> AllEquipment { get; set; }

        public IPagedList<eqProductMembership> EquipmentProductMembershipList { get; set; }
        public IPagedList<eqFamilyMembership> EquipmentFamilyMembershipList { get; set; }

        //temp
        public int PageNumber { get; set; } = 1;
        public int PageCount { get; set; } = 200;
        public int FamilyID { get; set; }

        public void GetEquipment()
        {
            EquipmentList = _ctx.eqEquipment                
                .Select(x => new TelerikEquipment
                {
                    Id = x.eqEquipmentID,
                    Equipment = x.description,
                    Manufacturer = x.manufacturer.equipmentManuName,
                    CartridgeType = (_ctx.Lookup
                                .Where(y => y.LookupType.LookupTypeName == "CartridgeType" && y.AltLookupId == x.eqCartridgeTypeFK)
                                .AsQueryable()
                                .FirstOrDefault()
                                .LookupName),
                    Product = x.product.productName ?? "N/A",
                    DateLastUpdated = x.dateLastUpdate,
                    Status = (_ctx.Lookup
                                .Where(y => y.LookupType.LookupTypeName == "EquipmentStatus" && y.AltLookupId == x.statusFK)
                                .AsQueryable()
                                .FirstOrDefault()
                                .LookupName),
                    DateInactive = x.dateInactive
                })
                .AsQueryable();
        }

        public void GetFamilies()
        {
            FamilyList = _ctx.eqFamily.Select(x => new TelerikFamily
            {
                Id = x.eqFamilyID,
                Family = x.description,
                Manufacturer = x.manufacturer.equipmentManuName,
                DateLastUpdated = x.dateLastUpdate
            })
            .AsQueryable();
        }

        public EquipmentViewModel GetEquipment(int id)
        {
            if (id > 0)
            {
                Equipment = _ctx.eqEquipment
                  .Include(x => x.product)
                  .Include("manufacturer")
                  .Include("eqProductMembership")
                  .Include("eqFamilyMembership.eqFamily")
                  .Where(x => x.eqEquipmentID == id).FirstOrDefault();
            }
            else
            {
                Equipment = new eqEquipment();
            }
            AllFamilies = SelectListViewModel.GetAllEquipFamilies();
            AllProductTypes = SelectListViewModel.GetNgmdLookupSelectList("ProductType");
            //AllCartridgeTypes = SelectListViewModel.GetAllCartridgeTypes();
            AllCartridgeTypes = SelectListViewModel.GetNgmdLookupSelectList("CartridgeType");
            AllEquipManufacturers = SelectListViewModel.GetAllEquipManufacturers();
            AllEquipStatus = SelectListViewModel.GetNgmdLookupSelectList("EquipmentStatus");
            AllMetaContentTypes = SelectListViewModel.GetNgmdLookupSelectList("MetaContentType");
            return this;
        }

        public EquipmentViewModel GetFamily(int id)
        {
            if (id > 0)
            {
                Family = _ctx.eqFamily.Where(x => x.eqFamilyID == id).FirstOrDefault();
            }
            else
            {
                Family = new eqFamily();
            }
            AllEquipManufacturers = SelectListViewModel.GetAllEquipManufacturers();
            return this;
        }

        public EquipmentViewModel GetFamilyMembership(int id)
        {
            if (id > 0)
            {
                FamilyMembership = _ctx.eqFamilyMembership.Where(x => x.eqFamilyMembershipID == id).FirstOrDefault();
            }
            else
            {
                FamilyMembership = new eqFamilyMembership();
            }
            AllEquipment = SelectListViewModel.GetAllEquipment();
            return this;
        }

        public EquipmentViewModel GetEquipmentProductMemberships(int id)
        {
            if (id > 0)
            {
                EquipmentProductMembershipList = _ctx.eqProductMembership
                                                     .Include(x => x.eqEquipment)
                                                     .Include(x => x.product)
                                                     .Include("product.manufacturer")
                                                     .Where(w => w.eqEquipmentFK == id)
                                                     .OrderBy(o => o.product.productName)
                                                     .ToPagedList(PageNumber, PageCount);
            }

            return this;
        }

        public EquipmentViewModel GetEquipmentFamilyMemberships(int id, bool useEquipmentId)
        {
            if (id > 0)
            {
                Expression<Func<eqFamilyMembership, bool>> where;
                if (useEquipmentId)
                {
                    where = x => x.eqEquipmentID == id;
                }
                else
                {
                    where = x => x.eqFamilyID == id;
                }
                EquipmentFamilyMembershipList = _ctx.eqFamilyMembership
                                                    .Include("eqEquipment")
                                                    .Include("eqFamily")
                                                    .Include("eqEquipment.manufacturer")
                                                    .Where(where)
                                                    .OrderBy(o => o.eqEquipment.description)
                                                    .ToPagedList(PageNumber, PageCount);
            }
      
            return this;
        }

        public string GetEquipmentName(int id)
        {
            return _ctx.eqEquipment.Find(id).description;
        }

        public bool SaveEquipment()
        {
            bool success = true;
            Equipment.dateLastUpdate = DateTime.Now;

            try
            {
                if (Equipment.eqEquipmentID > 0)
                {
                    _ctx.Entry(Equipment).State = EntityState.Modified;
                }
                else
                {
                    CheckEquipmentExists();
                    Equipment.dateCreated = DateTime.Now;
                    _ctx.Entry(Equipment).State = EntityState.Added;
                }
                foreach (eqFamilyMembership fm in Equipment.eqFamilyMembership)
                {
                    if (fm.eqFamilyMembershipID > 0)
                    {
                        _ctx.Entry(fm).State = EntityState.Modified;
                    }
                    else
                    {
                        _ctx.Entry(fm).State = EntityState.Added;
                    }
                }

                _ctx.SaveChanges();
            }
            catch (Exception ex)
            {
                success = false;
                throw new ApplicationException(ex.Message + ex.StackTrace);
            }

            return success;
        }

        public bool SaveFamily()
        {
            Family.dateLastUpdate = DateTime.Now;
            bool success = false;
            try
            {
                if (Family.eqFamilyID > 0)
                {
                    _ctx.Entry(Family).State = EntityState.Modified;
                }
                else
                {
                    CheckFamilyExists();
                    _ctx.Entry(Family).State = EntityState.Added;
                }

                _ctx.SaveChanges();
            }
            catch
            {
                success = false;
            }

            return success;
        }

        public SaveReturn DeleteEquipment(int id)
        {
            var sr = new SaveReturn();
            
            try
            {
                if (id > 0)
                {
                    eqEquipment equip = _ctx.eqEquipment.Where(w => w.eqEquipmentID == id).FirstOrDefault();
                    _ctx.Entry(equip).State = EntityState.Deleted;
                    _ctx.SaveChanges();
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

        public SaveReturn DeleteFamily(int id)
        {
            var sr = new SaveReturn();

            try
            {
                if (id > 0)
                {
                    eqFamily fam = _ctx.eqFamily.Where(w => w.eqFamilyID == id).FirstOrDefault();
                    _ctx.Entry(fam).State = EntityState.Deleted;
                    _ctx.SaveChanges();
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

        private void CheckEquipmentExists()
        {
            var result = _ctx.eqEquipment
                             .Where(w =>
                                w.manufacturerFK == Equipment.manufacturerFK &&
                                w.description == Equipment.description &&
                                w.eqCartridgeTypeFK == Equipment.eqCartridgeTypeFK
                             ).FirstOrDefault();

            if (result != null) throw new Exception("Equipment already exists");
        }

        private void CheckFamilyExists()
        {
            var result = _ctx.eqFamily
                             .Where(w =>
                                w.description == Family.description &&
                                w.manufacturerFK == Family.manufacturerFK
                             ).FirstOrDefault();

            if (result != null) throw new Exception("Family already exists");
        }

        public string GetFamilyName(int id)
        {
            return _ctx.eqFamily.Find(id).description;
        }

        public bool SaveMembership(int equipmentId, int[] products)
        {
            bool success = true;

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    for (int i = 0; i < products.Length; i++)
                    {
                        eqProductMembership membership = new eqProductMembership();
                        membership.productFK = products[i];
                        membership.eqEquipmentFK = equipmentId;

                        db.eqProductMembership.Add(membership);
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

        public bool DeleteMembership(int id)
        {
            bool success = true;

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    eqProductMembership prodMem = db.eqProductMembership.Find(id);
                    db.eqProductMembership.Remove(prodMem);
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                success = false;
                throw new ApplicationException(e.Message + e.StackTrace);
            }

            return success;
        }

        public bool DeleteFamilyMembership(int id)
        {
            bool success = true;

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    eqFamilyMembership eqFamMem = db.eqFamilyMembership.Find(id);
                    db.eqFamilyMembership.Remove(eqFamMem);
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                success = false;
                throw new ApplicationException(e.Message + e.StackTrace);
            }

            return success;
        }

        public bool SaveFamilyMembership(EquipmentViewModel eqVm)
        {
            bool success = true;

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    eqFamilyMembership famMem = new eqFamilyMembership();
                    famMem.eqFamilyID = eqVm.FamilyID;
                    famMem.eqEquipmentID = eqVm.Equipment.eqEquipmentID;

                    db.eqFamilyMembership.Add(famMem);
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                success = false;
                throw new ApplicationException(e.Message + e.StackTrace);
            }

            return success;
        }

 
        public static eqFamilyMembership CreateNewFamilyMapping(int familyID, int equipmentID)
        {
            var famMap = new eqFamilyMembership();

            using (ngmdEntities db = new ngmdEntities())
            {
                famMap.eqFamilyID = familyID;
                famMap.eqEquipmentID = equipmentID;
                famMap.eqFamily = db.eqFamily.Find(familyID);

                return famMap;
            }
        }

        public class TelerikEquipment
        {
            public int Id { get; set; }
            public string Equipment { get; set; }
            public string Manufacturer { get; set; }
            public string CartridgeType { get; set; }
            public string Product { get; set; }
            public DateTime DateLastUpdated { get; set; }
            public string Status { get; set; }
            public DateTime? DateInactive { get; set; }
            public DateTime dateCreated { get; set; }
        }

        public class TelerikFamily
        {
            public int Id { get; set; }
            public string Family { get; set; }
            public string Manufacturer { get; set; }
            public DateTime DateLastUpdated { get; set; }
        }

        public enum ReturnType
        {
            All,
            Families,
            Equipment,
            SingleFamily,
            SingleEquipment,
            ProductMemberships,
            SingleProductMembership,
            FamilyMemberships,
            SingleFamilyMembership
        }
    }
}
