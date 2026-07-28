using netGiant.Api.BusinessLayer.Models;
using netGiant.Api.BusinessLayer.XML;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Hosting;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Xml.Linq;

namespace netGiant.Api.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class XMLController : ApiController
    {
        [HttpGet]
        public XmlModel GetUpdatedXML(string a, int w)
        {
            XmlModel model = new XmlModel();

            var sitesPath = Path.GetDirectoryName(Path.GetDirectoryName(HostingEnvironment.MapPath("~")));

            ProcessXML xml = new ProcessXML(a, w, sitesPath);
            model.ProductGridXML = xml.GetUpdatedXML().ToString(SaveOptions.DisableFormatting);

            return model;
        }
    }
}
