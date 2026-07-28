using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using netGiant.Intranet.DataLayer;
using System.Data.Entity;
using MoreLinq;
using ngBatchProcesses.BusinessObjects.Shared;
using System.IO;
using NGBP.DataAccessLayer.SCOM.SimpleEntities;
using System.Threading;

namespace ngBatchProcesses.BusinessObjects.EcommerceWebsite
{
    public class ProductGridXML
    {
        public ProductGridXML()
        {
            CheckCreateNewFolder();
            GetFilterableAttributes();
            GetAxisValueLookups();
            GetCategoryCodeList();
            GetVATRate();
            GetWebsiteCount();
        }

        private string xmlRootPath;
        private List<categoryAttribute> validCategoryAttributes;
        private StandardFunctions stnFunc;
        private List<AxisValueLookup> axisValueLookups;
        private List<websiteInventory> defaultPrices;
        private List<categoryCode> categoryCodeList;
        private List<AxisPriceView> axisPriceList;
        private List<int> validParentCategories;
        private List<CMS_SE> cmsList;
        private RunType runType;
        private bool errorOccured;
        private int websiteFK = 0;
        private int languageID = 0;
        private double vatRate = 0;
        private bool useHTTPS;
        private static int websiteCount = 0;

        enum RunType
        {
            Full,
            Partial
        }

        public void BuildProductGroupsXML(Dictionary<string, string> parms)
        {
            try
            {
                SetupPrerequisites(parms);
                stnFunc.AddToActivityLog("Started building product grid XMLs. WebsiteID - " + parms["subtype"] +
                    "Run Type - " + (runType == RunType.Full ? "Full" : "Partial"));

                foreach (categoryCode cc in GetProductByCategory(websiteFK))
                {
                    XDocument xmlDoc = BuildFramework();
                    XElement psNode = xmlDoc.Descendants("ps").First();
                    categoryCode topParentCategory = GetTopParentCategory(cc);
                    int prodsCount = 0;

                    if (validProductGridCategory(topParentCategory))
                    {
                        List<string> acceptedAttrs = GetAcceptedCategoryAttributes(topParentCategory);
                        stnFunc.AddToActivityLog("Generating XML for category - " + cc.categoryCodeName);

                        foreach (var inventory in cc.websiteInventory)
                        {
                            prodsCount = ProcessProducts(websiteFK, psNode, topParentCategory,
                                prodsCount, acceptedAttrs, inventory);
                        }

                        foreach (var catCode in cc.secondaryCategoryLookup)
                        {
                            prodsCount = ProcessProducts(websiteFK, psNode, topParentCategory, prodsCount,
                                acceptedAttrs, catCode.websiteInventory);
                        }

                        if (prodsCount > 0)
                            SaveXmlFile(cc, xmlDoc);
                    }
                }

                FinalizeFolders();
                stnFunc.AddToActivityLog("Finished building product grid XMLs. WebsiteID - " + parms["subtype"] +
                    "Run Type - " + (runType == RunType.Full ? "Full" : "Partial"));
            }
            catch (Exception ex)
            {
                stnFunc.AddToActivityLog("Error Building XML file: " + ex.Message);
                stnFunc.AddToActivityLog("Inner Exception: " + ex.InnerException);
                errorOccured = true;
            }

            SaveSendLog();
            //CreateMissingIndexes();
        }

        private void SaveSendLog()
        {
            string filePath = stnFunc.LogActivity("genprodgroupxml");
            if (errorOccured && Properties.Settings.Default.Environment == "Live")
                stnFunc.SendSimpleEmail("genprodgroupxml - " + (runType == RunType.Full ? "Full" : "Partial"), filePath);
        }

        private void SaveXmlFile(categoryCode cc, XDocument xmlDoc)
        {
            try
            {
                xmlDoc.Save(xmlRootPath + "New\\" + cc.AXISGroupNo.Trim() + ".xml",
                    SaveOptions.DisableFormatting);
            }
            catch (Exception ex)
            {
                stnFunc.AddToActivityLog("Error Saving XML File: " + ex.Message);
                errorOccured = true;
            }
        }

