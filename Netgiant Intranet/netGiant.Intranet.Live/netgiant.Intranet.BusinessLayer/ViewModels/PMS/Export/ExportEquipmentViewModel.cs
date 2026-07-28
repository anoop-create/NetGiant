using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using netGiant.Intranet.DataLayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using netGiant.Intranet.BusinessLayer.Utilities;
using System.Data.Entity;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Export
{
    public class ExportEquipmentViewModel
    {
        public ExportEquipmentViewModel()
        {
            AllManufacturers = SelectListViewModel.AllEquipManufacturers();
            AllFamilies = SelectListViewModel.AllEquipFamilies();
            SetExportableFields();
        }

        public IQueryable<SelectListItem> AllManufacturers { get; set; }
        public IQueryable<SelectListItem> AllFamilies { get; set; }
        public string[] PostedFields { get; set; }
        public Dictionary<string, string> ExportableFields { get; set; }
        public List<string> PreSelectedFields { get; set; }
        public int EquipmentCount { get; set; }
        public string LocalDirectory { get; set; }
        public string FilePath { get; set; }
        public int SelectedManufacturerID { get; set; }
        public int SelectedFamilyID { get; set; }
        public string SelectedExportType { get; set; }

        public ExportEquipmentViewModel Export()
        {
            SetFilePath();

            switch (SelectedExportType)
            {
                case "equipment":
                    List<eqEquipment> equipmentList = GetEquipment();
                    CreateCSVFile(equipmentList);
                    break;
                case "family":
                    List<eqFamily> familyList = GetFamilies();
                    CreateCSVFile(familyList);
                    break;
                case "familyMapping":
                    List<eqFamilyMembership> familyMappings = GetFamilyMappings();
                    CreateCSVFile(familyMappings);
                    break;
                case "productMapping":
                    List<eqProductMembership> productMappings = GetProductMappings();
                    CreateCSVFile(productMappings);
                    break;
                default:
                    List<eqEquipment> equipmentList1 = GetEquipment();
                    CreateCSVFile(equipmentList1);
                    break;
            }

            return this;
        }

        private void SetFilePath()
        {
            FilePath = LocalDirectory + "\\PMSTempData\\ProductExport_" + DateTime.Now.ToString("dd_MM_yyyy_H_mm_ss") + ".csv";
        }

        private void CreateCSVFile(List<eqEquipment> equipmentList)
        {
            using (CsvFileWriter writer = new CsvFileWriter(FilePath, ','))
            {
                SetColumnHeadings(writer);

                foreach (var equip in equipmentList)
                {
                    InsertCSVData(writer, equip);
                }
            }
        }

        private void CreateCSVFile(List<eqFamily> familyList)
        {
            using (CsvFileWriter writer = new CsvFileWriter(FilePath, ','))
            {
                CsvRow firstRow = new CsvRow();
                firstRow.Add("Family ID");
                firstRow.Add("Family Description");
                firstRow.Add("Family Manufacturer");
                writer.WriteRow(firstRow);

                foreach (var family in familyList)
                {
                    CsvRow newRow = new CsvRow();
                    newRow.Add(family.eqFamilyID.ToSafeString());
                    newRow.Add(family.description);
                    newRow.Add(family.manufacturer.manufacturerName);
                    writer.WriteRow(newRow);
                }
            }
        }

        private void CreateCSVFile(List<eqFamilyMembership> familyMappings)
        {
            using (CsvFileWriter writer = new CsvFileWriter(FilePath, ','))
            {
                CsvRow firstRow = new CsvRow();
                firstRow.Add("Family Description");
                firstRow.Add("Equip Description");
                writer.WriteRow(firstRow);

                foreach (var map in familyMappings)
                {
                    CsvRow newRow = new CsvRow();
                    newRow.Add(map.eqFamily.description);
                    newRow.Add(map.eqEquipment.description);
                    writer.WriteRow(newRow);
                }
            }
        }

        private void CreateCSVFile(List<eqProductMembership> productMappings)
        {
            using (CsvFileWriter writer = new CsvFileWriter(FilePath, ','))
            {
                CsvRow firstRow = new CsvRow();
                firstRow.Add("Equip Description");
                firstRow.Add("Equip Product");
                writer.WriteRow(firstRow);

                foreach (var map in productMappings)
                {
                    CsvRow newRow = new CsvRow();
                    newRow.Add(map.eqEquipment.description);
                    newRow.Add(map.product.partNo);
                    writer.WriteRow(newRow);
                }
            }
        }

        private void InsertCSVData(CsvFileWriter writer, eqEquipment equip)
        {
            CsvRow newRow = new CsvRow();
            InsertEquipmentCSVData(equip, newRow);
            writer.WriteRow(newRow);
        }

        private void SetColumnHeadings(CsvFileWriter writer)
        {
            CsvRow firstRow = new CsvRow();
            AddCsvColumn(firstRow, "eqEquipmentID", ExportableFields, PostedFields);
            AddCsvColumn(firstRow, "equipName", ExportableFields, PostedFields);
            AddCsvColumn(firstRow, "manufacturer", ExportableFields, PostedFields);
            AddCsvColumn(firstRow, "productType", ExportableFields, PostedFields);
            AddCsvColumn(firstRow, "cartridgeType", ExportableFields, PostedFields);
            AddCsvColumn(firstRow, "mainURL", ExportableFields, PostedFields);
            AddCsvColumn(firstRow, "thumbnailURL", ExportableFields, PostedFields);
            AddCsvColumn(firstRow, "product", ExportableFields, PostedFields);
            AddCsvColumn(firstRow, "metaKeywords", ExportableFields, PostedFields);
            AddCsvColumn(firstRow, "metaContentType", ExportableFields, PostedFields);
            AddCsvColumn(firstRow, "globallyFeatured", ExportableFields, PostedFields);
            AddCsvColumn(firstRow, "brandFeatured", ExportableFields, PostedFields);
            writer.WriteRow(firstRow);
        }

        private void InsertEquipmentCSVData(eqEquipment equip, CsvRow newRow)
        {
            AddCsvData(newRow, "eqEquipmentID", equip.eqEquipmentID, ExportableFields, PostedFields);
            AddCsvData(newRow, "equipName", equip.description, ExportableFields, PostedFields);
            AddCsvData(newRow, "manufacturer", equip.manufacturer.manufacturerName, ExportableFields, PostedFields);
            AddCsvData(newRow, "productType", equip.productType.productTypeName, ExportableFields, PostedFields);
            AddCsvData(newRow, "cartridgeType", equip.eqCartridgeType.eqCartridgeTypeName, ExportableFields, PostedFields);
            AddCsvData(newRow, "mainURL", equip.mainURL, ExportableFields, PostedFields);
            AddCsvData(newRow, "thumbnailURL", equip.thumbnailURL, ExportableFields, PostedFields);
            AddCsvData(newRow, "product", equip.product != null ? equip.product.partNo : "", ExportableFields, PostedFields);
            AddCsvData(newRow, "metaKeywords", equip.metaKeywords, ExportableFields, PostedFields);
            AddCsvData(newRow, "metaContentType", equip.metaContentType.metaContentDescription, ExportableFields, PostedFields);
            AddCsvData(newRow, "globallyFeatured", equip.globallyFeatured, ExportableFields, PostedFields);
            AddCsvData(newRow, "brandFeatured", equip.brandFeatured, ExportableFields, PostedFields);
        }

        private void AddCsvData(CsvRow newRow, string entityName, object entityData,
                                Dictionary<string, string> dict, string[] postedFields)
        {
            if (dict.ContainsKey(entityName) && postedFields.Contains(entityName))
                newRow.Add(entityData.ToSafeString());
        }

        private void AddCsvColumn(CsvRow firstRow, string entityName,
                                Dictionary<string, string> dict, string[] postedFields)
        {
            if (dict.ContainsKey(entityName) && postedFields.Contains(entityName))
                firstRow.Add(dict.FirstOrDefault(m => m.Key == entityName).Value);
        }

        private List<eqEquipment> GetEquipment()
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                var query = db.eqEquipment
                    .Include(x => x.manufacturer)
                    .Include(x => x.eqCartridgeType)
                    .Include(x => x.productType)
                    .Include(x => x.product)
                    .Include(x => x.metaContentType);

                query = SetWhereClause(query);

                return query.OrderBy(x => x.description).ToList();
            }
        }

        private List<eqFamily> GetFamilies()
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                var query = db.eqFamily
                    .Include(x => x.manufacturer);

                query = SetWhereClause(query);

                return query.OrderBy(x => x.description).ToList();
            }
        }

        private List<eqFamilyMembership> GetFamilyMappings()
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                var query = db.eqFamilyMembership
                    .Include(x => x.eqFamily)
                    .Include(x => x.eqEquipment);

                query = SetWhereClause(query);

                return query
                    .OrderBy(x => x.eqFamily.description)
                    .ThenBy(x => x.eqEquipment.description)
                    .ToList();
            }
        }

        private List<eqProductMembership> GetProductMappings()
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                var query = db.eqProductMembership
                    .Include(x => x.product)
                    .Include(x => x.eqEquipment);

                query = SetWhereClause(query);

                return query
                    .OrderBy(x => x.eqEquipment.description)
                    .ToList();
            }
        }

        private IQueryable<eqEquipment> SetWhereClause(IQueryable<eqEquipment> query)
        {
            if (SelectedManufacturerID > 0)
                query = query.Where(x => x.manufacturerFK == SelectedManufacturerID);

            if (SelectedFamilyID > 0)
                query = query.Where(x => x.eqFamilyMembership.Any(y => y.eqFamilyID == SelectedFamilyID));

            return query;
        }

        private IQueryable<eqFamily> SetWhereClause(IQueryable<eqFamily> query)
        {
            if (SelectedManufacturerID > 0)
                query = query.Where(x => x.manufacturerFK == SelectedManufacturerID);

            return query;
        }

        private IQueryable<eqFamilyMembership> SetWhereClause(IQueryable<eqFamilyMembership> query)
        {
            if (SelectedManufacturerID > 0)
                query = query.Where(x => x.eqFamily.manufacturerFK == SelectedManufacturerID);

            return query;
        }

        private IQueryable<eqProductMembership> SetWhereClause(IQueryable<eqProductMembership> query)
        {
            if (SelectedManufacturerID > 0)
                query = query.Where(x => x.eqEquipment.manufacturerFK == SelectedManufacturerID);

            if (SelectedFamilyID > 0)
                query = query.Where(x => x.eqEquipment.eqFamilyMembership.Any(y => y.eqFamilyID == SelectedFamilyID));

            return query;
        }

        public ExportEquipmentViewModel GetResultsCount()
        {
            var returnCount = 0;

            using (ngmdEntities db = new ngmdEntities())
            {
                switch (SelectedExportType)
                {
                    case "equipment":
                        IQueryable<eqEquipment> q1 = db.eqEquipment;
                        q1 = SetWhereClause(q1);
                        returnCount = q1.Count();
                        break;
                    case "family":
                        IQueryable<eqFamily> q2 = db.eqFamily;
                        q2 = SetWhereClause(q2);
                        returnCount = q2.Count();
                        break;
                    case "familyMapping":
                        IQueryable<eqFamilyMembership> q3 = db.eqFamilyMembership;
                        q3 = SetWhereClause(q3);
                        returnCount = q3.Count();
                        break;
                    case "productMapping":
                        IQueryable<eqProductMembership> q4 = db.eqProductMembership;
                        q4 = SetWhereClause(q4);
                        returnCount = q4.Count();
                        break;
                    default:
                        IQueryable<eqEquipment> q5 = db.eqEquipment;
                        q5 = SetWhereClause(q5);
                        returnCount = q5.Count();
                        break;
                }
            }

            EquipmentCount = returnCount;

            return this;
        }

        private void SetExportableFields()
        {
            ExportableFields = new Dictionary<string, string>();
            ExportableFields.Add("eqEquipmentID", "Equip ID");
            ExportableFields.Add("equipName", "Equip Description");
            ExportableFields.Add("manufacturer", "Equip Manufacturer");
            ExportableFields.Add("productType", "Equip Product Type");
            ExportableFields.Add("cartridgeType", "Equip Cartridge Type");
            ExportableFields.Add("mainURL", "Equip Main URL");
            ExportableFields.Add("thumbnailURL", "Equip Thumbnail URL");
            ExportableFields.Add("product", "Equip Product");
            ExportableFields.Add("metaKeywords", "Equip Meta Keywords");
            ExportableFields.Add("metaContentType", "Equip Meta Content Type");
            ExportableFields.Add("globallyFeatured", "Globally Featured");
            ExportableFields.Add("brandFeatured", "Brand Featured");

            PreSelectedFields = new List<string>();
            PreSelectedFields.Add("eqEquipmentID");
            PreSelectedFields.Add("equipName");
            PreSelectedFields.Add("manufacturer");
            //PreSelectedFields.Add("productType");
            PreSelectedFields.Add("cartridgeType");
            //PreSelectedFields.Add("mainURL");
            //PreSelectedFields.Add("thumbnailURL");
            //PreSelectedFields.Add("product");
            //PreSelectedFields.Add("metaKeywords");
            //PreSelectedFields.Add("metaContentType");
            PreSelectedFields.Add("globallyFeatured");
            PreSelectedFields.Add("brandFeatured");
        }
    }
}
