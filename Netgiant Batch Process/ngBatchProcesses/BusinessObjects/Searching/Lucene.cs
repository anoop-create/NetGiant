using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using Lucene.Net.Index;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Store;
using Lucene.Net.Documents;
using ngBatchProcesses.BusinessObjects.Shared;

namespace ngBatchProcesses.BusinessObjects.Searching
{
    public class LuceneIndex
    {
        private List<Website> websites;
        private StandardFunctions stdFunc;
        private bool errorOccurred;

        public void CreateLuceneIndexes(Dictionary<string, string> parms)
        {
            // NO LONGER IN USE
            try
            {
            SetupPrerequisites();
            StandardFunctions.WriteProcessStarted();

            foreach (var site in websites)
            {
                //Product
                var products = EntityFunctions.GetProduct(x => x.websiteInventory.Any(y => y.websiteFK == site.WebsiteID) &&
                        (x.productStatusFK == 1 || x.productStatusFK == 8))
                    .OrderBy(x => x.productID)
                    .ToList();
                    IndexWriter writerProducts = SetupWriter(parms["output"] + "Product\\" + site.WebsiteID.ToString() + "\\");
                CreateProducts(products, writerProducts, site);

                //Catgeory
                var categories = GetCategories(site.WebsiteID);
                IndexWriter writerCategories = SetupWriter(parms["output"] + "Categories\\" + site.WebsiteID.ToString() + "\\");
                CreateCategories(categories, writerCategories);
            }

            //Equipment
            var equipment = EntityFunctions.GetEquipmentList(x => x.statusFK == 1)
                        .OrderBy(x => x.eqEquipmentID)
                        .ToList();
                IndexWriter writerEquipment = SetupWriter(parms["output"] + "Equipment\\");
            CreateEquipment(equipment, writerEquipment);

            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
            }
            catch (Exception ex)
            {
                errorOccurred = true;
                StandardFunctions.WriteException(ex);
            }
        }

        private static void CreateEquipment(List<eqEquipment> equipment, IndexWriter writerEquipment)
        {
            foreach (var eq in equipment)
            {
                Document document = new Document();
                document.Add(new Field("EquipID", eq.eqEquipmentID.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));
                document.Add(new Field("EquipName", eq.description.ToLower(), Field.Store.YES, Field.Index.ANALYZED));
                document.Add(new Field("EquipNameSpaced", ReplaceSpecialCharacters(eq.description, " ").ToLower(), Field.Store.YES, Field.Index.ANALYZED));
                document.Add(new Field("EquipNameNoSpaces", eq.description.Replace(" ", "").Replace("-", "").ToLower(), Field.Store.YES, Field.Index.ANALYZED));
                document.Add(new Field("CartridgeTypeID", eq.eqCartridgeTypeFK.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));
                document.Add(new Field("Manufacturer", eq.manufacturer.manufacturerName, Field.Store.YES, Field.Index.NOT_ANALYZED));
                document.Add(new Field("EquipNameFriendly", eq.description, Field.Store.YES, Field.Index.NOT_ANALYZED));
                document.Add(new Field("MetaKeywords", eq.metaKeywords != null ? eq.metaKeywords : "", Field.Store.YES, Field.Index.ANALYZED));
                document.Add(new Field("ImageUrl", eq.thumbnailURL != null ? eq.thumbnailURL : "images/missingEquipment.png", Field.Store.YES, Field.Index.NOT_ANALYZED));
                document.Add(new Field("ProductCount", eq.eqProductMembership.Count.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));
                writerEquipment.AddDocument(document);
            }

            writerEquipment.Optimize();
            writerEquipment.Dispose();
        }