        private int ProcessProducts(int websiteFK, XElement psNode, categoryCode topParentCategory,
            int prodsCount, List<string> acceptedAttrs, websiteInventory inventory)
        {
            AxisPriceView pp = axisPriceList.Where(x => x.partNo == inventory.product.partNo && x.language == languageID)
                .OrderBy(x => x.priceTypeID).FirstOrDefault();

            if (pp == null && websiteFK != 1)
            {
                pp = GetDefaultPrice(inventory, null);
            }

            if (inventory.product.productStatusFK == 1 || inventory.product.productStatusFK == 8)
            {
                if (pp != null && pp.tradePriceExVat > 0)
                {
                    AddProdToXML(psNode, topParentCategory, acceptedAttrs, inventory, pp.tradePriceExVat);
                }
                else
                {
                    stnFunc.AddToActivityLog("No Price found for active product: " + inventory.product.partNo +
                        ". Category: " + (inventory.categoryCode != null ? inventory.categoryCode.categoryCodeName : ""));
                    errorOccured = true;
                }
            }

            prodsCount++;
            return prodsCount;
        }

        private void AddProdToXML(XElement psNode,
            categoryCode topParentCategory,
            List<string> acceptedAttrs,
            websiteInventory inventory,
            double? price)
        {
            try
            {
                Dictionary<string, string> attributes = GetSearchableAttributes(inventory, topParentCategory);
                Dictionary<string, string> finalAttributes = GetFinalAttributes(attributes, acceptedAttrs, topParentCategory);
                XElement elementAttributes = new XElement("as", "");

                AddProductAttributes(inventory, finalAttributes, elementAttributes, price, topParentCategory);
                AddProductProperties(psNode, inventory, price);
                psNode.Descendants("pt").Last().Add(elementAttributes);
            }
            catch (Exception ex)
            {
                stnFunc.AddToActivityLog("Error Adding to XML for Product - "
                    + inventory.product.partNo + ": " + ex.Message);
                errorOccured = true;
            }
        }

        private void AddProductProperties(XElement psNode, websiteInventory inventory, double? price)
        {
            try
            {
                string productImage = ProductFunctions.GetProductImage(inventory);

                psNode.Add(new XElement("pt",
                    new XElement("p", inventory.product.partNo, new XAttribute("i", "no")),
                    new XElement("p", inventory.product.manufacturer.manufacturerName, new XAttribute("i", "manufacturer")),
                    new XElement("p", price, new XAttribute("i", "retailpriceex")),
                    new XElement("p", price, new XAttribute("i", "tradepriceex")),
                    new XElement("p", price.HasValue ? (double?)Math.Round(price.Value * (1 + vatRate), 2) : null, new XAttribute("i", "retailpriceinc")),
                    new XElement("p", price.HasValue ? (double?)Math.Round(price.Value * (1 + vatRate), 2) : null, new XAttribute("i", "tradepriceinc")),
                    new XElement("p", productImage, new XAttribute("i", "image")),
                    new XElement("p", inventory.product.productName.Replace("'", ""), new XAttribute("i", "title")),
                    new XElement("p", inventory.product.supplierStock > 0 ? 1 : GetStockOnHandEntryID(inventory), new XAttribute("i", "sohid")),
                    new XElement("p", GetCMSEntryFromMemory(22, GetStockOnHandEntryID(inventory)), new XAttribute("i", "sohtext")),
                    new XElement("p", inventory.product.AxisFields != null ? inventory.product.AxisFields.spec3 : "", new XAttribute("i", "packquantity")),
                    new XElement("p", inventory.product.AxisFields != null ? inventory.product.AxisFields.stockReference : "", new XAttribute("i", "productreference")),
                    new XElement("p", inventory.product.AxisFields != null ? inventory.product.AxisFields.attr6 : 0, new XAttribute("i", "specialofferid")),
                    new XElement("p", inventory.product.AxisFields != null ? LookupAttrDesc(6, inventory.product.AxisFields.attr6) : "", new XAttribute("i", "specialofferdesc")),
                    new XElement("p", GetSpecialOfferDetail(inventory), new XAttribute("i", "specialofferdetail")),
                    new XElement("p", GetProductActionLink(inventory), new XAttribute("i", "productlink")),
                    new XElement("p", inventory.product.pageYield, new XAttribute("i", "yield"))));
            }
            catch (Exception ex)
            {
                stnFunc.AddToActivityLog("Error Adding Properties for Product - "
                    + inventory.product.partNo + ": " + ex.Message);
                errorOccured = true;
            }
        }

