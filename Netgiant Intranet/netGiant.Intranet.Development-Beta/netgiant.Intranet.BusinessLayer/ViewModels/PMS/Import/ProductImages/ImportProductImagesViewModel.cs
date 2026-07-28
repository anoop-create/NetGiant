using Microsoft.VisualBasic.FileIO;
using netGiant.Intranet.BusinessLayer.Utilities;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Import.ProductImages
{
    public class ImportProductImagesViewModel : JobStatusCommonViewModel
    {
        private delegate bool CheckValidation(object prop);
        public string FilePath { get; set; }

        public void Import()
        {
            DataTable dt = ReadCsv();
            dt = GetAcceptedFields(dt);
            ProcessRows(dt);
        }

        private DataTable ReadCsv()
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

        private List<List<ProductImagesImportFields>> CreateListOfImagesPerWebsiteInventory(List<ProductImagesImportFields> validProductImagesList)
        {
            var orderedProductImagesList = validProductImagesList.OrderBy(x => x.websiteInventoryFK); // -> FK1, FK1, FK1, FK2, FK2, FK3 ...

            int currentWebsiteInventoryFK = 0;

            // will have list of FK1, list of FK2 etc
            List<List<ProductImagesImportFields>> productImagesListsByWebsiteInventoryFK = new List<List<ProductImagesImportFields>>();

            List<ProductImagesImportFields> productImagesListCurrentWebsiteInventoryFK = null;

            foreach (ProductImagesImportFields prd in orderedProductImagesList)
            {
                if (currentWebsiteInventoryFK != prd.websiteInventoryFK)
                {
                    productImagesListCurrentWebsiteInventoryFK = new List<ProductImagesImportFields>();
                    productImagesListsByWebsiteInventoryFK.Add(productImagesListCurrentWebsiteInventoryFK); // add new list

                    productImagesListCurrentWebsiteInventoryFK.Add(prd);

                    currentWebsiteInventoryFK = prd.websiteInventoryFK;
                }
                else
                {
                    productImagesListCurrentWebsiteInventoryFK.Add(prd);
                }
            }

            return productImagesListsByWebsiteInventoryFK;
        }

        private void EnsureOnlyOneMainEachOfFullAndThumbnailImages(List<List<ProductImagesImportFields>> productImagesListsByWebsiteInventoryFK)
        {
            // currentWebsiteInventoryList in loop is list of FK1, then list of FK2. Each list = 1 website inventory
            // Descending order, as items may be removed. Can't use foreach
            for (int i = productImagesListsByWebsiteInventoryFK.Count() - 1; i >= 0; i--)
            {
                if (productImagesListsByWebsiteInventoryFK[i].Count() > 0)
                {
                    websiteInventory wi = GetOrigWebsiteInventory(productImagesListsByWebsiteInventoryFK[i][0]);
                    try
                    {
                        // whole lists can be removed during validation
                        int mainFullImagesCount = productImagesListsByWebsiteInventoryFK[i].Where(x => x.isMain && !x.isThumbnail).Count();
                        if (mainFullImagesCount > 1)
                        {
                            productImagesListsByWebsiteInventoryFK.Remove(productImagesListsByWebsiteInventoryFK[i]);
                            throw new Exception("More than 1 main full image - Alt Ref " + wi.product.partNo);
                        }
                        if (mainFullImagesCount == 0)
                        {
                            productImagesListsByWebsiteInventoryFK.Remove(productImagesListsByWebsiteInventoryFK[i]);
                            throw new Exception("Missing a main full image - Alt Ref " + wi.product.partNo);
                        }

                        int mainThumbnailImagesCount = productImagesListsByWebsiteInventoryFK[i].Where(x => x.isMain && x.isThumbnail).Count();
                        if (mainThumbnailImagesCount > 1)
                        {
                            productImagesListsByWebsiteInventoryFK.Remove(productImagesListsByWebsiteInventoryFK[i]);
                            throw new Exception("More than 1 main thumbnail image - Alt Ref " + wi.product.partNo);
                        }
                        if (mainThumbnailImagesCount == 0)
                        {
                            productImagesListsByWebsiteInventoryFK.Remove(productImagesListsByWebsiteInventoryFK[i]);
                            throw new Exception("Missing a main thumbnail image - Alt Ref " + wi.product.partNo);
                        }
                    }
                    catch (Exception ex)
                    {
                        Warnings.Add(ex.Message); // allow continued importing
                        WriteJobStatusRecord("Product Images - Working", ex.Message, SavingErrorType.Validation);
                    }
                }
            }
        }

        private void ProcessRows(DataTable finalDt)
        {
            int currentRow = 1;
            List<ProductImagesImportFields> validProductImagesList = new List<ProductImagesImportFields>();

            foreach (DataRow row in finalDt.Rows)
            {
                ProductImagesImportFields prodFields = null;

                try
                {
                    prodFields = ExtractProductImage(row, currentRow);
                    ValidateProductImage(prodFields);
                    validProductImagesList.Add(prodFields);
                }
                catch (Exception ex)
                {
                    string message = BuildErrorString(currentRow, prodFields, ex);
                    Warnings.Add(message); // allow continued importing
                    WriteJobStatusRecord("Product Images - Working", message, SavingErrorType.Validation);
                }
                finally
                {
                    currentRow++;
                }
            }

            finalDt = null;

            List<List<ProductImagesImportFields>> productImagesListsByWebsiteInventoryFK = CreateListOfImagesPerWebsiteInventory(validProductImagesList);

            //EnsureOnlyOneMainEachOfFullAndThumbnailImages(productImagesListsByWebsiteInventoryFK);

            Save(productImagesListsByWebsiteInventoryFK);
        }

        private string BuildErrorString(int currentRow, ProductImagesImportFields prodFields, Exception ex)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("Error importing row " + currentRow + ".");
            if (prodFields != null && !string.IsNullOrEmpty(prodFields.altRef))
            {
                sb.Append("Error Alt Ref - " + prodFields.altRef + ".");
            }
            sb.Append(" Error Message - " + ex.Message + ".");
            sb.Append(" Re-Import a Valid Line.");

            return sb.ToString();
        }

        private ProductImagesImportFields ExtractProductImage(DataRow row, int csvRow)
        {
            ExtractData exrct = new ExtractData();
            ProductImagesImportFields fields = exrct.ExtractImageData(row, csvRow);
            return fields;
        }

        private void ValidateProductImage(ProductImagesImportFields prod)
        {
            ValidTest(prod.URL, x => x != null && x.ToString().Length > 800, "URL exceeds 800 characters");
        }

        private void ValidTest(object prop, CheckValidation validate, string errorMessage)
        {
            if (validate(prop))
            {
                throw new ApplicationException(errorMessage);
            }
        }
        
        private void Save(List<List<ProductImagesImportFields>> productImagesListsByWebsiteInventoryFK)
        {
            // Calling write from main thread, not worker thread here to avoid race condition.
            // This write must happen before the import product display status first displays on the main thread, 
            // or else the most recent job first seen can be the most recent, not the current.
            // This guarantees sequence on main thread of write new job to DB, then display view.
            WriteJobStatusRecord("Product Images - Working", "", SavingErrorType.Saving);

            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                SaveProductImages(productImagesListsByWebsiteInventoryFK);

                if (SaveHadErrors)
                {
                    WriteJobStatusRecord("Product Images - Complete", "", SavingErrorType.Saving);
                }
                else
                {
                    WriteJobStatusRecord("Product Images - Complete", "Successfully Amended Product Images", SavingErrorType.Saving);
                }
            }).Start();
        }

        private void SaveProductImages(List<List<ProductImagesImportFields>> productImagesListsByWebsiteInventoryFK)
        {
            foreach (var currentWebsiteInventoryList in productImagesListsByWebsiteInventoryFK)
            {
                if (currentWebsiteInventoryList.Count() > 0)
                {
                    websiteInventory wi = GetOrigWebsiteInventory(currentWebsiteInventoryList[0]);

                    SaveRecords upd = new SaveRecords();

                    try
                    {
                        upd.UpdateWebsiteInventoryProductImage(currentWebsiteInventoryList, wi);
                    }
                    catch (Exception ex)
                    {
                        string errorString = "";
                        errorString = "Error populating Product table - Alt Ref " + wi.product.partNo;

                        SaveHadErrors = true;

                        WriteJobStatusRecord("Product Images - Working", errorString, SavingErrorType.Saving);
                        WriteJobStatusRecord("Product Images - Working", ex.Message, SavingErrorType.Saving);
                    }
                }
            }
        }

        private websiteInventory GetOrigWebsiteInventory(ProductImagesImportFields prd)
        {
            websiteInventory origWi = null;

            using (ngmdEntities db = new ngmdEntities())
            {
                int productFK = db.product.Where(x => x.partNo == prd.altRef).FirstOrDefault().productID;
                origWi = db.websiteInventory
                            .Include(x => x.productImage)
                            .Where(x => x.websiteFK == prd.websiteFK && x.productFK == productFK)
                            .FirstOrDefault();
            }

            return origWi;
        }
    }
}
