using netGiant.Intranet.DataLayer;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Import.Product
{
    public class ExtractData
    {
        public ExtractData(string axisLang, int _websiteFK)
        {
            //stnFunc = func;
            axisLanguage = axisLang;
            websiteFK = _websiteFK;
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

        internal ImportFields ExtractKeyData(DataRow row, int currentRow)
        {
            //stnFunc.AddToActivityLog("Extracting Key Data for row " + currentRow);

            ImportFields fields = new ImportFields();

            fields.partNo = DataTableColExists(row, "Alt Ref") == true ? row["Alt Ref"].ToString() : null;
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
            else
            {
                if (DataTableColExists(row, "Attribute 10"))
                {
                    string attrVal = DataTableColExists(row, "Attribute 10") == true ? row["Attribute 10"].ToString() : null;
                    int attrValID = 0;

                    bool success = int.TryParse(attrVal, out attrValID);

                    if (success)
                    {
                        fields.attr10 = attrValID;
                        AxisValueLookup avl = LookupAxisManufacturer(attrValID);
                        manufacturer manu = GetManufacturerFK(avl);
                        if (manu != null)
                        {
                            fields.manufacturerFK = manu.manufacturerID;
                        }
                    }
                }
            }

            if (fields.partNo == null)
            {
                throw new ApplicationException("No Alt Ref was found for this row.");
            }

            if (fields.manufacturerFK == null)
            {
                throw new ApplicationException("No Manufacturer was found for this row, or it was not matched in the PMS.");
            }

            fields.websiteFK = websiteFK;
            fields.IsNew = CheckProductIsNew(fields);

            return fields;
        }

        internal void ExtractUnspsc(DataRow row, ImportFields fields)
        {
            fields.unspscCode = DataTableColExists(row, "UNSPSC") == true ? row["UNSPSC"].ToString() : null;
        }

        internal void ExtractPageYield(DataRow row, ImportFields fields)
        {
            string pageYield = DataTableColExists(row, "Page Yield") == true ? row["Page Yield"].ToString() : null;

            if (pageYield != null)
            {
                int iPageYield = 0;
                bool success = int.TryParse(pageYield, out iPageYield);

                if (success)
                {
                    fields.pageYield = iPageYield;
                }
                else
                {
                    fields.pageYield = null;
                }
            }
        }

        internal void ExtractDiscontinued(DataRow row, ImportFields fields)
        {
            string discontinued = DataTableColExists(row, "Discontinued Item") == true ?
                                        row["Discontinued Item"].ToString().ToLower() : null;
            fields.discontinuedItem = SetBoolean(discontinued);
        }

        internal void ExtractProductName(DataRow row, ImportFields fields)
        {
            fields.productName = DataTableColExists(row, "Product Name") == true ? row["Product Name"].ToString() : null;

            //if (fields.productName == null)
            //{
            //    string description1 = ColExists(row, "Description1") == true ? row["Description1"].ToString() : null;
            //    string description2 = ColExists(row, "Description2") == true ? row["Description2"].ToString() : null;
            //    fields.productName = description1 + " " + description2;
            //}
        }

        internal void ExtractCategoryCode(DataRow row, ImportFields fields)
        {
            string categoryCodeName = DataTableColExists(row, "Category Code") == true ? row["Category Code"].ToString() : null;

            if (categoryCodeName != null)
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    categoryCode cc = db.categoryCode.Where(x => x.websiteFK == websiteFK &&
                                                    x.categoryCodeName == categoryCodeName)
                                                    .FirstOrDefault();

                    if (cc != null)
                    {
                        fields.CategoryCodeFK = cc.categoryCodeID;
                    }
                    else
                    {
                        throw new ApplicationException("Category Code could not be found for specified website");
                    }
                }
            }
        }

        internal void ExtractStockReference(DataRow row, ImportFields fields)
        {
            fields.stockRef = DataTableColExists(row, "Stock Reference") == true ? row["Stock Reference"].ToString() : null;
        }

        internal void ExtractStockRecordType(DataRow row, ImportFields fields)
        {
            fields.stockRecordType = DataTableColExists(row, "Stock Record Type") == true ? row["Stock Record Type"].ToString() : null;   
        }

        internal void ExtractSpecification(DataRow row, ImportFields fields)
        {
            string spec1 = DataTableColExists(row, "Specification 1") == true ? row["Specification 1"].ToString() : null;
            string spec2 = DataTableColExists(row, "Specification 2") == true ? row["Specification 2"].ToString() : null;
            string spec3 = DataTableColExists(row, "Specification 3") == true ? row["Specification 3"].ToString() : null;
            string spec4 = DataTableColExists(row, "Specification 4") == true ? row["Specification 4"].ToString() : null;
            string spec5 = DataTableColExists(row, "Specification 5") == true ? row["Specification 5"].ToString() : null;
            string spec6 = DataTableColExists(row, "Specification 6") == true ? row["Specification 6"].ToString() : null;

            fields.spec1 = spec1;
            fields.spec2 = spec2;
            fields.spec3 = spec3;
            fields.spec4 = spec4;
            fields.spec5 = spec5;
            fields.spec6 = spec6;
        }

        internal void ExtractDataSupplier(DataRow row, ImportFields fields)
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
                        fields.dataSupplierFK = ds.dataSupplierID;
                    }
                    else
                    {
                        throw new ApplicationException("Data Supplier '" + dataSupplier + "' not matched in the PMS");
                    }
                }
            }
        }

        internal void ExtractSalesAreaGroup(DataRow row, ImportFields fields)
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
                        fields.salesAreaGroupFK = salAreaGrp.salesAreaGroupID;
                    }
                    else
                    {
                        throw new ApplicationException("Sales Area Group '" + salesAreaGroup + "' not matched in the PMS");
                    }
                }
            }
        }

        internal void ExtractProductGroup(DataRow row, ImportFields fields)
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
                        fields.productGroupFK = prdGrp.productGroupID;
                    }
                    else
                    {
                        throw new ApplicationException("Product Group '" + group + "' not matched in the PMS");
                    }
                }
            }
        }

        internal void ExtractProductStatus(DataRow row, ImportFields fields)
        {
            string status = DataTableColExists(row, "Product Status") == true ? row["Product Status"].ToString() : null;

            if (status != null)
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    productStatus ps = db.productStatus.Where(x => x.productStatusName.ToLower() == status.ToLower())
                                                        .FirstOrDefault();
                    if (ps != null)
                    {
                        fields.productStatusFK = ps.productStatusID;
                    }
                    else
                    {
                        throw new ApplicationException("Product Status '" + status + "' not matched in the PMS");
                    }
                }
            }
        }

        internal void ExtractDefaultDelivery(DataRow row, ImportFields fields)
        {
            string daysString = DataTableColExists(row, "Default delivery days") == true ? row["Default delivery days"].ToString() : null;
            int defDelDays = 0;
            bool success = int.TryParse(daysString, out defDelDays);

            if (success)
            {
                fields.defaultDeliveryToCust = defDelDays;
            }
            else
            {
                fields.defaultDeliveryToCust = null;
            }
        }

        internal void ExtractAttributes(DataRow row, ImportFields fields)
        {
            for (var i = 1; i < 11; i++)
            {
                switch (i)
                {
                    case 1:
                        fields.attr1 = ExtractSingleAttribute(row, i);
                        break;
                    case 2:
                        fields.attr2 = ExtractSingleAttribute(row, i);
                        break;
                    case 3:
                        fields.attr3 = ExtractSingleAttribute(row, i);
                        break;
                    case 4:
                        fields.attr4 = ExtractSingleAttribute(row, i);
                        break;
                    case 5:
                        fields.attr5 = ExtractSingleAttribute(row, i);
                        break;
                    case 6:
                        fields.attr6 = ExtractSingleAttribute(row, i);
                        break;
                    case 7:
                        fields.attr7 = ExtractSingleAttribute(row, i);
                        break;
                    case 8:
                        fields.attr8 = ExtractSingleAttribute(row, i);
                        break;
                    case 9:
                        fields.attr9 = ExtractSingleAttribute(row, i);
                        break;
                    case 10:
                        if (fields.attr10 != 0)
                        {
                            fields.attr10 = ExtractSingleAttribute(row, i);
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

        internal void ExtractMetaData(DataRow row, ImportFields fields)
        {
            string mTitle = DataTableColExists(row, axisLanguage + " Meta Title") == true ? row[axisLanguage + " Meta Title"].ToString() : null;
            string mKeywords = DataTableColExists(row, axisLanguage + " Meta Keywords") == true ? row[axisLanguage + " Meta Keywords"].ToString() : null;
            string mDesc = DataTableColExists(row, axisLanguage + " Meta Description") == true ? row[axisLanguage + " Meta Description"].ToString() : null;
            string addInfoUrl = DataTableColExists(row, "Additional Information URL") == true ? row["Additional Information URL"].ToString() : null;

            fields.metaTitle = mTitle;
            fields.metaKeywords = mKeywords;
            fields.metaDesc = mDesc;
            fields.additionalInfoUrl = addInfoUrl;
        }

        internal void ExtractEbusinessDetails(DataRow row, ImportFields fields)
        {
            string suppressOpenRangeImg = DataTableColExists(row, "Suppress open range image") == true ? row["Suppress open range image"].ToString().ToLower() : null;
            string suppressOpenRangeSpec = DataTableColExists(row, "Suppress open range spec") == true ? row["Suppress open range spec"].ToString().ToLower() : null;
            string featuredItem = DataTableColExists(row, "Featured item") == true ? row["Featured item"].ToString().ToLower() : null;
            string bestSeller = DataTableColExists(row, "Best seller") == true ? row["Best seller"].ToString().ToLower() : null;
            string published = DataTableColExists(row, "Published") == true ? row["Published"].ToString().ToLower() : null;

            fields.supressOpenRangeImage = SetBoolean(suppressOpenRangeImg);
            fields.supressOpenRangeSpec = SetBoolean(suppressOpenRangeSpec);
            fields.featured = SetBoolean(featuredItem);
            fields.bestSeller = SetBoolean(bestSeller);
            fields.published = SetBoolean(published);
        }

        internal void ExtractReSaleable(DataRow row, ImportFields fields)
        {
            string reSaleable = DataTableColExists(row, "Re-saleable") == true ? row["Re-saleable"].ToString().ToLower() : null;
            fields.reSaleable = SetBoolean(reSaleable);
        }

        internal void ExtractStockNotes(DataRow row, ImportFields fields)
        {
            fields.stockNoteDesc = DataTableColExists(row, "Stock Notes") == true ? row["Stock Notes"].ToString() : null;
        }

        internal void ExtractFeedInfo(DataRow row, ImportFields fields)
        {
            fields.googleFeedCategory = DataTableColExists(row, "Google Feed Category") == true ? row["Google Feed Category"].ToString().ToLower() : null;
            fields.googleFeedAvailability = DataTableColExists(row, "Google Feed Availability") == true ? row["Google Feed Availability"].ToString().ToLower() : null;
            fields.googleFeedCondition = DataTableColExists(row, "Google Feed Condition") == true ? row["Google Feed Condition"].ToString().ToLower() : null;
            fields.bespokeFeedAvailability = DataTableColExists(row, "Bespoke Feed Availability") == true ? row["Bespoke Feed Availability"].ToString().ToLower() : null;
            fields.bespokeFeedCondition = DataTableColExists(row, "Bespoke Feed Condition") == true ? row["Bespoke Feed Condition"].ToString().ToLower() : null;
            fields.googleFeedSite = DataTableColExists(row, "Google Feed Site") == true ? row["Google Feed Site"].ToString().ToLower() : null;
            fields.bespokeFeedSite = DataTableColExists(row, "Bespoke Feed Site") == true ? row["Bespoke Feed Site"].ToString().ToLower() : null;

            string gfInclude = DataTableColExists(row, "Google Feed Include") == true ? row["Google Feed Include"].ToString().ToLower() : null;
            string bsfInclude = DataTableColExists(row, "Bespoke Feed Include") == true ? row["Bespoke Feed Include"].ToString().ToLower() : null;
            string bsfCustomCost = DataTableColExists(row, "Bespoke Feed Custom Shipping Cost") == true ? row["Bespoke Feed Custom Shipping Cost"].ToString().ToLower() : null;

            fields.googleFeedInclude = SetBoolean(gfInclude);
            fields.bespokeFeedInclude = SetBoolean(bsfInclude);
            fields.bespokeFeedUseCustomShipCost = SetBoolean(bsfCustomCost);
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

        private bool CheckProductIsNew(ImportFields fields)
        {
            bool productIsNew = true;

            using (ngmdEntities db = new ngmdEntities())
            {
                product prd = db.product.Where(x => x.partNo == fields.partNo &&
                                x.manufacturerFK == fields.manufacturerFK).FirstOrDefault();
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
            //stnFunc.AddToActivityLog("Extracting SkuMapping for row " + currentRow);
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

        internal void ExtractGooglePromotionId(DataRow row, ImportFields fields)
        {
            string googlePromotionId = DataTableColExists(row, "Google Promotion IDs") == true ? row["Google Promotion IDs"].ToString() : null;
            if (googlePromotionId != null)
            {
                fields.googlePromotionId = googlePromotionId;
            }
        }

        internal void ExtractBreakQuantities(DataRow row, ImportFields fields)
        {
            string breakQuantity1 = DataTableColExists(row, "Break Quantity 1") == true ? row["Break Quantity 1"].ToString() : null;
            if (breakQuantity1 != null)
            {
                var iBreakQuantity1 = 0;
                var success = int.TryParse(breakQuantity1, out iBreakQuantity1);

                if (success)
                {
                    fields.breakQuantity1 = iBreakQuantity1;
                }
            }

            string breakQuantity2 = DataTableColExists(row, "Break Quantity 2") == true ? row["Break Quantity 2"].ToString() : null;
            if (breakQuantity2 != null)
            {
                var iBreakQuantity2 = 0;
                var success = int.TryParse(breakQuantity2, out iBreakQuantity2);

                if (success)
                {
                    fields.breakQuantity2 = iBreakQuantity2;
                }
            }

            string breakQuantity3 = DataTableColExists(row, "Break Quantity 3") == true ? row["Break Quantity 3"].ToString() : null;
            if (breakQuantity3 != null)
            {
                var iBreakQuantity3 = 0;
                var success = int.TryParse(breakQuantity3, out iBreakQuantity3);

                if (success)
                {
                    fields.breakQuantity3 = iBreakQuantity3;
                }
            }
        }
    }
}
