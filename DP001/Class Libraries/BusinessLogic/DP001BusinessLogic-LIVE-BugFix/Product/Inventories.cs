using DP001BusinessLogic.Shared;
using DP001DataAccess.Entities;
using DP001DataAccess.Utilities;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using MoreLinq;
using System.Data.SqlClient;

namespace DP001BusinessLogic
{
    public class Inventories
    {
        public Inventories(Dictionary<string, string> parms)
        {
            _suppliedParams = parms;
            InitializeTenant();
        }

        private Dictionary<string, string> _suppliedParams;
        private Tenant _tenant;
        private Channel _channel;
        private static List<Brand> _brandList;
        private static List<ProductCategory> _categoryList;
        private static List<MapBrandCategory> _brandCategoryList;
        private static List<ProductInventory> _productList;
        private static List<SupplierInventory> _supplierList;
        private static List<CompetitorInventory> _competitorInvList;
        private static List<Competitor> _competitorList;
        private static List<SupplierBrandMatching> _supplierBrandMatching;
        private static List<SupplierMfpnMatching> _supplierMfpnMatching;
        private static Lookup MfpnMatchingPrefixType;
        private static Lookup MfpnMatchingSuffixType;
        private static List<DownloadedFileData> _feedFiles;
        private static DateTime _processStartTime;

        enum FtpFileType
        {
            Supplier,
            Product,
            Competitor,
            SkuuudleSummary
        }

        public bool Populate()
        {
            var loadFeedsSuccess = true;

            CleanupStagingTables();
            CleanupNotifications();

            try
            {
                if (ValidateAndGetFeeds(_channel))
                {
                    _tenant.LoadProductInventory(_channel);
                    _tenant.LoadSupplierInventory(_channel);
                    LoadCompetitorDataFromFtp();
                    SetSkuMappings(_channel);
                }
                else
                {
                    CommonDataFunctions.CreateLogEntry(_channel, "One or more feed(s) failed validation check.", "Notification", true);
                    loadFeedsSuccess = false;
                }
            }
            catch (EmptyFeedException e)
            {
                CommonDataFunctions.CreateLogEntry(_channel, e.Message, "Notification", true);

                loadFeedsSuccess = false;
            }

            if (loadFeedsSuccess)
                CleanupFeeds(_feedFiles);

            return loadFeedsSuccess;
        }

