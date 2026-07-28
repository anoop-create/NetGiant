//TIDYUP
//using netGiant.Intranet.BusinessLayer.Utilities;
//using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
//using Newtonsoft.Json.Linq;
//using RestSharp;
//using RestSharp.Authenticators;
//using System;
//using System.Collections.Generic;
//using System.Data;
//using System.IO;
//using System.Linq;
//using System.Net;
//using System.Net.Cache;
//using System.Security.Cryptography;
//using System.Text;
//using System.Threading.Tasks;

//namespace netGiant.Intranet.BusinessLayer.ViewModels.Ecommerce
//{
//    public class WAFRulesViewModel : CommonViewModel
//    {
//        public WAFRulesViewModel()
//        {

//        }
//        public List<Telerik> WAFRulesList { get; set; }

//        public void GetWAFRules(string site)
//        {
//            // Get a JSONResult of Rules
//            const string consumerKey = "1df1089b5341ee3bd9583ed8acd4198805aba1600";
//            const string consumerSecret = "414195473d26acffca97f1d0f653daf2";
//            const string tokenSecret = "";
//            const string tokenValue = "";
//            const string url = "https://api.stackpath.com/v1";

//            int counter = 1;

//            OtherUtilities.SetTlsVersion();
//            var stackPathClient = new RestSharp.RestClient(url)
//            {
//                Authenticator = OAuth1Authenticator.ForProtectedResource(consumerKey, consumerSecret, tokenValue, tokenSecret)
//            };

//            WAFRulesList = new List<Telerik>();
//            var stackPathRequest = new RestRequest("/xtqqu9zh2ny61xj/sites/" + site + "/waf/rules?page=" + counter.ToString());
//            var stackPathResponse = stackPathClient.Execute(stackPathRequest, Method.GET);
//            while (stackPathResponse.StatusCode == HttpStatusCode.OK)
//            {
//                JObject interimResult = JObject.Parse(stackPathResponse.Content);
//                if (interimResult.SelectToken("data.rules").ToString() == "[]")
//                {
//                    break;
//                }
//                JObject finalResult = (JObject)interimResult.SelectToken("data.rules");

//                foreach (var item in finalResult)
//                {
//                    Telerik wafRule = new Telerik();
//                    wafRule.Name = item.Value["name"].ToString();
//                    wafRule.WRId = item.Value["id"].ToString();
//                    wafRule.Action = item.Value["action"].ToString();
//                    wafRule.Scope = item.Value["conditions"][0]["scope"].ToString();
//                    wafRule.Data = item.Value["conditions"][0]["data"].ToString();
//                    WAFRulesList.Add(wafRule);
//                }
//                counter += 1;
//                stackPathRequest = new RestRequest("/xtqqu9zh2ny61xj/sites/" + site + "/waf/rules?page=" + counter.ToString());
//                stackPathResponse = stackPathClient.Execute(stackPathRequest, Method.GET);
//            }
//        }
//        public class Telerik
//        {
//            public string WRId { get; set; }
//            public string Name { get; set; }
//            public string Action { get; set; }
//            public string Scope { get; set; }
//            public string Data { get; set; }
//        }
//    }
//}
