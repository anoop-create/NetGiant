using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ngSBSBatchProcesses.BusinessObjects.DataFeeds;
using System.Security.Principal;
using ngSBSBatchProcesses.BusinessObjects.Shared;

namespace ngSBSBatchProcesses
{
    class Program
    {
        static void Main(string[] args)
        {
            Program _program = new Program();
            _program.DetectSwitch(args);
        }

        private void DetectSwitch(string[] args)
        {
            StandardFunctions.SetGlobalUserVariables();
            Dictionary<string, string> parms = loadParms(args);

            switch (parms["type"])
            {
                case "feefofeed":
                    FeeFoFeed.ProcessFeeFo(parms);
                    break;
                case "axisproductids":
                    AXISProductId.WriteProductIds(parms);
                    break;
                case "createkpixml":
                    KPIXml.CreateXML(parms);
                    break;
                case "freshrelevancefeed":
                    var frf = new FreshRelevanceFeeds();
                    frf.GenerateFeeds(parms);
                    break;
                case "orderfeed":
                    OrderFeed feed = new OrderFeed(parms);
                    feed.Generate();
                    break;
                case "saleshistoryfeed":
                    var salesFeed = new SalesHistoryFeed(parms);
                    var timespan = parms["subtype"];
                    var period = parms["input"];
                    salesFeed.Generate(timespan, period);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Builds the parameter dictionary
        /// </summary>
        /// <param name="args">The arguments passed in</param>
        private Dictionary<string, string> loadParms(string[] args)
        {
            // /t = run type
            // /s = run sub type
            // /i = input
            // /o = output 
            // /fs = ftp site name
            // /fu = ftp username
            // /fpw = ftp password
            // /fp = ftp additional path

            Dictionary<string, string> parms = new Dictionary<string, string>();

            for (int i = 0; i < args.Count(); i++)
            {
                if (args[i].StartsWith("/"))
                {
                    switch (args[i].Substring(1))
                    {
                        case "t":
                            parms.Add("type", args[i + 1].ToLower());
                            break;
                        case "s":
                            parms.Add("subtype", args[i + 1].ToLower());
                            break;
                        case "i":
                            parms.Add("input", args[i + 1]);
                            break;
                        case "o":
                            parms.Add("output", args[i + 1]);
                            break;
                        case "fs":
                            parms.Add("ftpsite", args[i + 1]);
                            break;
                        case "fu":
                            parms.Add("ftpusername", args[i + 1]);
                            break;
                        case "fpw":
                            parms.Add("ftppassword", args[i + 1]);
                            break;
                        case "fp":
                            parms.Add("ftppath", args[i + 1]);
                            break;
                        default:
                            // unknown parameter
                            break;
                    }
                }
            }
            return parms;
        }
    }
}
