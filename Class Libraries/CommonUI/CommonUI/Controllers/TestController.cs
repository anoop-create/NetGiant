using BusinessLogic;
using BusinessLogic.ViewModels;
using DataAccess.EntityFramework;
using DataAccess.Utilities;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RestSharp.Authenticators;
using RestSharp;
using System.Configuration;
using System.Net;

namespace CommonUI.Controllers
{
    public class TestController : ApplicationController
    {
        // GET: Test
        public ActionResult Index()
        {
            MakeLiveRequest1("DeleteCache?cacheKey=PopularRanges/9/54&", "");
            return View();
        }

        public ActionResult Test1()
        {
            return View();
        }

        [AuthorizeIpAddress]
        public ActionResult SagePayTemplate()
        {
            var model = new HomeViewModel();
            return View(model);
        }

        private bool MakeLiveRequest1(string func, string message)
        {
            bool isSuccess = true;
            List<string> ip = new List<string> { "10.0.0.5", "10.0.0.10" };

            try
            {
                foreach (string i_p in ip)
                {
                    var client = new RestClient("http://" + i_p);
                    var request = new RestRequest("/portal/" + func + "exec=1", RestSharp.Method.Get)
                    {
                        Authenticator = new HttpBasicAuthenticator("webadmin", "Innovation2020")
                    }
                        .AddParameter("grant_type", "client_credentials")
                        .AddHeader("Host", ConfigurationManager.AppSettings["DomainName_Live"].Replace("/", ""))
                        .AddHeader("X-FORWARDED-PROTO", "https");
                    var response = client.Execute(request, RestSharp.Method.Get);
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        Utilities.LogInformationMessage("Unable to make portal request for server: " + i_p + ": " + func);
                        isSuccess = false;
                    }
                }
            }
            catch (Exception e)
            {
                Utilities.LogInformationMessage("Unable to make portal request for server: " + func);
                isSuccess = false;
            }

            return isSuccess;
        }
    }
}