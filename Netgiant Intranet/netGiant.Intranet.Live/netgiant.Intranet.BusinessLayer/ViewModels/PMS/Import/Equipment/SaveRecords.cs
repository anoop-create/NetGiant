using netGiant.Intranet.DataLayer;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Import.Equipment
{
    public class SaveRecords
    {
        public List<EquipmentImportFields> EquipFields { get; set; }
        public List<FamilyImportFields> FamilyFields { get; set; }
        public List<FamilyMappingImportFields> FamilyMappingFields { get; set; }
        public List<EquipmentProductMappingImportFields> EquipProdMappingFields { get; set; }
        public List<EquipmentImportFields> EquipDeleteFields { get; set; }
        public List<FamilyImportFields> FamilyDeleteFields { get; set; }
        public List<FamilyMappingImportFields> FamilyMappingDeleteFields { get; set; }
        public List<EquipmentProductMappingImportFields> EquipProdMappingFieldsDelete { get; set; }

        public void Save()
        {
            SaveEquipment();
            SaveFamilies();
            SaveFamilyMappings();
            SaveEquipProdMappings();
            DeleteEquipment();
            DeleteFamilies();
            DeleteFamilyMappings();
            DeleteEquipProdMappings();
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
            equip.mainURL = fields.EquipMainURL ?? equip.mainURL;
            equip.manufacturerFK = fields.EquipManuFK == 0 ? equip.manufacturerFK : fields.EquipManuFK;
            equip.metaKeywords = fields.EquipMetaKeywords ?? equip.metaKeywords;
            equip.metaContentTypeFK = fields.EquipMetaContentTypeFK == 0 ? equip.metaContentTypeFK : fields.EquipMetaContentTypeFK;
            equip.productFK = fields.EquipProductFK ?? equip.productFK;
            equip.productTypeFK = fields.EquipProductTypeFK == 0 ? equip.productTypeFK : fields.EquipProductTypeFK;
            equip.thumbnailURL = fields.EquipThumbnailURL ?? equip.thumbnailURL;
            equip.globallyFeatured = fields.GloballyFeatured == null ? equip.globallyFeatured : fields.GloballyFeatured ?? false;
            equip.brandFeatured = fields.BrandFeatured == null ? equip.brandFeatured : fields.BrandFeatured ?? false;
            equip.statusFK = 1;
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
