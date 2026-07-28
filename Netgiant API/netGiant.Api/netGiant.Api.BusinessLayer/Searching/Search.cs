using netGiant.Api.BusinessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Documents;
using System.IO;
using System.Runtime.Serialization;
using System.Diagnostics;
using netGiant.Intranet.DataLayer;

namespace netGiant.Api.BusinessLayer.Searching
{
    public class Search
    {
        public Search()
        {
            SearchModel = new ItemSearch();
        }

        public enum SearchType
        {
            Quick,
            Full
        }

        public ItemSearch SearchModel { get; set; }
        private string _indexPath;
        private string _searchTerm;
        private int _websiteID;
        private List<manufacturer> _equipManuList;

        private SearchType _searchType;

        public Search SearchProductAndEquipment(string indexPath,
            string searchTerm,
            int searchTypeID,
            int websiteID,
            object equipManuList)
        {
            SetupPrerequsites(indexPath,
                searchTerm,
                searchTypeID,
                websiteID,
                equipManuList);

            GetProductMatches();

            if (websiteID != 3)
                GetEquipmentMatches();

            if (websiteID == 3)
                GetCategoryMatches();

            FilterByRank();
            SetupIDs();

            return this;
        }

        private void GetProductMatches()
        {
            string[] searchableFields = new string[] { "ProductNameSpaced", "ProductNameNoSpaces", "PartSpaced" };

            var finalQuery = new BooleanQuery();
            var parser = new MultiFieldQueryParser(Lucene.Net.Util.Version.LUCENE_30, searchableFields,
                new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30));

            List<string> terms = _searchTerm.ToLower().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries).ToList();
            terms = CheckForBrandNames(terms);

            foreach (string term in terms)
            {
                var validTerm = RemoveInvalidCharacters(term);

                if (validTerm.Length > 0)
                {
                    var wildQuery = new WildcardQuery(new Term("ProductNameSpaced", "*" + validTerm + "*"));

                    if (_equipManuList.Select(x => x.equipmentManuName.ToLower()).Contains(validTerm))
                    {
                        finalQuery.Add(wildQuery, Occur.MUST);
                    }
                    else
                    {
                        finalQuery.Add(wildQuery, Occur.SHOULD);
                    }

                    var wildQuery2 = new WildcardQuery(new Term("ProductName", "*" + validTerm + "*"));
                    finalQuery.Add(wildQuery2, Occur.SHOULD);

                    var wildQuery3 = new WildcardQuery(new Term("ProductNameNoSpaces", "*" + validTerm + "*"));
                    finalQuery.Add(wildQuery3, Occur.SHOULD);

                    var wildQuery4 = new WildcardQuery(new Term("PartNo", "*" + validTerm + "*"));
                    finalQuery.Add(wildQuery4, Occur.SHOULD);

                    var wildQuery5 = new WildcardQuery(new Term("MetaKeywords", "*" + validTerm + "*"));
                    finalQuery.Add(wildQuery5, Occur.SHOULD);

                    finalQuery.Add(parser.Parse(validTerm.Replace("~", "") + "~"), Occur.SHOULD);
                }
            }

            var directoryProduct = FSDirectory.Open(new DirectoryInfo(_indexPath + "Product//" + _websiteID));
            var searcherProduct = new IndexSearcher(directoryProduct, true);
            searcherProduct.SetDefaultFieldSortScoring(true, true);

            var maxResults = 200;

            var hitsProduct = searcherProduct.Search(finalQuery, null, maxResults, Sort.RELEVANCE);

