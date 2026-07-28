using netGiant.Intranet.BusinessLayer.Utilities;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Mvc;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Export
{
    public class ExportProductImagesViewModel : CommonViewModel
    {
        public ExportProductImagesViewModel()
        {
            Initialize();
        }

        private void Initialize()
        {
            AllProductStatuses = SelectListViewModel.GetNgmdLookupSelectList("ProductStatus");
            AllWebsites = SelectListViewModel.GetAllWebsites();
            AllProductGroups = SelectListViewModel.GetAllProductGroups();
            AllSalesAreaGroups = SelectListViewModel.GetAllSalesAreaGroups();
            AllDataSuppliers = SelectListViewModel.GetAllDataSuppliers();
            AllManufacturers = SelectListViewModel.GetAllManufacturers();
            ExportProductFieldDictionary = new Dictionary<string, string>();
            ExportAxisFieldsFieldDictionary = new Dictionary<string, string>();
        }

        public IQueryable<SelectListItem> AllProductStatuses { get; set; }
        public IQueryable<SelectListItem> AllWebsites { get; set; }
        public IQueryable<SelectListItem> AllProductGroups { get; set; }
        public IQueryable<SelectListItem> AllSalesAreaGroups { get; set; }
        public IQueryable<SelectListItem> AllDataSuppliers { get; set; }
        public List<SelectListItem> AllManufacturers { get; set; }
        public int SelectedProductStatusFK { get; set; }
        public int SelectedWebsiteFK { get; set; }
        public int SelectedProductGroupFK { get; set; }
        public int SelectedSalesAreaGroupFK { get; set; }
        public int SelectedDataSupplierFK { get; set; }
        public int SelectedCategoryCodeFK { get; set; }
        public int SelectedManufacturerFK { get; set; }
        public Dictionary<string, string> ExportProductFieldDictionary { get; set; }
        public Dictionary<string, string> ExportAxisFieldsFieldDictionary { get; set; }
        public int ProductCount { get; set; }
        public string FilePath { get; set; }
        public string LocalDirectory { get; set; }
        public bool ExProductGroupById { get; set; }
        public bool ExSalesAreaGroupById { get; set; }
        public bool ExAttributesById { get; set; }

        public void GetExportableFields()
        {
            GetProductExportFieldLookupDictionary();
            GetAxisFieldsExportFieldLookupDictionary();
        }

        public ExportProductImagesViewModel Export()
        {
            GetExportableFields();
            List<productImage> productImagesList;
            productImagesList = GetProductImages();
            CreateCSVFile(productImagesList);

            return this;
        }

        private void CreateCSVFile(List<productImage> productImagesList)
        {
            FilePath = LocalDirectory + "\\PMSTempData\\ProductExport_" + DateTime.Now.ToString("dd_MM_yyyy_H_mm_ss") + ".csv";

            using (CsvFileWriter writer = new CsvFileWriter(FilePath, ','))
            {
                SetColumnHeadings(writer);

                foreach (productImage prodImage in productImagesList)
                {
                    InsertCSVData(writer, prodImage);
                }
            }
        }

        private void GetProductExportFieldLookupDictionary()
        {
            List<exportFieldLookup> list = new List<exportFieldLookup>();

            using (ngmdEntities db = new ngmdEntities())
            {
                list = db.exportFieldLookup.Where(x => x.tableName == "product").ToList();
            }

            foreach (exportFieldLookup field in list)
            {
                if (field.fieldName == "UNSPSCCode")
                    continue;

                ExportProductFieldDictionary.Add(field.fieldName, field.friendlyFieldName);
            }
        }

        private void GetAxisFieldsExportFieldLookupDictionary()
        {
            List<exportFieldLookup> list = new List<exportFieldLookup>();

            using (ngmdEntities db = new ngmdEntities())
            {
                if (SelectedWebsiteFK == 0)
                {
                    list = db.exportFieldLookup.Where(x => x.tableName == "axisFields" &&
                                        (x.websiteFK == null || x.websiteFK == 1))
                                            .ToList();
                }
                else
                {
                    list = db.exportFieldLookup.Where(x => x.tableName == "axisFields" &&
                                        (x.websiteFK == null || x.websiteFK == SelectedWebsiteFK))
                                            .ToList();
                }
            }

            foreach (exportFieldLookup field in list)
            {
                ExportAxisFieldsFieldDictionary.Add(field.fieldName, field.friendlyFieldName);
            }
        }

        private void InsertCSVData(CsvFileWriter writer, productImage prodImage)
        {
            CsvRow newRow = new CsvRow();
            InsertProductCSVData(prodImage, newRow);
            InsertAxisFieldsCSVData(prodImage, newRow);
            writer.WriteRow(newRow);
        }

        private void AddBlankCell(CsvRow newRow)
        {
            newRow.Add("");
        }

        private void InsertProductCSVData(productImage prodImage, CsvRow newRow)
        {
            AddCsvData(newRow, "partNo", prodImage.websiteInventory.product.partNo, ExportProductFieldDictionary);
            AddCsvData(newRow, "manufacturerFK", prodImage.websiteInventory.product.manufacturer.manufacturerName, ExportProductFieldDictionary);
        }

        private void InsertAxisFieldsCSVData(productImage prodImage, CsvRow newRow)
        {
            AddCsvData(newRow, "website", prodImage.websiteInventory.Website.FriendlyName, ExportAxisFieldsFieldDictionary);
            AddCsvData(newRow, "imageURL", prodImage.URL, ExportAxisFieldsFieldDictionary);
            AddCsvData(newRow, "isThumbnail", prodImage.thumbnailImage == true ? "1" : "0", ExportAxisFieldsFieldDictionary);
            AddCsvData(newRow, "isMain", prodImage.mainImage == true ? "1" : "0", ExportAxisFieldsFieldDictionary);
            AddCsvData(newRow, "ACDModifier", prodImage.ACDModifier, ExportAxisFieldsFieldDictionary);
        }

        private void AddCsvData(CsvRow newRow, string entityName, object entityData,
                                Dictionary<string, string> dict)
        {
            if (dict.ContainsKey(entityName))
                newRow.Add(entityData.ToSafeString());
        }

        private void SetColumnHeadings(CsvFileWriter writer)
        {
            CsvRow firstRow = new CsvRow();
            SetProductColumnHeadings(firstRow);
            SetAxisFieldsColumnHeadings(firstRow);
            writer.WriteRow(firstRow);
        }

        private void SetAxisFieldsColumnHeadings(CsvRow firstRow)
        {
            AddCsvColumn(firstRow, "website", ExportAxisFieldsFieldDictionary);
            AddCsvColumn(firstRow, "imageURL", ExportAxisFieldsFieldDictionary);
            AddCsvColumn(firstRow, "isThumbnail", ExportAxisFieldsFieldDictionary);
            AddCsvColumn(firstRow, "isMain", ExportAxisFieldsFieldDictionary);
            AddCsvColumn(firstRow, "ACDModifier", ExportAxisFieldsFieldDictionary);
        }

        private void SetProductColumnHeadings(CsvRow firstRow)
        {
            AddCsvColumn(firstRow, "partNo", ExportProductFieldDictionary);
            AddCsvColumn(firstRow, "manufacturerFK", ExportProductFieldDictionary);            
        }

        private void AddCsvColumn(CsvRow firstRow, string entityName,
                                Dictionary<string, string> dict)
        {
            if (dict.ContainsKey(entityName))
                firstRow.Add(dict.FirstOrDefault(m => m.Key == entityName).Value);
        }

        private List<productImage> GetProductImages()
        {
            List<productImage> productImagesList = new List<productImage>();
            using (ngmdEntities db = new ngmdEntities())
            {
                IQueryable<productImage> query = db.productImage
                                                    .Include("websiteInventory")
                                                    .Include("websiteInventory.product")
                                                    .Include("websiteInventory.product.manufacturer")
                                                    .Include("websiteInventory.Website");

                query = query.OrderBy(p => p.websiteInventoryFK);

                query = SetWhereClause(query);
                productImagesList = query.ToList();
            }
            return productImagesList;
        }

        private IQueryable<productImage> SetWhereClause(IQueryable<productImage> query)
        {
            if (SelectedProductStatusFK > 0)
            {
                query = query.Where(x => x.websiteInventory.product.productStatusFK == SelectedProductStatusFK);
            }

            if (SelectedWebsiteFK > 0)
            {
                query = query.Where(x => x.websiteInventory.websiteFK == SelectedWebsiteFK);

                if (SelectedCategoryCodeFK > 0)
                {
                    query = query.Where(x => x.websiteInventory.categoryCodeFK == SelectedCategoryCodeFK);
                }
            }

            if (SelectedProductGroupFK > 0)
            {
                query = query.Where(x => x.websiteInventory.product.productGroupFK == SelectedProductGroupFK);
            }

            if (SelectedSalesAreaGroupFK > 0)
            {
                query = query.Where(x => x.websiteInventory.product.salesAreaGroupFK == SelectedSalesAreaGroupFK);
            }

            if (SelectedDataSupplierFK > 0)
            {
                query = query.Where(x => x.websiteInventory.product.dataSupplierFK == SelectedDataSupplierFK);
            }

            if (SelectedManufacturerFK > 0)
            {
                query = query.Where(x => x.websiteInventory.product.manufacturerFK == SelectedManufacturerFK);
            }

            return query;
        }

        public ExportProductImagesViewModel GetProductCount()
        {
            int count = 0;

            using (ngmdEntities db = new ngmdEntities())
            {
                IQueryable<productImage> query = db.productImage;
                query = SetWhereClause(query);
                count = query.Count();
            }

            ProductCount = count;
            return this;
        }

        public void DeleteFile(string filePath)
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }
}
