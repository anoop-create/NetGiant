using netGiant.Intranet.DataLayer.NetgiantMasterData;
using ngBatchProcesses.BusinessObjects.Shared;
using NGBP.DataAccessLayer.DataUtilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;

namespace ngBatchProcesses.BusinessObjects.DataFeeds
{
    class SLIFeed
    {
        public static List<AxisValueLookup> Att6 { get; set; }
        public static string Version { get; set; }

        public static void CreateSliFeedXml(Dictionary<string, string> parms)
        {
            StandardFunctions.WriteProcessStarted();
            Properties.Settings settings = Properties.Settings.Default;
            bool errorHasOccurred = false;

            string fileName = parms["output"];
            int websiteid = Int32.Parse(parms["websiteid"]);
            Website w = EntityFunctions.GetWebsiteList(x => x.WebsiteID == websiteid).FirstOrDefault();
            string tempFileName = fileName.Replace(".", "-temp.");
            string directory = StandardFunctions.GetMachineConfigAppSetting("LocalDirectory");
            Version = EntityFunctions.GetConfigurationSetting("Website Application Variables", "VersionNumber",
                websiteid);

            using (XmlWriter xmlwriter = XmlWriter.Create(tempFileName))
            {
                try
                {
                    Att6 = EntityFunctions.GetAxisValueLookup(x => x.axisTypeNameFK == 1 && x.attrNameFK == 6);

                    xmlwriter.WriteStartDocument();

                    xmlwriter.WriteStartElement("website");
                    xmlwriter.WriteStartAttribute("id");
                    xmlwriter.WriteValue(w.WebsiteID);
                    xmlwriter.WriteEndAttribute();

                    xmlwriter.WriteStartElement("prodlist");
                    AddProductList(w, xmlwriter);
                    xmlwriter.WriteEndElement();

                    xmlwriter.WriteStartElement("equipmentlist");
                    AddEquipmentList(w, Version, xmlwriter);
                    xmlwriter.WriteEndElement();

                    xmlwriter.WriteStartElement("categorylist");
                    AddCategoryList(w, Version,  xmlwriter);
                    xmlwriter.WriteEndElement();

                    xmlwriter.WriteEndElement();

                    xmlwriter.WriteEndDocument();
                }
                catch
                {
                    errorHasOccurred = true;
                }
            }

            if (!errorHasOccurred)
            {
                errorHasOccurred = true;
                if (File.Exists(tempFileName))
                {
                    if (File.Exists(fileName))
                    {
                        File.Delete(fileName);
                    }
                    File.Move(tempFileName, fileName);
                    errorHasOccurred = false;
                }

                if (!errorHasOccurred)
                {
                    //FTP output file
                    if (parms.ContainsKey("ftpsite"))
                    {
                        string finalFileName = parms["filea"];

                        try
                        {
                            Tuple<bool, string> rtn = FtpUtilities.UploadFTPFile(fileName,
                             parms["ftpsite"],
                             parms["ftpusername"],
                             parms["ftppassword"],
                             "/" + parms["ftppath"] + finalFileName,
                             false);
                            if (rtn.Item1)
                            {
                                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "FTP Successful" });
                            }
                            else
                            {
                                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR Attempting to FTP File: " + fileName, ErrorCode = "ERROR" });
                            }
                        }
                        catch (Exception ex)
                        {
                            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR Attempting to FTP File: " + parms["output"] + ": " + ex.Message, ErrorCode = "ERROR" });
                            StandardFunctions.WriteException(ex);
                        }
                    }
                }
            }

