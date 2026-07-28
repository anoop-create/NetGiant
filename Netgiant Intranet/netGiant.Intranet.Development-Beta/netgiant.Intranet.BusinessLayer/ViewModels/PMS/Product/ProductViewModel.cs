using System;
using System.Collections.Generic;
using System.Linq;
using netGiant.Intranet.DataLayer;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using PagedList;
using System.Data.Entity;
using System.Web.Mvc;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using System.ComponentModel;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Product
{
    public enum CRUD
    {
        [Description("C")]
        Create,
        [Description("R")]
        Read,
        [Description("U")]
        Update,
        [Description("D")]
        Delete
    }

    public enum QueueType
    {
        Full,
        Partial
    }

    public class ProductViewModel : HelperViewModel
    {
        public ProductViewModel()
        {
            AllManufacturers = SelectListViewModel.GetAllManufacturers();
            AllProductStatuses = SelectListViewModel.GetNgmdLookupSelectList("ProductStatus");
            AllProductGroups = SelectListViewModel.GetAllProductGroups();
            AllSalesAreaGroups = SelectListViewModel.GetAllSalesAreaGroups();
            AllDataSuppliers = SelectListViewModel.GetAllDataSuppliers();

            Products = null;
            _ctx = new ngmdEntities();
        }

        private ngmdEntities _ctx;
        public List<product> Products { get; set; }
        public int ProductsCount { get; set; }
        public List<productPrice> productPrices { get; set; }
        public product prod { get; set; }
        public List<SelectListItem> AllManufacturers { get; set; }
        public IQueryable<SelectListItem> AllProductStatuses { get; set; }
        public IQueryable<SelectListItem> AllProductGroups { get; set; }
        public IQueryable<SelectListItem> AllSalesAreaGroups { get; set; }
        public IQueryable<SelectListItem> AllDataSuppliers { get; set; }
        public IQueryable<SelectListItem> AllEbusinessGroups { get; set; }
        public IQueryable<SelectListItem> AllCategoryCodes { get; set; }
        public IQueryable<Website> AllWebsites { get; set; }

        //Create Product Specific Properties
        public IQueryable<SelectListItem> AllAttribute1 { get; set; }
        public IQueryable<SelectListItem> AllAttribute2 { get; set; }
        public IQueryable<SelectListItem> AllAttribute3 { get; set; }
        public IQueryable<SelectListItem> AllAttribute4 { get; set; }
        public IQueryable<SelectListItem> AllAttribute5 { get; set; }
        public IQueryable<SelectListItem> AllAttribute6 { get; set; }
        public IQueryable<SelectListItem> AllAttribute7 { get; set; }
        public IQueryable<SelectListItem> AllAttribute8 { get; set; }
        public IQueryable<SelectListItem> AllAttribute9 { get; set; }
        public IQueryable<SelectListItem> AllAttribute10 { get; set; }
        public IQueryable<SelectListItem> AllProductItemType { get; set; }
        public IQueryable<SelectListItem> AllStockItems { get; set; }
        public bool ChangedWebsiteInv { get; set; }

        public int SelectedManufacturerID { get; set; }
        public int SelectedProductGroupID { get; set; }
        public int SelectedProductStatusID { get; set; }
        public int SelectedSalesAreaGroupID { get; set; }
        public int SelectedProductItemTypeID { get; set; }
        public List<Website> SelectedWebsites { get; set; }
        public int[] SelectedWebsiteIDs { get; set; }
        public int[] SelectedCatCodeIDs { get; set; }
        public int[] ImagesNotRequired { get; set; }

        public IPagedList<eqProductMembership> ProductMembership { get; set; }
        public IQueryable<TelerikProduct> ProductList { get; set; }

        public productImage ProductImage { get; set; }

        public ProductViewModel GetProducts()
        {
            ProductList = _ctx.product
                .Select(x => new TelerikProduct
                {
                    ProductId = x.productID,
                    ProductName = x.productName,
                    PartNo = x.partNo,
                    Manufacturer = x.manufacturer.manufacturerName,
                    ItemType = (_ctx.Lookup
                        .Where(y => y.LookupType.LookupTypeName == "ProductItemType" && y.AltLookupId == x.productItemTypeFK)
                        .AsQueryable()
                        .FirstOrDefault()
                        .LookupName),
                    ProductStatus = (_ctx.Lookup
                        .Where(y => y.LookupType.LookupTypeName == "ProductStatus" && y.AltLookupId == x.productStatusFK)
                        .AsQueryable()
                        .FirstOrDefault()
                        .LookupName),
                    ProductGroup = x.productGroup.productGroupName,
                    SalesAreaGroup = x.salesAreaGroup.salesAreaGroupName,
                    Stock = x.supplierStock ?? 0,
                    TGPrice = x.websiteInventory
                                .Where(w => w.websiteFK == 1)
                                .AsQueryable()
                                .FirstOrDefault()
                                .productPrice.AsQueryable()
                                .OrderByDescending(m => m.productPriceID)
                                .FirstOrDefault().price,
                    CMPrice = x.websiteInventory
                                .Where(w => w.websiteFK == 2)
                                .AsQueryable()
                                .FirstOrDefault()
                                .productPrice.AsQueryable()
                                .OrderByDescending(m => m.productPriceID)
                                .FirstOrDefault().price,
                    NGPrice = x.websiteInventory
                                .Where(w => w.websiteFK == 3)
                                .AsQueryable()
                                .FirstOrDefault()
                                .productPrice.AsQueryable()
                                .OrderByDescending(m => m.productPriceID)
                                .FirstOrDefault().price,
                    DataSupplier = x.dataSupplier.dataSupplierName,
                    SupplierLastUpdate = x.supplierLastUpdate,
                    SkuCount = x.skuMapping
                    .Where(y => y.productFK == x.productID)
                    .Where(y => y.providerInventory != null).Count(),
                    SecondaryCrossSellGroup = x.secondaryCrossSellGroupIdent ?? 0
                })
                .AsQueryable();

            return this;
        }

        public class TelerikProduct
        {
            public int ProductId { get; set; }
            public string ProductName { get; set; }
            public string PartNo { get; set; }
            public string Manufacturer { get; set; }
            public string ItemType { get; set; }
            public string ProductStatus { get; set; }
            public string ProductGroup { get; set; }
            public string SalesAreaGroup { get; set; }
            public int? Stock { get; set; }
            public double? TGPrice { get; set; }
            public double? CMPrice { get; set; }
            public double? NGPrice { get; set; }
            public string DataSupplier { get; set; }
            public DateTime? SupplierLastUpdate { get; set; }
            public int SkuCount { get; set; }
            public int? SecondaryCrossSellGroup { get; set; }
        }

        public ProductViewModel Get()
        {
            return Get(null, "", "", "");
        }

        public ProductViewModel Get(int? block, string search, string searchBy, string orderBy)
        {
            int blockSize = 50;
            int blockNumber = (block ?? 1);

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    IQueryable<product> list = db.product
                        .Include(p => p.productGroup)
                        .Include(p => p.salesAreaGroup)
                        .Include(p => p.manufacturer)
                        .Include(p => p.websiteInventory)
                        .Include("websiteInventory.productPrice")
                        .Include(p => p.dataSupplier);

                    if (SelectedManufacturerID > 0)
                        list = list.Where(x => x.manufacturerFK == SelectedManufacturerID);

                    if (SelectedProductGroupID > 0)
                        list = list.Where(x => x.productGroupFK == SelectedProductGroupID);

                    if (SelectedProductStatusID > 0)
                        list = list.Where(x => x.productStatusFK == SelectedProductStatusID);

                    if (SelectedSalesAreaGroupID > 0)
                        list = list.Where(x => x.salesAreaGroupFK == SelectedSalesAreaGroupID);

                    if (SelectedProductItemTypeID > 0)
                        list = list.Where(x => x.productItemTypeFK == SelectedProductItemTypeID);

                    if (!string.IsNullOrEmpty(search))
                    {
                        search = search.ToLower().Trim();

                        switch (searchBy)
                        {
                            case "name":
                                list = list.Where(x => x.productName.ToLower().Contains(search));
                                break;
                            case "partNo":
                                list = list.Where(x => x.partNo.ToLower().Contains(search));
                                break;
                            case "manufacturer":
                                list = list.Where(x => x.manufacturer.manufacturerName.ToLower().Contains(search));
                                break;
                            case "productGroup":
                                list = list.Where(x => x.productGroup.productGroupName.ToLower().Contains(search));
                                break;
                            case "salesAreaGroup":
                                list = list.Where(x => x.salesAreaGroup.salesAreaGroupName.ToLower().Contains(search));
                                break;
                            default:
                                break;
                        }
                    }

                    //Sorting
                    switch (orderBy)
                    {
                        case "productNameAsc":
                            list = list.OrderBy(x => x.productName);
                            break;
                        case "productNameDesc":
                            list = list.OrderByDescending(x => x.productName);
                            break;
                        case "partNoAsc":
                            list = list.OrderBy(x => x.partNo);
                            break;
                        case "partNoDesc":
                            list = list.OrderByDescending(x => x.partNo);
                            break;
                        case "manufacturerAsc":
                            list = list.OrderBy(x => x.manufacturer.manufacturerName);
                            break;
                        case "manufacturerDesc":
                            list = list.OrderByDescending(x => x.manufacturer.manufacturerName);
                            break;
                        case "productGroupAsc":
                            list = list.OrderBy(x => x.productGroup.productGroupName);
                            break;
                        case "productGroupDesc":
                            list = list.OrderByDescending(x => x.productGroup.productGroupName);
                            break;
                        case "salesGroupAsc":
                            list = list.OrderBy(x => x.salesAreaGroup.salesAreaGroupName);
                            break;
                        case "salesGroupDesc":
                            list = list.OrderByDescending(x => x.salesAreaGroup.salesAreaGroupName);
                            break;
                        case "dataSupplierAsc":
                            list = list.OrderBy(x => x.dataSupplier.dataSupplierName);
                            break;
                        case "dataSupplierDesc":
                            list = list.OrderByDescending(x => x.dataSupplier.dataSupplierName);
                            break;
                        case "supplierLastUpdateAsc":
                            list = list.OrderBy(x => x.supplierLastUpdate);
                            break;
                        case "supplierLastUpdateDesc":
                            list = list.OrderByDescending(x => x.supplierLastUpdate);
                            break;
                        case "stockAsc":
                            list = list.OrderBy(x => x.supplierStock);
                            break;
                        case "stockDesc":
                            list = list.OrderByDescending(x => x.supplierStock);
                            break;
                        case "priceTGAsc":
                            list = list.OrderBy(x => x.websiteInventory.Where(y => y.websiteFK == 1).FirstOrDefault() != null ?
                                x.websiteInventory.Where(y => y.websiteFK == 1).FirstOrDefault().productPrice.OrderBy(p => p.price).FirstOrDefault().price : 0);
                            break;
                        case "priceTGDesc":
                            list = list.OrderByDescending(x => x.websiteInventory.Where(y => y.websiteFK == 1).FirstOrDefault() != null ?
                                x.websiteInventory.Where(y => y.websiteFK == 1).FirstOrDefault().productPrice.OrderByDescending(p => p.price).FirstOrDefault().price : 0);
                            break;
                        case "priceCMAsc":
                            list = list.OrderBy(x => x.websiteInventory.Where(y => y.websiteFK == 2).FirstOrDefault() != null ?
                                x.websiteInventory.Where(y => y.websiteFK == 1).FirstOrDefault().productPrice.OrderBy(p => p.price).FirstOrDefault().price : 0);
                            break;
                        case "priceCMDesc":
                            list = list.OrderByDescending(x => x.websiteInventory.Where(y => y.websiteFK == 2).FirstOrDefault() != null ?
                                x.websiteInventory.Where(y => y.websiteFK == 1).FirstOrDefault().productPrice.OrderByDescending(p => p.price).FirstOrDefault().price : 0);
                            break;
                        case "priceNGAsc":
                            list = list.OrderBy(x => x.websiteInventory.Where(y => y.websiteFK == 3).FirstOrDefault() != null ?
                                x.websiteInventory.Where(y => y.websiteFK == 1).FirstOrDefault().productPrice.OrderBy(p => p.price).FirstOrDefault().price : 0);
                            break;
                        case "priceNGDesc":
                            list = list.OrderByDescending(x => x.websiteInventory.Where(y => y.websiteFK == 3).FirstOrDefault() != null ?
                                x.websiteInventory.Where(y => y.websiteFK == 1).FirstOrDefault().productPrice.OrderByDescending(p => p.price).FirstOrDefault().price : 0);
                            break;
                        default:
                            list = list.OrderBy(x => x.productName);
                            break;
                    }

                    NoLockInterceptor.ApplyNoLock = true;

                    ProductsCount = list.Count();
                    Products = list.Skip((blockNumber - 1) * blockSize).Take(blockSize).ToList();
                    AllProductItemType = SelectListViewModel.GetNgmdLookupSelectList("ProductItemType");
                }
            }
            catch (Exception e)
            {
                throw new ApplicationException(e.Message, e.InnerException);
            }

            return this;
        }

        public bool DeleteProductImage(int productImageID)
        {
            bool success = true;

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    productImage productImage = db.productImage.Find(productImageID);
                    db.productImage.Remove(productImage);
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

        public static ProductViewModel Create(int id)
        {
            ProductViewModel model = new ProductViewModel();
            model.SelectedWebsiteIDs = new int[0];
            model.SelectedWebsites = new List<Website>();

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    if (id == 0)
                    {
                        model.prod = new product();
                        model.prod.AxisFields = new AxisFields();

                        //SetupAxisAdditional(model, db);
                    }
                    else
                    {
                        model.prod = db.product
                            .Include("websiteInventory.categoryCode")
                                                    .Include("websiteInventory.website")
                                                    .Include("websiteInventory.secondaryCategoryLookup")
                                                    .Include("websiteInventory.secondaryCategoryLookup.categoryCode")
                                                    .Include("websiteInventory.productImage")
                                                    .Include("manufacturer")
                                                    .Include("AxisFields.AxisFieldsAdditional")
                                                    .Include("AxisEbusinessMapping.AxisEbusiness")
                                                    .Include("assemblyComponent.product1")
                                                    .Where(x => x.productID == id).First();

                        foreach (websiteInventory webInventory in db.websiteInventory.Where(x => x.productFK == model.prod.productID))
                        {
                            model.SelectedWebsites.Add(db.Website.FirstOrDefault(x => x.WebsiteID == webInventory.websiteFK));
                        }

                        if (model.prod.AxisFields == null)
                        {
                            model.prod.AxisFields = new AxisFields();

                            //SetupAxisAdditional(model, db);
                        }

                        model.ProductMembership = db.eqProductMembership
                        .Include(x => x.eqEquipment)
                        .Include(x => x.product)
                        .Include("eqEquipment.manufacturer")
                        .Where(x => x.productFK == id)
                        .OrderBy(x => x.eqProductMembershipID)
                        .ToPagedList(1, 10);
                    }

                    if (model.prod.AxisFields.AxisFieldsAdditional.Count == 0)
                        SetupAxisAdditional(model, db);

                    model.AllWebsites = db.Website.ToList().AsQueryable();
                    model.AllAttribute1 = SelectListViewModel.GetAttributeDescs(1, 1);
                    model.AllAttribute2 = SelectListViewModel.GetAttributeDescs(1, 2);
                    model.AllAttribute3 = SelectListViewModel.GetAttributeDescs(1, 3);
                    model.AllAttribute4 = SelectListViewModel.GetAttributeDescs(1, 4);
                    model.AllAttribute5 = SelectListViewModel.GetAttributeDescs(1, 5);
                    model.AllAttribute6 = SelectListViewModel.GetAttributeDescs(1, 6);
                    model.AllAttribute7 = SelectListViewModel.GetAttributeDescs(1, 7);
                    model.AllAttribute8 = SelectListViewModel.GetAttributeDescs(1, 8);
                    model.AllAttribute9 = SelectListViewModel.GetAttributeDescs(1, 9);
                    model.AllAttribute10 = SelectListViewModel.GetAttributeDescs(1, 10);
                    model.AllEbusinessGroups = SelectListViewModel.GetAllEbusinessGroups();
                    model.AllCategoryCodes = SelectListViewModel.GetAllCategoryCodes();
                    model.AllProductItemType = SelectListViewModel.GetNgmdLookupSelectList("ProductItemType");
                    model.AllStockItems = SelectListViewModel.GetAllStockItems();
                }
            }

            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }

            return model;
        }

        public string GetFullPathOfWebInventoryImages(websiteInventory websiteInventory)
        {
            //var strImageDomainRoot = SharedFunctions.GetConfigurationSetting("Website Application Variables", "CDN", websiteInventory.websiteFK);
            //var strVersionNumber = SharedFunctions.GetConfigurationSetting("Website Application Variables", "VersionNumber", websiteInventory.websiteFK);

            //strImageDomainRoot = strImageDomainRoot.Replace("[version]", strVersionNumber);

            //return strImageDomainRoot;

            switch (websiteInventory.websiteFK)
            {
                case 1:
                    {
                        return "../../../TGImages";
                    }
                case 2:
                    {
                        return "../../../CMImages";
                    }
            }

            return "../../../NGImages";
        }

        private void UpdateProductImage(productImage productImage)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                db.Entry(productImage).State = EntityState.Modified;
                db.SaveChanges();
            }
        }

        private void AddNewProductImage(productImage productImage)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                db.Entry(productImage).State = EntityState.Added;
                db.SaveChanges();
            }
        }

        public void SaveProductImage(productImage productImage)
        {
            try
            {
                if (productImage.productImageID > 0)
                {
                    UpdateProductImage(productImage);
                }
                else
                {
                    AddNewProductImage(productImage);
                }
            }
            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }
        }

        public void CreateProductImage(int webInvId, int id)
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    if (id == 0)
                    {
                        ProductImage = new productImage() { websiteInventoryFK = webInvId };
                    }
                    else
                    {
                        ProductImage = db.productImage
                            .Include(x => x.websiteInventory)
                            .Where(x => x.productImageID == id).First();
                    }
                }
            }
            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }
        }

        public void SetProviderUntrusted(int? providerInventoryID, bool untrusted)
        {
            if (providerInventoryID == null)
                return;

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    providerInventory pi = db.providerInventory.Find(providerInventoryID);
                    pi.untrustedProvider = untrusted;
                    pi.untrustedAuto = false;
                    db.Entry(pi).State = EntityState.Modified;
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }
        }

        private static void SetupAxisAdditional(ProductViewModel model, ngmdEntities db)
        {
            List<websiteInventory> webInvList = db.websiteInventory.Where(x => x.productFK == model.prod.productID).ToList();

            foreach (websiteInventory ws in webInvList)
            {
                model.prod.AxisFields.AxisFieldsAdditional.Add(new AxisFieldsAdditional()
                {
                    websiteFK = ws.websiteFK,
                    breakQuantity1 = 1,
                    breakQuantity2 = 2,
                    breakQuantity3 = 999999999
                });
            }
        }

        public void Save()
        {
            if (prod.productItemTypeFK == 1)
            {
                prod.AxisFields.stockRecordType = "Stock Item";
            }
            else if (prod.productItemTypeFK == 2)
            {
                prod.AxisFields.stockRecordType = "Assembly";
            }
            else if (prod.productItemTypeFK == 3)
            {
                prod.AxisFields.stockRecordType = "Manufacturer Assembly";
            }

            if (prod.productID > 0)
            {
                UpdateProduct();
            }
            else
            {
                AddNewProduct();
            }
        }

        public static AxisEbusinessMapping CreateNewEbusinessMapping(string eBusRef, int productFK)
        {
            var aEbus = new AxisEbusinessMapping();

            using (ngmdEntities db = new ngmdEntities())
            {
                aEbus.AxisEbusiness = db.AxisEbusiness.Find(eBusRef);
                aEbus.eBusinessRef = eBusRef;
                aEbus.productFK = productFK;
            }

            return aEbus;
        }

        public static secondaryCategoryLookup CreateNewSecondaryCategoryLookup(int categoryCodeId, int websiteInventoryId)
        {
            var secCatLkp = new secondaryCategoryLookup();

            using (ngmdEntities db = new ngmdEntities())
            {
                secCatLkp.categoryCode = db.categoryCode.Find(categoryCodeId);
                secCatLkp.websiteInventory = db.websiteInventory.Find(websiteInventoryId);
                secCatLkp.websiteInventoryFK = websiteInventoryId;
                secCatLkp.categoryCodeFK = categoryCodeId;
            }

            return secCatLkp;
        }

        public static assemblyComponent CreateNewAssemblyComponent(int productFK, int componentFK)
        {
            var assCom = new assemblyComponent();

            using (ngmdEntities db = new ngmdEntities())
            {
                assCom.assemblyProductFK = productFK;
                assCom.assemblyComponentFK = componentFK;
                assCom.product = db.product.Find(productFK);
                assCom.product1 = db.product.Find(componentFK);
                assCom.quantity = 1;
            }

            return assCom;
        }

        private product GetExistingProduct()
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                return db.product.Include("AxisFields.AxisFieldsAdditional")
                    .Include("AxisEbusinessMapping.AxisEbusiness")
                    .Include("websiteInventory.secondaryCategoryLookup")
                    .Include("websiteInventory.secondaryCategoryLookup.categoryCode")
                    .Include("websiteInventory.productImage")
                    .Include(x => x.assemblyComponent)
                    .Include(x => x.websiteInventory)
                    .Where(x => x.productID == prod.productID)
                    .FirstOrDefault();
            }
        }

        private void AddNewProduct()
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                prod.dateLastUpdate = DateTime.Now;
                prod.dateCreated = DateTime.Now;
                db.Entry(prod).State = EntityState.Added;
                db.SaveChanges();

                foreach (AxisFieldsAdditional afa in prod.AxisFields.AxisFieldsAdditional)
                {
                    if (afa != null)
                    {
                        db.Entry(afa).State = EntityState.Added;
                        db.SaveChanges();
                    }
                }

                bool isNew = true;
                UpdateEbusinessGroups(isNew, prod, null, db, 0);
                UpdateWebsiteInventory(isNew);

                AXISQueue aq = CreateAxisQueueEntry(db);
                CreateQueueDetailsEntry("All", "All", aq.AXISQueueID, CRUD.Create);
            }
        }

        private void UpdateProduct()
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                product existingProduct = GetExistingProduct();
                prod.dateLastUpdate = DateTime.Now;

                //Axis Fields Additional Table
                foreach (AxisFieldsAdditional afa in prod.AxisFields.AxisFieldsAdditional)
                {
                    if (afa.googleFeedInclude == false)
                        afa.googleFeedInclude = null;
                    if (afa.bespokeFeedInclude == false)
                        afa.bespokeFeedInclude = null;

                    if (afa.productFK == 0)
                        afa.productFK = prod.productID;

                    if (afa.breakQuantity1 == null || afa.breakQuantity1 == 0) afa.breakQuantity1 = 1;
                    if (afa.breakQuantity2== null || afa.breakQuantity2 == 0) afa.breakQuantity2 = 2;
                    if (afa.breakQuantity3 == null || afa.breakQuantity3 == 0) afa.breakQuantity3 = 999999999;

                    if (afa.axisFieldsAdditionalID > 0)
                    {
                        db.Entry(afa).State = EntityState.Modified;
                    }
                    else
                    {
                        db.Entry(afa).State = EntityState.Added;
                    }
                }

                //Axis Fields Table
                if (prod.AxisFields.productFK == 0)
                {
                    prod.AxisFields.productFK = prod.productID;
                    db.Entry(prod.AxisFields).State = EntityState.Added;
                }
                else
                {
                    db.Entry(prod.AxisFields).State = EntityState.Modified;
                }

                db.SaveChanges();

                AXISQueue aq = CreateAxisQueueEntry(db);

                bool isNew = false;
                UpdateEbusinessGroups(isNew, prod, existingProduct, db, aq.AXISQueueID);
                UpdateAssemblyComponents(isNew, prod, existingProduct, db, aq.AXISQueueID);

                if (prod.websiteInventory != null)
                    UpdateSecondaryCategoryLookup(isNew, prod, existingProduct, db, aq.AXISQueueID);

                db.Entry(prod).State = EntityState.Modified;
                db.SaveChanges();

                UpdateWebsiteInventory(isNew);

                //If changing from a 'No Status' to 'Active' or 'Active Unpublished'
                if (existingProduct.productStatusFK == 4 && (prod.productStatusFK == 1 || prod.productStatusFK == 9))
                {
                    CreateQueueDetailsEntry("All", "All", aq.AXISQueueID, CRUD.Create);
                }
                else
                {
                    CompareProduct(prod, existingProduct, aq);
                    CompareAxisFields(prod, existingProduct, aq);
                    CompareAxisFieldsAdditional(prod, existingProduct, aq);
                }
            }
        }

        private void UpdateAssemblyComponents(bool isNew, product prod, product existingProd, ngmdEntities db, int AXISQueueID)
        {
            if (isNew)
            {
                foreach (var item in prod.assemblyComponent)
                {
                    db.Entry(item).State = EntityState.Added;
                }

                db.SaveChanges();
            }
            else
            {
                bool createQueueEntry = false;

                if (prod.productItemTypeFK > 1)
                {
                    List<int> nonSelectedComponents = existingProd.assemblyComponent.Select(x => x.assemblyComponentID)
                        .Except(prod.assemblyComponent.Select(x => x.assemblyComponentID)).ToList();

                    for (var i = 0; i < nonSelectedComponents.Count(); i++)
                    {
                        db.assemblyComponent.Remove(db.assemblyComponent.Find(nonSelectedComponents[i]));
                        createQueueEntry = true;
                    }

                    foreach (var item in prod.assemblyComponent)
                    {
                        if (item.assemblyProductFK > 0)
                        {
                            if (item.assemblyComponentID > 0)
                            {
                                db.Entry(item).State = EntityState.Modified;
                            }
                            else
                            {
                                db.Entry(item).State = EntityState.Added;
                                createQueueEntry = true;
                            }
                        }
                    }
                }
                else
                {
                    foreach (var comp in prod.assemblyComponent)
                    {
                        db.Entry(comp).State = EntityState.Deleted;
                        createQueueEntry = true;
                    }
                }

                if (createQueueEntry)
                    CreateQueueDetailsEntry("product", "components", AXISQueueID, CRUD.Update);

                db.SaveChanges();
            }
        }

        private void UpdateEbusinessGroups(bool isNew, product prod, product existingProd, ngmdEntities db, int AXISQueueID)
        {
            if (isNew)
            {
                foreach (var item in prod.AxisEbusinessMapping)
                {
                    db.Entry(item).State = EntityState.Added;
                }

                db.SaveChanges();
            }
            else
            {
                bool createQueueEntry = false;

                List<int> nonSelectedRefs = existingProd.AxisEbusinessMapping.Select(x => x.eBusinessMappingID)
                    .Except(prod.AxisEbusinessMapping.Select(x => x.eBusinessMappingID)).ToList();

                for (var i = 0; i < nonSelectedRefs.Count(); i++)
                {
                    db.AxisEbusinessMapping.Remove(db.AxisEbusinessMapping.Find(nonSelectedRefs[i]));
                    createQueueEntry = true;
                }

                foreach (var item in prod.AxisEbusinessMapping)
                {
                    if (item.productFK > 0)
                    {
                        if (item.eBusinessMappingID > 0)
                        {
                            db.Entry(item).State = EntityState.Modified;
                        }
                        else
                        {
                            db.Entry(item).State = EntityState.Added;
                            createQueueEntry = true;
                        }
                    }
                }

                db.SaveChanges();

                if (createQueueEntry)
                    CreateQueueDetailsEntry("product", "eBusiness", AXISQueueID, CRUD.Update);
            }
        }

        private AXISQueue CreateAxisQueueEntry(ngmdEntities db)
        {
            AXISQueue aq = db.AXISQueue.Where(x => x.productFK == prod.productID).FirstOrDefault();

            if (aq == null)
            {
                aq = new AXISQueue()
                {
                    productFK = prod.productID,
                    dateLastUpdated = DateTime.Now
                };

                db.AXISQueue.Add(aq);
                db.SaveChanges();
            }

            return aq;
        }

        private void UpdateSecondaryCategoryLookup(bool isNew, product prod, product existingProd, ngmdEntities db, int AXISQueueID)
        {
            if (isNew)
            {
                foreach (var wi in prod.websiteInventory)
                {
                    foreach (var scc in wi.secondaryCategoryLookup)
                    {
                        db.Entry(scc).State = EntityState.Added;
                    }
                }

                db.SaveChanges();
            }
            else
            {
                foreach (var wi in existingProd.websiteInventory)
                {
                    websiteInventory inv = prod.websiteInventory.Where(x => x.websiteFK == wi.websiteFK).FirstOrDefault();
                    List<int> nonSelectedRefs = new List<int>();

                    if (inv != null)
                    {
                        nonSelectedRefs = wi.secondaryCategoryLookup.Select(x => x.secondaryCategoryLookupID)
                            .Except(inv.secondaryCategoryLookup.Select(x => x.secondaryCategoryLookupID)).ToList();
                    }

                    for (var i = 0; i < nonSelectedRefs.Count(); i++)
                    {
                        db.secondaryCategoryLookup.Remove(db.secondaryCategoryLookup.Find(nonSelectedRefs[i]));
                    }
                }

                foreach (var wi in prod.websiteInventory)
                {
                    foreach (var item in wi.secondaryCategoryLookup)
                    {
                        if (item.websiteInventoryFK > 0)
                        {
                            if (item.secondaryCategoryLookupID > 0)
                            {
                                db.Entry(item).State = EntityState.Modified;
                            }
                            else
                            {
                                db.Entry(item).State = EntityState.Added;
                            }
                        }
                    }
                }

                db.SaveChanges();
            }
        }

        private void UpdateWebsiteInventory(bool isNew)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                List<categoryCode> catCodesList = new List<categoryCode>();

                if (SelectedCatCodeIDs != null)
                {
                    foreach (int cc in SelectedCatCodeIDs)
                    {
                        if (cc != 0) catCodesList.Add(db.categoryCode.Find(cc));
                    }
                }

                if (null != SelectedWebsiteIDs)
                {
                    //create web inventory if doesn't exist and based on the website selection
                    for (int i = 0; i < SelectedWebsiteIDs.Count(); i++)
                    {
                        int websiteID = SelectedWebsiteIDs[i];
                        if (!db.websiteInventory.Any(x => x.productFK == prod.productID && x.websiteFK == websiteID))
                        {
                            //Create new website inventory
                            websiteInventory webInventory = new websiteInventory();
                            webInventory.websiteFK = websiteID;
                            webInventory.productFK = prod.productID;
                            webInventory.categoryCodeFK = catCodesList.Where(x => x.websiteFK == websiteID).First().categoryCodeID;
                            webInventory.imageIsNotRequired = ImagesNotRequired == null ? false :
                            ImagesNotRequired.Contains(websiteID) ? true : false;
                            webInventory.dateLastUpdate = DateTime.Now;
                            db.websiteInventory.Add(webInventory);

                            var existingadd = db.AxisFieldsAdditional.FirstOrDefault(w => w.productFK == prod.productID && w.websiteFK == websiteID);

                            if (existingadd == null)
                            {
                                AxisFieldsAdditional afadd = new AxisFieldsAdditional();
                                afadd.productFK = prod.productID;
                                afadd.websiteFK = websiteID;
                                db.AxisFieldsAdditional.Add(afadd);
                            }

                            db.SaveChanges();

                            if (isNew == false)
                                CreateWebInventoryAXISQueue(webInventory, GetEnumDescription(CRUD.Update), "productFK");
                        }
                        else
                        {
                            websiteInventory wi = db.websiteInventory.Where(x => x.productFK == prod.productID && x.websiteFK == websiteID).First();
                            wi.categoryCodeFK = catCodesList.Where(x => x.websiteFK == websiteID).First().categoryCodeID;
                            wi.imageIsNotRequired = ImagesNotRequired == null ? false : 
                                ImagesNotRequired.Contains(websiteID) ? true : false;
                            db.Entry(wi).State = EntityState.Modified;
                            db.SaveChanges();
                        }
                    }

                    //Remove the web inventory for the non selected websites
                    int[] nonSelectedWebsiteIDs = db.Website.Select(x => x.WebsiteID).Except(SelectedWebsiteIDs).ToArray();

                    for (int i = 0; i < nonSelectedWebsiteIDs.Count(); i++)
                    {
                        int nonSelectedWebsiteID = nonSelectedWebsiteIDs[i];
                        if (db.websiteInventory.Any(x => x.productFK == prod.productID && x.websiteFK == nonSelectedWebsiteID))
                        {
                            db.Configuration.ProxyCreationEnabled = false;
                            websiteInventory wi = db.websiteInventory.FirstOrDefault(x => x.productFK == prod.productID && x.websiteFK == nonSelectedWebsiteID);
                            db.websiteInventory.Remove(wi);
                            CreateWebInventoryAXISQueue(wi, GetEnumDescription(CRUD.Delete), "productFK");

                            db.SaveChanges();
                        }

                        if (db.AxisFieldsAdditional.Any(x => x.productFK == prod.productID && x.websiteFK == nonSelectedWebsiteID))
                        {
                            AxisFieldsAdditional afadd = db.AxisFieldsAdditional.FirstOrDefault(x => x.productFK == prod.productID && x.websiteFK == nonSelectedWebsiteID);
                            db.AxisFieldsAdditional.Remove(afadd);
                            db.SaveChanges();
                        }
                    }
                }
                //remove all website inventory mappings if any of the website is not selected
                else
                {
                    foreach (websiteInventory wi in db.websiteInventory.Where(x => x.productFK == prod.productID))
                    {
                        db.websiteInventory.Remove(wi);
                        //CreateWebInventoryAXISQueue(wi, GetEnumDescription(CRUD.Delete), "productFK");
                    }
                    db.SaveChanges();
                }
            }
        }

        public void Delete(int id)
        {
            //First reset the provider inventory items back to potential new
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    product prod = db.product.Find(id);
                    var pis = db.skuMapping.Include(x => x.providerInventory).Where(x => x.altRef == prod.partNo).ToList();
                    pis.ForEach(x => x.providerInventory.potentialNewProduct = true);
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }

            //Then delete the product, cascade delete works only this way
            using (ngmdEntities db = new ngmdEntities())
            {
                product prod = db.product.Find(id);

                #region AXIS Queue
                AXISQueue axisQueue = null;
                if (!db.AXISQueue.Any(x => x.productFK == prod.productID))
                {
                    axisQueue = new AXISQueue() { productFK = prod.productID, dateLastUpdated = DateTime.Now };
                    db.AXISQueue.Add(axisQueue);
                    db.SaveChanges();
                }
                else
                {
                    axisQueue = db.AXISQueue.FirstOrDefault(x => x.productFK == prod.productID);
                }

                Type entityProductType = prod.GetType();

                AXISQueueDetails queueDetails = new AXISQueueDetails()
                {
                    entityName = entityProductType.Name,
                    fieldName = "productID",
                    createdDate = DateTime.Now,
                    completedDate = null,
                    AXISQueueFK = axisQueue.AXISQueueID,
                    CRUD = GetEnumDescription(CRUD.Delete)
                };

                db.AXISQueueDetails.Add(queueDetails);
                #endregion

                db.product.Remove(prod);
                db.SaveChanges();

            }
        }

        public ProductViewModel GetProductPrices(int id)
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    productPrices = db.productPrice.Include("websiteInventory").Include("websiteInventory.website")
                                        .Where(x => x.websiteInventory.productFK == id)
                                        .OrderBy(x => x.websiteInventory.websiteFK)
                                        .ThenByDescending(x => x.productPriceID).ToList();
                }
            }
            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }

            return this;
        }

        private static void CompareProduct(product newProduct, product existingProduct, AXISQueue axisQueue)
        {
            CompareAndCreateQueueEntry(newProduct.partNo, existingProduct.partNo, "product", "partNo", axisQueue.AXISQueueID);
            CompareAndCreateQueueEntry(newProduct.productName, existingProduct.productName, "product", "productName", axisQueue.AXISQueueID);
            CompareAndCreateQueueEntry(newProduct.UNSPSCCode, existingProduct.UNSPSCCode, "product", "UNSPSCCode", axisQueue.AXISQueueID);
            CompareAndCreateQueueEntry(newProduct.manufacturerFK, existingProduct.manufacturerFK, "product", "manufacturerFK", axisQueue.AXISQueueID);
            CompareAndCreateQueueEntry(newProduct.productStatusFK, existingProduct.productStatusFK, "product", "productStatusFK", axisQueue.AXISQueueID);
            CompareAndCreateQueueEntry(newProduct.productGroupFK, existingProduct.productGroupFK, "product", "productGroupNo", axisQueue.AXISQueueID);
            CompareAndCreateQueueEntry(newProduct.salesAreaGroupFK, existingProduct.salesAreaGroupFK, "product", "salesAreaGroupNo", axisQueue.AXISQueueID);
            CompareAndCreateQueueEntry(newProduct.dataSupplierFK, existingProduct.dataSupplierFK, "product", "dataSupplierFK", axisQueue.AXISQueueID);
            CompareAndCreateQueueEntry(newProduct.productItemTypeFK, existingProduct.productItemTypeFK, "product", "productItemTypeFK", axisQueue.AXISQueueID);
        }

        private static void CompareAxisFields(product newProduct, product existingProduct, AXISQueue axisQueue)
        {
            var nA = newProduct.AxisFields;
            var eA = existingProduct.AxisFields;
            CompareAndCreateQueueEntry(nA.additionalInfoUrl, eA != null ? eA.additionalInfoUrl : null, "product", "additionalInfoUrl", axisQueue.AXISQueueID);
            CompareAndCreateQueueEntry(nA.attr1, eA != null ? eA.attr1 : null, "product", "attr1", axisQueue.AXISQueueID);
            CompareAndCreateQueueEntry(nA.attr2, eA != null ? eA.attr2 : null, "product", "attr2", axisQueue.AXISQueueID);
            CompareAndCreateQueueEntry(nA.attr3, eA != null ? eA.attr3 : null, "product", "attr3", axisQueue.AXISQueueID);
            CompareAndCreateQueueEntry(nA.attr4, eA != null ? eA.attr4 : null, "product", "attr4", axisQueue.AXISQueueID);
            CompareAndCreateQueueEntry(nA.attr5, eA != null ? eA.attr5 : null, "product", "attr5", axisQueue.AXISQueueID);
            CompareAndCreateQueueEntry(nA.attr6, eA != null ? eA.attr6 : null, "product", "attr6", axisQueue.AXISQueueID);
            CompareAndCreateQueueEntry(nA.attr7, eA != null ? eA.attr7 : null, "product", "attr7", axisQueue.AXISQueueID);
            CompareAndCreateQueueEntry(nA.attr8, eA != null ? eA.attr8 : null, "product", "attr8", axisQueue.AXISQueueID);
            CompareAndCreateQueueEntry(nA.attr9, eA != null ? eA.attr9 : null, "product", "attr9", axisQueue.AXISQueueID);
            CompareAndCreateQueueEntry(nA.attr10, eA != null ? eA.attr10 : null, "product", "attr10", axisQueue.AXISQueueID);
            CompareAndCreateQueueEntry(nA.bestSeller, eA != null ? eA.bestSeller : null, "product", "bestSeller", axisQueue.AXISQueueID);
            CompareAndCreateQueueEntry(nA.defaultDeliveryToCust, eA != null ? eA.defaultDeliveryToCust : null, "product", "defaultDeliveryToCust", axisQueue.AXISQueueID);
            CompareAndCreateQueueEntry(nA.discontinuedItem, eA != null ? eA.discontinuedItem : null, "product", "discontinuedItem", axisQueue.AXISQueueID);
            CompareAndCreateQueueEntry(nA.featured, eA != null ? eA.featured : null, "product", "featured", axisQueue.AXISQueueID);
            CompareAndCreateQueueEntry(nA.published, eA != null ? eA.published : null, "product", "published", axisQueue.AXISQueueID);
            CompareAndCreateQueueEntry(nA.reSaleable, eA != null ? eA.reSaleable : null, "product", "reSaleable", axisQueue.AXISQueueID);
            CompareAndCreateQueueEntry(nA.spec1, eA != null ? eA.spec1 : null, "product", "spec1", axisQueue.AXISQueueID);
            CompareAndCreateQueueEntry(nA.spec2, eA != null ? eA.spec2 : null, "product", "spec2", axisQueue.AXISQueueID);
            CompareAndCreateQueueEntry(nA.spec3, eA != null ? eA.spec3 : null, "product", "spec3", axisQueue.AXISQueueID);
            CompareAndCreateQueueEntry(nA.spec4, eA != null ? eA.spec4 : null, "product", "spec4", axisQueue.AXISQueueID);
            CompareAndCreateQueueEntry(nA.spec5, eA != null ? eA.spec5 : null, "product", "spec5", axisQueue.AXISQueueID);
            CompareAndCreateQueueEntry(nA.spec6, eA != null ? eA.spec6 : null, "product", "spec6", axisQueue.AXISQueueID);
            CompareAndCreateQueueEntry(nA.stockRecordType, eA != null ? eA.stockRecordType : null, "product", "stockRecordType", axisQueue.AXISQueueID);
            CompareAndCreateQueueEntry(nA.stockReference, eA != null ? eA.stockReference : null, "product", "stockReference", axisQueue.AXISQueueID);
            CompareAndCreateQueueEntry(nA.supressOpenRangeSpec, eA != null ? eA.supressOpenRangeSpec : null, "product", "supressOpenRangeSpec", axisQueue.AXISQueueID);
            CompareAndCreateQueueEntry(nA.supressOpenRangeImage, eA != null ? eA.supressOpenRangeImage : null, "product", "supressOpenRangeImage", axisQueue.AXISQueueID);
        }

        private static void CompareAxisFieldsAdditional(product newProduct, product existingProduct, AXISQueue axisQueue)
        {
            var nAfa = newProduct.AxisFields.AxisFieldsAdditional;

            if (existingProduct.AxisFields != null)
            {
                var eAfa = existingProduct.AxisFields.AxisFieldsAdditional;

                foreach (AxisFieldsAdditional a in nAfa)
                {
                    var ex = eAfa.Where(x => x.websiteFK == a.websiteFK).FirstOrDefault();

                    if (ex != null)
                    {
                        CompareAndCreateQueueEntry(a.bespokeFeedAvailability, ex.bespokeFeedAvailability, "product", "bespokeFeedAvailability", axisQueue.AXISQueueID);
                        CompareAndCreateQueueEntry(a.bespokeFeedCondition, ex.bespokeFeedCondition, "product", "bespokeFeedCondition", axisQueue.AXISQueueID);
                        CompareAndCreateQueueEntry(a.bespokeFeedInclude, ex.bespokeFeedInclude, "product", "bespokeFeedInclude", axisQueue.AXISQueueID);
                        CompareAndCreateQueueEntry(a.bespokeFeedSite, ex.bespokeFeedSite, "product", "bespokeFeedSite", axisQueue.AXISQueueID);
                        CompareAndCreateQueueEntry(a.bespokeFeedUseCustomShipCost, ex.bespokeFeedUseCustomShipCost, "product", "bespokeFeedUseCustomShipCost", axisQueue.AXISQueueID);
                        CompareAndCreateQueueEntry(a.googleFeedAvailability, ex.googleFeedAvailability, "product", "googleFeedAvailability", axisQueue.AXISQueueID);
                        CompareAndCreateQueueEntry(a.googleFeedCategory, ex.googleFeedCategory, "product", "googleFeedCategory", axisQueue.AXISQueueID);
                        CompareAndCreateQueueEntry(a.googleFeedCondition, ex.googleFeedCondition, "product", "googleFeedCondition", axisQueue.AXISQueueID);
                        CompareAndCreateQueueEntry(a.googleFeedInclude, ex.googleFeedInclude, "product", "googleFeedInclude", axisQueue.AXISQueueID);
                        CompareAndCreateQueueEntry(a.googleFeedSite, ex.googleFeedSite, "product", "googleFeedSite", axisQueue.AXISQueueID);
                        CompareAndCreateQueueEntry(a.metaDesc, ex.metaDesc, "product", "metaDesc" + a.websiteFK, axisQueue.AXISQueueID);
                        CompareAndCreateQueueEntry(a.metaKeywords, ex.metaKeywords, "product", "metaKeywords" + a.websiteFK, axisQueue.AXISQueueID);
                        CompareAndCreateQueueEntry(a.metaTitle, ex.metaTitle, "product", "metaTitle" + a.websiteFK, axisQueue.AXISQueueID);
                        CompareAndCreateQueueEntry(a.stockNoteDesc, ex.stockNoteDesc, "product", "stockNoteDesc" + a.websiteFK, axisQueue.AXISQueueID);
                    }
                }
            }
        }

        private static void CompareAndCreateQueueEntry(object a, object b, string entityName, string fieldName, int axisQueueID)
        {
            if (a == null)
                a = string.Empty;
            if (b == null)
                b = string.Empty;

            if (a.ToString() != b.ToString())
            {
                CreateQueueDetailsEntry(entityName, fieldName, axisQueueID, CRUD.Update);
            }
        }

        private static void CreateQueueDetailsEntry(string entityName, string fieldName, int axisQueueID, CRUD type)
        {
            AXISQueueDetails queueDetails = new AXISQueueDetails()
            {
                entityName = entityName,
                fieldName = fieldName,
                createdDate = DateTime.Now,
                completedDate = null,
                AXISQueueFK = axisQueueID,
                CRUD = GetEnumDescription(type)
            };

            using (ngmdEntities db = new ngmdEntities())
            {
                db.AXISQueueDetails.Add(queueDetails);
                db.SaveChanges();
            }
        }

        public static void CreateWebInventoryAXISQueue(websiteInventory wi, string cRUD, string field)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                db.Configuration.ProxyCreationEnabled = true;
                Type entityWebInventoryType = wi.GetType();

                //AXIS QUEUE
                AXISQueue axisQueue = null;
                if (!db.AXISQueue.Any(x => x.productFK == wi.productFK))
                {
                    axisQueue = new AXISQueue() { productFK = wi.productFK, dateLastUpdated = DateTime.Now };
                    db.AXISQueue.Add(axisQueue);
                    db.SaveChanges();
                }
                else
                {
                    axisQueue = db.AXISQueue.FirstOrDefault(x => x.productFK == wi.productFK);
                }

                AXISQueueDetails queueDetails = new AXISQueueDetails()
                {
                    entityName = "product",
                    fieldName = field,
                    createdDate = DateTime.Now,
                    completedDate = null,
                    AXISQueueFK = axisQueue.AXISQueueID,
                    CRUD = cRUD
                };

                db.AXISQueueDetails.Add(queueDetails);
                db.SaveChanges();
            }
        }

        public static void AddProductToAxisQueue(int productFK, string crud, QueueType type, string field)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                AXISQueue aq = db.AXISQueue.Where(x => x.productFK == productFK).FirstOrDefault();

                if (aq == null)
                {
                    aq = new AXISQueue()
                    {
                        productFK = productFK,
                        dateLastUpdated = DateTime.Now
                    };

                    db.AXISQueue.Add(aq);
                    db.SaveChanges();
                }

                AXISQueueDetails aqd = new AXISQueueDetails();
                aqd.AXISQueueFK = aq.AXISQueueID;
                aqd.createdDate = DateTime.Now;
                aqd.CRUD = crud;

                if (type == QueueType.Full)
                {
                    aqd.entityName = "All";
                    aqd.fieldName = "All";
                }
                else
                {
                    aqd.entityName = "product";
                    aqd.fieldName = field;
                }

                db.Entry(aqd).State = EntityState.Added;
                db.SaveChanges();
            }
        }
    }
}

