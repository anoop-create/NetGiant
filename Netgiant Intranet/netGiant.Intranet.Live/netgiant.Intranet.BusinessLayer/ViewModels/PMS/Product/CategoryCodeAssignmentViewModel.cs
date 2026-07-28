using netGiant.Intranet.DataLayer;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Product
{
    public class CategoryCodeAssignmentViewModel
    {
        public List<websiteInventory> CategoryCodeAssignmentList { get; set; }
        public int CategoryCodeAssignmentListCount { get; set; }
        public product Product { get; set; }
        public websiteInventory WebsiteInventory { get; set; }
        public IQueryable<SelectListItem> WebsiteNameList { get; set; }
        public IQueryable<SelectListItem> ManufacturerNameList { get; set; }
        public IQueryable<SelectListItem> ProductStatusList { get; set; }
        public IQueryable<SelectListItem> CategoryCodeList { get; set; }
        public List<AssignedProducts> InventoryIDs { get; set; }
        public int SelectedCategory { get; set; }

        public class AssignedProducts
        {
            public int ID { get; set; }
            public bool PrimaryChecked { get; set; }
            public bool SecondaryChecked { get; set; }
        }

        public CategoryCodeAssignmentViewModel GetCategoryCodeAssignment()
        {
            return GetCategoryCodeAssignment(null, null, null, null, null, null, null, false, 1);
        }

        public CategoryCodeAssignmentViewModel GetCategoryCodeAssignment(string orderBy, string searchTerm, string searchBy, int? websiteID,
            int? ManufacturerID, int? ProductStatusID, int? CategoryID, bool useMax, int? block)
        {
            int blockSize = 50;

            if(useMax == true)
            {
                blockSize = 40000;
                block = 1;
            }

            int blockNumber = (block ?? 1);

            using (ngmdEntities db = new ngmdEntities())
            {
                IQueryable<websiteInventory> query = db.websiteInventory
                    .Include(x => x.product)
                    .Include(x => x.Website)
                    .Include(x => x.product.manufacturer)
                    .Include(x => x.product.productGroup)
                    .Include(x => x.product.productStatus)
                    .Include(x => x.product.salesAreaGroup)
                    .Include(x => x.product.dataSupplier)
                    .Include(x => x.categoryCode);

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    switch (searchBy)
                    {
                        case "productgroup":
                            query = query.Where(x => x.product.productGroup.productGroupName.ToString().Contains(searchTerm.Trim()));
                            break;
                        case "salesareagroup":
                            query = query.Where(x => x.product.salesAreaGroup.salesAreaGroupName.ToString().Contains(searchTerm.Trim().ToLower()));
                            break;
                        case "datasupplier":
                            query = query.Where(x => x.product.dataSupplier.dataSupplierName.ToLower().Contains(searchTerm.Trim().ToLower()));
                            break;
                        default:
                            break;
                    }
                }

                if (websiteID != null && websiteID > 0)
                {
                    query = query.Where(x => x.websiteFK == websiteID);
                }

                if (ManufacturerID != null && ManufacturerID > 0)
                {
                    query = query.Where(x => x.product.manufacturerFK == ManufacturerID);
                }

                if (ProductStatusID != null && ProductStatusID > 0)
                {
                    query = query.Where(x => x.product.productStatusFK == ProductStatusID);
                }

                if (CategoryID != null && CategoryID > 0)
                {
                    query = query.Where(x => x.categoryCodeFK == CategoryID);
                }

                switch (orderBy)
                {
                    case "nameAsc":
                        query = query.OrderBy(x => x.product.productName);
                        break;
                    case "nameDesc":
                        query = query.OrderByDescending(x => x.product.productName);
                        break;
                    case "categoryAsc":
                        query = query.OrderBy(x => x.categoryCode.categoryCodeName);
                        break;
                    case "categoryDesc":
                        query = query.OrderByDescending(x => x.categoryCode.categoryCodeName);
                        break;
                    case "partnoAsc":
                        query = query.OrderBy(x => x.product.partNo);
                        break;
                    case "partnoDesc":
                        query = query.OrderByDescending(x => x.product.partNo);
                        break;
                    case "websiteAsc":
                        query = query.OrderBy(x => x.websiteFK);
                        break;
                    case "websiteDesc":
                        query = query.OrderByDescending(x => x.websiteFK);
                        break;
                    case "manufacturerAsc":
                        query = query.OrderBy(x => x.product.manufacturer.manufacturerName);
                        break;
                    case "manufacturerDesc":
                        query = query.OrderByDescending(x => x.product.manufacturer.manufacturerName);
                        break;
                    case "statusAsc":
                        query = query.OrderBy(x => x.product.productStatus.productStatusName);
                        break;
                    case "statusDesc":
                        query = query.OrderByDescending(x => x.product.productStatus.productStatusName);
                        break;
                    case "productgroupAsc":
                        query = query.OrderBy(x => x.product.productGroup.productGroupName);
                        break;
                    case "productgroupDesc":
                        query = query.OrderByDescending(x => x.product.productGroup.productGroupName);
                        break;
                    case "salesareaAsc":
                        query = query.OrderBy(x => x.product.salesAreaGroup.salesAreaGroupName);
                        break;
                    case "salesareaDesc":
                        query = query.OrderByDescending(x => x.product.salesAreaGroup.salesAreaGroupName);
                        break;
                    case "datasupplierAsc":
                        query = query.OrderBy(x => x.product.dataSupplier.dataSupplierName);
                        break;
                    case "datasupplierDesc":
                        query = query.OrderByDescending(x => x.product.dataSupplier.dataSupplierName);
                        break;
                    default:
                        query = query.OrderBy(x => x.product.productID);
                        break;
                }

                CategoryCodeAssignmentListCount = query.Count();
                CategoryCodeAssignmentList = query
                    .Skip((blockNumber - 1) * blockSize)
                    .Take(blockSize)
                    .ToList();
                WebsiteNameList = GetWebsiteNames();
                ManufacturerNameList = GetManufacturerNames();
                ProductStatusList = GetProductStatus();
                CategoryCodeList = GetCategoryCodes(websiteID);

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

        private IQueryable<SelectListItem> GetProductStatus()
        {
            IQueryable<SelectListItem> query;

            using (ngmdEntities db = new ngmdEntities())
            {
                query = db.productStatus.OrderBy(x => x.productStatusName).Select(x => new SelectListItem
                {
                    Value = x.productStatusID.ToString(),
                    Text = x.productStatusName.ToString()
                }).ToList().AsQueryable();
            }
            return query;
        }

        private IQueryable<SelectListItem> GetCategoryCodes(int? websiteID)
        {
            IQueryable<SelectListItem> query;

            using (ngmdEntities db = new ngmdEntities())
            {
                query = db.categoryCode.OrderBy(x => x.categoryCodeName).Where(x => x.websiteFK == websiteID).Select(x => new SelectListItem
                {
                    Value = x.categoryCodeID.ToString(),
                    Text = x.categoryCodeName.ToString()
                }).ToList().AsQueryable();
            }
            return query;
        }

        public void Save()
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    foreach (AssignedProducts inventory in InventoryIDs) 
                    {
                        if (inventory.PrimaryChecked == true)
                        {
                            websiteInventory wi = db.websiteInventory.Where(x => x.websiteInventoryID == inventory.ID).FirstOrDefault();
                            wi.categoryCodeFK = SelectedCategory;
                            db.Entry(wi).State = EntityState.Modified;
                        }
                        if (inventory.SecondaryChecked == true)
                        {
                            secondaryCategoryLookup scl = new secondaryCategoryLookup 
                            { 
                                websiteInventoryFK = inventory.ID, 
                                categoryCodeFK = SelectedCategory
                            };

                            db.Entry(scl).State = EntityState.Added;
                        }
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
