using Microsoft.VisualBasic.FileIO;
using NGBP.DataAccessLayer.DataUtilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using netGiant.Intranet.DataLayer;
using System.Reflection;
using System.Data.Entity;

namespace ngBatchProcesses.BusinessObjects.Product.Import
{
    public class ImportProduct
    {
        static ImportProduct()
        {
            stnFunc = new Shared.StandardFunctions();
            //hasErrorOccurred = false;
        }

        private static Shared.StandardFunctions stnFunc;
        //private static bool hasErrorOccurred;
        private static int websiteFK;
        private static string axisLanguage = string.Empty;
        private delegate bool CheckValidation(object prop);

        public void Import(int _websiteFK)
        {
            websiteFK = _websiteFK;
            stnFunc.AddToActivityLog("Batch Program started with switch - loadproducts and websiteFK=" + _websiteFK);
            DataTable dt = ReadAxisCsv();
            dt = GetAcceptedFields(dt);
            ProcessRows(dt);
            Console.WriteLine(stnFunc.LogActivity());
        }

        private static DataTable ReadAxisCsv()
        {
            DataTable dt = new DataTable();

            string filePath = string.Empty;

            switch (websiteFK)
            {
                case 1:
                    filePath = @"D:\ProductFieldData\tgPacks.csv";
                    break;
                case 2:
                    filePath = @"D:\ProductFieldData\cmPacks.csv";
                    break;
                case 3:
                    filePath = @"D:\ProductFieldData\ngPacks.csv";
                    break;
            }

            stnFunc.AddToActivityLog("Reading CSV File - " + filePath);

            try
            {
                using (TextFieldParser csvReader = new TextFieldParser(filePath, Encoding.GetEncoding("ISO-8859-1")))
                using (StreamReader reader = new StreamReader(filePath))
                {
                    csvReader.SetDelimiters(new string[] { FtpUtilities.DetectDelimiter(reader, File.ReadAllLines(filePath).Count()).ToString() });
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
                stnFunc.AddToActivityLog("***Error*** Unable to download data from " + filePath);
                stnFunc.AddToActivityLog("Message: " + ex.Message);
                stnFunc.AddToActivityLog("Stack Trace: " + ex.StackTrace);
                //hasErrorOccurred = true;
            }

            return dt;
        }

        private static DataTable GetAcceptedFields(DataTable csvData)
        {
            stnFunc.AddToActivityLog("Getting Accepted Fields" + Environment.NewLine);
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
                stnFunc.AddToActivityLog("***Error*** Unable to get desired column data. Either column name is missing or column name is not spelled right.");
                stnFunc.AddToActivityLog("Message: " + ex.Message);
                stnFunc.AddToActivityLog("Stack Trace: " + ex.StackTrace);
                //hasErrorOccurred = true;
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
                ////SkuMappingFields skuMapFields = null;
                ///eBusinessMappingFields eBusFields = null;

                stnFunc.AddToActivityLog("Updating " + row["Alt Ref"].ToString());

                try
                {
                    ExtractData.RecordType recType = ExtractData.ExtractRecordType(row);
                    if (recType == ExtractData.RecordType.Product)
                    {
                        prodFields = ExtractProduct(row, currentRow);
                        //ValidateProduct(prodFields);
                        validProductList.Add(prodFields);
                    }
                    //else if (recType == ExtractData.RecordType.SkuMapping)
                    //{
                    //    skuMapFields = ExtractSkuMapping(row, currentRow);
                    //    ValidateSkuMapping(skuMapFields);
                    //    validSkuMappingList.Add(skuMapFields);
                    //}
                    //else if (recType == ExtractData.RecordType.eBusinessMapping)
                    //{
                    //    eBusFields = ExtractEbusinessMapping(row, currentRow);
                    //    ValidateEbusinessMapping(eBusFields);
                    //    validEbusinessMappingList.Add(eBusFields);
                    //}

                    stnFunc.AddToActivityLog("Updating " + row["Alt Ref"].ToString());
                    currentRow++;
                }
                catch (Exception ex)
                {
                    string message = LogErrorString(currentRow, prodFields, ex);
                    //throw new ApplicationException(message);
                }
            }

            finalDt = null;
            Save(validProductList, validSkuMappingList, validEbusinessMappingList);

        }

