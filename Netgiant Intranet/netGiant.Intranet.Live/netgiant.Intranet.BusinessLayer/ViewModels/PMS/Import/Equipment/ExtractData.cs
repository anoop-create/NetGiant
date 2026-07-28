using netGiant.Intranet.DataLayer;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Import.Equipment
{
    public class ExtractData
    {
        public enum RecordType
        {
            Equipment,
            Family,
            EquipmentProductMapping,
            FamilyMapping,
            EquipmentDelete,
            FamilyDelete,
            FamilyMappingDelete,
            EquipmentProductMappingDelete
        }

        internal static RecordType ExtractRecordType(DataRow row)
        {
            RecordType returnEnum = RecordType.Equipment;
            string recType = DataTableColExists(row, "Record Type") == true ? row["Record Type"].ToString() : null;

            if (recType != null)
            {
                switch (recType.ToLower())
                {
                    case "equipment":
                        returnEnum = RecordType.Equipment;
                        break;
                    case "family":
                        returnEnum = RecordType.Family;
                        break;
                    case "equipmentproductmapping":
                        returnEnum = RecordType.EquipmentProductMapping;
                        break;
                    case "familymapping":
                        returnEnum = RecordType.FamilyMapping;
                        break;
                    case "equipmentdelete":
                        returnEnum = RecordType.EquipmentDelete;
                        break;
                    case "familydelete":
                        returnEnum = RecordType.FamilyDelete;
                        break;
                    case "familymappingdelete":
                        returnEnum = RecordType.FamilyMappingDelete;
                        break;
                    case "equipmentproductmappingdelete":
                        returnEnum = RecordType.EquipmentProductMappingDelete;
                        break;
                    default:
                        returnEnum = RecordType.Equipment;
                        break;
                }
            }

            return returnEnum;
        }

        public static bool DataTableColExists(DataRow dr, string colName)
        {
            return dr.Table.Columns.Contains(colName);
        }

        internal void ExtractEquipmentID(DataRow row, EquipmentImportFields fields)
        {
            string equipID = DataTableColExists(row, "Equip ID") == true ? row["Equip ID"].ToString() : null;

            if (equipID != null)
            {
                int equipmentID = 0;
                bool success = int.TryParse(equipID, out equipmentID);

                eqEquipment eqEquip = null;

                using (ngmdEntities db = new ngmdEntities())
                {
                    if (success)
                    {
                        eqEquip = db.eqEquipment.Find(equipmentID);
                    }
                    else
                    {
                        eqEquip = db.eqEquipment
                            .Where(x => x.description.ToLower().Trim() == equipID.ToLower().Trim())
                            .FirstOrDefault();
                    }
                }

                fields.EquipID = eqEquip != null ? eqEquip.eqEquipmentID : 0;
            }
        }

        internal void ExtractEquipmentID(DataRow row, FamilyMappingImportFields fields)
        {
            string equipID = DataTableColExists(row, "Equip ID") == true ? row["Equip ID"].ToString() : null;

            if (equipID != null)
            {
                int equipmentID = 0;
                bool success = int.TryParse(equipID, out equipmentID);

                eqEquipment eqEquip = null;

                using (ngmdEntities db = new ngmdEntities())
                {
                    if (success)
                    {
                        eqEquip = db.eqEquipment.Find(equipmentID);
                    }
                    else
                    {
                        eqEquip = db.eqEquipment
                            .Where(x => x.description.ToLower().Trim() == equipID.ToLower().Trim())
                            .FirstOrDefault();
                    }
                }

                fields.EquipID = eqEquip != null ? eqEquip.eqEquipmentID : 0;
            }
        }

        internal void ExtractEquipmentID(DataRow row, EquipmentProductMappingImportFields fields)
        {
            string equipID = DataTableColExists(row, "Equip ID") == true ? row["Equip ID"].ToString() : null;

            if (equipID != null)
            {
                int equipmentID = 0;
                bool success = int.TryParse(equipID, out equipmentID);

                eqEquipment eqEquip = null;

                using (ngmdEntities db = new ngmdEntities())
                {
                    if (success)
                    {
                        eqEquip = db.eqEquipment.Find(equipmentID);
                    }
                    else
                    {
                        eqEquip = db.eqEquipment
                            .Where(x => x.description.ToLower().Trim() == equipID.ToLower().Trim())
                            .FirstOrDefault();
                    }
                }

                fields.EquipID = eqEquip != null ? eqEquip.eqEquipmentID : 0;
            }
        }

        internal void ExtractEquipmentDescription(DataRow row, EquipmentImportFields fields)
        {
            string equipDescription = DataTableColExists(row, "Equip Description") == true ? row["Equip Description"].ToString() : null;

            if (equipDescription != null)
            {
                fields.EquipDescription = equipDescription;
            }

            if (fields.EquipID == 0)
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    eqEquipment eqEquip = null;

                    eqEquip = db.eqEquipment
                        .Where(x => x.description.Trim().ToLower() == equipDescription.Trim().ToLower())
                        .FirstOrDefault();

                    if (eqEquip != null)
                        fields.EquipID = eqEquip.eqEquipmentID;
                }
            }
        }

        internal void ExtractEquipmentDescription(DataRow row, FamilyMappingImportFields fields)
        {
            string equipDescription = DataTableColExists(row, "Equip Description") == true ? row["Equip Description"].ToString() : null;

            if (fields.EquipID == 0)
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    eqEquipment eqEquip = null;

                    eqEquip = db.eqEquipment
                        .Where(x => x.description.Trim().ToLower() == equipDescription.Trim().ToLower())
                        .FirstOrDefault();

                    if (eqEquip != null)
                        fields.EquipID = eqEquip.eqEquipmentID;
                }
            }
        }

        internal void ExtractEquipmentDescription(DataRow row, EquipmentProductMappingImportFields fields, bool enforceDescription)
        {
            string equipDescription = DataTableColExists(row, "Equip Description") == true ? row["Equip Description"].ToString() : null;

            if (fields.EquipID == 0)
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    eqEquipment eqEquip = null;

                    eqEquip = db.eqEquipment
                        .Where(x => x.description.Trim().ToLower() == equipDescription.Trim().ToLower())
                        .FirstOrDefault();

                    if (enforceDescription && eqEquip == null)
                    {
                        throw new ApplicationException("Equipment '" + equipDescription + "' not matched in the PMS");
                    }

                    if (eqEquip != null)
                        fields.EquipID = eqEquip.eqEquipmentID;
                }
            }
        }

        internal void ExtractEquipManufacturer(DataRow row, EquipmentImportFields fields)
        {
            string equipManufacturer = DataTableColExists(row, "Equip Manufacturer") == true ? row["Equip Manufacturer"].ToString() : null;

            if (equipManufacturer != null)
            {
                int equipManuFK = 0;
                bool success = int.TryParse(equipManufacturer, out equipManuFK);

                manufacturer equipManu;

                using (ngmdEntities db = new ngmdEntities())
                {
                    if (success)
                    {
                        equipManu = db.manufacturer.Where(x => x.manufacturerID == equipManuFK).FirstOrDefault();
                    }
                    else
                    {
                        equipManu = db.manufacturer
                            .Where(x => x.manufacturerName.ToLower() == equipManufacturer.ToLower())
                            .FirstOrDefault();
                    }

                    if (equipManu != null)
                    {
                        fields.EquipManuFK = equipManu.manufacturerID;
                    }
                    else
                    {
                        throw new ApplicationException("Manufacturer '" + equipManufacturer + "' not matched in the PMS");
                    }
                }
            }
        }

        internal void ExtractEquipCartType(DataRow row, EquipmentImportFields fields)
        {
            string equipCartType = DataTableColExists(row, "Equip Cartridge Type") == true ? row["Equip Cartridge Type"].ToString() : null;

            if (equipCartType != null)
            {
                int equipCartTypeFK = 0;
                bool success = int.TryParse(equipCartType, out equipCartTypeFK);

                eqCartridgeType eqCartType;

                using (ngmdEntities db = new ngmdEntities())
                {
                    if (success)
                    {
                        eqCartType = db.eqCartridgeType
                            .Where(x => x.eqCartridgeTypeID == equipCartTypeFK)
                            .FirstOrDefault();
                    }
                    else
                    {
                        eqCartType = db.eqCartridgeType
                            .Where(x => x.eqCartridgeTypeName.ToLower() == equipCartType.ToLower())
                            .FirstOrDefault();
                    }

                    if (eqCartType != null)
                    {
                        fields.EquipCartTypeFK = eqCartType.eqCartridgeTypeID;
                    }
                    else
                    {
                        throw new ApplicationException("Cartridge Type '" + equipCartType + "' not matched in the PMS");
                    }
                }
            }
        }

        internal void ExtractEquipFeaturedFlags(DataRow row, EquipmentImportFields fields)
        {
            string equipGloballyFeatured = DataTableColExists(row, "Globally Featured") == true ? row["Globally Featured"].ToString() : null;
            fields.GloballyFeatured = SetBoolean(equipGloballyFeatured);

            string equipBrandFeatured = DataTableColExists(row, "Brand Featured") == true ? row["Brand Featured"].ToString() : null;
            fields.BrandFeatured = SetBoolean(equipBrandFeatured);
        }

        internal void ExtractProduct(DataRow row, EquipmentImportFields fields)
        {
            string equipProduct = DataTableColExists(row, "Equip Product") == true ? row["Equip Product"].ToString() : null;

            if (!String.IsNullOrEmpty(equipProduct))
            {
                product prod;

                using (ngmdEntities db = new ngmdEntities())
                {
                    prod = db.product
                        .Where(x => x.partNo.ToLower() == equipProduct.ToLower())
                        .FirstOrDefault();

                    if (prod != null)
                    {
                        fields.EquipProductFK = prod.productID;
                    }
                    else
                    {
                        throw new ApplicationException("Product '" + equipProduct + "' not matched in the PMS");
                    }
                }
            }
            else
            {
                if (equipProduct == "")
                    fields.EquipProductFK = null;
            }
        }

        internal void ExtractProduct(DataRow row, EquipmentProductMappingImportFields fields)
        {
            string equipProduct = DataTableColExists(row, "Equip Product") == true ? row["Equip Product"].ToString() : null;

            if (!String.IsNullOrEmpty(equipProduct))
            {
                product prod;

                using (ngmdEntities db = new ngmdEntities())
                {
                    prod = db.product
                        .Where(x => x.partNo.ToLower() == equipProduct.ToLower())
                        .FirstOrDefault();

                    if (prod != null)
                    {
                        fields.ProductID = prod.productID;
                    }
                    else
                    {
                        throw new ApplicationException("Product '" + equipProduct + "' not matched in the PMS");
                    }
                }
            }
            else
            {
                if (equipProduct == "")
                    fields.EquipID = 0;
            }
        }

        internal void ExtractProductType(DataRow row, EquipmentImportFields fields)
        {
            string equipProductType = DataTableColExists(row, "Equip Product Type") == true ? row["Equip Product Type"].ToString() : null;

            if (equipProductType != null)
            {
                int equipProductTypeFK = 0;
                bool success = int.TryParse(equipProductType, out equipProductTypeFK);

                productType prodType;

                using (ngmdEntities db = new ngmdEntities())
                {
                    if (success)
                    {
                        prodType = db.productType
                            .Where(x => x.productTypeID == equipProductTypeFK)
                            .FirstOrDefault();
                    }
                    else
                    {
                        prodType = db.productType
                            .Where(x => x.productTypeName.ToLower() == equipProductType.ToLower())
                            .FirstOrDefault();
                    }

                    if (prodType != null)
                    {
                        fields.EquipProductTypeFK = prodType.productTypeID;
                    }
                    else
                    {
                        throw new ApplicationException("Product Type '" + equipProductType + "' not matched in the PMS");
                    }
                }
            }
        }

        internal void ExtractMainURL(DataRow row, EquipmentImportFields fields)
        {
            string equipMainURL = DataTableColExists(row, "Equip Main URL") == true ? row["Equip Main URL"].ToString() : null;

            if (equipMainURL != null)
            {
                fields.EquipMainURL = equipMainURL;
            }
        }

        internal void ExtractThumbnailURL(DataRow row, EquipmentImportFields fields)
        {
            string equipThumbnailURL = DataTableColExists(row, "Equip Thumbnail URL") == true ? row["Equip Thumbnail URL"].ToString() : null;

            if (equipThumbnailURL != null)
            {
                fields.EquipThumbnailURL = equipThumbnailURL;
            }
        }

        internal void ExtractMetaKeywords(DataRow row, EquipmentImportFields fields)
        {
            string equipMetaKeywords = DataTableColExists(row, "Equip Meta Keywords") == true ? row["Equip Meta Keywords"].ToString() : null;

            if (equipMetaKeywords != null)
            {
                fields.EquipMetaKeywords = equipMetaKeywords;
            }
        }

        internal void ExtractMetaContentType(DataRow row, EquipmentImportFields fields)
        {
            string equipMetaContentType = DataTableColExists(row, "Equip Meta Content Type") == true ? row["Equip Meta Content Type"].ToString() : null;

            if (equipMetaContentType != null)
            {
                int equipMetaContentTypeFK = 0;
                bool success = int.TryParse(equipMetaContentType, out equipMetaContentTypeFK);

                metaContentType metaContentType;

                using (ngmdEntities db = new ngmdEntities())
                {
                    if (success)
                    {
                        metaContentType = db.metaContentType
                            .Where(x => x.metaContentTypeID == equipMetaContentTypeFK)
                            .FirstOrDefault();
                    }
                    else
                    {
                        metaContentType = db.metaContentType
                            .Where(x => x.metaContentDescription.ToLower() == equipMetaContentType.ToLower())
                            .FirstOrDefault();
                    }

                    if (metaContentType != null)
                    {
                        fields.EquipMetaContentTypeFK = metaContentType.metaContentTypeID;
                    }
                    else
                    {
                        throw new ApplicationException("Meta Content Type '" + equipMetaContentType + "' not matched in the PMS");
                    }
                }
            }
        }

        internal void ExtractFamilyDescription(DataRow row, FamilyImportFields fields)
        {
            string familyDescription = DataTableColExists(row, "Family Description") == true ? row["Family Description"].ToString() : null;

            if (familyDescription != null)
            {
                fields.FamilyDescription = familyDescription;
            }

            if (fields.FamilyID == 0)
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    eqFamily eqFamily = null;

                    eqFamily = db.eqFamily
                        .Where(x => x.description.Trim().ToLower() == familyDescription.Trim().ToLower())
                        .FirstOrDefault();

                    if (eqFamily != null)
                        fields.FamilyID = eqFamily.eqFamilyID;
                }
            }
        }

        internal void ExtractFamilyDescription(DataRow row, FamilyMappingImportFields fields)
        {
            string familyDescription = DataTableColExists(row, "Family Description") == true ? row["Family Description"].ToString() : null;

            if (fields.FamilyID == 0)
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    eqFamily eqFamily = null;

                    eqFamily = db.eqFamily
                        .Where(x => x.description.Trim().ToLower() == familyDescription.Trim().ToLower())
                        .FirstOrDefault();

                    if (eqFamily != null)
                        fields.FamilyID = eqFamily.eqFamilyID;
                }
            }
        }

        internal void ExtractFamilyManufacturer(DataRow row, FamilyImportFields fields)
        {
            string familyManufacturer = DataTableColExists(row, "Family Manufacturer") == true ? row["Family Manufacturer"].ToString() : null;

            if (familyManufacturer != null)
            {
                int familyManuFK = 0;
                bool success = int.TryParse(familyManufacturer, out familyManuFK);

                manufacturer familyManu;

                using (ngmdEntities db = new ngmdEntities())
                {
                    if (success)
                    {
                        familyManu = db.manufacturer.Where(x => x.manufacturerID == familyManuFK).FirstOrDefault();
                    }
                    else
                    {
                        familyManu = db.manufacturer
                            .Where(x => x.manufacturerName.ToLower() == familyManufacturer.ToLower())
                            .FirstOrDefault();
                    }

                    if (familyManu != null)
                    {
                        fields.FamilyManuFK = familyManu.manufacturerID;
                    }
                    else
                    {
                        throw new ApplicationException("Manufacturer '" + familyManufacturer + "' not matched in the PMS");
                    }
                }
            }
        }

        internal void ExtractFamilyID(DataRow row, FamilyImportFields fields)
        {
            string famID = DataTableColExists(row, "Family ID") == true ? row["Family ID"].ToString() : null;

            if (famID != null)
            {
                int familyID = 0;
                bool success = int.TryParse(famID, out familyID);

                eqFamily eqFam = null;

                using (ngmdEntities db = new ngmdEntities())
                {
                    if (success)
                    {
                        eqFam = db.eqFamily.Find(familyID);
                    }
                    else
                    {
                        eqFam = db.eqFamily
                            .Where(x => x.description.ToLower().Trim() == famID.ToLower().Trim())
                            .FirstOrDefault();
                    }
                }

                fields.FamilyID = eqFam != null ? eqFam.eqFamilyID : 0;
            }
        }

        internal void ExtractFamilyID(DataRow row, FamilyMappingImportFields fields)
        {
            string famID = DataTableColExists(row, "Family ID") == true ? row["Family ID"].ToString() : null;

            if (famID != null)
            {
                int familyID = 0;
                bool success = int.TryParse(famID, out familyID);

                eqFamily eqFam = null;

                using (ngmdEntities db = new ngmdEntities())
                {
                    if (success)
                    {
                        eqFam = db.eqFamily.Find(familyID);
                    }
                    else
                    {
                        eqFam = db.eqFamily
                            .Where(x => x.description.ToLower().Trim() == famID.ToLower().Trim())
                            .FirstOrDefault();
                    }
                }

                fields.FamilyID = eqFam != null ? eqFam.eqFamilyID : 0;
            }
        }

        private static bool? SetBoolean(string value)
        {
            bool? returnValue = null;

            if (value != null)
            {
                switch (value.ToLower())
                {
                    case "y":
                        returnValue = true;
                        break;
                    case "n":
                        returnValue = false;
                        break;
                    case "true":
                        returnValue = true;
                        break;
                    case "false":
                        returnValue = false;
                        break;
                    default:
                        returnValue = null;
                        break;
                }
            }

            return returnValue;
        }
    }
}
