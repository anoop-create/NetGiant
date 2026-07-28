using ngBatchProcesses.BusinessObjects.Axis;
using ngBatchProcesses.BusinessObjects.DataFeeds;
using ngBatchProcesses.BusinessObjects.EcommerceWebsite;
using ngBatchProcesses.BusinessObjects.FileSystem;
using ngBatchProcesses.BusinessObjects.PricingEngine;
using ngBatchProcesses.BusinessObjects.Provider;
using System.Collections.Generic;
using System.Linq;
using netGiant.Intranet.DataLayer.CustomerData;
using ngBatchProcesses.BusinessObjects.Searching;
using ngBatchProcesses.BusinessObjects.Apis;
using System.Configuration;
using System;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using NGBP.DataAccessLayer.DataUtilities;
using System.Collections.Specialized;

namespace ngBatchProcesses.BusinessObjects.Shared
{
    public static class SwitchDetection
    {
        internal static async void DetectSwitch(string[] args)
        {
            StandardFunctions.SetGlobalUserVariables();
            StandardFunctions.SetPropertySettings();
            Dictionary<string, string> parms = loadParms(args);

            //// Use the following when remote debugging on beta / live
            if (parms.ContainsKey("debug"))
            {
                Console.WriteLine("Waiting for debugger to attach");
                while (!Debugger.IsAttached)
                {
                    Thread.Sleep(100);
                }
                Console.WriteLine("Debugger attached");
            }
            //// End debugging

            switch (parms["type"])
            {
                //*** START TEMP SWITCHES
                case "movefiles":
                    // sample: /t movefiles
                    StandardFunctions.WriteProcessStarted();
                    FileFunctions.MoveFiles(parms);
                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
                    break;
                case "storemedia":
                    FileFunctions.StoreMedia();
                    break;
                //case "test":
                //    StandardFunctions.WriteProcessStarted();

                //    RunSP.ExecuteStoredProcedure(parms);

                //    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
                //    break;
                //*** END TEMP SWITCHES

                // case statements in alphabetical order, please. ALWAYS provide sample code.
                case "archivedirectory":
                    // sample: /t archivedirectory /i D:\DeliveryTracking\Archive\ /o D:\DeliveryTracking\Archive\
                    StandardFunctions.WriteProcessStarted();
                    StandardFunctions stf= new StandardFunctions();
                    stf.ArchiveFile(parms["input"], parms["output"] + DateTime.Now.ToString("dd_MM_yyyy_H_mm_ss"));
                    stf.CleanupArchiveLocation(parms["output"]);
                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
                    break;
                case "axisproductids":
                    LoadAXISProductIds prodIds = new LoadAXISProductIds(parms);
                    prodIds.UpdateProductIds();
                    break;
                case "apppoolcheck":
                    var iis = new IISUtilities(parms);
                    iis.Check_Restart();
                    break;
                case "bulkemail":
                    // sample: /t bulkemail /s delayed
                    // sample: /t bulkemail /s backorders
                    var be = new BulkEmail(parms);
                    be.Process();
                    break;
                case "clearserverfiles":
                    ServerFiles.ClearServerFiles(parms);
                    break;
                case "copydirectory":
                    FileFunctions.CopyDirectory(parms);
                    break;
                case "copycnetdata":
                    // NO LONGER IN USE
                    //DataSuppliers.CopyCNETData(parms["type"]);
                    break;
                case "copyfile":
                    StandardFunctions sf = new StandardFunctions();
                    sf.CopyFile(parms);
                    break;
                case "createelasticindexes":
                    // sample: /t createelasticindexes
                    var elastic = new ElasticSearch();
                    elastic.Build();
                    break;
                case "createelasticportalindex":
                    // NO LONGER IN USE
                    var elasticPortal = new ElasticPortalSearch();
                    elasticPortal.CreateIndex();
                    break;
                case "createluceneindexes":
                    // NO LONGER IN USE
                    var li = new LuceneIndex();
                    li.CreateLuceneIndexes(parms);
                    break;
                case "createsitemaps":
                    SiteMaps.CreateSiteMaps(parms);
                    break;
                case "createstaticfile":
                    // sample: /t createstaticfile /s newmastheadmenu /ws 1 /o "C:\\zz\\"
                    StaticFiles.Control(parms);
                    break;
                case "deleteinvoicefiles":
                    // sample: /t deleteinvoicefiles /ws 1 /p 1000 /n 1 /fp "D:\IIS-Content-VPC\beta.tonergiant.co.uk\media\archive"
                    // sample: /t deleteinvoicefiles /ws 1 /p 1000 /n 2 /fp "D:\IIS-Content-VPC\www.cartridgemonkey.com\media\archive"
                    FileFunctions.DeleteInvoiceFiles(parms);
                    break;
                case "downloadjsfiles":
                    DownloadJSFiles.ProcessFiles(parms);
                    break;
                case "emailtest":
                    // sample: /t emailtest /o "glen.dale130@gmail.com" /fila "C:\Program Files\NetGiant\NG Batch\EntityFramework.xml"
                    try
                    {
                        Email.EmailTest(parms);
                    }
                    catch (Exception ex)
                    {
                        StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR Unable to send email", ErrorCode = "ERROR" });
                        StandardFunctions.WriteException(ex);
                    }
                    break;
                case "facebookfeed":
                    FacebookFeed fbfeed = new FacebookFeed(parms);
                    fbfeed.CreateProductFeed();
                    break;
                case "feefofeed":
                    // sample: /t feefofeed /s TG /o "C:\ZZ\TG-FeeFoFeed.txt" /ws 1
                    // sample: /t feefofeed /s CM /o "C:\ZZ\CM-FeeFoFeed.txt" /ws 2
                    var rr1 = new ReviewRequest(parms);
                    rr1.ProcessReviewRequests();
                    break;
                case "flushcache":
                    // sample: /t flushcache
                    StandardFunctions.WriteProcessStarted();
                    DataCache cache = new DataCache(1);
                    cache.ClearCache();
                    cache = new DataCache(2);
                    cache.ClearCache();
                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
                    break;
                case "ftpfile":
                    // sample: /t ftpfile /s usessl /i "D:\FTP\ProductFeeds\TG-SkuuudleFeed.csv" /o "zTG-SkuuudleFeed.csv" /fs "[ftpSiteName]" /fu "net_giant" /fpw "Innovation2020" /fp "Feed"
                    StandardFunctions.WriteProcessStarted();
                    StandardFunctions.FTPFile(parms);
                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
                    break;
                //case "uploadftp":
                //    // sample: /t ftpfile /i "D:\FTP\ProductFeeds\TG-SkuuudleFeed.csv" /o "zTG-SkuuudleFeed.csv" /fs "[ftpSiteName]" /fu "net_giant" /fpw "Innovation2020" /fp "Feed"
                //    StandardFunctions.WriteProcessStarted();
                //    StandardFunctions.FTPFile(parms);
                //    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" });
                //    break;
                case "freshrelevancefeeds":
                    // NO LONGER IN USE
                    //var frf = new FreshRelevanceFeed(parms);
                    //frf.GenerateFeeds();
                    break;
                case "genequipmenttext":
                    // NO LONGER IN USE
                    //RandomText.GenerateEquipmentText(parms);
                    break;
                case "generateprdgrpsxml":
                    // NO LONGER IN USE
                    //StandardFunctions.GeneratePrdGrpXMLs(parms["subtype"]);
                    break;
                case "genevofeed":
                    // NO LONGER IN USE
                    //WizardFeed.ProcessEvoFeed(parms);
                    break;
                case "genpriceologyfeeds":
                    var feeds = new PriceologyFeeds(parms);
                    feeds.Generate();
                    break;
                case "genprodgroupxml":
                    // NO LONGER IN USE
                    //ProductGridXML prdGrid = new ProductGridXML();
                    //prdGrid.BuildProductGroupsXML(parms);
                    break;
                case "genproducttext":
                    // NO LONGER IN USE
                    //RandomText.GenerateProductText(parms);
                    break;
                case "googleshoppingfeed":
                    // sample: /t googleshoppingfeed /ws 1 /o "C:\ZZ\TG-GoogleFeed"
                    GoogleShoppingFeed.ProcessFeed(parms);
                    break;
                case "insertpriceologyprices":
                    Priceology.InsertPrices(parms);
                    break;                
                case "loadbackorders":
                    // sample: /t loadbackorders
                    var bo = new BackOrderFeed(parms);
                    bo.LoadData();
                    bo.SetStatus();
                    break;
                case "loadfeefodata":
                    // sample: /t loadfeefodata /ws 1 /p 1
                    var rr2 = new ReviewRequest(parms);
                    rr2.LoadData();
                    break;
                case "loadicecat":
                    // sample: /t loadicecat /n 100
                    // sample: /t loadicecat /n 100 /a truncate
                    // sample: /t loadicecat /n 100 /test
                    // sample: /t loadicecat /n 100 /a imagesonly
                    var ic = new IceCat(parms);
                    var t = ic.LoadData();
                    t.Wait();
                    break;
                case "loadinterimorders":
                    // sample: /t loadinterimorders /ws 1
                    //new InterimOrders(parms).LoadInterimOrders();
                    var io = new InterimOrders(parms);
                    io.LoadInterimOrders();
                    break;
                case "loadmailinglist":
                    // sample: /t loadmailinglist /ws 1
                    MailingLists ml = new MailingLists();
                    ml.LoadList(parms);
                    break;
                case "loadopenrange":
                    // sample: /t loadopenrange /s incremental /a day
                    // sample: /t loadopenrange /s incremental /a month
                    // sample: /t loadopenrange /s full /a week
                    // sample: /t loadopenrange /s full
                    DataSuppliers.UpdateOpenRange(parms["subtype"]);
                    break;
                case "iisloganalysis":
                    // sample: /t iisloganalysis
                    IISLog l = new IISLog();
                    l.Analyse();
                    break;
                case "mailchimp":
                    // sample: /t mailchimp /s product /a add /ws 1
                    // sample: /t mailchimp /s product /a delete /ws 1
                    // sample: /t mailchimp /s cart /a delete /ws 1 /p 3
                    // sample: /t mailchimp /s order /a load /ws 1 /p "2018-09-11 00:00:00"
                    // sample: /t mailchimp /s order /a add /ws 1 /p 1
                    // sample: /t mailchimp /s order /a lapsed /ws 1 /p 180 /wh 1
                    // sample: /t mailchimp /s order /a predict /ws 1 /p 21 
                    // sample: /t mailchimp /s order /a delete /ws 1
                    // sample: /t mailchimp /s customer /a delete /ws 1 /wh stuart.deavall@netgiant.com
                    // sample: /t mailchimp /s list /ws 1
                    var mc = new MailChimpFeed(parms);
                    mc.ProcessFeed();
                    break;
                //case "mergefiles":
                //    var fileFunc = new FileFunctions(parms);
                //    fileFunc.MergeSpicersCsvFiles();
                //    break;
                case "notify":
                    // sample: /t notify
                    Notifications notify = new Notifications(parms);
                    notify.Notify();
                    break;
                case "orderfeed":
                    OrderFeed feed = new OrderFeed(parms);
                    feed.Generate();
                    break;
                case "orcopyimage":
                    // sample: /t orcopyimage
                    Pimberly.CopyImages();
                    break;
                case "pimberly":
                    // sample: /t pimberly /s changes /a daily
                    // sample: /t pimberly /s changes /a weekly
                    // sample: /t pimberly /s complete
                    // sample: /t pimberly /s feed /fs "ftp://cloudpim.exavault.com" /fp "/Import File/" /fu "netgiant" /fpw "L8CknCmeyPSv"
                    // sample: /t pimberly /s feed /sk yes
                    var pim = new Pimberly(parms);
                    pim.ProcessFeed();
                    break;
                case "processaxisqueuev2":
                    // sample: /t processaxisqueuev2
                    XMLFeedV2 xf = new XMLFeedV2(parms);
                    xf.ProcessAxisQueue();
                    //XMLFeedV2.ProcessAxisQueue();
                    break;
                case "processequipmentfeed":
                    EquipmentFeeds.ProcessFeed(parms);
                    break;
                case "processtrackingemails":
                    // sample: /t processtrackingemails
                    // sample: /t processtrackingemails /a bypassfilechecks
                    ProcessTrackingEmails pte = new ProcessTrackingEmails(parms);
                    pte.Process();
                    break;
                case "processfeed":
                    ProductFeeds.ProcessFeed(parms);
                    break;
                case "refreshsite":
                    FileFunctions.RefreshSite(parms);
                    break;
                case "refreshsitenew":
                    FileFunctions.RefreshSite(parms);
                    break;
                case "runsp":
                    RunSP.ExecuteStoredProcedure(parms);
                    break;
                case "salesforce":
                    // sample: /t salesforce /s orders /a load /dt 2019/11/29 
                    // sample: /t salesforce /s orderlines /a load /dt 2019/11/29 
                    // sample: /t salesforce /s accounts /a load /dt 2019/11/29
                    // sample: /t salesforce /s accounts /a load /id CreditApp /dt 2019/09/29
                    // sample: /t salesforce /s contacts /a load /dt 2019/11/29 
                    // sample: /t salesforce /s products /a load /dt 2019/11/29 
                    // sample: /t salesforce /s delete /tb contact /id 0033N000007jmT4QAI
                    // sample: /t salesforce /s all /a load /dt 2019/11/29 
                    // sample: /t salesforce /s updatestats
                    var sf1 = new SalesforceFeed(parms);
                    sf1.ProcessFeed();
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
                case "stannp":
                    // sample: /t stannp /s newcustomer /p 7 /n 3 /ws 1
                    // sample: /t stannp /s retention /p 30 /n 1 /ws 1
                    // sample: /t stannp /s newcustomer2 /p 0 /n 1 /ws 1 /test
                    // sample: /t stannp /s maintainlist /ws 1 /id 41031
                    var s = new StannpFeed(parms);
                    s.ProcessFeed();
                    break;
                //case "tradesupply":
                //    // NO LONGER IN USE
                //    TradeSupplyTrackingReport.GetTradeSupplyTrackingReport(parms);
                //    break;
                case "updateproviderinventory":
                    // sample: /t updateproviderinventory /s 0 /o v2 /i 2
                    // sample: /t updateproviderinventory /s 42 /o v2 /i 2
                    ProviderInventory.PopulateProviderInventory(parms);
                    break;
                case "watchdirectory":
                    Watcher.WatchFolder(parms["input"]);
                    break;

                default:
                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "ERROR: No valid type parameter passed", ErrorCode = "ERROR" });
                    break;
            }
        }

        /// <summary>
        /// Builds the parameter dictionary
        /// </summary>
        /// <param name="args">The arguments passed in</param>
        private static Dictionary<string, string> loadParms(string[] args)
        {
            //  /a      =  action
            //  /fblog  =  blogfile
            //  /fcat   =  catfile
            //  /dt     =  date
            //  /db     =  dbname
            //  /d      =  delete
            //  /debug  =  enable remote debugging
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
            //  /id     =  id
            //  /i      =  input
            //  /n      =  number
            //  /o      =  output
            //  /p      =  period
            //  /fprod  =  prodfile
            //  /spp    =  spparams
            //  /s      =  subtype
            //  /sk     =  skip
            //  /tb     =  table
            //  /t      =  type
            //  /test   = testmode
            //  /wspath =  websitepath
            //  /ws     =  websiteid
            //  /wh     =  where
            //  /sid    =  siteid
            //  /ssl    =  sslid

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

                        case "a":
                            parms.Add("action", args[i + 1]);
                            break;
                        case "bm":
                            parms.Add("bypassmessages", args[i + 1]);
                            break;
                        case "d":
                            parms.Add("delete", args[i + 1]);
                            break;
                        case "db":
                            parms.Add("dbname", args[i + 1]);
                            break;
                        case "debug":
                            parms.Add("debug", "true");
                            break;
                        case "dt":
                            parms.Add("date", args[i + 1]);
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
                        case "id":
                            parms.Add("id", args[i + 1]);
                            break;
                        case "i":
                            parms.Add("input", args[i + 1]);
                            break;
                        case "n":
                            parms.Add("number", args[i + 1]);
                            break;
                        case "o":
                            parms.Add("output", args[i + 1]);
                            break;
                        case "p":
                            parms.Add("period", args[i + 1]);
                            break;
                        case "sid":
                            parms.Add("siteid", args[i + 1]);
                            break;
                        case "sk":
                            parms.Add("skip", args[i + 1]);
                            break;
                        case "spp":
                            parms.Add("spparams", args[i + 1]);
                            break;
                        case "ssl":
                            parms.Add("sslid", args[i + 1]);
                            break;
                        case "test":
                            parms.Add("testmode", "true");
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

            // Make parms globally accessable
            foreach (KeyValuePair<string, string> item in parms)
            {
                Global.Variable.Add(item.Key, item.Value);
            }
            Global.Variable.Add("command", String.Join(" ", args));
            Global.Variable.Add("BatchLogId", "0");
            if (!Global.Variable.ContainsKey("bypassmessages"))
            {
                Global.Variable.Add("bypassmessages", "0");
            }

            return parms;
        }
    }
}