        private eBusinessMappingFields ExtractEbusinessMapping(DataRow row, int csvRow)
        {
            ExtractData exrct = new ExtractData(axisLanguage, websiteFK);
            return exrct.ExtractEbusinessFields(row, csvRow);
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

        private static void SetAxisLanguage()
        {
            switch (websiteFK)
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

        private static ImportFields ExtractProduct(DataRow row, int csvRow)
        {
            ExtractData exrct = new ExtractData(axisLanguage, websiteFK);
            ImportFields fields = exrct.ExtractKeyData(row, csvRow);

            exrct.ExtractProductName(row, fields);
            exrct.ExtractCategoryCode(row, fields);
            exrct.ExtractDiscontinued(row, fields);
            exrct.ExtractUnspsc(row, fields);
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

            return fields;
        }

        private static SkuMappingFields ExtractSkuMapping(DataRow row, int csvRow)
        {
            ExtractData exrct = new ExtractData(axisLanguage, websiteFK);
            return exrct.ExtractSkuMapping(row, csvRow);
        }

        private void ValidateProduct(ImportFields prod)
        {
            if (prod.IsNew)
            {
                ValidTest(prod.productName, x => x == null || x.ToString().Length == 0, "Product Name not found");
                ValidTest(prod.unspscCode, x => x == null || x.ToString().Length == 0, "UNSPSC Code not found");
                ValidTest(prod.productStatusFK, x => x == null, "Product Status not found");
                ValidTest(prod.productGroupFK, x => x == null, "Product Group not found");
                ValidTest(prod.salesAreaGroupFK, x => x == null, "Sales Area Group not found");
                ValidTest(prod.dataSupplierFK, x => x == null, "Data Supplier not found");
                ValidTest(prod.CategoryCodeFK, x => x == null, "Category Code is required for new products");
            }

            ValidTest(prod.productName, x => x != null && x.ToString().Length > 255, "Product Name exceeds 255 characters");
            ValidTest(prod.unspscCode, x => x != null && x.ToString().Length > 100, "UNSPSC Code exceeds 100 characters");
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
            //SaveSkuMappings(validSkuMappingList);
            //SaveEbusinessMappings(validEbusinessMappingList);
        }

        private void SaveSkuMappings(List<SkuMappingFields> validSkuMappingList)
        {
            foreach (SkuMappingFields sku in validSkuMappingList)
            {
                SaveRecords upd = new SaveRecords(websiteFK);

                try
                {
                    skuMapping skumapp = upd.CreateSkuMapping(sku);
                    upd.UpdateProviderInventory(skumapp);
                }
                catch (Exception)
                {
                    //Warnings.Add("Could not create SkuMapping entry");
                    //Warnings.Add(ex.Message);
                }
            }
        }

        private void SaveProducts(List<ImportFields> validProductList)
        {
            foreach (ImportFields prd in validProductList)
            {
                product dbProduct = GetOrigProd(prd);
                product origProduct = GetOrigProd(prd);

                SaveRecords upd = new SaveRecords(websiteFK);

                try
                {
                    dbProduct = upd.PopulateProduct(prd, dbProduct);
                    upd.PopulateWebsiteInventory(prd, dbProduct);
                }
                catch (Exception)
                {
                    //Warnings.Add("Error populating Product table - " + prd.partNo);
                    //Warnings.Add(ex.Message);
                }

                try
                {
                    upd.PopulateAxisFields(prd, dbProduct);
                }
                catch (Exception)
                {
                    //throw new ApplicationException(ex.Message);
                    //Warnings.Add("Error populating Axis Fields tables - " + prd.partNo);
                    //Warnings.Add(ex.Message);
                }

                if (!prd.IsNew)
                    try
                    {
                        upd.CompareFields(origProduct, dbProduct, prd);
                    }
                    catch (Exception )
                    {
                        //Warnings.Add("Error updating Axis queue - " + prd.partNo);
                        //Warnings.Add(ex.Message);
                    }
            }
        }

        private void SaveEbusinessMappings(List<eBusinessMappingFields> validEbusinessMappingList)
        {
            foreach (eBusinessMappingFields eBus in validEbusinessMappingList)
            {
                SaveRecords upd = new SaveRecords(websiteFK);

                try
                {
                    upd.SaveEbusinessMapping(eBus);
                }
                catch (Exception)
                {
                    //Warnings.Add("Could not create eBusinessMapping entry");
                    //Warnings.Add(e.Message);
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
