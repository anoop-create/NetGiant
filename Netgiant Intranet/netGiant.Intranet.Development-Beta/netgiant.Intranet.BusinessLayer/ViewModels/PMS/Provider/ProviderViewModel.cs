using System;
using System.Linq;
using netGiant.Intranet.DataLayer;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using PagedList;
using System.Data.Entity;
using System.Web.Mvc;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using System.Collections.Generic;
using System.Net;
using netGiant.Intranet.BusinessLayer.Utilities;
using System.IO;
using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Product;
using System.Linq.Expressions;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Provider
{
    public class ProviderViewModel : CommonViewModel
    {
        public ProviderViewModel()
        {
            this.allProviderTypes = SelectListViewModel.GetAllProviderTypes();
            _ctx = new ngmdEntities();
        }

        private ngmdEntities _ctx;

        //Shared
        public int selectedProviderTypeID { get; set; }
        public IQueryable<SelectListItem> allPMSMappingFields { get; set; }
        public IQueryable<SelectListItem> allProviders { get; set; }

        //Provider
        public provider prov { get; set; }
        public PagedList.IPagedList<provider> providers { get; set; }
        public IQueryable<SelectListItem> allProviderTypes { get; set; }

        //Provider Inventory
        public PagedList.IPagedList<providerInventory> providerInventories { get; set; }
        public PagedList.IPagedList<providerPrice> providerItemPrices { get; set; }
        public List<SelectListItem> allManufacturers { get; set; }
        public IQueryable<SelectListItem> allUnspscClasses { get; set; }
        public int manufacturerFK { get; set; }
        public string selectedUnspscCode { get; set; }
        public string FilePath { get; set; }
        public string LocalDirectory { get; set; }

        //Provider Type
        public IQueryable<SelectListItem> AllProvidersList { get; set; }

        //Settings
        public IQueryable<SelectListItem> allSuppliers { get; set; }
        public IQueryable<TelerikProviderInventory> providerInventory { get; set; }

        #region Provider

        public ProviderViewModel GetInventory()
        {
            providerInventory = _ctx.providerInventory
                .Join(_ctx.Lookup,
                    providerInventory => providerInventory.provider.providerTypeFK,
                    Lookup => Lookup.AltLookupId,
                    (providerInventory, Lookup) => new {providerInventory, Lookup}
                )
                .Where(x => x.Lookup.LookupType.LookupTypeName == "ProviderType")
                .Select(x => new TelerikProviderInventory
                    {
                        ProviderInventoryId = x.providerInventory.providerInventoryID,
                        ProviderType = x.Lookup.LookupName,
                        PartNo = x.providerInventory.partNo,
                        Description = x.providerInventory.description,
                        PotentialNew = x.providerInventory.potentialNewProduct ?? false,
                        Quantity = x.providerInventory.quantity,
                        DateLastUpdated = x.providerInventory.dateLastUpdate,
                        ProviderName = x.providerInventory.provider.providerName,
                        ProviderPartNo = x.providerInventory.providerPartNo,
                        Manufacturer = x.providerInventory.manufacturer.manufacturerName == null ? "Unknown" : x.providerInventory.manufacturer.manufacturerName,
                        Code = x.providerInventory.unspscCode,
                        Class = x.providerInventory.unspscClass,
                        ManufacturerReference = x.providerInventory.providerManuRef,
                        Barcode = x.providerInventory.barcode ?? "",
                        Price = x.providerInventory.providerPrice.OrderByDescending(y => y.dateLastUpdate).FirstOrDefault().price,
                        Untrusted = x.providerInventory.untrustedProvider,
                        Unwanted = x.providerInventory.unwantedProduct ?? false,
                        UntrustedAuto = x.providerInventory.untrustedProvider ? x.providerInventory.untrustedAuto ? "Auto" : "Manual" : ""
                    }
                );
            return this;
        }



        public class TelerikProvider
        {
            public int ID { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public string Type { get; set; }
            public DateTime? FeedFileDateTime { get; set; }
            public int? AxisSupplierReference { get; set; }
            public int? ReviewTotal { get; set; }
            public decimal? ReviewRating { get; set; }
            public Boolean Active { get; set; }
            public string ShortName { get; set; }
        }

        public IQueryable<TelerikProvider> ProviderList { get; set; }

        public ProviderViewModel GetProviders()
        {
            ProviderList = _ctx.provider
                .Join(
                    _ctx.Lookup,
                    provider => provider.providerTypeFK,
                    Lookup => Lookup.AltLookupId,
                    (provider, Lookup) => new {provider, Lookup}
                )
                .Where(x => x.Lookup.LookupType.LookupTypeName == "ProviderType")
                .Select(x => new TelerikProvider
                {
                    ID = x.provider.providerID,
                    Name = x.provider.providerName,
                    Description = x.provider.providerDesc,
                    Type = x.Lookup.LookupName,
                    FeedFileDateTime = x.provider.feedFileDateTime,
                    AxisSupplierReference = x.provider.axisSupplierRef,
                    ReviewTotal = x.provider.reviewTotal,
                    ReviewRating = x.provider.reviewRating,
                    Active = x.provider.active,
                    ShortName = x.provider.shortName
                })
                .AsQueryable();
            return this;
        }

        

        public ProviderViewModel GetProviderByID(int id)
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    NoLockInterceptor.ApplyNoLock = true;

                    prov = db.provider.Include(p => p.ftpDetails).Include(p => p.fieldMapping).FirstOrDefault(x => x.providerID == id);
                    hasRelevantFTPDetails = prov.ftpDetails.Count > 0 ? true : false;
                    allPMSMappingFields = GetRemainingFieldMappings(id);
                }
            }

            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }

            return this;
        }

        public static ProviderViewModel CreateProvider(int id)
        {
            ProviderViewModel model = new ProviderViewModel();

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    if (id > 0)
                    {
                        model.prov = db.provider.Find(id);
                    }
                    else
                    {
                        model.prov = new provider();
                    }

                    model.allProviderTypes = SelectListViewModel.GetAllProviderTypes();
                }
            }

            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }

            return model;
        }

        public void SaveProvider()
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    prov.dateLastUpdate = DateTime.Now;
                    if (prov.providerID > 0)
                    {
                        db.Entry(prov).State = EntityState.Modified;
                    }
                    else
                    {
                        db.provider.Add(prov);
                    }

                    db.SaveChanges();
                }
            }

            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }
        }

        public void DeleteProvider(int id)
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    provider p = db.provider.Find(id);
                    db.provider.Remove(p);
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }
        }

        public static List<priorityProvider> GetPriorityProviders(int manufacturerFK)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                return db.priorityProvider.Include(x => x.provider)
                    .Where(x => x.manufacturerFK == manufacturerFK)
                    .ToList();
            }
        }

        public static List<provider> GetProvidersByType(int providerTypeID)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                return db.provider.Where(x => x.providerTypeFK == providerTypeID).ToList();
            }
        }

        public void EnableDisableProvider(int id)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                var prov = db.provider.Find(id);
                prov.active = !prov.active;
                prov.dateLastUpdate = DateTime.Now;
                db.Entry(prov).State = EntityState.Modified;
                db.SaveChanges();
            }
        }

        #endregion Provider

        #region FTP Details

        public IQueryable<TelerikFtpDetails> FtpDetailsList { get; set; }
        public ftpDetails FtpDetails { get; set; }
        public int selectedProviderID { get; set; }
        public int selectedFtpDetailID { get; set; }
        public bool hasIndexedMapping { get; set; }

        public ProviderViewModel GetFtpDetails()
        {
            FtpDetailsList = _ctx.ftpDetails
                .Join(
                    _ctx.Lookup,
                    ftpDetails => ftpDetails.provider.providerTypeFK,
                    Lookup => Lookup.AltLookupId,
                    (ftpDetails, Lookup) => new {ftpDetails, Lookup}
                 )
                .Where(x => x.Lookup.LookupType.LookupTypeName == "ProviderType")
                .Select(x => new TelerikFtpDetails
                {
                    Id = x.ftpDetails.ftpDetailID,
                    Host = x.ftpDetails.ftpHost,
                    User = x.ftpDetails.ftpUser,
                    Password = x.ftpDetails.ftpPassword,
                    Directory = x.ftpDetails.ftpFolder,
                    Filename = x.ftpDetails.ftpFilename,
                    ZipFilename = x.ftpDetails.ftpZipFilename,
                    Provider = x.ftpDetails.provider.providerName,
                    ProviderType = x.Lookup.LookupName,
                    DateLastFeedFile = x.ftpDetails.dateLastFeedFile
                })
                .AsQueryable();
            return this;
        }

        public static ProviderViewModel CreateFtpDetails(int id, int selectedProviderID)
        {
            var model = new ProviderViewModel();
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    if (id > 0)
                    {
                        model.FtpDetails = db.ftpDetails.Find(id);
                    }
                    else
                    {
                        model.FtpDetails = new ftpDetails();
                        model.FtpDetails.fileColumnHeader = true;
                    }

                    if (selectedProviderID > 0)
                    {
                        model.FtpDetails.providerFK = selectedProviderID;
                        model.hasIndexedMapping = !model.FtpDetails.fileColumnHeader &&
                            db.fieldMapping.Where(x => x.providerFK == selectedProviderID).Count() > 0 ? true : false;
                    }

                    model.allProviders = SelectListViewModel.GetAllProviders();
                    model.selectedProviderID = selectedProviderID;
                }
            }

            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }

            return model;
        }

        public void SaveFtpDetails()
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    FtpDetails.dateLastUpdate = DateTime.Now;
                    FtpDetails.ftpHost = FtpDetails.ftpHost.Trim();
                    FtpDetails.ftpFilename = FtpDetails.ftpFilename.Trim();
                    FtpDetails.ftpZipFilename = FtpDetails.ftpZipFilename?.Trim() ?? "";
                    FtpDetails.ftpFolder = FtpDetails.ftpFolder?.Trim() ?? "";

                    if (FtpDetails.ftpDetailID > 0)
                    {
                        db.Entry(FtpDetails).State = EntityState.Modified;
                    }
                    else
                    {
                        db.ftpDetails.Add(FtpDetails);
                    }
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }
        }

        public SaveReturn DeleteFtpDetails(int id)
        {
            var sr = new SaveReturn();
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    ftpDetails fd = db.ftpDetails.Find(id);
                    db.ftpDetails.Remove(fd);
                    db.SaveChanges();
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

        public static bool TestFTPConnection(int id)
        {
            bool connected = false;

            using (ngmdEntities db = new ngmdEntities())
            {
                ftpDetails ftp = db.ftpDetails.Find(id);

                FtpWebRequest ftpWR = (FtpWebRequest)WebRequest.Create("ftp://" + ftp.ftpHost);
                ftpWR.Method = WebRequestMethods.Ftp.ListDirectory;
                ftpWR.Credentials = new NetworkCredential(ftp.ftpUser, ftp.ftpPassword);

                try
                {
                    FtpWebResponse res = (FtpWebResponse)ftpWR.GetResponse();
                    connected = true;
                }
                catch
                {
                    connected = false;
                }
            }

            return connected;
        }

        #endregion FTP Details

        #region Provider Inventory

        public ProviderViewModel GetProviderInventory()
        {
            return GetProviderInventory(null, "", "", "", null, null, true, null, true, false, false, null);
        }

        public ProviderViewModel GetProviderInventory(int? page, string searchTerm, string searchBy, string orderBy, int? providerTypeFK, int? manufacturerFK, bool inStockOnly, int? providerFK, bool potentialNewOnly, bool unwantedProduct, bool untrustedOnly, string unspscClass)
        {
            int pageSize = 24;
            int pageNumber = (page ?? 1);

            using (ngmdEntities db = new ngmdEntities())
            {
                IQueryable<providerInventory> list = db.providerInventory.Include(p => p.provider)
                                                        .Include(p => p.manufacturer).Include(p => p.providerPrice);

                if (this.selectedProviderTypeID > 0)
                    list = list.Where(x => x.provider.providerTypeFK == selectedProviderTypeID);

                list = SetPivWhere(searchTerm, searchBy, providerTypeFK, manufacturerFK,
                            inStockOnly, providerFK, potentialNewOnly, unwantedProduct,
                            untrustedOnly, unspscClass, list);

                //Sorting
                list = SetPivOrderBy(orderBy, list);

                NoLockInterceptor.ApplyNoLock = true;

                providerInventories = list.ToPagedList(pageNumber, pageSize);
                allManufacturers = SelectListViewModel.GetAllManufacturers(true);
                allUnspscClasses = SelectListViewModel.GetAllUnspscClasses();
                AllProvidersList = SelectListViewModel.GetAllProviders(selectedProviderTypeID);

            }

            return this;
        }

        private static IQueryable<providerInventory> SetPivWhere(string searchTerm, string searchBy, int? providerTypeFK, int? manufacturerFK, bool inStockOnly, int? providerFK, bool potentialNewOnly, bool unwantedProduct, bool untrustedOnly, string unspscClass, IQueryable<providerInventory> list)
        {
            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.ToLower().Trim();

                switch (searchBy)
                {
                    case "partNo":
                        list = list.Where(x => x.partNo.ToLower().Contains(searchTerm));
                        break;
                    case "provider":
                        list = list.Where(x => x.provider.providerName.ToLower().Contains(searchTerm));
                        break;
                    case "description":
                        list = list.Where(x => x.description.ToLower().Contains(searchTerm));
                        break;
                    case "providerPartNo":
                        list = list.Where(x => x.providerPartNo.ToLower().Contains(searchTerm));
                        break;
                    case "unspscClass":
                        list = list.Where(x => x.unspscCode.ToLower().Contains(searchTerm));
                        break;
                    default:
                        break;
                }
            }

            if (providerTypeFK != null && providerTypeFK > 0)
            {
                list = list.Where(x => x.provider.providerTypeFK == providerTypeFK);
            }

            if (providerFK != null && providerFK > 0)
            {
                list = list.Where(x => x.provider.providerID == providerFK);
            }

            if (manufacturerFK != null && manufacturerFK != 0)
            {
                if (manufacturerFK != -1)
                {
                    list = list.Where(x => x.manufacturerFK == manufacturerFK);
                }
                else
                {
                    list = list.Where(x => x.manufacturerFK == null);
                }
            }

            if (inStockOnly)
            {
                list = list.Where(x => x.quantity > 0);
            }

            if (potentialNewOnly)
            {
                list = list.Where(x => x.potentialNewProduct == true);
            }

            if (unwantedProduct)
            {
                list = list.Where(x => x.unwantedProduct == true);
            }
            else
            {
                list = list.Where(x => x.unwantedProduct == false);
            }

            if (untrustedOnly)
            {
                list = list.Where(x => x.untrustedProvider == true);
            }

            if (!string.IsNullOrEmpty(unspscClass))
            {
                list = list.Where(x => x.unspscClass == unspscClass);
            }

            return list;
        }

        private static IQueryable<providerInventory> SetPivOrderBy(string orderBy, IQueryable<providerInventory> list)
        {
            switch (orderBy)
            {
                case "partNoAsc":
                    list = list.OrderBy(x => x.partNo);
                    break;
                case "partNoDesc":
                    list = list.OrderByDescending(x => x.partNo);
                    break;
                case "descriptionAsc":
                    list = list.OrderBy(x => x.description);
                    break;
                case "descriptionDesc":
                    list = list.OrderByDescending(x => x.description);
                    break;
                case "quantityAsc":
                    list = list.OrderBy(x => x.quantity);
                    break;
                case "quantityDesc":
                    list = list.OrderByDescending(x => x.quantity);
                    break;
                case "effectiveDateAsc":
                    list = list.OrderBy(x => x.dateLastUpdate);
                    break;
                case "effectiveDateDesc":
                    list = list.OrderByDescending(x => x.dateLastUpdate);
                    break;
                case "providerAsc":
                    list = list.OrderBy(x => x.provider.providerName);
                    break;
                case "providerDesc":
                    list = list.OrderByDescending(x => x.provider.providerName);
                    break;
                case "manufacturerAsc":
                    list = list.OrderBy(x => (x.manufacturer.manufacturerName == null) ? "Unknown" : x.manufacturer.manufacturerName);
                    break;
                case "manufacturerDesc":
                    list = list.OrderByDescending(x => (x.manufacturer.manufacturerName == null) ? "Unknown" : x.manufacturer.manufacturerName);
                    break;
                case "providerPartNoAsc":
                    list = list.OrderBy(x => x.providerPartNo);
                    break;
                case "providerPartNoDesc":
                    list = list.OrderByDescending(x => x.providerPartNo);
                    break;
                case "unspscCodeAsc":
                    list = list.OrderBy(x => x.unspscCode);
                    break;
                case "unspscCodeDesc":
                    list = list.OrderByDescending(x => x.unspscCode);
                    break;
                case "unspscClassAsc":
                    list = list.OrderBy(x => x.unspscClass);
                    break;
                case "unspscClassDesc":
                    list = list.OrderByDescending(x => x.unspscClass);
                    break;
                case "manuRefAsc":
                    list = list.OrderBy(x => x.providerManuRef);
                    break;
                case "manuRefDesc":
                    list = list.OrderByDescending(x => x.providerManuRef);
                    break;
                default:
                    list = list.OrderBy(x => x.partNo);
                    break;
            }
            return list;
        }

        public ProviderViewModel GetProviderInventoryItemPrices(int id, int? page)
        {
            int pageSize = 24;
            int pageNumber = (page ?? 1);

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    NoLockInterceptor.ApplyNoLock = true;

                    providerItemPrices = db.providerPrice.Include(p => p.providerInventory).Where(x => x.providerInventoryFK == id)
                        .OrderBy(x => x.effectiveDate).ToPagedList(pageNumber, pageSize);
                }
            }

            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }

            return this;
        }

        public void ReinstateUnwantedProduct(string unwantedProviderIntIDs)
        {
            try
            {
                string[] unwPrds = unwantedProviderIntIDs.Split(',');

                using (ngmdEntities db = new ngmdEntities())
                {
                    foreach (string prd in unwPrds)
                    {
                        int unwPrvInvID = 0;
                        int.TryParse(prd, out unwPrvInvID);

                        if (unwPrvInvID > 0)
                        {
                            providerInventory unwPrvInv = db.providerInventory.Find(unwPrvInvID);
                            unwPrvInv.unwantedProduct = false;
                            db.SaveChanges();
                        }
                    }
                }
            }

            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }
        }

        public void SetProductInterest(string IDs, bool interested)
        {
            providerInventory potentialNewProduct = new providerInventory();

            try
            {
                string[] prdIDs = IDs.Split(',');

                using (ngmdEntities db = new ngmdEntities())
                {
                    foreach (string Id in prdIDs)
                    {
                        int ID = 0;
                        int.TryParse(Id, out ID);

                        if (ID > 0)
                        {
                            providerInventory piv = db.providerInventory.Include(m => m.manufacturer)
                                                        .Where(m => m.providerInventoryID == ID).First();

                            if (interested)
                            {
                                product product = new product();
                                product.partNo = piv.partNo ?? "";
                                product.productStatusFK = (int)db.Lookup.FirstOrDefault(x => x.LookupType.LookupTypeName == "ProductStatus" && x.LookupName == "No Status").AltLookupId;
                                product.manufacturerFK = piv.manufacturerFK;
                                product.salesAreaGroupFK = product.productGroupFK = null;
                                product.dateLastUpdate = DateTime.Now;
                                product.productItemTypeFK = 1;

                                string manu = piv.manufacturer != null ? piv.manufacturer.manufacturerName : null;
                                string partNo = piv.partNo;

                                var dataSource = (from x in db.pim_products
                                                  where x.partno == partNo && x.manufacturer == manu
                                                  select x).FirstOrDefault();


                                product.UNSPSCCode = dataSource == null ? "99999999" : dataSource.unspsc;
                                product.productName = dataSource == null ? piv.description : dataSource.model;
                                product.dataSupplierFK = 2;
                                product.barcode = GetBarcode(piv);

                                db.product.Add(product);
                                db.SaveChanges();

                                piv.potentialNewProduct = false;
                                piv.unwantedProduct = false;
                                db.Entry(piv).State = EntityState.Modified;

                                //Hide any other inventory entries with the same partNo, potentialNewProduct = false
                                List<providerInventory> updateProds = db.providerInventory.Where(x => x.partNo == piv.partNo).ToList();
                                updateProds.ForEach(x => x.potentialNewProduct = false);

                                //Hide any inventory entries where there is a skuMapping match, potentialNewProduct = false
                                List<skuMapping> skuMappedProviders = db.skuMapping.Include(m => m.providerInventory)
                                                                       .Where(x => x.providerInventoryFK == piv.providerInventoryID)
                                                                       .ToList();

                                skuMappedProviders.ForEach(m => m.providerInventory.potentialNewProduct = false);

                                //Auto map where possible, matching the altRef to the MFPN. Add to skuMapping the entries
                                foreach (providerInventory p in updateProds)
                                {
                                    skuMapping skmap = new skuMapping();
                                    skmap.altRef = p.partNo;
                                    skmap.productFK = product.productID;
                                    skmap.providerFK = p.providerFK;
                                    skmap.providerInventoryFK = p.providerInventoryID;
                                    skmap.providerPartNo = p.providerPartNo;
                                    skmap.supplierNo = p.provider.axisSupplierRef;
                                    db.skuMapping.Add(skmap);
                                }

                            }
                            else
                            {
                                piv.potentialNewProduct = true;
                                piv.unwantedProduct = true;
                            }

                            db.SaveChanges();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace, e.InnerException != null ? e.InnerException : null);
            }
        }

        private string GetBarcode(providerInventory piv)
        {
            string barcode = null;

            using (ngmdEntities db = new ngmdEntities())
            {
                if (!string.IsNullOrEmpty(piv.barcode))
                {
                    barcode = piv.barcode;
                }
                else
                {
                    var dataSupplierBarcode = db.ds_attributeView
                        .Where(x => x.dataSupplierID == 1 &&
                            x.partNo == piv.partNo &&
                            x.attrName == "Barcode")
                        .FirstOrDefault();

                    if (dataSupplierBarcode != null)
                        barcode = dataSupplierBarcode.attrValue;
                }
            }

            return barcode;
        }

        public void SetProviderUntrusted(int providerInventoryID, bool untrusted)
        {
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

        public void CreateCSVFile(List<TelerikProviderInventory> provList)
        {
            FilePath = LocalDirectory + "\\PMSTempData\\ProviderInvExport_" + DateTime.Now.ToString("dd_MM_yyyy_H_mm_ss") + ".csv";

            using (CsvFileWriter writer = new CsvFileWriter(FilePath, ','))
            {
                SetColumnHeadings(writer);

                foreach (TelerikProviderInventory prov in provList)
                {
                    InsertCSVData(writer, prov);
                }
            }
        }

        private void InsertCSVData(CsvFileWriter writer, TelerikProviderInventory prov)
        {
            CsvRow newRow = new CsvRow();
            newRow.Add(prov.ProviderType);
            newRow.Add(prov.PartNo);
            newRow.Add(prov.Description);
            newRow.Add(Convert.ToString(prov.PotentialNew));
            newRow.Add(Convert.ToString(prov.Quantity));
            newRow.Add(prov.DateLastUpdated.ToString("dd/MM/yyyy"));
            newRow.Add(prov.ProviderName);
            newRow.Add(prov.ProviderPartNo);
            newRow.Add(prov.Manufacturer);
            newRow.Add(prov.Code ?? "");
            newRow.Add(prov.Class ?? "");
            newRow.Add(prov.ManufacturerReference ?? "");
            newRow.Add(prov.Barcode ?? "");
            newRow.Add(Convert.ToString(prov.Price));
            newRow.Add(Convert.ToString(prov.Untrusted));
            newRow.Add(Convert.ToString(prov.Unwanted));

            writer.WriteRow(newRow);
        }

        private void SetColumnHeadings(CsvFileWriter writer)
        {
            CsvRow firstRow = new CsvRow();
            firstRow.Add("ProviderType");
            firstRow.Add("PartNo");
            firstRow.Add("Description");
            firstRow.Add("PotentialNew");
            firstRow.Add("Quantity");
            firstRow.Add("DateLastUpdated");
            firstRow.Add("ProviderName");
            firstRow.Add("ProviderPartNo");
            firstRow.Add("Manufacturer");
            firstRow.Add("Code");
            firstRow.Add("Class");
            firstRow.Add("ManufacturerReference");
            firstRow.Add("Barcode");
            firstRow.Add("Price");
            firstRow.Add("Untrusted");
            firstRow.Add("Unwanted");

            writer.WriteRow(firstRow);
        }

        public void DeleteFile(string filePath)
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }

        #endregion Provider Inventory

        #region Shared
        public static IQueryable<SelectListItem> GetRemainingFieldMappings(int selectedProviderID)
        {
            IQueryable<SelectListItem> remainings = null;
            List<SelectListItem> existingMappings = new List<SelectListItem>();
            string providerType = "";
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    foreach (fieldMapping mapp in db.fieldMapping.Where(x => x.providerFK == selectedProviderID))
                    {
                        switch (mapp.fieldMappingTo)
                        {
                            case "partNo":
                                existingMappings.Add(new SelectListItem() { Text = "Part No", Value = mapp.fieldMappingTo });
                                break;
                            case "price":
                                existingMappings.Add(new SelectListItem() { Text = "Price", Value = mapp.fieldMappingTo });
                                break;
                            case "quantity":
                                existingMappings.Add(new SelectListItem() { Text = "Quantity", Value = mapp.fieldMappingTo });
                                break;
                            case "description":
                                existingMappings.Add(new SelectListItem() { Text = "Description", Value = mapp.fieldMappingTo });
                                break;
                            case "barcode":
                                existingMappings.Add(new SelectListItem() { Text = "Barcode", Value = mapp.fieldMappingTo });
                                break;
                            default:
                                break;

                        }
                    }
                    providerType = db.provider
                        .Join(
                            db.Lookup,
                            provider => provider.providerTypeFK,
                            Lookup => Lookup.AltLookupId,
                            (provider, Lookup) => new {provider, Lookup}
                        )
                        .Where(x => x.Lookup.LookupType.LookupTypeName == "ProviderType")
                        .Where(x => x.provider.providerID == selectedProviderID)
                        .FirstOrDefault().Lookup.LookupName;
                }
            }
            catch (InvalidOperationException e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }

            HashSet<string> existingMappingFields = new HashSet<string>(existingMappings.Select(x => x.Value));
            //remainings = SelectListViewModel.GetAllPMSMappingFields().Where(m => !existingMappingFields.Contains(m.Value));
            remainings = SelectListViewModel.GetAllPMSMappingFields(providerType);
            return remainings;
        }

        #region SKU Mappings

        public IQueryable<TelerikSkuMapping> SkuMappingList { get; set; }
        public skuMapping SkuMapping { get; set; }

        public ProviderViewModel GetSkuMappings()
        {
            SkuMappingList = _ctx.skuMapping
                                 .Select(x => new TelerikSkuMapping
                                 {
                                     Id = x.skuMappingID,
                                     Provider = x.provider.providerName,
                                     ProviderPartNo = x.providerPartNo,
                                     OurPartNo = x.altRef,
                                     AxisSupplierNo = x.provider.axisSupplierRef
                                 })
                                 .AsQueryable();
            return this;
        }

        public static ProviderViewModel CreateSkuMapping(int id)
        {
            var model = new ProviderViewModel();
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    if (id > 0)
                    {
                        model.SkuMapping = db.skuMapping.Find(id);
                    }
                    else
                    {
                        model.SkuMapping = new skuMapping();
                    }
                    model.allSuppliers = SelectListViewModel.GetAllSuppliers();
                }
            }
            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }
            return model;
        }

        public void SaveSkuMapping()
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    if (SkuMapping.skuMappingID > 0)
                    {
                        SetSkuMapDetails(db);
                        db.Entry(SkuMapping).State = EntityState.Modified;
                    }
                    else
                    {
                        SetSkuMapDetails(db);
                        db.skuMapping.Add(SkuMapping);
                    }
                    AXISQueueViewModel que = new AXISQueueViewModel();
                    que.CreateQueueDetail(SkuMapping.product.productID);
                    UpdatePotentialNew(SkuMapping.providerPartNo, SkuMapping.providerFK, false);
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }
        }

        private void SetSkuMapDetails(ngmdEntities db)
        {
            providerInventory prov = db.providerInventory.Where(x => x.providerFK == SkuMapping.providerFK
                                            && x.providerPartNo == SkuMapping.providerPartNo).FirstOrDefault();
            product prod = db.product.Where(x => x.partNo == SkuMapping.altRef).FirstOrDefault();

            if (prov != null)
            {
                SkuMapping.providerInventoryFK = prov.providerInventoryID;
                SkuMapping.supplierNo = prov.provider.axisSupplierRef;
            }
            if (prod != null)
            {
                SkuMapping.productFK = prod.productID;
            }
        }

        public SaveReturn DeleteSkuMapping(int id)
        {
            var sr = new SaveReturn();
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    skuMapping p = db.skuMapping.Find(id);
                    UpdatePotentialNew(p.providerPartNo, p.providerFK, true);
                    db.skuMapping.Remove(p);
                    db.SaveChanges();
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

        #endregion

        public void UpdatePotentialNew(string providerPartNo, int? providerFK, bool IsPotentialNew)
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    providerInventory pi = db.providerInventory.Where(x => x.providerPartNo == providerPartNo &&
                                                x.providerFK == providerFK).FirstOrDefault();

                    if (pi != null)
                    {
                        pi.potentialNewProduct = IsPotentialNew;
                    }

                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }
        }

        #endregion

        #region Mfpn Extensions

        public IQueryable<TelerikMfpnExtension> MfpnExtensionsList { get; set; }
        public mfpnExtensions MfpnExtension { get; set; }

        public ProviderViewModel GetMfpnExtensions()
        {
            MfpnExtensionsList = _ctx.mfpnExtensions
                                     .Select(x => new TelerikMfpnExtension
                                     {
                                         Id = x.mfpnExtensionID,
                                         Manufacturer = x.manufacturer.manufacturerName,
                                         Extension = x.extension
                                     })
                                     .AsQueryable();
            return this;
        }

        public static ProviderViewModel CreateMfpnExtension(int id)
        {
            ProviderViewModel model = new ProviderViewModel();
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    if (id > 0)
                    {
                        model.MfpnExtension = db.mfpnExtensions.Find(id);
                    }
                    else
                    {
                        model.MfpnExtension = new mfpnExtensions();
                    }
                    model.allManufacturers = SelectListViewModel.GetAllManufacturers();
                }
            }
            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }

            return model;
        }

        public void SaveMfpnExtension()
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    if (MfpnExtension.mfpnExtensionID > 0)
                    {
                        db.Entry(MfpnExtension).State = EntityState.Modified;
                    }
                    else
                    {
                        db.mfpnExtensions.Add(MfpnExtension);
                    }
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }
        }

        public SaveReturn DeleteMfpnExtension(int id)
        {
            var sr = new SaveReturn();
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    mfpnExtensions p = db.mfpnExtensions.Find(id);
                    db.mfpnExtensions.Remove(p);
                    db.SaveChanges();
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

        #endregion

        #region Field Mappings 

        public IQueryable<TelerikFieldMapping> FieldMappingList { get; set; }
        public fieldMapping FieldMapping { get; set; }
        public bool hasRelevantFTPDetails { get; set; }
        public bool hasColumnHeaders { get; set; }

        public ProviderViewModel GetFieldMappings(int id = 0)
        {
            Expression<Func<fieldMapping, bool>> where = x => false;
            if (id != 0)
            {
                where = x => x.providerFK == id;
            }
            FieldMappingList = _ctx.fieldMapping
                                   .Where(where)
                                   .Select(x => new TelerikFieldMapping
                                   {
                                       Id = x.fieldMappingID,
                                       MappedTo = x.fieldMappingTo,
                                       MappedWith = x.fieldMappingWith,
                                       Provider = x.provider.providerName,
                                       LastUpdated = x.dateLastUpdate
                                   })
                                   .AsQueryable();
            return this;
        }

        public static ProviderViewModel CreateFieldMapping(int id, int selectedProviderId, int selectedFtpDetailId)
        {
            ProviderViewModel model = new ProviderViewModel();

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    if (id > 0)
                    {
                        model.FieldMapping = db.fieldMapping.Find(id);
                    }
                    else
                    {
                        model.FieldMapping = new fieldMapping();
                    }

                    if (selectedProviderId > 0)
                    {
                        model.FieldMapping.providerFK = selectedProviderId;
                        IQueryable<ftpDetails> ftps = db.provider.Find(selectedProviderId).ftpDetails.AsQueryable();

                        if (ftps.Count() > 0)
                        {
                            model.hasColumnHeaders = ftps.First().fileColumnHeader;
                        }
                    }

                    model.allPMSMappingFields = ProviderViewModel.GetRemainingFieldMappings(selectedProviderId);
                    model.allProviders = SelectListViewModel.GetAllProviders();
                    model.selectedProviderID = selectedProviderId;
                    model.selectedFtpDetailID = selectedFtpDetailId;
                }
            }

            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }

            return model;
        }

        public void SaveFieldMapping()
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {

                    FieldMapping.dateLastUpdate = DateTime.Now;
                    if (FieldMapping.fieldMappingID > 0)
                    {
                        db.Entry(FieldMapping).State = EntityState.Modified;
                    }
                    else
                    {
                        db.fieldMapping.Add(FieldMapping);
                    }

                    db.SaveChanges();

                }
            }

            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }
        }

        public SaveReturn DeleteFieldMapping(int id)
        {
            var sr = new SaveReturn();
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    fieldMapping fieldMapp = db.fieldMapping.Find(id);
                    db.fieldMapping.Remove(fieldMapp);
                    db.SaveChanges();
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

        #endregion

        #region Supplier Manufacturer Mappings

        public IQueryable<TelerikSupplierManufacturerMapping> SupplierManufacturerMappingList { get; set; }
        public supplierManuMapping SupplierManufacturerMapping { get; set; }

        public ProviderViewModel GetSupplierManufacturerMappings()
        {
            SupplierManufacturerMappingList = _ctx.supplierManuMapping
                                                  .Select(x => new TelerikSupplierManufacturerMapping
                                                  {
                                                      Id = x.supplierManuMappingID,
                                                      Provider = x.provider.providerName,
                                                      Reference = x.supplierManuRef,
                                                      Manufacturer = x.manufacturer1.manufacturerName
                                                  })
                                                  .AsQueryable();
            return this;
        }

        public static ProviderViewModel CreateSupplierManufacturerMapping(int id)
        {
            ProviderViewModel model = new ProviderViewModel();
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    if (id > 0)
                    {
                        model.SupplierManufacturerMapping = db.supplierManuMapping.Find(id);
                    }
                    else
                    {
                        model.SupplierManufacturerMapping = new supplierManuMapping();
                    }

                    model.allSuppliers = SelectListViewModel.GetAllSuppliers();
                    model.allManufacturers = SelectListViewModel.GetAllManufacturers();
                }
            }
            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }
            return model;
        }

        public void SaveSupplierManufacturerMapping()
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    SupplierManufacturerMapping.manufacturer = db.manufacturer.FirstOrDefault(x => x.manufacturerID == SupplierManufacturerMapping.manufacturerFK).manufacturerName;
                    SupplierManufacturerMapping.supplier = db.provider.FirstOrDefault(x => x.providerID == SupplierManufacturerMapping.providerFK).providerName;

                    if (SupplierManufacturerMapping.supplierManuMappingID > 0)
                    {
                        db.Entry(SupplierManufacturerMapping).State = EntityState.Modified;
                    }
                    else
                    {
                        db.supplierManuMapping.Add(SupplierManufacturerMapping);
                    }
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }
        }

        public SaveReturn DeleteSupplierManufacturerMapping(int id)
        {
            var sr = new SaveReturn();
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    supplierManuMapping p = db.supplierManuMapping.Find(id);
                    db.supplierManuMapping.Remove(p);
                    db.SaveChanges();
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

        #endregion
    }

    public class TelerikProviderInventory
    {
        public int ProviderInventoryId { get; set; }
        public string ProviderType { get; set; }
        public string PartNo { get; set; }
        public string Description { get; set; }
        public bool PotentialNew { get; set; }
        public int Quantity { get; set; }
        public DateTime DateLastUpdated { get; set; }
        public string ProviderName { get; set; }
        public string ProviderPartNo { get; set; }
        public string Manufacturer { get; set; }
        public string Code { get; set; }
        public string Class { get; set; }
        public string ManufacturerReference { get; set; }
        public string Barcode { get; set; }
        public double? Price { get; set; }
        public bool Untrusted { get; set; }
        public bool Unwanted { get; set; }
        public string UntrustedAuto { get; set; }
    }

    public class TelerikSkuMapping
    {
        public int Id { get; set; }
        public string Provider { get; set; }
        public string ProviderPartNo { get; set; }
        public string OurPartNo { get; set; }
        public int? AxisSupplierNo { get; set; }
    }

    public class TelerikMfpnExtension
    {
        public int Id { get; set; }
        public string Manufacturer { get; set; }
        public string Extension { get; set; }
    }

    public class TelerikFtpDetails
    {
        public int Id { get; set; }
        public string Host { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public string Directory { get; set; }
        public string Filename { get; set; }
        public string ZipFilename { get; set; }
        public string Provider { get; set; }
        public string ProviderType { get; set; }
        public DateTime? DateLastFeedFile { get; set; }
    }

    public class TelerikFieldMapping
    {
        public int Id { get; set; }
        public string MappedTo { get; set; }
        public string MappedWith { get; set; }
        public string Provider { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class TelerikSupplierManufacturerMapping
    {
        public int Id { get; set; }
        public string Provider { get; set; }
        public string Reference { get; set; }
        public string Manufacturer { get; set; }
    }
}
