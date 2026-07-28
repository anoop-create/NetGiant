using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections.Generic;

namespace NGBP.DataAccessLayer.DataUtilities
{
    public class ApiUtility
    {
        private string _consumer_key;
        private string _consumer_secret;
        private bool _debug;
        private string _authType;
        private RestClient _client;

        public ApiUtility(string type, string key, string secret, bool debug = false)
        {
            _authType = type;
            _consumer_key = key;
            _consumer_secret = secret;
            _debug = debug;
        }

        public string Get(string url)
        {
            _client = new RestClient(url);
            GetAuthenticator();

            var request = new RestRequest(Method.GET);
            IRestResponse response = _client.Execute(request);

            return ProcessResponse(response);
        }

        public string Post(string url, Dictionary<string, string> body)
        {
            _client = new RestClient(url);
            GetAuthenticator();

            var request = new RestRequest(Method.POST);
            foreach (KeyValuePair<string, string> item in body)
            {
                request.AddParameter(item.Key, item.Value, ParameterType.GetOrPost);
            }
            IRestResponse response = _client.Execute(request);

            return ProcessResponse(response);
        }

        public string Put(string url, Dictionary<string, string> body)
        {
            _client = new RestClient(url);
            GetAuthenticator();

            var request = new RestRequest(Method.PUT);
            request.AddHeader("User-Agent", "Netgiant Application");
            foreach (KeyValuePair<string, string> item in body)
            {
                request.AddParameter(item.Key, item.Value, ParameterType.GetOrPost);
            }
            IRestResponse response = _client.Execute(request);

            return ProcessResponse(response);
        }

        public string Delete(string url)
        {
            _client = new RestClient(url);
            GetAuthenticator();

            var request = new RestRequest(Method.DELETE);
            IRestResponse response = _client.Execute(request);

            return ProcessResponse(response);
        }

        private void GetAuthenticator()
        {
            switch (_authType)
            {
                case "oauth1":
                    {
                        _client.Authenticator = OAuth1Authenticator.ForRequestToken(_consumer_key, _consumer_secret);
                        break;
                    }
                case "oauth2":
                    {
                        //_client.Authenticator = OAuth1Authenticator.ForRequestToken(_consumer_key, _consumer_secret);
                        break;
                    }
                default:
                    {
                        break;
                    }

            }

        }

        private string ProcessResponse(IRestResponse response)
        {
            if (_debug) Console.WriteLine(response.Content);

            return response.StatusCode.ToString();
        }
    }
}
