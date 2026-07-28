using ngBatchProcesses.BusinessObjects.Shared;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Xml.Linq;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using NGBP.DataAccessLayer.DataUtilities;

namespace ngBatchProcesses.BusinessObjects.Axis
{
    public class XMLFeedV2
    {
        public XMLFeedV2(Dictionary<string, string> parms)
        {
            StandardFunctions.WriteProcessStarted();
            Parms = parms;
            if (!Parms.ContainsKey("action"))
            {
                Parms.Add("action", "");
            }
            InTestMode = Parms.ContainsKey("testmode");
            websiteCount = EntityFunctions.GetWebsiteList(x => true).Count();
        }
        private Dictionary<string, string> Parms { get; set; }
        private bool InTestMode { get; set; } = false;
        private int recordCount { get; set; } = 0;
        private List<string> _fieldNames { get; set; }
        private bool _isFull { get; set; }
        private string _currentPartNo { get; set; }
        private List<int> _axisQueueIds { get; set; }
        private int websiteCount { get; set; }
        private XNamespace af { get; set; } = "http://resources.axisfirst.co.uk/xml/";
        private XNamespace wc { get; set; } = "http://www.w3.org/2001/XMLSchema-instance";

        public void ProcessAxisQueue()
        {
            BuildXML();
        }

