using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System;
using System.Data;
using System.Linq;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Import.ProductImages
{
    public class ExtractData
    {
        internal ProductImagesImportFields ExtractImageData(DataRow row, int currentRow)
        {
            ProductImagesImportFields fields = new ProductImagesImportFields();

            fields.altRef = DataTableColExists(row, "Alt Ref") == true ? row["Alt Ref"].ToString() : null;

            string manufacturer = DataTableColExists(row, "Manufacturer") == true ? row["Manufacturer"].ToString() : null;
            if (manufacturer != null)
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    manufacturer manu = db.manufacturer.Where(x => x.manufacturerName.ToLower() == manufacturer.ToLower())
                                                    .FirstOrDefault();
                    if (manu != null)
                    {
                        fields.manufacturerFK = manu.manufacturerID;
                    }
                }
            }

            string website = DataTableColExists(row, "Website") == true ? row["Website"].ToString() : null;
            if (website != null)
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    Website wbst = db.Website.Where(x => x.FriendlyName.ToLower() == website.ToLower())
                                                    .FirstOrDefault();
                    if (wbst != null)
                    {
                        fields.websiteFK = wbst.WebsiteID;
                    }
                }
            }

            fields.URL = DataTableColExists(row, "URL") == true ? row["URL"].ToString() : null;

            string isThumbnail = DataTableColExists(row, "IsThumbnail") == true ? row["IsThumbnail"].ToString().ToLower() : null;
            fields.isThumbnail = SetBoolean(isThumbnail);

            string isMain = DataTableColExists(row, "IsMain") == true ? row["IsMain"].ToString().ToLower() : null;
            fields.isMain = SetBoolean(isMain);

            fields.ACDModifier = DataTableColExists(row, "ACD Modifier") == true ? row["ACD Modifier"].ToString().ToUpper() : null;

            if (fields.altRef == null)
            {
                throw new ApplicationException("No Alt Ref was found for this row.");
            }

            if (fields.manufacturerFK == 0)
            {
                throw new ApplicationException("No Manufacturer was found for this row, or it was not matched in the PMS.");
            }

            if (fields.websiteFK == 0)
            {
                throw new ApplicationException("No Website was found for this row, or it was not matched in the PMS.");
            }

            if (string.IsNullOrEmpty(fields.URL))
            {
                throw new ApplicationException("No image URL was found for this row.");
            }

            if (!"ACD".Contains(fields.ACDModifier))
            {
                throw new ApplicationException("Invalid value for the ACDModifier column.");
            }

            using (ngmdEntities db = new ngmdEntities())
            {
                int productFK = db.product.Where(x => x.partNo == fields.altRef).FirstOrDefault().productID;
                websiteInventory origWi = db.websiteInventory
                            .Where(x => x.websiteFK == fields.websiteFK && x.productFK == productFK)
                            .FirstOrDefault();
                fields.websiteInventoryFK = origWi.websiteInventoryID;
            }

            return fields;
        }

        private static bool SetBoolean(string value)
        {
            bool returnValue = false;

            if (value != null)
            {
                switch (value.ToLower())
                {
                    case "1":
                        returnValue = true;
                        break;
                    case "0":
                    default:
                        returnValue = false;
                        break;
                }
            }

            return returnValue;
        }

        public static bool DataTableColExists(DataRow dr, string colName)
        {
            return dr.Table.Columns.Contains(colName);
        }

    }
}
