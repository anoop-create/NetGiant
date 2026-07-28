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
    public class ImportObsoleteItemViewModel : JobStatusCommonViewModel
    {
        private string _filepath;

        public ImportObsoleteItemViewModel(string filepath)
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

                for (int i = 0; i < ObsoleteItemAcceptedFields.Fields.Length; i++)
                {
                    if (dt.Columns.Contains(ObsoleteItemAcceptedFields.Fields[i]))
                    {
                        mapped.Add(i, ObsoleteItemAcceptedFields.Fields[i]);
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
            var list = new List<ObsoleteItemImportFields>();
            int currentRow = 1;

            foreach (DataRow dr in dt.Rows)
            {
                try
                {
                    if (DataTableColExists(dr, "Website ID") 
                        && DataTableColExists(dr, "Stock Reference")
                        && DataTableColExists(dr, "Equipment Name")
                        && DataTableColExists(dr, "URL"))
                    {
                        ObsoleteItemImportFields fields = new ObsoleteItemImportFields();
                        fields.WebsiteId = Convert.ToInt32(dr["Website ID"]);
                        fields.StockReference = Convert.ToString(dr["Stock Reference"]) == "" ? null : Convert.ToString(dr["Stock Reference"]);
                        fields.EquipmentName = Convert.ToString(dr["Equipment Name"]) == "" ? null : Convert.ToString(dr["Equipment Name"]);
                        fields.URL = Convert.ToString(dr["URL"]);
                        list.Add(fields);
                    }
                    currentRow++;
                }
                catch (Exception ex)
                {
                    var message = ErrorMessage(currentRow, ex);
                    Warnings.Add(message);
                    WriteJobStatusRecord("Obsolete Item - Working", message, SavingErrorType.Validation);
                }
            }

            SaveRecord(list);
        }

        private void SaveRecord(List<ObsoleteItemImportFields> list)
        {
            WriteJobStatusRecord("Obsolete Item - Working", "", SavingErrorType.Saving);

            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                Save(list);

                if (SaveHadErrors)
                {
                    WriteJobStatusRecord("Obsolete Item - Complete", "", SavingErrorType.Saving);
                }
                else
                {
                    WriteJobStatusRecord("Obsolete Item - Complete", "Successfully Saved Obsolete Items", SavingErrorType.Saving);
                }
            }).Start();
        }

        private void Save(List<ObsoleteItemImportFields> obsoleteItemList)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                try
                {
                    for (int i = 0; i < obsoleteItemList.Count; i++)
                    {
                        var oi = new obsoleteItem
                        {
                            websiteFK = obsoleteItemList[i].WebsiteId,
                            stockReference = obsoleteItemList[i].StockReference,
                            equipmentName = obsoleteItemList[i].EquipmentName,
                            URL = obsoleteItemList[i].URL
                        };

                        var exists = db.obsoleteItem.Where(x => x.websiteFK == oi.websiteFK && x.stockReference == oi.stockReference && x.equipmentName == oi.equipmentName).FirstOrDefault();

                        if (exists == null)
                        {
                            db.Entry(oi).State = EntityState.Added;
                        }
                        else
                        {
                            exists.websiteFK = oi.websiteFK;
                            exists.stockReference = oi.stockReference;
                            exists.equipmentName = oi.equipmentName;
                            exists.URL = oi.URL;
                            db.Entry(exists).State = EntityState.Modified;
                        }

                        db.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    string errorString = "Could not save Obsolete Item";
                    SaveHadErrors = true;
                    WriteJobStatusRecord("Obsolete Item - Working", errorString, SavingErrorType.Saving);
                    WriteJobStatusRecord("Obsolete Item - Working", ex.Message, SavingErrorType.Saving);
                }
            }
        }

        private bool DataTableColExists(DataRow dr, string colName)
        {
            return dr.Table.Columns.Contains(colName);
        }
    }
}
