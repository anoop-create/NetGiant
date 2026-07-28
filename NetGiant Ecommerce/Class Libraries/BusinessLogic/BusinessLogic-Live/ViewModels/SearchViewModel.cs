using DataAccess.Utilities;
using Nest;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using DataAccess.EntityFramework;
using VMerchantWrapper.Entities;
using System.Xml.Linq;

namespace BusinessLogic.ViewModels
{
    public class SearchViewModel : ProductViewModel
    {
        public SearchViewModel()
        {
            Results = new List<SearchEntry>();
            node = new Uri(ConfigurationManager.AppSettings["ElasticSearchUri"]);
            _equipManuList = DataCache.GetManufacturers("").Select(x => x.Text).ToList();

            if (ConfigurationManager.AppSettings["SearchApplication"] == "SLI")
            {
                ResultsUrl = "https://" + ConfigurationManager.AppSettings["SLIDomain"] + "/search?";
            }
            if (ConfigurationManager.AppSettings["SearchApplication"] == "Elastic")
            {
                settings = new ConnectionSettings(node);
                client = new ElasticClient(settings);
                settings.DefaultIndex(CommonDataLookup("SearchIndexName"));
            }
        }

        public EquipmentEntry Equipment { get; set; }
        public CategoryEntry Category { get; set; }
        public string ResultsUrl { get; set; }
        public string SliLoggingUrl { get; set; }
        public List<SearchEntry> Results { get; set; }
        public List<ProductEntry> Products { get; set; }
        public string CategoryRestriction { get; set; }
        public string JumpUrl { get; set; }
        public SLIBanner FilterTopBanner { get; set; }
        public SLIBanner ResultsTopBanner { get; set; }

        public string SearchTerm
        {
            get
            {
                return _searchTerm;
            }
            set
            {
                _searchTerm = value.FormatForIndex();
            }
        }

        private Uri node;
        private ConnectionSettings settings;
        private ElasticClient client;
        private string _searchTerm;
        private IReadOnlyCollection<IHit<SearchEntry>> _elasticResults;
        private List<string> _equipManuList;

        public void GetResults(int takeTop = 100, bool isQuickSearch = false)
        {
            if (ConfigurationManager.AppSettings["SearchApplication"] == "SLI")
            {
                GetResultsSli(takeTop, isQuickSearch);
            }
            else
            {
                GetResultsElastic(takeTop, isQuickSearch);
            }
        }

        private void GetResultsSli(int takeTop, bool isQuickSearch)
        {
            // SLI Search
            if (isQuickSearch) return;

            string ip = ConfigurationManager.AppSettings["Environment"] == "Live"
                ? HttpContext.Current.Request.Headers["X-Real-IP"]
                : HttpContext.Current.Request.UserHostAddress;
            string uuid = HttpContext.Current.Request.Cookies["SLIBeacon"] == null ? Guid.NewGuid().ToString() : HttpContext.Current.Request.Cookies["SLIBeacon"].Value;

            string url = ResultsUrl + "w=" + HttpUtility.UrlEncode(SearchTerm) + "&cnt=" + takeTop + "&cip=" + ip + "&ua=" + HttpUtility.UrlEncode(HttpContext.Current.Request.UserAgent) + "&uid=" + uuid;
            if (CategoryRestriction != null)
            {
                url += "&af=cat:" + CategoryRestriction;
            }

            JObject results = new JObject();
            List<string> products = new List<string>();
            List<string> equips = new List<string>();

            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            int attempt = 0;

            do
            {
                attempt++;

                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

                    WebResponse response = request.GetResponse();
                    using (Stream responseStream = response.GetResponseStream())
                    {
                        StreamReader reader = new StreamReader(responseStream, Encoding.UTF8);
                        results = JObject.Parse(reader.ReadToEnd());
                    }

                    break;
                }
                catch (WebException ex)
                {
                    if (attempt > 1)
                    {
                        Utilities.ProcessException(ex, url);
                    }
                }
            }
            while (attempt < 2);


