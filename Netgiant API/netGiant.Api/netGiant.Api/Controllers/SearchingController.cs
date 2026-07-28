using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Web.Http.Cors;
using netGiant.Api.BusinessLayer.Searching;
using System.Net.Http;
using System.Web;
using System.Web.Hosting;

namespace netGiant.Api.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class SearchingController : ApiController
    {
        [HttpGet]
        public Search GenericSearch(string term, int t, int w)
        {
            Search src = new Search();
            src = src.SearchProductAndEquipment(HostingEnvironment.MapPath("~/Lucene/"),
                term,
                t,
                w,
                HttpContext.Current.Application["EquipmentManuList"]);
            
            return src;
        }
    }
}
