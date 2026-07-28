using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System.Data.Entity;
using netGiant.Intranet.BusinessLayer.Utilities;
using System.Threading;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Import.Product
{
    public class ImportProductViewModel : JobStatusCommonViewModel
    {
        private static string axisLanguage = string.Empty;
        private delegate bool CheckValidation(object prop);
        public string FilePath { get; set; }
        public int WebsiteFK { get; set; }
        public int ImportPrimaryKey { get; set; }
        public bool IsSecondaryCrossSell { get; set; } = false;

        public void Import()
        {
            DataTable dt = ReadAxisCsv();
            dt = GetAcceptedFields(dt);
            if (IsSecondaryCrossSell)
            {
                ProcessecondaryCrossSellGroup(dt);
            }
            else
            {
                ProcessRows(dt);
            }
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

                foreach (string col in ProductAcceptedFields.Fields)
                {
                    if (columns.Contains(col))
                    {
                        if (col == "Secondary Cross Sell Group")
                        {
                            IsSecondaryCrossSell = true;
                        }
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

        private void ProcessecondaryCrossSellGroup(DataTable dt)
        {
            var list = new List<ProductFields>();

            foreach (DataRow dr in dt.Rows)
            {
                var fields = new ProductFields();
                fields.AltRef = Convert.ToString(dr["Alt Ref"]);
                fields.SecondaryCrossSellGroup = string.IsNullOrEmpty(Convert.ToString(dr["Secondary Cross Sell Group"])) ? (int?)null : Convert.ToInt32(dr["Secondary Cross Sell Group"]);
                list.Add(fields);
            }

            WriteJobStatusRecord("Product Secondary Cross Sell - Working", "", SavingErrorType.Saving);

            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                UpdateSecondaryCrossSellGroups(list);

                if (SaveHadErrors)
                {
                    WriteJobStatusRecord("Product Secondary Cross Sell - Complete", "", SavingErrorType.Saving);
                }
                else
                {
                    WriteJobStatusRecord("Product Secondary Cross Sell - Complete", "Successfully Saved Product Secondary Cross Sell Groups", SavingErrorType.Saving);
                }
            }).Start();
        }

        private void UpdateSecondaryCrossSellGroups(List<ProductFields> list)
        {
            using (var db = new ngmdEntities())
            {
                try
                {
                    db.Configuration.AutoDetectChangesEnabled = false;

                    for (int i = 0; i < list.Count; i++)
                    {
                        string altref = list[i].AltRef;

                        var product = db.product.Where(x => x.partNo == altref && (x.productStatusFK == 1 || x.productStatusFK == 8))
                                                .FirstOrDefault();

                        if (product == null) continue;

                        product.secondaryCrossSellGroupIdent = list[i].SecondaryCrossSellGroup;
                    }

                    db.Configuration.AutoDetectChangesEnabled = true;
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    string errorString = "Could not save product secondary cross sell group";
                    SaveHadErrors = true;

                    WriteJobStatusRecord("Product Secondary Cross Sell - Working", errorString, SavingErrorType.Saving);
                    WriteJobStatusRecord("Product Secondary Cross Sell - Working", ex.Message, SavingErrorType.Saving);
                }
            }
        }

        private void ProcessRows(DataTable finalDt)
        {
            int currentRow = 1;
            var validProductList = new List<ProductFields>();
            List<SkuMappingFields> validSkuMappingList = new List<SkuMappingFields>();
            List<eBusinessMappingFields> validEbusinessMappingList = new List<eBusinessMappingFields>();
            SetAxisLanguage();

            foreach (DataRow row in finalDt.Rows)
            {
                ProductFields prodFields = null;
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
                }
                catch (Exception ex)
                {
                    string message = BuildErrorString(currentRow, prodFields, ex);
                    Warnings.Add(message); // allow continued importing
                    WriteJobStatusRecord("Products - Working", message, SavingErrorType.Validation);
                }
                finally
                {
                    currentRow++;
                }
            }

            finalDt = null;
            Save(validProductList, validSkuMappingList, validEbusinessMappingList);
        }

        private string BuildErrorString(int currentRow, ProductFields prodFields, Exception ex)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("Error importing row " + currentRow + ".");
            if (prodFields != null && !string.IsNullOrEmpty(prodFields.PartNo))
            {
                if (ImportPrimaryKey == 1)
                    sb.Append("Error Alt Ref - " + prodFields.PartNo + ".");
                if (ImportPrimaryKey == 2)
                    sb.Append("Error Stock Ref - " + prodFields.StockRef + ".");
            }
            sb.Append(" Error Message - " + ex.Message + ".");
            sb.Append(" Re-Import a Valid Line.");

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

        private ProductFields ExtractProduct(DataRow row, int csvRow)
        {
            ExtractData exrct = new ExtractData(axisLanguage, WebsiteFK, ImportPrimaryKey);
            ProductFields fields = exrct.ExtractKeyData(row, csvRow);

            exrct.ExtractProductName(row, fields);
            exrct.ExtractDiscontinued(row, fields);
            exrct.ExtractProductStatus(row, fields);
            exrct.ExtractProductGroup(row, fields);
            exrct.ExtractSalesAreaGroup(row, fields);
            exrct.ExtractDataSupplier(row, fields);
            exrct.ExtractStockRecordType(row, fields);
            exrct.ExtractSpecification(row, fields);
            exrct.ExtractAttributes(row, fields);
            exrct.ExtractMetaData(row, fields);
            exrct.ExtractEbusinessDetails(row, fields);
            exrct.ExtractReSaleable(row, fields);
            exrct.ExtractDefaultDelivery(row, fields);
            exrct.ExtractNotes(row, fields);
            exrct.ExtractFeedInfo(row, fields);
            exrct.ExtractGooglePromotionId(row, fields);
            exrct.ExtractPageYield(row, fields);
            exrct.ExtractCapacity(row, fields);
            exrct.ExtractBreakQuantities(row, fields);
            exrct.ExtractBarcode(row, fields);
            exrct.ExtractAssemblyComponents(row, fields);
            exrct.ExtractProductCrossSellGroup(row, fields);

            return fields;
        }

        private SkuMappingFields ExtractSkuMapping(DataRow row, int csvRow)
        {
            ExtractData exrct = new ExtractData(axisLanguage, WebsiteFK, ImportPrimaryKey);
            return exrct.ExtractSkuMapping(row, csvRow);
        }

        private eBusinessMappingFields ExtractEbusinessMapping(DataRow row, int csvRow)
        {
            ExtractData exrct = new ExtractData(axisLanguage, WebsiteFK, ImportPrimaryKey);
            return exrct.ExtractEbusinessFields(row, csvRow);
        }

        private void ValidateProduct(ProductFields prod)
        {
            if (prod.IsNew)
            {
                ValidTest(prod.ProductName, x => x == null || x.ToString().Length == 0, "Product Name not found");
                ValidTest(prod.ProductStatusFK, x => x == null, "Product Status not found");
                ValidTest(prod.ProductGroupFK, x => x == null, "Product Group not found");
                ValidTest(prod.SalesAreaGroupFK, x => x == null, "Sales Area Group not found");
                ValidTest(prod.DataSupplierFK, x => x == null, "Data Supplier not found");
            }

            ValidTest(prod.ProductName, x => x != null && x.ToString().Length > 255, "Product Name exceeds 255 characters");
            ValidTest(prod.StockRef, x => x != null && x.ToString().Length == 0, "Stock ref is blank");
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

        private void Save(List<ProductFields> validProductList, 
            List<SkuMappingFields> validSkuMappingList,
            List<eBusinessMappingFields> validEbusinessMappingList)
        {
            // Calling write from main thread, not worker thread here to avoid race condition.
            // This write must happen before the import product display status first displays on the main thread, 
            // or else the most recent job first seen can be the most recent, not the current.
            // This guarantees sequence on main thread of write new job to DB, then display view.
            WriteJobStatusRecord("Products - Working", "", SavingErrorType.Saving);

            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                SaveProducts(validProductList);
                SaveSkuMappings(validSkuMappingList);
                SaveEbusinessMappings(validEbusinessMappingList);

                if (SaveHadErrors)
                {
                    WriteJobStatusRecord("Products - Complete", "", SavingErrorType.Saving);
                }
                else
                {
                    WriteJobStatusRecord("Products - Complete", "Successfully Saved Products", SavingErrorType.Saving);
                }
            }).Start();
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
                    string errorString = "Could not create SkuMapping entry";
                    SaveHadErrors = true;

                    WriteJobStatusRecord("Products - Working", errorString, SavingErrorType.Saving);
                    WriteJobStatusRecord("Products - Working", ex.Message, SavingErrorType.Saving);
                }
            }
        }

        private void SaveProducts(List<ProductFields> validProductList)
        {
            foreach (ProductFields prd in validProductList)
            {
                product dbProduct = GetOrigProd(prd);
                product origProduct = GetOrigProd(prd);

                SaveRecords upd = new SaveRecords(WebsiteFK, ImportPrimaryKey);

                try
                {
                    dbProduct = upd.PopulateProduct(prd, dbProduct);
                }
                catch (Exception ex)
                {
                    string errorString = "";
                    if (ImportPrimaryKey == 1)
                        errorString = "Error populating Product table - Alt Ref " + prd.PartNo;
                    else if (ImportPrimaryKey == 2)
                        errorString = "Error populating Product table - Stock Ref " + prd.StockRef;

                    SaveHadErrors = true;

                    WriteJobStatusRecord("Products - Working", errorString, SavingErrorType.Saving);
                    WriteJobStatusRecord("Products - Working", ex.Message, SavingErrorType.Saving);
                }

                try
                {
                    upd.PopulateAxisFields(prd, dbProduct);
                }
                catch (Exception ex)
                {
                    string errorString = "";
                    if (ImportPrimaryKey == 1)
                        errorString = "Error populating Axis Fields tables - Alt Ref " + prd.PartNo;
                    else if (ImportPrimaryKey == 2)
                        errorString = "Error populating Axis Fields tables - Stock Ref " + prd.StockRef;

                    SaveHadErrors = true;

                    WriteJobStatusRecord("Products - Working", errorString, SavingErrorType.Saving);
                    WriteJobStatusRecord("Products - Working", ex.Message, SavingErrorType.Saving);
                }

                if (!prd.IsNew)
                    try
                    {
                        upd.CompareFields(origProduct, dbProduct, prd);
                    }
                    catch (Exception ex)
                    {
                        string errorString = "";
                        if (ImportPrimaryKey == 1)
                            errorString = "Error updating Axis queue - Alt Ref " + prd.PartNo;
                        else if (ImportPrimaryKey == 2)
                            errorString = "Error updating Axis queue - Alt Ref " + prd.StockRef;

                        SaveHadErrors = true;

                        WriteJobStatusRecord("Products - Working", errorString, SavingErrorType.Saving);
                        WriteJobStatusRecord("Products - Working", ex.Message, SavingErrorType.Saving);
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
                    string errorString = "Could not create eBusinessMapping entry";

                    SaveHadErrors = true;

                    WriteJobStatusRecord("Products - Working", errorString, SavingErrorType.Saving);
                    WriteJobStatusRecord("Products - Working", e.Message, SavingErrorType.Saving);
                }
            }
        }

        private product GetOrigProd(ProductFields prd)
        {
            product origProd = null;

            using (ngmdEntities db = new ngmdEntities())
            {
                if (ImportPrimaryKey == 1)
                {
                    origProd = db.product.Include("AxisFields.AxisFieldsAdditional")
                                            .Include(p => p.websiteInventory)
                                            .Include(p => p.assemblyComponent)
                                            .Where(x => x.partNo == prd.PartNo &&
                                                            x.manufacturerFK == prd.ManufacturerFK)
                                                            .FirstOrDefault();
                }
                else if (ImportPrimaryKey == 2)
                {
                    origProd = db.product.Include("AxisFields.AxisFieldsAdditional")
                                            .Include(p => p.websiteInventory)
                                            .Include(p => p.assemblyComponent)
                                            .Where(x => x.AxisFields != null && x.AxisFields.stockReference == prd.StockRef &&
                                                            x.manufacturerFK == prd.ManufacturerFK)
                                                            .FirstOrDefault();
                }
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
    }
}
