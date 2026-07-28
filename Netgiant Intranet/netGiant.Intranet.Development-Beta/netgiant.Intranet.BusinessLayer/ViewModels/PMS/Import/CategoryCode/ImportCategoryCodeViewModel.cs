using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Import
{
    public class ImportCategoryCodeViewModel : JobStatusCommonViewModel
    {
        private string _filepath;

        public ImportCategoryCodeViewModel(string filepath)
        {
            _filepath = filepath;
        }

        public void Import()
        {
            var dt = SharedFunctions.ReadTextFile(_filepath);
            dt = GetAcceptedFields(dt);
            ProcessRows(dt);
        }

        private DataTable GetAcceptedFields(DataTable dt)
        {
            try
            {
                var mapped = new Dictionary<int, string>();

                for (int i = 0; i < CategoryCodeAcceptedFields.Fields.Length; i++)
                {
                    if (dt.Columns.Contains(CategoryCodeAcceptedFields.Fields[i]))
                    {
                        mapped.Add(i, CategoryCodeAcceptedFields.Fields[i]);
                    }
                }

                if (mapped.Count == 0)
                {
                    throw new Exception("The column titles are not correct for this import type.");
                }

                dt = new DataView(dt).ToTable(false, mapped.Select(x => x.Value).ToArray());
            }
            catch (Exception ex)
            {
                throw new ApplicationException(ex.Message);
            }

            return dt;
        }

        private void ProcessRows(DataTable dt)
        {
            var list = new List<CategoryCodeFields>();

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                try
                {
                    var dr = dt.Rows[i];
                    var item = new CategoryCodeFields
                    {
                        AltRef = Convert.ToString(dr["Alt Ref"]),
                        Manufacturer = Convert.ToString(dr["Manufacturer"]),
                        SecondaryCategoriesTG = string.IsNullOrEmpty(Convert.ToString(dr["Secondary Categories TG"])) ? new List<int>() : Convert.ToString(dr["Secondary Categories TG"]).Split(',').Select(Int32.Parse).ToList(),
                        SecondaryCategoriesCM = string.IsNullOrEmpty(Convert.ToString(dr["Secondary Categories CM"])) ? new List<int>() : Convert.ToString(dr["Secondary Categories CM"]).Split(',').Select(Int32.Parse).ToList(),
                    };

                    item.CategoryCodeTG = GetCategoryCodeIdFromName(Convert.ToString(dr["Category Code TG"]), 1);
                    item.CategoryCodeCM = GetCategoryCodeIdFromName(Convert.ToString(dr["Category Code CM"]), 2);

                    list.Add(item);
                }
                catch (Exception ex)
                {
                    var msg = ErrorMessage(i + 1, ex);
                    Warnings.Add(msg);
                    WriteJobStatusRecord("Category Code - Working", msg, SavingErrorType.Validation);
                }
            }

            SaveRecord(list);
        }

        private int? GetCategoryCodeIdFromName(string name, int websiteId)
        {
            using (var db = new ngmdEntities())
            {
                var query = db.categoryCode.Where(w => w.categoryCodeName == name && w.websiteFK == websiteId)
                                      .Select(x => x.categoryCodeID);
                return query.Count() == 0 ? (int?)null : query.FirstOrDefault();
            }
        }

        private void SaveRecord(List<CategoryCodeFields> list)
        {
            WriteJobStatusRecord("Product Category Codes - Working", "", SavingErrorType.Saving);

            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                Save(list);

                if (SaveHadErrors)
                {
                    WriteJobStatusRecord("Product Category Codes - Complete", "", SavingErrorType.Saving);
                }
                else
                {
                    WriteJobStatusRecord("Product Category Codes - Complete", "Successfully Saved Category Codes", SavingErrorType.Saving);
                }
            }).Start();
        }

        private void Save(List<CategoryCodeFields> list)
        {
            foreach (var fields in list)
            {
                var websiteInventoryTG = ProcessCategoryCode(fields.AltRef, fields.CategoryCodeTG, 1);
                var websiteInventoryCM = ProcessCategoryCode(fields.AltRef, fields.CategoryCodeCM, 2);

                if (websiteInventoryTG != null) ProcessSecondaryCategoryCode(websiteInventoryTG, fields.AltRef, fields.SecondaryCategoriesTG);
                if (websiteInventoryCM != null) ProcessSecondaryCategoryCode(websiteInventoryCM, fields.AltRef, fields.SecondaryCategoriesCM);
            }
        }

        private websiteInventory ProcessCategoryCode(string altRef, int? categoryCodeId, int websiteId)
        {
            var wi = GetWebsiteInventory(altRef, websiteId);

            if (wi == null && categoryCodeId != null)
            {
                wi = CreateWebsiteInventory(websiteId, altRef, (int)categoryCodeId);
            }
            else if (wi != null && categoryCodeId == null)
            {
                DeleteWebsiteInventory(wi);
            }
            else if (wi != null && categoryCodeId != null && wi.categoryCodeFK != categoryCodeId)
            {
                UpdateWebsiteInventory(wi, (int)categoryCodeId);
            }

            return wi;
        }

        private websiteInventory GetWebsiteInventory(string altRef, int websiteId)
        {
            using (var db = new ngmdEntities())
            {
                return db.websiteInventory.Where(w => w.product == db.product
                                                                     .Where(x => x.partNo == altRef)
                                                                     .FirstOrDefault()
                                                    && w.websiteFK == websiteId
                                                ).FirstOrDefault();
            }
        }

        private websiteInventory CreateWebsiteInventory(int websiteId, string altRef, int categoryCodeId)
        {
            using (var db = new ngmdEntities())
            {
                try
                {
                    var wi = new websiteInventory
                    {
                        websiteFK = websiteId,
                        productFK = db.product.Where(w => w.partNo == altRef).FirstOrDefault().productID,
                        categoryCodeFK = categoryCodeId,
                        dateLastUpdate = DateTime.Now
                    };

                    db.Entry(wi).State = EntityState.Added;
                    db.SaveChanges();

                    return wi;
                }
                catch (Exception ex)
                {
                    string errorString = "Could not create website inventory";
                    SaveHadErrors = true;
                    WriteJobStatusRecord("Product Category Codes - Working", errorString, SavingErrorType.Saving);
                    WriteJobStatusRecord("Product Category Codes - Working", ex.Message, SavingErrorType.Saving);
                    return null;
                }
            }
        }

        private void DeleteWebsiteInventory(websiteInventory wi)
        {
            using (var db = new ngmdEntities())
            {
                try
                {
                    db.Entry(wi).State = EntityState.Deleted;
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    string errorString = "Could not delete website inventory";
                    SaveHadErrors = true;
                    WriteJobStatusRecord("Product Category Codes - Working", errorString, SavingErrorType.Saving);
                    WriteJobStatusRecord("Product Category Codes - Working", ex.Message, SavingErrorType.Saving);
                }
            }
        }

        private void UpdateWebsiteInventory(websiteInventory wi, int categoryCodeId)
        {
            using (var db = new ngmdEntities())
            {
                try
                {
                    wi.categoryCodeFK = categoryCodeId;
                    wi.dateLastUpdate = DateTime.Now;
                    db.Entry(wi).State = EntityState.Modified;
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    string errorString = "Could not update website inventory";
                    SaveHadErrors = true;
                    WriteJobStatusRecord("Product Category Codes - Working", errorString, SavingErrorType.Saving);
                    WriteJobStatusRecord("Product Category Codes - Working", ex.Message, SavingErrorType.Saving);
                }
            }
        }

        private void ProcessSecondaryCategoryCode(websiteInventory wi, string altRef, List<int> secondaryCategories)
        {
            if (secondaryCategories.Count == 0)
            {
                DeleteSecondaryCategoryCodes(wi);
                return;
            }

            var list = GetSecondaryCategoryCodes(wi);

            for (int i=0; i < list.Count; i++)
            {
                if (!secondaryCategories.Contains((int)list[i].categoryCodeFK))
                {
                    DeleteSecondaryCategoryCode(list[i]);
                }
            }

            for (int i=0; i < secondaryCategories.Count; i++)
            {
                if (list.Count == 0 || list.Where(w => w.categoryCodeFK == secondaryCategories[i]).Count() == 0)
                {
                    CreateSecondaryCategoryCode(wi, secondaryCategories[i]);
                }
            }
        }

        private void CreateSecondaryCategoryCode(websiteInventory wi, int categoryCodeId)
        {
            using (var db = new ngmdEntities())
            {
                try
                {
                    var secondary = new secondaryCategoryLookup
                    {
                        websiteInventoryFK = wi.websiteInventoryID,
                        categoryCodeFK = categoryCodeId
                    };

                    db.Entry(secondary).State = EntityState.Added;
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    string errorString = "Could not create secondary category code";
                    SaveHadErrors = true;
                    WriteJobStatusRecord("Product Category Codes - Working", errorString, SavingErrorType.Saving);
                    WriteJobStatusRecord("Product Category Codes - Working", ex.Message, SavingErrorType.Saving);
                }
            }
        }

        private List<secondaryCategoryLookup> GetSecondaryCategoryCodes(websiteInventory wi)
        {
            using (var db = new ngmdEntities())
            {
                return db.secondaryCategoryLookup.Where(w => w.websiteInventoryFK == wi.websiteInventoryID).ToList();
            }
        }

        private void DeleteSecondaryCategoryCodes(websiteInventory wi)
        {
            using (var db = new ngmdEntities())
            {
                try
                {
                    var list = db.secondaryCategoryLookup.Where(w => w.websiteInventoryFK == wi.websiteInventoryID);

                    foreach (var secondary in list)
                    {
                        db.Entry(secondary).State = EntityState.Deleted;
                    }

                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    string errorString = "Could not delete secondary category codes";
                    SaveHadErrors = true;
                    WriteJobStatusRecord("Product Category Codes - Working", errorString, SavingErrorType.Saving);
                    WriteJobStatusRecord("Product Category Codes - Working", ex.Message, SavingErrorType.Saving);
                }
            }
        }

        private void DeleteSecondaryCategoryCode(secondaryCategoryLookup secondaryCategoryLookup)
        {
            using (var db = new ngmdEntities())
            {
                try
                {
                    db.Entry(secondaryCategoryLookup).State = EntityState.Deleted;
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    string errorString = "Could not delete secondary category code";
                    SaveHadErrors = true;
                    WriteJobStatusRecord("Product Category Codes - Working", errorString, SavingErrorType.Saving);
                    WriteJobStatusRecord("Product Category Codes - Working", ex.Message, SavingErrorType.Saving);
                }
            }
        }
    }
}