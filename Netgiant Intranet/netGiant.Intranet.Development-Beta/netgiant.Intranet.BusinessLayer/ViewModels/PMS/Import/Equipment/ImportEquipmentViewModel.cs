using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Import.Equipment
{
    public class ImportEquipmentViewModel : JobStatusCommonViewModel
    {
        public ImportEquipmentViewModel()
        {
            equipmentList = new List<EquipmentImportFields>();
            equipmentNotesList = new List<EquipmentNotesImportFields>();
            familyList = new List<FamilyImportFields>();
            familyMappingList = new List<FamilyMappingImportFields>();
            equipmentProductMappingsList = new List<EquipmentProductMappingImportFields>();
            equipmentDeleteList = new List<EquipmentImportFields>();
            familyDeleteList = new List<FamilyImportFields>();
            familyMappingDeleteList = new List<FamilyMappingImportFields>();
            equipmentProductMappingDeleteList = new List<EquipmentProductMappingImportFields>();
        }

        public string FilePath { get; set; }

        List<EquipmentImportFields> equipmentList;
        List<EquipmentNotesImportFields> equipmentNotesList;
        List<FamilyImportFields> familyList;
        List<FamilyMappingImportFields> familyMappingList;
        List<EquipmentProductMappingImportFields> equipmentProductMappingsList;
        List<EquipmentImportFields> equipmentDeleteList;
        List<FamilyImportFields> familyDeleteList;
        List<FamilyMappingImportFields> familyMappingDeleteList;
        List<EquipmentProductMappingImportFields> equipmentProductMappingDeleteList;

        public void Import(string filePath)
        {
            DataTable dt = SharedFunctions.ReadTextFile(filePath);
            dt = GetAcceptedFields(dt);

            // Calling write from main thread, not worker thread here to avoid race condition.
            // This write must happen before the import product display status first displays on the main thread, 
            // or else the most recent job first seen can be the most recent, not the current.
            // This guarantees sequence on main thread of write new job to DB, then display view.
            WriteJobStatusRecord("Equipment - Working", "", SavingErrorType.Saving);

            // ProcessRows is on worker thread as it can do DB saves in the validation part if there is a column called "Family Description"
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                ProcessRows(dt);

                if (SaveHadErrors)
                {
                    WriteJobStatusRecord("Equipment - Complete", "", SavingErrorType.Saving);
                }
                else
                {
                    WriteJobStatusRecord("Equipment - Complete", "Successfully Saved Equipment", SavingErrorType.Saving);
                }
            }).Start();
        }

        private DataTable GetAcceptedFields(DataTable csvData)
        {
            DataColumnCollection columns = csvData.Columns;

            try
            {
                Dictionary<int, string> mappedColumns = new Dictionary<int, string>();
                int colIndex = 0;

                foreach (string col in EquipmentAcceptedFields.Fields)
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

            foreach (DataRow row in finalDt.Rows)
            {
                try
                {
                    ExtractData.RecordType recType = ExtractData.ExtractRecordType(row);

                    switch (recType)
                    {
                        case ExtractData.RecordType.Equipment:
                            ProcessEquipment(currentRow, row);
                            break;
                        case ExtractData.RecordType.EquipmentNote:
                            ProcessEquipmentNotes(currentRow, row);
                            break;
                        case ExtractData.RecordType.Family:
                            ProcessFamily(currentRow, row);
                            break;
                        case ExtractData.RecordType.EquipmentProductMapping:
                            ProcessEquipProdMapping(currentRow, row);
                            break;
                        case ExtractData.RecordType.FamilyMapping:
                            ProcessFamilyMapping(currentRow, row);
                            break;
                        case ExtractData.RecordType.EquipmentDelete:
                            ProcessEquipmentDelete(currentRow, row);
                            break;
                        case ExtractData.RecordType.FamilyDelete:
                            ProcessFamilyDelete(currentRow, row);
                            break;
                        case ExtractData.RecordType.FamilyMappingDelete:
                            ProcessFamilyMappingDelete(currentRow, row);
                            break;
                        case ExtractData.RecordType.EquipmentProductMappingDelete:
                            ProcessEquipProdMappingDelete(currentRow, row);
                            break;
                        default:
                            break;
                    }
                }
                catch (Exception ex)
                {
                    string message = ErrorMessage(currentRow, ex);
                    Warnings.Add(message);
                    WriteJobStatusRecord("Equipment - Working", message, SavingErrorType.Validation);
                }
                finally
                {
                    currentRow++;
                }
            }

            finalDt = null;
            Save();
        }

        private void ProcessEquipment(int currentRow, DataRow row)
        {
            EquipmentImportFields equipFields = null;
            FamilyImportFields familyFields = null;
            FamilyMappingImportFields familyMappingFields = null;

            equipFields = ExtractEquipment(row, currentRow);
            equipmentList.Add(equipFields);

            familyFields = ExtractFamilies(row, currentRow);
            if (familyFields.FamilyDescription != null)
            {
                familyList.Add(familyFields);
            }

            if (EquipRowHasFamily(row))
                Save();

            familyMappingFields = ExtractFamilyMappings(row, currentRow);
            if (familyMappingFields.FamilyID != 0)
            {
                familyMappingList.Add(familyMappingFields);
            }
        }

        private void ProcessEquipmentNotes(int currentRow, DataRow row)
        {
            EquipmentNotesImportFields equipNotesFields = null;

            equipNotesFields = ExtractEquipmentNotes(row, currentRow);
            equipmentNotesList.Add(equipNotesFields);
        }

        private void ProcessFamily(int currentRow, DataRow row)
        {
            FamilyImportFields familyFields = null;
            familyFields = ExtractFamilies(row, currentRow);
            familyList.Add(familyFields);
        }

        private void ProcessFamilyMapping(int currentRow, DataRow row)
        {
            FamilyMappingImportFields familyMappingFields = null;
            familyMappingFields = ExtractFamilyMappings(row, currentRow);
            familyMappingList.Add(familyMappingFields);
        }

        private void ProcessEquipProdMapping(int currentRow, DataRow row)
        {
            EquipmentProductMappingImportFields equipProdMappingFields = null;
            equipProdMappingFields = ExtractEquipProdMappings(row, currentRow);
            equipmentProductMappingsList.Add(equipProdMappingFields);
        }

        private void ProcessEquipmentDelete(int currentRow, DataRow row)
        {
            EquipmentImportFields equipFields = null;
            equipFields = ExtractEquipment(row, currentRow);
            equipmentDeleteList.Add(equipFields);
        }

        private void ProcessFamilyDelete(int currentRow, DataRow row)
        {
            FamilyImportFields familyFields = null;
            familyFields = ExtractFamilies(row, currentRow);
            familyDeleteList.Add(familyFields);
        }

        private void ProcessFamilyMappingDelete(int currentRow, DataRow row)
        {
            FamilyMappingImportFields familyMappingFields = null;
            familyMappingFields = ExtractFamilyMappings(row, currentRow);
            familyMappingDeleteList.Add(familyMappingFields);
        }

        private void ProcessEquipProdMappingDelete(int currentRow, DataRow row)
        {
            EquipmentProductMappingImportFields equipProdMappingFields = null;
            equipProdMappingFields = ExtractEquipProdMappings(row, currentRow);
            equipmentProductMappingDeleteList.Add(equipProdMappingFields);
        }

        private EquipmentImportFields ExtractEquipment(DataRow row, int csvRow)
        {
            ExtractData exrct = new ExtractData();
            EquipmentImportFields fields = new EquipmentImportFields();

            exrct.ExtractEquipmentID(row, fields);
            exrct.ExtractEquipmentDescription(row, fields);
            exrct.ExtractEquipManufacturer(row, fields);
            exrct.ExtractEquipCartType(row, fields);
            exrct.ExtractProduct(row, fields);
            exrct.ExtractProductType(row, fields);
            exrct.ExtractMainURL(row, fields);
            exrct.ExtractThumbnailURL(row, fields);
            exrct.ExtractMetaKeywords(row, fields);
            exrct.ExtractMetaTitle(row, fields);
            exrct.ExtractMetaDescription(row, fields);
            exrct.ExtractMetaContentType(row, fields);
            exrct.ExtractEquipFeaturedFlags(row, fields);
            exrct.ExtractEquipStatus(row, fields);

            return fields;
        }

        private EquipmentNotesImportFields ExtractEquipmentNotes(DataRow row, int csvRow)
        {
            ExtractData exrct = new ExtractData();
            EquipmentNotesImportFields fields = new EquipmentNotesImportFields();

            exrct.ExtractEquipNotesID(row, fields);
            exrct.ExtractWebsiteID(row, fields);
            exrct.ExtractEquipmentID(row, fields);
            exrct.ExtractEquipNotesNote(row, fields);
            exrct.ExtractEquipNotesIsDetail(row, fields);

            return fields;
        }


        private FamilyImportFields ExtractFamilies(DataRow row, int currentRow)
        {
            ExtractData exrct = new ExtractData();
            FamilyImportFields fields = new FamilyImportFields();

            exrct.ExtractFamilyID(row, fields);
            exrct.ExtractFamilyDescription(row, fields);
            exrct.ExtractFamilyManufacturer(row, fields);

            return fields;
        }

        private FamilyMappingImportFields ExtractFamilyMappings(DataRow row, int currentRow)
        {
            ExtractData exrct = new ExtractData();
            FamilyMappingImportFields fields = new FamilyMappingImportFields();

            exrct.ExtractFamilyID(row, fields);
            exrct.ExtractFamilyDescription(row, fields);
            exrct.ExtractEquipmentID(row, fields);
            exrct.ExtractEquipmentDescription(row, fields);

            return fields;
        }

        private EquipmentProductMappingImportFields ExtractEquipProdMappings(DataRow row, int currentRow)
        {
            ExtractData exrct = new ExtractData();
            EquipmentProductMappingImportFields fields = new EquipmentProductMappingImportFields();

            exrct.ExtractEquipmentID(row, fields);
            exrct.ExtractEquipmentDescription(row, fields, true);
            exrct.ExtractProduct(row, fields);

            return fields;
        }

        private bool EquipRowHasFamily(DataRow row)
        {
            return ExtractData.DataTableColExists(row, "Family Description") == true ? true : false;
        }

        private void Save()
        {
            SaveRecords sr = new SaveRecords();
            sr.EquipFields = equipmentList;
            sr.EquipNotesFields = equipmentNotesList;
            sr.FamilyFields = familyList;
            sr.FamilyMappingFields = familyMappingList;
            sr.EquipProdMappingFields = equipmentProductMappingsList;
            sr.EquipDeleteFields = equipmentDeleteList;
            sr.FamilyDeleteFields = familyDeleteList;
            sr.FamilyMappingDeleteFields = familyMappingDeleteList;
            sr.EquipProdMappingFieldsDelete = equipmentProductMappingDeleteList;

            sr.Save(this);
            Cleanup();
        }

        private void Cleanup()
        {
            equipmentList.Clear();
            equipmentNotesList.Clear();
            familyList.Clear();
            familyMappingList.Clear();
            equipmentProductMappingsList.Clear();
            equipmentDeleteList.Clear();
            familyDeleteList.Clear();
            familyMappingDeleteList.Clear();
            equipmentProductMappingDeleteList.Clear();
        }
    }
}
