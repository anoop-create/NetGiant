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
using System.Reflection;
using System.Data;
using System.Diagnostics;
using System.IO;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Export
{
    public class ExportProductViewModel
    {
        public ExportProductViewModel()
        {
            Initialize();
        }

        private void Initialize()
        {
            AllProductStatuses = SelectListViewModel.AllProductStatuses();
            AllWebsites = SelectListViewModel.AllWebsites();
            AllProductGroups = SelectListViewModel.AllProductGroups();
            AllSalesAreaGroups = SelectListViewModel.AllSalesAreaGroups();
            AllDataSuppliers = SelectListViewModel.AllDataSuppliers();
            AllManufacturers = SelectListViewModel.AllManufacturers();
            ExportProductFieldDictionary = new Dictionary<string, string>();
            ExportAxisFieldsFieldDictionary = new Dictionary<string, string>();
            axisAttributeLookup = new List<AxisAttribute>();
        }

        public IQueryable<SelectListItem> AllProductStatuses { get; set; }
        public IQueryable<SelectListItem> AllWebsites { get; set; }
        public IQueryable<SelectListItem> AllProductGroups { get; set; }
        public IQueryable<SelectListItem> AllSalesAreaGroups { get; set; }
        public IQueryable<SelectListItem> AllDataSuppliers { get; set; }
        public List<SelectListItem> AllManufacturers { get; set; }
        public int SelectedProductStatusFK { get; set; }
        public int SelectedWebsiteFK { get; set; }
        public int SelectedProductGroupFK { get; set; }
        public int SelectedSalesAreaGroupFK { get; set; }
        public int SelectedDataSupplierFK { get; set; }
        public int SelectedCategoryCodeFK { get; set; }
        public int SelectedManufacturerFK { get; set; }
        public string SearchTerm { get; set; }
        public string SearchBy { get; set; }
        public Dictionary<string, string> ExportProductFieldDictionary { get; set; }
        public Dictionary<string, string> ExportAxisFieldsFieldDictionary { get; set; }
        private List<AxisAttribute> axisAttributeLookup { get; set; }
        public int ProductCount { get; set; }
        public string FilePath { get; set; }
        public string LocalDirectory { get; set; }
        public string[] PostedProductFields { get; set; }
        public string[] PostedAxisFields { get; set; }
        public bool ExProductGroupById { get; set; }
        public bool ExSalesAreaGroupById { get; set; }
        public bool ExAttributesById { get; set; }

        public void GetExportableFields()
        {
            GetProductExportFieldLookpDictionary();
            GetAxisFieldsExportFieldLookpDictionary();
        }

        public ExportProductViewModel Export()
        {
            GetExportableFields();
            List<product> productList;
            productList = GetProducts();
            GetAxisAttributeLookup();
            CreateCSVFile(productList);

            return this;
        }

        private void CreateCSVFile(List<product> productList)
        {
            FilePath = LocalDirectory + "\\PMSTempData\\ProductExport_" + DateTime.Now.ToString("dd_MM_yyyy_H_mm_ss") + ".csv";

            using (CsvFileWriter writer = new CsvFileWriter(FilePath, ','))
            {
                SetColumnHeadings(writer);

                foreach (product prod in productList)
                {
                    InsertCSVData(writer, prod);
                }
            }
        }

        private void GetProductExportFieldLookpDictionary()
        {
            List<exportFieldLookup> list = new List<exportFieldLookup>();

            using (ngmdEntities db = new ngmdEntities())
            {
                list = db.exportFieldLookup.Where(x => x.tableName == "product").ToList();
            }

            foreach (exportFieldLookup field in list)
            {
                if (field.fieldName == "UNSPSCCode")
                    continue;

                ExportProductFieldDictionary.Add(field.fieldName, field.friendlyFieldName);
            }
        }

        private void GetAxisFieldsExportFieldLookpDictionary()
        {
            List<exportFieldLookup> list = new List<exportFieldLookup>();

            using (ngmdEntities db = new ngmdEntities())
            {
                if (SelectedWebsiteFK == 0)
                {
                    list = db.exportFieldLookup.Where(x => x.tableName == "axisFields" &&
                                        (x.websiteFK == null || x.websiteFK == 1))
                                            .ToList();
                }
                else
                {
                    list = db.exportFieldLookup.Where(x => x.tableName == "axisFields" &&
                                        (x.websiteFK == null || x.websiteFK == SelectedWebsiteFK))
                                            .ToList();
                }
            }

            foreach (exportFieldLookup field in list)
            {
                ExportAxisFieldsFieldDictionary.Add(field.fieldName, field.friendlyFieldName);
            }
        }

        private void GetAxisAttributeLookup()
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                List<AxisValueLookup> avlList = db.AxisValueLookup.Where(x => x.axisTypeNameFK == 1).ToList();

                foreach (AxisValueLookup lookup in avlList)
                {
                    AxisAttribute a = new AxisAttribute();
                    a.AttributeNameFK = lookup.attrNameFK;
                    a.AttributeValueID = lookup.attrValueID;
                    a.AttributeValueDesc = lookup.attrValueDesc;
                    axisAttributeLookup.Add(a);
                }
            }
        }

        private void InsertCSVData(CsvFileWriter writer, product prod)
        {
            CsvRow newRow = new CsvRow();
            InsertProductCSVData(prod, newRow);
            InsertAxisFieldsCSVData(prod, newRow);
            writer.WriteRow(newRow);
        }

        private void InsertAxisFieldsCSVData(product prod, CsvRow newRow)
        {
            if (PostedAxisFields != null)
            {

                AxisFields af = prod.AxisFields;

                AddCsvData(newRow, "stockReference", af != null ? af.stockReference.ToSafeString() : "", ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvData(newRow, "spec1", af != null ? af.spec1.ToSafeString() : "", ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvData(newRow, "spec2", af != null ? af.spec2.ToSafeString() : "", ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvData(newRow, "spec3", af != null ? af.spec3.ToSafeString() : "", ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvData(newRow, "spec4", af != null ? af.spec4.ToSafeString() : "", ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvData(newRow, "spec5", af != null ? af.spec5.ToSafeString() : "", ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvData(newRow, "spec6", af != null ? af.spec6.ToSafeString() : "", ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvData(newRow, "reSaleable", af != null ? af.reSaleable.ToSafeString() : "", ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvData(newRow, "stockRecordType", af != null ? af.stockRecordType.ToSafeString() : "", ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvData(newRow, "discontinuedItem", af != null ? af.discontinuedItem.ToSafeString() : "", ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvData(newRow, "defaultDeliveryToCust", af != null ? af.defaultDeliveryToCust.ToSafeString() : "", ExportAxisFieldsFieldDictionary, PostedAxisFields);

                if (ExportAxisFieldsFieldDictionary.ContainsKey("attr1") && PostedAxisFields.Contains("attr1"))
                {
                    if (af != null && af.attr1 != null)
                    {
                        newRow.Add(ExAttributesById == false ? axisAttributeLookup.Where(x => x.AttributeNameFK == 1 &&
                                x.AttributeValueID == af.attr1).FirstOrDefault().AttributeValueDesc : af.attr1.ToString());
                    }
                    else
                    {
                        AddBlankCell(newRow);
                    }
                }
                if (ExportAxisFieldsFieldDictionary.ContainsKey("attr2") && PostedAxisFields.Contains("attr2"))
                {
                    if (af != null && af.attr2 != null)
                    {
                        newRow.Add(ExAttributesById == false ? axisAttributeLookup.Where(x => x.AttributeNameFK == 2 &&
                                x.AttributeValueID == af.attr2).FirstOrDefault().AttributeValueDesc : af.attr2.ToString());
                    }
                    else
                    {
                        AddBlankCell(newRow);
                    }
                }
                if (ExportAxisFieldsFieldDictionary.ContainsKey("attr3") && PostedAxisFields.Contains("attr3"))
                {
                    if (af != null && af.attr3 != null)
                    {
                        newRow.Add(ExAttributesById == false ? axisAttributeLookup.Where(x => x.AttributeNameFK == 3 &&
                                x.AttributeValueID == af.attr3).FirstOrDefault().AttributeValueDesc : af.attr3.ToString());
                    }
                    else
                    {
                        AddBlankCell(newRow);
                    }
                }
                if (ExportAxisFieldsFieldDictionary.ContainsKey("attr4") && PostedAxisFields.Contains("attr4"))
                {
                    if (af != null && af.attr4 != null)
                    {
                        newRow.Add(ExAttributesById == false ? axisAttributeLookup.Where(x => x.AttributeNameFK == 4 &&
                                x.AttributeValueID == af.attr4).FirstOrDefault().AttributeValueDesc : af.attr4.ToString());
                    }
                    else
                    {
                        AddBlankCell(newRow);
                    }
                }
                if (ExportAxisFieldsFieldDictionary.ContainsKey("attr5") && PostedAxisFields.Contains("attr5"))
                {
                    if (af != null && af.attr5 != null)
                    {
                        newRow.Add(ExAttributesById == false ? axisAttributeLookup.Where(x => x.AttributeNameFK == 5 &&
                                x.AttributeValueID == af.attr5).FirstOrDefault().AttributeValueDesc : af.attr5.ToString());
                    }
                    else
                    {
                        AddBlankCell(newRow);
                    }
                }
                if (ExportAxisFieldsFieldDictionary.ContainsKey("attr6") && PostedAxisFields.Contains("attr6"))
                {
                    if (af != null && af.attr6 != null)
                    {
                        newRow.Add(ExAttributesById == false ? axisAttributeLookup.Where(x => x.AttributeNameFK == 6 &&
                                x.AttributeValueID == af.attr6).FirstOrDefault().AttributeValueDesc : af.attr6.ToString());
                    }
                    else
                    {
                        AddBlankCell(newRow);
                    }
                }
                if (ExportAxisFieldsFieldDictionary.ContainsKey("attr7") && PostedAxisFields.Contains("attr7"))
                {
                    if (af != null && af.attr7 != null)
                    {
                        newRow.Add(ExAttributesById == false ? axisAttributeLookup.Where(x => x.AttributeNameFK == 7 &&
                                x.AttributeValueID == af.attr7).FirstOrDefault().AttributeValueDesc : af.attr7.ToString());
                    }
                    else
                    {
                        AddBlankCell(newRow);
                    }
                }
                if (ExportAxisFieldsFieldDictionary.ContainsKey("attr8") && PostedAxisFields.Contains("attr8"))
                {
                    if (af != null && af.attr8 != null)
                    {
                        newRow.Add(ExAttributesById == false ? axisAttributeLookup.Where(x => x.AttributeNameFK == 8 &&
                                x.AttributeValueID == af.attr8).FirstOrDefault().AttributeValueDesc : af.attr8.ToString());
                    }
                    else
                    {
                        AddBlankCell(newRow);
                    }
                }
                if (ExportAxisFieldsFieldDictionary.ContainsKey("attr9") && PostedAxisFields.Contains("attr9"))
                {
                    if (af != null && af.attr9 != null)
                    {
                        newRow.Add(ExAttributesById == false ? axisAttributeLookup.Where(x => x.AttributeNameFK == 9 &&
                                x.AttributeValueID == af.attr9).FirstOrDefault().AttributeValueDesc : af.attr9.ToString());
                    }
                    else
                    {
                        AddBlankCell(newRow);
                    }
                } 
                if (ExportAxisFieldsFieldDictionary.ContainsKey("attr10") && PostedAxisFields.Contains("attr10"))
                {
                    if (af != null && af.attr10 != null)
                    {
                        newRow.Add(ExAttributesById == false ? axisAttributeLookup.Where(x => x.AttributeNameFK == 10 &&
                                x.AttributeValueID == af.attr10).FirstOrDefault().AttributeValueDesc : af.attr10.ToString());
                    }
                    else
                    {
                        AddBlankCell(newRow);
                    }
                }


                if (ExportAxisFieldsFieldDictionary.ContainsKey("published") && PostedAxisFields.Contains("published"))
                    newRow.Add(af != null ? af.published == true ? "Y" : "N" : "");
                if (ExportAxisFieldsFieldDictionary.ContainsKey("featured") && PostedAxisFields.Contains("featured"))
                    newRow.Add(af != null ? af.featured == true ? "Y" : "N" : "");
                if (ExportAxisFieldsFieldDictionary.ContainsKey("bestSeller") && PostedAxisFields.Contains("bestSeller"))
                    newRow.Add(af != null ? af.bestSeller == true ? "Y" : "N" : "");
                if (ExportAxisFieldsFieldDictionary.ContainsKey("suppressOpenRangeImage") && PostedAxisFields.Contains("suppressOpenRangeImage"))
                    newRow.Add(af != null ? af.supressOpenRangeImage == true ? "Y" : "N" : "");
                if (ExportAxisFieldsFieldDictionary.ContainsKey("suppressOpenRangeDesc") && PostedAxisFields.Contains("suppressOpenRangeDesc"))
                    newRow.Add(af != null ? af.supressOpenRangeSpec == true ? "Y" : "N" : "");
                if (ExportAxisFieldsFieldDictionary.ContainsKey("additionalInfoUrl") && PostedAxisFields.Contains("additionalInfoUrl"))
                    newRow.Add(af != null ? af.additionalInfoUrl.ToSafeString() : "");
                if (ExportAxisFieldsFieldDictionary.ContainsKey("dateLastUpdate") && PostedAxisFields.Contains("dateLastUpdate"))
                    newRow.Add(af != null ? af.dateLastUpdate.ToSafeString() : "");

                if (af != null)
                {
                    InsertAxisFieldsAdditionalData(af, newRow);
                }

            }
        }

        private void InsertAxisFieldsAdditionalData(AxisFields af, CsvRow newRow)
        {
            AxisFieldsAdditional afa = af.AxisFieldsAdditional.FirstOrDefault(
                x => SelectedWebsiteFK == 0 ? x.websiteFK == 1 : x.websiteFK == SelectedWebsiteFK);

            if (afa != null)
            {
                var stkNoteDesc = "";
                if (afa.stockNoteDesc != null)
                    stkNoteDesc = afa.stockNoteDesc.Replace(Environment.NewLine, "");

                AddCsvData(newRow, "stockNoteDesc", stkNoteDesc, ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvData(newRow, "metaTitle", afa.metaTitle, ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvData(newRow, "metaKeywords", afa.metaKeywords, ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvData(newRow, "metaDesc", afa.metaDesc, ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvData(newRow, "googleFeedSite", afa.googleFeedSite, ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvData(newRow, "googleFeedInclude", afa.googleFeedInclude, ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvData(newRow, "googleFeedCategory", afa.googleFeedCategory, ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvData(newRow, "googleFeedAvailability", afa.googleFeedAvailability, ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvData(newRow, "googleFeedCondition", afa.googleFeedCondition, ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvData(newRow, "bespokeFeedInclude", afa.bespokeFeedInclude, ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvData(newRow, "bespokeFeedSite", afa.bespokeFeedSite, ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvData(newRow, "bespokeFeedUseCustomShipCost", afa.bespokeFeedUseCustomShipCost, ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvData(newRow, "bespokeFeedAvailability", afa.bespokeFeedAvailability, ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvData(newRow, "bespokeFeedCondition", afa.bespokeFeedCondition, ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvData(newRow, "googlePromotionId", af != null ? afa.googlePromotionId.ToSafeString() : "", ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvData(newRow, "breakQuantity1", af != null ? afa.breakQuantity1.ToSafeString() : "", ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvData(newRow, "breakQuantity2", af != null ? afa.breakQuantity2.ToSafeString() : "", ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvData(newRow, "breakQuantity3", af != null ? afa.breakQuantity3.ToSafeString() : "", ExportAxisFieldsFieldDictionary, PostedAxisFields);
            }
            else
            {
                for (int i = 0; i < 14; i++)
                {
                    AddBlankCell(newRow);
                }
            }
        }

        private void AddBlankCell(CsvRow newRow)
        {
            newRow.Add("");
        }

        private void InsertProductCSVData(product prod, CsvRow newRow)
        {
            if (PostedProductFields != null)
            {
                AddCsvData(newRow, "productID", prod.productID, ExportProductFieldDictionary, PostedProductFields);
                AddCsvData(newRow, "productName", prod.productName, ExportProductFieldDictionary, PostedProductFields);
                AddCsvData(newRow, "partNo", prod.partNo, ExportProductFieldDictionary, PostedProductFields);
                AddCsvData(newRow, "UNSPSCCode", prod.UNSPSCCode, ExportProductFieldDictionary, PostedProductFields);

                AddCsvData(newRow, "manufacturerFK", prod.manufacturer != null ?
                                        prod.manufacturer.manufacturerName : "", ExportProductFieldDictionary, PostedProductFields);

                if (ExportProductFieldDictionary.ContainsKey("categoryCodeFK") &&
                                                PostedProductFields.Contains("categoryCodeFK"))
                {
                    if (SelectedWebsiteFK > 0)
                    {
                        websiteInventory wi = prod.websiteInventory.Where(x => x.websiteFK == SelectedWebsiteFK).FirstOrDefault();
                        if (wi != null)
                        {
                            if (wi.categoryCode != null)
                            {
                                AddCsvData(newRow, "categoryCodeFK", wi.categoryCode.categoryCodeName, ExportProductFieldDictionary, PostedProductFields);
                            }
                            else
                            {
                                AddBlankCell(newRow);
                            }
                        }
                        else
                        {
                            AddBlankCell(newRow);
                        }
                    }
                    else
                    {
                        AddBlankCell(newRow);
                    }
                }

                AddCsvData(newRow, "productStatusFK", prod.productStatus != null ?
                                        prod.productStatus.productStatusName : "", ExportProductFieldDictionary, PostedProductFields);

                AddCsvData(newRow, "productGroupFK", prod.productGroup != null ?
                    ExProductGroupById == false ? prod.productGroup.productGroupName : prod.productGroup.productGroupNo : "",
                    ExportProductFieldDictionary, PostedProductFields);

                AddCsvData(newRow, "salesAreaGroupFK", prod.salesAreaGroup != null ?
                    ExSalesAreaGroupById == false ? prod.salesAreaGroup.salesAreaGroupName : prod.salesAreaGroup.salesAreaGroupNo : "",
                    ExportProductFieldDictionary, PostedProductFields);

                AddCsvData(newRow, "dateLastUpdate", prod.dateLastUpdate, ExportProductFieldDictionary, PostedProductFields);

                AddCsvData(newRow, "dataSupplierFK", prod.dataSupplier != null ?
                                        prod.dataSupplier.dataSupplierName : "", ExportProductFieldDictionary, PostedProductFields);

                AddCsvData(newRow, "supplierStock", prod.supplierStock, ExportProductFieldDictionary, PostedProductFields);

                AddCsvData(newRow, "pageYield", prod.pageYield, ExportProductFieldDictionary, PostedProductFields);

                AddCsvData(newRow, "supplierLastUpdate", prod.supplierLastUpdate, ExportProductFieldDictionary, PostedProductFields);
            }
        }

        private void AddCsvData(CsvRow newRow, string entityName, object entityData,
                                Dictionary<string, string> dict, string[] postedFields)
        {
            if (dict.ContainsKey(entityName) && postedFields.Contains(entityName))
                newRow.Add(entityData.ToSafeString());
        }

        private void SetColumnHeadings(CsvFileWriter writer)
        {
            CsvRow firstRow = new CsvRow();
            SetProductColumnHeadings(firstRow);
            SetAxisFieldsColumnHeadings(firstRow);
            writer.WriteRow(firstRow);
        }

        private void SetAxisFieldsColumnHeadings(CsvRow firstRow)
        {
            if (PostedAxisFields != null)
            {
                AddCsvColumn(firstRow, "axisFieldsID", ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvColumn(firstRow, "stockReference", ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvColumn(firstRow, "spec1", ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvColumn(firstRow, "spec2", ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvColumn(firstRow, "spec3", ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvColumn(firstRow, "spec4", ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvColumn(firstRow, "spec5", ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvColumn(firstRow, "spec6", ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvColumn(firstRow, "reSaleable", ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvColumn(firstRow, "stockRecordType", ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvColumn(firstRow, "discontinuedItem", ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvColumn(firstRow, "defaultDeliveryToCust", ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvColumn(firstRow, "attr1", ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvColumn(firstRow, "attr2", ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvColumn(firstRow, "attr3", ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvColumn(firstRow, "attr4", ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvColumn(firstRow, "attr5", ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvColumn(firstRow, "attr6", ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvColumn(firstRow, "attr7", ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvColumn(firstRow, "attr8", ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvColumn(firstRow, "attr9", ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvColumn(firstRow, "attr10", ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvColumn(firstRow, "published", ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvColumn(firstRow, "featured", ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvColumn(firstRow, "bestSeller", ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvColumn(firstRow, "suppressOpenRangeImage", ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvColumn(firstRow, "suppressOpenRangeDesc", ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvColumn(firstRow, "additionalInfoUrl", ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvColumn(firstRow, "dateLastUpdate", ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvColumn(firstRow, "websiteFK", ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvColumn(firstRow, "stockNoteDesc", ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvColumn(firstRow, "metaTitle", ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvColumn(firstRow, "metaKeywords", ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvColumn(firstRow, "metaDesc", ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvColumn(firstRow, "googleFeedSite", ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvColumn(firstRow, "googleFeedInclude", ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvColumn(firstRow, "googleFeedCategory", ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvColumn(firstRow, "googleFeedAvailability", ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvColumn(firstRow, "googleFeedCondition", ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvColumn(firstRow, "bespokeFeedInclude", ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvColumn(firstRow, "bespokeFeedSite", ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvColumn(firstRow, "bespokeFeedUseCustomShipCost", ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvColumn(firstRow, "bespokeFeedAvailability", ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvColumn(firstRow, "bespokeFeedCondition", ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvColumn(firstRow, "googlePromotionId", ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvColumn(firstRow, "breakQuantity1", ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvColumn(firstRow, "breakQuantity2", ExportAxisFieldsFieldDictionary, PostedAxisFields);
                AddCsvColumn(firstRow, "breakQuantity3", ExportAxisFieldsFieldDictionary, PostedAxisFields);
            }
        }

        private void SetProductColumnHeadings(CsvRow firstRow)
        {
            if (PostedProductFields != null)
            {
                AddCsvColumn(firstRow, "productID", ExportProductFieldDictionary, PostedProductFields);
                AddCsvColumn(firstRow, "productName", ExportProductFieldDictionary, PostedProductFields);
                AddCsvColumn(firstRow, "partNo", ExportProductFieldDictionary, PostedProductFields);
                AddCsvColumn(firstRow, "UNSPSCCode", ExportProductFieldDictionary, PostedProductFields);
                AddCsvColumn(firstRow, "manufacturerFK", ExportProductFieldDictionary, PostedProductFields);
                AddCsvColumn(firstRow, "categoryCodeFK", ExportProductFieldDictionary, PostedProductFields);
                AddCsvColumn(firstRow, "productStatusFK", ExportProductFieldDictionary, PostedProductFields);
                AddCsvColumn(firstRow, "productGroupFK", ExportProductFieldDictionary, PostedProductFields);
                AddCsvColumn(firstRow, "salesAreaGroupFK", ExportProductFieldDictionary, PostedProductFields);
                AddCsvColumn(firstRow, "dateLastUpdate", ExportProductFieldDictionary, PostedProductFields);
                AddCsvColumn(firstRow, "dataSupplierFK", ExportProductFieldDictionary, PostedProductFields);
                AddCsvColumn(firstRow, "supplierStock", ExportProductFieldDictionary, PostedProductFields);
                AddCsvColumn(firstRow, "pageYield", ExportProductFieldDictionary, PostedProductFields);
                AddCsvColumn(firstRow, "supplierLastUpdate", ExportProductFieldDictionary, PostedProductFields);
            }
        }

        private void AddCsvColumn(CsvRow firstRow, string entityName,
                                Dictionary<string, string> dict, string[] postedFields)
        {
            if (dict.ContainsKey(entityName) && postedFields.Contains(entityName))
                firstRow.Add(dict.FirstOrDefault(m => m.Key == entityName).Value);
        }

        private List<product> GetProducts()
        {
            List<product> productList = new List<product>();
            using (ngmdEntities db = new ngmdEntities())
            {
                IQueryable<product> query = db.product.Include("AxisFields.AxisFieldsAdditional")
                                                    .Include(p => p.manufacturer)
                                                    .Include(p => p.productStatus)
                                                    .Include(p => p.productGroup)
                                                    .Include(p => p.salesAreaGroup)
                                                    .Include(p => p.dataSupplier);

                if (PostedProductFields.Contains("categoryCodeFK") && SelectedWebsiteFK > 0)
                    query = query.Include("websiteInventory.categoryCode");

                query = query.OrderBy(p => p.productName);

                query = SetWhereClause(query);
                productList = query.ToList();
            }
            return productList;
        }

        private IQueryable<product> SetWhereClause(IQueryable<product> query)
        {
            if (SelectedProductStatusFK > 0)
            {
                query = query.Where(x => x.productStatusFK == SelectedProductStatusFK);
            }

            if (SelectedWebsiteFK > 0)
            {
                query = query.Where(x => x.websiteInventory.Where(w => w.websiteFK == SelectedWebsiteFK).FirstOrDefault() != null);

                if (SelectedCategoryCodeFK > 0)
                {
                    query = query.Where(x => x.websiteInventory.FirstOrDefault(w => w.categoryCodeFK == SelectedCategoryCodeFK) != null);
                }
            }

            if (SelectedProductGroupFK > 0)
            {
                query = query.Where(x => x.productGroupFK == SelectedProductGroupFK);
            }

            if (SelectedSalesAreaGroupFK > 0)
            {
                query = query.Where(x => x.salesAreaGroupFK == SelectedSalesAreaGroupFK);
            }

            if (SelectedDataSupplierFK > 0)
            {
                query = query.Where(x => x.dataSupplierFK == SelectedDataSupplierFK);
            }

            if (SelectedManufacturerFK > 0)
            {
                query = query.Where(x => x.manufacturerFK == SelectedManufacturerFK);
            }

            if (!string.IsNullOrEmpty(SearchTerm))
            {
                switch (SearchBy)
                {
                    case "productName":
                        query = query.Where(x => x.productName.ToLower().Contains(SearchTerm.ToLower()));
                        break;
                    case "partNo":
                        query = query.Where(x => x.partNo.ToLower().Contains(SearchTerm.ToLower()));
                        break;
                    case "unspsc":
                        query = query.Where(x => x.UNSPSCCode.ToLower().Contains(SearchTerm.ToLower()));
                        break;
                }

                query = query.Where(x => x.productName.ToLower().Contains(SearchTerm));
            }

            return query;
        }

        public ExportProductViewModel GetProductCount()
        {
            int count = 0;

            using (ngmdEntities db = new ngmdEntities())
            {
                IQueryable<product> query = db.product;
                query = SetWhereClause(query);
                count = query.Count();
                //product prd = db.product.First();
                //prd.AxisFields.
            }

            ProductCount = count;
            return this;
        }

        public void DeleteFile(string filePath)
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }

    public static class StringExtensions
    {
        public static string ToSafeString(this object obj)
        {
            return (obj ?? string.Empty).ToString();
        }
    }
    class AxisAttribute
    {
        public int AttributeNameFK { get; set; }
        public int AttributeValueID { get; set; }
        public string AttributeValueDesc { get; set; }
    }
}