        private static void CreateProducts(List<product> products, IndexWriter writerProducts, Website site)
        {
            var useHTTPS = GetHttpsSetting(site.WebsiteID);
            string vn = EntityFunctions.GetConfigurationSetting("Website Application Variables", "VersionNumber", site.WebsiteID);

            foreach (var prod in products)
            {
                var afa = prod.AxisFields.AxisFieldsAdditional.Where(x => x.websiteFK == site.WebsiteID).FirstOrDefault();

                Document document = new Document();
                document.Add(new Field("ProductID", prod.productID.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));
                document.Add(new Field("AxisID", prod.AxisFields.stockReference != null ? prod.AxisFields.stockReference : "", Field.Store.YES, Field.Index.NOT_ANALYZED));
                document.Add(new Field("PartNo", prod.partNo.ToLower(), Field.Store.YES, Field.Index.ANALYZED));
                document.Add(new Field("PartSpaced", ReplaceSpecialCharacters(prod.partNo, " ").ToLower(), Field.Store.YES, Field.Index.ANALYZED));
                document.Add(new Field("PartNoNoSpaces", ReplaceSpecialCharacters(prod.partNo, "").Replace(" ", "").ToLower(), Field.Store.YES, Field.Index.ANALYZED));
                document.Add(new Field("ProductName", prod.productName.ToLower(), Field.Store.YES, Field.Index.ANALYZED));
                document.Add(new Field("ProductNameSpaced", ReplaceSpecialCharacters(prod.productName, " ").ToLower(), Field.Store.YES, Field.Index.ANALYZED));
                document.Add(new Field("ProductNameNoSpaces", ReplaceSpecialCharacters(prod.productName, "").Replace(" ", "").ToLower(), Field.Store.YES, Field.Index.ANALYZED));
                document.Add(new Field("ProductNameFriendly", prod.productName, Field.Store.YES, Field.Index.NOT_ANALYZED));
                document.Add(new Field("PartNoFriendly", prod.partNo, Field.Store.YES, Field.Index.NOT_ANALYZED));
                document.Add(new Field("MetaKeywords", afa != null ? afa.metaKeywords != null ? afa.metaKeywords : "" : "", Field.Store.YES, Field.Index.ANALYZED));

                string productImage = ProductFunctions.GetProductImage(site, prod, vn, "S");

                document.Add(new Field("ProductImage", productImage, Field.Store.YES, Field.Index.NOT_ANALYZED));

                writerProducts.AddDocument(document);
            }

            writerProducts.Optimize();
            writerProducts.Dispose();
        }

        private void CreateCategories(List<categoryCode> categories, IndexWriter writerCategories)
        {
            foreach (var category in categories)
            {
                if (category.AXISGroupNo != null)
                {
                    Document document = new Document();
                    document.Add(new Field("CategoryName", category.categoryCodeName, Field.Store.YES, Field.Index.ANALYZED));
                    document.Add(new Field("AxisCode", category.AXISGroupNo, Field.Store.YES, Field.Index.NOT_ANALYZED));
                    writerCategories.AddDocument(document);
                }
            }

            writerCategories.Optimize();
            writerCategories.Dispose();
        }

        private IndexWriter SetupWriter(string path)
        {
            return new IndexWriter(FSDirectory.Open(path), new
                    StandardAnalyzer(global::Lucene.Net.Util.Version.LUCENE_30), true,
                    IndexWriter.MaxFieldLength.LIMITED);
        }

        private void SetupPrerequisites()
        {
            stdFunc = new StandardFunctions();
            websites = EntityFunctions.GetWebsiteList(x => true);
        }

        private List<categoryCode> GetCategories(int websiteFK)
        {
            List<categoryCode> dbCatgeories = null;
            var finalCategories = new List<categoryCode>();

            dbCatgeories = EntityFunctions.GetCategoryCodeList(x => x.websiteFK == websiteFK && !x.categoryCodeName.Contains("_OLD"));

            foreach (var code in dbCatgeories)
            {
                if (CategoryHasProducts(code))
                    if (!finalCategories.Select(x => x.categoryCodeName).Contains(code.categoryCodeName))
                        finalCategories.Add(code);
            }

            return finalCategories;
        }

        private static bool GetHttpsSetting(int websiteID)
        {
            return Convert.ToBoolean(EntityFunctions.GetConfigurationSetting("Website Application Variables", "UseHTTPS", websiteID));
        }

        private static string ReplaceSpecialCharacters(string text, string replaceWith)
        {
            return text.Replace("-", replaceWith).Replace("+", replaceWith).Replace("(", replaceWith)
                .Replace(")", replaceWith).Replace("[", replaceWith).Replace("]", replaceWith).Replace("/", replaceWith)
                .Replace("\\", replaceWith).Replace(".", replaceWith);
        }

        public static bool CategoryHasProducts(categoryCode cc)
        {
            bool returnValue = false;
            var cnt1 = cc.websiteInventory.Where(x => x.product.productStatusFK == 1).Count();
            if (cnt1 > 0)
            {
                returnValue = true;
            }
            var cnt2 = cc.secondaryCategoryLookup.Where(x => x.websiteInventory.product.productStatusFK == 1).Count();
            if (cnt2 > 0)
            {
                returnValue = true;
            }

            return returnValue;
        }
    }
}
