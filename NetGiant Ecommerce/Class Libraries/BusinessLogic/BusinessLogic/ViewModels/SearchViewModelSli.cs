//using DataAccess.Utilities;
//using Nest;
//using Newtonsoft.Json;
//using Newtonsoft.Json.Linq;
//using System;
//using System.Collections.Generic;
//using System.Configuration;
//using System.Data;
//using System.Data.SqlClient;
//using System.IO;
//using System.Linq;
//using System.Net;
//using System.Net.Http;
//using System.Net.Http.Headers;
//using System.Text;
//using System.Web;

//namespace BusinessLogic.ViewModels
//{
//    public class SearchViewModel : ProductViewModel
//    {
//        public SearchViewModel()
//        {
//            Results = new List<SearchEntry>();
//            ResultsUrl = ConfigurationManager.AppSettings["SLIResultsUrl"];
//            //settings = new ConnectionSettings(node);
//            //client = new ElasticClient(settings);
//            //settings.DefaultIndex(CommonDataLookup("SearchIndexName"));
//            _equipManuList = DataCache.GetManufacturers("").Select(x => x.Text).ToList();
//        }

//        public EquipmentEntry Equipment { get; set; }
//        public CategoryEntry Category { get; set; }
//        public string ResultsUrl { get; set; }
//        public List<SearchEntry> Results { get; set; }
//        public List<ProductEntry> Products { get; set; }
//        public string SearchTerm
//        {
//            get
//            {
//                return _searchTerm;
//            }
//            set
//            {
//                _searchTerm = value.FormatForIndex();
//            }
//        }

//        //private Uri node;
//        //private ConnectionSettings settings;
//        //private ElasticClient client;
//        private string _searchTerm;
//        //private IReadOnlyCollection<IHit<SearchEntry>> _elasticResults;
//        private List<string> _equipManuList;

//        public void GetResults(int takeTop = 100, bool isQuickSearch = false)
//        {
//            // SLI Search
//            if (isQuickSearch) return;

//            string url = ResultsUrl + "w=" + SearchTerm;
//            JObject results = new JObject();
//            List<string> products = new List<string>();

//            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
//            try
//            {
//                WebResponse response = request.GetResponse();
//                using (Stream responseStream = response.GetResponseStream())
//                {
//                    StreamReader reader = new StreamReader(responseStream, Encoding.UTF8);
//                    results = JObject.Parse(reader.ReadToEnd());
//                }
//            }
//            catch (WebException ex)
//            {
//                WebResponse errorResponse = ex.Response;
//                using (Stream responseStream = errorResponse.GetResponseStream())
//                {
//                    StreamReader reader = new StreamReader(responseStream, Encoding.GetEncoding("utf-8"));
//                    String errorText = reader.ReadToEnd();
//                    // log errorText
//                }
//                throw;
//            }

//            foreach (JObject jo in results["results"])
//            {
//                //if (jo["Type"] != null)
//                //{
//                //    if (jo["Type"].ToString() == "1")
//                //    {
//                products.Add(jo["ID"].ToString());
//                //    }
//                //}
//            }

//            // Get Products from database
//            GetProducts(string.Join(",", products));
//            //GetProducts(string.Join(",",
//            //    _elasticResults.Where(x => x.Source.ItemType == "Product").Select(x => x.Source.ItemId)));

//            foreach (var equip in Results.Where(x => x.ItemType == "Equipment"))
//            {
//                var equipManuName = char.ToUpper(equip.ManufacturerName[0]) + equip.ManufacturerName.Substring(1);
//                ProductFilterList = BuildProductFilter(ProductFilterList, 22, "Manufacturer",
//                    equip.ManufacturerId.ToString(), equipManuName);
//            }

//            ProductFilterList = ProductFilterList.OrderBy(x => x.Id).ThenBy(x => x.ElementName).ToList();







//            //List<string> terms = _searchTerm.ToLower().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries).ToList();
//            //    terms = CheckForBrandNames(terms);

//            //    var bQ = new BoolQuery();
//            //    var shouldQueries = new List<QueryContainer>();
//            //    var mustQueries = new List<QueryContainer>();

//            //    foreach (var term in terms)
//            //    {
//            //        var validTerm = RemoveInvalidCharacters(term);

