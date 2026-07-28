using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web.Mvc;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Import.Product
{
    public class ExtractData
    {
        public ExtractData(string axisLang, int _websiteFK, int _importPrimaryKey)
        {
            //stnFunc = func;
            axisLanguage = axisLang;
            websiteFK = _websiteFK;
            importPrimaryKey = _importPrimaryKey;
        }

        public enum RecordType
        {
            Product,
            SkuMapping,
            eBusinessMapping
        }

        //private Shared.StandardFunctions stnFunc;
        private string axisLanguage = string.Empty;
        private int websiteFK;
        private int importPrimaryKey;

        internal static RecordType ExtractRecordType(DataRow row)
        {
            RecordType returnEnum = RecordType.Product;
            string recType = DataTableColExists(row, "Record Type") == true ? row["Record Type"].ToString() : null;

            if (recType != null)
            {
                if (recType.ToLower() == "skumapping")
                {
                    returnEnum = RecordType.SkuMapping;
                }
                else if (recType.ToLower() == "ebus")
                {
                    returnEnum = RecordType.eBusinessMapping;
                }
            }

            return returnEnum;
        }

        internal ProductFields ExtractKeyData(DataRow row, int currentRow)
        {
            //stnFunc.AddToActivityLog("Extracting Key Data for row " + currentRow);

            var fields = new ProductFields();

            fields.PartNo = DataTableColExists(row, "Alt Ref") == true ? row["Alt Ref"].ToString() : null;

            fields.StockRef = DataTableColExists(row, "Stock Reference") == true ? row["Stock Reference"].ToString() : null;

            string manufacturer = DataTableColExists(row, "Manufacturer") == true ? row["Manufacturer"].ToString() : null;

            if (manufacturer != null)
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    manufacturer manu = db.manufacturer.Where(x => x.manufacturerName.ToLower() == manufacturer.ToLower())
                                                    .FirstOrDefault();
                    if (manu != null)
                    {
                        fields.ManufacturerFK = manu.manufacturerID;
                    }
                }
            }
            else
            {
                if (DataTableColExists(row, "Attribute 10"))
                {
                    string attrVal = DataTableColExists(row, "Attribute 10") == true ? row["Attribute 10"].ToString() : null;
                    int attrValID = 0;

                    bool success = int.TryParse(attrVal, out attrValID);

                    if (success)
                    {
                        fields.Attr10 = attrValID;
                        AxisValueLookup avl = LookupAxisManufacturer(attrValID);
                        manufacturer manu = GetManufacturerFK(avl);
                        if (manu != null)
                        {
                            fields.ManufacturerFK = manu.manufacturerID;
                        }
                    }
                }
            }

            if (importPrimaryKey == 1 && fields.PartNo == null)
            {
                throw new ApplicationException("No Alt Ref was found for this row.");
            }

            if (importPrimaryKey == 2 && fields.StockRef == null)
            {
                throw new ApplicationException("No Stock Ref was found for this row.");
            }

            if (fields.ManufacturerFK == null)
            {
                throw new ApplicationException("No Manufacturer was found for this row, or it was not matched in the PMS.");
            }

            fields.WebsiteFK = websiteFK;
            fields.IsNew = CheckProductIsNew(fields);

            return fields;
        }

        internal void ExtractUnspsc(DataRow row, ProductFields fields)
        {
            fields.UnspscCode = DataTableColExists(row, "UNSPSC") == true ? row["UNSPSC"].ToString() : null;
        }

        internal void ExtractPageYield(DataRow row, ProductFields fields)
        {
            string pageYield = DataTableColExists(row, "Page Yield") == true ? row["Page Yield"].ToString() : null;

            if (pageYield != null)
            {
                int iPageYield = 0;
                bool success = int.TryParse(pageYield, out iPageYield);

                if (success)
                {
                    fields.PageYield = iPageYield;
                }
                else
                {
                    fields.PageYield = null;
                }
            }
        }

        internal void ExtractCapacity(DataRow row, ProductFields fields)
        {
            fields.Capacity = DataTableColExists(row, "Capacity") == true ? row["Capacity"].ToString() : null;
        }

        internal void ExtractBarcode(DataRow dr, ProductFields fields)
        {
            string barcode = DataTableColExists(dr, "Barcode") == true ? dr["Barcode"].ToString() : null;

            if (barcode != null)
            {
                fields.Barcode = barcode;
            }
        }

        internal void ExtractDiscontinued(DataRow row, ProductFields fields)
        {
            string discontinued = DataTableColExists(row, "Discontinued Item") == true ?
                                        row["Discontinued Item"].ToString().ToLower() : null;
            fields.DiscontinuedItem = SetBoolean(discontinued);
        }

        internal void ExtractProductName(DataRow row, ProductFields fields)
        {
            fields.ProductName = DataTableColExists(row, "Product Name") == true ? row["Product Name"].ToString() : null;
        }

        internal void ExtractAssemblyComponents(DataRow row, ProductFields fields)
        {
            string assemblyComponents = DataTableColExists(row, "Assembly Components") == true ? row["Assembly Components"].ToString() : null;

            if (assemblyComponents == null) // not in spreadsheet at all
                return;

            fields.AssemblyComponents = new List<int>();

            if (!string.IsNullOrEmpty(assemblyComponents))
            {
                var codesStrArray = assemblyComponents.Split(',');
                for (int i = 0; i<codesStrArray.Length; i++)
                {
                    codesStrArray[i] = codesStrArray[i].Trim(' ');
                }

                using (ngmdEntities db = new ngmdEntities())
                {
                    foreach (var codeStr in codesStrArray)
                    {
                        var assemblyComponent = db.product
                                                    .Where(x => x.partNo.ToUpper() == codeStr.ToUpper())
                                                    .FirstOrDefault();

                        if (assemblyComponent != null)
                        {
                            fields.AssemblyComponents.Add(assemblyComponent.productID);
                        }
                        else
                        {
                            throw new ApplicationException("Assembly Component could not be found");
                        }
                    }
                }
            }
        }

        internal void ExtractProductCrossSellGroup(DataRow row, ProductFields fields)
        {
            fields.SecondaryCrossSellGroup = DataTableColExists(row, "Secondary Cross Sell Group") == true ? Convert.ToInt32(row["Secondary Cross Sell Group"]) : (int?)null;
        }

        internal void ExtractStockRecordType(DataRow row, ProductFields fields)
        {
            fields.StockRecordType = DataTableColExists(row, "Stock Record Type") == true ? row["Stock Record Type"].ToString() : null;   
        }

        internal void ExtractSpecification(DataRow row, ProductFields fields)
        {
            string spec1 = DataTableColExists(row, "Specification 1") == true ? row["Specification 1"].ToString() : null;
            string spec2 = DataTableColExists(row, "Specification 2") == true ? row["Specification 2"].ToString() : null;
            string spec3 = DataTableColExists(row, "Specification 3") == true ? row["Specification 3"].ToString() : null;
            string spec4 = DataTableColExists(row, "Specification 4") == true ? row["Specification 4"].ToString() : null;
            string spec5 = DataTableColExists(row, "Specification 5") == true ? row["Specification 5"].ToString() : null;
            string spec6 = DataTableColExists(row, "Specification 6") == true ? row["Specification 6"].ToString() : null;

            fields.Spec1 = spec1;
            fields.Spec2 = spec2;
            fields.Spec3 = spec3;
            fields.Spec4 = spec4;
            fields.Spec5 = spec5;
            fields.Spec6 = spec6;
        }

        internal void ExtractDataSupplier(DataRow row, ProductFields fields)
        {
            string dataSupplier = DataTableColExists(row, "Data Supplier") == true ? row["Data Supplier"].ToString() : null;

            if (dataSupplier != null)
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    dataSupplier ds = db.dataSupplier.Where(x => x.dataSupplierName.ToLower() == dataSupplier.ToLower())
                                                        .FirstOrDefault();
                    if (ds != null)
                    {
                        fields.DataSupplierFK = ds.dataSupplierID;
                    }
                    else
                    {
                        throw new ApplicationException("Data Supplier '" + dataSupplier + "' not matched in the PMS");
                    }
                }
            }
        }

        internal void ExtractSalesAreaGroup(DataRow row, ProductFields fields)
        {
            string salesAreaGroup = DataTableColExists(row, "Sales Area Group") == true ? row["Sales Area Group"].ToString() : null;
            int iSalesAreaGroup = 0;
            bool success = int.TryParse(salesAreaGroup, out iSalesAreaGroup);

            if (salesAreaGroup != null)
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    salesAreaGroup salAreaGrp;

                    if (success)
                    {
                        salAreaGrp = db.salesAreaGroup.Where(x => x.salesAreaGroupNo == salesAreaGroup)
                                                        .FirstOrDefault();
                    }
                    else
                    {
                        salAreaGrp = db.salesAreaGroup.Where(x => x.salesAreaGroupName.ToLower() == salesAreaGroup.ToLower())
                                                        .FirstOrDefault();
                    }

                    if (salAreaGrp != null)
                    {
                        fields.SalesAreaGroupFK = salAreaGrp.salesAreaGroupID;
                    }
                    else
                    {
                        throw new ApplicationException("Sales Area Group '" + salesAreaGroup + "' not matched in the PMS");
                    }
                }
            }
        }

        internal void ExtractProductGroup(DataRow row, ProductFields fields)
        {
            string group = DataTableColExists(row, "Product Group") == true ? row["Product Group"].ToString() : null;
            int iGroup = 0;
            bool success = int.TryParse(group, out iGroup);

            if (group != null)
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    productGroup prdGrp;

                    if (success)
                    {
                        prdGrp = db.productGroup.Where(x => x.productGroupNo == group)
                                                .FirstOrDefault();
                    }
                    else
                    {
                        prdGrp = db.productGroup.Where(x => x.productGroupName.ToLower() == group.ToLower())
                                                .FirstOrDefault();
                    }

                    if (prdGrp != null)
                    {
                        fields.ProductGroupFK = prdGrp.productGroupID;
                    }
                    else
                    {
                        throw new ApplicationException("Product Group '" + group + "' not matched in the PMS");
                    }
                }
            }
        }

        internal void ExtractProductStatus(DataRow row, ProductFields fields)
        {
            string status = DataTableColExists(row, "Product Status") == true ? row["Product Status"].ToString() : null;

            if (status != null)
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    Lookup l = DataCache.GetNgmdLookups(x => x.LookupType.LookupTypeName == "ProductStatus")
                        .FirstOrDefault(x => x.LookupName.ToLower() == status.ToLower());
                    if (l != null)
                    {
                        fields.ProductStatusFK = l.AltLookupId;
                    }
                    else
                    {
                        throw new ApplicationException("Product Status '" + status + "' not matched in the PMS");
                    }
                }
            }
        }

        internal void ExtractDefaultDelivery(DataRow row, ProductFields fields)
        {
            string daysString = DataTableColExists(row, "Default delivery days") == true ? row["Default delivery days"].ToString() : null;
            int defDelDays = 0;
            bool success = int.TryParse(daysString, out defDelDays);

            if (success)
            {
                fields.DefaultDeliveryToCust = defDelDays;
            }
            else
            {
                fields.DefaultDeliveryToCust = null;
            }
        }

        internal void ExtractAttributes(DataRow row, ProductFields fields)
        {
            for (var i = 1; i < 11; i++)
            {
                switch (i)
                {
                    case 1:
                        fields.Attr1 = ExtractSingleAttribute(row, i);
                        break;
                    case 2:
                        fields.Attr2 = ExtractSingleAttribute(row, i);
                        break;
                    case 3:
                        fields.Attr3 = ExtractSingleAttribute(row, i);
                        break;
                    case 4:
                        fields.Attr4 = ExtractSingleAttribute(row, i);
                        break;
                    case 5:
                        fields.Attr5 = ExtractSingleAttribute(row, i);
                        break;
                    case 6:
                        fields.Attr6 = ExtractSingleAttribute(row, i);
                        break;
                    case 7:
                        fields.Attr7 = ExtractSingleAttribute(row, i);
                        break;
                    case 8:
                        fields.Attr8 = ExtractSingleAttribute(row, i);
                        break;
                    case 9:
                        fields.Attr9 = ExtractSingleAttribute(row, i);
                        break;
                    case 10:
                        if (fields.Attr10 != 0)
                        {
                            fields.Attr10 = ExtractSingleAttribute(row, i);
                        }
                        break;
                }
            }
        }

        internal int? ExtractSingleAttribute(DataRow row, int att)
        {
            string attr = DataTableColExists(row, "Attribute " + att) == true ? row["Attribute " + att].ToString() : null;

            if (attr == null)
                return null;
            
            int attrID = 0;

            if (attr != "0" && attr != string.Empty)
            {
                int.TryParse(attr, out attrID);

                if (attrID == 0)
                {
                    //Try to get by the name
                    int lookupId = LookupAxisAttributeValueID(attr, att);
                    if (lookupId > 0)
                        attrID = lookupId;
                }

                if ((attr != null && attrID == 0) || (!ValidateAxisAttributeId(attrID, att)))
                {
                    throw new ApplicationException("Attribute " + att + " value not found in PMS");
                }
            }

            return attrID;
        }

        internal void ExtractMetaData(DataRow row, ProductFields fields)
        {
            string mTitle = DataTableColExists(row, axisLanguage + " Meta Title") == true ? row[axisLanguage + " Meta Title"].ToString() : null;
            string mKeywords = DataTableColExists(row, axisLanguage + " Meta Keywords") == true ? row[axisLanguage + " Meta Keywords"].ToString() : null;
            string mDesc = DataTableColExists(row, axisLanguage + " Meta Description") == true ? row[axisLanguage + " Meta Description"].ToString() : null;
            string addInfoUrl = DataTableColExists(row, "Additional Information URL") == true ? row["Additional Information URL"].ToString() : null;

            fields.MetaTitle = mTitle;
            fields.MetaKeywords = mKeywords;
            fields.MetaDesc = mDesc;
            fields.AdditionalInfoUrl = addInfoUrl;
        }

        internal void ExtractEbusinessDetails(DataRow row, ProductFields fields)
        {
            string suppressOpenRangeImg = DataTableColExists(row, "Suppress open range image") == true ? row["Suppress open range image"].ToString().ToLower() : null;
            string suppressOpenRangeSpec = DataTableColExists(row, "Suppress open range spec") == true ? row["Suppress open range spec"].ToString().ToLower() : null;
            string featuredItem = DataTableColExists(row, "Featured item") == true ? row["Featured item"].ToString().ToLower() : null;
            string bestSeller = DataTableColExists(row, "Best seller") == true ? row["Best seller"].ToString().ToLower() : null;
            string published = DataTableColExists(row, "Published") == true ? row["Published"].ToString().ToLower() : null;
            string primaryebusGroup = DataTableColExists(row, "Primary eBusiness Group") == true ? row["Primary eBusiness Group"].ToString() : null;

            fields.SupressOpenRangeImage = SetBoolean(suppressOpenRangeImg);
            fields.SupressOpenRangeSpec = SetBoolean(suppressOpenRangeSpec);
            fields.Featured = SetBoolean(featuredItem);
            fields.BestSeller = SetBoolean(bestSeller);
            fields.Published = SetBoolean(published);
            fields.PrimaryEbusinessGroup = primaryebusGroup;

            string secondaryebusGroups = DataTableColExists(row, "Secondary eBusiness Groups") == true ? row["Secondary eBusiness Groups"].ToString() : null;

            if (secondaryebusGroups == null) // not in spreadsheet at all
                return;

            fields.SecondaryEbusinessGroups = new List<string>();

            if (!string.IsNullOrEmpty(secondaryebusGroups))
            {
                var codesStrArray = secondaryebusGroups.Split(',');
                for (int i = 0; i < codesStrArray.Length; i++)
                {
                    fields.SecondaryEbusinessGroups.Add(codesStrArray[i].Trim(' '));
                }
            }
        }

        internal void ExtractReSaleable(DataRow row, ProductFields fields)
        {
            string reSaleable = DataTableColExists(row, "Re-saleable") == true ? row["Re-saleable"].ToString().ToLower() : null;
            fields.ReSaleable = SetBoolean(reSaleable);
        }

        internal void ExtractNotes(DataRow row, ProductFields fields)
        {
            fields.StockNoteDesc = DataTableColExists(row, "Stock Notes") == true ? row["Stock Notes"].ToString() : null;
            fields.PriorityNote = DataTableColExists(row, "Priority Note") == true ? row["Priority Note"].ToString() : null;
        }

        internal void ExtractFeedInfo(DataRow row, ProductFields fields)
        {
            fields.GoogleFeedCategory = DataTableColExists(row, "Google Feed Category") == true ? row["Google Feed Category"].ToString().ToLower() : null;
            fields.GoogleFeedAvailability = DataTableColExists(row, "Google Feed Availability") == true ? row["Google Feed Availability"].ToString().ToLower() : null;
            fields.GoogleFeedCondition = DataTableColExists(row, "Google Feed Condition") == true ? row["Google Feed Condition"].ToString().ToLower() : null;
            fields.BespokeFeedAvailability = DataTableColExists(row, "Bespoke Feed Availability") == true ? row["Bespoke Feed Availability"].ToString().ToLower() : null;
            fields.BespokeFeedCondition = DataTableColExists(row, "Bespoke Feed Condition") == true ? row["Bespoke Feed Condition"].ToString().ToLower() : null;
            fields.GoogleFeedSite = DataTableColExists(row, "Google Feed Site") == true ? row["Google Feed Site"].ToString().ToLower() : null;
            fields.BespokeFeedSite = DataTableColExists(row, "Bespoke Feed Site") == true ? row["Bespoke Feed Site"].ToString().ToLower() : null;

            string gfInclude = DataTableColExists(row, "Google Feed Include") == true ? row["Google Feed Include"].ToString().ToLower() : null;
            string bsfInclude = DataTableColExists(row, "Bespoke Feed Include") == true ? row["Bespoke Feed Include"].ToString().ToLower() : null;
            string bsfCustomCost = DataTableColExists(row, "Bespoke Feed Custom Shipping Cost") == true ? row["Bespoke Feed Custom Shipping Cost"].ToString().ToLower() : null;

            fields.GoogleFeedInclude = SetBoolean(gfInclude);
            fields.BespokeFeedInclude = SetBoolean(bsfInclude);
            fields.BespokeFeedUseCustomShipCost = SetBoolean(bsfCustomCost);
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
                    default:
                        returnValue = null;
                        break;
                }
            }

            return returnValue;
        }

        private bool CheckProductIsNew(ProductFields fields)
        {
            bool productIsNew = true;

            using (ngmdEntities db = new ngmdEntities())
            {
                product prd = null;

                if (importPrimaryKey == 1)
                {
                    prd = db.product.Where(x => x.partNo == fields.PartNo &&
                                    x.manufacturerFK == fields.ManufacturerFK).FirstOrDefault();
                }
                else if (importPrimaryKey == 2)
                {
                    prd = db.product.Where(x => x.AxisFields != null && x.AxisFields.stockReference == fields.StockRef &&
                                    x.manufacturerFK == fields.ManufacturerFK).FirstOrDefault();
                }

                if (prd != null)
                    productIsNew = false;
            }

            return productIsNew;
        }

        private AxisValueLookup LookupAxisManufacturer(int attrValID)
        {
            AxisValueLookup avl;

            using (ngmdEntities db = new ngmdEntities())
            {
                avl = db.AxisValueLookup.Where(x => x.axisTypeNameFK == 1 &&
                                    x.attrNameFK == 10 && x.attrValueID == attrValID).FirstOrDefault();
            }
            return avl;
        }

        private manufacturer GetManufacturerFK(AxisValueLookup avl)
        {
            manufacturer manu;

            using (ngmdEntities db = new ngmdEntities())
            {
                manu = db.manufacturer.Where(x => x.manufacturerName.ToLower() == avl.attrValueDesc.ToLower())
                                        .FirstOrDefault();
            }
            return manu;
        }

        private int LookupAxisAttributeValueID(string attrName, int attrID)
        {
            int returnValue = 0;

            using (ngmdEntities db = new ngmdEntities())
            {
                AxisValueLookup avl = db.AxisValueLookup.Where(x => x.axisTypeNameFK == 1 && x.attrNameFK == attrID &&
                                        x.attrValueDesc.Trim().ToLower() == attrName.Trim().ToLower())
                                        .FirstOrDefault();

                if (avl != null)
                    returnValue = avl.attrValueID;
            }

            return returnValue;
        }

        private bool ValidateAxisAttributeId(int attrValId, int attrNameFK)
        {
            bool returnValue = false;

            using (ngmdEntities db = new ngmdEntities())
            {
                AxisValueLookup avl = db.AxisValueLookup.Where(x => x.axisTypeNameFK == 1 && x.attrNameFK == attrNameFK
                                        && x.attrValueID == attrValId).FirstOrDefault();

                if (avl != null)
                    returnValue = true;
            }

            return returnValue;
        }

        internal SkuMappingFields ExtractSkuMapping(DataRow row, int currentRow)
        {
            SkuMappingFields fields = new SkuMappingFields();

            fields.ProviderPartNo = DataTableColExists(row, "Provider Part No") == true ?
                                        row["Provider Part No"].ToString() : null;

            fields.AltRef = DataTableColExists(row, "Alt Ref") == true ?
                                        row["Alt Ref"].ToString() : null;

            string providerFK = DataTableColExists(row, "Provider ID") == true ?
                                        row["Provider ID"].ToString() : null;

            string axisSupplierID = DataTableColExists(row, "Axis Supplier ID") == true ?
                                        row["Axis Supplier ID"].ToString() : null;

            int iProviderFK = 0;
            bool successProviderFK = int.TryParse(providerFK, out iProviderFK);

            if (successProviderFK)
            {
                fields.ProviderFK = iProviderFK;
            }
            else
            {
                fields.ProviderFK = null;
            }

            int iAxisSupplierID = 0;
            bool successAxisSupplierID = int.TryParse(axisSupplierID, out iAxisSupplierID);

            if (successAxisSupplierID)
            {
                fields.AxisSupplierNo = iAxisSupplierID;
            }
            else
            {
                fields.AxisSupplierNo = null;
            }

            return fields;
        }

        internal eBusinessMappingFields ExtractEbusinessFields(DataRow row, int currentRow)
        {
            eBusinessMappingFields fields = new eBusinessMappingFields();

            string partNo = DataTableColExists(row, "Alt Ref") == true ? row["Alt Ref"].ToString() : null;
            string eBus = DataTableColExists(row, "eBus") == true ? row["eBus"].ToString() : null;
            string isPrimary = DataTableColExists(row, "eBusIsPrimary") == true ? row["eBusIsPrimary"].ToString() : null;

            if (partNo != null)
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    product prod = db.product.Where(x => x.partNo == partNo).FirstOrDefault();

                    if (prod != null)
                    {
                        fields.productFK = prod.productID;
                    }
                    else
                    {
                        throw new ApplicationException("Alt Ref not matched to a PMS product.");
                    }
                }
            }

            if (eBus != null)
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    AxisEbusiness map = db.AxisEbusiness.Where(x => x.eBusinessRef == eBus || 
                        x.description == eBus).FirstOrDefault();

                    if (map != null)
                    {
                        fields.eBusinessRef = map.eBusinessRef;
                    }
                    else
                    {
                        throw new ApplicationException("Ebusiness group not matched to a PMS eBusiness record.");
                    }
                }
            }

            if (isPrimary != null)
            {
                fields.isPrimary = SetBoolean(isPrimary);
            }

            return fields;
        }

        public static bool DataTableColExists(DataRow dr, string colName)
        {
            return dr.Table.Columns.Contains(colName);
        }

        internal void ExtractGooglePromotionId(DataRow row, ProductFields fields)
        {
            string googlePromotionId = DataTableColExists(row, "Google Promotion IDs") == true ? row["Google Promotion IDs"].ToString() : null;
            if (googlePromotionId != null)
            {
                fields.GooglePromotionId = googlePromotionId;
            }
        }

        internal void ExtractBreakQuantities(DataRow row, ProductFields fields)
        {
            string breakQuantity1 = DataTableColExists(row, "Break Quantity 1") == true ? row["Break Quantity 1"].ToString() : null;
            if (breakQuantity1 != null)
            {
                var iBreakQuantity1 = 0;
                var success = int.TryParse(breakQuantity1, out iBreakQuantity1);

                if (success)
                {
                    fields.BreakQuantity1 = iBreakQuantity1;
                }
            }

            string breakQuantity2 = DataTableColExists(row, "Break Quantity 2") == true ? row["Break Quantity 2"].ToString() : null;
            if (breakQuantity2 != null)
            {
                var iBreakQuantity2 = 0;
                var success = int.TryParse(breakQuantity2, out iBreakQuantity2);

                if (success)
                {
                    fields.BreakQuantity2 = iBreakQuantity2;
                }
            }

            string breakQuantity3 = DataTableColExists(row, "Break Quantity 3") == true ? row["Break Quantity 3"].ToString() : null;
            if (breakQuantity3 != null)
            {
                var iBreakQuantity3 = 0;
                var success = int.TryParse(breakQuantity3, out iBreakQuantity3);

                if (success)
                {
                    fields.BreakQuantity3 = iBreakQuantity3;
                }
            }
        }
    }
}
