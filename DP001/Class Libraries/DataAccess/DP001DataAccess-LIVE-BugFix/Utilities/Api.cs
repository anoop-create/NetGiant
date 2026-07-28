using DP001DataAccess.Entities;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Web.WebPages.OAuth;
using System.Net;
using System.IO;
using System.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Reflection;
using System.Threading;

namespace DP001DataAccess.Utilities
{
    public class Api
    {
        //Constructor
        public Api(Channel channel)
        {
            _channel = channel;
            _client = new HttpClient();
            _response = new HttpResponseMessage();
            ProductList = new List<ProductInventory>();
            SupplierList = new List<SupplierInventory>();
            using (DP001Entities db = new DP001Entities())
            {
                APISetting apiSettings;
                apiSettings = db.APISettings
                                .Include("Lookup")
                                .Where(x => x.GatewayFK == _channel.TenantSetting.GatewayFK)
                                .FirstOrDefault();

                GatewayName     = apiSettings.Lookup.LookupName;
                _apiAuthURL     = apiSettings.ApiAuthURL;
                _apiRequestURL  = apiSettings.ApiRequestURL;
                _apiClientID    = apiSettings.ApiClientID;
                _apiSecret      = apiSettings.ApiSecret;
                _apiRefreshToken = apiSettings.ApiRefreshToken;
            }
            if (GatewayName == "SAP Anywhere")
            {
                AuthenticateToSapAPI().Wait();
            }
        }

        private Channel _channel;
        private HttpResponseMessage _response;
        private HttpClient _client;
        private string _apiAuthURL;
        private string _apiRequestURL;
        private string _apiClientID;
        private string _apiSecret;
        private string _apiRefreshToken;
        private string _apiAccessToken;

        public string GatewayName { get; set; }
        public List<ProductInventory> ProductList { get; set; }
        public List<SupplierInventory> SupplierList { get; set; }

        private void ReAuthenticateToSapAPI()
        {
            AuthenticateToSapAPI().Wait();
        }

