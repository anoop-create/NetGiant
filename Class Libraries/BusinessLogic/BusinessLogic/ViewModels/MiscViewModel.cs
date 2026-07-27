using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Web;
using DataAccess.Utilities;

namespace BusinessLogic.ViewModels
{
    public class MiscViewModel : CommonViewModel
    {
        public MiscViewModel()
        {
            MiscData = DataCache.GetSectionData("MiscData");
        }

        public Dictionary<string, string> MiscData { get; set; }
        public string JsonPrinters { get; set; }
        public ProductViewModel ProductViewModel { get; set; }
        public AccountApplicationDetails AccountApplicationDetails { get; set; }
        public TradeApplicationDetails TradeApplicationDetails { get; set; }

        private List<string> _stockRefs = new List<string>();

        public void ProcessPrinterFinderFile()
        {
            var jsonPrinters = DataCache.GetCache<string>("PrinterFinderJsonPrinters");
            if (jsonPrinters == null)
            {
                var fileName = HttpContext.Current.Server.MapPath(Utilities.GetStaticFilePrefix() + "/printerFinder.xls");
                var conn = string.Format("Provider=Microsoft.ACE.OLEDB.12.0; data source={0}; Extended Properties=Excel 8.0;", fileName);
                var jsonList = new List<PrinterFinderEntry>();

                var adapter = new OleDbDataAdapter("SELECT * FROM [Sheet2$]", conn);
                var ds = new DataSet();
                adapter.Fill(ds, "printers");
                var data = ds.Tables["printers"];

                foreach (DataRow row in data.Rows)
                {
                    if (row[0].ToString() != "" && row[0].ToString() != "Brand" && row[18].ToString() != "")
                    {
                        var stockRef = row[18].ToString().Trim();

                        jsonList.Add(new PrinterFinderEntry
                        {
                            Model = row[3].ToString().ToUpper(),
                            Function = row[4].ToString(),
                            Colour = row[5].ToString().ToUpper(),
                            Type = row[6].ToString().ToUpper(),
                            Pagesize = row[7].ToString().ToUpper(),
                            Wifi = row[10].ToString().ToUpper(),
                            Mobile = row[11].ToString().ToUpper(),
                            Duplex = row[12].ToString().ToUpper(),
                            Network = row[13].ToString().ToUpper(),
                            Traysize = row[16].ToString().ToUpper(),
                            StockRef = stockRef
                        });

                        _stockRefs.Add(stockRef);
                    }
                }

                JsonPrinters = JsonConvert.SerializeObject(new { Printers = jsonList });
                DataCache.PutCache("PrinterFinderJsonPrinters", JsonPrinters);
            }
            else
            {
                JsonPrinters = jsonPrinters;
            }

            GetProductsFromDatabase();
        }

        private void GetProductsFromDatabase()
        {
            ProductViewModel = new ProductViewModel()
            {
                ProductList = new List<ProductEntry>()
            };

            var productList = DataCache.GetCache<List<ProductEntry>>("PrinterFinderProductList");
            if (productList == null)
            {
                var productIds = EntityAccess.ReadProduct(x => _stockRefs.Contains(x.AxisFields.stockReference)).Select(x => x.productID).ToList();
                var searchModel = new SearchViewModel();
                searchModel.GetProducts(string.Join(",", productIds));
                ProductViewModel.ProductList = searchModel.Products;
                ProductViewModel.ProductList = ProductViewModel.ProductList.OrderBy(x => x.PriceTrExVat).ToList();
                DataCache.PutCache("PrinterFinderProductList", ProductViewModel.ProductList);
            }
            else
            {
                ProductViewModel.ProductList = productList;
            }
        }

        public new void GetMeta()
        {
            var action = HttpContext.Current.Request.RequestContext.RouteData.Values["action"].ToString().ToLower();
            switch (action)
            {
                case "printerfinder":
                    GetMeta("Find Your Perfect Printer" + " | " + Utilities.GetItemFromDict(CommonData, "ShortSiteName"), "Find Your Perfect Printer");
                    break;
                default:
                    var cvm = new CommonViewModel
                    {
                        SignUp = new SignUp(),
                        SignIn = new SignIn()
                    };
                    cvm.GetMeta();
                    break;
            }
        }

        public static void TestDbConnection()
        {
            SQL.ExecuteReadInline("netgiantmasterdata", "SELECT GETDATE()");
        }
    }
}