        private void BuildXML()
        {
            try
            {
                StandardFunctions.WriteProcessStarted();
                _axisQueueIds = new List<int>();

                XDocument xml = ConstructXML();
                xml.Save((string)Properties.Settings.Default["AxisQueueDirectory"] + "AxisRichData_" + DateTime.Now.ToString("yyyyMMddhhmmss_000000001") + ".xml");

                xml = null;

                UpdateCompletedDate();
                ClearAxisQueueRecords();
            }
            catch (Exception ex)
            {
                StandardFunctions.WriteException(ex);
            }
            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "No, of products processed: " + recordCount });
            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
        }

        private XDocument ConstructXML()
        {
            DataTable data = GetAxisQueue();
            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Successfully extracted data from SQL" });
            XDocument xml = BuildXMLFramework();

            if (data.Rows.Count > 0)
            {
                foreach (DataRow row in data.Rows)
                {
                    if (CheckValidity(row))
                        continue;

                    ExtractKeyData(row);

                    XElement prodElement = BuildProductElement(row);
                    BuildProductDetailElement(prodElement, row);
                    BuildAttributesElement(prodElement, row);
                    BuildPricingElement(prodElement, row);
                    BuildEbusinessElement(prodElement, row);
                    BuildSuppliersElement(prodElement, row);
                    BuildComponentsElement(prodElement, row);
                    AddProductToXML(xml, prodElement);
                    _axisQueueIds.Add((int)row["AXISQueueID"]);
                }
            }
            return xml;
        }

        private bool CheckValidity(DataRow row)
        {
            return ((int)row["productStatusFK"] == 1 || (int)row["productStatusFK"] == 9) && row["prices"].ToString() == "";
        }

        private DataTable GetAxisQueue()
        {
            try
            {
                return SQLUtilities.ExecuteStoredProcedureQuery("netgiantmasterdata", "ngmd.AXISQueueFeedData", 3000);
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Error getting Axis Queue Data from SQL, using SP ngmd.AXISQueueFeedData. " +
                    "Error Message: " + ex.Message);
            }
        }

        private void ExtractKeyData(DataRow row)
        {
            try
            {
                _currentPartNo = row["partNo"].ToString();
                recordCount++;

                _isFull = row["CRUD"].ToString() == "C";
                _fieldNames = new List<string>();
                _fieldNames = row["fieldNames"].ToString().Split(new [] { "$$" }, StringSplitOptions.None)
                    .Where(x => x != string.Empty).ToList();
            }
            catch (Exception ex)
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR extracting the key data for Axis Queue record", ErrorCode = "ERROR" });
                StandardFunctions.WriteException(ex);
            }
        }

        private XDocument BuildXMLFramework()
        {
            XDocument xmlDoc = new XDocument(
                new XDeclaration("1.0", "UTF-8", ""),
                    new XElement(af + "AXISEnvelope",
                    new XAttribute(XNamespace.Xmlns + "xsi", wc),
                        new XElement(af + "Products", "")));

            return xmlDoc;
        }

        private XElement BuildProductElement(DataRow row)
        {
            try
            {
                return new XElement(af + "Product",
                    new XAttribute("altref", row["partNo"]),
                    new XElement(af + "ProductDetail", ""));
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Error building product element of xml for partNo - " +
                    _currentPartNo + " Error Message: " + ex.Message);
            }
        }

        private void BuildProductDetailElement(XElement xml, DataRow row)
        {
            try
            {
                XElement productDetail = xml.Element(af + "ProductDetail");

                AddElement("SalesGroup", "salesAreaGroupNo", productDetail, row);
                AddElement("ProductGroup", "productGroupNo", productDetail, row);
                AddResaleable(productDetail, row);
                AddDiscontinued(productDetail, row);
                AddProductAlias(productDetail, row);

                var spec1 = AddProductName(row["productName"].ToString(), xml, row);
                productDetail.Add(new XElement(af + "Specification",
                    new XAttribute("line", "1"),
                    new XAttribute("language", "en"),
                    spec1));

                AddAttribute("language", "en", AddAttribute("line", "2", AddElement("Specification", "spec2", productDetail, row)));
                AddAttribute("language", "en", AddAttribute("line", "3", AddElement("Specification", "spec3", productDetail, row)));
                AddAttribute("language", "en", AddAttribute("line", "4", AddElement("Specification", "spec4", productDetail, row)));
                AddAttribute("language", "en", AddAttribute("line", "5", AddElement("Specification", "UNSPSCCode", productDetail, row)));
                AddAttribute("language", "en", AddAttribute("line", "6", AddElement("Specification", "spec6", productDetail, row)));
                AddProductNotes(productDetail, row);
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Error building product detail element of xml for partNo - " +
                    _currentPartNo + " Error Message: " + ex.Message);
            }
        }

        private void AddProductAlias(XElement productDetail, DataRow row)
        {
            try
            {
                string inventoryIdString = (string)row["inventoryIds"];
                var inventoryIdArray = inventoryIdString.Split(new [] { "$$" }, StringSplitOptions.None);

                foreach (var id in inventoryIdArray)
                {
                    if (id.Length > 0)
                    {
                        productDetail.Add(new XElement(af + "ProductAlias", id));
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException("AddProductAlias " + ex.Message);
            }
        }

        private void AddProductNotes(XElement productDetail, DataRow row)
        {
            try
            {
                if (PassField("stockNoteDesc1") ||
                    PassField("stockNoteDesc2") ||
                    PassField("stockNoteDesc3"))
                {
                    string stockNotes = row["stockNotes"].ToString();
                    string[] stockNotesList = stockNotes.Split(new [] { "$$" }, StringSplitOptions.None);

                    foreach (string note in stockNotesList)
                    {
                        string[] stockNoteDetail = note.Split(new [] { "##" }, StringSplitOptions.None);

                        if (stockNoteDetail.Count() > 1)
                        {
                            string websiteFK = stockNoteDetail[0];
                            string stockNote = Uri.EscapeDataString(stockNoteDetail[1].Replace("£", "&pound;"));
                            string language = string.Empty;

                            if (stockNote.Length > 0)
                            {
                                switch (websiteFK)
                                {
                                    case "1":
                                        language = "en";
                                        break;
                                    case "2":
                                        language = "fr";
                                        break;
                                    case "3":
                                        language = "de";
                                        break;
                                }

                                productDetail.Add(new XElement(af + "Notes",
                                    new XAttribute("language", language),
                                    stockNote));
                            }
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException("AddProductNotes " + ex.Message);
            }
        }

        private void AddResaleable(XElement productDetail, DataRow row)
        {
            try
            {
                if (PassField("reSaleable"))
                {
                    string resaleable = "true";

                    if ((int)row["productStatusFK"] == 3)
                    {
                        resaleable = "false";
                    }

                    productDetail.Add(new XElement(af + "Resaleable", resaleable));
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException("AddResaleable " + ex.Message);
            }
        }

        private void AddDiscontinued(XElement productDetail, DataRow row)
        {
            try
            {
                if (PassField("productStatusFK"))
                {
                    string discontinued = "false";

                    if ((int)row["productStatusFK"] == 3)
                    {
                        discontinued = "true";
                    }

                    productDetail.Add(new XElement(af + "Discontinued", discontinued));
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException("AddDiscontinued " + ex.Message);
            }
        }

        private string AddProductName(string name, XElement xml, DataRow row)
        {
            var leftover = "";

            try
            {
                var desc1 = "";
                var desc2 = "";
                XElement productDetail = xml.Element(af + "ProductDetail");

                if (PassField("productName"))
                {

                    var length = name.Length;

                    if (length <= 30)
                    {
                        desc1 = name;
                    }
                    else if (length > 30)
                    {
                        desc1 = name.Substring(0, 30);
                        var desc1LastIndex = 0;

                        if (name.Substring(30, 1) != " ")
                        {
                            desc1LastIndex = desc1.LastIndexOf(" ");
                            desc1 = desc1.Substring(0, desc1LastIndex);
                        }
                        else
                        {
                            desc1LastIndex = 30;
                        }

                        if (length - desc1LastIndex <= 30)
                        {
                            desc1LastIndex = desc1LastIndex == 0 ? 31 : desc1LastIndex + 1;
                            desc2 = name.Substring(desc1LastIndex, length - desc1LastIndex);
                        }
                        else
                        {
                            desc2 = name.Substring(desc1LastIndex, 30);
                            var desc2LastIndex = desc2.LastIndexOf(" ");

                            if (name.Substring(desc1LastIndex + 30, 1) != " ")
                            {
                                desc2 = name.Substring(desc1LastIndex, desc2LastIndex);
                            }

                            leftover = name.Substring(desc1.Length + desc2.Length, length - (desc1.Length + desc2.Length));

                            if (leftover.Length <= 30)
                            {

                            }
                            else
                            {
                                if (leftover.Substring(0 + 30, 1) != " ")
                                {
                                    leftover = leftover.Substring(1, leftover.Substring(0, 30).LastIndexOf(" "));
                                }
                                else
                                {
                                    leftover = leftover.Substring(0, 30);
                                }
                            }
                        }
                    }

                    productDetail.Add(new XElement(af + "Description", desc1.Trim(),
                        new XAttribute("line", "1"),
                        new XAttribute("language", "en")));

                    if (desc2.Length > 0)
                    {
                        productDetail.Add(new XElement(af + "Description", desc2.Trim(),
                            new XAttribute("line", "2"),
                            new XAttribute("language", "en")));
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException("AddProductName " + ex.Message);
            }

            return leftover.Trim();
        }

        private XElement AddElement(string nodeName, string fieldName,
            XElement ele, DataRow row, bool empty = false)
        {
            try
            {
                if (PassField(fieldName))
                {
                    ele.Add(new XElement(af + nodeName, empty == false ? row[fieldName].ToString() : ""));

                    IEnumerable<XElement> elements = ele.Elements(af + nodeName);

                    if (elements.Count() > 0)
                    {
                        return elements.Last();
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException("AddElement - " + nodeName + " " + fieldName + ex.Message);
            }
        }

        private XElement AddAttribute(string attrName, string attrValue, XElement element)
        {
            if (element != null)
                element.Add(new XAttribute(attrName, attrValue));

            return element;
        }

        private void BuildAttributesElement(XElement prodElement, DataRow row)
        {
            try
            {
                string passAttrs = _fieldNames.FirstOrDefault(str => str.Contains("attr") || str.Contains("manufacturerFK"));

                if (_isFull || passAttrs != null)
                {
                    prodElement.Add(new XElement(af + "Attributes", ""));
                    XElement attrs = prodElement.Element(af + "Attributes");

                    // Attribute 1 removed 03/02/2017. Attribute 1 in PMS is now related to the ordering on the printer4 page.
                    //if (_isFull || _fieldNames.Contains("attr1"))
                    //    AddElement("Attribute", "attr1", attrs, row, true)
                    //        .Add(new XAttribute("id", "1"), new XAttribute("code", row["attr1"].ToString()));
                    //if (_isFull || _fieldNames.Contains("attr2"))
                    //    AddElement("Attribute", "attr2", attrs, row, true)
                    //        .Add(new XAttribute("id", "2"), new XAttribute("code", row["attr2"].ToString()));
                    //if (_isFull || _fieldNames.Contains("attr3"))
                    //    AddElement("Attribute", "attr3", attrs, row, true)
                    //        .Add(new XAttribute("id", "3"), new XAttribute("code", row["attr3"].ToString()));
                    //if (_isFull || _fieldNames.Contains("attr4"))
                    //    AddElement("Attribute", "attr4", attrs, row, true)
                    //        .Add(new XAttribute("id", "4"), new XAttribute("code", row["attr4"].ToString()));
                    //if (_isFull || _fieldNames.Contains("attr5"))
                    //    AddElement("Attribute", "attr5", attrs, row, true)
                    //        .Add(new XAttribute("id", "5"), new XAttribute("code", row["attr5"].ToString()));
                    //if (_isFull || _fieldNames.Contains("attr6"))
                    //    AddElement("Attribute", "attr6", attrs, row, true)
                    //        .Add(new XAttribute("id", "6"), new XAttribute("code", row["attr6"].ToString()));
                    //if (_isFull || _fieldNames.Contains("attr7"))
                    //    AddElement("Attribute", "attr7", attrs, row, true)
                    //        .Add(new XAttribute("id", "7"), new XAttribute("code", row["attr7"].ToString()));
                    //if (_isFull || _fieldNames.Contains("attr8"))
                    //    AddElement("Attribute", "attr8", attrs, row, true)
                    //        .Add(new XAttribute("id", "8"), new XAttribute("code", row["attr8"].ToString()));
                    //if (_isFull || _fieldNames.Contains("attr9"))
                    //    AddElement("Attribute", "attr9", attrs, row, true)
                    //        .Add(new XAttribute("id", "9"), new XAttribute("code", row["attr9"].ToString()));
                    if (_isFull || _fieldNames.Contains("manufacturerFK"))
                        AddElement("Attribute", "manufacturerFK", attrs, row, true)
                            .Add(new XAttribute("id", "10"), new XAttribute("value", row["manufacturerName"].ToString()));
                }

            }
            catch (Exception ex)
            {
                throw new ApplicationException("Error building product attributes element of xml for partNo - " +
                    _currentPartNo + " Error Message: " + ex.Message);
            }
        }

        private void BuildPricingElement(XElement prodElement, DataRow row)
        {
            try
            {
                if (_isFull || _fieldNames.Contains("price"))
                {
                    var pricingField = row["prices"].ToString();
                    
                    if (!string.IsNullOrEmpty(pricingField))
                    {
                        string[] sitesPricing = pricingField.Remove(pricingField.Length - 2).Split(new string[] { "||" }, StringSplitOptions.None);
                        var defaultPrice = ExtractDefaultPrice(sitesPricing);
                        string[] tgPriceDetails = defaultPrice.Split('|');

                        prodElement.Add(new XElement(af + "Pricing"));
                        XElement priceElement = prodElement.Descendants(af + "Pricing").FirstOrDefault();

                        AddStockPricingElements(tgPriceDetails, priceElement);

                        priceElement.Add(new XElement(af + "MatrixPricing"));
                        XElement matrix = priceElement.Descendants(af + "MatrixPricing").First();

                        string[] breakQuantities = null;
                        if (!string.IsNullOrEmpty(row["breakQuantities"].ToString()))
                        {
                            breakQuantities = row["breakQuantities"].ToString().Remove(row["breakQuantities"].ToString().Length - 2).Split(new string[] { "$$" }, StringSplitOptions.None);
                        }

                        for (int i = 1; i <= websiteCount; i++)
                        {
                            var currentSite = sitesPricing.Where(x => x.Split('|')[0] == i.ToString()).FirstOrDefault();
                            if (currentSite != null)
                            {
                                AddSiteSpecificPricingElements(matrix, i, currentSite, breakQuantities);
                            }
                            else
                            {
                                AddSiteSpecificPricingElements(matrix, i, defaultPrice, breakQuantities);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Error building product pricing element of xml for partNo - " +
                    _currentPartNo + " Error Message: " + ex.Message);
            }
        }

        private void AddStockPricingElements(string[] tgPriceDetails, XElement priceElement)
        {
            priceElement.Add(new XElement(af + "RetailPrice", string.Format("{0:0.00}", Convert.ToDecimal(tgPriceDetails[2]))),
                new XElement(af + "TradePrice", string.Format("{0:0.00}", Convert.ToDecimal(tgPriceDetails[2]))),
                new XElement(af + "QuantityBreak1", "1"),
                new XElement(af + "QuantityBreak2", "2"),
                new XElement(af + "QuantityPrice2", string.Format("{0:0.00}", Convert.ToDecimal(tgPriceDetails[4]))),
                new XElement(af + "QuantityBreak3", "999999999"),
                new XElement(af + "QuantityPrice3", string.Format("{0:0.00}", Convert.ToDecimal(tgPriceDetails[6]))));
        }

        private string ExtractDefaultPrice(string[] sitesPricing)
        {
            //Try to get the master price first - TG
            var defaultPrice = sitesPricing.Where(x => x.Split('|')[0] == "1").FirstOrDefault();

            if (defaultPrice == null)
            {
                //Default back to the other websites if no TG price is found
                for (int i = 2; i <= websiteCount; i++)
                {
                    if (sitesPricing.Where(x => x.Split('|')[0] == i.ToString()).FirstOrDefault() != null)
                        defaultPrice = sitesPricing.Where(x => x.Split('|')[0] == i.ToString()).FirstOrDefault();
                }
            }

            return defaultPrice;
        }

        private void AddSiteSpecificPricingElements(XElement matrix, int i, string currentSite, string[] breakQuantities)
        {
            string[] siteDetails = currentSite.Split('|');
            string websiteID = siteDetails[0];
            string websitePrice = siteDetails[2];
            string break2Price = siteDetails[4];
            string break3Price = siteDetails[6];

            string breakQty1 = "1";
            string breakQty2 = "2";
            string breakQty3 = "999999999";

            if (breakQuantities != null)
            {
                string breakQuantitiesArray = breakQuantities.Where(x => x.Split(new string[] { "##" }, StringSplitOptions.None)[0] == i.ToString()).FirstOrDefault();

                if (breakQuantitiesArray != null)
                {
                    string[] currentSiteBreakQuantities = breakQuantitiesArray.Split(new string[] { "##" }, StringSplitOptions.None);
                    string breakQtyWebsiteID = currentSiteBreakQuantities[0];
                    breakQty1 = currentSiteBreakQuantities[1];
                    breakQty2 = currentSiteBreakQuantities[2];
                    breakQty3 = currentSiteBreakQuantities[3];
                }
            }

            matrix.Add(new XElement(af + "CustomerPriceGroup", new XAttribute("id", i),
                new XElement(af + "Price", websitePrice),
                new XElement(af + "QuantityBreak1", breakQty1),
                new XElement(af + "QuantityPrice2", break2Price),
                new XElement(af + "QuantityBreak2", breakQty2),
                new XElement(af + "QuantityPrice3", break3Price),
                new XElement(af + "QuantityBreak3", breakQty3)));
        }

        private void BuildEbusinessElement(XElement prodElement, DataRow row)
        {
            try
            {
                if (PassEbusinessSection())
                {
                    prodElement.Add(new XElement(af + "eBusiness",
                            new XElement(af + "Sites", "")));

                    XElement sitesElement = prodElement.Descendants(af + "Sites").First();

                    //Axis Site Ids
                    int[] websiteIds = { 3 };

                    foreach (int siteId in websiteIds)
                    {
                        sitesElement.Add(new XElement(af + "Site",
                            new XAttribute("id", siteId)));

                        XElement siteElement = prodElement.Descendants(af + "Site").Where(x => x.Attribute("id").Value == siteId.ToString()).First();
                        string[] eBusinessArray = row["eBusinessGroups"].ToString().Split(new string[] { "$$" }, StringSplitOptions.None);

                        //Primary eBusiness
                        string primaryEbus = string.Empty;
                        if (eBusinessArray.Count() > 0)
                        {
                            primaryEbus = eBusinessArray[0];
                        }

                        if (string.IsNullOrEmpty(primaryEbus.Trim()))
                        {
                            //If no primary group set, default back to group Ref. 00007
                            primaryEbus = "00007";
                        }

                        siteElement.Add(new XElement(af + "PrimaryGroupReference", primaryEbus));

                        //Published - if active or alert
                        string published = string.Empty;
                        if ((int)row["productStatusFK"] == 1 || (int)row["productStatusFK"] == 8)
                        {
                            published = "true";
                        }
                        else
                        {
                            published = "false";
                        }
                        siteElement.Add(new XElement(af + "Published", published));

                        //Featured
                        string featured = string.Empty;
                        if (row["featured"].ToString() == "True")
                        {
                            featured = "true";
                        }
                        else
                        {
                            featured = "false";
                        }
                        siteElement.Add(new XElement(af + "Featured", featured));

                        //Best Seller
                        string bestSeller = string.Empty;
                        if (row["bestSeller"].ToString() == "True")
                        {
                            bestSeller = "true";
                        }
                        else
                        {
                            bestSeller = "false";
                        }
                        siteElement.Add(new XElement(af + "BestSeller", bestSeller));

                        //Additional Info URL
                        string addInforURL = string.Empty;
                        if (row["additionalInfoURL"].ToString() != "")
                        {
                            siteElement.Add(new XElement(af + "AdditionalInfoURL", row["additionalInfoURL"].ToString()));
                        }

                        BuildWebSpecific(row, siteElement);
                        BuildAdditionalEbusiness(eBusinessArray, siteElement);
                        BuildFeedElement(row, siteElement);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Error building product eBusiness element of xml for partNo - " +
                    _currentPartNo + " Error Message: " + ex.Message);
            }
        }

        private void BuildFeedElement(DataRow row, XElement siteElement)
        {
            if (row["googleFeedInclude"].ToString() == "True")
            {
                GetFeedElement(siteElement, "Google", "true");
            }
            else
            {
                GetFeedElement(siteElement, "Google", "false");
            }

            if (row["bespokeFeedInclude"].ToString() == "True")
            {
                GetFeedElement(siteElement, "Bespoke", "true");
            }
            else
            {
                GetFeedElement(siteElement, "Bespoke", "false");
            }
        }

        private void GetFeedElement(XElement siteElement, string feedName, string include)
        {
            siteElement.Add(new XElement(af + feedName + "Feed",
                new XElement(af + "Include", include),
                new XElement(af + "Category", ""),
                new XElement(af + "AvailabilityInStock", ""),
                new XElement(af + "AvailabilityOutOfStock", ""),
                new XElement(af + "Condition", "")));
        }

        private bool PassEbusinessSection()
        {
            if (PassField("eBusiness")) { return true; }
            if (PassField("bestSeller")) { return true; }
            if (PassField("featured")) { return true; }
            if (PassField("published")) { return true; }
            if (PassField("additionalInfoUrl")) { return true; }
            if (PassField("metaKeywords1")) { return true; }
            if (PassField("metaKeywords2")) { return true; }
            if (PassField("metaKeywords3")) { return true; }
            if (PassField("metaTitle1")) { return true; }
            if (PassField("metaTitle2")) { return true; }
            if (PassField("metaTitle3")) { return true; }
            if (PassField("metaDesc1")) { return true; }
            if (PassField("metaDesc2")) { return true; }
            if (PassField("metaDesc3")) { return true; }
            if (PassField("googleFeedInclude")) { return true; }
            if (PassField("bespokeFeedInclude")) { return true; }
            if (PassField("productStatusFK")) { return true; }
            return false;
        }

        private void BuildAdditionalEbusiness(string[] eBusinessArray, XElement siteElement)
        {
            List<XElement> eBusAdd = new List<XElement>();

            foreach (string eBus in eBusinessArray.Skip(1))
            {
                if (eBus != "")
                    eBusAdd.Add(new XElement(af + "AdditionalGroupReference", eBus));
            }

            //if this element is empty then additional groups will be removed from the product in Axis
            siteElement.Add(new XElement(af + "AdditionalGroups", eBusAdd));
        }

        private void BuildWebSpecific(DataRow row, XElement siteElement)
        {
            string[] webSpecificArray = row["webSpecific"].ToString().Split(new string[] { "$$" }, StringSplitOptions.None);

            foreach (string site in webSpecificArray)
            {
                if (site.Length > 0)
                {
                    string[] itemsArray = site.Split(new string[] { "##" }, StringSplitOptions.None);
                    string website = itemsArray[0];
                    string metaDesc = itemsArray[1];
                    string metaKeywords = itemsArray[2];
                    string metaTitle = itemsArray[3];
                    string language = string.Empty;

                    switch (website)
                    {
                        case "1":
                            language = "en";
                            break;
                        case "2":
                            language = "fr";
                            break;
                        case "3":
                            language = "de";
                            break;
                    }

                    if (PassField("metaTitle" + website))
                    {
                        siteElement.Add(new XElement(af + "MetaTitle",
                        new XAttribute("language", language), metaTitle));
                    }

                    if (PassField("metaKeywords" + website))
                    {
                        siteElement.Add(new XElement(af + "MetaKeywords",
                        new XAttribute("language", language), metaKeywords));
                    }

                    if (PassField("metaDesc" + website))
                    {
                        siteElement.Add(new XElement(af + "MetaDescription",
                        new XAttribute("language", language), metaDesc));
                    }
                }
            }
        }

        private void BuildSuppliersElement(XElement prodElement, DataRow row)
        {
            try
            {
                List<XElement> supplierElements = new List<XElement>();
                string[] suppliersArray = row["suppliers"].ToString().Split(',');

                foreach (string supplier in suppliersArray)
                {
                    if (supplier != "")
                    {
                        string[] supplierDetails = supplier.Split('$');
                        string axisSupplierRef = supplierDetails[0];
                        string providerRef = supplierDetails[1];
                        string providerStock = supplierDetails[2];
                        string providerPrice = supplierDetails[3];

                        supplierElements.Add(new XElement(af + "Supplier",
                            new XAttribute("id", axisSupplierRef),
                                new XElement(af + "SupplierReference", providerRef),
                                new XElement(af + "Price", providerPrice),
                                new XElement(af + "Stock", providerStock)));
                    }
                }

                var pricingField = row["prices"].ToString();

                if (!string.IsNullOrEmpty(pricingField))
                {
                    string[] sitesPricing = pricingField.Remove(pricingField.Length - 2).Split(new string[] { "||" }, StringSplitOptions.None);
                    var defaultPrice = ExtractDefaultPrice(sitesPricing);

                    supplierElements.Add(new XElement(af + "Supplier",
                                new XAttribute("id", "8307"),
                                    new XElement(af + "SupplierReference", row["manufacturerFK"] +
                                        "-" + row["partNo"]),
                                    new XElement(af + "Price", defaultPrice.Split('|')[1]),
                                    new XElement(af + "Stock", "0")));
                }

                prodElement.Add(new XElement(af + "Suppliers", supplierElements));
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Error building product suppliers element of xml for partNo - " +
                    _currentPartNo + " Error Message: " + ex.Message);
            }
        }

        private void BuildComponentsElement(XElement prodElement, DataRow row)
        {
            try
            {
                if (PassField("components") || PassField("productItemTypeFK"))
                {
                    string productItemTypeFK = row["productItemTypeFK"].ToString();

                    if (!string.IsNullOrEmpty(productItemTypeFK) && productItemTypeFK == "2")
                    {
                        string[] componentList = row["components"].ToString().Split(new string[] { "$$" }, StringSplitOptions.None);

                        prodElement.Add(new XElement(af + "Components", ""));

                        foreach (string comp in componentList)
                        {
                            if (comp.Length > 0)
                            {
                                string[] compDetails = comp.Split(new string[] { "##" }, StringSplitOptions.None);
                                string compAltRef = compDetails[0];
                                string compStock = compDetails[1];

                                prodElement.Descendants(af + "Components").First().Add(
                                    new XElement(af + "Component",
                                    new XAttribute("altref", compAltRef),
                                    new XAttribute("quantity", compStock)));
                            }
                        }

                    }
                }

            }
            catch (Exception ex)
            {
                throw new ApplicationException("Error building product components element of xml for partNo - " +
                    _currentPartNo + " Error Message: " + ex.Message);
            }
        }

        private void AddProductToXML(XDocument xml, XElement prodElement)
        {
            var query = xml.Descendants(af + "Products").First();
            query.Add(prodElement);
        }

        private bool PassField(string fieldName)
        {
            if (_isFull || _fieldNames.Contains(fieldName))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void UpdateCompletedDate()
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    db.AXISQueueDetails.Where(x => _axisQueueIds.Contains(x.AXISQueueFK))
                        .ToList()
                        .ForEach(u => u.completedDate = DateTime.Now);
                    db.SaveChanges();
                }

                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Successfully updated completed dates" });
            }
            catch (Exception ex)
            {
                throw new ApplicationException("**ERROR** updating the completed dates. Error Message: " + ex.Message);
            }
        }

        private void ClearAxisQueueRecords()
        {
            try
            {
                int daysOlderThan = 3;
                List<KeyValuePair<string, string>> parms = new List<KeyValuePair<string, string>>();
                parms.Add(new KeyValuePair<string, string>("daysOlderThan", daysOlderThan.ToString()));

                SQLUtilities.ExecuteSimpleStoredProcedure("netgiantmasterdata", "ngmd.ClearAxisQueueRecords", parms);
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Successfully cleared Axis Queue Records" });
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Error clearing the Axis queue. Error Message: " + ex.Message);
            }
        }
    }
}