        private async Task AuthenticateToSapAPI()
        {
           //bool errorHasOccurred = false;
            string errorMessage = "";

            try
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(_apiAuthURL);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    HttpResponseMessage response = await client.GetAsync("token?client_id=" + _apiClientID + "&client_secret=" + _apiSecret + "&grant_type=refresh_token&refresh_token=" + _apiRefreshToken);
                    if (response.IsSuccessStatusCode)
                    {
                        string jsonString = await response.Content.ReadAsStringAsync();
                        SapAAuthentication auth = JsonConvert.DeserializeObject<SapAAuthentication>(jsonString);
                        _apiAccessToken = auth.access_token;
                    }
                }
            }
            catch (Exception e)
            {
                //errorHasOccurred = true;
                errorMessage = e.Message;
            }
        }

        public async Task HttpGetSAPProductsTask()
        {
            CommonDataFunctions.CreateLogEntry(_channel, "START HttpGetSAPProductsTask", "Information");

            //string errorMessage = "";
            string apiCall;
            int limit = 1000;
            int offset = 0;
            bool isSuccess;
            int tryCount;

            List<SapASku> tempProducts = new List<SapASku>();

            using (_client = new HttpClient())
            {
                _client.BaseAddress = new Uri(_apiRequestURL);
                _client.DefaultRequestHeaders.Accept.Clear();
                _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                apiCall = "SKUs?limit=" + limit.ToString() + "&expand=product&offset=" + offset.ToString() + "&access_token=" + _apiAccessToken;
                isSuccess = false;
                tryCount = 0;

                while (!isSuccess && tryCount < 5)
                {
                    _response = await _client.GetAsync(apiCall);
                    switch (_response.StatusCode)
                    {
                        case (HttpStatusCode)200:
                            isSuccess = true;
                            break;
                        case (HttpStatusCode)401:
                            ReAuthenticateToSapAPI();
                            break;
                        case (HttpStatusCode)429:
                            tryCount += 1;
                            Thread.Sleep(2000);
                            break;
                        default:
                            tryCount = 5;
                            break;
                    }
                }

                if (_response.IsSuccessStatusCode)
                {
                    string jsonString = await _response.Content.ReadAsStringAsync();
                    tempProducts = JsonConvert.DeserializeObject<List<SapASku>>(jsonString);
                    //int justOnce = 0;

                    //while (justOnce == 0)
                    while (tempProducts.Count > 0)
                    {
                        foreach (SapASku tp in tempProducts)
                        {
                            if (tp.status == "Active")
                            {
                                //if (tp.product.code == "GLENPACK1")
                                //{
                                //    int i = 0;
                                //}
                                ProductInventory pi = new ProductInventory();
                                pi.ChannelFK = _channel.ChannelID;
                                if (tp.product.brand != null)
                                {
                                    pi.BrandName = tp.product.brand.name;
                                }
                                else
                                {
                                    pi.BrandName = "Unknown";
                                }
                                pi.ManufacturerPartNo = tp.code;
                                pi.Description = tp.name;
                                pi.ClientProductID = "";   //???????
                                pi.LnKdBrandName = "";
                                pi.LnkdManufacturerPartNo = "";
                                if (tp.product.category != null)
                                {
                                    pi.ProductCategoryName = tp.product.category.name;
                                }
                                else
                                {
                                    pi.ProductCategoryName = "Unknown";
                                }
                                ProductList.Add(pi);

                                SupplierInventory si = new SupplierInventory();
                                si.ChannelFK = _channel.ChannelID;
                                si.SupplierFK = 0;  //??????
                                if (tp.product.brand != null)
                                {
                                    si.BrandName = tp.product.brand.name;
                                }
                                else
                                {
                                    si.BrandName = "Unknown";
                                }
                                si.ManufacturerPartNo = tp.code;
                                si.StockQuantity = 0;
                                si.Price = tp.netPurchasePrice ?? 0;
                                SupplierList.Add(si);
                            }
                        }
                        offset += limit;

                        apiCall = "SKUs?limit=" + limit.ToString() + "&expand=product&offset=" + offset.ToString() + "&access_token=" + _apiAccessToken;
                        isSuccess = false;
                        tryCount = 0;

                        while (!isSuccess && tryCount < 5)
                        {
                            _response = await _client.GetAsync(apiCall);
                            switch (_response.StatusCode)
                            {
                                case (HttpStatusCode)200:
                                    isSuccess = true;
                                    break;
                                case (HttpStatusCode)401:
                                    ReAuthenticateToSapAPI();
                                    break;
                                case (HttpStatusCode)429:
                                    tryCount += 1;
                                    Thread.Sleep(2000);
                                    break;
                                default:
                                    tryCount = 5;
                                    break;
                            }
                        } 
                        if (!_response.IsSuccessStatusCode)
                            break;

                        jsonString = await _response.Content.ReadAsStringAsync();
                        tempProducts = null;
                        tempProducts = JsonConvert.DeserializeObject<List<SapASku>>(jsonString);
                        //justOnce = 1;
                    }
                }
            }

            CommonDataFunctions.CreateLogEntry(_channel, "END HttpGetSAPProductsTask", "Information");
        }

        private async void DoApiCall(string apiCall)
        {
            bool isSuccess = false;
            int tryCount = 0;

            while (!isSuccess && tryCount < 5)
            {
                _response = await _client.GetAsync(apiCall);
                switch (_response.StatusCode)
                {
                    case (HttpStatusCode)200:
                        isSuccess = true;
                        break;
                    case (HttpStatusCode)401:
                        ReAuthenticateToSapAPI();
                        break;
                    case (HttpStatusCode)429:
                        tryCount += 1;
                        Thread.Sleep(2000);
                        break;
                    default:
                        tryCount = 5;
                        break;
                }
            }
        }

        private class SapAAuthentication
        {
            public string access_token { get; set; }
            public string token_type { get; set; }
            public string refresh_token { get; set; }
            public int expires_in { get; set; }
            public string scope { get; set; }
        }

        public class SapASku
        {
            public string code { get; set; }
            public string name { get; set; }
            public string status { get; set; }
            public decimal? netPurchasePrice { get; set; }
            public decimal? grossPurchasePrice { get; set; }
            public SapAProduct product { get; set; }
        }

        public class SapAProduct
        {
            public string code { get; set; }
            public string name { get; set; }
            public SapACategory category { get; set; }
            public string status { get; set; }
            public SapAManufacturer manufacturer { get; set; }
            public SapABrand brand { get; set; }
        }
        
        public class SapACategory
        {
            public int id { get; set; }
            public string name { get; set; }
        }

        public class SapAManufacturer
        {
            public int id { get; set; }
            public string name { get; set; }
        }

        public class SapABrand
        {
            public int id { get; set; }
            public string name { get; set; }
        }
    }

}
