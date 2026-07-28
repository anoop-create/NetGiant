using ngBatchProcesses.BusinessObjects.Axis;
using ngBatchProcesses.BusinessObjects.DataFeeds;
using ngBatchProcesses.BusinessObjects.EcommerceWebsite;
using ngBatchProcesses.BusinessObjects.FileSystem;
using ngBatchProcesses.BusinessObjects.PricingEngine;
using ngBatchProcesses.BusinessObjects.Provider;
using System.Collections.Generic;
using System.Linq;
using ngBatchProcesses.BusinessObjects.Searching;

namespace ngBatchProcesses.BusinessObjects.Shared
{
    public static class SwitchDetection
    {
        internal static void DetectSwitch(string[] args)
        {
            StandardFunctions.SetGlobalUserVariables();
            StandardFunctions.SetPropertySettings();
            Dictionary<string, string> parms = loadParms(args);

            switch (parms["type"])
            {
                case "axisproductids":
                    LoadAXISProductIds prodIds = new LoadAXISProductIds(parms);
                    prodIds.UpdateProductIds();
                    break;
                case "clearserverfiles":
                    ServerFiles.ClearServerFiles(parms);
                    break;
                case "copydirectory":
                    FileFunctions.CopyDirectory(parms, null);
                    break;
                case "copycnetdata":
                    DataSuppliers.CopyCNETData(parms["type"]);
                    break;
                case "copyfile":
                    StandardFunctions sf = new StandardFunctions();
                    sf.CopyFile(parms);
                    break;
                case "createelasticindexes":
                    var elastic = new ElasticSearch();
                    elastic.Build();
                    break;
                case "createelasticportalindex":
                    var elasticPortal = new ElasticPortalSearch();
                    elasticPortal.CreateIndex();
                    break;
                case "createluceneindexes":
                    var li = new LuceneIndex();
                    li.CreateLuceneIndexes(parms);
                    break;
                case "createsitemaps":
                    SiteMaps.CreateSiteMaps(parms);
                    break;
                case "createstaticfile":
                    StaticFiles.Control(parms);
                    break;
                case "downloadjsfiles":
                    DownloadJSFiles.ProcessFiles(parms);
                    break;
                case "feefofeed":
                    FeeFoFeed.ProcessFeeFo(parms);
                    break;
                case "ftpfile":
                    StandardFunctions.FTPFile(parms);
                    break;
                case "freshrelevancefeeds":
                    var frf = new FreshRelevanceFeed(parms);
                    frf.GenerateFeeds();
                    break;
                case "genequipmenttext":
                    RandomText.GenerateEquipmentText(parms);
                    break;
                case "generateprdgrpsxml":
                    StandardFunctions.GeneratePrdGrpXMLs(parms["subtype"]);
                    break;
                case "genevofeed":
                    WizardFeed.ProcessEvoFeed(parms);
                    break;
                case "genpriceologyfeeds":
                    var feeds = new PriceologyFeeds(parms);
                    feeds.Generate();
                    break;
                case "genprodgroupxml":
                    ProductGridXML prdGrid = new ProductGridXML();
                    prdGrid.BuildProductGroupsXML(parms);
                    break;
                case "genproducttext":
                    RandomText.GenerateProductText(parms);
                    break;
                case "googleshoppingfeed":
                    GoogleShoppingFeed.ProcessFeed(parms);
                    break;
                case "insertpriceologyprices":
                    Priceology.InsertPrices(parms);
                    break;
                case "loadfeefodata":
                    // sample: /t loadfeefodata /ws 1 /p 1
                    FeeFoFeed.LoadData(parms);
                    break;
                case "loadopenrange":
                    DataSuppliers.UpdateOpenRange(parms["subtype"]);
                    break;
                case "mergefiles":
                    var fileFunc = new FileFunctions(parms);
                    fileFunc.MergeSpicersCsvFiles();
                    break;
                case "orderfeed":
                    OrderFeed feed = new OrderFeed(parms);
                    feed.Generate();
                    break;
                case "processaxisqueuev2":
                    XMLFeedV2.ProcessAxisQueue();
                    break;
                case "processequipmentfeed":
                    EquipmentFeeds.ProcessFeed(parms);
                    break;
                case "processtrackingemails":
                    ProcessTrackingEmails.Process();
                    break;
                case "processfeed":
                    ProductFeeds.ProcessFeed(parms);
                    break;
                case "refreshsite":
                    FileFunctions.RefreshSite(parms);
                    break;
                case "runsp":
                    RunSP.ExecuteStoredProcedure(parms);
                    break;
                case "saleshistoryfeed":
                    var salesFeed = new SalesHistoryFeed(parms);
                    salesFeed.Generate();
                    break;
                case "skuuudlefeed":
                    SkuuudleFeed.ProcessFeed(parms);
                    break;
                case "slifeed":
                    SLIFeed.CreateSliFeedXml(parms);
                    break;
                case "updateproviderinventory":
                    ProviderInventory.PopulateProviderInventory(parms);
                    break;
                case "watchdirectory":
                    Watcher.WatchFolder(parms["input"]);
                    break;
            }
        }

