using netGiant.Intranet.BusinessLayer.Utilities;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Validation
{
    public class ImageCheckViewModel : CommonViewModel
    {
        private ngmdEntities _ctx;

        public ImageCheckViewModel()
        {
            _ctx = new ngmdEntities();
        }

        public eqEquipment EquipmentEntry { get; set; }
        public websiteInventory WebsiteInventoryEntry { get; set; }
        public IQueryable<TelerikImageCheck> ImageCheckList { get; set; }
        public string LocalDirectory { get; set; }
        public string FilePath { get; set; }

        public void GetImageCheckList()
        {
            List<SqlParameter> sqlParms = new List<SqlParameter>();
            DataTable imgData = SQLUtilities
                .ExecuteReadStoredProcedure("netgiantmasterdata", "ngmd.GetMissingImages", sqlParms, "imgdata")
                .Tables[0];

            // Convert DataTable to IQueryable
            ImageCheckList = imgData.AsEnumerable()
                .Select(row => new TelerikImageCheck
                {
                    EquipmentId = int.Parse(row["EquipmentId"].ToString()),
                    WebsiteInventoryId = int.Parse(row["WebsiteInventoryId"].ToString()),
                    Type = row["Type"].ToString(),
                    Name = row["Description"].ToString() ?? "",
                    PartNo = row["PartNo"].ToString(),
                    Website = row["Website"].ToString(),
                    EditLink = row["EditLink"].ToString()
                })                
                .AsQueryable();

            //List<int?> validStatus = _ctx.Lookup
            //    .Include(x => x.LookupType)
            //    .Where(x => x.LookupType.LookupTypeName == "ProductStatus" && x.LookupName == "Active" || x.LookupName == "Alert")
            //    .Select(x => x.AltLookupId)
            //    .ToList();

            //IQueryable<TelerikImageCheck> equipmentList = _ctx.eqEquipment
            //    .Where(x => string.IsNullOrEmpty(x.description) && !x.imageIsNotRequired)
            //    .Select(x => new TelerikImageCheck
            //    {
            //        EquipmentId = x.eqEquipmentID,
            //        WebsiteInventoryId = 0,
            //        Type = "Equipment",
            //        Name = x.description ?? "",
            //        PartNo = "",
            //        Website = "N/A",
            //        EditLink = "../Equipment/CreateEquipment/" + x.eqEquipmentID.ToString()
            //    });

            //IQueryable<TelerikImageCheck> inventoryList = _ctx.websiteInventory
            //    .Include(x => x.product)
            //    .Include(x => x.productImage)
            //    .Include(x => x.Website)
            //    .Where(x => x.productImage.Count == 0 && validStatus.Contains(x.product.productStatusFK) && !x.imageIsNotRequired)
            //    .Select(x => new TelerikImageCheck
            //    {
            //        EquipmentId = 0,
            //        WebsiteInventoryId = x.websiteInventoryID,
            //        Type = "Product",
            //        Name = x.product.productName,
            //        PartNo = x.product.partNo,
            //        Website = x.Website.Abbreviation,
            //        EditLink = "../Product/CreateProduct/" + x.productFK.ToString()
            //    });

            //ImageCheckList = equipmentList.Union(inventoryList)
            //    .AsQueryable();
        }

        public SaveReturn SetExcludedImage(int websiteInventoryId, int equipmentId)
        {
            var sr = new SaveReturn();

            using (ngmdEntities db = new ngmdEntities())
            {
                try
                {
                    if (websiteInventoryId > 0)
                    {
                        var wi = db.websiteInventory.Find(websiteInventoryId);
                        wi.imageIsNotRequired = !wi.imageIsNotRequired;
                        db.Entry(wi).State = EntityState.Modified;
                    }
                    if (equipmentId > 0)
                    {
                        var eq = db.eqEquipment.Find(equipmentId);
                        eq.imageIsNotRequired = !eq.imageIsNotRequired;
                        db.Entry(eq).State = EntityState.Modified;
                    }
                    db.SaveChanges();

                    sr.IsSuccess = true;
                }
                catch (Exception ex)
                {
                    sr.IsSuccess = false;
                    sr.Message = ex.Message;
                }
            }

            return sr;
        }

        public void CreateImageCheckCSVFile(List<TelerikImageCheck> imageCheckList)
        {
            FilePath = LocalDirectory + "\\PMSTempData\\BatchLogExport_" + DateTime.Now.ToString("dd_MM_yyyy_H_mm_ss") + ".csv";

            using (CsvFileWriter writer = new CsvFileWriter(FilePath, ','))
            {
                SetColumnHeadings(writer);

                foreach (TelerikImageCheck imageCheck in imageCheckList)
                {
                    InsertCSVData(writer, imageCheck);
                }
            }
        }

        private void InsertCSVData(CsvFileWriter writer, TelerikImageCheck imageCheck)
        {
            CsvRow newRow = new CsvRow();
            newRow.Add(imageCheck.Type);
            newRow.Add(imageCheck.Website);
            newRow.Add(imageCheck.PartNo ?? "");
            newRow.Add(imageCheck.Name ?? "");

            writer.WriteRow(newRow);
        }
        private void SetColumnHeadings(CsvFileWriter writer)
        {
            CsvRow firstRow = new CsvRow();
            firstRow.Add("Type");
            firstRow.Add("Website");
            firstRow.Add("PartNo");
            firstRow.Add("Description");

            writer.WriteRow(firstRow);
        }

        public class TelerikImageCheck
        {
            public int EquipmentId { get; set; }
            public int WebsiteInventoryId { get; set; }
            public string Type { get; set; }
            public string Name { get; set; }
            public string PartNo { get; set; }
            public string Website { get; set; }
            public string EditLink { get; set; }
        }
    }
}
