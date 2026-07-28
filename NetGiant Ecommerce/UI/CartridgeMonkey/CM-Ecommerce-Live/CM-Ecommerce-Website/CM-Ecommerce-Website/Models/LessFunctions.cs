using dotless.Core.Parser.Functions;
using dotless.Core.Parser.Infrastructure;
using dotless.Core.Parser.Infrastructure.Nodes;
using dotless.Core.Parser.Tree;
using dotless.Core.Plugins;
using dotless.Core.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using BusinessLogic;
using DataAccess.EntityFramework;

namespace CM_Ecommerce_Website.Models
{
    [DisplayName("LessPlugins")]
    public class LessFunctions : IFunctionPlugin
    {
        public Dictionary<string, Type> GetFunctions()
        {
            return new Dictionary<string, Type>
            {
                { "getCdn", typeof(GetCdnFunction) }
            };
        }
    }

    public class GetCdnFunction : Function
    {
        protected override Node Evaluate(Env env)
        {
            Guard.ExpectMinArguments(0, Arguments.Count(), this, Location);
            List<configurationSetting> lcs = EntityAccess.ReadConfigurationSetting(x => x.sectionName == "Website Application Variables" && (x.settingName == "CDN" || x.settingName == "VersionNumber"));
            string versionNo = lcs.Find(x => x.settingName == "VersionNumber").settingValue.ToString();
            string cdn = lcs.Find(x => x.settingName == "CDN").settingValue.ToString().Replace("[version]", versionNo);

            return new Keyword(cdn);

            //if (ConfigurationManager.AppSettings["Environment"] == "Live")
            //{
            //    return new Keyword("//" + ConfigurationManager.AppSettings["DomainName_Live"] + "cdn");
            //}
            //else
            //{
            //    return new Keyword("//" + ConfigurationManager.AppSettings["DomainName_Dev"] + "cdn");
            //}
        }
    }
}