        public static bool ValidateAndGetFeeds(Channel channel)
        {
            CommonDataFunctions.CreateLogEntry(channel, "START ValidateAndGetFeeds", "Information");

            var passValidation = true;

            try
            {
                _feedFiles.AddRange(GetFtpFiles(channel, FtpFileType.Product));
                _feedFiles.AddRange(GetFtpFiles(channel, FtpFileType.Supplier));
                _feedFiles.AddRange(GetFtpFiles(channel, FtpFileType.Competitor));

                foreach (var feed in _feedFiles)
                {
                    if (passValidation)
                    {
                        if (!feed.DownloadResult.IsSuccess)
                        {
                            passValidation = false;
                            break;
                        }
                        else
                        {
                            if (!FeedHasData(feed, channel))
                            {
                                passValidation = false;
                                CommonDataFunctions.CreateLogEntry(channel, "Feed: " + feed.Settings.FTPFileName +
                                    " is invalid. Processing stopped. No prices have been calculated or amended.",
                                    "Notification", true);
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                passValidation = false;
                CommonDataFunctions.CreateLogEntry(channel, "Feeds have failed validation checks. Error: " +
                    e.Message + " Stack: " + e.StackTrace, "Notification", true);
            }

            CommonDataFunctions.CreateLogEntry(channel, "END ValidateAndGetFeeds", "Information");

            return passValidation;
        }

        private static FtpDownloadResult ValidateSkuuudleSummary(FTPSetting setting, Channel channel)
        {
            var ftpHostDetails = new Ftp.FtpHostDetails()
            {
                BlobContainer = "tenantfolders",
                FileName = setting.FTPSummaryFileName,
                FtpHost = setting.FTPServer,
                FtpUser = setting.FTPUser,
                FtpPassword = setting.FTPPassword,
                FolderPath = setting.FTPPath,
                Protocol = CommonFunctions.LookupFtpProtocol(setting.FTPProtocolFK),
                SavePath = channel.TenantFK.ToString() + "\\" + setting.FTPSettingsID + "_" + setting.FTPSummaryFileName,
            };

            return Ftp.DownloadFTPFile(ftpHostDetails);
        }

        private static bool FeedHasData(DownloadedFileData feed, Channel channel)
        {
            var hasData = false;
            var fileContent = CommonFunctions.ReadFileToString(feed.DownloadResult.Path, feed.DownloadResult.BlobContainer);
            var isIndexBased = IsIndexBased(feed.Settings.FieldMapping);

            if (feed.FileType == FtpFileType.SkuuudleSummary)
            {
                isIndexBased = false;
            }

            var counter = 1;
            using (var csvReader = new TextFieldParser(new StringReader(fileContent)))
            {
                while (!csvReader.EndOfData && counter < 5)
                {
                    var currentLine = csvReader.ReadLine();

                    if (!isIndexBased && counter == 2)
                    {
                        if (!string.IsNullOrEmpty(currentLine))
                        {
                            hasData = true;
                            break;
                        }
                    }
                    else if (isIndexBased && counter == 1)
                    {
                        if (!string.IsNullOrEmpty(currentLine))
                        {
                            hasData = true;
                            break;
                        }
                    }

                    counter++;
                }
            }

            if (feed.FileType == FtpFileType.SkuuudleSummary)
            {
                var delimeter = "";

                using (var sr = new StringReader(fileContent))
                {
                    delimeter = Ftp.DetectDelimiter(sr, 1).ToString();
                }

                using (TextFieldParser csvReader = new TextFieldParser(new StringReader(fileContent)))
                {
                    csvReader.SetDelimiters(new string[] { delimeter });
                    csvReader.TrimWhiteSpace = true;

                    var headings = csvReader.ReadFields();

                    var indexList = new List<int>();
                    var competitorNameColumn = LookupFieldIndex(feed, headings, "Competitor name", indexList);
                    var averageRatingColumn = LookupFieldIndex(feed, headings, "Average rating", indexList);
                    var numberOfReviewsColumn = LookupFieldIndex(feed, headings, "No. of reviews (qty)", indexList);

                    if (!CheckValidColumns(indexList))
                    {
                        hasData = false;
                        CommonDataFunctions.CreateLogEntry(channel, "Feed: " + feed.Settings.FTPSummaryFileName +
                                    " is invalid. The headings could not be found. Processing stopped. No prices have been calculated or amended.",
                                    "Notification", true);
                    }
                }
            }

            return hasData;
        }

        public static bool LoadProductDataFromFtp(Channel channel)
        {
            CommonDataFunctions.CreateLogEntry(channel, "START ProcessProductFiles", "Information");

            var ftpFiles = _feedFiles.Where(x => x.Settings.Lookup.LookupName == "Product Inventory").ToList();

            _productList = new List<ProductInventory>();

            foreach (var downloadedFile in ftpFiles)
            {
                var productInventory = GetProductList(channel, downloadedFile);

                if (productInventory != null)
                {
                    if (productInventory.Count == 0)
                    {
                        throw new EmptyFeedException("Product file: " + downloadedFile.Settings.FTPFileName +
                            " Contains no data. Processing stopped. No prices have been calculated or amended.");
                    }
                }

                _productList.AddRange(productInventory);
                BuildBrandCategory(channel);
                _productList = TidyProductList(_productList);
            }

            CreateProductData(channel);

            CommonDataFunctions.CreateLogEntry(channel, "END ProcessProductFiles", "Information");

            return true;
        }

        public static bool LoadSupplierDataFromFtp(Channel channel)
        {
            CommonDataFunctions.CreateLogEntry(channel, "START ProcessSupplierFiles", "Information");

            var ftpFiles = _feedFiles.Where(x => x.Settings.Lookup.LookupName == "Supplier Inventory").ToList();

            _supplierList = new List<SupplierInventory>();

            foreach (var file in ftpFiles)
            {
                var supplier = LookupSupplier(channel, file);
                var supplierInventory = GetSupplierList(channel, file, supplier);

                if (supplierInventory != null)
                {
                    if (supplierInventory.Count == 0)
                    {
                        throw new EmptyFeedException("Supplier file: " + file.Settings.FTPFileName +
                            " Contains no data. Processing stopped. No prices have been calculated or amended.");
                    }
                }

                _supplierList.AddRange(supplierInventory);
                _supplierList = TidySupplierList(_supplierList, channel);
            }

            CreateSupplierData(channel);

            CommonDataFunctions.CreateLogEntry(channel, "END ProcessSupplierFiles", "Information");

            return true;
        }

        private static Supplier LookupSupplier(Channel channel, DownloadedFileData file)
        {
            var supplier = new Supplier();

            try
            {
                supplier = channel.Suppliers.Where(x => x.FTPSettingsFK == file.Settings.FTPSettingsID).FirstOrDefault();
            }
            catch (Exception e)
            {
                CommonDataFunctions.CreateLogEntry(channel, "Could not find supplier for the file '" +
                    file.Settings.FTPFileName + "'. Error: " + e.Message + " Stack: " + e.StackTrace, "Error");
            }

            return supplier;
        }

        private bool LoadCompetitorDataFromFtp()
        {
            CommonDataFunctions.CreateLogEntry(_channel, "START ProcessCompetitorFiles", "Information");

            var ftpFiles = _feedFiles.Where(x => x.Settings.Lookup.LookupName == "Competitor Inventory").ToList();

            CommonDataFunctions.CreateLogEntry(_channel, "START LoadCompetitorData", "Information");

            foreach (var file in ftpFiles.Where(x => x.FileType == FtpFileType.Competitor))
            {
                if (ftpFiles != null)
                {
                    if (!file.Settings.UseSkuuudleLite)
                    {
                        _competitorInvList = GetCompetitorInvList(_channel, file);

                        if (_competitorInvList != null)
                        {
                            if (_competitorInvList.Count == 0)
                            {
                                throw new EmptyFeedException("Competitor file: " + file.Settings.FTPFileName +
                                    " Contains no data. Processing stopped. No prices have been calculated or amended.");
                            }
                        }

                        var comps = BuildCompetitor(_channel, true);
                        _competitorList = CreateCompetitorData(_channel, comps);
                        _competitorInvList = TidyCompetitorList(_competitorInvList, _channel);
                        CreateCompetitorInventoryData(_channel);
                    }
                    else
                    {
                        ProcessSkuudleLite(file, ftpFiles.Where(x => x.FileType == FtpFileType.SkuuudleSummary).FirstOrDefault());
                    }
                }
            }

            CommonDataFunctions.CreateLogEntry(_channel, "END LoadCompetitorData", "Information");
            CommonDataFunctions.CreateLogEntry(_channel, "END ProcessCompetitorFiles", "Information");

            return true;
        }

        private void ProcessSkuudleLite(DownloadedFileData file, DownloadedFileData skuuudleFile)
        {
            CommonDataFunctions.CreateLogEntry(_channel, "START DownloadSkuudleLiteFtp", "Information");

            try
            {
                CommonDataFunctions.CreateLogEntry(_channel, "END DownloadSkuudleLiteFtp", "Information");

                CommonDataFunctions.CreateLogEntry(_channel, "START ExtractSkuudleLite", "Information");
                var skuudleCompetitors = GetSkuudleLiteSummary(_channel, skuuudleFile);
                CommonDataFunctions.CreateLogEntry(_channel, "END ExtractSkuudleLite", "Information");

                CommonDataFunctions.CreateLogEntry(_channel, "START InsertUpdateSkuudleLiteComp", "Information");
                _competitorList = CreateCompetitorData(_channel, skuudleCompetitors);
                CommonDataFunctions.CreateLogEntry(_channel, "END InsertUpdateSkuudleLiteComp", "Information");

                CommonDataFunctions.CreateLogEntry(_channel, "START ExtractCompetitorDataBasedOnSL", "Information");
                var crudCompetitor = new CrudCompetitor();
                var activeCompetitors = crudCompetitor.Read(x => x.IsActive && x.ChannelFK == _channel.ChannelID);
                _competitorInvList = GetCompetitorInvList(_channel, file, activeCompetitors);
                _competitorInvList = TidyCompetitorList(_competitorInvList, _channel);
                CommonDataFunctions.CreateLogEntry(_channel, "END ExtractCompetitorDataBasedOnSL", "Information");

                CommonDataFunctions.CreateLogEntry(_channel, "START InsertUpdateCompetitorData", "Information");
                CreateCompetitorInventoryData(_channel);
                CommonDataFunctions.CreateLogEntry(_channel, "END InsertUpdateCompetitorData", "Information");

                CleanupFeeds(new List<DownloadedFileData> { file });
            }
            catch (Exception e)
            {
                CommonDataFunctions.CreateLogEntry(_channel, "Failed to load skuudle lite data. Error: " +
                    e.Message + " Stack: " + e.StackTrace, "Error");
            }
        }

        private void SetSkuMappings(Channel channel)
        {
            try
            {
                CommonDataFunctions.CreateLogEntry(channel, "START SetSkuMappings", "Information");
                using (DP001Entities db = new DP001Entities())
                {
                    db.SetSkuMappings(channel.ChannelID, _processStartTime);
                }
                CommonDataFunctions.CreateLogEntry(channel, "END SetSkuMappings", "Information");
            }
            catch (Exception e)
            {
                CommonDataFunctions.CreateLogEntry(channel, "Failed to Set Sku Mappings. Error: " +
                    e.Message + " Stack: " + e.StackTrace, "Error");
            }
        }

        public static bool LoadSapAProductDataFromApi(Channel channel)
        {
            try
            {
                Api api = new Api(channel);
                api.HttpGetSAPProductsTask().Wait();
                _productList = api.ProductList;
                _supplierList = api.SupplierList;
                BuildBrandCategory(channel);
                CreateProductData(channel);
                CreateSupplierData(channel);
            }
            catch (Exception e)
            {
                CommonDataFunctions.CreateLogEntry(channel, "Failed to get product data from Api. Error: " +
                    e.Message + " Stack: " + e.StackTrace, "Error");

                return false;
            }

            return true;
        }

        public static bool LoadSapASupplierDataFromApi(Channel channel)
        {
            //For SAP API Supplier Data is loaded at the same time as the Product Inventory
            return false;
        }

        private void InitializeTenant()
        {
            try
            {
                _tenant = new Tenant();
                _channel = _tenant.GetChannelRecord(Convert.ToInt32(_suppliedParams["channelid"]));
                _tenant.SetupTenantDelegates(_channel);
                _supplierBrandMatching = GetSupplierBrandMatching(_channel);
                _supplierMfpnMatching = GetSupplierMfpnMatching(_channel);
                _brandList = new List<Brand>();
                _categoryList = new List<ProductCategory>();
                _brandCategoryList = new List<MapBrandCategory>();
                _feedFiles = new List<DownloadedFileData>();
                _processStartTime = CommonDataFunctions.GetCurrentDateTime();

                var crudLookup = new CrudLookup();
                MfpnMatchingPrefixType = crudLookup.Read(x => x.LookupType.LookupTypeName == "MfpnMatchType" && x.LookupName == "Prefix").FirstOrDefault();
                MfpnMatchingSuffixType = crudLookup.Read(x => x.LookupType.LookupTypeName == "MfpnMatchType" && x.LookupName == "Suffix").FirstOrDefault();
            }
            catch (Exception e)
            {
                CommonDataFunctions.CreateLogEntry(_channel, "Failed to initialize tenant. Error: " +
                    e.Message + " Stack: " + e.StackTrace, "Error");
            }
        }

        private List<SupplierMfpnMatching> GetSupplierMfpnMatching(Channel _channel)
        {
            var crud = new CrudSupplierMfpnMatching();
            return crud.Read(x => x.ChannelFK == _channel.ChannelID);
        }

        private static List<DownloadedFileData> GetFtpFiles(Channel channel, FtpFileType fileType)
        {
            var ftpFileList = new List<DownloadedFileData>();
            var ftpSettingList = new List<FTPSetting>();

            switch (fileType)
            {
                case FtpFileType.Supplier:

                    ftpSettingList = channel.FTPSettings.Where(x => x.Lookup.LookupName == "Supplier Inventory").ToList();
                    break;

                case FtpFileType.Product:

                    ftpSettingList = channel.FTPSettings.Where(x => x.Lookup.LookupName == "Product Inventory").ToList();
                    break;

                case FtpFileType.Competitor:

                    ftpSettingList = channel.FTPSettings.Where(x => x.Lookup.LookupName == "Competitor Inventory").ToList();
                    break;

                default:
                    break;
            }

            foreach (var ftpSetting in ftpSettingList)
            {
                ftpSetting.FTPPath = !string.IsNullOrEmpty(ftpSetting.FTPPath) ?
                    string.Format("//{0}//", ftpSetting.FTPPath) : string.Empty;

                var ftpFileName = !string.IsNullOrEmpty(ftpSetting.FTPZipFileName) ? ftpSetting.FTPZipFileName : ftpSetting.FTPFileName;

                var ftpHostDetails = new Ftp.FtpHostDetails()
                {
                    BlobContainer = "tenantfolders",
                    FileName = ftpFileName,
                    FtpHost = ftpSetting.FTPServer,
                    FtpUser = ftpSetting.FTPUser,
                    FtpPassword = ftpSetting.FTPPassword,
                    FolderPath = ftpSetting.FTPPath,
                    Protocol = CommonFunctions.LookupFtpProtocol(ftpSetting.FTPProtocolFK),
                    SavePath = channel.TenantFK.ToString() + "\\" + ftpSetting.FTPSettingsID + "_" + ftpFileName,
                };

                var ftpDownloadResult = Ftp.DownloadFTPFile(ftpHostDetails);

                if (ftpDownloadResult.IsSuccess)
                {
                    if (Path.GetExtension(ftpDownloadResult.Path) == ".zip")
                    {
                        var extractZipResult = CommonFunctions.ExtractZipFile(ftpDownloadResult.Path,
                            channel.TenantFK.ToString() + "\\",
                            "tenantfolders", ftpSetting.FTPFileName,
                            ftpSetting.FTPSettingsID.ToString());

                        if (extractZipResult.IsSuccess)
                        {
                            ftpDownloadResult.Path = extractZipResult.Path;
                        }
                        else
                        {
                            LogExtractZipError(channel, ftpSetting, extractZipResult);
                            continue;
                        }
                    }
                }
                else
                {
                    LogFtpError(channel, ftpDownloadResult);
                }

                ftpFileList.Add(new DownloadedFileData()
                {
                    Settings = ftpSetting,
                    DownloadResult = ftpDownloadResult,
                    FileType = fileType
                });

                if (fileType == FtpFileType.Competitor && ftpSetting.UseSkuuudleLite)
                {
                    ftpFileList.Add(new DownloadedFileData()
                    {
                        Settings = ftpSetting,
                        DownloadResult = ValidateSkuuudleSummary(ftpSetting, channel),
                        FileType = FtpFileType.SkuuudleSummary
                    });
                }
            }

            return ftpFileList;
        }

        private static List<SupplierInventory> GetSupplierList(Channel channel, DownloadedFileData file, Supplier supp)
        {
            CommonDataFunctions.CreateLogEntry(channel, "START GetSupplierList", "Information");
            var supplierList = new List<SupplierInventory>();
            var fileContent = CommonFunctions.ReadFileToString(file.DownloadResult.Path, file.DownloadResult.BlobContainer);
            var delimeter = "";

            using (var sr = new StringReader(fileContent))
            {
                delimeter = Ftp.DetectDelimiter(sr, 1).ToString();
            }

            using (var csvReader = new TextFieldParser(new StringReader(fileContent)))
            {
                csvReader.SetDelimiters(new string[] { delimeter });
                csvReader.TrimWhiteSpace = true;

                var firstLine = new string[] { };
                var fields = file.Settings.FieldMapping;

                if (!IsIndexBased(fields))
                {
                    firstLine = csvReader.ReadFields();
                }

                var indexList = new List<int>();
                var manufacturerPartNoColumn = LookupFieldIndex(file, firstLine, fields.ManufacturerPartNo, indexList);
                var manufacturerColumn = LookupFieldIndex(file, firstLine, fields.Brand, indexList, false);
                var stockColumn = LookupFieldIndex(file, firstLine, fields.StockQuantity, indexList);
                var priceColumn = LookupFieldIndex(file, firstLine, fields.Price, indexList);
                var descriptionColumn = LookupFieldIndex(file, firstLine, fields.Description, indexList);
                var clientProductIdColumn = LookupFieldIndex(file, firstLine, fields.ClientProductID, indexList, false);

                if (CheckValidColumns(indexList))
                {
                    while (!csvReader.EndOfData)
                    {
                        try
                        {
                            var rowData = csvReader.ReadFields();
                            var brand = LookupSupplierBrand(GetRowFieldData(rowData, manufacturerColumn), supp.SupplierID, channel);
                            var manufacturerPartNo = DoMfpnMatching(GetRowFieldData(rowData, manufacturerPartNoColumn), supp.SupplierID, brand, channel);
                            var stock = GetRowFieldData(rowData, stockColumn);
                            var price = DoPriceReplacement(GetRowFieldData(rowData, priceColumn));
                            var description = GetRowFieldData(rowData, descriptionColumn);
                            var originalBrand = GetRowFieldData(rowData, manufacturerColumn);
                            var clientProductId = GetRowFieldData(rowData, clientProductIdColumn);

                            var supplier = new SupplierInventory()
                            {
                                ManufacturerPartNo = manufacturerPartNo,
                                BrandName = brand,
                                StockQuantity = Convert.ToInt32(stock),
                                Price = !string.IsNullOrEmpty(price) ? Convert.ToDecimal(price) : 0,
                                Description = description,
                                SupplierFK = supp.SupplierID,
                                OriginalBrand = originalBrand,
                                ClientProductID = clientProductId
                            };

                            supplierList.Add(supplier);
                        }
                        catch (MalformedLineException e)
                        {
                            CommonDataFunctions.CreateLogEntry(channel, "**ERROR**: " + e.Message, "Error");
                            CommonDataFunctions.CreateLogEntry(channel, "**ERROR**: " + e.StackTrace, "Error");
                        }
                        catch (Exception e)
                        {
                            CommonDataFunctions.CreateLogEntry(channel, "**ERROR** in file: " + file.Settings.FTPFileName +
                                ": " + e.Message, "Error");
                            CommonDataFunctions.CreateLogEntry(channel, "**ERROR**: " + e.StackTrace, "Error");
                        }
                    }
                }
                else
                {
                    LogFieldMappingError(channel, file);
                    supplierList = null;
                }
            }

            CommonDataFunctions.CreateLogEntry(channel, "END GetSupplierList", "Information");

            return supplierList;
        }

        private static string DoPriceReplacement(string price)
        {
            return price.Replace("£", "");
        }

        private static bool IsIndexBased(FieldMapping fields)
        {
            int colIndex;
            return int.TryParse(fields.ManufacturerPartNo, out colIndex);

        }

        private static List<SupplierInventory> TidySupplierList(List<SupplierInventory> _supplierList, Channel channel)
        {
            if (channel.UseClientProductId)
            {
                return _supplierList.DistinctBy(m => new
                {
                    m.ChannelFK,
                    m.SupplierFK,
                    m.ClientProductID
                }).ToList();
            }
            else
            {
                return _supplierList.DistinctBy(m => new
                {
                    m.ChannelFK,
                    m.SupplierFK,
                    bran = m.OriginalBrand.ToLower(),
                    mfpn = m.ManufacturerPartNo.ToLower(),
                }).ToList();
            }
        }

        private static List<CompetitorInventory> TidyCompetitorList(List<CompetitorInventory> _competitorData, Channel channel)
        {
            if (channel.UseClientProductId)
            {
                return _competitorData.DistinctBy(m => new
                {
                    m.ChannelFK,
                    cm = m.CompetitorName.ToLower(),
                    m.ClientProductID
                }).ToList();
            }
            else
            {
                return _competitorData.DistinctBy(m => new
                {
                    m.ChannelFK,
                    cm = m.CompetitorName.ToLower(),
                    bn = m.OriginalBrand.ToLower(),
                    mfpn = m.ManufacturerPartNo.ToLower()
                }).ToList();
            }
        }

        private static List<ProductInventory> TidyProductList(List<ProductInventory> _productList)
        {
            return _productList.DistinctBy(m => new
            {
                m.ChannelFK,
                m.BrandFK,
                mfpn = m.ManufacturerPartNo.ToLower(),
                ci = m.ClientProductID.ToLower()
            }).ToList();
        }

        private static string DoMfpnMatching(string supplierMfpn, int supplierID, string brand, Channel channel)
        {
            try
            {
                var prefixMatches = _supplierMfpnMatching.Where(x => x.BrandName == brand && x.TypeFK == MfpnMatchingPrefixType.LookupID).ToList();
                var suffixMatches = _supplierMfpnMatching.Where(x => x.BrandName == brand && x.TypeFK == MfpnMatchingSuffixType.LookupID).ToList();

                if (suffixMatches.Count > 0)
                {
                    foreach (var match in suffixMatches)
                    {
                        if (supplierMfpn.EndsWith(match.MatchTerm))
                        {
                            supplierMfpn = supplierMfpn.Substring(0, supplierMfpn.LastIndexOf(match.MatchTerm));
                            break;
                        }
                    }
                }

                if (prefixMatches.Count > 0)
                {
                    foreach (var match in prefixMatches)
                    {
                        if (supplierMfpn.StartsWith(match.MatchTerm))
                        {
                            supplierMfpn = supplierMfpn.Substring(match.MatchTerm.Length, (supplierMfpn.Length - match.MatchTerm.Length));
                            break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                CommonDataFunctions.CreateLogEntry(channel, "Failed to do Mfpn matching. Error: " +
                    e.Message + " Stack: " + e.StackTrace, "Error");
            }

            return supplierMfpn;
        }

        private static string LookupSupplierBrand(string supplierReference, int supplierFK, Channel channel)
        {
            string supplierBrand = null;

            try
            {
                supplierBrand = _supplierBrandMatching.Where(x => x.SupplierFK == supplierFK &&
                    x.Reference.ToLower() == supplierReference.ToLower()).FirstOrDefault()?.BrandName;
            }
            catch (Exception e)
            {
                CommonDataFunctions.CreateLogEntry(channel, "Failed to lookup supplier brand. Error: " +
                    e.Message + " Stack: " + e.StackTrace, "Error");
            }

            return supplierBrand ?? supplierReference;
        }

        private static List<SupplierBrandMatching> GetSupplierBrandMatching(Channel channel)
        {
            var crud = new CrudSupplierBrandMatching();
            return crud.Read(x => x.Supplier.ChannelFK == channel.ChannelID);
        }

        private static List<ProductInventory> GetProductList(Channel channel, DownloadedFileData file)
        {
            CommonDataFunctions.CreateLogEntry(channel, "START GetProductList", "Information");

            var productList = new List<ProductInventory>();
            var fileContent = CommonFunctions.ReadFileToString(file.DownloadResult.Path, file.DownloadResult.BlobContainer);
            var delimeter = "";

            using (var sr = new StringReader(fileContent))
            {
                delimeter = Ftp.DetectDelimiter(sr, 1).ToString();
            }

            using (TextFieldParser csvReader = new TextFieldParser(new StringReader(fileContent)))
            {
                csvReader.SetDelimiters(new string[] { delimeter });
                csvReader.TrimWhiteSpace = true;

                var firstLine = new string[] { };
                var fields = file.Settings.FieldMapping;

                if (!IsIndexBased(fields))
                {
                    firstLine = csvReader.ReadFields();
                }

                var indexList = new List<int>();
                var manufacturerPartNoColumn = LookupFieldIndex(file, firstLine, fields.ManufacturerPartNo, indexList);
                var manufacturerColumn = LookupFieldIndex(file, firstLine, fields.Brand, indexList);
                var descriptionColumn = LookupFieldIndex(file, firstLine, fields.Description, indexList);
                var clientProductIDColumn = LookupFieldIndex(file, firstLine, fields.ClientProductID, indexList, false);
                var lnkdManufacturerColumn = LookupFieldIndex(file, firstLine, fields.LnKdManufacturer, indexList, false);
                var lnkdManufacturerPartNoColumn = LookupFieldIndex(file, firstLine, fields.LnKdManufacturerPartNo, indexList, false);
                var productCategoryColumn = LookupFieldIndex(file, firstLine, fields.ProductCategory, indexList);
                var isKeyLineColumn = LookupFieldIndex(file, firstLine, fields.IsKeyLine, indexList, false);

                if (CheckValidColumns(indexList))
                {
                    while (!csvReader.EndOfData)
                    {
                        try
                        {
                            var rowData = csvReader.ReadFields();
                            var manufacturerPartNo = GetRowFieldData(rowData, manufacturerPartNoColumn);
                            var manufacturer = GetRowFieldData(rowData, manufacturerColumn);
                            var description = GetRowFieldData(rowData, descriptionColumn);
                            var clientProductID = GetRowFieldData(rowData, clientProductIDColumn);
                            var lnkdManufacturer = GetRowFieldData(rowData, lnkdManufacturerColumn);
                            var lnkdManufacturerPartNo = GetRowFieldData(rowData, lnkdManufacturerPartNoColumn);
                            var category = GetRowFieldData(rowData, productCategoryColumn);
                            var isKeyline = GetRowFieldData(rowData, isKeyLineColumn);

                            ProductInventory product = new ProductInventory()
                            {
                                ManufacturerPartNo = manufacturerPartNo,
                                BrandName = manufacturer,
                                Description = description,
                                ClientProductID = clientProductID,
                                LnkdManufacturerPartNo = lnkdManufacturerPartNo,
                                LnKdBrandName = lnkdManufacturer,
                                ProductCategoryName = category,
                                IsKeyLine = ExtractBool(isKeyline)
                            };

                            productList.Add(product);
                        }
                        catch (Exception e)
                        {
                            CommonDataFunctions.CreateLogEntry(channel, "ERROR:" + e.Message + " " + e.StackTrace, "Error");
                        }
                    }
                }
                else
                {
                    LogFieldMappingError(channel, file);
                    productList = null;
                }
            }

            CommonDataFunctions.CreateLogEntry(channel, "END GetProductList", "Information");

            return productList;
        }

        private static bool ExtractBool(string inputString)
        {
            bool returnValue = false;

            if (!string.IsNullOrEmpty(inputString))
            {
                var value = inputString.ToLower().Trim();

                if (value == "true" || value == "yes" || value == "1")
                {
                    returnValue = true;
                }
            }

            return returnValue;
        }

        private List<CompetitorInventory> GetCompetitorInvList(Channel channel,
            DownloadedFileData file,
            List<Competitor> activeCompetitors = null)
        {
            CommonDataFunctions.CreateLogEntry(channel, "START GetCompetitorInvList", "Information");

            var competitorList = new List<CompetitorInventory>();
            var fileContent = CommonFunctions.ReadFileToString(file.DownloadResult.Path, file.DownloadResult.BlobContainer);
            var delimeter = "";

            using (var sr = new StringReader(fileContent))
            {
                delimeter = Ftp.DetectDelimiter(sr, 1).ToString();
            }

            using (TextFieldParser csvReader = new TextFieldParser(new StringReader(fileContent)))
            {
                csvReader.SetDelimiters(new string[] { delimeter });
                csvReader.TrimWhiteSpace = true;

                var firstLine = new string[] { };
                var fields = file.Settings.FieldMapping;

                if (!IsIndexBased(fields))
                {
                    firstLine = csvReader.ReadFields();
                }

                var indexList = new List<int>();
                var manufacturerPartNoColumn = LookupFieldIndex(file, firstLine, fields.ManufacturerPartNo, indexList);
                var manufacturerColumn = LookupFieldIndex(file, firstLine, fields.Brand, indexList);
                var priceColumn = LookupFieldIndex(file, firstLine, fields.Price, indexList);
                var competitorColumn = LookupFieldIndex(file, firstLine, fields.Competitor, indexList);
                var clientProductIdColumn = LookupFieldIndex(file, firstLine, fields.ClientProductID, indexList, false);

                if (CheckValidColumns(indexList))
                {
                    while (!csvReader.EndOfData)
                    {
                        try
                        {
                            var rowData = csvReader.ReadFields();
                            var manufacturerPartNo = GetRowFieldData(rowData, manufacturerPartNoColumn);
                            var manufacturer = GetRowFieldData(rowData, manufacturerColumn);
                            var price = DoPriceReplacement(GetRowFieldData(rowData, priceColumn));
                            var compName = GetRowFieldData(rowData, competitorColumn);
                            var competitorLookup = activeCompetitors?.Find(x => x.CompetitorName == compName);
                            var clientProductId = GetRowFieldData(rowData, clientProductIdColumn);

                            if (activeCompetitors == null || competitorLookup != null)
                            {
                                var competitorInv = new CompetitorInventory()
                                {
                                    ManufacturerPartNo = manufacturerPartNo.Length < 45 ? manufacturerPartNo : manufacturerPartNo.Substring(0, 45),
                                    BrandName = !string.IsNullOrEmpty(manufacturer) ? manufacturer : "Unknown",
                                    OriginalBrand = manufacturer,
                                    Price = !string.IsNullOrEmpty(price) ? Convert.ToDecimal(price) : 0,
                                    CompetitorName = compName,
                                    ChannelFK = _channel.ChannelID,
                                    ClientProductID = clientProductId
                                };

                                competitorList.Add(competitorInv);
                            }
                        }
                        catch (Exception e)
                        {
                            CommonDataFunctions.CreateLogEntry(_channel, "**ERROR**: " + e.Message, "Error");
                            CommonDataFunctions.CreateLogEntry(_channel, "**ERROR**: " + e.StackTrace, "Error");
                        }
                    }
                }
                else
                {
                    LogFieldMappingError(channel, file);
                    competitorList = null;
                }
            }

            CommonDataFunctions.CreateLogEntry(channel, "END GetCompetitorInvList", "Information");

            return competitorList;
        }

        private List<Competitor> GetSkuudleLiteSummary(Channel _channel, DownloadedFileData file)
        {
            var competitorList = new List<Competitor>();
            var fileContent = CommonFunctions.ReadFileToString(file.DownloadResult.Path, file.DownloadResult.BlobContainer);
            var delimeter = "";

            using (var sr = new StringReader(fileContent))
            {
                delimeter = Ftp.DetectDelimiter(sr, 1).ToString();
            }

            using (TextFieldParser csvReader = new TextFieldParser(new StringReader(fileContent)))
            {
                csvReader.SetDelimiters(new string[] { delimeter });
                csvReader.TrimWhiteSpace = true;

                var headings = csvReader.ReadFields();

                var indexList = new List<int>();
                var competitorNameColumn = LookupFieldIndex(file, headings, "Competitor name", indexList);
                var averageRatingColumn = LookupFieldIndex(file, headings, "Average rating", indexList);
                var numberOfReviewsColumn = LookupFieldIndex(file, headings, "No. of reviews (qty)", indexList);

                if (CheckValidColumns(indexList))
                {
                    while (!csvReader.EndOfData)
                    {
                        try
                        {
                            var rowData = csvReader.ReadFields();
                            var competitorName = GetRowFieldData(rowData, competitorNameColumn);
                            var averageRating = decimal.Parse(GetRowFieldData(rowData, averageRatingColumn));
                            var numberOfReviews = int.Parse(GetRowFieldData(rowData, numberOfReviewsColumn));

                            var competitor = new Competitor()
                            {
                                ChannelFK = _channel.ChannelID,
                                CompetitorName = competitorName,
                                ReviewRating = averageRating,
                                ReviewTotal = numberOfReviews,
                                IsActive = SetSkuudleLiteActive(_channel, averageRating, numberOfReviews)
                            };

                            competitorList.Add(competitor);
                        }
                        catch (MalformedLineException e)
                        {
                            CommonDataFunctions.CreateLogEntry(_channel, "**ERROR**: " + e.Message, "Error");
                            CommonDataFunctions.CreateLogEntry(_channel, "**ERROR**: " + e.StackTrace, "Error");
                        }
                    }
                }
                else
                {
                    CommonDataFunctions.CreateLogEntry(_channel, "Could not find all column mappings in file '" +
                                    file.Settings.FTPSummaryFileName + "'", "Notification", true);
                }
            }

            return competitorList;
        }

        private bool SetSkuudleLiteActive(Channel _channel, decimal averageRating, int numberOfReviews)
        {
            var setActiveCompetitor = false;

            try
            {
                switch (_channel.Lookup.LookupName)
                {
                    case "SL Reviews Only":

                        setActiveCompetitor = numberOfReviews > _channel.SLMinReviews;

                        break;

                    case "SL Rating Only":

                        setActiveCompetitor = averageRating > _channel.SLMinRating;

                        break;

                    case "SL Both":

                        setActiveCompetitor = numberOfReviews > _channel.SLMinReviews && averageRating > _channel.SLMinRating;

                        break;

                    case "SL None":

                        setActiveCompetitor = false;

                        break;

                    default:

                        setActiveCompetitor = false;

                        break;
                }
            }
            catch (Exception e)
            {
                CommonDataFunctions.CreateLogEntry(_channel, "Failed to set Skuudle Lite Active competitor. Error: " +
                    e.Message + " Stack: " + e.StackTrace, "Error");
            }

            return setActiveCompetitor;
        }

        //private static List<Brand> BuildBrand(Channel channel)
        //{
        //    var brandCrud = new CrudBrand();

        //    try
        //    {
        //        List<string> uniqueBrands = _productList.Select(x => x.BrandName).Distinct().ToList();

        //        foreach (string brandName in uniqueBrands)
        //        {
        //            var brand = SetupBrand(brandName, channel);
        //            brandCrud.Create(brand);
        //        }
        //        List<string> uniqueLinkedBrands = _productList.Select(x => x.LnKdBrandName).Distinct().ToList();
        //        foreach (string brandName in uniqueLinkedBrands)
        //        {
        //            var brand = SetupBrand(brandName, channel);
        //            brandCrud.Create(brand);
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        CommonDataFunctions.CreateLogEntry(channel, "Could not build brands. Error: " + e.Message +
        //            " Stack " + e.StackTrace, "Error");
        //    }

        //    return brandCrud.GetBrands(channel.ChannelID);
        //}

        //private static List<ProductCategory> BuildCategory(Channel channel)
        //{
        //    var prodCateg = new CrudProductCategory();
        //    List<ProductCategory> pgl = new List<ProductCategory>();

        //    try
        //    {
        //        List<string> uniqueCategories = _productList.Select(x => x.ProductCategoryName).Distinct().ToList();
        //        foreach (string categoryName in uniqueCategories)
        //        {
        //            var category = SetupCategory(categoryName, channel.ChannelID);
        //            category = prodCateg.Create(category);
        //            pgl.Add(category);
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        CommonDataFunctions.CreateLogEntry(channel, "Could not build categories. Error: " +
        //            e.Message + " Stack: " + e.StackTrace, "Error");
        //    }

        //    return pgl;
        //}

        private static void BuildBrandCategory(Channel channel)
        {
            var brandCrud = new CrudBrand();
            var categoryCrud = new CrudProductCategory();
            var brandCategoryCrud = new CrudMapBrandCategory();

            try
            {
                var uniqueBrandCategories = _productList.Select(x => new { x.BrandName, x.ProductCategoryName }).Distinct().ToList();

                foreach (var bc in uniqueBrandCategories)
                {
                    var brand = SetupBrand(bc.BrandName, channel);
                    brand = brandCrud.Create(brand);
                    if (_brandList.Where(x => x.BrandName == brand.BrandName).FirstOrDefault() == null)
                    {
                        _brandList.Add(brand);
                    }

                    var category = SetupCategory(bc.ProductCategoryName, channel.ChannelID);
                    category = categoryCrud.Create(category);
                    if (_categoryList.Where(x => x.CategoryName == category.CategoryName).FirstOrDefault() == null)
                    {
                        _categoryList.Add(category);
                    }

                    var brandCategory = SetupBrandCategory(brand.BrandID, category.ProductCategoryID);
                    brandCategory = brandCategoryCrud.Create(brandCategory);
                    if (_brandCategoryList.Where(x => x.BrandFK == brand.BrandID && x.ProductCategoryFK == category.ProductCategoryID).FirstOrDefault() == null)
                    {
                        _brandCategoryList.Add(brandCategory);
                    }
                }

                List<string> uniqueLinkedBrands = _productList.Select(x => x.LnKdBrandName).Distinct().ToList();
                foreach (string brandName in uniqueLinkedBrands)
                {
                    var brand = SetupBrand(brandName, channel);
                    brand = brandCrud.Create(brand);
                }
            }
            catch (Exception e)
            {
                CommonDataFunctions.CreateLogEntry(channel, "Could not build brands. Error: " + e.Message +
                    " Stack " + e.StackTrace, "Error");
            }
        }

        private static List<Competitor> BuildCompetitor(Channel channel, bool isActive)
        {
            CommonDataFunctions.CreateLogEntry(channel, "START BuildCompetitor", "Information");

            var comp = new CrudCompetitor();
            var cl = new List<Competitor>();

            try
            {
                var uniqueCompetitors = _competitorInvList.DistinctBy(x => x.CompetitorName.ToLower()).ToList();
                foreach (var c in uniqueCompetitors)
                {
                    var competitor = SetupCompetitor(c, channel.ChannelID, isActive);
                    competitor = comp.Create(competitor);
                    cl.Add(competitor);
                }
            }
            catch (Exception e)
            {
                CommonDataFunctions.CreateLogEntry(channel, "Failed to buil competitors. Error: " +
                    e.Message + " Stack: " + e.StackTrace, "Error");
            }

            CommonDataFunctions.CreateLogEntry(channel, "END BuildCompetitor", "Information");

            return cl;
        }

        private static void CreateProductData(Channel channel)
        {
            CommonDataFunctions.CreateLogEntry(channel, "START CreateProductData", "Information");

            try
            {
                var prodCrud = new CrudProductInventory();
                foreach (var product in _productList)
                {
                    product.ChannelFK = channel.ChannelID;
                    if (!string.IsNullOrEmpty(product.BrandName))
                    {
                        product.BrandFK = _brandList.Find(x => x.BrandName.ToLower() == product.BrandName.ToLower()).BrandID;
                    }
                    if (!string.IsNullOrEmpty(product.ProductCategoryName))
                    {
                        product.ProductCategoryFK = _categoryList.Find(x => x.CategoryName.ToLower() == product.ProductCategoryName.ToLower()).ProductCategoryID;
                    }
                    if (!string.IsNullOrEmpty(product.LnKdBrandName))
                    {
                        product.LnkdBrandFK = _brandList.Find(x => x.BrandName.ToLower() == product.LnKdBrandName.ToLower()).BrandID;
                    }
                }
                prodCrud.Create(_productList, channel.ChannelID);
            }
            catch (Exception e)
            {
                CommonDataFunctions.CreateLogEntry(channel, "Failed to create / update product data. Error: " +
                    e.Message + " Stack: " + e.StackTrace, "Error");
            }

            CommonDataFunctions.CreateLogEntry(channel, "END CreateProductData", "Information");
        }

        private static void CreateSupplierData(Channel channel)
        {
            CommonDataFunctions.CreateLogEntry(channel, "START CreateSupplierData", "Information");

            try
            {
                var suppCrud = new CrudSupplierInventory();
                foreach (var supplier in _supplierList)
                {
                    supplier.ChannelFK = channel.ChannelID;

                    if (!string.IsNullOrEmpty(supplier.BrandName) && _brandList != null)
                    {
                        var lookupBrand = _brandList.Find(x => x.BrandName.ToLower() == supplier.BrandName.ToLower());

                        if (lookupBrand != null)
                        {
                            supplier.BrandFK = lookupBrand.BrandID;
                        }
                        else
                        {
                            var lookupUnknownBrand = _brandList.Find(x => x.BrandName.ToLower() == "unknown");

                            if (lookupUnknownBrand != null)
                            {
                                supplier.BrandFK = lookupUnknownBrand.BrandID;
                            }
                            else
                            {
                                var brandCrud = new CrudBrand();
                                var unknownBrand = SetupBrand("Unknown", channel);
                                unknownBrand = brandCrud.Create(unknownBrand);
                                _brandList.Add(unknownBrand);
                                supplier.BrandFK = unknownBrand.BrandID;
                            }
                        }
                    }
                    else
                    {
                        var lookupUnknownBrand = _brandList.Find(x => x.BrandName.ToLower() == "unknown");

                        if (lookupUnknownBrand != null)
                        {
                            supplier.BrandFK = lookupUnknownBrand.BrandID;
                        }
                        else
                        {
                            var brandCrud = new CrudBrand();
                            var unknownBrand = SetupBrand("Unknown", channel);
                            unknownBrand = brandCrud.Create(unknownBrand);
                            _brandList.Add(unknownBrand);
                            supplier.BrandFK = unknownBrand.BrandID;
                        }
                    }
                }

                suppCrud.Create(_supplierList, channel.ChannelID);
            }
            catch (Exception e)
            {
                CommonDataFunctions.CreateLogEntry(channel, "Failed to create / update supplier data. Error: " +
                    e.Message + " Stack: " + e.StackTrace, "Error");
            }

            CommonDataFunctions.CreateLogEntry(channel, "END CreateSupplierData", "Information");
        }

        private void CreateCompetitorInventoryData(Channel channel)
        {
            CommonDataFunctions.CreateLogEntry(channel, "START CreateCompetitorData", "Information");

            try
            {
                var compInvCrud = new CrudCompetitorInventory();
                foreach (var c in _competitorInvList)
                {
                    c.ChannelFK = _channel.ChannelID;
                    var comp = _competitorList.Find(x => x.CompetitorName.ToLower() == c.CompetitorName.ToLower());
                    if (comp != null)
                    {
                        c.CompetitorFK = comp.CompetitorID;
                    }

                    if (!string.IsNullOrEmpty(c.BrandName) && _brandList != null)
                    {
                        var brand = _brandList.Find(x => x.BrandName.ToLower() == c.BrandName.ToLower());
                        if (brand != null)
                        {
                            c.BrandFK = brand.BrandID;
                        }
                        else
                        {
                            var lookupUnknownBrand = _brandList.Find(x => x.BrandName.ToLower() == "unknown");

                            if (lookupUnknownBrand != null)
                            {
                                c.BrandFK = lookupUnknownBrand.BrandID;
                            }
                            else
                            {
                                var brandCrud = new CrudBrand();
                                var unknownBrand = SetupBrand("Unknown", channel);
                                unknownBrand = brandCrud.Create(unknownBrand);
                                _brandList.Add(unknownBrand);
                                c.BrandFK = unknownBrand.BrandID;
                            }
                        }
                    }
                }

                compInvCrud.Create(_competitorInvList, _channel.ChannelID);
            }
            catch (Exception e)
            {
                CommonDataFunctions.CreateLogEntry(channel, "Failed to create / update competitor inventory data. Error: " +
                    e.Message + " Stack: " + e.StackTrace, "Error");
            }

            CommonDataFunctions.CreateLogEntry(_channel, "END CreateCompetitorData", "Information");
        }

        private List<Competitor> CreateCompetitorData(Channel _channel, List<Competitor> skuudleCompetitors)
        {
            var crud = new CrudCompetitor();
            return crud.Create(skuudleCompetitors, _channel.ChannelID);
        }

        private static string GetRowFieldData(string[] row, int columnIndex)
        {
            string fieldData = "";

            if (columnIndex != -1)
            {
                fieldData = string.IsNullOrEmpty(row[columnIndex]) ? "" : row[columnIndex];
            }

            return fieldData;
        }

        private static Brand SetupBrand(string brandName, Channel channel)
        {
            return new Brand()
            {
                BrandName = !String.IsNullOrEmpty(brandName) ? brandName : "Unknown",
                ChannelFK = channel.ChannelID
            };
        }

        private static ProductCategory SetupCategory(string categoryName, int channelId)
        {
            return new ProductCategory()
            {
                CategoryName = !String.IsNullOrEmpty(categoryName) ? categoryName : "Unknown",
                ChannelFK = channelId
            };
        }

        private static MapBrandCategory SetupBrandCategory(int brandFK, long categoryFK)
        {
            return new MapBrandCategory()
            {
                BrandFK = brandFK,
                ProductCategoryFK = categoryFK
            };
        }

        private static Brand SetupLinkedBrand(ProductInventory product, int channelId)
        {
            return new Brand()
            {
                BrandName = product.LnKdBrandName,
                ChannelFK = channelId
            };
        }

        private static Competitor SetupCompetitor(CompetitorInventory c, int channelId, bool isActive)
        {
            return new Competitor()
            {
                CompetitorName = !String.IsNullOrEmpty(c.CompetitorName) ? c.CompetitorName : "Unknown",
                ChannelFK = channelId,
                IsActive = isActive
            };
        }

        private void CleanupStagingTables()
        {
            CommonDataFunctions.CreateLogEntry(_channel, "START CleanupStaging Tables", "Information");

            try
            {
                List<SqlParameter> sqlParms = new List<SqlParameter>();
                SqlParameter sqlParm1 = new SqlParameter("@ChannelFK", SqlDbType.Int);
                sqlParm1.Value = _channel.ChannelID;
                sqlParms.Add(sqlParm1);
                SQL.ExecuteStoredProcedure("DP001", "DeleteStagingTableEntries", sqlParms, _channel.ChannelID);
            }
            catch (Exception e)
            {
                CommonDataFunctions.CreateLogEntry(_channel, "Failed to cleanup staging tables. Error: " +
                    e.Message + " Stack: " + e.StackTrace, "Error");
            }

            CommonDataFunctions.CreateLogEntry(_channel, "END CleanupStaging Tables", "Information");
        }

        private void CleanupNotifications()
        {
            CommonDataFunctions.CreateLogEntry(_channel, "START CleanupNotifications", "Information");

            try
            {
                List<SqlParameter> sqlParms = new List<SqlParameter>();
                SqlParameter sqlParm1 = new SqlParameter("@ChannelFK", SqlDbType.Int);
                sqlParm1.Value = _channel.ChannelID;
                sqlParms.Add(sqlParm1);
                SQL.ExecuteStoredProcedure("DP001", "CleanupNotifications", sqlParms, _channel.ChannelID);
            }
            catch (Exception e)
            {
                CommonDataFunctions.CreateLogEntry(_channel, "Failed to cleanup notifications. Error: " +
                    e.Message + " Stack: " + e.StackTrace, "Error");
            }

            CommonDataFunctions.CreateLogEntry(_channel, "END CleanupNotifications", "Information");
        }

        private static void LogFtpError(Channel channel, FtpDownloadResult ftpDownloadResult)
        {
            var message = "Could Not download ftp file '" + ftpDownloadResult.FileName +
                                    "' Reason: " + ftpDownloadResult.ErrorMessage;

            CommonDataFunctions.CreateLogEntry(channel, message, "Notification", true);
        }

        private static void LogExtractZipError(Channel channel, FTPSetting ftpSetting, ExtractZipFileResult extractZipResult)
        {
            var message = "Could Not extract '" + ftpSetting.FTPFileName + "' from '" + ftpSetting.FTPZipFileName +
                                            "' Reason: " + extractZipResult.ErrorException.Message;

            CommonDataFunctions.CreateLogEntry(channel, message, "Notification", true);
        }

        private static int LookupFieldIndex(
            DownloadedFileData file,
            string[] headings,
            string lookupField,
            List<int> columnIndexes,
            bool requiredField = true)
        {
            int colIndex;
            bool result = int.TryParse(lookupField, out colIndex);

            if (result)
            {
                colIndex--;
                if (requiredField)
                    columnIndexes.Add(colIndex);

            }
            else
            {
                colIndex = Array.FindIndex(headings, t => t.Equals(lookupField, StringComparison.InvariantCultureIgnoreCase));
                if (requiredField)
                    columnIndexes.Add(colIndex);
            }

            return colIndex;
        }

        private static void LogFieldMappingError(Channel channel, DownloadedFileData file)
        {
            CommonDataFunctions.CreateLogEntry(channel, "Could not find all column mappings in file '" +
                                    file.Settings.FTPFileName + "'", "Notification", true);
        }

        private static bool CheckValidColumns(List<int> indexList)
        {
            var valid = true;

            foreach (var col in indexList)
            {
                if (col < 0)
                {
                    valid = false;
                    break;
                }
            }

            return valid;
        }

        private static void CleanupFeeds(List<DownloadedFileData> feeds)
        {
            foreach (var feed in feeds)
            {
                CommonFunctions.DeleteFile(feed.DownloadResult.Path, "tenantfolders");
            }
        }

        private class DownloadedFileData
        {
            public FtpDownloadResult DownloadResult { get; set; }
            public FTPSetting Settings { get; set; }
            public FtpFileType FileType { get; set; }
        }

    }

    public class EmptyFeedException : Exception
    {
        public EmptyFeedException()
        {
        }

        public EmptyFeedException(string message) : base(message)
        {
        }
    }
}
