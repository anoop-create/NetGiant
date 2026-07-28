using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Import.PromotionalGroup
{
    public class ImportPromotionalGroupViewModel : JobStatusCommonViewModel
    {
        public string FilePath { get; set; }

        public void Import(string filePath)
        {
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

                foreach (string col in PromotionalGroupsAcceptedFields.Fields)
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
            List<PromotionalGroupImportFields> promotionalGroupList = new List<PromotionalGroupImportFields>();

            foreach (DataRow dr in dt.Rows)
            {
                try
                {
                    if (DataTableColExists(dr, "Website ID") && DataTableColExists(dr, "Alt Ref")) {
                        PromotionalGroupImportFields fields = new PromotionalGroupImportFields();
                        fields.WebsiteId = Convert.ToInt32(dr["Website ID"]);
                        fields.AltRef = Convert.ToString(dr["Alt Ref"]);
                        fields.PromoName = DataTableColExists(dr, "Promo Name") ? Convert.ToString(dr["Promo Name"]) : null;
                        promotionalGroupList.Add(fields);
                    }
                    currentRow++;
                }
                catch (Exception ex)
                {
                    var message = ErrorMessage(currentRow, ex);
                    Warnings.Add(message);
                    WriteJobStatusRecord("Promotional Group - Working", message, SavingErrorType.Validation);
                }
            }
            dt = null;
            SaveRecords(promotionalGroupList);
        }

        public static bool DataTableColExists(DataRow dr, string colName)
        {
            return dr.Table.Columns.Contains(colName);
        }

        private void SaveRecords(List<PromotionalGroupImportFields> promotionalGroupList)
        {
            WriteJobStatusRecord("Promotional Group - Working", "", SavingErrorType.Saving);

            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                Save(promotionalGroupList);

                if (SaveHadErrors)
                {
                    WriteJobStatusRecord("Promotional Group - Complete", "", SavingErrorType.Saving);
                }
                else
                {
                    WriteJobStatusRecord("Promotional Group - Complete", "Successfully Saved Promotional Group", SavingErrorType.Saving);
                }
            }).Start();
        }

        private void Save(List<PromotionalGroupImportFields> promotionalGroupList)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                try
                {
                    for (int i = 0; i < promotionalGroupList.Count; i++)
                    {
                        int websiteId = promotionalGroupList[i].WebsiteId;
                        string altRef = promotionalGroupList[i].AltRef;
                        string promoName = promotionalGroupList[i].PromoName;

                        websiteInventory webInventory = (from w in db.websiteInventory
                                                         join p in db.product on w.productFK equals p.productID
                                                         where p.partNo == altRef && w.websiteFK == websiteId
                                                         select w).FirstOrDefault();

                        webInventory.promotionalGroupFK = null;

                        if (promoName != "")
                        {
                            promotionalGroup promotion = db.promotionalGroup
                                                             .Where(x =>
                                                                 x.promotionalGroupName == promoName
                                                             ).FirstOrDefault();

                            webInventory.promotionalGroupFK = promotion.promotionalGroupId;
                        }
                        else if (websiteId > 1 && !String.IsNullOrEmpty(altRef))
                        {
                            webInventory.promotionalGroup = null;
                            webInventory.promotionalGroupFK = null;
                        }

                        db.Entry(webInventory).State = EntityState.Modified;
                        db.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    string errorString = "Could not save Promotional Groups";
                    SaveHadErrors = true;
                    WriteJobStatusRecord("Promotional Group - Working", errorString, SavingErrorType.Saving);
                    WriteJobStatusRecord("Promotional Group - Working", ex.Message, SavingErrorType.Saving);
                }
            }
        }
    }
}
