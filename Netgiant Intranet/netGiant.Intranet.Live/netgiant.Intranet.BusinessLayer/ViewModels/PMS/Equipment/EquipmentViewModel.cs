using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PagedList;
using netGiant.Intranet.DataLayer;
using System.Data.Entity.Core.Objects;
using System.Data.Entity;
using System.Web.Mvc;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using System.Data.Entity.Infrastructure;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Equipment
{
    public class EquipmentViewModel
    {

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

        public PagedList.IPagedList<eqFamily> familiesList { get; set; }
        public PagedList.IPagedList<eqEquipment> equipmentList { get; set; }
        public PagedList.IPagedList<eqProductMembership> equipmentMemList { get; set; }
        public PagedList.IPagedList<eqFamilyMembership> familyMembershipList { get; set; }
        public eqFamily family { get; set; }
        public eqEquipment equipment { get; set; }
        public eqFamilyMembership famMembership { get; set; }
        public int productID { get; set; }
        public int equipID { get; set; }
        public int familyID { get; set; }
        public IQueryable<SelectListItem> AllFamilies { get; set; }
        public IQueryable<SelectListItem> AllEquips { get; set; }
        public IQueryable<SelectListItem> AllProductTypes { get; set; }
        public IQueryable<SelectListItem> AllProductsPartNoDesc { get; set; }
        public IQueryable<SelectListItem> AllEquipManufacturers { get; set; }
        public IQueryable<SelectListItem> AllCartridgeTypes { get; set; }
        public IQueryable<SelectListItem> AllEquipStatus { get; set; }
        public IQueryable<SelectListItem> AllMetaContentTypes { get; set; }

        private int pN;
        private int pS;
        private string sT;
        private int? manId;
        private int? statId;
        private string sOrderBy;

        /// <summary>
        /// Populate properties of class based on the enum return type requested. Enum keeps DB trans to a minimum
        /// </summary>
        /// <param name="page">Page number requested from Controller</param>
        /// <param name="type">The enum type requested</param>
        /// <param name="id">The ID of the family or equipment, when requesting a single record</param>
        /// <param name="searchBy">The search string passed</param>
        /// <returns></returns>
        public EquipmentViewModel Get(int? page, ReturnType type, int id, string searchTerm,
                                        int? manufacturerId, int? statusId , string orderBy)
        {
            int pageSize = 21;
            int pageNumber = (page ?? 1);

            using (ngmdEntities db = new ngmdEntities())
            {

                pN = pageNumber;
                pS = pageSize;
                sT = searchTerm;
                manId = manufacturerId;
                statId = statusId;
                sOrderBy = orderBy;

                //Execute queries here, based on the enum type requested
                switch (type)
                {
                    case ReturnType.All:

                        ExecuteFamiliesQuery(db);
                        ExecuteEquipmentQuery(db);
                        ExecuteSelectListQuery_EquipManus();
                        break;

                    case ReturnType.Families:

                        ExecuteFamiliesQuery(db);
                        ExecuteSelectListQuery_EquipManus();
                        break;

                    case ReturnType.Equipment:

                        ExecuteEquipmentQuery(db);
                        ExecuteSelectListQuery_EquipManus();
                        AllEquipStatus = GetStatusNames();
                        break;

                    case ReturnType.SingleFamily:

                        ExecuteSingleFamilyQuery(db, id);
                        ExecuteSelectListQuery_EquipManus();
                        break;

                    case ReturnType.SingleEquipment:

                        ExecuteSingleEquipmentQuery(db, id);
                        ExecuteSelectListQuery_EquipManus();
                        ExecuteSelectListQuery_Fams();
                        ExecuteSelectListQuery_ProdTypes();
                        AllEquipStatus = GetStatusNames();
                        AllMetaContentTypes = GetMetaContentTypes();
                        break;

                    case ReturnType.ProductMemberships:

                        ExecuteMembershipQuery(db, id);
                        break;

                    case ReturnType.SingleProductMembership:

                        ExecuteSingleEquipmentQuery(db, id);
                        ExecuteSelectListQuery_Products();
                        break;

                    case ReturnType.FamilyMemberships:

                        ExecuteFamilyMembershipQuery(db, id);
                        break;

                    case ReturnType.SingleFamilyMembership:

                        ExecuteSingleFamilyMembershipQuery(db, id);
                        ExecuteSelectListQuery_Equips();
                        break;

                    default:
                        break;
                }
            }
            return this;
        }

        /// <summary>
        /// Create or update a single family
        /// </summary>
        /// <param name="eqVm"></param>
        /// <returns></returns>
        public bool SaveFamily(EquipmentViewModel eqVm)
        {
            bool success = true;

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    eqVm.family.dateLastUpdate = DateTime.Now;

                    if (eqVm.family.eqFamilyID > 0)
                    {
                        db.Entry(eqVm.family).State = EntityState.Modified;
                    }
                    else
                    {
                        db.eqFamily.Add(eqVm.family);
                    }

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

        /// <summary>
        /// Deletes a family based on the id
        /// </summary>
        /// <param name="id">The eqFamilyID</param>
        /// <returns></returns>
        public bool DeleteFamily(int id)
        {
            bool success = true;

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    //IEnumerable<eqMembership> memList = db.eqMembership.Where(x => x.);

                    //IEnumerable<eqEquipment> equipList = db.eqEquipment.Where(x => x.eqFamilyFK == id).ToList();
                    //db.eqEquipment.RemoveRange(equipList);

                    eqFamily fam = db.eqFamily.Find(id);
                    db.eqFamily.Remove(fam);
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

        /// <summary>
        /// Create or update a single equipment
        /// </summary>
        /// <param name="eqVm"></param>
        /// <returns></returns>
        public bool SaveEquipment(EquipmentViewModel eqVm)
        {
            bool success = true;

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    eqVm.equipment.dateLastUpdate = DateTime.Now;

                    if (eqVm.equipment.productFK == 0)
                    {
                        eqVm.equipment.productFK = null;
                    }

                    if (eqVm.equipment.eqEquipmentID > 0)
                    {
                        //eqVm.equipment.eqFamilyMembership = null;
                        db.Entry(eqVm.equipment).State = EntityState.Modified;
                    }
                    else
                    {
                        db.eqEquipment.Add(eqVm.equipment);
                    }

                    foreach (var map in eqVm.equipment.eqFamilyMembership)
                    {
                        var dbMapping = db.eqFamilyMembership
                            .Where(x => x.eqFamilyID == map.eqFamilyID &&
                                x.eqEquipmentID == map.eqEquipmentID)
                                .FirstOrDefault();

                        if (dbMapping == null)
                        {
                            db.Entry(map).State = EntityState.Added;
                            db.SaveChanges();
                        }
                    }

                    db.SaveChanges();
                }

                using (ngmdEntities db = new ngmdEntities())
                {
                    var existingEquip = db.eqEquipment.Find(eqVm.equipment.eqEquipmentID);

                    List<int> nonSelectedFamilies = existingEquip.eqFamilyMembership.Select(x => x.eqFamilyMembershipID)
                        .Except(eqVm.equipment.eqFamilyMembership.Select(x => x.eqFamilyMembershipID)).ToList();

                    for (var i = 0; i < nonSelectedFamilies.Count(); i++)
                    {
                        db.eqFamilyMembership.Remove(db.eqFamilyMembership.Find(nonSelectedFamilies[i]));
                    }

                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                success = false;
                throw new ApplicationException(e.Message + e.StackTrace, e.InnerException);
            }

            return success;
        }

        /// <summary>
        /// Deletes equipment based on the id
        /// </summary>
        /// <param name="id">The eqEquipmentID</param>
        /// <returns></returns>
        public bool DeleteEquipment(int id)
        {
            bool success = true;

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    eqEquipment equip = db.eqEquipment.Find(id);
                    equip.statusFK = 3;
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

        /// <summary>
        /// Deletes family membership based on the id
        /// </summary>
        /// <param name="id">The eqFamilyMembershipID</param>
        /// <returns></returns>
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

        public bool SaveMembership(EquipmentViewModel eqVm)
        {
            bool success = true;

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    eqProductMembership prodMem = new eqProductMembership();
                    prodMem.productFK = eqVm.productID;
                    prodMem.eqEquipmentFK = eqVm.equipment.eqEquipmentID;

                    db.eqProductMembership.Add(prodMem);
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
                    famMem.eqFamilyID = eqVm.familyID;
                    famMem.eqEquipmentID = eqVm.equipID;

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

        /// <summary>
        /// Deletes equipment based on the id
        /// </summary>
        /// <param name="id">The eqEquipmentID</param>
        /// <returns></returns>
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

        public string GetEquipmentName(int id)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                return db.eqEquipment.Find(id).description;
            }
        }

        public string GetFamilyName(int id)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                return db.eqFamily.Find(id).description;
            }
        }

        //These private methods are used to Execute the queries
        private void ExecuteFamiliesQuery(ngmdEntities db)
        {
            IQueryable<eqFamily> familiesQuery = db.eqFamily.Include("manufacturer").OrderBy(x => x.description);

            if (!string.IsNullOrEmpty(sT))
            {
                familiesQuery = familiesQuery.Where(x => x.description.ToLower().Contains(sT.ToLower()));
            }

            if (manId != null && manId > 0)
            {
                familiesQuery = familiesQuery.Where(x => x.manufacturerFK == manId);
            }

            switch (sOrderBy)
            {
                case "familyAsc":
                    familiesQuery = familiesQuery.OrderBy(x => x.description);
                    break;
                case "familyDesc":
                    familiesQuery = familiesQuery.OrderByDescending(x => x.description);
                    break;
                case "manufacturerAsc":
                    familiesQuery = familiesQuery.OrderBy(x => x.manufacturer.manufacturerName);
                    break;
                case "manufacturerDesc":
                    familiesQuery = familiesQuery.OrderByDescending(x => x.manufacturer.manufacturerName);
                    break;
                case "dateLastUpdatedAsc":
                    familiesQuery = familiesQuery.OrderBy(x => x.dateLastUpdate);
                    break;
                case "dateLastUpdatedDesc":
                    familiesQuery = familiesQuery.OrderByDescending(x => x.dateLastUpdate);
                    break;
                default:
                    familiesQuery = familiesQuery.OrderBy(x => x.description);
                    break;
            }

            familiesList = familiesQuery.ToPagedList(pN, pS);
        }

        private void ExecuteEquipmentQuery(ngmdEntities db)
        {
            IQueryable<eqEquipment> equipmentQuery = db.eqEquipment
                .Include("manufacturer").OrderBy(x => x.description)
                .Include("eqCartridgeType")
                .Include("product")
                .Include("equipmentStatus");

            if (!string.IsNullOrEmpty(sT))
            {
                equipmentQuery = equipmentQuery.Where(x => x.description.ToLower().Contains(sT.ToLower()));
            }

            if (manId != null && manId > 0)
            {
                equipmentQuery = equipmentQuery.Where(x => x.manufacturerFK == manId);
            }

            if (statId != null && statId > 0)
            {
                equipmentQuery = equipmentQuery.Where(x => x.statusFK == statId);
            }

            switch (sOrderBy)
            {
                case "equipmentAsc":
                    equipmentQuery = equipmentQuery.OrderBy(x => x.description);
                    break;
                case "equipmentDesc":
                    equipmentQuery = equipmentQuery.OrderByDescending(x => x.description);
                    break;
                case "manufacturerAsc":
                    equipmentQuery = equipmentQuery.OrderBy(x => x.manufacturer.manufacturerName);
                    break;
                case "manufacturerDesc":
                    equipmentQuery = equipmentQuery.OrderByDescending(x => x.manufacturer.manufacturerName);
                    break;
                case "cartridgeAsc":
                    equipmentQuery = equipmentQuery.OrderBy(x => x.eqCartridgeType.eqCartridgeTypeName);
                    break;
                case "cartridgeDesc":
                    equipmentQuery = equipmentQuery.OrderByDescending(x => x.eqCartridgeType.eqCartridgeTypeName);
                    break;
                case "productAsc":
                    equipmentQuery = equipmentQuery.OrderBy(x => x.product.productName);
                    break;
                case "productDesc":
                    equipmentQuery = equipmentQuery.OrderByDescending(x => x.product.productName);
                    break;
                case "dateLastUpdatedAsc":
                    equipmentQuery = equipmentQuery.OrderBy(x => x.dateLastUpdate);
                    break;
                case "dateLastUpdatedDesc":
                    equipmentQuery = equipmentQuery.OrderByDescending(x => x.dateLastUpdate);
                    break;
                case "statusAsc":
                    equipmentQuery = equipmentQuery.OrderBy(x => x.equipmentStatus.Status);
                    break;
                case "statusDesc":
                    equipmentQuery = equipmentQuery.OrderByDescending(x => x.equipmentStatus.Status);
                    break;
                default:
                    equipmentQuery = equipmentQuery.OrderBy(x => x.description);
                    break;
            }

            equipmentList = equipmentQuery.ToPagedList(pN, pS);
        }

        private void ExecuteSingleFamilyQuery(ngmdEntities db, int id)
        {
            family = db.eqFamily.Where(x => x.eqFamilyID == id).FirstOrDefault();

            if (family == null)
            {
                family = new eqFamily();
            }
        }

        private void ExecuteSingleEquipmentQuery(ngmdEntities db, int id)
        {
            equipment = db.eqEquipment
                .Include(x => x.product)
                .Include("manufacturer")
                .Include("eqCartridgeType")
                .Include("eqProductMembership")
                .Include("eqFamilyMembership.eqFamily")
                .Include("equipmentStatus")
                .OrderBy(x => x.description)
                .Where(x => x.eqEquipmentID == id)
                .FirstOrDefault();

            if (equipment == null)
            {
                equipment = new eqEquipment();
            }

            ExecuteSelectListQuery_Cartridge();
        }

        private void ExecuteMembershipQuery(ngmdEntities db, int id)
        {
            IQueryable<eqProductMembership> eqProductMembQuery = db.eqProductMembership
                .Include(x => x.eqEquipment)
                .Include(x => x.product)
                .Include("product.manufacturer").Where(x => x.eqEquipmentFK == id);

            switch (sOrderBy)
            {
                case "productAsc":
                    eqProductMembQuery = eqProductMembQuery.OrderBy(x => x.product.productName);
                    break;
                case "productDesc":
                    eqProductMembQuery = eqProductMembQuery.OrderByDescending(x => x.product.productName);
                    break;
                case "altRefAsc":
                    eqProductMembQuery = eqProductMembQuery.OrderBy(x => x.product.partNo);
                    break;
                case "altRefDesc":
                    eqProductMembQuery = eqProductMembQuery.OrderByDescending(x => x.product.partNo);
                    break;
                case "manufacturerAsc":
                    eqProductMembQuery = eqProductMembQuery.OrderBy(x => x.product.manufacturer.manufacturerName);
                    break;
                case "manufacturerDesc":
                    eqProductMembQuery = eqProductMembQuery.OrderByDescending(x => x.product.manufacturer.manufacturerName);
                    break;
                default:
                    eqProductMembQuery = eqProductMembQuery.OrderBy(x => x.product.productName);
                    break;
            }

            equipmentMemList = eqProductMembQuery.ToPagedList(pN, pS);
        }

        private void ExecuteFamilyMembershipQuery(ngmdEntities db, int id)
        {
            IQueryable<eqFamilyMembership> eqFamilyMemQuery = db.eqFamilyMembership.Include("eqEquipment").Include("eqFamily")
                                            .Include("eqEquipment.manufacturer").Where(x => x.eqFamilyID == id);

            eqFamilyMemQuery = eqFamilyMemQuery.OrderBy(x => x.eqEquipment.description);

            switch (sOrderBy)
            {
                case "equipmentAsc":
                    eqFamilyMemQuery = eqFamilyMemQuery.OrderBy(x => x.eqEquipment.description);
                    break;
                case "equipmentDesc":
                    eqFamilyMemQuery = eqFamilyMemQuery.OrderByDescending(x => x.eqEquipment.description);
                    break;
                default:
                    eqFamilyMemQuery = eqFamilyMemQuery.OrderBy(x => x.eqEquipment.description);
                    break;
            }

            familyMembershipList = eqFamilyMemQuery.ToPagedList(pN, pS);

        }

        private void ExecuteSingleFamilyMembershipQuery(ngmdEntities db, int id)
        {
            famMembership = db.eqFamilyMembership.Where(x => x.eqFamilyMembershipID == id).FirstOrDefault();

            if (famMembership == null)
            {
                famMembership = new eqFamilyMembership();
            }
        }

        private void ExecuteSelectListQuery_Fams()
        {
            AllFamilies = SelectListViewModel.AllEquipFamilies();
        }

        private void ExecuteSelectListQuery_Equips()
        {
            AllEquips = SelectListViewModel.AllEquipment();
        }

        private void ExecuteSelectListQuery_EquipManus()
        {
            AllEquipManufacturers = SelectListViewModel.AllEquipManufacturers();
        }

        private void ExecuteSelectListQuery_ProdTypes()
        {
            AllProductTypes = SelectListViewModel.AllProductTypes();
        }

        private void ExecuteSelectListQuery_Products()
        {
            AllProductsPartNoDesc = SelectListViewModel.AllProductsPartNoDesc();
        }

        private void ExecuteSelectListQuery_Cartridge()
        {
            AllCartridgeTypes = SelectListViewModel.GetAllCartridgeTypes();
        }

        private IQueryable<SelectListItem> GetStatusNames()
        {
            IQueryable<SelectListItem> query;

            using (ngmdEntities db = new ngmdEntities())
            {
                query = db.equipmentStatus.OrderBy(x => x.Status).Select(x => new SelectListItem
                {
                    Value = x.StatusID.ToString(),
                    Text = x.Status.ToString()
                }).ToList().AsQueryable();
            }
            return query;
        }

        private IQueryable<SelectListItem> GetMetaContentTypes()
        {
            IQueryable<SelectListItem> query;

            using (ngmdEntities db = new ngmdEntities())
            {
                query = db.metaContentType.OrderBy(x => x.metaContentDescription).Select(x => new SelectListItem
                {
                    Value = x.metaContentTypeID.ToString(),
                    Text = x.metaContentDescription.ToString()
                }).ToList().AsQueryable();
            }
            return query;
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
    }
}
