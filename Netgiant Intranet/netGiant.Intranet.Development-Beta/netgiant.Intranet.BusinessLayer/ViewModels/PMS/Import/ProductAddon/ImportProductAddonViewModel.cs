using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Web;
using EntityState = System.Data.Entity.EntityState;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Import
{
    // Bulk import for the ProductAddon ("Find Out More" / basket cross-sell) mappings.
    // CSV has two columns:
    //   "Product SKU"  - the product the add-ons are attached to - either its AltRef/PartNo
    //                    OR its exact Product Name
    //   "Add On SKUs"  - one or more add-ons for that product, each either an AltRef/PartNo or
    //                    an exact Product Name, separated by a comma (,) in a single cell,
    //                    e.g. "AB123,Some Product Name,EF789"
    //                    (a semicolon is also still accepted for backwards compatibility with
    //                    previously-exported/saved files)
    //
    // Behaviour: for a given Product row, the add-ons listed become the COMPLETE set of
    // add-ons for that product - anything previously configured that is not in the list is
    // removed, and DisplayOrder is set to match the left-to-right order in the cell. This
    // mirrors ExportProductAddonViewModel's output so an export -> edit -> re-import round-trip
    // (including removing an add-on by deleting it from the cell) works cleanly.
    public class ImportProductAddonViewModel : JobStatusCommonViewModel
    {
        private const char AddOnSeparator = ',';
        private string userName;

        public void Import(string filePath)
        {
            // Capture the current user on the request thread - HttpContext.Current is not
            // available once SaveRecords hands off to the background Thread below.
            userName = (HttpContext.Current != null && HttpContext.Current.User != null && !string.IsNullOrEmpty(HttpContext.Current.User.Identity.Name))
                ? HttpContext.Current.User.Identity.Name
                : "Import";

            DataTable dt = SharedFunctions.ReadTextFile(filePath);
            dt = GetAcceptedFields(dt);
            ProcessRows(dt);
        }

        private DataTable GetAcceptedFields(DataTable csvData)
        {
            DataColumnCollection cols = csvData.Columns;

            try
            {
                Dictionary<int, string> mappedColumns = new Dictionary<int, string>();
                int colIndex = 0;

                foreach (string col in ProductAddonAcceptedFields.Fields)
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

        private void ProcessRows(DataTable dt)
        {
            int currentRow = 1;
            var productAddonList = new List<ProductAddonImportFields>();

            foreach (DataRow dr in dt.Rows)
            {
                try
                {
                    if (DataTableColExists(dr, "Product SKU"))
                    {
                        var fields = new ProductAddonImportFields();
                        fields.ProductSKU = Convert.ToString(dr["Product SKU"]);
                        fields.AddOnSKUs = DataTableColExists(dr, "Add On SKUs") ? Convert.ToString(dr["Add On SKUs"]) : "";

                        if (string.IsNullOrWhiteSpace(fields.ProductSKU))
                        {
                            string message = "Row " + currentRow + ": Product SKU is blank - row skipped.";
                            Warnings.Add(message);
                            WriteJobStatusRecord("Product Add Ons - Working", message, SavingErrorType.Validation);
                        }
                        else
                        {
                            productAddonList.Add(fields);
                        }
                    }
                    currentRow++;
                }
                catch (Exception ex)
                {
                    var message = ErrorMessage(currentRow, ex);
                    Warnings.Add(message);
                    WriteJobStatusRecord("Product Add Ons - Working", message, SavingErrorType.Validation);
                }
            }

            dt = null;
            SaveRecords(productAddonList);
        }

        private void SaveRecords(List<ProductAddonImportFields> productAddonList)
        {
            WriteJobStatusRecord("Product Add Ons - Working", "", SavingErrorType.Saving);

            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                Save(productAddonList);

                if (SaveHadErrors)
                {
                    WriteJobStatusRecord("Product Add Ons - Complete", "", SavingErrorType.Saving);
                }
                else
                {
                    WriteJobStatusRecord("Product Add Ons - Complete", "Successfully Saved Product Add Ons", SavingErrorType.Saving);
                }
            }).Start();
        }

        private void Save(List<ProductAddonImportFields> productAddonList)
        {
            using (var db = new ngmdEntities())
            {
                for (int i = 0; i < productAddonList.Count; i++)
                {
                    try
                    {
                        string productIdentifier = productAddonList[i].ProductSKU;
                        int productId = FindProductId(db, productIdentifier);

                        if (productId == 0)
                        {
                            string message = "Product '" + productIdentifier + "' was not found (checked SKU and Product Name) - row skipped.";
                            Warnings.Add(message);
                            WriteJobStatusRecord("Product Add Ons - Working", message, SavingErrorType.Validation);
                            continue;
                        }

                        List<string> addOnIdentifiers = (productAddonList[i].AddOnSKUs ?? string.Empty)
                        .Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .Where(s => s.Length > 0)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                        List<int> addOnProductIds = new List<int>();

                        foreach (string addOnIdentifier in addOnIdentifiers)
                        {
                            int addOnProductId = FindProductId(db, addOnIdentifier);

                            if (addOnProductId == 0)
                            {
                                string message = "Add On '" + addOnIdentifier + "' (for Product '" + productIdentifier + "') was not found (checked SKU and Product Name) - skipped.";
                                Warnings.Add(message);
                                WriteJobStatusRecord("Product Add Ons - Working", message, SavingErrorType.Validation);
                                continue;
                            }

                            if (addOnProductId == productId)
                            {
                                string message = "Product '" + productIdentifier + "' cannot be its own Add On - skipped.";
                                Warnings.Add(message);
                                WriteJobStatusRecord("Product Add Ons - Working", message, SavingErrorType.Validation);
                                continue;
                            }

                            if (!addOnProductIds.Contains(addOnProductId))
                            {
                                addOnProductIds.Add(addOnProductId);
                            }
                        }

                        SaveProductAddons(db, productId, addOnProductIds);
                    }
                    catch (Exception ex)
                    {
                        string errorString = "Could not save Add Ons for row " + (i + 1);
                        SaveHadErrors = true;
                        WriteJobStatusRecord("Product Add Ons - Working", errorString, SavingErrorType.Saving);
                        WriteJobStatusRecord("Product Add Ons - Working", ex.Message, SavingErrorType.Saving);
                    }
                }
            }
        }

        // Resolves either a SKU (partNo) or an exact Product Name to a productID.
        // SKU is checked first since it's unique; Product Name is a best-effort fallback and
        // could theoretically match more than one product if names aren't unique, in which case
        // whichever one the database returns first wins.
        private int FindProductId(ngmdEntities db, string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                return 0;
            }

            int productId = db.product
                .Where(w => w.partNo.Trim().ToLower() == identifier.Trim().ToLower())
                .Select(x => x.productID)
                .FirstOrDefault();

            if (productId == 0)
            {
                productId = db.product
                    .Where(w => w.productName.Trim().ToLower() == identifier.Trim().ToLower())
                    .Select(x => x.productID)
                    .FirstOrDefault();
            }

            return productId;
        }

        private void SaveProductAddons(ngmdEntities db, int productId, List<int> addOnProductIds)
        {
            List<ProductAddon> existing = db.ProductAddon.Where(w => w.ProductId == productId).ToList();

            // Anything currently saved that is no longer in the imported list gets removed -
            // this is what makes the import capable of removing an add-on, not just adding.
            foreach (var oldAddon in existing.Where(x => !addOnProductIds.Contains(x.AddonProductId)).ToList())
            {
                db.Entry(oldAddon).State = EntityState.Deleted;
            }

            for (int i = 0; i < addOnProductIds.Count; i++)
            {
                int addOnProductId = addOnProductIds[i];
                var match = existing.FirstOrDefault(x => x.AddonProductId == addOnProductId);

                if (match == null)
                {
                    var newAddon = new ProductAddon
                    {
                        ProductId = productId,
                        AddonProductId = addOnProductId,
                        DisplayOrder = i + 1,
                        IsActive = true,
                        CreatedDate = DateTime.Now,
                        CreatedBy = userName
                    };
                    db.Entry(newAddon).State = EntityState.Added;
                }
                else
                {
                    match.DisplayOrder = i + 1;
                    match.IsActive = true;
                    db.Entry(match).State = EntityState.Modified;
                }
            }

            db.SaveChanges();
        }

        public static bool DataTableColExists(DataRow dr, string colName)
        {
            return dr.Table.Columns.Contains(colName);
        }
    }
}
