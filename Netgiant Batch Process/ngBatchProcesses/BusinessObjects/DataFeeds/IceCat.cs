using netGiant.Intranet.DataLayer.NetgiantMasterData;
using Newtonsoft.Json.Linq;
using ngBatchProcesses.BusinessObjects.Shared;
using NGBP.DataAccessLayer.DataUtilities;
using RestSharp;
using RestSharp.Authenticators;
using RestSharp.Serializers.NewtonsoftJson;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;

namespace ngBatchProcesses.BusinessObjects.DataFeeds
{
    public class IceCat
    {
        public IceCat(Dictionary<string, string> parms)
        {
            StandardFunctions.WriteProcessStarted();
            Parms = parms;
            InTestMode = Parms.ContainsKey("testmode");
            if (Parms.ContainsKey("action"))
            {
                Action = Parms["action"];
            }
            if (Parms.ContainsKey("number"))
            {
                BatchSize = Int32.Parse(Parms["number"]);
            }
            Url = EntityFunctions.GetConfigurationSettings(x => x.sectionName == "BatchProgram" && x.settingName == "IceCatURL").FirstOrDefault().settingValue;
            string si = EntityFunctions.GetConfigurationSettings(x => x.sectionName == "BatchProgram" && x.settingName == "IceCatImageSuppression").FirstOrDefault().settingValue;
            SuppressedImages = si.Split(',').ToList();
            Account = EntityFunctions.GetConfigurationSettings(x => x.sectionName == "BatchProgram" && x.settingName == "IceCatAccount").FirstOrDefault().settingValue;
            if (Action == "truncate")
            {
                int i = EntityFunctions.TruncateTable("IcImage");
                StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "IcImage table truncated" });
            }