            if(results["merch"] != null)
            {
                if (results["merch"]["jumpurl"] != null)
                {
                    var logUrl = Convert.ToString(results["merch"]["logURL"]);
                    var jumpRequest = (HttpWebRequest)WebRequest.Create(logUrl);
                    jumpRequest.GetResponse();

                    JumpUrl = Convert.ToString(results["merch"]["jumpurl"]);

                    return;
                }

                if(results["merch"]["banners"] != null)
                {
                    for (int j = 0; j < results["merch"]["banners"].Count(); j++)
                    {

                        var content = Convert.ToString(results["merch"]["banners"][j]["content"]);
                        XElement e = XElement.Parse(content);

                        if (!String.IsNullOrEmpty((string)e.Attribute("id")))
                        {

                            var banner = new SLIBanner
                            {
                                Name = Convert.ToString(results["merch"]["banners"][j]["name"]),
                                Placement = (string)e.Attribute("id"),
                                Type = Convert.ToString(results["merch"]["banners"][j]["type"]),
                                Content = content
                            };

                            if (banner.Placement == "sli_banner_filter_top")
                            {
                                FilterTopBanner = banner;
                            }

                            if (banner.Placement == "sli_banner_results_top")
                            {
                                ResultsTopBanner = banner;
                            }
                        }
                    }
                }
            }

            JumpUrl = null;

            int i = 0;
            if (results["results"] != null)
            {
                foreach (JObject jo in results["results"])
                {
                    if (jo["type"] != null)
                    {
                        if (jo["type"].ToString() == "1")
                        {
                            products.Add(jo["ID"].ToString());
                            Results.Add(new SearchEntry()
                            {
                                Model = "",
                                Description = jo["title"].ToString(),
                                CrossSellModel = "",
                                CrossSellDescription = "",
                                CrossSellManufacturer = "",
                                FriendlyModel = "",
                                FriendlyDescription = jo["title"].ToString(),
                                ItemType = "Product",
                                ItemId = Int32.Parse(jo["ID"].ToString()),
                                ImageUrl = jo["imageurl"].ToString(),
                                UrlLink = null,
                                CartridgeType = null,
                                ManufacturerName = "",
                                ManufacturerId = 0,
                                ProductCount = i,
                                ProductUrl = jo["url"].ToString(),
                                SliLoggingUrl = jo["logURL"].ToString(),
                                ProductType = "",
                                MetaKeywords = "",
                                Product = null
                            });
                        }
                        if (jo["type"].ToString() == "2")
                        {
                            products.Add(jo["ID"].ToString());
                            string imageurl = jo["imageurl"].ToString();
                            if (imageurl.StartsWith("https://"))
                            {
                                int j = ("http:" + ConfigurationManager.AppSettings["CDN"]).Length + 1;
                                imageurl = imageurl.Substring(j);
                            }
                            Results.Add(new SearchEntry()
                            {
                                Model = jo["title"].ToString(),
                                Description = jo["title"].ToString(),
                                CrossSellModel = "",
                                CrossSellDescription = "",
                                CrossSellManufacturer = "",
                                FriendlyModel = jo["title"].ToString(),
                                FriendlyDescription = "Equipment",
                                ItemType = "Equipment",
                                ItemId = Int32.Parse(jo["ID"].ToString()),
                                ImageUrl = imageurl,
                                UrlLink = null,
                                CartridgeType = "",
                                ManufacturerName = "",
                                ManufacturerId = 0,
                                ProductCount = i,
                                ProductUrl = null,
                                SliLoggingUrl = jo["logURL"].ToString(),
                                ProductType = null,
                                MetaKeywords = Convert.ToString(jo["mfpn"]),
                                Product = null
                            });
                        }
                        if (jo["type"].ToString() == "3")
                        {
                            products.Add(jo["ID"].ToString());
                            string imageurl = jo["imageurl"].ToString();
                            if (imageurl.StartsWith("https://"))
                            {
                                int j = ("http:" + ConfigurationManager.AppSettings["CDN"]).Length + 1;
                                imageurl = imageurl.Substring(j);
                            }
                            Results.Add(new SearchEntry()
                            {
                                Model = jo["title"].ToString(),
                                Description = jo["title"].ToString(),
                                CrossSellModel = "",
                                CrossSellDescription = "",
                                CrossSellManufacturer = "",
                                FriendlyModel = jo["title"].ToString(),
                                FriendlyDescription = "Category",
                                ItemType = "Category",
                                ItemId = Int32.Parse(jo["ID"].ToString()),
                                ImageUrl = imageurl,
                                UrlLink = jo["url"].ToString(),
                                CartridgeType = "",
                                ManufacturerName = "",
                                ManufacturerId = 0,
                                ProductCount = i,
                                ProductUrl = null,
                                SliLoggingUrl = jo["logURL"].ToString(),
                                ProductType = null,
                                MetaKeywords = null,
                                Product = null
                            });
                        }
                    }
                    i++;
                }
                SLITrackingCode = results.SelectToken("tracking.code") != null ? results.SelectToken("tracking.code").ToString() : "";
            }