//            //        if (validTerm.Length > 0)
//            //        {
//            //            var wildQuery = new WildcardQuery { Field = "description", Value = "*" + validTerm + "*" };

//            //            if (_equipManuList.Select(x => x.ToLower()).Contains(validTerm))
//            //            {
//            //                mustQueries.Add(wildQuery);
//            //            }
//            //            else
//            //            {
//            //                shouldQueries.Add(wildQuery);
//            //            }

//            //            shouldQueries.Add(new WildcardQuery { Field = "model", Value = "*" + validTerm + "*" });
//            //            shouldQueries.Add(new WildcardQuery { Field = "metakeywords", Value = "*" + validTerm + "*" });
//            //            shouldQueries.Add(new MultiMatchQuery
//            //            {
//            //                Fields = Infer.Field<SearchEntry>(p => p.Description)
//            //                    .And(Infer.Field<SearchEntry>(p => p.Model)),
//            //                Query = validTerm,
//            //                Fuzziness = Fuzziness.EditDistance(2)
//            //            } );
//            //        }
//            //    }

//            //    bQ.Should = shouldQueries;
//            //    bQ.Must = mustQueries;
//            //    var response = client.Search<SearchEntry>(new SearchRequest<SearchEntry>{ Query = bQ, Size = takeTop });
//            //    _elasticResults = response.Hits;
//            //    Results = response.Hits.Select(x => x.Source).ToList();

//            //    if (isQuickSearch) return;

//            //    // Get Products from database
//            //    GetProducts(string.Join(",",
//            //        _elasticResults.Where(x => x.Source.ItemType == "Product").Select(x => x.Source.ItemId)));

//            //    foreach (var equip in Results.Where(x => x.ItemType == "Equipment"))
//            //    {
//            //        var equipManuName = char.ToUpper(equip.ManufacturerName[0]) + equip.ManufacturerName.Substring(1);
//            //        ProductFilterList = BuildProductFilter(ProductFilterList, 22, "Manufacturer",
//            //            equip.ManufacturerId.ToString(), equipManuName);
//            //    }

//            //    ProductFilterList = ProductFilterList.OrderBy(x => x.Id).ThenBy(x => x.ElementName).ToList();
//        }

//        public string RemoveInvalidCharacters(string term, bool replaceDash = false)
//        {
//            term = term.Replace(")", "").Replace("(", "").Replace("[", "")
//                .Replace("]", "").Replace("*", "").Replace("&", "").Replace("|", "").Replace(":", "")
//                .Replace("!", "").Replace("{", "").Replace("}", "").Replace("^", "").Replace("?", "")
//                .Replace("\"", "");

//            if (replaceDash && term.Length > 1)
//                term = term.Replace("-", " ");

//            if (term.Length == 1)
//                term = term.Replace("-", "");

//            return term;
//        }

//        private List<string> CheckForBrandNames(List<string> terms)
//        {
//            var newTerms = terms;

//            try
//            {
//                var exitLoop = false;

//                for (int i = 0; i < newTerms.Count; i++)
//                {
//                    if (!newTerms[i].Contains(" "))
//                    {
//                        foreach (var manu in _equipManuList)
//                        {
//                            if (newTerms[i].Length > manu.Length)
//                            {
//                                if (newTerms[i].Substring(0, manu.Length) == manu.ToLower())
//                                {
//                                    newTerms.Add(newTerms[i].Substring(0, manu.Length));
//                                    newTerms.Add(newTerms[i].Substring(manu.Length, (newTerms[i].Length) - manu.Length));
//                                    newTerms.Remove(newTerms[i]);
//                                    exitLoop = true;
//                                }
//                            }

//                            if (exitLoop)
//                                break;
//                        }
//                    }

//                    if (exitLoop)
//                        break;
//                }
//            }
//            catch (Exception)
//            {
//                newTerms = terms;
//            }

//            return newTerms;
//        }

//        //public SearchViewModel GetResults(int takeTop = 50)
//        //{
//        //    if (!string.IsNullOrEmpty(_searchTerm) && _searchTerm.Length > 2)
//        //    {
//        //        var response = DoStandardSearch(takeTop);

//        //        if (response.Documents.Count == 0)
//        //            response = DoFallbackSearch(takeTop);