        private int GetStockOnHandEntryID(websiteInventory inventory)
        {
            int returnValue = 0;

            try
            {
                if (inventory.product.supplierStock > 0)
                {
                    returnValue = 1; //In Stock
                }
                else
                {
                    if (inventory.product.AxisFields.defaultDeliveryToCust != null)
                    {
                        returnValue = (int)inventory.product.AxisFields.defaultDeliveryToCust + 2;
                    }
                    else
                    {
                        returnValue = 3; //Out of Stock
                    }
                }
            }
            catch (Exception ex)
            {
                stnFunc.AddToActivityLog("Error Getting Stock on Hand Details for Product - "
                    + inventory.product.partNo + ": " + ex.Message);
                errorOccured = true;
            }

            return returnValue;
        }

        private string GetProductActionLink(websiteInventory inventory)
        {
            string link = "";

            try
            {
                product prd = inventory.product;
                string axisStockRef = prd.AxisFields != null ? prd.AxisFields.stockReference : "";
                link = "/product/" + StandardFunctions.CleanupURL(prd.productName + "-" + prd.partNo + "-" + axisStockRef) + "/";
            }
            catch (Exception ex)
            {
                stnFunc.AddToActivityLog("Error Getting Product Action Link for Product - "
                    + inventory.product.partNo + ": " + ex.Message);
                errorOccured = true;
            }

            return link;
        }

        private string GetSpecialOfferDetail(websiteInventory inventory)
        {
            string specialOffer = "";

            try
            {
                int? attribute6 = inventory.product.AxisFields != null ? inventory.product.AxisFields.attr6 : 0;
                int? attribute9 = inventory.product.AxisFields != null ? inventory.product.AxisFields.attr9 : 0;
                string specLine2 = inventory.product.AxisFields != null ? inventory.product.AxisFields.spec2 : "";

                if (attribute6 > 0)
                {
                    switch (attribute6)
                    {
                        case 1:
                            //specialOffer = attribute9 == 4 || attribute9 == 0 ? "Get " + specLine2 + " Cashback!" : "Get Cashback";
                            specialOffer = specLine2 != "" ? "Get " + specLine2 + " Cashback!" : "Get Cashback";
                            break;
                        case 2:
                            specialOffer = "Get Extended " + specLine2 + " Year Warranty";
                            break;
                        case 3:
                            specialOffer = "Reduction - " + specLine2;
                            break;
                        case 4:
                            specialOffer = "Ideal for " + specLine2;
                            break;
                        default:
                            specialOffer = GetCMSEntryFromMemory(50, (int)attribute6);
                            break;
                    }
                }
                else
                {
                    if (attribute9 == 0 || attribute9 == 4)
                        specialOffer = specLine2;
                }
            }
            catch (Exception ex)
            {
                stnFunc.AddToActivityLog("Error Getting Special Offer Detail for Product - "
                    + inventory.product.partNo + ": " + ex.Message);
                errorOccured = true;
            }

            return specialOffer;
        }

        private void AddProductAttributes(websiteInventory inventory,
            Dictionary<string, string> finalAttributes,
            XElement elementAttributes,
            double? price,
            categoryCode topParentCategory)
        {
            try
            {
                elementAttributes.Add(new XElement("at",
                    new XAttribute("n", "price"),
                    new XAttribute("v", price)));

                if (topParentCategory.categoryCodeID == 407 || //Office Facilities
                        topParentCategory.categoryCodeID == 541 || //Office Supplies
                        topParentCategory.categoryCodeID == 498 || //Office Machines
                        topParentCategory.categoryCodeID == 765)   //Technology
                {
                    elementAttributes.Add(new XElement("at",
                        new XAttribute("v", "#" + LookupAttrDesc(7, inventory.product.AxisFields != null ? inventory.product.AxisFields.attr7 : 0) + "#"),
                        new XAttribute("n", "producttype"), new XAttribute("f", "Product Type")));
                }

                elementAttributes.Add(new XElement("at",
                    new XAttribute("v", "#" + inventory.product.manufacturer.manufacturerName + "#"),
                    new XAttribute("n", "manufacturer"), new XAttribute("f", "Manufacturer")));

                elementAttributes.Add(new XElement("at",
                    new XAttribute("n", "specialoffer"),
                    new XAttribute("v", "#" + (inventory.product.AxisFields != null ?
                        LookupAttrDesc(6, inventory.product.AxisFields.attr6) : "") + "#"),
                    new XAttribute("f", "Promotion")));

                foreach (var att in finalAttributes)
                {
                    elementAttributes.Add(new XElement("at",
                        new XAttribute("v", "#" + att.Value + "#"),
                        new XAttribute("f", att.Key),
                        new XAttribute("n", att.Key.Replace(" ", "").Replace("(", "").Replace(")", "").Replace("/", "").ToLower())));
                }
            }
            catch (Exception ex)
            {
                stnFunc.AddToActivityLog("Error Adding Product Attributes for Product - "
                    + inventory.product.partNo + ": " + ex.Message);
                errorOccured = true;
            }
        }