            // Get Products from database
            GetProducts(string.Join(",", products));

            foreach (var equip in Results.Where(x => x.ItemType == "Equipment"))
            {
                eqEquipment eq = EntityAccess.ReadEquipment(x => x.eqEquipmentID == equip.ItemId).FirstOrDefault();
                if (eq != null)
                {
                    var equipManuName = char.ToUpper(eq.manufacturer.equipmentManuName[0]) + eq.manufacturer.equipmentManuName.Substring(1);
                    equip.CartridgeType = eq.eqCartridgeType.eqCartridgeTypeName;
                    ProductFilterList = BuildProductFilter(ProductFilterList, 22, "Manufacturer", equip.ManufacturerId.ToString(), equipManuName);
                }
            }

            ProductFilterList = ProductFilterList.OrderBy(x => x.Id).ThenBy(x => x.ElementName).ToList();
        }

        private void GetResultsElastic(int takeTop, bool isQuickSearch)
        {
            List<string> terms = _searchTerm.ToLower().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries).ToList();
            terms = CheckForBrandNames(terms);

            var bQ = new BoolQuery();
            var shouldQueries = new List<QueryContainer>();
            var mustQueries = new List<QueryContainer>();

            foreach (var term in terms)
            {
                var validTerm = RemoveInvalidCharacters(term);

                if (validTerm.Length > 0)
                {
                    var wildQuery = new WildcardQuery { Field = "description", Value = "*" + validTerm + "*" };

                    if (_equipManuList.Select(x => x.ToLower()).Contains(validTerm))
                    {
                        mustQueries.Add(wildQuery);
                    }
                    else
                    {
                        shouldQueries.Add(wildQuery);
                    }

                    shouldQueries.Add(new WildcardQuery { Field = "model", Value = "*" + validTerm + "*" });
                    shouldQueries.Add(new WildcardQuery { Field = "metakeywords", Value = "*" + validTerm + "*" });
                    shouldQueries.Add(new MultiMatchQuery
                    {
                        Fields = Infer.Field<SearchEntry>(p => p.Description)
                            .And(Infer.Field<SearchEntry>(p => p.Model)),
                        Query = validTerm,
                        Fuzziness = Fuzziness.EditDistance(2)
                    });
                }
            }

            bQ.Should = shouldQueries;
            bQ.Must = mustQueries;
            var response = client.Search<SearchEntry>(new SearchRequest<SearchEntry> { Query = bQ, Size = takeTop });
            _elasticResults = response.Hits;
           Results = response.Hits.Select(x => x.Source).ToList();

            if (isQuickSearch) return;

            // Get Products from database
            GetProducts(string.Join(",",
                _elasticResults.Where(x => x.Source.ItemType == "Product").Select(x => x.Source.ItemId)));

            foreach (var equip in Results.Where(x => x.ItemType == "Equipment"))
            {
                var equipManuName = char.ToUpper(equip.ManufacturerName[0]) + equip.ManufacturerName.Substring(1);
                ProductFilterList = BuildProductFilter(ProductFilterList, 22, "Manufacturer",
                    equip.ManufacturerId.ToString(), equipManuName);
            }

