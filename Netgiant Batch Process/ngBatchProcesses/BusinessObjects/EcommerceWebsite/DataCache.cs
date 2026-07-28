using Google.Apis.Util;
using MailChimp.Net.Models;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ngBatchProcesses.BusinessObjects.Shared;
using RestSharp;
using RestSharp.Authenticators;
using RestSharp.Serializers.NewtonsoftJson;
using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Text;

namespace ngBatchProcesses.BusinessObjects.EcommerceWebsite
{
    public class DataCache
    {
        private string _url;
        private string _userAgent;
        private const string _username = "webadmin";
        private const string _password = "Innovation2020";

        public DataCache(int websiteId)
        {
            _userAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/67.0.3396.99 Safari/537.36";
            _url = "https://" + EntityFunctions.GetConfigurationSetting("Website Application Variables", "siteRoot", websiteId);

            ServicePointManager.Expect100Continue = true;
            StandardFunctions.SetTlsVersion();

            //if (Properties.Settings.Default.Environment != "Live")
            //{
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            //}
        }

        public void ClearCache(string cacheKey = null)
        {
            string resource = "Portal/DeleteCache?cacheKey=" + cacheKey;

            var client = new RestClient(new RestClientOptions(_url), configureSerialization: s => s.UseNewtonsoftJson());

            var request = new RestRequest(resource, RestSharp.Method.Get)
            {
                Authenticator = new HttpBasicAuthenticator("webadmin", "Innovation2020")
            }
                .AddParameter("grant_type", "client_credentials")
                .AddHeader("Host", _url.Replace("https://", "").Replace("/", ""))
                .AddHeader("X-FORWARDED-PROTO", "https");

            var response = client.Execute(request, RestSharp.Method.Get);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Successfully cleared cache" });
            }
            else
            {
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Error: Could not clear cache. Status Code: " + 
                    response.StatusCode.ToString() + " " + 
                    response.StatusDescription + " " +
                    _url + " " + resource, ErrorCode = "ERROR" });
            }
        }
    }
}