        private List<string> GetAcceptedCategoryAttributes(categoryCode cc)
        {
            return validCategoryAttributes.Where(x => x.categoryCodeFK == cc.categoryCodeID)
                                .Select(x => x.filterableAttribute.attributeName).ToList();
        }

        private Dictionary<string, string> GetFinalAttributes(Dictionary<string, string> attributes,
            List<string> catAtts, categoryCode topParentCategory)
        {
            try
            {
                if (topParentCategory.categoryCodeID != 407 &&      //Office Facilities
                        topParentCategory.categoryCodeID != 541 &&  //Office Supplies
                        topParentCategory.categoryCodeID != 498 &&  //Office Machines
                        topParentCategory.categoryCodeID != 765)    //Technology
                {
                    attributes = attributes.Where(x => catAtts.Contains(x.Key))
                    .ToDictionary(t => t.Key, t => t.Value);
                }
            }
            catch (Exception ex)
            {
                stnFunc.AddToActivityLog("Error Getting Final Attributes for Category - "
                    + topParentCategory.categoryCodeName + ": " + ex.Message);
                errorOccured = true;
            }

            return attributes;
        }

        private Dictionary<string, string> GetSearchableAttributes(websiteInventory inventory, categoryCode topParentCategory)
        {
            Dictionary<string, string> atts = new Dictionary<string, string>();

            try
            {
                KeyValuePair<string, string> stationeryAddAtts = default(KeyValuePair<string, string>);

                using (ngmdEntities db = new ngmdEntities())
                {
                    IQueryable<ds_searchableView> searchableQuery = db.ds_searchableView
                        .Where(x => x.manufacturer == inventory.product.manufacturer.manufacturerName);

                    if (!string.IsNullOrEmpty(inventory.product.manuReference))
                    {
                        searchableQuery = searchableQuery.Where(x => x.partNo == inventory.product.partNo ||
                            x.partNo == inventory.product.manuReference);
                    }
                    else
                    {
                        searchableQuery = searchableQuery.Where(x => x.partNo == inventory.product.partNo);
                    }

                    atts = searchableQuery.DistinctBy(x => x.name).ToDictionary(x => x.name, x => x.value);

                    //if (topParentCategory.categoryCodeID == 407 || //Office Facilities
                    //    topParentCategory.categoryCodeID == 541 || //Office Supplies
                    //    topParentCategory.categoryCodeID == 498 || //Office Machines
                    //    topParentCategory.categoryCodeID == 765)   //Technology
                    //{
                    //    stationeryAddAtts = new KeyValuePair<string, string>("Product Type",
                    //        LookupAttrDesc(7, inventory.product.AxisFields != null ? inventory.product.AxisFields.attr7 :
                    //            0));
                    //}
                }

                if (!stationeryAddAtts.Equals(default(KeyValuePair<string, string>)))
                    atts.Add(stationeryAddAtts.Key, stationeryAddAtts.Value);
            }
            catch (Exception ex)
            {
                stnFunc.AddToActivityLog("Error Getting Searchable Attributes for Product - "
                    + inventory.product.partNo + ": " + ex.Message);
                stnFunc.AddToActivityLog("Inner Exception: " + ex.InnerException);
                errorOccured = true;
            }

            return atts;
        }

