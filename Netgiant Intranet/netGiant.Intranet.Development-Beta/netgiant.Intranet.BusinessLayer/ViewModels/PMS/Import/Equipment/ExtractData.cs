using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System;
using System.Data;
using System.Linq;
using System.Web.Mvc;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Import.Equipment
{
    public class ExtractData
    {
        public enum RecordType
        {
            Equipment,
            EquipmentNote,
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
                    case "equipmentnote":
                        returnEnum = RecordType.EquipmentNote;
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

        internal void ExtractEquipmentID(DataRow row, EquipmentNotesImportFields fields)
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
                        // Error Invalid Equipment ID
                    }
                }

                fields.EquipmentID = eqEquip != null ? eqEquip.eqEquipmentID : 0;
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

        internal void ExtractEquipStatus(DataRow row, EquipmentImportFields fields)
        {
            string equipStatus = DataTableColExists(row, "Equip Status") == true ? row["Equip Status"].ToString() : null;

            if (equipStatus != null)
            {
                int equipStatusFK = 0;
                bool success = int.TryParse(equipStatus, out equipStatusFK);

                Lookup eqType;

                using (ngmdEntities db = new ngmdEntities())
                {
                    if (success)
                    {
                        eqType = db.Lookup
                            .Where(x => x.LookupType.LookupTypeName == "EquipmentStatus" && x.AltLookupId == equipStatusFK)
                            .FirstOrDefault();
                    }
                    else
                    {
                        eqType = db.Lookup
                            .Where(x => x.LookupType.LookupTypeName == "EquipmentStatus" && x.LookupName.ToLower() == equipStatus.ToLower())
                            .FirstOrDefault();
                    }
                }

                if (eqType != null)
                {
                    fields.EquipStatusFK = eqType.AltLookupId ?? 2;
                }
                else
                {
                    throw new ApplicationException("Product Type '" + equipStatus + "' not matched in the PMS");
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

                Lookup eqCartType;

                using (ngmdEntities db = new ngmdEntities())
                {
                    if (success)
                    {
                        eqCartType = db.Lookup
                            .Where(x => x.LookupType.LookupTypeName == "CartridgeType" && x.AltLookupId == equipCartTypeFK)
                            .FirstOrDefault();
                    }
                    else
                    {
                        eqCartType = db.Lookup
                            .Where(x => x.LookupType.LookupTypeName == "CartridgeType" && x.LookupName == equipCartType)
                            .FirstOrDefault();
                    }

                    if (eqCartType != null)
                    {
                        fields.EquipCartTypeFK = eqCartType.AltLookupId.Value;
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

            string equipHomeFeatured = DataTableColExists(row, "Home Featured") == true ? row["Home Featured"].ToString() : null;
            fields.HomeFeatured = SetBoolean(equipHomeFeatured);

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

                Lookup prodType;

                using (ngmdEntities db = new ngmdEntities())
                {
                    if (success)
                    {
                        prodType = db.Lookup
                            .Where(x => x.LookupType.LookupTypeName == "ProductType" && x.AltLookupId == equipProductTypeFK)
                            .FirstOrDefault();
                    }
                    else
                    {
                        prodType = db.Lookup
                            .Where(x => x.LookupType.LookupTypeName == "ProductType" && x.LookupName.ToLower() == equipProductType.ToLower())
                            .FirstOrDefault();
                    }
                }

                if (prodType != null)
                {
                    fields.EquipProductTypeFK = prodType.AltLookupId ?? 2;
                }
                else
                {
                    throw new ApplicationException("Product Type '" + equipProductType + "' not matched in the PMS");
                }
            }
        }

        internal void ExtractMainURL(DataRow row, EquipmentImportFields fields)
        {
            fields.EquipMainURL = DataTableColExists(row, "Equip Main URL") == true ? row["Equip Main URL"].ToString() : fields.EquipMainURL = "##NotSet##";
        }

        internal void ExtractThumbnailURL(DataRow row, EquipmentImportFields fields)
        {
            fields.EquipThumbnailURL = DataTableColExists(row, "Equip Thumbnail URL") == true ? row["Equip Thumbnail URL"].ToString() : fields.EquipThumbnailURL = "##NotSet##";
        }

        internal void ExtractMetaKeywords(DataRow row, EquipmentImportFields fields)
        {
            string equipMetaKeywords = DataTableColExists(row, "Equip Meta Keywords") == true ? row["Equip Meta Keywords"].ToString() : null;

            if (equipMetaKeywords != null)
            {
                fields.EquipMetaKeywords = equipMetaKeywords;
            }
        }

        internal void ExtractMetaTitle(DataRow row, EquipmentImportFields fields)
        {
            string equipMetaTitle = DataTableColExists(row, "Equip Meta Title") == true ? row["Equip Meta Title"].ToString() : null;

            if (equipMetaTitle != null)
            {
                fields.EquipMetaTitle = equipMetaTitle;
            }
        }

        internal void ExtractMetaDescription(DataRow row, EquipmentImportFields fields)
        {
            string equipMetaDescription = DataTableColExists(row, "Equip Meta Description") == true ? row["Equip Meta Description"].ToString() : null;

            if (equipMetaDescription != null)
            {
                fields.EquipMetaDescription = equipMetaDescription;
            }
        }

        internal void ExtractMetaContentType(DataRow row, EquipmentImportFields fields)
        {
            string equipMetaContentType = DataTableColExists(row, "Equip Meta Content Type") == true ? row["Equip Meta Content Type"].ToString() : null;

            if (equipMetaContentType != null)
            {
                int equipMetaContentTypeFK = 0;
                bool success = int.TryParse(equipMetaContentType, out equipMetaContentTypeFK);

                Lookup metaContentType;

                using (ngmdEntities db = new ngmdEntities())
                {
                    if (success)
                    {
                        metaContentType = db.Lookup
                            .Where(x => x.LookupType.LookupTypeName == "MetaContentType" && x.AltLookupId == equipMetaContentTypeFK)
                            .FirstOrDefault();
                    }
                    else
                    {
                        metaContentType = db.Lookup
                            .Where(x => x.LookupType.LookupTypeName == "MetaContentType" && x.LookupName.ToLower() == equipMetaContentType.ToLower())
                            .FirstOrDefault();
                    }
                }

                if (metaContentType != null)
                {
                    fields.EquipMetaContentTypeFK = Convert.ToByte(metaContentType.AltLookupId);
                }
                else
                {
                    throw new ApplicationException("Metqa Content Type '" + equipMetaContentType + "' not matched in the PMS");
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

        internal void ExtractEquipNotesID(DataRow row, EquipmentNotesImportFields fields)
        {
            string equipNotesID = DataTableColExists(row, "Equip Notes ID") == true ? row["Equip Notes ID"].ToString() : null;

            if (equipNotesID != null)
            {
                int equipmentNotesID = 0;
                bool success = int.TryParse(equipNotesID, out equipmentNotesID);

                equipmentNotes equipNotes = null;

                using (ngmdEntities db = new ngmdEntities())
                {
                    if (success)
                    {
                        equipNotes = db.equipmentNotes.Find(equipmentNotesID);
                    }
                    else
                    {
                        // Error invalid ID
                        //equipNotes = db.equipmentNotes
                        //    .Where(x => x.description.ToLower().Trim() == equipNotesID.ToLower().Trim())
                        //    .FirstOrDefault();
                    }
                }

                fields.EquipNotesID = equipNotes != null ? equipNotes.equipmentNotesID : 0;
            }
        }

        internal void ExtractWebsiteID(DataRow row, EquipmentNotesImportFields fields)
        {
            string wsID = DataTableColExists(row, "Website ID") == true ? row["Website ID"].ToString() : null;

            if (wsID != null)
            {
                int websiteID = 0;
                bool success = int.TryParse(wsID, out websiteID);

                Website website = null;

                using (ngmdEntities db = new ngmdEntities())
                {
                    if (success)
                    {
                        website = db.Website.Find(websiteID);
                    }
                    else
                    {
                        // Error invalid ID
                    }
                }

                fields.WebsiteID = website != null ? website.WebsiteID : 0;
            }
        }

        internal void ExtractEquipNotesNote(DataRow row, EquipmentNotesImportFields fields)
        {
            string equipNote = DataTableColExists(row, "Equip Note") == true ? row["Equip Note"].ToString() : null;

            if (equipNote != null)
            {
                fields.EquipNote = equipNote;
            }
        }

        internal void ExtractEquipNotesIsDetail(DataRow row, EquipmentNotesImportFields fields)
        {
            string isDetail = DataTableColExists(row, "Is Detail") == true ? row["Is Detail"].ToString() : null;
            fields.IsDetail = SetBoolean(isDetail);
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