            foreach (ScoreDoc scoreDoc in hitsProduct.ScoreDocs)
            {
                Lucene.Net.Documents.Document doc = searcherProduct.Doc(scoreDoc.Doc);
                string id = doc.Get("ProductID");
                string axisID = doc.Get("AxisID");
                string part = doc.Get("PartNoFriendly");
                string name = doc.Get("ProductNameFriendly");
                string productImage = doc.Get("ProductImage");

                SearchModel.Products.Add(new ItemSearch.ProductSearch
                {
                    ID = id,
                    AxisID = axisID,
                    PartNo = part,
                    ProductName = name,
                    ProductImage = productImage,
                    LuceneRank = scoreDoc.Score.ToString()
                });
            }
        }

        private void GetEquipmentMatches()
        {
            string[] searchableFieldsEquipment = new string[] { "EquipNameSpaced" };
            var finalQueryEquipment = new BooleanQuery();
            var parserEquipment = new MultiFieldQueryParser(Lucene.Net.Util.Version.LUCENE_30, searchableFieldsEquipment,
                new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30));

            List<string> terms = _searchTerm.ToLower().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries).ToList();
            terms = CheckForBrandNames(terms);

            foreach (string term in terms)
            {
                var validTerm = RemoveInvalidCharacters(term, true);

                if (validTerm.Length > 0)
                {
                    var wildQuery = new WildcardQuery(new Term("EquipNameSpaced", "*" + validTerm + "*"));

                    if (_equipManuList.Select(x => x.equipmentManuName.ToLower()).Contains(validTerm))
                    {
                        finalQueryEquipment.Add(wildQuery, Occur.MUST);
                    }
                    else
                    {
                        finalQueryEquipment.Add(wildQuery, Occur.SHOULD);
                    }

                    var wildQuery2 = new WildcardQuery(new Term("EquipName", "*" + validTerm + "*"));
                    finalQueryEquipment.Add(wildQuery2, Occur.SHOULD);

                    var wildQuery3 = new WildcardQuery(new Term("MetaKeywords", "*" + validTerm + "*"));
                    finalQueryEquipment.Add(wildQuery3, Occur.SHOULD);

                    var wildQuery4 = new WildcardQuery(new Term("EquipNameNoSpaces", "*" + validTerm + "*"));
                    finalQueryEquipment.Add(wildQuery4, Occur.SHOULD);

                    finalQueryEquipment.Add(parserEquipment.Parse(validTerm.Replace("~", "") + "~"), Occur.SHOULD);
                }
            }

            var directoryEquipment = FSDirectory.Open(new DirectoryInfo(_indexPath + "Equipment"));
            var searcherEquipment = new IndexSearcher(directoryEquipment, true);
            searcherEquipment.SetDefaultFieldSortScoring(true, true);

            var maxResults = 30;

            var hitsEquipment = searcherEquipment.Search(finalQueryEquipment, null, maxResults, Sort.RELEVANCE);

            foreach (ScoreDoc scoreDoc in hitsEquipment.ScoreDocs)
            {
                Lucene.Net.Documents.Document doc = searcherEquipment.Doc(scoreDoc.Doc);
                string equipID = doc.Get("EquipID");
                string equipName = doc.Get("EquipNameFriendly");
                string cartridgeTypeID = doc.Get("CartridgeTypeID");
                string manufacturer = doc.Get("Manufacturer");
                string thumbnailUrl = doc.Get("ImageUrl");
                string productCount = doc.Get("ProductCount");

                SearchModel.Equipment.Add(new ItemSearch.EquipmentSearch
                {
                    ID = equipID,
                    EquipmentName = equipName,
                    CartridgeTypeID = cartridgeTypeID,
                    Manufacturer = manufacturer,
                    ThumbnailUrl = thumbnailUrl,
                    ProductCount = productCount,
                    LuceneRank = scoreDoc.Score.ToString()
                });
            }
        }

        private void GetCategoryMatches()
        {
            var finalQuery = new BooleanQuery();
            List<string> terms = _searchTerm.ToLower().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries).ToList();
            foreach (string term in terms)
            {
                Term t = new Term("CategoryName", term);
                FuzzyQuery query = new FuzzyQuery(t, (float)0.6);

                var validTerm = RemoveInvalidCharacters(term, true);
                finalQuery.Add(query, Occur.SHOULD);
            }

            var directoryCategory = FSDirectory.Open(new DirectoryInfo(_indexPath + "Categories\\" + _websiteID));
            var searcherCategory = new IndexSearcher(directoryCategory, true);
            var hitsCategories = searcherCategory.Search(finalQuery, null, 12, Sort.RELEVANCE);

            foreach (ScoreDoc scoreDoc in hitsCategories.ScoreDocs)
            {
                Document doc = searcherCategory.Doc(scoreDoc.Doc);
                string categoryName = doc.Get("CategoryName");
                string axisCode = doc.Get("AxisCode");

                SearchModel.Categories.Add(new ItemSearch.CategorySearch
                {
                    CategoryName = categoryName,
                    AxisCode = axisCode
                });
            }
        }

        private void SetupIDs()
        {
            string equipIDs = "";

            foreach (var id in SearchModel.Equipment.Select(x => x.ID))
            {
                if (id != null)
                    equipIDs += id.ToString() + ",";
            }

            string prodIDs = "";

            foreach (var id in SearchModel.Products.Select(x => x.ID))
            {
                if (id != null)
                    prodIDs += id.ToString() + ",";
            }

            if (prodIDs.Length > 0)
                SearchModel.ProductIDs = prodIDs.Remove(prodIDs.Length - 1);

            if (equipIDs.Length > 0)
                SearchModel.EquipmentIDs = equipIDs.Remove(equipIDs.Length - 1);
        }

        private void SetupPrerequsites(string indexPath,
            string searchTerm,
            int searchTypeID,
            int websiteID,
            object equipManulist)
        {
            _indexPath = indexPath;
            _searchTerm = searchTerm ?? "";
            _websiteID = websiteID;

            switch (searchTypeID)
            {
                case 1:
                    _searchType = SearchType.Quick;
                    break;
                case 2:
                    _searchType = SearchType.Full;
                    break;
                default:
                    _searchType = SearchType.Quick;
                    break;
            }

            if (equipManulist != null)
            {
                _equipManuList = (List<manufacturer>)equipManulist;
            }
            else
            {
                _equipManuList = new List<manufacturer>();
            }
        }

        private string RemoveInvalidCharacters(string term, bool replaceDash = false)
        {
            term = term.Replace(")", "").Replace("(", "").Replace("[", "")
                .Replace("]", "").Replace("*", "").Replace("&", "").Replace("|", "").Replace(":", "")
                .Replace("!", "").Replace("{", "").Replace("}", "").Replace("^", "").Replace("?", "")
                .Replace(":", "").Replace("\"", "");

            if (replaceDash && term.Length > 1)
                term = term.Replace("-", " ");

            if (term.Length == 1)
                term = term.Replace("-", "");

            return term;
        }

        private List<string> CheckForBrandNames(List<string> terms)
        {
            var newTerms = terms;

            try
            {
                var exitLoop = false;

                for (int i = 0; i < newTerms.Count; i++)
                {
                    if (!newTerms[i].Contains(" "))
                    {
                        foreach (var manu in _equipManuList)
                        {
                            if (newTerms[i].Length > manu.equipmentManuName.Length)
                            {
                                if (newTerms[i].Substring(0, manu.equipmentManuName.Length) == manu.equipmentManuName.ToLower())
                                {
                                    newTerms.Add(newTerms[i].Substring(0, manu.equipmentManuName.Length));
                                    newTerms.Add(newTerms[i].Substring(manu.equipmentManuName.Length, (newTerms[i].Length) - manu.equipmentManuName.Length));
                                    newTerms.Remove(newTerms[i]);
                                    exitLoop = true;
                                }
                            }

                            if (exitLoop)
                                break;
                        }
                    }

                    if (exitLoop)
                        break;
                }
            }
            catch (Exception)
            {
                newTerms = terms;
            }

            return newTerms;
        }

        private void FilterByRank()
        {
            try
            {
                var sumMatchRank = SearchModel.Equipment.Sum(x => Convert.ToDecimal(x.LuceneRank));
                sumMatchRank += SearchModel.Products.Sum(x => Convert.ToDecimal(x.LuceneRank));

                var matchCount = SearchModel.Equipment.Count + SearchModel.Products.Count;
                var matchAverage = sumMatchRank / matchCount;

                SearchModel.Equipment.RemoveAll(x => Convert.ToDecimal(x.LuceneRank) < matchAverage);
                //SearchModel.Products.RemoveAll(x => Convert.ToDecimal(x.LuceneRank) < matchAverage);

                if (_searchType == SearchType.Quick)
                {
                    SearchModel.Equipment = SearchModel.Equipment.Take(6).ToList();
                    SearchModel.Products = SearchModel.Products.Take(6).ToList();
                }
            }
            catch (Exception)
            {
                //Prevent error
            }
        }
    }
}