        private categoryCode GetTopParentCategory(categoryCode catCode)
        {
            categoryCode cc = null;

            try
            {
                int? parentCatCodeID = catCode.parentCategoryCodeID;

                while ((parentCatCodeID != null && parentCatCodeID > 0))
                {
                    if (cc != null && cc.categoryCodeID == cc.parentCategoryCodeID)
                        break;

                    cc = categoryCodeList.Where(x => x.categoryCodeID == parentCatCodeID).FirstOrDefault();
                    parentCatCodeID = cc.parentCategoryCodeID;
                }
            }
            catch (Exception ex)
            {
                stnFunc.AddToActivityLog("Error Getting Top Parent Category: " + ex.Message);
                errorOccured = true;
            }

            return cc;
        }

        private XDocument BuildFramework()
        {
            XDocument xml = new XDocument();
            xml.Add(new XElement("xml",
                new XElement("ps")));

            return xml;
        }

        private List<categoryCode> GetProductByCategory(int websiteFK)
        {
            List<categoryCode> ccList = null;

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    if (runType == RunType.Full)
                    {
                        ccList = (from cc in db.categoryCode
                            .Include("websiteInventory.product.manufacturer")
                            .Include("websiteInventory.product.AxisFields")
                            .Include("websiteInventory.productPrice")
                            .Include("websiteInventory.product.productGroup")
                            .Include("secondaryCategoryLookup.websiteInventory.product.manufacturer")
                            .Include("secondaryCategoryLookup.websiteInventory.product.AxisFields")
                            .Include("secondaryCategoryLookup.websiteInventory.productPrice")
                                  where cc.websiteFK == websiteFK
                                  && cc.parentCategoryCodeID != null
                                  select cc).ToList();
                    }
                    else if (runType == RunType.Partial)
                    {
                        DateTime todayMinus2 = DateTime.Today.AddDays(-2);
                        ccList = db.websiteInventory
                            .Where(x => x.productPrice.OrderByDescending(y => y.productPriceID).FirstOrDefault()
                                .dateLastUpdated > todayMinus2 && x.websiteFK == websiteFK)
                            .Select(x => x.categoryCode)
                            .Include("websiteInventory.product.manufacturer")
                            .Include("websiteInventory.product.AxisFields")
                            .Include("websiteInventory.productPrice")
                            .Include("websiteInventory.product.productGroup")
                            .Include("secondaryCategoryLookup.websiteInventory.product.manufacturer")
                            .Include("secondaryCategoryLookup.websiteInventory.product.AxisFields")
                            .Include("secondaryCategoryLookup.websiteInventory.productPrice")
                            .Distinct()
                            .ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                stnFunc.AddToActivityLog("Error Getting Products by Category: " + ex.Message);
                errorOccured = true;
            }

            return ccList;
        }

        private string LookupAttrDesc(int attrNameId, int? attrId)
        {
            string returnValue = "";

            try
            {
                if (attrId != null)
                {
                    returnValue = axisValueLookups.Where(x => x.axisTypeNameFK == 1 &&
                                    x.attrNameFK == attrNameId && x.attrValueID == attrId)
                                    .FirstOrDefault().attrValueDesc;
                }
            }
            catch (Exception ex)
            {
                stnFunc.AddToActivityLog("Error Getting Axis Attribute Lookup Value for Attribute - Name="
                    + attrNameId + " ID=" + attrId + ": " + ex.Message);
                errorOccured = true;
            }

            return returnValue;
        }

        private bool validProductGridCategory(categoryCode topParentCategory)
        {
            bool returnValue = false;
            if (topParentCategory != null && validParentCategories.Contains(topParentCategory.categoryCodeID))
            {
                returnValue = true;
            }

            return returnValue;
        }

