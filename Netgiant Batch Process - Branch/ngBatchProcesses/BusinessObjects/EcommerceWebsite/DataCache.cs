using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ngBatchProcesses.BusinessObjects.Shared;
using System;
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
        private StandardFunctions _stnFunc;

        public DataCache(int websiteId)
        {
            _userAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/67.0.3396.99 Safari/537.36";
            _url = "https://" + StandardFunctions.GetConfigurationSetting("Website Application Variables", "siteRoot", websiteId) + "Portal/DeleteCache?cacheKey=";
            _stnFunc = new StandardFunctions();

            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            if (Properties.Settings.Default.Environment != "Live")
            {
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            }
        }

        public void ClearCache(string cacheKey)
        {
            _url += cacheKey;

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(_url);
            request.UserAgent = _userAgent;
            string username = _username;
            string password = _password;
            string encoded = Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1").GetBytes(username + ":" + password));
            request.Headers.Add("Authorization", "Basic " + encoded);
            request.PreAuthenticate = true;

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            if (response.StatusCode == HttpStatusCode.OK)
            {
                using (Stream responseStream = response.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(responseStream, Encoding.UTF8);
                    var data = JsonConvert.DeserializeObject(reader.ReadToEnd());
                    JObject result = JObject.Parse(data.ToString());

                    if (Convert.ToBoolean(result["issuccess"]))
                    {
                        _stnFunc.AddToActivityLog("Successfully cleared menu cache");
                    }
                    else
                    {
                        _stnFunc.AddToActivityLog("Error: Could not clear menu cache, issuccess false");
                    }
                }
            }
            else
            {
                _stnFunc.AddToActivityLog("Error: Could not clear menu cache");
            }
        }
    }
}