            //Log in activity log
            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
        }

        public static void AddProductList(Website w, XmlWriter xmlwriter)
        {
            // Retrieve data
            List<websiteInventory> lwi = EntityFunctions.GetWebsiteInventoryList(x => x.websiteFK == w.WebsiteID && (x.product.productStatusFK == 1 || x.product.productStatusFK == 6 || x.product.productStatusFK == 8));

            foreach (websiteInventory wi in lwi)
            {
                string imageUrl;
                try
                {
                    imageUrl = ProductFunctions.GetProductImage(
                        w, wi.product, Version, "M");
                }
                catch
                {
                    // log and skip/ignore this product
                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR partNo: " + wi.product.partNo + ", could not be added to the Product List", ErrorCode = "ERROR" });
                    continue;
                }

                if (imageUrl.StartsWith("/"))
                {
                    imageUrl = "https://" + w.WebURL + "/" + Version + "/cdn" + imageUrl;
                }

                var additional = wi.product.AxisFields.AxisFieldsAdditional.Where(x => x.websiteFK == w.WebsiteID).Select(x => x.metaKeywords).FirstOrDefault();

                double price = wi.productPrice.OrderByDescending(x => x.dateLastUpdated).FirstOrDefault() == null ? 999 : wi.productPrice.OrderByDescending(x => x.dateLastUpdated).FirstOrDefault().priceIncVat ?? 999;
                xmlwriter.WriteStartElement("product");
                xmlwriter.WriteStartAttribute("desc");
                xmlwriter.WriteValue(wi.product.productName);
                xmlwriter.WriteEndAttribute();
                xmlwriter.WriteElementString("id", wi.productFK.ToString());
                xmlwriter.WriteElementString("name", wi.product.productName);
                xmlwriter.WriteElementString("shortdescription", wi.product.AxisFields.spec1);
                xmlwriter.WriteElementString("brand", wi.product.manufacturer.manufacturerName);
                xmlwriter.WriteElementString("mfpn", wi.product.partNo);
                xmlwriter.WriteElementString("availability", wi.product.supplierStock > 0 ? "1" : "0");
                xmlwriter.WriteElementString("price", price.ToString("N2"));
                xmlwriter.WriteElementString("url", "https://" + w.WebURL + "/product/" + CleanUrl(wi.product.productName + "-" + wi.product.partNo) + "-" + wi.product.AxisFields.stockReference + "/");
                xmlwriter.WriteElementString("imageurl", imageUrl);
                xmlwriter.WriteElementString("keyword", additional);
                if (wi.product.crossSellingLink.Count > 0)
                {
                    List<crossSellingLink> lcsl = wi.product.crossSellingLink.OrderBy(x => x.crossSellingLinkTypeFK).ToList();
                    xmlwriter.WriteElementString("primaryxsellproductid", lcsl.First().bProductFK.ToString());
                }
                else
                {
                    if (wi.product.crossSellingLink1.Count > 0)
                    {
                        List<crossSellingLink> lcsl = wi.product.crossSellingLink1.OrderBy(x => x.crossSellingLinkTypeFK).ToList();
                        xmlwriter.WriteElementString("primaryxsellproductid", lcsl.First().bProductFK.ToString());
                    }
                    else
                    {
                        xmlwriter.WriteElementString("primaryxsellproductid", "");
                    }
                }
                // Other Attributes
                string manuName = FixManufacturer(wi.product.manufacturer.manufacturerName);
                //or_products orp = StandardFunctions.GetOrAttribute(x =>
                //    x.partno == wi.product.partNo && x.manufacturer == manuName).FirstOrDefault();
                List<Searchable> atts = EntityFunctions.GetAttribute(wi.product.manufacturer.manufacturerName, wi.product.partNo);
                //if (orp != null)
                if (atts.Count > 0)
                {
                    Searchable s;
                    s = atts.FirstOrDefault(x => x.Name.Contains("Colour"));
                    if (s != null)
                    {
                        xmlwriter.WriteElementString("colour", s.Value);
                    }
                    s = atts.FirstOrDefault(x => x.Name == "Product Type");
                    if (s != null)
                    {
                        xmlwriter.WriteElementString("producttype", s.Value);
                    }
                    s = atts.FirstOrDefault(x => x.Name == "Form Factor");
                    if (s != null)
                    {
                        xmlwriter.WriteElementString("formfactor", s.Value);
                    }
                    s = atts.FirstOrDefault(x => x.Name == "Material");
                    if (s != null)
                    {
                        xmlwriter.WriteElementString("material", s.Value);
                    }
                    s = atts.FirstOrDefault(x => x.Name == "Storage Capacity");
                    if (s != null)
                    {
                        xmlwriter.WriteElementString("capacity", s.Value);
                    }
                    s = atts.FirstOrDefault(x => x.Name.Contains("Size"));
                    if (s != null)
                    {
                        xmlwriter.WriteElementString("size", s.Value);
                    }
                }

                AxisValueLookup promo = Att6.Find(x => x.attrValueID == wi.product.AxisFields.attr6);
                if (promo != null)
                {
                    xmlwriter.WriteElementString("promotion", promo.attrValueDesc);
                }
                xmlwriter.WriteElementString("weighting", "1");  // <== Future use

                // Equipment Mappings
                xmlwriter.WriteStartElement("equipmentmappings");
                List<eqProductMembership> lpm = EntityFunctions.GetProductMembershipList(x => x.productFK == wi.product.productID).ToList();
                foreach (eqProductMembership pm in lpm)
                {
                    xmlwriter.WriteElementString("equipmentid", pm.eqEquipmentFK.ToString());
                }
                xmlwriter.WriteEndElement();

                // Category Mappings
                xmlwriter.WriteStartElement("categorymappings");
                xmlwriter.WriteElementString("categoryid", wi.categoryCodeFK.ToString());
                List<secondaryCategoryLookup> lsc = EntityFunctions.GetSecondaryCategoryList(x => x.websiteInventoryFK == wi.websiteInventoryID).ToList();
                foreach (secondaryCategoryLookup sc in lsc)
                {
                    xmlwriter.WriteElementString("categoryid", sc.categoryCodeFK.ToString());
                }
                xmlwriter.WriteEndElement();

                xmlwriter.WriteEndElement();
            }
        }

        public static void AddEquipmentList(Website w, string vn, XmlWriter xmlwriter)
        {
            // Retrieve data
            List<eqEquipment> leq = EntityFunctions.GetEquipmentList(x => x.statusFK == 1);
            List<Lookup> lct = EntityFunctions.GetNgmdLookup(x => x.LookupType.LookupTypeName == "CartridgeType");

            foreach (eqEquipment eq in leq)
            {
                string tn = ProductFunctions.GenerateImageURL(w, eq.thumbnailURL ?? "", vn, "S");
                xmlwriter.WriteStartElement("equipment");
                xmlwriter.WriteStartAttribute("desc");
                xmlwriter.WriteValue(eq.description);
                xmlwriter.WriteEndAttribute();
                xmlwriter.WriteElementString("id", eq.eqEquipmentID.ToString());
                xmlwriter.WriteElementString("name", eq.description);
                xmlwriter.WriteElementString("shortdescription", eq.description);
                xmlwriter.WriteElementString("fulldescription", eq.description);
                xmlwriter.WriteElementString("url", "https://" + w.WebURL + "/model/" + CleanUrl(eq.description) + "-" +
                    lct.Find(x => x.AltLookupId == eq.eqCartridgeTypeFK).LookupName.ToLower().Replace(" ", "-").Replace("toner-range", "toner-cartridges").Replace("ink-range", "ink-cartridges") + "/");

                xmlwriter.WriteElementString("imageurl", tn);
                xmlwriter.WriteElementString("weighting", "1");  // <== Future use
                xmlwriter.WriteElementString("mfpn", eq.metaKeywords);

                // Product Mappings
                xmlwriter.WriteStartElement("productmappings");
                List<eqProductMembership> lpm = EntityFunctions.GetProductMembershipList(x => x.eqEquipmentFK == eq.eqEquipmentID).ToList();
                foreach (eqProductMembership pm in lpm)
                {
                    if (pm.product.websiteInventory.FirstOrDefault(x => x.websiteFK == w.WebsiteID) != null)
                    {
                        xmlwriter.WriteElementString("productid", pm.product.websiteInventory.FirstOrDefault(x => x.websiteFK == w.WebsiteID).websiteInventoryID.ToString());
                    }
                }
                xmlwriter.WriteEndElement();

                xmlwriter.WriteEndElement();
            }
        }

        public static void AddCategoryList(Website w, string vn, XmlWriter xmlwriter)
        {
            // Retrieve data
            List<categoryCode> lcc = EntityFunctions.GetCategoryCodeList(x => x.websiteFK == w.WebsiteID && x.websiteInventory.Count > 0);

            foreach (categoryCode cc in lcc)
            {
                string tn = ProductFunctions.GenerateImageURL(w, "Images/category-icons/" + CleanUrl(cc.categoryCodeName.ToLower()) + ".jpg", vn, "S");
                xmlwriter.WriteStartElement("category");
                xmlwriter.WriteStartAttribute("desc");
                xmlwriter.WriteValue(cc.categoryCodeName);
                xmlwriter.WriteEndAttribute();
                xmlwriter.WriteElementString("id", cc.categoryCodeID.ToString());
                xmlwriter.WriteElementString("name", cc.categoryCodeName);
                xmlwriter.WriteElementString("shortdescription", cc.categoryCodeName);
                xmlwriter.WriteElementString("fulldescription", cc.categoryCodeName);
                xmlwriter.WriteElementString("url", "https://" + w.WebURL + "/products/" + CleanUrl(cc.categoryCodeName) + "-" + (cc.AXISGroupNo?.Trim() ?? "") + "/");
                xmlwriter.WriteElementString("imageurl", tn);
                xmlwriter.WriteElementString("weighting", "1");  // <== Future use

                // Product Mappings
                xmlwriter.WriteStartElement("productmappings");
                List<websiteInventory> lwi = EntityFunctions.GetCategoryMembershipList(x => x.categoryCodeFK == cc.categoryCodeID).ToList();
                foreach (websiteInventory wi in lwi)
                {
                    xmlwriter.WriteElementString("productid", wi.websiteInventoryID.ToString());
                }
                xmlwriter.WriteEndElement();

                xmlwriter.WriteEndElement();
            }
        }

        public static string CleanUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return "";

            var newUrl = Regex.Replace(url, @"[\,\(\)\[\]']", "");
            newUrl = Regex.Replace(newUrl, @"[\/\s\+\.\&]", "-");
            newUrl = newUrl.Replace("&amp;", "-");
            newUrl = Regex.Replace(newUrl, @"\-+", "-");
            return newUrl;
        }

        public static string FixManufacturer(string manuName)
        {
            return manuName;
        }

    }
}