            // Testing only
            if (Properties.Settings.Default.Environment == "Local")
            {
                ImageRoot = "C:\\zz\\icImages\\";
            }
        }

        public Dictionary<string, string> Parms { get; set; }
        public bool ErrorOccured { get; set; } = false;
        public bool InTestMode { get; set; } = false;
        public string Action { get; set; }
        public int BatchSize { get; set; } = 1000;
        public List<product> ProductList { get; set; }
        public int AddedCount { get; set; } = 0;
        public int UpdatedCount { get; set; } = 0;
        public int FailureCount { get; set; } = 0;
        public int ImageSuccessCount { get; set; } = 0;
        public int ImageFailureCount { get; set; } = 0;
        private List<Results> Res { get; set; } = new List<Results>();
        private List<int> LManu { get; set; } = new List<int>();
        private int ActiveStatus { get; set; }
        private int AlertStatus { get; set; }
        private string Url { get; set; }
        private List<string> SuppressedImages { get; set; }
        private string ImageRoot { get; set; } = Properties.Settings.Default.LocalDirectory + "IIS-Content-VPC\\netgiant_files\\stock-icecat\\";
        private string Account { get; set; }
        private static readonly HttpClient _HttpClient = new HttpClient();
        public async Task LoadData()
        {
            ActiveStatus = EntityFunctions.GetNgmdLookup(x => x.LookupType.LookupTypeName == "ProductStatus" && x.LookupName == "Active").FirstOrDefault().AltLookupId.Value;
            AlertStatus = EntityFunctions.GetNgmdLookup(x => x.LookupType.LookupTypeName == "ProductStatus" && x.LookupName == "Alert").FirstOrDefault().AltLookupId.Value;
            GetManufacturerList();

            ProductList = EntityFunctions.GetProduct(x => (x.productStatusFK == ActiveStatus || x.productStatusFK == AlertStatus) && LManu.Contains((int)x.manufacturerFK))
                    //.Where(x => x.productID == 6785)
                    //.Take(2000)
                    .OrderBy(x => x.manufacturerFK).ThenBy(x => x.partNo)
                    .ToList();

            StandardFunctions.SetTlsVersion();
            StandardFunctions.CWrite("Start Processing");
            var tasks = new List<Task>();
            int counter = 0;
            foreach (product p in ProductList)
            {
                counter++;
                if (counter > BatchSize)    // Process asynchronously in batches so that the IceCat server is not overloaded
                {
                    Task t = Task.WhenAll(tasks);
                    try
                    {
                        await t;
                    }
                    catch { }
                    StandardFunctions.CWrite("New Batch");
                    tasks = new List<Task>();
                    counter = 1;
                }
                tasks.Add(ProcessProductAsync(p));
            }
            Task tt = Task.WhenAll(tasks);
            try
            {
                await tt;
            }
            catch { }

            if (InTestMode)
            {
                WriteCsv();
            }
            StandardFunctions.CWrite("End Processing");     // Local only
            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Added Count: " + AddedCount.ToString() + ". Updated Count: " + UpdatedCount.ToString() + ". Failure Count: " + FailureCount.ToString() + "." });
            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Images Downloaded: Success Count: " + ImageSuccessCount.ToString() + ". Failed Count: " + ImageFailureCount.ToString() + "." });
            StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Process completed" }); 
            StandardFunctions.CReadKey();                   // Local only

            return;
        }

        private async Task ProcessProductAsync(product p)
        {
            StandardFunctions.CWrite("Start Processing: " + p.partNo);
            // Get IceCat Data
            RestClient client = new RestClient(Url, configureSerialization: s => s.UseNewtonsoftJson());
            string url =
               "?UserName=" + Account +
               "&Language=en" +
               "&Brand=" + p.manufacturer.manufacturerName +
               "&ProductCode=" + HttpUtility.UrlEncode(p.partNo) +
               "&Content=Gallery";
            var request = new RestRequest(url, RestSharp.Method.Get);
            RestResponse response = await client.ExecuteAsync(request, RestSharp.Method.Get);

            JObject results;

            if (InTestMode)
            {
                WriteResult(p, response, url);
            }
            if (response.StatusCode == HttpStatusCode.OK)
            {
                results = JObject.Parse(response.Content);
            }
            else
            {
                StandardFunctions.CWrite("No images found for " + p.manufacturer.manufacturerName + ", " + p.partNo);
                FailureCount++;
                return;
            }

            // Process Results
            if (results["data"] != null)
            {
                if (results["data"]["Gallery"] != null)
                {
                    for (int i = 0; i < results["data"]["Gallery"].Count(); i++)
                    {
                        JObject gItem;
                        gItem = JObject.Parse(results["data"]["Gallery"][i].ToString());
                        string gItemType = gItem["Type"].ToString();
                        if (gItemType.StartsWith("ProductImage") && !gItemType.Contains("Annotated"))
                        {
                            string medImageUri;
                            medImageUri = gItem["Pic"].ToString() ?? null;

                            string filename = medImageUri.Split('/').LastOrDefault();
                            if (SuppressedImages.Contains(filename))
                            {
                                continue;
                            }

                            if (Action != "imagesonly")
                            {
                                using (var db = new ngmdEntities())
                                {

                                    int iceCatID = Int32.Parse(gItem["ID"].ToString());
                                    IcImage ici = db.IcImage.FirstOrDefault(x => x.IcId == iceCatID) ?? new IcImage() { IcId = iceCatID };

                                    ici.ProductFk = p.productID;
                                    ici.IsMain = gItem["IsMain"].ToString() == "Y" ? true : false;
                                    ici.Thumb = "Images/stock-icecat" + gItem["ThumbPic"].ToString().Replace("https://images.icecat.biz/img", "").Replace("https://images.icecat.biz/thumbs", "") ?? null;
                                    ici.Low = "Images/stock-icecat" + gItem["LowPic"].ToString().Replace("https://images.icecat.biz/img", "") ?? null;
                                    ici.Med = "Images/stock-icecat" + gItem["Pic"].ToString().Replace("https://images.icecat.biz/img", "") ?? null;
                                    ici.High = "Images/stock-icecat" + gItem["Pic500x500"].ToString().Replace("https://images.icecat.biz/img", "") ?? null;

                                    try
                                    {
                                        if (ici.IcImageId > 0)
                                        {
                                            db.Entry(ici).State = EntityState.Modified;
                                            UpdatedCount++;
                                        }
                                        else
                                        {
                                            db.Entry(ici).State = EntityState.Added;
                                            AddedCount++;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        StandardFunctions.WriteException(ex);
                                        StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Error updating IcImage table " + p.partNo });
                                    }
                                    finally
                                    {
                                        db.SaveChanges();
                                    }
                                }
                            }

                            // Now download the image to our server so that it can be served on the website without hotlinking to IceCat
                            if (!string.IsNullOrEmpty(medImageUri))
                            {
                                await DownloadImageAsync(medImageUri);
                            }
                        }
                    }
                }
            }
            StandardFunctions.CWrite("Ending Process " + p.partNo);
            return;
        }

        private async Task DownloadImageAsync(string imageUri)
        {
            string path = imageUri.Replace("https://images.icecat.biz/img/", "").Replace("/", "\\");
            string[] paths = imageUri.Split('/');

            if (!Directory.Exists(ImageRoot + path.Replace(paths[paths.Count() - 1], "")))
            {
                Directory.CreateDirectory(ImageRoot + path.Replace(paths[paths.Count() - 1], ""));
            }

            if (!File.Exists(ImageRoot + path))
            {
                try
                {
                    using (HttpResponseMessage response = await _HttpClient.GetAsync(imageUri))
                    {
                        response.EnsureSuccessStatusCode();
                        byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();
                        using (var fs = new FileStream(ImageRoot + path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
                        {
                            await fs.WriteAsync(imageBytes, 0, imageBytes.Length);
                        }
                        ImageSuccessCount++;
                    }
                }
                catch (Exception ex)
                {
                    StandardFunctions.WriteBatchLog(new BatchLogDetail() { Message = "Error downloading " + imageUri + ", " + ex.Message });
                    ImageFailureCount++;
                }
            }
        }

        private void WriteResult(product p, RestResponse response, string url)
        {
            Results r = new Results();
            r.Url = url;
            r.Manufacturer = p.manufacturer.manufacturerName ?? "";
            r.PartNo = p.partNo ?? "";
            r.StatusCode = response.StatusCode.ToString() ?? "";
            r.ErrorMessage = response.ErrorMessage ?? "";

            Res.Add(r);
        }

        private void WriteCsv()
        {
            string filePath = Properties.Settings.Default.LocalDirectory + "\\IceCatLoad_" + DateTime.Now.ToString("dd_MM_yyyy_H_mm_ss") + ".csv";

            using (CsvFileWriter writer = new CsvFileWriter(filePath, ','))
            {
                CsvRow newRow = new CsvRow();
                newRow.Add("URL");
                newRow.Add("Manufacturer");
                newRow.Add("PartNo");
                newRow.Add("StatusCode");
                newRow.Add("ErrorMessage");
                writer.WriteRow(newRow);

                int counter = 0;
                foreach (Results r in Res)
                {
                    newRow = new CsvRow();
                    if (r != null)
                    {
                        newRow.Add(r.Url);
                        newRow.Add(r.Manufacturer);
                        newRow.Add(r.PartNo);
                        newRow.Add(r.StatusCode);
                        newRow.Add(r.ErrorMessage);
                    }

                    writer.WriteRow(newRow);
                    counter++;
                }
            }
        }

        private void GetManufacturerList()
        {
            string s = EntityFunctions.GetConfigurationSettings(x => x.sectionName == "BatchProgram" && x.settingName == "IceCatManufacturers").FirstOrDefault()
                .settingValue;

            List<string> m = new List<string>();
            if (s == "ALL")
            {
                // All Manufacturers
                LManu = EntityFunctions.GetManufacturers(x => x.product.Any(y => y.productStatusFK == ActiveStatus || y.productStatusFK == AlertStatus) && x.manufacturerName != "Own Brand")
                    .Select(x => x.manufacturerID)
                    .ToList();
            }
            else
            {
                // Selected Manufacturers
                m = s.Split(',').ToList();
                LManu = EntityFunctions.GetManufacturers(x => m.Contains(x.manufacturerName))
                    .Select(x => x.manufacturerID)
                    .ToList();
            }
        }

        private class Results
        {
            public string Url { get; set; }
            public string Manufacturer { get; set; }
            public string PartNo { get; set; }
            public string StatusCode { get; set; }
            public string ErrorMessage { get; set; }
        }
    }
}
