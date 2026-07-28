using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using netGiant.Intranet.DataLayer;
using System.Data.Entity;
using netGiant.Intranet.BusinessLayer.Utilities;
using System.Web.Mvc;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Import.Product
{
    public class ImportProductViewModel
    {
        public ImportProductViewModel()
        {
            Warnings = new List<string>();
            AllWebsites = SelectListViewModel.AllWebsites();
        }

        private static string axisLanguage = string.Empty;
        private delegate bool CheckValidation(object prop);
        public string FilePath { get; set; }
        public int WebsiteFK { get; set; }
        public List<string> Warnings { get; set; }
        public IQueryable<SelectListItem> AllWebsites { get; set; }

        public void Import()
        {
            DataTable dt = ReadAxisCsv();
            dt = GetAcceptedFields(dt);
            ProcessRows(dt);
        }

        private DataTable ReadAxisCsv()
        {
            DataTable dt = new DataTable();

            try
            {
                using (TextFieldParser csvReader = new TextFieldParser(FilePath, System.Text.Encoding.GetEncoding("ISO-8859-1")))
                using (StreamReader reader = new StreamReader(FilePath))
                {
                    csvReader.SetDelimiters(new string[] { FtpUtilities.DetectDelimiter(reader, File.ReadAllLines(FilePath).Count()).ToString() });
                    csvReader.TrimWhiteSpace = true;

                    //column headers
                    string[] colFields = csvReader.ReadFields();
                    foreach (string column in colFields)
                    {
                        DataColumn datecolumn = new DataColumn(column);
                        datecolumn.AllowDBNull = true;
                        dt.Columns.Add(datecolumn);
                    }

                    //column data
                    while (!csvReader.EndOfData)
                    {
                        string[] fieldData = csvReader.ReadFields();

                        //Making empty value as null
                        for (int i = 0; i < fieldData.Length; i++)
                        {
                            if (fieldData[i] == "")
                            {
                                fieldData[i] = null;
                            }
                        }

                        dt.Rows.Add(fieldData);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException(ex.Message);
            }

            return dt;
        }

        private DataTable GetAcceptedFields(DataTable csvData)
        {
            DataColumnCollection columns = csvData.Columns;

            try
            {
                Dictionary<int, string> mappedColumns = new Dictionary<int, string>();
                int colIndex = 0;

                foreach (string col in AcceptedFields.Fields)
                {
                    if (columns.Contains(col))
                    {
                        mappedColumns.Add(colIndex, col);
                        colIndex++;
                    }
                }

                csvData = new DataView(csvData).ToTable(false, mappedColumns.Select(x => x.Value).ToArray());
            }
            catch (Exception ex)
            {
                throw new ApplicationException(ex.Message);
            }

            return csvData;
        }

        private void ProcessRows(DataTable finalDt)
        {
            int currentRow = 1;
            List<ImportFields> validProductList = new List<ImportFields>();
            List<SkuMappingFields> validSkuMappingList = new List<SkuMappingFields>();
            List<eBusinessMappingFields> validEbusinessMappingList = new List<eBusinessMappingFields>();
            SetAxisLanguage();

            foreach (DataRow row in finalDt.Rows)
            {
                ImportFields prodFields = null;
                SkuMappingFields skuMapFields = null;
                eBusinessMappingFields eBusFields = null;

                try
                {
                    ExtractData.RecordType recType = ExtractData.ExtractRecordType(row);
                    if (recType == ExtractData.RecordType.Product)
                    {
                        prodFields = ExtractProduct(row, currentRow);
                        ValidateProduct(prodFields);
                        validProductList.Add(prodFields);
                    }
                    else if (recType == ExtractData.RecordType.SkuMapping)
                    {
                        skuMapFields = ExtractSkuMapping(row, currentRow);
                        ValidateSkuMapping(skuMapFields);
                        validSkuMappingList.Add(skuMapFields);
                    }
                    else if (recType == ExtractData.RecordType.eBusinessMapping)
                    {
                        eBusFields = ExtractEbusinessMapping(row, currentRow);
                        ValidateEbusinessMapping(eBusFields);
                        validEbusinessMappingList.Add(eBusFields);
                    }

                    currentRow++;
                }
                catch (Exception ex)
                {
                    string message = LogErrorString(currentRow, prodFields, ex);
                    throw new ApplicationException(message);
                }
            }

            finalDt = null;
            Save(validProductList, validSkuMappingList, validEbusinessMappingList);
        }

        private string LogErrorString(int currentRow, ImportFields prodFields, Exception ex)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("Error importing row " + currentRow + ".");
            if (prodFields != null)
            {
                sb.Append("Error Alt Ref - " + prodFields.partNo + ".");
            }
            sb.Append(" Error Message - " + ex.Message + ".");
            sb.Append(" File Processing Ended Due to Errors in the File.");
            sb.Append(" Re-Upload a Valid File.");

            return sb.ToString();
        }

        private void SetAxisLanguage()
        {
            switch (WebsiteFK)
            {
                case 1:
                    axisLanguage = "English";
                    break;
                case 2:
                    axisLanguage = "French";
                    break;
                case 3:
                    axisLanguage = "German";
                    break;
            }
        }

        private ImportFields ExtractProduct(DataRow row, int csvRow)
        {
            ExtractData exrct = new ExtractData(axisLanguage, WebsiteFK);
            ImportFields fields = exrct.ExtractKeyData(row, csvRow);

            exrct.ExtractProductName(row, fields);
            exrct.ExtractCategoryCode(row, fields);
            exrct.ExtractDiscontinued(row, fields);
            exrct.ExtractProductStatus(row, fields);
            exrct.ExtractProductGroup(row, fields);
            exrct.ExtractSalesAreaGroup(row, fields);
            exrct.ExtractDataSupplier(row, fields);
            exrct.ExtractStockReference(row, fields);
            exrct.ExtractStockRecordType(row, fields);
            exrct.ExtractSpecification(row, fields);
            exrct.ExtractAttributes(row, fields);
            exrct.ExtractMetaData(row, fields);
            exrct.ExtractEbusinessDetails(row, fields);
            exrct.ExtractReSaleable(row, fields);
            exrct.ExtractDefaultDelivery(row, fields);
            exrct.ExtractStockNotes(row, fields);
            exrct.ExtractFeedInfo(row, fields);
            exrct.ExtractGooglePromotionId(row, fields);
            exrct.ExtractPageYield(row, fields);
            exrct.ExtractBreakQuantities(row, fields);

            return fields;
        }

        private SkuMappingFields ExtractSkuMapping(DataRow row, int csvRow)
        {
            ExtractData exrct = new ExtractData(axisLanguage, WebsiteFK);
            return exrct.ExtractSkuMapping(row, csvRow);
        }

        private eBusinessMappingFields ExtractEbusinessMapping(DataRow row, int csvRow)
        {
            ExtractData exrct = new ExtractData(axisLanguage, WebsiteFK);
            return exrct.ExtractEbusinessFields(row, csvRow);
        }

        private void ValidateProduct(ImportFields prod)
        {
            if (prod.IsNew)
            {
                ValidTest(prod.productName, x => x == null || x.ToString().Length == 0, "Product Name not found");
                ValidTest(prod.productStatusFK, x => x == null, "Product Status not found");
                ValidTest(prod.productGroupFK, x => x == null, "Product Group not found");
                ValidTest(prod.salesAreaGroupFK, x => x == null, "Sales Area Group not found");
                ValidTest(prod.dataSupplierFK, x => x == null, "Data Supplier not found");
                ValidTest(prod.CategoryCodeFK, x => x == null, "Category Code is required for new products");
            }

            ValidTest(prod.productName, x => x != null && x.ToString().Length > 255, "Product Name exceeds 255 characters");
            ValidTest(prod.stockRef, x => x != null && x.ToString().Length == 0, "Stock ref is blank");
        }

        private void ValidateSkuMapping(SkuMappingFields skuMapFields)
        {
            ValidTest(skuMapFields.AltRef, x => x == null, "SkuMapping does not contain an Alt Ref");
            ValidTest(skuMapFields.AxisSupplierNo, x => x == null, "SkuMapping does not contain an axis supplier No.");
            ValidTest(skuMapFields.ProviderFK, x => x == null, "SkuMapping does not contain a provider ID");

            bool skuMappingExists = CheckSkuMappingExists(skuMapFields);
            ValidTest(skuMappingExists, x => Convert.ToBoolean(x) == true, "SkuMapping entry already exists for : " +
                                "Provider Part No.: " + skuMapFields.ProviderPartNo +
                                ", ProviderFK: " + skuMapFields.ProviderFK);
        }

        private void ValidateEbusinessMapping(eBusinessMappingFields eBusFields)
        {
            ValidTest(eBusFields.productFK, x => (int)x == 0, "eBusiness mapping does not have a productFK match in the PMS");
            ValidTest(eBusFields.eBusinessRef, x => string.IsNullOrEmpty(x.ToString()), "eBusinessRef not matched to the PMS");
        }

        private void ValidTest(object prop, CheckValidation validate, string errorMessage)
        {
            if (validate(prop))
            {
                throw new ApplicationException(errorMessage);
            }
        }

        private void Save(List<ImportFields> validProductList, 
            List<SkuMappingFields> validSkuMappingList,
            List<eBusinessMappingFields> validEbusinessMappingList)
        {
            SaveProducts(validProductList);
            SaveSkuMappings(validSkuMappingList);
            SaveEbusinessMappings(validEbusinessMappingList);
        }

        private void SaveSkuMappings(List<SkuMappingFields> validSkuMappingList)
        {
            foreach (SkuMappingFields sku in validSkuMappingList)
            {
                SaveRecords upd = new SaveRecords(WebsiteFK);

                try
                {
                    skuMapping skumapp = upd.CreateSkuMapping(sku);
                    upd.UpdateProviderInventory(skumapp);
                }
                catch (Exception ex)
                {
                    Warnings.Add("Could not create SkuMapping entry");
                    Warnings.Add(ex.Message);
                }
            }
        }

        private void SaveProducts(List<ImportFields> validProductList)
        {
            foreach (ImportFields prd in validProductList)
            {
                product dbProduct = GetOrigProd(prd);
                product origProduct = GetOrigProd(prd);

                SaveRecords upd = new SaveRecords(WebsiteFK);

                try
                {
                    dbProduct = upd.PopulateProduct(prd, dbProduct);
                    upd.PopulateWebsiteInventory(prd, dbProduct);
                }
                catch (Exception ex)
                {
                    Warnings.Add("Error populating Product table - " + prd.partNo);
                    Warnings.Add(ex.Message);
                }

                try
                {
                    upd.PopulateAxisFields(prd, dbProduct);
                }
                catch (Exception ex)
                {
                    Warnings.Add("Error populating Axis Fields tables - " + prd.partNo);
                    Warnings.Add(ex.Message);
                }

                if (!prd.IsNew)
                    try
                    {
                        upd.CompareFields(origProduct, dbProduct, prd);
                    }
                    catch (Exception ex)
                    {
                        Warnings.Add("Error updating Axis queue - " + prd.partNo);
                        Warnings.Add(ex.Message);
                    }
            }
        }

        private void SaveEbusinessMappings(List<eBusinessMappingFields> validEbusinessMappingList)
        {
            foreach (eBusinessMappingFields eBus in validEbusinessMappingList)
            {
                SaveRecords upd = new SaveRecords(WebsiteFK);

                try
                {
                    upd.SaveEbusinessMapping(eBus);
                }
                catch (Exception e)
                {
                    Warnings.Add("Could not create eBusinessMapping entry");
                    Warnings.Add(e.Message);
                }
            }
        }

        private product GetOrigProd(ImportFields prd)
        {
            product origProd = null;

            using (ngmdEntities db = new ngmdEntities())
            {
                origProd = db.product.Include("AxisFields.AxisFieldsAdditional")
                                        .Include(p => p.websiteInventory)
                                        .Where(x => x.partNo == prd.partNo &&
                                                        x.manufacturerFK == prd.manufacturerFK)
                                                        .FirstOrDefault();
            }

            return origProd;
        }

        private bool CheckSkuMappingExists(SkuMappingFields sku)
        {
            bool exists = false;

            using (ngmdEntities db = new ngmdEntities())
            {
                skuMapping sk = db.skuMapping.Where(x => x.providerFK == sku.ProviderFK &&
                                                    x.providerPartNo == sku.ProviderPartNo)
                                                    .FirstOrDefault();
                if (sk != null)
                {
                    exists = true;
                }
            }

            return exists;
        }

        public void DeleteFile(string filePath)
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }
}