        private AxisPriceView GetDefaultPrice(websiteInventory inventory, AxisPriceView pp)
        {
            try
            {
                //pp = axisPriceList.Where(x => x.language == 1 && x.partNo == inventory.product.partNo).FirstOrDefault();

                //Try to get the master price first - TG
                pp = axisPriceList.Where(x => x.language == 1 && x.partNo == inventory.product.partNo).FirstOrDefault();

                if (pp == null)
                {
                    //Default back to the other websites if no TG price is found
                    for (int i = 2; i <= websiteCount; i++)
                    {
                        int lanID = i;
                        if (i == 3)
                            lanID = 5;

                        if (axisPriceList.Where(x => x.language == lanID && x.partNo == inventory.product.partNo).FirstOrDefault() != null)
                            pp = axisPriceList.Where(x => x.language == lanID && x.partNo == inventory.product.partNo).FirstOrDefault();
                    }
                }
            }
            catch (Exception ex)
            {
                stnFunc.AddToActivityLog("Error Getting Default Price for product - " + inventory.product.partNo + ": " + ex.Message);
                errorOccured = true;
            }

            return pp;
        }

        private void SetupPrerequisites(Dictionary<string, string> parms)
        {
            stnFunc = new StandardFunctions();
            websiteFK = Convert.ToInt32(parms["subtype"]);
            xmlRootPath = parms["output"];
            SetRunType(parms["input"].ToLower().Trim());
            SetLanguageID();
            GetAxisPrices();
            GetValidParentCategories();
            GetCMSEntries();
            GetHttpsSetting();
        }