//        //        _elasticResults = response.Hits;
//        //        Results = response.Hits.Select(x => x.Source).ToList();

//        //        // Get Products from database
//        //        GetProducts(string.Join(",", _elasticResults.Where(x => x.Source.IsProduct).Select(x => x.Source.ItemId)));

//        //        foreach (var equip in Results.Where(x => !x.IsProduct))
//        //        {
//        //            ProductFilterList = BuildProductFilter(ProductFilterList, 22, "Manufacturer", equip.ManufacturerId.ToString(), equip.ManufacturerName);
//        //        }
//        //    }

//        //    return this;
//        //}

//        //private ISearchResponse<SearchEntry> DoStandardSearch(int takeTop)
//        //{

//        //    var search = client.Search<SearchEntry>(s => s
//        //            .Take(takeTop)
//        //            .Query(q =>
//        //                    q.MatchPhrase(x => x
//        //                        .Field(p => p.Model)
//        //                        .Query(_searchTerm)
//        //                        .Boost(2.2)
//        //                        )
//        //                    ||
//        //                    q.MatchPhrase(x => x
//        //                        .Field(p => p.CrossSellModel)
//        //                        .Query(_searchTerm)
//        //                        .Boost(2.1)
//        //                        )
//        //                    ||
//        //                    q.MatchPhrase(x => x
//        //                        .Field(p => p.Description)
//        //                        .Query(_searchTerm)
//        //                        .Boost(2.2)
//        //                        )
//        //                    ||
//        //                    q.MatchPhrase(x => x
//        //                        .Field(p => p.CrossSellDescription)
//        //                        .Query(_searchTerm)
//        //                        .Boost(2.1)
//        //                        )
//        //                )
//        //            );

//        //    if (search.Documents.Count == 0)
//        //    {

//        //        search = client.Search<SearchEntry>(s => s
//        //            .Take(takeTop)
//        //            .Query(q =>
//        //                    q.Fuzzy(x => x.Value(_searchTerm)
//        //                        .Fuzziness(Fuzziness.EditDistance(1))
//        //                        .Field(y => y.Model))
//        //                    ||
//        //                    q.Fuzzy(x => x.Value(_searchTerm)
//        //                        .Fuzziness(Fuzziness.EditDistance(1))
//        //                        .Field(y => y.CrossSellModel))
//        //                    ||
//        //                    q.Fuzzy(x => x.Value(_searchTerm)
//        //                        .Fuzziness(Fuzziness.EditDistance(1))
//        //                        .Field(y => y.Description))
//        //                )
//        //            );
//        //    }

//        //    return search;
//        //}

//        //private ISearchResponse<SearchEntry> DoFallbackSearch(int takeTop)
//        //{
//        //    //var manufacturerForSearch = "";
//        //    //var Manufacturers = new HashSet<string>();

//        //    //using (var db = new Ngmd())
//        //    //{
//        //    //    Manufacturers = new HashSet<string>(db.manufacturers.Select(x => x.manufacturerName).ToList());
//        //    //}

//        //    //foreach (var manu in Manufacturers)
//        //    //{
//        //    //    if (_searchTerm.Contains(manu.ToLower()))
//        //    //    {
//        //    //        manufacturerForSearch = manu.ToLower();

//        //    //        if (!_searchTerm.Contains(" "))
//        //    //        {
//        //    //            var startIndex = _searchTerm.IndexOf(manu.ToLower());
//        //    //            var endIndex = startIndex + manu.Length;
//        //    //            _searchTerm = _searchTerm.Insert(endIndex, " ");
//        //    //        }
//        //    //        else
//        //    //        {
//        //    //            var startIndex = _searchTerm.IndexOf(manu.ToLower());
//        //    //            var endIndex = (startIndex + manu.Length) + 1;
//        //    //            _searchTerm = _searchTerm.Substring(endIndex, _searchTerm.Length - endIndex);
//        //    //        }
//        //    //    }
//        //    //}

