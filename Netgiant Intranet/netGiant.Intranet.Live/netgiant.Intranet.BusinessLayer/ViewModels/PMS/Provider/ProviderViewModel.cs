using System;
using System.Linq;
using netGiant.Intranet.DataLayer;
using PagedList;
using System.Data.Entity;
using System.Web.Mvc;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using netGiant.Intranet.BusinessLayer.Utilities;
using System.IO;
using netGiant.Intranet.BusinessLayer.ViewModels.PMS.Product;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Provider
{
    public class ProviderViewModel
    {
        public ProviderViewModel()
        {
            this.allProviderTypes = SelectListViewModel.AllProviderTypes();
        }

        //Shared
        public int selectedProviderTypeID { get; set; }

        //Provider
        public provider prov { get; set; }
        public PagedList.IPagedList<provider> providers { get; set; }
        public IQueryable<SelectListItem> allProviderTypes { get; set; }

        //Field Mapping
        public fieldMapping fieldMapp { get; set; }
        public PagedList.IPagedList<fieldMapping> fieldMappings { get; set; }
        public IQueryable<SelectListItem> allProviders { get; set; }
        public bool hasRelevantFTPDetails { get; set; }
        public bool hasColumnHeaders { get; set; }
        public IQueryable<SelectListItem> allPMSMappingFields { get; set; }

        //FTP Details
        public ftpDetails ftp { get; set; }
        public PagedList.IPagedList<ftpDetails> listFTPDetails { get; set; }
        public int selectedProviderID { get; set; }
        public bool hasIndexedMapping { get; set; }

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
        public providerType provType { get; set; }
        public PagedList.IPagedList<providerType> ProviderTypes { get; set; }
        public IQueryable<SelectListItem> AllProvidersList { get; set; }

        //Settings
        public PagedList.IPagedList<skuMapping> skuMappingsList { get; set; }
        public IQueryable<SelectListItem> allSuppliers { get; set; }
        public skuMapping skuMap { get; set; }
        public PagedList.IPagedList<supplierManuMapping> supManuMappingsList { get; set; }
        public supplierManuMapping supManuMap { get; set; }
        public PagedList.IPagedList<mfpnExtensions> mfpnExtensionsList { get; set; }
        public mfpnExtensions mfpnEntension { get; set; }

        #region Provider

        public ProviderViewModel GetProviders()
        {
            return GetProviders(null, "", "", "");
        }

        public ProviderViewModel GetProviders(int? page, string searchTerm, string searchBy, string orderBy)
        {
            int pageSize = 24;
            int pageNumber = (page ?? 1);

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    IQueryable<provider> list = db.provider.Include(p => p.providerType).Include(p => p.ftpDetails)
                                                    .Include(p => p.fieldMapping);

                    if (this.selectedProviderTypeID > 0)
                        list = list.Where(x => x.providerTypeFK == selectedProviderTypeID);

                    if (!string.IsNullOrEmpty(searchTerm))
                    {
                        list = list.Where(x => x.providerName.ToLower().Contains(searchTerm.ToLower().Trim()));
                    }

                    switch (orderBy)
                    {
                        case "providerIDAsc":
                            list = list.OrderBy(x => x.providerID);
                            break;
                        case "providerIDDesc":
                            list = list.OrderByDescending(x => x.providerID);
                            break;
                        case "providerNameAsc":
                            list = list.OrderBy(x => x.providerName);
                            break;
                        case "providerNameDesc":
                            list = list.OrderByDescending(x => x.providerName);
                            break;
                        case "descriptionAsc":
                            list = list.OrderBy(x => x.providerDesc);
                            break;
                        case "descriptionDesc":
                            list = list.OrderByDescending(x => x.providerDesc);
                            break;
                        case "providerTypeAsc":
                            list = list.OrderBy(x => x.providerType.providerTypeName);
                            break;
                        case "providerTypeDesc":
                            list = list.OrderByDescending(x => x.providerType.providerTypeName);
                            break;
                        case "dateLastUpdatedAsc":
                            list = list.OrderBy(x => x.dateLastUpdate);
                            break;
                        case "dateLastUpdatedDesc":
                            list = list.OrderByDescending(x => x.dateLastUpdate);
                            break;
                        case "axisSupplierRefAsc":
                            list = list.OrderBy(x => x.axisSupplierRef);
                            break;
                        case "axisSupplierRefDesc":
                            list = list.OrderByDescending(x => x.axisSupplierRef);
                            break;
                        case "feedFileDateStampAsc":
                            list = list.OrderBy(x => x.feedFileDateTime);
                            break;
                        case "feedFileDateStampDesc":
                            list = list.OrderByDescending(x => x.feedFileDateTime);
                            break;
                        case "activeAsc":
                            list = list.OrderBy(x => x.active);
                            break;
                        case "activeDesc":
                            list = list.OrderByDescending(x => x.active);
                            break;
                        case "reviewTotalAsc":
                            list = list.OrderBy(x => x.reviewTotal);
                            break;
                        case "reviewTotalDesc":
                            list = list.OrderByDescending(x => x.reviewTotal);
                            break;
                        case "reviewRatingAsc":
                            list = list.OrderBy(x => x.reviewRating);
                            break;
                        case "reviewRatingDesc":
                            list = list.OrderByDescending(x => x.reviewRating);
                            break;
                        default:
                            list = list.OrderBy(x => x.providerID);
                            break;
                    }

                    NoLockInterceptor.ApplyNoLock = true;

                    providers = list.ToPagedList(pageNumber, pageSize);
                }
            }

            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }

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
                    allPMSMappingFields = ProviderViewModel.GetRemainingFieldMappings(id);
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

                    model.allProviderTypes = SelectListViewModel.AllProviderTypes();
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

        #region Field Mappings

        public ProviderViewModel GetFieldMappings()
        {
            return GetFieldMappings(null, "", "", "");
        }

        public ProviderViewModel GetFieldMappings(int? page, string searchTerm, string searchBy, string orderBy)
        {
            int pageSize = 24;
            int pageNumber = (page ?? 1);

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    IQueryable<fieldMapping> list = db.fieldMapping.Include(p => p.provider);

                    if (!string.IsNullOrEmpty(searchTerm))
                    {
                        switch (searchBy)
                        {
                            case "provider":
                                list = list.Where(x => x.provider.providerName.ToLower().Contains(searchTerm.ToLower().Trim()));
                                break;
                            default:
                                break;
                        }
                    }

                    switch (orderBy)
                    {
                        case "fieldMappingToAsc":
                            list = list.OrderBy(x => x.fieldMappingTo);
                            break;
                        case "fieldMappingToDesc":
                            list = list.OrderByDescending(x => x.fieldMappingTo);
                            break;
                        case "fieldMappingFromAsc":
                            list = list.OrderBy(x => x.fieldMappingWith);
                            break;
                        case "fieldMappingFromDesc":
                            list = list.OrderByDescending(x => x.fieldMappingWith);
                            break;
                        case "providerAsc":
                            list = list.OrderBy(x => x.provider.providerName);
                            break;
                        case "providerDesc":
                            list = list.OrderByDescending(x => x.provider.providerName);
                            break;
                        case "dateLastUpdatedAsc":
                            list = list.OrderBy(x => x.dateLastUpdate);
                            break;
                        case "dateLastUpdatedDesc":
                            list = list.OrderByDescending(x => x.dateLastUpdate);
                            break;
                        default:
                            list = list.OrderBy(x => x.fieldMappingID);
                            break;
                    }

                    fieldMappings = list.ToPagedList(pageNumber, pageSize);
                }
            }

            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }

            return this;
        }

        public static ProviderViewModel CreateFieldMapping(int id, int selectedProviderID)
        {
            ProviderViewModel model = new ProviderViewModel();

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    if (id > 0)
                    {
                        model.fieldMapp = db.fieldMapping.Find(id);
                    }
                    else
                    {
                        model.fieldMapp = new fieldMapping();
                    }

                    if (selectedProviderID > 0)
                    {
                        model.fieldMapp.providerFK = selectedProviderID;
                        IQueryable<ftpDetails> ftps = db.provider.Find(selectedProviderID).ftpDetails.AsQueryable();

                        if (ftps.Count() > 0)
                        {
                            model.hasColumnHeaders = ftps.First().fileColumnHeader;
                        }
                    }

                    model.allPMSMappingFields = ProviderViewModel.GetRemainingFieldMappings(selectedProviderID);
                    model.allProviders = SelectListViewModel.AllProviders();
                    model.selectedProviderID = selectedProviderID;
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

                    fieldMapp.dateLastUpdate = DateTime.Now;
                    if (fieldMapp.fieldMappingID > 0)
                    {
                        db.Entry(fieldMapp).State = EntityState.Modified;
                    }
                    else
                    {
                        db.fieldMapping.Add(fieldMapp);
                    }

                    db.SaveChanges();

                }
            }

            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }
        }

        public void DeleteFieldMapping(int id)
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    fieldMapping fieldMapp = db.fieldMapping.Find(id);
                    db.fieldMapping.Remove(fieldMapp);
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }
        }

        #endregion Field Mappings

        #region FTP Details

        public ProviderViewModel GetFtpDetails()
        {
            return GetFtpDetails(null, "", "", "");
        }

        public ProviderViewModel GetFtpDetails(int? page, string searchTerm, string searchBy, string orderBy)
        {
            int pageSize = 24;
            int pageNumber = (page ?? 1);

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    IQueryable<ftpDetails> list = db.ftpDetails.Include(p => p.provider);

                    if (!string.IsNullOrEmpty(searchTerm))
                    {
                        searchTerm = searchTerm.ToLower().Trim();

                        switch (searchBy)
                        {
                            case "host":
                                list = list.Where(x => x.ftpHost.ToLower().Contains(searchTerm));
                                break;
                            case "user":
                                list = list.Where(x => x.ftpUser.ToLower().Contains(searchTerm));
                                break;
                            case "filename":
                                list = list.Where(x => x.ftpFilename.ToLower().Contains(searchTerm));
                                break;
                            default:
                                break;
                        }
                    }

                    switch (orderBy)
                    {
                        case "ftpHostAsc":
                            list = list.OrderBy(x => x.ftpHost);
                            break;
                        case "ftpHostDesc":
                            list = list.OrderByDescending(x => x.ftpHost);
                            break;
                        case "ftpUserAsc":
                            list = list.OrderBy(x => x.ftpUser);
                            break;
                        case "ftpUserDesc":
                            list = list.OrderByDescending(x => x.ftpUser);
                            break;
                        case "ftpPasswordAsc":
                            list = list.OrderBy(x => x.ftpPassword);
                            break;
                        case "ftpPasswordDesc":
                            list = list.OrderByDescending(x => x.ftpPassword);
                            break;
                        case "ftpFolderAsc":
                            list = list.OrderBy(x => x.ftpFolder);
                            break;
                        case "ftpFolderDesc":
                            list = list.OrderByDescending(x => x.ftpFolder);
                            break;
                        case "filenameAsc":
                            list = list.OrderBy(x => x.ftpFilename);
                            break;
                        case "filenameDesc":
                            list = list.OrderByDescending(x => x.ftpFilename);
                            break;
                        case "zipFilenameAsc":
                            list = list.OrderBy(x => x.ftpZipFilename);
                            break;
                        case "zipFilenameDesc":
                            list = list.OrderByDescending(x => x.ftpZipFilename);
                            break;
                        case "providerAsc":
                            list = list.OrderBy(x => x.provider.providerName);
                            break;
                        case "providerDesc":
                            list = list.OrderByDescending(x => x.provider.providerName);
                            break;
                        default:
                            list = list.OrderBy(x => x.ftpDetailID);
                            break;
                    }

                    listFTPDetails = list.ToPagedList(pageNumber, pageSize);
                }
            }

            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }

            return this;
        }

        public static ProviderViewModel CreateFtpDetails(int id, int selectedProviderID)
        {
            ProviderViewModel model = new ProviderViewModel();

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    if (id > 0)
                    {
                        model.ftp = db.ftpDetails.Find(id);
                    }
                    else
                    {
                        model.ftp = new ftpDetails();
                        model.ftp.fileColumnHeader = true;
                    }

                    if (selectedProviderID > 0)
                    {
                        model.ftp.providerFK = selectedProviderID;
                        model.hasIndexedMapping = !model.ftp.fileColumnHeader &&
                            db.fieldMapping.Where(x => x.providerFK == selectedProviderID).Count() > 0 ? true : false;
                    }

                    model.allProviders = SelectListViewModel.AllProviders();
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
                    ftp.dateLastUpdate = DateTime.Now;
                    ftp.ftpZipFilename = ftp.ftpZipFilename ?? "";
                    ftp.ftpFolder = ftp.ftpFolder ?? "";

                    if (ftp.ftpDetailID > 0)
                    {
                        db.Entry(ftp).State = EntityState.Modified;
                    }
                    else
                    {
                        db.ftpDetails.Add(ftp);
                    }

                    db.SaveChanges();
                }
            }

            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }
        }

        public void DeleteFtpDetails(int id)
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    ftpDetails fd = db.ftpDetails.Find(id);
                    db.ftpDetails.Remove(fd);
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }
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

        public ProviderViewModel GetProviderInventory(int? page, string searchTerm, string searchBy,
                                                        string orderBy, int? providerTypeFK, int? manufacturerFK,
                                                        bool inStockOnly, int? providerFK, bool potentialNewOnly,
                                                        bool unwantedProduct, bool untrustedOnly, string unspscClass)
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
                allManufacturers = SelectListViewModel.AllManufacturers(true);
                allUnspscClasses = SelectListViewModel.AllUnspscClasses();
                AllProvidersList = SelectListViewModel.AllProviders(selectedProviderTypeID);

            }

            return this;
        }

        private static IQueryable<providerInventory> SetPivWhere(string searchTerm, string searchBy, int? providerTypeFK,
                                        int? manufacturerFK, bool inStockOnly, int? providerFK,
                                        bool potentialNewOnly, bool unwantedProduct, bool untrustedOnly,
                                        string unspscClass, IQueryable<providerInventory> list)
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
                                product.productStatusFK = db.productStatus.FirstOrDefault(x => x.productStatusName.Equals("No Status", StringComparison.InvariantCultureIgnoreCase)).productStatusID;
                                product.manufacturerFK = piv.manufacturerFK;
                                product.salesAreaGroupFK = product.productGroupFK = null;
                                product.dateLastUpdate = DateTime.Now;
                                product.productItemTypeFK = 1;

                                string manu = piv.manufacturer != null ? piv.manufacturer.manufacturerName : null;
                                string partNo = piv.partNo;

                                var dataSource = (from x in db.ds_product
                                                  where x.partNo == partNo && x.manufacturer == manu
                                                  select x).OrderBy(x => x.dataSupplierID).FirstOrDefault();


                                product.UNSPSCCode = dataSource == null ? "99999999" : dataSource.UNSPSC_Code;
                                product.productName = dataSource == null ? piv.description : dataSource.model;
                                product.dataSupplierFK = dataSource == null ? 2 : dataSource.dataSupplierID;
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
                    db.Entry(pi).State = EntityState.Modified;
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }
        }

        public void ExportProviderInventory(ProviderViewModel model)
        {
            ExportProviderInventory(model, "", "", "", null, null, true, null, true, false, false, null);
        }

        public void ExportProviderInventory(ProviderViewModel model, string searchTerm, string searchBy,
                                                    string orderBy, int? providerTypeFK, int? manufacturerFK,
                                                    bool inStockOnly, int? providerFK, bool potentialNewOnly,
                                                    bool unwantedProduct, bool untrustedOnly, string unspscClass)
        {
            try
            {
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

                    List<providerInventory> finalList = list.Take(10000).ToList();
                    CreateCSVFile(finalList);

                }
            }
            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }
        }

        private void CreateCSVFile(List<providerInventory> provList)
        {
            FilePath = LocalDirectory + "\\PMSTempData\\ProviderInvExport_" + DateTime.Now.ToString("dd_MM_yyyy_H_mm_ss") + ".csv";

            using (CsvFileWriter writer = new CsvFileWriter(FilePath, ','))
            {
                SetColumnHeadings(writer);

                foreach (providerInventory prov in provList)
                {
                    InsertCSVData(writer, prov);
                }
            }
        }

        private void InsertCSVData(CsvFileWriter writer, providerInventory prov)
        {
            CsvRow newRow = new CsvRow();
            newRow.Add(prov.partNo != null ? prov.partNo : "");
            newRow.Add(prov.description != null ? prov.description : "");
            newRow.Add(prov.untrustedProvider.ToString());
            newRow.Add(prov.quantity.ToString());
            newRow.Add(prov.dateLastUpdate != null ? prov.dateLastUpdate.ToString() : "");
            newRow.Add(prov.providerFK.ToString());
            newRow.Add(prov.provider.providerName != null ? prov.provider.providerName : "");
            newRow.Add(prov.providerPartNo != null ? prov.providerPartNo : "");
            newRow.Add(prov.manufacturer != null ? prov.manufacturer.manufacturerName : "");
            newRow.Add(prov.unspscCode != null ? prov.unspscCode : "");
            newRow.Add(prov.unspscClass != null ? prov.unspscClass : "");
            newRow.Add(prov.providerManuRef != null ? prov.providerManuRef : "");

            providerPrice pp = prov.providerPrice.OrderByDescending(x => x.dateLastUpdate).FirstOrDefault();

            if (pp != null)
            {
                newRow.Add(pp.price.ToString());
            }
            else
            {
                newRow.Add("");
            }

            writer.WriteRow(newRow);
        }

        private void SetColumnHeadings(CsvFileWriter writer)
        {
            CsvRow firstRow = new CsvRow();
            firstRow.Add("Supplier Mfpn");
            firstRow.Add("Product Name");
            firstRow.Add("Untrusted");
            firstRow.Add("Quantity");
            firstRow.Add("Date Last Updated");
            firstRow.Add("Provider Id");
            firstRow.Add("Provider Name");
            firstRow.Add("Provider Part No");
            firstRow.Add("Manufacturer");
            firstRow.Add("UNSPSC");
            firstRow.Add("UNSPSC Class");
            firstRow.Add("Provider Manu Ref");
            firstRow.Add("Price");
            writer.WriteRow(firstRow);
        }

        public void DeleteFile(string filePath)
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }

        #endregion Provider Inventory

        #region Provider Type

        public ProviderViewModel GetProviderTypes()
        {
            return GetProviderTypes(null, "", "", "");
        }

        public ProviderViewModel GetProviderTypes(int? page, string searchTerm, string searchBy, string orderBy)
        {
            int pageSize = 24;
            int pageNumber = (page ?? 1);

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    IQueryable<providerType> list = db.providerType;

                    if (!string.IsNullOrEmpty(searchTerm))
                    {
                        searchTerm = searchTerm.ToLower().Trim();

                        switch (searchBy)
                        {
                            case "name":
                                list = list.Where(x => x.providerTypeName.ToLower().Contains(searchTerm));
                                break;
                            default:
                                break;
                        }
                    }

                    switch (orderBy)
                    {
                        case "providerTypeNameAsc":
                            list = list.OrderBy(x => x.providerTypeName);
                            break;
                        case "providerTypeNameDesc":
                            list = list.OrderByDescending(x => x.providerTypeName);
                            break;
                        case "dateLastUpdatedAsc":
                            list = list.OrderBy(x => x.dateLastUpdate);
                            break;
                        case "dateLastUpdatedDesc":
                            list = list.OrderByDescending(x => x.dateLastUpdate);
                            break;
                        default:
                            list = list.OrderBy(x => x.providerTypeID);
                            break;
                    }

                    ProviderTypes = list.ToPagedList(pageNumber, pageSize);
                }
            }

            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }

            return this;
        }

        public static ProviderViewModel CreateProviderType(int id)
        {
            ProviderViewModel model = new ProviderViewModel();

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    if (id > 0)
                    {
                        model.provType = db.providerType.Find(id);
                    }
                    else
                    {
                        model.provType = new providerType();
                    }
                }
            }

            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }

            return model;
        }

        public void SaveProviderType()
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    provType.dateLastUpdate = DateTime.Now;
                    if (provType.providerTypeID > 0)
                    {
                        db.Entry(provType).State = EntityState.Modified;
                    }
                    else
                    {
                        db.providerType.Add(provType);
                    }

                    db.SaveChanges();
                }
            }

            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }
        }

        public void DeleteProviderType(int id)
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    providerType pt = db.providerType.Find(id);
                    db.providerType.Remove(pt);
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }
        }
        #endregion Provider Type

        #region Shared
        public static IQueryable<SelectListItem> GetRemainingFieldMappings(int selectedProviderID)
        {
            IQueryable<SelectListItem> remainings = null;
            List<SelectListItem> existingMappings = new List<SelectListItem>();
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
                }
            }
            catch (InvalidOperationException e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }

            HashSet<string> existingMappingFields = new HashSet<string>(existingMappings.Select(x => x.Value));
            remainings = SelectListViewModel.AllPMSMappingFields().Where(m => !existingMappingFields.Contains(m.Value));
            return remainings;
        }

        public ProviderViewModel GetSkuMappings(int? page, string searchTerm, string searchBy, string orderBy,
                                                int? providerID)
        {
            int pageSize = 24;
            int pageNumber = (page ?? 1);

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    IQueryable<skuMapping> list = db.skuMapping.Include("provider")
                                                                .Include("product")
                                                                .Include("providerInventory");

                    if (!string.IsNullOrEmpty(searchTerm))
                    {
                        searchTerm = searchTerm.ToLower().Trim();

                        switch (searchBy)
                        {
                            case "providerPartNo":
                                list = list.Where(x => x.providerPartNo.ToLower().Contains(searchTerm));
                                break;
                            case "ourPartNo":
                                list = list.Where(x => x.altRef.ToLower().Contains(searchTerm));
                                break;
                            default:
                                break;
                        }
                    }

                    switch (orderBy)
                    {
                        case "providerPartNoAsc":
                            list = list.OrderBy(x => x.providerPartNo);
                            break;
                        case "providerPartNoDesc":
                            list = list.OrderByDescending(x => x.providerPartNo);
                            break;
                        case "providerAsc":
                            list = list.OrderBy(x => (x.provider.providerName == null) ? "Unknown" : x.provider.providerName);
                            break;
                        case "providerDesc":
                            list = list.OrderByDescending(x => (x.provider.providerName == null) ? "Unknown" : x.provider.providerName);
                            break;
                        case "ourPartNoAsc":
                            list = list.OrderBy(x => x.altRef);
                            break;
                        case "ourPartNoDesc":
                            list = list.OrderByDescending(x => x.altRef);
                            break;
                        case "axisSupplierNoAsc":
                            list = list.OrderBy(x => x.supplierNo);
                            break;
                        case "axisSupplierNoDesc":
                            list = list.OrderByDescending(x => x.supplierNo);
                            break;
                        default:
                            list = list.OrderBy(x => (x.provider.providerName == null) ? "Unknown" : x.provider.providerName);
                            break;
                    }

                    if (providerID != null && providerID > 0)
                    {
                        list = list.Where(x => x.providerFK == providerID);
                    }

                    skuMappingsList = list.ToPagedList(pageNumber, pageSize);
                    allSuppliers = SelectListViewModel.AllSuppliers();
                }
            }

            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }

            return this;
        }

        public static ProviderViewModel CreateSkuMapping(int id)
        {
            ProviderViewModel model = new ProviderViewModel();

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    if (id > 0)
                    {
                        model.skuMap = db.skuMapping.Find(id);
                    }
                    else
                    {
                        model.skuMap = new skuMapping();
                    }

                    model.allSuppliers = SelectListViewModel.AllSuppliers();
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
                    if (skuMap.skuMappingID > 0)
                    {
                        SetSkuMapDetails(db);
                        db.Entry(skuMap).State = EntityState.Modified;
                    }
                    else
                    {
                        SetSkuMapDetails(db);
                        db.skuMapping.Add(skuMap);
                    }
                    AXISQueueViewModel que = new AXISQueueViewModel();
                    que.CreateQueueDetail(skuMap.product.productID);
                    UpdatePotentialNew(skuMap.providerPartNo, skuMap.providerFK, false);
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
            providerInventory prov = db.providerInventory.Where(x => x.providerFK == skuMap.providerFK
                                            && x.providerPartNo == skuMap.providerPartNo).FirstOrDefault();
            product prod = db.product.Where(x => x.partNo == skuMap.altRef).FirstOrDefault();

            if (prov != null)
            {
                skuMap.providerInventoryFK = prov.providerInventoryID;
            }
            if (prod != null)
            {
                skuMap.productFK = prod.productID;
            }
        }

        public void DeleteSkuMapping(int id)
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    skuMapping p = db.skuMapping.Find(id);
                    UpdatePotentialNew(p.providerPartNo, p.providerFK, true);
                    db.skuMapping.Remove(p);

                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }
        }

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

        public ProviderViewModel GetSupManuMappings(int? page, string searchTerm, string searchBy, string orderBy,
                                                int? providerID, int? manufacturerID)
        {
            int pageSize = 24;
            int pageNumber = (page ?? 1);

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    IQueryable<supplierManuMapping> list = db.supplierManuMapping.Include(m => m.provider).Include(m => m.manufacturer1);

                    if (!string.IsNullOrEmpty(searchTerm))
                    {
                        searchTerm = searchTerm.ToLower().Trim();

                        switch (searchBy)
                        {
                            case "supManuRef":
                                list = list.Where(x => x.supplierManuRef.ToLower().Contains(searchTerm));
                                break;
                            default:
                                break;
                        }
                    }

                    switch (orderBy)
                    {
                        case "supManuRefAsc":
                            list = list.OrderBy(x => (x.supplierManuRef));
                            break;
                        case "supManuRefDesc":
                            list = list.OrderByDescending(x => (x.supplierManuRef));
                            break;
                        case "providerAsc":
                            list = list.OrderBy(x => (x.provider.providerName));
                            break;
                        case "providerDesc":
                            list = list.OrderByDescending(x => (x.provider.providerName));
                            break;
                        case "manufacturerAsc":
                            list = list.OrderBy(x => (x.manufacturer1.manufacturerName));
                            break;
                        case "manufacturerDesc":
                            list = list.OrderByDescending(x => (x.manufacturer1.manufacturerName));
                            break;
                        default:
                            list = list.OrderBy(x => (x.provider.providerName == null) ? "Unknown" : x.provider.providerName);
                            break;
                    }

                    if (providerID != null && providerID > 0)
                    {
                        list = list.Where(x => x.providerFK == providerID);
                    }

                    if (manufacturerID != null && manufacturerID > 0)
                    {
                        list = list.Where(x => x.manufacturerFK == manufacturerID);
                    }

                    supManuMappingsList = list.ToPagedList(pageNumber, pageSize);
                    allSuppliers = SelectListViewModel.AllSuppliers();
                    allManufacturers = SelectListViewModel.AllManufacturers();
                }
            }

            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }

            return this;
        }

        public static ProviderViewModel CreateSupManuMapping(int id)
        {
            ProviderViewModel model = new ProviderViewModel();

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    if (id > 0)
                    {
                        model.supManuMap = db.supplierManuMapping.Find(id);
                    }
                    else
                    {
                        model.supManuMap = new supplierManuMapping();
                    }

                    model.allSuppliers = SelectListViewModel.AllSuppliers();
                    model.allManufacturers = SelectListViewModel.AllManufacturers();
                }
            }

            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }

            return model;
        }

        public void SaveSupManuMapping()
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    if (supManuMap.supplierManuMappingID > 0)
                    {
                        db.Entry(supManuMap).State = EntityState.Modified;
                    }
                    else
                    {
                        db.supplierManuMapping.Add(supManuMap);
                    }

                    db.SaveChanges();
                }
            }

            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }
        }

        public void DeleteSupManuMapping(int id)
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    supplierManuMapping p = db.supplierManuMapping.Find(id);
                    db.supplierManuMapping.Remove(p);
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }
        }




































        public ProviderViewModel GetMfpnExtensions(int? page, string searchTerm, string searchBy, string orderBy,
                                                int? manufacturerID)
        {
            int pageSize = 24;
            int pageNumber = (page ?? 1);

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    IQueryable<mfpnExtensions> list = db.mfpnExtensions.Include(m => m.manufacturer);

                    if (!string.IsNullOrEmpty(searchTerm))
                    {
                        searchTerm = searchTerm.ToLower().Trim();

                        switch (searchBy)
                        {
                            case "extension":
                                list = list.Where(x => x.extension.ToLower().Contains(searchTerm));
                                break;
                            default:
                                break;
                        }
                    }

                    switch (orderBy)
                    {
                        case "extensionAsc":
                            list = list.OrderBy(x => (x.extension));
                            break;
                        case "extensionDesc":
                            list = list.OrderByDescending(x => (x.extension));
                            break;
                        case "manufacturerAsc":
                            list = list.OrderBy(x => (x.manufacturer.manufacturerName));
                            break;
                        case "manufacturerDesc":
                            list = list.OrderByDescending(x => (x.manufacturer.manufacturerName));
                            break;
                        default:
                            list = list.OrderBy(x => (x.manufacturer.manufacturerName));
                            break;
                    }

                    if (manufacturerID != null && manufacturerID > 0)
                    {
                        list = list.Where(x => x.manuID == manufacturerID);
                    }

                    mfpnExtensionsList = list.ToPagedList(pageNumber, pageSize);
                    allManufacturers = SelectListViewModel.AllManufacturers();
                }
            }

            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }

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
                        model.mfpnEntension = db.mfpnExtensions.Find(id);
                    }
                    else
                    {
                        model.mfpnEntension = new mfpnExtensions();
                    }

                    model.allManufacturers = SelectListViewModel.AllManufacturers();
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
                    if (mfpnEntension.mfpnExtensionID > 0)
                    {
                        db.Entry(mfpnEntension).State = EntityState.Modified;
                    }
                    else
                    {
                        db.mfpnExtensions.Add(mfpnEntension);
                    }

                    db.SaveChanges();
                }
            }

            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }
        }

        public void DeleteMfpnExtension(int id)
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    mfpnExtensions p = db.mfpnExtensions.Find(id);
                    db.mfpnExtensions.Remove(p);
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }
        }

        #endregion

    }
}
