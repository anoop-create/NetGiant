using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using EntityState = System.Data.Entity.EntityState;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Import.Equipment
{
    public class SaveRecords
    {
        public List<EquipmentImportFields> EquipFields { get; set; }
        public List<EquipmentNotesImportFields> EquipNotesFields { get; set; }
        public List<FamilyImportFields> FamilyFields { get; set; }
        public List<FamilyMappingImportFields> FamilyMappingFields { get; set; }
        public List<EquipmentProductMappingImportFields> EquipProdMappingFields { get; set; }
        public List<EquipmentImportFields> EquipDeleteFields { get; set; }
        public List<FamilyImportFields> FamilyDeleteFields { get; set; }
        public List<FamilyMappingImportFields> FamilyMappingDeleteFields { get; set; }
        public List<EquipmentProductMappingImportFields> EquipProdMappingFieldsDelete { get; set; }

        public void Save(IJobStatusCommonViewModel JobStatusCommonViewModel)
        {
            try
            {
                SaveEquipment();
            }
            catch (Exception ex)
            {
                string errorString = "Could not save equipment entry";
                JobStatusCommonViewModel.SaveHadErrors = true;

                JobStatusCommonViewModel.WriteJobStatusRecord("Equipment - Working", errorString);
                JobStatusCommonViewModel.WriteJobStatusRecord("Equipment - Working", ex.Message);
            }

            try
            {
                SaveEquipmentNotes();
            }
            catch (Exception ex)
            {
                string errorString = "Could not save equipment notes entry";
                JobStatusCommonViewModel.SaveHadErrors = true;

                JobStatusCommonViewModel.WriteJobStatusRecord("Equipment Notes - Working", errorString);
                JobStatusCommonViewModel.WriteJobStatusRecord("Equipment Notes - Working", ex.Message);
            }

            try
            {
                SaveFamilies();
            }
            catch (Exception ex)
            {
                string errorString = "Could not save families entry";
                JobStatusCommonViewModel.SaveHadErrors = true;

                JobStatusCommonViewModel.WriteJobStatusRecord("Equipment - Working", errorString);
                JobStatusCommonViewModel.WriteJobStatusRecord("Equipment - Working", ex.Message);
            }

            try
            {
                SaveFamilyMappings();
            }
            catch (Exception ex)
            {
                string errorString = "Could not save family mappings entry";
                JobStatusCommonViewModel.SaveHadErrors = true;

                JobStatusCommonViewModel.WriteJobStatusRecord("Equipment - Working", errorString);
                JobStatusCommonViewModel.WriteJobStatusRecord("Equipment - Working", ex.Message);
            }

            try
            {
                SaveEquipProdMappings();
            }
            catch (Exception ex)
            {
                string errorString = "Could not save equipment prod mappings entry";
                JobStatusCommonViewModel.SaveHadErrors = true;

                JobStatusCommonViewModel.WriteJobStatusRecord("Equipment - Working", errorString);
                JobStatusCommonViewModel.WriteJobStatusRecord("Equipment - Working", ex.Message);
            }

            try
            {
                DeleteEquipment();
            }
            catch (Exception ex)
            {
                string errorString = "Could not delete equipment entry";
                JobStatusCommonViewModel.SaveHadErrors = true;

                JobStatusCommonViewModel.WriteJobStatusRecord("Equipment - Working", errorString);
                JobStatusCommonViewModel.WriteJobStatusRecord("Equipment - Working", ex.Message);
            }

            try
            {
                DeleteFamilies();
            }
            catch (Exception ex)
            {
                string errorString = "Could not delete families entry";
                JobStatusCommonViewModel.SaveHadErrors = true;

                JobStatusCommonViewModel.WriteJobStatusRecord("Equipment - Working", errorString);
                JobStatusCommonViewModel.WriteJobStatusRecord("Equipment - Working", ex.Message);
            }

            try
            {
                DeleteFamilyMappings();
            }
            catch (Exception ex)
            {
                string errorString = "Could not delete family mappings entry";
                JobStatusCommonViewModel.SaveHadErrors = true;

                JobStatusCommonViewModel.WriteJobStatusRecord("Equipment - Working", errorString);
                JobStatusCommonViewModel.WriteJobStatusRecord("Equipment - Working", ex.Message);
            }

            try
            {
                DeleteEquipProdMappings();
            }
            catch (Exception ex)
            {
                string errorString = "Could not delete equipment prod mappings entry";
                JobStatusCommonViewModel.SaveHadErrors = true;

                JobStatusCommonViewModel.WriteJobStatusRecord("Equipment - Working", errorString);
                JobStatusCommonViewModel.WriteJobStatusRecord("Equipment - Working", ex.Message);
            }

        }

        private void SaveEquipment()
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                foreach (var equipment in EquipFields)
                {
                    var dbEquip = equipment.EquipID > 0 ? db.eqEquipment.Find(equipment.EquipID) : null;

                    if (dbEquip != null)
                    {
                        SetEquipment(equipment, dbEquip);
                        db.Entry(dbEquip).State = EntityState.Modified;
                    }
                    else
                    {
                        eqEquipment newEquip = new eqEquipment();
                        SetEquipment(equipment, newEquip);
                        db.Entry(newEquip).State = EntityState.Added;
                    }
                }

                db.SaveChanges();
            }
        }

        private void SaveEquipmentNotes()
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                foreach (var equipmentNote in EquipNotesFields)
                {
                    var dbEquipNote = equipmentNote.EquipNotesID > 0 ? db.equipmentNotes.Find(equipmentNote.EquipNotesID) : null;

                    // Check 

                    if (dbEquipNote == null)
                    {
                        // NEW CHECKS: Make sure there isn't already an entry for this equipment/website/isDetail combo
                        var dbCheck = db.equipmentNotes.Where(x => x.eqEquipmentFK == equipmentNote.EquipmentID && x.websiteFK == equipmentNote.WebsiteID && x.isDetail == equipmentNote.IsDetail).FirstOrDefault();
                        if (dbCheck != null)
                        {
                            // Error
                            throw new Exception("A Notes entry already exists in the database for Equipment ID/Website ID/IsDetail: " + equipmentNote.EquipmentID.ToString() + "/" + equipmentNote.WebsiteID.ToString() + "/" + equipmentNote.IsDetail.ToString());
                        }
                    }
                    else
                    {
                        // EXISTING CHECKS: Make sure WebsiteID, EqipmentID and IsDetail haven't been changed
                        if (dbEquipNote.websiteFK != equipmentNote.WebsiteID || dbEquipNote.eqEquipmentFK != equipmentNote.EquipmentID || dbEquipNote.isDetail != equipmentNote.IsDetail)
                        {
                            // Error
                            throw new Exception("Invalid attempt to change key fields (Equipment ID/Website ID/IsDetail) for Equipment Notes ID: " + equipmentNote.EquipNotesID.ToString());
                        }
                    }

                    if (dbEquipNote != null)
                    {
                        SetEquipmentNote(equipmentNote, dbEquipNote);
                        db.Entry(dbEquipNote).State = EntityState.Modified;
                    }
                    else
                    {
                        equipmentNotes newEquipNote = new equipmentNotes();
                        SetEquipmentNote(equipmentNote, newEquipNote);
                        db.Entry(newEquipNote).State = EntityState.Added;
                    }
                }

                db.SaveChanges();
            }
        }

        private void SaveFamilies()
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                foreach (var family in FamilyFields)
                {
                    var dbFamily = family.FamilyID > 0 ? db.eqFamily.Find(family.FamilyID) : null;

                    if (dbFamily != null)
                    {
                        SetFamily(family, dbFamily);
                        db.Entry(dbFamily).State = EntityState.Modified;
                    }
                    else
                    {
                        eqFamily newFamily = new eqFamily();
                        SetFamily(family, newFamily);
                        db.Entry(newFamily).State = EntityState.Added;
                    }
                }

                db.SaveChanges();
            }
        }

        private void SaveFamilyMappings()
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                foreach (var mapping in FamilyMappingFields)
                {
                    eqFamilyMembership dbFamilyMapping = null;

                    if (mapping.EquipID > 0 && mapping.FamilyID > 0)
                    {
                        dbFamilyMapping = db.eqFamilyMembership
                            .Where(x => x.eqEquipmentID == mapping.EquipID && x.eqFamilyID == mapping.FamilyID)
                            .FirstOrDefault();
                    }

                    if (dbFamilyMapping == null)
                    {
                        eqFamilyMembership newFamilyMapping = new eqFamilyMembership();
                        SetFamilyMapping(mapping, newFamilyMapping);
                        db.Entry(newFamilyMapping).State = EntityState.Added;
                    }
                }

                db.SaveChanges();
            }
        }

        private void SaveEquipProdMappings()
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                foreach (var mapping in EquipProdMappingFields)
                {
                    eqProductMembership dbProductMembership = null;

                    if (mapping.EquipID > 0 && mapping.ProductID > 0)
                    {
                        dbProductMembership = db.eqProductMembership
                            .Where(x => x.eqEquipmentFK == mapping.EquipID && x.productFK == mapping.ProductID)
                            .FirstOrDefault();
                    }

                    if (dbProductMembership == null)
                    {
                        eqProductMembership newProductMembership = new eqProductMembership();
                        SetProductMembership(mapping, newProductMembership);
                        db.Entry(newProductMembership).State = EntityState.Added;
                    }
                }

                db.SaveChanges();
            }
        }

        private static void SetEquipment(EquipmentImportFields fields, eqEquipment equip)
        {
            equip.dateLastUpdate = DateTime.Now;
            equip.description = fields.EquipDescription ?? equip.description;
            equip.eqCartridgeTypeFK = fields.EquipCartTypeFK == 0 ? equip.eqCartridgeTypeFK : fields.EquipCartTypeFK;

            equip.mainURL = fields.EquipMainURL == "##NotSet##" ? equip.mainURL : 
                string.IsNullOrEmpty(fields.EquipMainURL) ? equip.mainURL = null : 
                fields.EquipMainURL;

            equip.manufacturerFK = fields.EquipManuFK == 0 ? equip.manufacturerFK : fields.EquipManuFK;
            equip.metaKeywords = fields.EquipMetaKeywords ?? equip.metaKeywords;
            equip.metaContentTypeFK = fields.EquipMetaContentTypeFK == 0 ? equip.metaContentTypeFK : fields.EquipMetaContentTypeFK;
            equip.productFK = fields.EquipProductFK ?? equip.productFK;
            equip.productTypeFK = fields.EquipProductTypeFK == 0 ? equip.productTypeFK : fields.EquipProductTypeFK;

            equip.thumbnailURL = fields.EquipThumbnailURL == "##NotSet##" ? equip.thumbnailURL :
                string.IsNullOrEmpty(fields.EquipThumbnailURL) ? equip.thumbnailURL = null :
                fields.EquipThumbnailURL;

            equip.globallyFeatured = fields.GloballyFeatured == null ? equip.globallyFeatured : fields.GloballyFeatured ?? false;
            equip.homeFeatured = fields.HomeFeatured == null ? equip.homeFeatured : fields.HomeFeatured ?? false;
            equip.brandFeatured = fields.BrandFeatured == null ? equip.brandFeatured : fields.BrandFeatured ?? false;
            //equip.statusFK = 1;
            equip.statusFK = fields.EquipStatusFK == 0 ? equip.statusFK : Convert.ToByte(fields.EquipStatusFK);
        }

        private static void SetEquipmentNote(EquipmentNotesImportFields fields, equipmentNotes equipNote)
        {
            equipNote.note = fields.EquipNote ?? equipNote.note;
            equipNote.eqEquipmentFK = fields.EquipmentID == 0 ? equipNote.eqEquipmentFK : fields.EquipmentID;
            equipNote.websiteFK = fields.WebsiteID == 0 ? equipNote.websiteFK : fields.WebsiteID;
            equipNote.isDetail = fields.IsDetail == null ? equipNote.isDetail : fields.IsDetail ?? false;
        }

        private void SetFamily(FamilyImportFields fields, eqFamily family)
        {
            family.dateLastUpdate = DateTime.Now;
            family.description = fields.FamilyDescription;
            family.manufacturerFK = fields.FamilyManuFK;
        }

        private void SetFamilyMapping(FamilyMappingImportFields fields, eqFamilyMembership mapping)
        {
            mapping.eqEquipmentID = fields.EquipID;
            mapping.eqFamilyID = fields.FamilyID;
        }

        private void SetProductMembership(EquipmentProductMappingImportFields fields, eqProductMembership prodMembership)
        {
            prodMembership.eqEquipmentFK = fields.EquipID;
            prodMembership.productFK = fields.ProductID;
        }

        private void DeleteEquipment()
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                foreach (var equipment in EquipDeleteFields)
                {
                    var dbEquip = db.eqEquipment.Find(equipment.EquipID);
                    db.Entry(dbEquip).State = EntityState.Deleted;
                }

                db.SaveChanges();
            }
        }

        private void DeleteFamilies()
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                foreach (var family in FamilyDeleteFields)
                {
                    var dbFamily = db.eqFamily.Find(family.FamilyID);
                    db.Entry(dbFamily).State = EntityState.Deleted;
                }

                db.SaveChanges();
            }
        }

        private void DeleteFamilyMappings()
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                foreach (var family in FamilyMappingDeleteFields)
                {
                    var dbFamilyMap = db.eqFamilyMembership
                        .Where(x => x.eqFamilyID == family.FamilyID && 
                            x.eqEquipmentID == family.EquipID).FirstOrDefault();

                    if (dbFamilyMap != null)
                        db.Entry(dbFamilyMap).State = EntityState.Deleted;
                }

                db.SaveChanges();
            }
        }

        private void DeleteEquipProdMappings()
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                foreach (var equip in EquipProdMappingFieldsDelete)
                {
                    var dbProdMap = db.eqProductMembership
                        .Where(x => x.eqEquipmentFK == equip.EquipID &&
                            x.productFK == equip.ProductID).FirstOrDefault();

                    if (dbProdMap != null)
                        db.Entry(dbProdMap).State = EntityState.Deleted;
                }

                db.SaveChanges();
            }
        }
    }
}