        private void GetAxisPrices()
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                //Exclude account specific axis pricing
                axisPriceList = db.AxisPriceView.Where(x => x.account == null).ToList();
            }
        }

        private void GetDefaultPrices()
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    defaultPrices = db.websiteInventory.Include(x => x.productPrice).Include(x => x.product)
                        .Where(x => x.websiteFK == 1).ToList();
                }
            }
            catch (Exception ex)
            {
                stnFunc.AddToActivityLog("Error Getting Default Pricing List: " + ex.Message);
                errorOccured = true;
            }
        }

        private void GetAxisValueLookups()
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    axisValueLookups = db.AxisValueLookup.Where(x => x.axisTypeNameFK == 1).ToList();
                }
            }
            catch (Exception ex)
            {
                stnFunc.AddToActivityLog("Error Getting Axis Value Lookups List: " + ex.Message);
                errorOccured = true;
            }
        }

        private void GetFilterableAttributes()
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    validCategoryAttributes = db.categoryAttribute.Include(x => x.filterableAttribute).ToList();
                }
            }
            catch (Exception ex)
            {
                stnFunc.AddToActivityLog("Error Getting Filterable Attributes List: " + ex.Message);
                errorOccured = true;
            }
        }

        private void GetCategoryCodeList()
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    categoryCodeList = db.categoryCode.ToList();
                }
            }
            catch (Exception ex)
            {
                stnFunc.AddToActivityLog("Error Getting Category Code List: " + ex.Message);
                errorOccured = true;
            }
        }

        private void GetValidParentCategories()
        {
            try
            {
                validParentCategories = StandardFunctions.GetConfigurationSetting("Product Grid", "validProductGridParents", websiteFK)
                    .Split('|')
                    .Select(Int32.Parse)
                    .ToList();
            }
            catch (Exception ex)
            {
                stnFunc.AddToActivityLog("Error Getting valid parent categories: " + ex.Message);
                errorOccured = true;
            }
        }

        private void FinalizeFolders()
        {
            try
            {
                if (runType == RunType.Partial)
                {
                    KeepUnchangedFiles();
                }

                RenameFolders();
                CleanupFolders();
                VerifyFolders();
            }
            catch (Exception ex)
            {
                stnFunc.AddToActivityLog("Error Finalizing Folder Names: " + ex.Message);
                errorOccured = true;
                VerifyFolders();
            }
        }

        private void VerifyFolders()
        {
            var rollback = false;

            if (Directory.Exists(xmlRootPath + "Current"))
            {
                var currentDirectory = new DirectoryInfo(xmlRootPath + "Current");

                if (currentDirectory.GetFiles().Count() == 0)
                {
                    rollback = true;
                }
            }
            else
            {
                rollback = true;
            }

            if (rollback)
            {
                stnFunc.AddToActivityLog("Attempting to rollback folder names due to error in file paths.");

                foreach (var subDir in new DirectoryInfo(xmlRootPath)
                    .EnumerateDirectories()
                    .OrderByDescending(x => x.CreationTime))
                {
                    if (subDir.Name.Substring(0, 3) == "Old")
                    {
                        subDir.MoveTo(xmlRootPath + "Current");
                        break;
                    }
                }
            }

            CheckCreateNewFolder();
        }

        private void KeepUnchangedFiles()
        {
            foreach (FileInfo file in new DirectoryInfo(xmlRootPath + "Current").GetFiles())
            {
                if (!File.Exists(xmlRootPath + "New\\" + file.Name))
                {
                    file.CopyTo(xmlRootPath + "New\\" + file.Name);
                }
            }
        }

        private void RenameFolders()
        {
            TimeSpan timespan = (DateTime.Now - new DateTime(1970, 1, 1));
            Directory.Move(xmlRootPath + "Current", xmlRootPath + "Old" + timespan.Ticks);
            Directory.Move(xmlRootPath + "New", xmlRootPath + "Current");
            CheckCreateNewFolder();
        }

        private void CleanupFolders()
        {
            foreach (var subDir in Directory.GetDirectories(xmlRootPath))
            {
                DirectoryInfo subDirInfo = new DirectoryInfo(subDir);

                if (subDirInfo.Name.Substring(0, 3) == "Old")
                {
                    double daysAge = (DateTime.Now - subDirInfo.CreationTime).TotalDays;
                    if (daysAge > 4)
                    {
                        foreach (var file in subDirInfo.GetFiles())
                        {
                            file.Delete();
                        }

                        Directory.Delete(subDir);
                    }
                }
            }
        }

        private void CheckCreateNewFolder()
        {
            try
            {
                var retryCount = 10;

                for (int i = 0; i <= retryCount; i++)
                {
                    if (!Directory.Exists(xmlRootPath + "New"))
                    {
                        Thread.Sleep(2000);
                        CreateNewFolder();
                    }
                    else
                    {
                        break;
                    }

                    retryCount++;
                }
            }
            catch (Exception ex)
            {
                stnFunc.AddToActivityLog("Error Creating 'New' Directory: " + ex.Message);
                errorOccured = true;
            }
        }

        private void CreateNewFolder()
        {
            Directory.CreateDirectory(xmlRootPath + "New");
        }

        private void SetRunType(string p)
        {
            if (p == "full")
            {
                runType = RunType.Full;
            }
            else
            {
                runType = RunType.Partial;
            }
        }

        private void SetLanguageID()
        {
            switch (websiteFK)
            {
                case 1:
                    languageID = 1;
                    break;
                case 2:
                    languageID = 2;
                    break;
                case 3:
                    languageID = 5;
                    break;
                default:
                    languageID = 1;
                    break;
            }
        }

        private void GetVATRate()
        {
            string vatRateSetting = StandardFunctions.GetConfigurationSetting("Pricing", "vat");
            double.TryParse(vatRateSetting, out vatRate);
        }

        private void GetCMSEntries()
        {
            try
            {
                cmsList = StandardFunctions.GetAllCMSEntries(websiteFK, "T");
            }
            catch (Exception ex)
            {
                stnFunc.AddToActivityLog("**ERROR** Executing StandardFunctions.GetAllCMSEntries");
                stnFunc.ProcessException(ex);
                errorOccured = true;
            }
        }

        private string GetCMSEntryFromMemory(int seriesID, int textID)
        {
            string cmsContent = "";
            CMS_SE cmsEntry = cmsList.Where(x => x.SeriesID == seriesID && x.TextID == textID).FirstOrDefault();

            if (cmsEntry != null)
                cmsContent = cmsEntry.CMSContent;

            return cmsContent;
        }

        private void GetHttpsSetting()
        {
            useHTTPS = Convert.ToBoolean(StandardFunctions.GetConfigurationSetting("Website Application Variables", "UseHTTPS", websiteFK));
        }

        private void GetWebsiteCount()
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                websiteCount = db.Websites.Count();
            }
        }

        /// <summary>
        /// This runs a stored procedure which creates missing indexes.
        /// Axis removes indexes on a publish so this recreates them.
        /// This whole job is tied into a publish so the reason it is run at this point.
        /// </summary>
        private static void CreateMissingIndexes()
        {
            Dictionary<string, string> di = new Dictionary<string, string>();
            di.Add("type", "runsp");
            di.Add("subtype", "ngmd.CreateMissingIndexes");
            di.Add("dbname", "netgiantMasterData");

            RunSP.ExecuteStoredProcedure(di);
        }
    }
}