//        //    //if (!string.IsNullOrEmpty(manufacturerForSearch))
//        //    //{
//        //    return client.Search<SearchEntry>(s => s
//        //    .Take(takeTop)
//        //    .Query(q =>
//        //            (q.MatchPhrasePrefix(x => x
//        //                .Field(y => y.Description)
//        //                .Query(_searchTerm)
//        //                .Boost(1.1)
//        //                .Slop(2)
//        //                .MaxExpansions(10)))
//        //            ||
//        //            (q.MatchPhrasePrefix(x => x
//        //                .Field(y => y.CrossSellDescription)
//        //                .Query(_searchTerm)
//        //                .Boost(1.1)
//        //                .Slop(2)
//        //                .MaxExpansions(10)))
//        //            ||
//        //            q.Terms(x => x.Terms(_searchTerm.Split(' '))
//        //                .Field(y => y.Description))
//        //            ||
//        //                q.MultiMatch(x => x
//        //                    .Query(_searchTerm)
//        //                    .Fields(f => f.Field(p => p.Description))
//        //                    .Fuzziness(Fuzziness.EditDistance(3)))
//        //            ));

//        //        //return client.Search<SearchEntry>(s => s
//        //        //.Take(takeTop)
//        //        //.Query(q =>
//        //        //        (q.Term(x => x.Value(manufacturerForSearch)
//        //        //            .Field(y => y.ManufacturerName))
//        //        //        //||
//        //        //        //q.Term(x => x.Value(manufacturerForSearch)
//        //        //        //    .Field(y => y.CrossSellManufacturer)))
//        //        //        &&
//        //        //        (q.Terms(x => x.Terms(_searchTerm.Split(' '))
//        //        //            .Field(y => y.Description))
//        //        //        //||
//        //        //        //q.Terms(x => x.Terms(_searchTerm.Split(' '))
//        //        //        //    .Field(y => y.CrossSellDescription))
//        //        //        ||
//        //        //        q.Terms(x => x.Terms(_searchTerm.Split(' '))
//        //        //            .Field(y => y.Model))
//        //        //        //||
//        //        //        //q.Terms(x => x.Terms(_searchTerm.Split(' '))
//        //        //        //    .Field(y => y.CrossSellModel))
//        //        //        ))
//        //        //));
//        //    //}
//        //    //else
//        //    //{
//        //    //    return client.Search<SearchEntry>(s => s
//        //    //    .Take(takeTop)
//        //    //    .Query(q =>
//        //    //            q.Terms(x => x.Terms(_searchTerm.Split(' '))
//        //    //                .Field(y => y.Description))
//        //    //            ||
//        //    //            q.Terms(x => x.Terms(_searchTerm.Split(' '))
//        //    //                .Field(y => y.CrossSellDescription))
//        //    //            ||
//        //    //            q.Terms(x => x.Terms(_searchTerm.Split(' '))
//        //    //                .Field(y => y.Model))
//        //    //            ||
//        //    //            q.Terms(x => x.Terms(_searchTerm.Split(' '))
//        //    //                .Field(y => y.CrossSellModel))
//        //    //            )
//        //    //    );
//        //    //}
//        //}

//        public void GetProducts(string productIds)
//        {
//            bool isEntitledToPromo = false;
//            decimal promoDiscount = 0;
//            if (Convert.ToBoolean(HttpContext.Current.Session["U_IsFromPPC"]) && Convert.ToBoolean(ConfigurationManager.AppSettings["PPCPromoIsOn"]))
//            {
//                isEntitledToPromo = true;
//                promoDiscount = Convert.ToDecimal(ConfigurationManager.AppSettings["PPCPromoDisc"].ToString());
//            }
//            string account = "";
//            if (HttpContext.Current.Session["U_AccountNo"] != null)
//            {
//                account = HttpContext.Current.Session["U_AccountNo"].ToString();
//            }

