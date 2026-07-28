using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Import.ProviderInventory
{
    public class ImportProviderInventoryViewModel : JobStatusCommonViewModel
    {
        public string FilePath { get; set; }

        public void Import(string filePath)
        {
            DataTable dt = SharedFunctions.ReadTextFile(filePath);
            dt = GetAcceptedFields(dt);
            ProcessRows(dt);
        }

        private void ProcessRows(DataTable dt)
        {
            int currentRow = 1;
            List<ProviderInventoryImportFields> providerInventoryList = new List<ProviderInventoryImportFields>();

            foreach (DataRow dr in dt.Rows)
            {
                try
                {

                    ProviderInventoryImportFields fields = new ProviderInventoryImportFields();
                    fields.PartNo = Convert.ToString(dr["Part No"]);
                    fields.Description = dr.Table.Columns.Contains("Description") ? Convert.ToString(dr["Description"]) : null;
                    fields.Quantity = dr.Table.Columns.Contains("Quantity") ? Convert.ToInt32(dr["Quantity"]) : (int?)null;
                    fields.EffectiveDate = dr.Table.Columns.Contains("Effective Date") ? Convert.ToDateTime(dr["Effective Date"]) : (DateTime?)null;
                    fields.ProviderID = Convert.ToInt32(dr["Provider ID"]);
                    fields.ProviderPartNo = dr.Table.Columns.Contains("Provider Part No") ? Convert.ToString(dr["Provider Part No"]) : null;
                    fields.DateLastUpdate = dr.Table.Columns.Contains("Date Last Updated") ? Convert.ToDateTime(dr["Date Last Updated"]) : DateTime.Now;
                    fields.ManufacturerID = dr.Table.Columns.Contains("Manufacturer ID") ? Convert.ToInt32(dr["Manufacturer ID"]) : (int?)null;
                    fields.PotentialNewProduct = dr.Table.Columns.Contains("Potential New Product") ? Convert.ToBoolean(dr["Potential New Product"]) : (bool?)null;
                    fields.UnwantedProduct = dr.Table.Columns.Contains("Unwanted Product") ? Convert.ToBoolean(dr["Unwanted Product"]) : (bool?)null;
                    fields.UntrustedProvider = dr.Table.Columns.Contains("Untrusted Provider") ? Convert.ToBoolean(dr["Untrusted Provider"]) : (bool?)null;
                    fields.UNSPSCCode = dr.Table.Columns.Contains("UNSPSC Code") ? Convert.ToString(dr["UNSPSC Code"]) : null;
                    fields.UNSPSCClass = dr.Table.Columns.Contains("UNSPSC Class") ? Convert.ToString(dr["UNSPSC Class"]) : null;
                    fields.ProviderManuRef = dr.Table.Columns.Contains("Provider Manu Ref") ? Convert.ToString(dr["Provider Manu Ref"]) : null;
                    fields.Barcode = dr.Table.Columns.Contains("Barcode") ? Convert.ToString(dr["Barcode"]) : null;
                    providerInventoryList.Add(fields);
                }
                catch (Exception ex)
                {
                    var message = ErrorMessage(currentRow, ex);
                    Warnings.Add(message); // allow continued importing
                    WriteJobStatusRecord("Provider Inventory - Working", message, SavingErrorType.Validation);
                }
                finally
                {
                    currentRow++;
                }
            }

            dt = null;
            SaveRecords(providerInventoryList);
        }

        private void SaveRecordsToDB(List<ProviderInventoryImportFields> providerInventoryList)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                try
                {
                    db.Configuration.AutoDetectChangesEnabled = false;

                    for (int i = 0; i < providerInventoryList.Count; i++)
                    {
                        var partNo = providerInventoryList[i].PartNo;
                        var providerFK = providerInventoryList[i].ProviderID;

                        providerInventory pi = db.providerInventory.Where(x => x.partNo == partNo && x.providerFK == providerFK).FirstOrDefault();

                        if (pi != null)
                        {
                            pi.description = providerInventoryList[i].Description ?? pi.description;
                            pi.quantity = providerInventoryList[i].Quantity ?? pi.quantity;
                            pi.effectiveDate = providerInventoryList[i].EffectiveDate ?? pi.effectiveDate;
                            pi.dateLastUpdate = providerInventoryList[i].DateLastUpdate;
                            pi.providerPartNo = providerInventoryList[i].ProviderPartNo ?? pi.providerPartNo;
                            pi.manufacturerFK = providerInventoryList[i].ManufacturerID ?? pi.manufacturerFK;
                            pi.potentialNewProduct = providerInventoryList[i].PotentialNewProduct ?? pi.potentialNewProduct;
                            pi.unwantedProduct = providerInventoryList[i].UnwantedProduct ?? pi.unwantedProduct;
                            pi.untrustedProvider = providerInventoryList[i].UntrustedProvider ?? pi.untrustedProvider;
                            pi.unspscCode = providerInventoryList[i].UNSPSCCode ?? pi.unspscCode;
                            pi.unspscClass = providerInventoryList[i].UNSPSCClass ?? pi.unspscClass;
                            pi.providerManuRef = providerInventoryList[i].ProviderManuRef ?? pi.providerManuRef;
                            pi.barcode = providerInventoryList[i].Barcode ?? pi.barcode;
                        }
                    }
                    db.Configuration.AutoDetectChangesEnabled = true;
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    string errorString = "Could not save provider inventory entry";
                    SaveHadErrors = true;

                    WriteJobStatusRecord("Provider Inventory - Working", errorString, SavingErrorType.Saving);
                    WriteJobStatusRecord("Provider Inventory - Working", ex.Message, SavingErrorType.Saving);
                }
            }
        }

        private void SaveRecords(List<ProviderInventoryImportFields> providerInventoryList)
        {
            // Calling write from main thread, not worker thread here to avoid race condition.
            // This write must happen before the import product display status first displays on the main thread, 
            // or else the most recent job first seen can be the most recent, not the current.
            // This guarantees sequence on main thread of write new job to DB, then display view.
            WriteJobStatusRecord("Provider Inventory - Working", "", SavingErrorType.Saving);

            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                SaveRecordsToDB(providerInventoryList);

                if (SaveHadErrors)
                {
                    WriteJobStatusRecord("Provider Inventory - Complete", "", SavingErrorType.Saving);
                }
                else
                {
                    WriteJobStatusRecord("Provider Inventory - Complete", "Successfully Saved Provider Inventory", SavingErrorType.Saving);
                }
            }).Start();
        }

        public static bool DataTableColExists(DataRow dr, string colName)
        {
            return dr.Table.Columns.Contains(colName);
        }

        private DataTable GetAcceptedFields(DataTable csvData)
        {
            DataColumnCollection cols = csvData.Columns;

            try
            {
                Dictionary<int, string> mappedColumns = new Dictionary<int, string>();
                int colIndex = 0;

                foreach (string col in ProviderInventoryAcceptedFields.Fields)
                {
                    if (cols.Contains(col))
                    {
                        mappedColumns.Add(colIndex, col);
                        colIndex++;
                    }
                }

                if (mappedColumns.Count == 0)
                {
                    throw new Exception("The columns titles are not correct for this import type.");
                }

                csvData = new DataView(csvData).ToTable(false, mappedColumns.Select(x => x.Value).ToArray());
            }
            catch (Exception ex)
            {
                throw new ApplicationException(ex.Message);
            }

            return csvData;
        }
    }
}