        /// <summary>
        /// Builds the parameter dictionary
        /// </summary>
        /// <param name="args">The arguments passed in</param>
        private static Dictionary<string, string> loadParms(string[] args)
        {
            //  /fblog  =  blogfile
            //  /fcat   =  catfile
            //  /db     =  dbname
            //  /d      =  delete
            //  /feq    =  equipfile
            //  /fd     =  field
            //  /fila   =  filea
            //  /filb   =  fileb
            //  /filc   =  filec
            //  /fp     =  ftppath, filepath
            //  /fpw    =  ftppassword
            //  /fpwo   =  ftpoutputpassword
            //  /fs     =  ftpsite
            //  /fso    =  ftpoutputsite
            //  /fu     =  ftpusername
            //  /fuo    =  ftpoutputusername
            //  /i      =  input
            //  /o      =  output
            //  /p      =  period
            //  /fprod  =  prodfile
            //  /spp    =  spparams
            //  /s      =  subtype
            //  /tb     =  table
            //  /t      =  type
            //  /wspath =  websitepath
            //  /ws     =  websiteid
            //  /wh     =  where

            Dictionary<string, string> parms = new Dictionary<string, string>();

            for (int i = 0; i < args.Count(); i++)
            {
                if (args[i].StartsWith("/"))
                {
                    switch (args[i].Substring(1))
                    //alphabetical order please
                    {
                        case "t":
                            parms.Add("type", args[i + 1].ToLower());
                            break;
                        case "s":
                            parms.Add("subtype", args[i + 1].ToLower());
                            break;
                        case "d":
                            parms.Add("delete", args[i + 1]);
                            break;
                        case "db":
                            parms.Add("dbname", args[i + 1]);
                            break;
                        case "fblog":
                            parms.Add("blogfile", args[i + 1]);
                            break;
                        case "fd":
                            parms.Add("field", args[i + 1]);
                            break;
                        case "fcat":
                            parms.Add("catfile", args[i + 1]);
                            break;
                        case "feq":
                            parms.Add("equipfile", args[i + 1]);
                            break;
                        case "fila":
                            parms.Add("filea", args[i + 1]);
                            break;
                        case "filb":
                            parms.Add("fileb", args[i + 1]);
                            break;
                        case "filc":
                            parms.Add("filec", args[i + 1]);
                            break;
                        case "fp":
                            parms.Add("ftppath", args[i + 1]);
                            parms.Add("filepath", args[i + 1]);
                            break;
                        case "fprod":
                            parms.Add("prodfile", args[i + 1]);
                            break;
                        case "fpw":
                            parms.Add("ftppassword", args[i + 1]);
                            break;
                        case "fpwo":
                            parms.Add("ftpoutputpassword", args[i + 1]);
                            break;
                        case "fs":
                            parms.Add("ftpsite", args[i + 1]);
                            break;
                        case "fso":
                            parms.Add("ftpoutputsite", args[i + 1]);
                            break;
                        case "fu":
                            parms.Add("ftpusername", args[i + 1]);
                            break;
                        case "fuo":
                            parms.Add("ftpoutputusername", args[i + 1]);
                            break;
                        case "i":
                            parms.Add("input", args[i + 1]);
                            break;
                        case "o":
                            parms.Add("output", args[i + 1]);
                            break;
                        case "p":
                            parms.Add("period", args[i + 1]);
                            break;
                        case "spp":
                            parms.Add("spparams", args[i + 1]);
                            break;
                        case "tb":
                            parms.Add("table", args[i + 1]);
                            break;
                        case "wspath":
                            parms.Add("websitepath", args[i + 1]);
                            break;                        
                        case "ws":
                            parms.Add("websiteid", args[i + 1]);
                            break;
                        case "wh":
                            parms.Add("where", args[i + 1]);
                            break;
                    }
                }
            }
            return parms;
        }
    }
}