            ProductFilterList = ProductFilterList.OrderBy(x => x.Id).ThenBy(x => x.ElementName).ToList();
        }

        public string RemoveInvalidCharacters(string term, bool replaceDash = false)
        {
            term = term.Replace(")", "").Replace("(", "").Replace("[", "")
                .Replace("]", "").Replace("*", "").Replace("&", "").Replace("|", "").Replace(":", "")
                .Replace("!", "").Replace("{", "").Replace("}", "").Replace("^", "").Replace("?", "")
                .Replace("\"", "");

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
                            if (newTerms[i].Length > manu.Length)
                            {
                                if (newTerms[i].Substring(0, manu.Length) == manu.ToLower())
                                {
                                    newTerms.Add(newTerms[i].Substring(0, manu.Length));
                                    newTerms.Add(newTerms[i].Substring(manu.Length, (newTerms[i].Length) - manu.Length));
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

        //public SearchViewModel GetResults(int takeTop = 50)
        //{
        //    if (!string.IsNullOrEmpty(_searchTerm) && _searchTerm.Length > 2)
        //    {
        //        var response = DoStandardSearch(takeTop);

        //        if (response.Documents.Count == 0)
        //            response = DoFallbackSearch(takeTop);

        //        _elasticResults = response.Hits;
        //        Results = response.Hits.Select(x => x.Source).ToList();

        //        // Get Products from database
        //        GetProducts(string.Join(",", _elasticResults.Where(x => x.Source.IsProduct).Select(x => x.Source.ItemId)));

        //        foreach (var equip in Results.Where(x => !x.IsProduct))
        //        {
        //            ProductFilterList = BuildProductFilter(ProductFilterList, 22, "Manufacturer", equip.ManufacturerId.ToString(), equip.ManufacturerName);
        //        }
        //    }

        //    return this;
        //}

        //private ISearchResponse<SearchEntry> DoStandardSearch(int takeTop)
        //{

        //    var search = client.Search<SearchEntry>(s => s
        //            .Take(takeTop)
        //            .Query(q =>
        //                    q.MatchPhrase(x => x
        //                        .Field(p => p.Model)
        //                        .Query(_searchTerm)
        //                        .Boost(2.2)
        //                        )
        //                    ||
        //                    q.MatchPhrase(x => x
        //                        .Field(p => p.CrossSellModel)
        //                        .Query(_searchTerm)
        //                        .Boost(2.1)
        //                        )
        //                    ||
        //                    q.MatchPhrase(x => x
        //                        .Field(p => p.Description)
        //                        .Query(_searchTerm)
        //                        .Boost(2.2)
        //                        )
        //                    ||
        //                    q.MatchPhrase(x => x
        //                        .Field(p => p.CrossSellDescription)
        //                        .Query(_searchTerm)
        //                        .Boost(2.1)
        //                        )
        //                )
        //            );

        //    if (search.Documents.Count == 0)
        //    {

        //        search = client.Search<SearchEntry>(s => s
        //            .Take(takeTop)
        //            .Query(q =>
        //                    q.Fuzzy(x => x.Value(_searchTerm)
        //                        .Fuzziness(Fuzziness.EditDistance(1))
        //                        .Field(y => y.Model))
        //                    ||
        //                    q.Fuzzy(x => x.Value(_searchTerm)
        //                        .Fuzziness(Fuzziness.EditDistance(1))
        //                        .Field(y => y.CrossSellModel))
        //                    ||
        //                    q.Fuzzy(x => x.Value(_searchTerm)
        //                        .Fuzziness(Fuzziness.EditDistance(1))
        //                        .Field(y => y.Description))
        //                )
        //            );
        //    }

        //    return search;
        //}

        //private ISearchResponse<SearchEntry> DoFallbackSearch(int takeTop)
        //{
        //    //var manufacturerForSearch = "";
        //    //var Manufacturers = new HashSet<string>();

        //    //using (var db = new Ngmd())
        //    //{
        //    //    Manufacturers = new HashSet<string>(db.manufacturers.Select(x => x.manufacturerName).ToList());
        //    //}

        //    //foreach (var manu in Manufacturers)
        //    //{
        //    //    if (_searchTerm.Contains(manu.ToLower()))
        //    //    {
        //    //        manufacturerForSearch = manu.ToLower();

        //    //        if (!_searchTerm.Contains(" "))
        //    //        {
        //    //            var startIndex = _searchTerm.IndexOf(manu.ToLower());
        //    //            var endIndex = startIndex + manu.Length;
        //    //            _searchTerm = _searchTerm.Insert(endIndex, " ");
        //    //        }
        //    //        else
        //    //        {
        //    //            var startIndex = _searchTerm.IndexOf(manu.ToLower());
        //    //            var endIndex = (startIndex + manu.Length) + 1;
        //    //            _searchTerm = _searchTerm.Substring(endIndex, _searchTerm.Length - endIndex);
        //    //        }
        //    //    }
        //    //}

        //    //if (!string.IsNullOrEmpty(manufacturerForSearch))
        //    //{
        //    return client.Search<SearchEntry>(s => s
        //    .Take(takeTop)
        //    .Query(q =>
        //            (q.MatchPhrasePrefix(x => x
        //                .Field(y => y.Description)
        //                .Query(_searchTerm)
        //                .Boost(1.1)
        //                .Slop(2)
        //                .MaxExpansions(10)))
        //            ||
        //            (q.MatchPhrasePrefix(x => x
        //                .Field(y => y.CrossSellDescription)
        //                .Query(_searchTerm)
        //                .Boost(1.1)
        //                .Slop(2)
        //                .MaxExpansions(10)))
        //            ||
        //            q.Terms(x => x.Terms(_searchTerm.Split(' '))
        //                .Field(y => y.Description))
        //            ||
        //                q.MultiMatch(x => x
        //                    .Query(_searchTerm)
        //                    .Fields(f => f.Field(p => p.Description))
        //                    .Fuzziness(Fuzziness.EditDistance(3)))
        //            ));

        //        //return client.Search<SearchEntry>(s => s
        //        //.Take(takeTop)
        //        //.Query(q =>
        //        //        (q.Term(x => x.Value(manufacturerForSearch)
        //        //            .Field(y => y.ManufacturerName))
        //        //        //||
        //        //        //q.Term(x => x.Value(manufacturerForSearch)
        //        //        //    .Field(y => y.CrossSellManufacturer)))
        //        //        &&
        //        //        (q.Terms(x => x.Terms(_searchTerm.Split(' '))
        //        //            .Field(y => y.Description))
        //        //        //||
        //        //        //q.Terms(x => x.Terms(_searchTerm.Split(' '))
        //        //        //    .Field(y => y.CrossSellDescription))
        //        //        ||
        //        //        q.Terms(x => x.Terms(_searchTerm.Split(' '))
        //        //            .Field(y => y.Model))
        //        //        //||
        //        //        //q.Terms(x => x.Terms(_searchTerm.Split(' '))
        //        //        //    .Field(y => y.CrossSellModel))
        //        //        ))
        //        //));
        //    //}
        //    //else
        //    //{
        //    //    return client.Search<SearchEntry>(s => s
        //    //    .Take(takeTop)
        //    //    .Query(q =>
        //    //            q.Terms(x => x.Terms(_searchTerm.Split(' '))
        //    //                .Field(y => y.Description))
        //    //            ||
        //    //            q.Terms(x => x.Terms(_searchTerm.Split(' '))
        //    //                .Field(y => y.CrossSellDescription))
        //    //            ||
        //    //            q.Terms(x => x.Terms(_searchTerm.Split(' '))
        //    //                .Field(y => y.Model))
        //    //            ||
        //    //            q.Terms(x => x.Terms(_searchTerm.Split(' '))
        //    //                .Field(y => y.CrossSellModel))
        //    //            )
        //    //    );
        //    //}
        //}

        public void GetProducts(string productIds)
        {
            bool isEntitledToPromo = false;
            decimal promoDiscount = 0;
            if (Convert.ToBoolean(HttpContext.Current.Session["U_IsFromPPC"]) && Convert.ToBoolean(ConfigurationManager.AppSettings["PPCPromoIsOn"]))
            {
                isEntitledToPromo = true;
                promoDiscount = Convert.ToDecimal(ConfigurationManager.AppSettings["PPCPromoDisc"].ToString());
            }
            string account = "";
            if (HttpContext.Current.Session["U_AccountNo"] != null)
            {
                account = HttpContext.Current.Session["U_AccountNo"].ToString();
            }

            var sqlParms = new List<SqlParameter>();
            var sqlParm = new SqlParameter("@WebsiteID", SqlDbType.Int);
            sqlParm.Value = int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString());
            sqlParms.Add(sqlParm);
            sqlParm = new SqlParameter("@ProductIDArray", SqlDbType.VarChar);
            sqlParm.Value = productIds;
            sqlParms.Add(sqlParm);
            sqlParm = new SqlParameter("@Account", SqlDbType.VarChar);
            sqlParm.Value = account;
            sqlParms.Add(sqlParm);
            sqlParm = new SqlParameter("@PageSize", SqlDbType.Int);
            sqlParm.Value = 200;
            sqlParms.Add(sqlParm);
            sqlParm = new SqlParameter("@PageNumber", SqlDbType.Int);
            sqlParm.Value = 1;
            sqlParms.Add(sqlParm);
            sqlParm = new SqlParameter("@productTypeID", SqlDbType.Int);
            sqlParm.Value = 0;
            sqlParms.Add(sqlParm);
            sqlParm = new SqlParameter("@manufacturerID", SqlDbType.Int);
            sqlParm.Value = 0;
            sqlParms.Add(sqlParm);
            sqlParm = new SqlParameter("@attribute8ID", SqlDbType.Int);
            sqlParm.Value = 0;
            sqlParms.Add(sqlParm);
            sqlParm = new SqlParameter("@showCompatibles", SqlDbType.Int);
            sqlParm.Value = 1;
            sqlParms.Add(sqlParm);
            var dt = SQL.ExecuteReadStoredProcedure("netgiantmasterdata", "ngmd.GetSearchResults", sqlParms, "searchResults").Tables[0];

            Products = new List<ProductEntry>();
            foreach (DataRow dr in dt.Rows)
            {
                Products.Add(CreateProductEntry(dr));
            }

            ProductFilterList = new List<ProductFilter>();
            foreach (var product in Products)
            {
                if (product.AttValue8 > 0)
                    ProductFilterList = BuildProductFilter(ProductFilterList, 8, "Colours", product.AttValue8.ToString(), product.AttDesc8);

                ProductFilterList = BuildProductFilter(ProductFilterList, 21, "Product Type", product.BrandFlag.ToString(), product.BrandFlag == BrandFlag.Original ? "Original" : "Compatible");
                ProductFilterList = BuildProductFilter(ProductFilterList, 22, "Manufacturer", product.ManufacturerId.ToString(), product.Brand);
                if (product.AttValue6 != 0 && product.AttValue6 != 25 && !String.IsNullOrEmpty(product.OfferFilterText))
                {
                    ProductFilterList = BuildProductFilter(ProductFilterList, 6, "Promotion", product.AttValue6.ToString(), product.OfferFilterText);
                }

                if (product.AssemblyCount > 1)
                {
                    sqlParms = new List<SqlParameter>();
                    sqlParm = new SqlParameter("@ProductID", SqlDbType.Int);
                    sqlParm.Value = product.ProductId;
                    sqlParms.Add(sqlParm);
                    dt = SQL.ExecuteReadStoredProcedure("netgiantmasterdata", "ngmd.GetProductComponents", sqlParms, "searchResults").Tables[0];

                    product.ComponentList = new List<ProductComponent>();
                    foreach (DataRow dr in dt.Rows)
                    {
                        product.ComponentList.Add(CreateProductComponent(dr));
                    }                  
                }

                if ((IsCompatibleSaleActive && product.BrandFlag.Equals(BrandFlag.Compatible)) 
                    || (IsOEMSaleActive && product.BrandFlag.Equals(BrandFlag.Original))
                    || (IsStationerySaleActive && product.IsStationerySaleItem)
                    )
                {
                    GenerateSalePrices(product);
                }
                else
                {
                    if (isEntitledToPromo)
                    {
                        GeneratePromoPrices(product, promoDiscount);
                    }
                }
                SetSaleStatus(product);

                var se = Results.Find(x => x.ItemType == "Product" && x.ItemId == product.ProductId);
                if (se != null)
                {
                    se.Product = product;
                }
            }

            ProductFilterList = ProductFilterList.OrderBy(x => x.Name).ThenBy(x => x.ElementName).ToList();
        }
    }

    public static class StringExtensions
    {
        public static string FormatForIndex(this string value)
        {
            return string.IsNullOrEmpty(value) ? "" : value.Replace("-", "").ToLower().Trim();
        }
    }
}