//            var sqlParms = new List<SqlParameter>();
//            var sqlParm = new SqlParameter("@WebsiteID", SqlDbType.Int);
//            sqlParm.Value = int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString());
//            sqlParms.Add(sqlParm);
//            sqlParm = new SqlParameter("@ProductIDArray", SqlDbType.VarChar);
//            sqlParm.Value = productIds;
//            sqlParms.Add(sqlParm);
//            sqlParm = new SqlParameter("@Account", SqlDbType.VarChar);
//            sqlParm.Value = account;
//            sqlParms.Add(sqlParm);
//            sqlParm = new SqlParameter("@PageSize", SqlDbType.Int);
//            sqlParm.Value = 200;
//            sqlParms.Add(sqlParm);
//            sqlParm = new SqlParameter("@PageNumber", SqlDbType.Int);
//            sqlParm.Value = 1;
//            sqlParms.Add(sqlParm);
//            sqlParm = new SqlParameter("@productTypeID", SqlDbType.Int);
//            sqlParm.Value = 0;
//            sqlParms.Add(sqlParm);
//            sqlParm = new SqlParameter("@manufacturerID", SqlDbType.Int);
//            sqlParm.Value = 0;
//            sqlParms.Add(sqlParm);
//            sqlParm = new SqlParameter("@attribute8ID", SqlDbType.Int);
//            sqlParm.Value = 0;
//            sqlParms.Add(sqlParm);
//            sqlParm = new SqlParameter("@showCompatibles", SqlDbType.Int);
//            sqlParm.Value = 1;
//            sqlParms.Add(sqlParm);
//            var dt = SQL.ExecuteReadStoredProcedure("netgiantmasterdata", "ngmd.GetSearchResults", sqlParms, "searchResults").Tables[0];

//            Products = new List<ProductEntry>();
//            foreach (DataRow dr in dt.Rows)
//            {
//                Products.Add(CreateProductEntry(dr));
//            }

//            ProductFilterList = new List<ProductFilter>();
//            foreach (var product in Products)
//            {
//                if (product.AttValue8 > 0)
//                    ProductFilterList = BuildProductFilter(ProductFilterList, 8, "Colours", product.AttValue8.ToString(), product.AttDesc8);

//                ProductFilterList = BuildProductFilter(ProductFilterList, 21, "Product Type", product.BrandFlag.ToString(), product.BrandFlag == BrandFlag.Original ? "Original" : "Compatible");
//                ProductFilterList = BuildProductFilter(ProductFilterList, 22, "Manufacturer", product.ManufacturerId.ToString(), product.Brand);
//                if (product.AttValue6 != 0 && product.AttValue6 != 25)
//                {
//                    ProductFilterList = BuildProductFilter(ProductFilterList, 6, "Promotion", product.AttValue6.ToString(), product.OfferShortText);
//                }

//                if (product.AssemblyCount > 1)
//                {
//                    sqlParms = new List<SqlParameter>();
//                    sqlParm = new SqlParameter("@WebsiteId", SqlDbType.Int);
//                    sqlParm.Value = int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString());
//                    sqlParms.Add(sqlParm);
//                    sqlParm = new SqlParameter("@ProductID", SqlDbType.Int);
//                    sqlParm.Value = product.ProductId;
//                    sqlParms.Add(sqlParm);
//                    sqlParm = new SqlParameter("@Account", SqlDbType.VarChar);
//                    sqlParm.Value = account;
//                    sqlParms.Add(sqlParm);
//                    dt = SQL.ExecuteReadStoredProcedure("netgiantmasterdata", "ngmd.GetProductComponents", sqlParms, "searchResults").Tables[0];

//                    product.ComponentList = new List<ProductEntry>();
//                    foreach (DataRow dr in dt.Rows)
//                    {
//                        product.ComponentList.Add(CreateProductEntry(dr, product.ProductId));
//                    }
//                }

//                if ((IsCompatibleSaleActive && product.BrandFlag.Equals(BrandFlag.Compatible))
//                    || (IsOEMSaleActive && product.BrandFlag.Equals(BrandFlag.Original))
//                    || (IsStationerySaleActive && product.IsStationerySaleItem)
//                    )
//                {
//                    GenerateSalePrices(product);
//                }
//                else
//                {
//                    if (isEntitledToPromo)
//                    {
//                        GeneratePromoPrices(product, promoDiscount);
//                    }
//                }

//                var se = Results.Find(x => x.ItemType == "Product" && x.ItemId == product.ProductId);
//                if (se != null)
//                {
//                    se.Product = product;
//                }
//            }

//            ProductFilterList = ProductFilterList.OrderBy(x => x.Name).ThenBy(x => x.ElementName).ToList();
//        }
//    }

//    public static class StringExtensions
//    {
//        public static string FormatForIndex(this string value)
//        {
//            return string.IsNullOrEmpty(value) ? "" : value.Replace("-", "").ToLower().Trim();
//        }
//    }
//}


