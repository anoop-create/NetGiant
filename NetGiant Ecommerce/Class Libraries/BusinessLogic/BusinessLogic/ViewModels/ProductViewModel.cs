using DataAccess.EntityFramework;
using DataAccess.Utilities;
using LinqKit;
using MailChimp.Net.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace BusinessLogic.ViewModels
{
    public class ProductViewModel : CommonViewModel
    {
        public ProductViewModel()
        {
            ProductData = DataCache.GetSectionData("ProductData");
            PromotionData = DataCache.GetSectionDataTriplet("PromotionData");
        }

        public Dictionary<string, string> ProductData { get; set; }
        public static List<Tuple<string, string, string>> PromotionData { get; set; }
        public DataTable EquipmentDetail { get; set; }
        public DataTable PrintersForProducts { get; set; }
        public List<ProductEntry> ProductList { get; set; }
        public List<SearchList> SearchList { get; set; }

        public ProductEntry Product { get; set; }
        public ProductEntry XsProduct { get; set; }

        public List<ExtdSelectListItem> ModelList { get; set; }

        public List<CategoryEntry> CategoryList { get; set; }
        public List<QandA> EquipmentQA { get; set; }
        public List<QandA> ProductQA { get; set; }
        public List<ProductFilter> ProductFilterList { get; set; }
        public List<feefoFeedback> FeeFoList { get; set; }
        public List<MiniProductEntry> SecondaryXSellList { get; set; }
        public List<string> ImageList { get; set; }
        public string Breadcrumb { get; set; }
        public string CatalogueName { get; set; }
        public int BasketCount { get; set; }
        public string HideBasketCount { get; set; }
        public DataTable DataSupplierSpec { get; set; }
        public List<MiniProductEntry> PrinterSupplies { get; set; }
        public List<MiniProductEntry> MiniProductEntries { get; set; }

        // --- Printer PDP additions --------------------------------------------------------
        // Gates every new Printer PDP section. Reuses the exact same check GetPrinterSupplies()
        // already uses (Product.Type == "Printers") rather than introducing a second, possibly
        // inconsistent, flag - defaults to false for every existing consumable product, so
        // nothing about the current PDP changes.
        public bool IsPrinterProduct => Product != null && Product.Type == "Printers";
        public List<PrinterBundleGroup> PrinterBundles { get; set; } = new List<PrinterBundleGroup>();
        public decimal PrinterBundleDiscount { get; set; }
        public List<AttributeGroup> AttributeGroups { get; set; } = new List<AttributeGroup>();
        public List<ProductDownloadEntry> Downloads { get; set; } = new List<ProductDownloadEntry>();
        // --- end Printer PDP additions -----------------------------------------------------
        public List<ProductPdf> ProductPdfs { get; set; }
        public bool PpcSuppress { get; set; }
        public bool CrossSellSuppress { get; set; }
        public bool OEMSaleIsApplicable { get; set; } = false;
        public bool CompatibleSaleIsApplicable { get; set; } = false;
        public int CategoryId { get; set; }
        public categoryCode CategoryCode { get; set; }

        public void GetProductDetail(string masterId)
        {
            List<SqlParameter> sqlParms = new List<SqlParameter>();
            SqlParameter sqlParm = new SqlParameter("@WebsiteId", SqlDbType.Int);
            sqlParm.Value = int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString());
            sqlParms.Add(sqlParm);
            sqlParm = new SqlParameter("@ProductRef", SqlDbType.VarChar);
            sqlParm.Value = masterId;
            sqlParms.Add(sqlParm);
            sqlParm = new SqlParameter("@Account", SqlDbType.VarChar);
            sqlParm.Value = HttpContext.Current.Session["U_AccountNo"] != null
                ? HttpContext.Current.Session["U_AccountNo"].ToString()
                : "";
            sqlParms.Add(sqlParm);
            DataSet ds = SQL
                .ExecuteReadStoredProcedure("netgiantmasterdata", "ngmd.GetProductResults", sqlParms, "p3results");

            DataTable productDetail = ds.Tables[0];
            DataTable xsProductDetail = new DataTable();
            if (ds.Tables.Count > 1)
            {
                xsProductDetail = ds.Tables[1];
            }

            if (productDetail.Rows.Count > 0)
            {
                Product = new ProductEntry();
                Product = CreateProductEntry(productDetail.Rows[0]);

                if ((IsCompatibleSaleActive && Product.BrandFlag.Equals(BrandFlag.Compatible))
                    || (IsOEMSaleActive && Product.BrandFlag.Equals(BrandFlag.Original))
                    || (IsStationerySaleActive && Product.IsStationerySaleItem))
                {
                    GenerateSalePrices(Product);
                }
                else
                {
                    if (Convert.ToBoolean(HttpContext.Current.Session["U_IsFromPPC"]) &&
                    Convert.ToBoolean(ConfigurationManager.AppSettings["PPCOEMPromoIsOn"]) &&
                    Product.BrandFlag.Equals(BrandFlag.Original))
                    {
                        GeneratePromoPrices(Product, Convert.ToDecimal(ConfigurationManager.AppSettings["PPCOEMPromoDisc"].ToString()));
                    }
                    if (Convert.ToBoolean(HttpContext.Current.Session["U_IsFromPPC"]) &&
                        Convert.ToBoolean(ConfigurationManager.AppSettings["PPCCOMPPromoIsOn"]) &&
                        Product.BrandFlag.Equals(BrandFlag.Compatible))
                    {
                        GeneratePromoPrices(Product, Convert.ToDecimal(ConfigurationManager.AppSettings["PPCCOMPPromoDisc"].ToString()));
                    }
                }
                SetSaleStatus(Product);

                if (Product.AssemblyCount > 1)
                {
                    sqlParms = new List<SqlParameter>();
                    sqlParm = new SqlParameter("@ProductID", SqlDbType.Int);
                    sqlParm.Value = Product.ProductId;
                    sqlParms.Add(sqlParm);
                    DataTable productComponents = SQL
                        .ExecuteReadStoredProcedure("netgiantmasterdata", "ngmd.GetProductComponents", sqlParms,
                            "p3results").Tables[0];

                    foreach (DataRow dr in productComponents.Rows)
                    {
                        Product.ComponentList.Add(CreateProductComponent(dr));
                    }
                }

                FeeFoList = EntityAccess.ReadFeeFoFeedback(x => x.productFK == Product.ProductId);

                sqlParms = new List<SqlParameter>();
                sqlParm = new SqlParameter("@WebsiteID", SqlDbType.Int);
                sqlParm.Value = int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString());
                sqlParms.Add(sqlParm);
                sqlParm = new SqlParameter("@ProductID", SqlDbType.Int);
                sqlParm.Value = Product.ProductId;
                sqlParms.Add(sqlParm);
                sqlParm = new SqlParameter("@Account", SqlDbType.VarChar);
                sqlParm.Value = HttpContext.Current.Session["U_AccountNo"] != null
                    ? HttpContext.Current.Session["U_AccountNo"].ToString()
                    : "";
                sqlParms.Add(sqlParm);
                DataTable xSellDetail = SQL
                    .ExecuteReadStoredProcedure("netgiantmasterdata", "ngmd.GetXSells", sqlParms, "xsells").Tables[0];

                SecondaryXSellList = new List<MiniProductEntry>();
                foreach (DataRow dr in xSellDetail.Rows)
                {
                    // This check removes compatibles xsells from original product pages, and originals xsells from compatible product pages. May be removed later.
                    if ((Product.AttribValue4 == 2 && Convert.ToInt32(dr["AttribValue4"]) != 2) || (Product.AttribValue4 != 2 && Convert.ToInt32(dr["AttribValue4"]) == 2))
                    {
                        continue;
                    }

                    SecondaryXSellList.Add(CreateMiniProductEntry(dr));
                }

                sqlParms = new List<SqlParameter>();
                sqlParm = new SqlParameter("@WebsiteId", SqlDbType.Int);
                sqlParm.Value = int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString());
                sqlParms.Add(sqlParm);
                sqlParm = new SqlParameter("@PartNo", SqlDbType.VarChar);
                sqlParm.Value = Product.PartNo;
                sqlParms.Add(sqlParm);
                sqlParm = new SqlParameter("@ManuRef", SqlDbType.VarChar);
                sqlParm.Value = Product.ManuRef;
                sqlParms.Add(sqlParm);
                DataTable prdImages = SQL
                    .ExecuteReadStoredProcedure("netgiantmasterdata", "ngmd.GetProductImages", sqlParms, "images")
                    .Tables[0];

                ImageList = new List<string>();
                CarouselList = new List<CarouselEntry>();
                foreach (DataRow dr in prdImages.Rows)
                {
                    ImageList.Add(Convert.ToString(dr["ImageURL"]));
                    CarouselList.Add(new CarouselEntry()
                    {
                        ThumbnailUrl = dr["ThumbURL"].ToString(),
                        MainImageUrl = dr["ImageURL"].ToString(),
                        ZoomImageUrl = dr["ZoomURL"].ToString()
                    });
                }

                GetPrinterSupplies();

                // Printer PDP sections - each method self-guards on Product.Type == "Printers"
                // (IsPrinterProduct), same as GetPrinterSupplies() above, so this is a no-op for
                // every existing consumable product.
                if (IsPrinterProduct)
                {
                    GetPrinterBundles();
                    GetAttributeGroups();
                    GetDownloads();
                }

                GetProductPdfs();
            }

            if (xsProductDetail.Rows.Count > 0)
            {
                XsProduct = new ProductEntry();
                XsProduct = CreateProductEntry(xsProductDetail.Rows[0]);

                if ((IsCompatibleSaleActive && XsProduct.BrandFlag.Equals(BrandFlag.Compatible))
                    || (IsOEMSaleActive && XsProduct.BrandFlag.Equals(BrandFlag.Original))
                    || (IsStationerySaleActive && XsProduct.IsStationerySaleItem))
                {
                    GenerateSalePrices(XsProduct);
                }
                else
                {
                    if (Convert.ToBoolean(HttpContext.Current.Session["U_IsFromPPC"]) &&
                    Convert.ToBoolean(ConfigurationManager.AppSettings["PPCOEMPromoIsOn"]) &&
                    XsProduct.BrandFlag.Equals(BrandFlag.Original))
                    {
                        GeneratePromoPrices(XsProduct, Convert.ToDecimal(ConfigurationManager.AppSettings["PPCOEMPromoDisc"].ToString()));
                    }
                    if (Convert.ToBoolean(HttpContext.Current.Session["U_IsFromPPC"]) &&
                        Convert.ToBoolean(ConfigurationManager.AppSettings["PPCCOMPPromoIsOn"]) &&
                        XsProduct.BrandFlag.Equals(BrandFlag.Compatible))
                    {
                        GeneratePromoPrices(XsProduct, Convert.ToDecimal(ConfigurationManager.AppSettings["PPCCOMPPromoDisc"].ToString()));
                    }
                }
                SetSaleStatus(XsProduct);
            }
        }

        public void GetDetailForModel(string modelname)
        {
            string record = HttpContext.Current.Session["U_Record"] != null ? HttpContext.Current.Session["U_Record"].ToString() : "";
            // Test if new customer
            //if (record.Contains("@"))
            //{
            //    record = "";
            //}
            if (!record.Contains("/"))
            {
                record = HttpContext.Current.Session["U_Email"] != null ? HttpContext.Current.Session["U_Email"].ToString() : "";
            }
            List<SqlParameter> sqlParms = new List<SqlParameter>();
            SqlParameter sqlParm = new SqlParameter("@WebsiteId", SqlDbType.Int);
            sqlParm.Value = int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString());
            sqlParms.Add(sqlParm);
            sqlParm = new SqlParameter("@EquipName", SqlDbType.VarChar);
            sqlParm.Value = modelname;
            sqlParms.Add(sqlParm);
            sqlParm = new SqlParameter("@CustomerId", SqlDbType.VarChar);
            sqlParm.Value = record;
            sqlParms.Add(sqlParm);
            EquipmentDetail = SQL
                .ExecuteReadStoredProcedure("netgiantmasterdata", "ngmd.GetPrinter3Equip", sqlParms, "p3results")
                .Tables[0];
        }

        public void GetProductsForModel(string modelname)
        {
            string account = "";
            if (HttpContext.Current.Session["U_AccountNo"] != null)
            {
                account = HttpContext.Current.Session["U_AccountNo"].ToString();
            }
            List<SqlParameter> sqlParms = new List<SqlParameter>();
            SqlParameter sqlParm = new SqlParameter("@WebsiteId", SqlDbType.Int);
            sqlParm.Value = int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString());
            sqlParms.Add(sqlParm);
            sqlParm = new SqlParameter("@EquipName", SqlDbType.VarChar);
            sqlParm.Value = modelname;
            sqlParms.Add(sqlParm);
            sqlParm = new SqlParameter("@ProductID", SqlDbType.Int);
            sqlParm.Value = 0;
            sqlParms.Add(sqlParm);
            sqlParm = new SqlParameter("@Account", SqlDbType.VarChar);
            sqlParm.Value = account;
            sqlParms.Add(sqlParm);
            DataTable dt = SQL.ExecuteReadStoredProcedure("netgiantmasterdata", "ngmd.GetPrinter3Results", sqlParms,
                "p3results").Tables[0];

            SearchList = new List<SearchList>();
            foreach (DataRow dr in dt.Rows)
            {
                SearchList.Add(new SearchList
                {
                    ItemType = "Product",
                    Product = CreateProductEntry(dr)
                });
            }
            if (PpcSuppress)
            {
                // Remove compatibles
                SearchList.RemoveAll(x => x.Product.BrandFlag == BrandFlag.Compatible);
            }
            SearchList = SearchList.OrderBy(x => x.Product.PrimarySortSeq).ThenBy(x => x.Product.AttDesc8).ToList();

            ProductFilterList = new List<ProductFilter>();
            foreach (SearchList se in SearchList)
            {
                ProductFilterList = BuildProductFilter(ProductFilterList, 8, "Colours", se.Product.AttValue8.ToString(), se.Product.AttDesc8);
                ProductFilterList = BuildProductFilter(ProductFilterList, 21, "Product Type", se.Product.BrandFlag.ToString(), se.Product.BrandFlag == BrandFlag.Original ? "Original" : "Compatible");
                ProductFilterList = BuildProductFilter(ProductFilterList, 22, "Manufacturer", se.Product.ManufacturerId.ToString(), se.Product.Brand);
                if (se.Product.AttValue6 != 0 && se.Product.AttValue6 != 25 && !String.IsNullOrEmpty(se.Product.OfferFilterText))
                {
                    ProductFilterList = BuildProductFilter(ProductFilterList, 6, "Promotion", se.Product.AttValue6.ToString(), se.Product.OfferFilterText);
                }

                // Format: ABCDE where A=Compatible, B=Multi-Pack, C=Black, D=Coloured, E=Maintenance
                se.Product.ProductAltFilter =
                    (se.Product.BrandFlag == BrandFlag.Compatible ? "Y" : "N") +
                    (se.Product.PrimarySortSeq.Equals(ProductFlag.Assembly) ? "Y" : "N") +
                    (se.Product.AttDesc8 == "Black" ? "Y" : "N") +
                    (se.Product.AttDesc8 != "Black" && !se.Product.PrimarySortSeq.Equals(ProductFlag.Ancillary) && !se.Product.PrimarySortSeq.Equals(ProductFlag.Assembly) ? "Y" : "N") +
                    (se.Product.PrimarySortSeq.Equals(ProductFlag.Ancillary) ? "Y" : "N");

                if (se.Product.AssemblyCount > 1)
                {
                    sqlParms = new List<SqlParameter>();
                    sqlParm = new SqlParameter("@ProductID", SqlDbType.Int);
                    sqlParm.Value = se.Product.ProductId;
                    sqlParms.Add(sqlParm);
                    dt = SQL.ExecuteReadStoredProcedure("netgiantmasterdata", "ngmd.GetProductComponents", sqlParms,
                        "p3results").Tables[0];

                    se.Product.ComponentList = new List<ProductComponent>();
                    foreach (DataRow dr in dt.Rows)
                    {
                        se.Product.ComponentList.Add(CreateProductComponent(dr));
                    }
                }

                if ((IsCompatibleSaleActive && se.Product.BrandFlag.Equals(BrandFlag.Compatible))
                    || (IsOEMSaleActive && se.Product.BrandFlag.Equals(BrandFlag.Original))
                    || (IsStationerySaleActive && se.Product.IsStationerySaleItem))
                {
                    GenerateSalePrices(se.Product);
                }
                else
                {
                    if (Convert.ToBoolean(HttpContext.Current.Session["U_IsFromPPC"])
                        && Convert.ToBoolean(ConfigurationManager.AppSettings["PPCOEMPromoIsOn"])
                        && se.Product.BrandFlag.Equals(BrandFlag.Original))
                    {
                        GeneratePromoPrices(se.Product, Convert.ToDecimal(ConfigurationManager.AppSettings["PPCOEMPromoDisc"].ToString()));
                    }
                    if (Convert.ToBoolean(HttpContext.Current.Session["U_IsFromPPC"]) &&
                        Convert.ToBoolean(ConfigurationManager.AppSettings["PPCCOMPPromoIsOn"]) &&
                        se.Product.BrandFlag.Equals(BrandFlag.Compatible))
                    {
                        GeneratePromoPrices(se.Product, Convert.ToDecimal(ConfigurationManager.AppSettings["PPCCOMPPromoDisc"].ToString()));
                    }
                }
                SetSaleStatus(se.Product);
            }
            ProductFilterList = ProductFilterList.OrderBy(x => x.Name).ThenBy(x => x.AdditionalSortField + x.ElementName).ToList();
        }

        public void GetProductsForCategory(int categoryId)
        {
            string account = "";
            bool isContractPricing = false;
            if (HttpContext.Current.Session["U_AccountNo"] != null)
            {
                account = HttpContext.Current.Session["U_AccountNo"].ToString();
                isContractPricing = Convert.ToBoolean(HttpContext.Current.Session["U_IsContractPricing"]);
            }

            Tuple<List<ProductEntry>, List<ProductFilter>, Dictionary<string, string>> tpl;
            tpl = DataCache.GetCategoryProducts(categoryId, account, isContractPricing);

            ProductList = tpl.Item1;
            // Break reference to Product Filter Cache object in order that subsequent changes don't affect the cached version
            ProductFilterList = new List<ProductFilter>();
            foreach (ProductFilter pf in tpl.Item2)
            {
                ProductFilterList.Add(new ProductFilter
                {
                    Count = pf.Count,
                    ElementId = pf.ElementId,
                    ElementName = pf.ElementName,
                    Id = pf.Id,
                    Name = pf.Name,
                    Selected = false
                });
            }

            foreach (KeyValuePair<string, string> bc in tpl.Item3)
            {
                BreadcrumbTrail.Add(bc.Key, bc.Value);
            }
        }

        public void GetPrintersForProducts()
        {
            PrintersForProducts = new DataTable();

            if (EquipmentDetail.Rows[0]["CartridgeTypeName"].ToString().Contains("Range"))
            {
                string ids = string.Join(",", SearchList.Select(x => x.Product.ProductId).ToList());

                List<SqlParameter> sqlParms = new List<SqlParameter>();
                SqlParameter sqlParm = new SqlParameter("@ProductIdArray", SqlDbType.VarChar);
                sqlParm.Value = ids;
                sqlParms.Add(sqlParm);
                sqlParm = new SqlParameter("@ThisId", SqlDbType.Int);
                sqlParm.Value = EquipmentDetail.Rows[0]["ModelID"];
                sqlParms.Add(sqlParm);
                PrintersForProducts = SQL.ExecuteReadStoredProcedure("netgiantmasterdata", "ngmd.GetPrintersForProducts", sqlParms,
                    "prtresults").Tables[0];
            }
        }

        public void GetCategory(int groupNo)
        {
            CategoryCode = EntityAccess.ReadCategoryCode(x => x.AXISGroupNo == groupNo.ToString()).FirstOrDefault();
        }

        public void GetSubCategories(int groupNo)
        {
            List<SqlParameter> sqlParms = new List<SqlParameter>();
            SqlParameter sqlParm = new SqlParameter("@WebsiteId", SqlDbType.Int);
            sqlParm.Value = int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString());
            sqlParms.Add(sqlParm);
            sqlParm = new SqlParameter("@ParentAxisGroupNo", SqlDbType.Int);
            sqlParm.Value = groupNo;
            sqlParms.Add(sqlParm);

            DataSet ds = SQL.ExecuteReadStoredProcedure("netgiantmasterdata", "ngmd.GetChildCategories", sqlParms,
                "cat");
            DataTable dt = ds.Tables[0];
            // Master Category
            if (dt.Rows.Count > 0)
            {
                if (dt.Rows[0]["ParentCatName"].ToString() != "")
                {
                    BreadcrumbTrail.Add(dt.Rows[0]["ParentCatName"].ToString(), dt.Rows[0]["ParentCategoryURL"].ToString().Substring(1));
                }
                CatalogueName = dt.Rows[0]["ParentCategoryName"].ToString();
            }

            dt = ds.Tables[1];
            // Sub Categories
            CategoryList = new List<CategoryEntry>();
            foreach (DataRow dr in dt.Rows)
            {
                CategoryEntry ce = new CategoryEntry();
                ce.Name = dr["ChildCategoryName"].ToString();
                ce.BoGroupNo = int.Parse(dr["ChildAxisGroupNo"].ToString());
                ce.Url = dr["ChildCategoryURL"].ToString();
                ce.HasCategories = dr["HasCategories"].ToString() == "" ? 0 : int.Parse(dr["HasCategories"].ToString());
                ce.HasProducts = dr["HasProducts"].ToString() == "" ? 0 : int.Parse(dr["HasProducts"].ToString());

                CategoryList.Add(ce);
            }
        }

        public void GetEquipmentQA(int equipmentId)
        {
            int w = int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString());
            List<qa_Main> qal =
                EntityAccess.ReadQandA(x => x.eqEquipmentFK == equipmentId &&
                                            x.qa_WebsiteMapping.Any(y => y.WebsiteFK == w) &&
                                            x.RepliedDate != null);

            EquipmentQA = new List<QandA>();
            foreach (qa_Main qa in qal)
            {
                QandA q = new QandA();
                q.Question = qa.Question;
                q.Answer = qa.Answer;
                q.Date = qa.AskedDate.ToString("D");

                EquipmentQA.Add(q);
            }
        }

        public void GetCategoryQA(int categoryId)
        {
            int w = int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString());
        }

        public void GetProductQA(int productId)
        {
            int w = int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString());
            List<qa_Main> qal =
                EntityAccess.ReadQandA(x => x.ProductID == productId &&
                                            x.qa_WebsiteMapping.Any(y => y.WebsiteFK == w) &&
                                            x.RepliedDate != null);

            ProductQA = new List<QandA>();
            foreach (qa_Main qa in qal)
            {
                QandA q = new QandA();
                q.Question = qa.Question;
                q.Answer = qa.Answer;
                q.Date = qa.AskedDate.ToString("D");

                ProductQA.Add(q);
            }
        }


        public void GetModelList(int productId)
        {
            List<eqEquipment> leq =
                EntityAccess.ReadEquipment(x => x.statusFK == 1 && x.eqProductMemberships.Any(y => y.productFK == productId));

            ModelList = leq.Select(x => new ExtdSelectListItem
            {
                Text = x.description,
                Value = x.eqEquipmentID.ToString(),
                Data = new { data_ctype = DataCache.GetCartridgeTypeName(x.eqCartridgeTypeFK).ToLower().Replace(' ', '-') },
            })
                .OrderBy(x => x.Text)
                .ToList();
        }

        public void GetMeta(string modelName)
        {
            string title = EquipmentDetail.Rows[0]["MetaTitle"].ToString();
            string description = EquipmentDetail.Rows[0]["MetaDescription"].ToString();

            // Always use the standard meta descriptions for non-TG websites
            if (title == "" || int.Parse(ConfigurationManager.AppSettings["WebsiteId"]) != 1)
            {
                title = Utilities.GetItemFromDict(ProductData, "ModelMetaTitle", true).ToString()
                    .Replace("[Model-Name]", modelName.Replace("-", " "));
            }
            if (description == "" || int.Parse(ConfigurationManager.AppSettings["WebsiteId"]) != 1)
            {
                description = Utilities.GetItemFromDict(ProductData, "ModelMetaDescription", true).ToString()
                    .Replace("[Model-Name]", modelName.Replace("-", " "));
            }

            GetMeta(title, description);
        }

        public void GetMeta(string action, int id)
        {
            if (action == "grid")
            {
                if (CategoryCode != null)
                {
                    GetMeta(CategoryCode.metaTitle, CategoryCode.metaDescription);
                }
                if (String.IsNullOrEmpty(MetaData["Title"]) || String.IsNullOrEmpty(MetaData["Description"]))
                {
                    GetMeta();
                }
            }

            if (action == "index")
            {
                string title = Product.MetaTitle;
                string titleType = "";
                string descType = "";
                string description = Product.MetaDesc;

                switch (Product.Type.ToLower())
                {
                    case "toner":
                        {
                            titleType = "Quality Toner at Low Prices";
                            descType = "toner";
                            break;
                        }
                    case "ink":
                        {
                            titleType = "Quality Printer Ink at Low Prices";
                            descType = "printer ink";
                            break;
                        }
                    case "paper":
                        {
                            titleType = "Printer Paper at Low Prices";
                            descType = "printer paper";
                            break;
                        }
                    case "printers":
                        {
                            titleType = "Quality Printers at Low Prices";
                            descType = "printers";
                            break;
                        }
                    case "solid ink":
                        {
                            titleType = "Quality Solid Ink Sticks at Low Prices";
                            descType = "solid ink";
                            break;
                        }
                    case "franking":
                        {
                            titleType = "Quality Franking Ink & Labels";
                            descType = "franking ink and labels";
                            break;
                        }
                    default:
                        {
                            titleType = "Great Low Prices at " + Utilities.GetItemFromDict(CommonData, "SiteName").ToString();
                            descType = "supplies";
                            break;
                        }
                }
                if (title == "")
                {
                    title = Utilities.GetItemFromDict(ProductData, "ProductMetaTitle", true).ToString()
                                .Replace("[Product-Name]", Product.Description)
                                .Replace("[Product-Type]", descType);
                }
                if (description == "")
                {
                    description = Utilities.GetItemFromDict(ProductData, "ProductMetaDescription", true).ToString()
                                .Replace("[Product-Name]", Product.Description)
                                .Replace("[Product-Type]", descType);
                }

                GetMeta(title, description);
            }
        }

        public void GenerateSalePrices(ProductEntry pe)
        {
            if (pe.BrandFlag.Equals(BrandFlag.Original) && IsOEMSaleActive)
            {
                pe.PriceSaleIncVat = pe.PriceRetIncVat * (100 - OEMDiscount) / 100;
            }
            if (pe.BrandFlag.Equals(BrandFlag.Compatible) && IsCompatibleSaleActive)
            {
                pe.PriceSaleIncVat = pe.PriceRetIncVat * (100 - CompatibleDiscount) / 100;
            }
            if (pe.IsStationerySaleItem && IsStationerySaleActive)
            {
                pe.PriceSaleIncVat = pe.PriceRetIncVat * (100 - StationeryDiscount) / 100;
            }
        }

        public void Compare(string productIds)
        {
            var searchModel = new SearchViewModel();
            searchModel.GetProducts(productIds);
            ProductList = searchModel.Products;

            var dataSupplierAttributeLookup = searchModel.Products
                .Select(x => new DataSupplierAttributeLookup { PartNo = x.PartNo, ManufacturerName = x.Brand }).ToList();
            GetSpecification(dataSupplierAttributeLookup);
        }

        public void GetSpecification(List<DataSupplierAttributeLookup> dataSupplierAttributeLookup)
        {
            List<SqlParameter> sqlParms = new List<SqlParameter>();
            SqlParameter sqlParm = new SqlParameter("@PartNos", SqlDbType.VarChar);
            sqlParm.Value = String.Join(",", dataSupplierAttributeLookup.Select(y => y.PartNo).ToList());
            sqlParms.Add(sqlParm);
            sqlParm = new SqlParameter("@Manufacturers", SqlDbType.VarChar);
            sqlParm.Value = String.Join(",", dataSupplierAttributeLookup.Select(y => y.ManufacturerName).ToList());
            sqlParms.Add(sqlParm);
            DataTable specAttributes = new DataTable();
            try
            {
                specAttributes = SQL
                    .ExecuteReadStoredProcedure("netgiantmasterdata", "ngmd.GetSpecificationAttributes", sqlParms, "specAttributes", 5)
                    .Tables[0];
            }
            catch (Exception)
            {
                //SQL SP is not working. Do nothing, don't raise a log entry.
                //Utilities.ProcessException(e);
            }
            // Convert this datatable to a List<>
            List<ds_attributeView> baseQuery = new List<ds_attributeView>();
            baseQuery = Utilities.ConvertDataTable<ds_attributeView>(specAttributes);

            // GRD. The following block ensures all selected entries are for a part number in the selection and a manufacturer in the selection
            // GRD. I don't think this is necessary as this is part of the criteria for the SP
            var predicate = PredicateBuilder.New<ds_attributeView>();

            foreach (var prod in dataSupplierAttributeLookup)
            {
                string temp = prod.PartNo;
                predicate = predicate.Or(p => p.partNo == temp && p.manufacturer == prod.ManufacturerName);
            }
            var attributes = baseQuery.Where(predicate).ToList();

            // Get a unique list of all possible attributes
            var rows = attributes.Select(x => x.attrName).Distinct().OrderBy(x => x).ToList();

            // Create a data table with an AttributeName column and a Part Number value column for each product
            var specDataTable = new DataTable();
            specDataTable.Columns.Add("AttributeName", typeof(string));
            dataSupplierAttributeLookup.ForEach(x => specDataTable.Columns.Add(x.PartNo, typeof(string)));

            // Loop through each distinct attribute name and put the attribute value into the appropriate slot of specDataTable
            foreach (var row in rows)
            {
                DataRow newRow = specDataTable.NewRow();
                newRow[0] = row;
                var counter = 1;
                foreach (var prod in dataSupplierAttributeLookup)
                {
                    newRow[counter] = attributes.FirstOrDefault(x => x.attrName == row && x.partNo == prod.PartNo)
                        ?.attrValue;
                    counter++;
                }

                specDataTable.Rows.Add(newRow);
            }

            DataSupplierSpec = specDataTable;
        }

        public void SetProductFilters(string filter)
        {
            try
            {
                if (filter.Contains("*"))
                {
                    List<string> filters = filter.Split('$').ToList();
                    foreach (string item in filters)
                    {
                        string name = item.Split('*')[0];
                        string elementname = item.Split('*')[1];
                        ProductFilter pf = ProductFilterList.Find(x => x.Name == name && x.ElementName == elementname);
                        if (pf != null)
                        {
                            pf.Selected = true;
                        }
                    }
                }
                // Old Style filters
                if (filter.Contains("||"))
                {
                    List<string> filterItems = filter.Split(new string[] { "|||" }, StringSplitOptions.None).ToList();
                    foreach (string filterItem in filterItems)
                    {
                        List<string> filterEntry = filterItem.Split(new string[] { "||" }, StringSplitOptions.None)
                            .ToList();
                        List<string> filterElements = filterEntry[1].Split('|').ToList();
                        foreach (string filterElement in filterElements)
                        {
                            string name = filterEntry[0];
                            string elementname = filterElement;
                            ProductFilter pf =
                                ProductFilterList.Find(
                                    x => x.Name.ToLower() == name.ToLower() &&
                                         x.ElementName.ToLower() == elementname.ToLower());
                            if (pf != null)
                            {
                                pf.Selected = true;
                            }
                        }
                    }
                }
            }
            catch
            {
                // Ignore the filter 
            }
        }

        public bool CheckPPCSuppression(string brand)
        {
            bool ret = false;
            string refererGclid = "";

            if (HttpContext.Current.Request.UrlReferrer != null)
            {
                refererGclid = HttpUtility.ParseQueryString(HttpContext.Current.Request.UrlReferrer.Query)["gclid"];
            }

            if (ConfigurationManager.AppSettings["SuppressCompatiblesForPPC"] != "Off" && (!String.IsNullOrEmpty(HttpContext.Current.Request["gclid"])) || !String.IsNullOrEmpty(refererGclid))
            {
                string[] arr = ConfigurationManager.AppSettings["SuppressCompatiblesForPPC"].Split('|');
                foreach (string b in arr)
                {
                    if (b.ToLower() == brand.ToLower())
                    {
                        ret = true;
                    }
                }
            }

            return ret;
        }

        public bool CheckCrossSellSuppression(string brand, string xsbrand)
        {
            bool ret = false;

            if (brand == "HP" || xsbrand == "HP")
            {
                ret = true;
            }

            return ret;
        }

        public void SetSaleStatus(ProductEntry pe)
        {
            if (IsOEMSaleActive
                && (OEMSaleType == "All"
                    || (OEMSaleType == "Toner" && pe.Type == "Toner")
                    || (OEMSaleType == "Ink" && "Ink|Solid Ink|Franking".Contains(pe.Type))))
            {
                pe.OEMSaleIsApplicable = true;
            }
            if (IsCompatibleSaleActive
                && (CompatibleSaleType == "All"
                    || (CompatibleSaleType == "Toner" && pe.Type == "Toner")
                    || (CompatibleSaleType == "Ink" && "Ink|Solid Ink|Franking".Contains(pe.Type))))
            {
                pe.CompatibleSaleIsApplicable = true;
            }
        }

        public string GetImageSash(bool useXSell = false)
        {
            ProductEntry product = Product;
            if (useXSell)
            {
                product = XsProduct;
            }
            bool isConsumable = false;
            if (product.Type == "Toner" || product.Type == "Ink" || product.Type == "Solid Ink" || product.Type == "Franking")
            {
                isConsumable = true;
            }
            if (product.CompatibleSaleIsApplicable && product.BrandFlag.Equals(BrandFlag.Compatible) && isConsumable)
            {
                return Utilities.GetItemFromDict(SaleData, "CompatibleSash", true);
            }
            if (product.OEMSaleIsApplicable && product.BrandFlag.Equals(BrandFlag.Original) && isConsumable)
            {
                return Utilities.GetItemFromDict(SaleData, "OEMSash", true);
            }
            if (!string.IsNullOrEmpty(product.OfferSashImage))
            {
                return product.OfferSashImage;
            }
            if (product.BrandFlag.Equals(BrandFlag.Compatible) && isConsumable)
            {
                return Utilities.GetItemFromDict(ProductData, "3YearSash", true);
            }

            return "";
        }

        protected ProductComponent CreateProductComponent(DataRow dr, bool suppressCapacity = false)
        {
            return new ProductComponent()
            {
                ProductId = Convert.ToInt32(dr["ProductID"]),
                AttValue8 = Convert.ToInt32(dr["AttribValue8"]),
                AttValue9 = Convert.ToInt32(dr["AttribValue9"]),
                AttDesc8 = Convert.ToString(dr["AttribDesc8"]),
                PageYield = Convert.ToInt32(dr["PageYield"]),
                Capacity = suppressCapacity ? "" : dr["Capacity"].ToString(),
                PackQuantity = Convert.ToInt32(dr["PackQuantity"])
            };
        }

        // Static Classes Below this point

        public static ProductEntry CreateProductEntry(DataRow dr, int parentId = 0)
        {
            // populated from the following Stored Procedures
            // GetProductResults ('P' and 'X')
            // GetPrinter3Results ('M')
            // GetSearchResults ('M')
            // GetCategoryResults (No record type)

            ProductEntry pe = new ProductEntry();

            decimal vatMultiplier = Convert.ToDecimal(ConfigurationManager.AppSettings["VatMultiplier"].ToString());

            // Basics

            pe.ProductId = int.Parse(dr["ProductID"].ToString());
            pe.Url = dr["ProductURL"].ToString();
            pe.ImageUrl = dr["ImageURL"].ToString();
            pe.PartNo = dr["PartNo"].ToString();
            pe.BarCode = dr["BarCode"].ToString();
            pe.Description = dr["Description"].ToString().Trim();
            pe.Brand = dr["Brand"].ToString();
            pe.ManufacturerId = int.Parse(dr["ManufacturerId"].ToString());
            pe.BoBrandNo = dr["AxisBrandNo"].ToString() != "" ? int.Parse(dr["AxisBrandNo"].ToString()) : 0;
            pe.BrandFlag = int.Parse(dr["BrandFlag"].ToString()) == 1 ? BrandFlag.Original : BrandFlag.Compatible;
            pe.Availability = int.Parse(dr["Availability"].ToString());
            pe.Reference = dr["ProductReference"].ToString();
            pe.PriceRetIncVat = Convert.ToDecimal(dr["PriceRetail"]);
            pe.PriceTrExVat = Convert.ToDecimal(dr["PriceTrade"]);
            //pe.PriceTrExVat = Convert.ToDecimal(dr["PriceTrade"]) != 0 ? Convert.ToDecimal(dr["PriceTrade"]) : Decimal.Divide(pe.PriceRetIncVat, vatMultiplier);

            pe.AttValue6 = 0;
            if (dr.Table.Columns.Contains("AttribValue6"))
            {
                pe.AttValue6 = int.Parse(dr["AttribValue6"].ToString());
            }
            if (dr.Table.Columns.Contains("SpecLine2"))
            {
                pe.SpecLine2 = dr["SpecLine2"].ToString();
            }
            if (dr.Table.Columns.Contains("SpecLine3"))
            {
                pe.SpecLine3 = dr["SpecLine3"].ToString();
            }
            if (pe.AttValue6 != 0 && pe.AttValue6 != 25)
            {
                if (!(dr["promotionalGroupName"] is DBNull))
                {
                    var sashImage = PromotionData.Where(x => x.Item1 == Convert.ToString(dr["promotionalGroupName"]) + "-Sash").FirstOrDefault();
                    var bullet = PromotionData.Where(x => x.Item1 == Convert.ToString(dr["promotionalGroupName"]) + "-Bullet").FirstOrDefault();

                    pe.OfferSashImage = sashImage != null ? sashImage.Item2 : "";
                    pe.OfferBullet = bullet != null ? bullet.Item2 : "";
                }

                if (dr.Table.Columns.Contains("promotionalFilterName") && !(dr["promotionalFilterName"] is DBNull))
                {
                    pe.OfferFilterText = Convert.ToString(dr["promotionalFilterName"]);
                }
            }
            pe.CrossSellSaving = dr.Table.Columns.Contains("CrossSellSaving") ? Convert.ToDecimal(dr["CrossSellSaving"]) : 0;
            pe.CrossSellBrand = dr.Table.Columns.Contains("CrossSellBrand") ? dr["CrossSellBrand"].ToString() : "";
            pe.ComponentList = new List<ProductComponent>();

            // Model, Product, XSell & Search Page
            if (dr.Table.Columns.Contains("recordType") && (dr["recordType"].ToString() == "M" || dr["recordType"].ToString() == "P" || dr["recordType"].ToString() == "X"))
            {
                pe.FeeFoCount = int.Parse(dr["FeeFoCount"].ToString());
                pe.FeeFoRating = Convert.ToDecimal(dr["FeeFoRating"]);
            }

            // Model, Product & Search Page
            if (dr.Table.Columns.Contains("recordType") && (dr["recordType"].ToString() == "M" || dr["recordType"].ToString() == "P"))
            {
                pe.AttValue5 = int.Parse(dr["AttribValue5"].ToString());
                pe.AttValue8 = int.Parse(dr["AttribValue8"].ToString());
                pe.AttValue9 = int.Parse(dr["AttribValue9"].ToString());
                pe.AttDesc6 = dr["AttribDesc6"].ToString();
                pe.AttDesc8 = dr["AttribDesc8"].ToString();
                pe.AttDesc9 = dr["AttribDesc9"].ToString();
                pe.SpecLine6 = dr["SpecLine6"].ToString();
                pe.ItemType = int.Parse(dr["ProductItemType"].ToString());
                pe.ProductTypeID = int.Parse(dr["ProductTypeID"].ToString());
                pe.BreakQty1 = Convert.ToDecimal(dr["BreakQty1"]);
                pe.BreakQty2 = Convert.ToDecimal(dr["BreakQty2"]);
                pe.BreakQty3 = Convert.ToDecimal(dr["BreakQty3"]);
                pe.BreakPrice2IncVat = Convert.ToDecimal(dr["BreakPrice2"]);
                pe.BreakPrice3IncVat = Convert.ToDecimal(dr["BreakPrice3"]);
                pe.AssemblySaving = Convert.ToDouble(dr["AssemblySaving"]);
                pe.AssemblyCount = int.Parse(dr["AssemblyCount"].ToString());
                pe.ParentProductId = parentId;

                //0 = Pack
                //1 = Single Toner
                //2 = Maintenance
                if (pe.AssemblyCount > 1)
                {
                    pe.PrimarySortSeq = ProductFlag.Assembly;
                }
                else
                {
                    pe.PrimarySortSeq = ProductFlag.Product;
                }
                if (pe.AttValue9 > 4)
                {
                    pe.PrimarySortSeq = ProductFlag.Ancillary;
                }
                pe.SecondarySortSeq = 0;
            }

            // Product Page
            if (dr.Table.Columns.Contains("ManuRef"))
            {
                pe.ManuRef = dr["ManuRef"].ToString();
                pe.AttribValue4 = int.Parse(dr["AttribValue4"].ToString());
                pe.SpecLine1 = dr["SpecLine1"].ToString();
                pe.SpecLine4 = dr["SpecLine4"].ToString();
                pe.ProductGroup = dr["ProductGroup"].ToString();
                pe.AxisGroupNo = dr["AxisGroupNo"].ToString();
                pe.CategoryCodeName = dr["CategoryCodeName"].ToString();
                pe.ProductTypeID = int.Parse(dr["ProductTypeID"].ToString());
                pe.ProductVideoURL = dr["ProductVideoURL"].ToString();
            }

            // Product Page - Primary product
            if (dr.Table.Columns.Contains("recordType") && dr["recordType"].ToString() == "P")
            {
                pe.CrossSellProductID = int.Parse(dr["CrossSellProductID"].ToString());
                pe.CrossSellProductURL = dr["CrossSellProductURL"].ToString();
                pe.CrossSellDescription = dr["CrossSellDescription"].ToString();
                pe.CrossSellBrand = dr["CrossSellBrand"].ToString();
                pe.CrossSellStatus = dr["CrossSellStatus"].ToString() == ""
                    ? 0
                    : int.Parse(dr["CrossSellStatus"].ToString());
                pe.CrossSellPriceIncVat = Convert.ToDecimal(dr["CrossSellPrice"]);
                pe.CrossSellImage = dr["CrossSellImage"].ToString();
                pe.CrossSellRef = dr["CrossSellRef"].ToString();
                pe.CrossSellPageYield = int.Parse(dr["CrossSellPageYield"].ToString());

                pe.ProductNotes = dr["ProductNotes"].ToString();
                pe.DSNotes = dr["DSNotes"].ToString();
                pe.DSSuppress = dr["DSSuppress"].ToString() != "" && Convert.ToBoolean(dr["DSSuppress"].ToString());
                pe.PriorityNote = dr["PriorityNote"].ToString();
                pe.MetaTitle = dr["MetaTitle"].ToString();
                pe.MetaDesc = dr["MetaDesc"].ToString();
                pe.MetaKeywords = dr["MetaKeywords"].ToString();
                pe.FeeFoCount = int.Parse(dr["FeeFoCount"].ToString());
                pe.FeeFoRating = Convert.ToDecimal(dr["FeeFoRating"]);
            }

            // AdHoc
            if (dr.Table.Columns.Contains("PageYield"))
            {
                pe.PageYield = int.Parse(dr["PageYield"].ToString());
            }
            if (dr.Table.Columns.Contains("Capacity"))
            {
                pe.Capacity = dr["Capacity"].ToString();
            }
            if (dr.Table.Columns.Contains("ProductType"))
            {
                pe.Type = dr["ProductType"].ToString();
            }
            if (dr.Table.Columns.Contains("AttribValue7"))
            {
                pe.AttribValue7 = int.Parse(dr["AttribValue7"].ToString());
            }
            if (dr.Table.Columns.Contains("AttribDesc7"))
            {
                pe.AttDesc7 = dr["AttribDesc7"].ToString();
            }
            if (dr.Table.Columns.Contains("IsStationerySaleItem"))
            {
                pe.IsStationerySaleItem = Convert.ToBoolean(dr["IsStationerySaleItem"].ToString());
            }

            //Future Use
            if (dr.Table.Columns.Contains("PackQuantity"))
            {
                pe.PackQuantity = int.Parse(dr["PackQuantity"].ToString());
            }

            return pe;
        }

        public static List<ProductFilter> BuildProductFilter(List<ProductFilter> lpf, int id, string name,
            string attValue, string attName)
        {
            ProductFilter tpf;

            attValue = attValue.Replace("+", "").Replace(",", "").Replace("-", "").Replace(".", "_").Replace(" ", "").Replace("/", "_");

            tpf = lpf.Find(x => x.Id == id && x.ElementId == attValue);
            if (tpf == null)
            {
                ProductFilter pf = new ProductFilter();
                pf.Id = id;
                pf.Name = name;
                pf.ElementId = attValue;
                pf.ElementName = attName;
                if (name == "Manufacturer" && attName == "Own Brand")
                {
                    Dictionary<string, string> commonData = DataCache.GetSectionData("CommonData");
                    pf.ElementName = Utilities.GetItemFromDict(commonData, "SiteName").ToString().Split('.')[0];
                }
                // AdditionalSortField used to out-sort unusual items
                if (attName == "Maintenance")
                {
                    pf.AdditionalSortField = "z";
                }
                pf.Count = 1;
                pf.Selected = false;

                lpf.Add(pf);
            }
            else
            {
                tpf.Count += 1;
            }

            return lpf;
        }

        public string BuildProductJson()
        {
            string json =
                "{" +
                "\"@context\":\"http://schema.org\"," +
                "\"@type\":\"Product\"," +
                "\"name\" : " + JsonConvert.ToString(Product.Description) + "," +
                "\"mpn\" : " + JsonConvert.ToString(Product.PartNo) + "," +
                "\"image\" : \"" + (Product.ImageUrl.StartsWith("http") ? Product.ImageUrl : "https:" + Product.ImageUrl) + "\"," +
                "\"description\" : " + JsonConvert.ToString(Product.Description) + "," +
                "\"category\" : " + JsonConvert.ToString(Product.CategoryCodeName) + "," +
                "\"sku\" : " + JsonConvert.ToString(Product.PartNo.ToString()) + "," +
                "\"color\" : " + JsonConvert.ToString(Product.AttDesc8.ToString());

            // Brand
            string brand = Product.Brand;
            if (brand == "Own Brand")
            {
                brand = Utilities.GetItemFromDict(CommonData, "ShortSiteName").ToString();
            }
            json +=
                ",\"brand\" : {" +
                    "\"@type\" : \"Brand\"," +
                    "\"name\" : " + JsonConvert.ToString(brand) +
            "}";

            switch (Product.BarCode.Length)
            {
                case 8:
                    {
                        json += ",\"gtin8\" : " + JsonConvert.ToString(Product.BarCode);
                        break;
                    }
                case 12:
                    {
                        json += ",\"gtin12\" : " + JsonConvert.ToString(Product.BarCode);
                        break;
                    }
                case 13:
                    {
                        json += ",\"gtin13\" : " + JsonConvert.ToString(Product.BarCode);
                        break;
                    }
                case 14:
                    {
                        json += ",\"gtin14\" : " + JsonConvert.ToString(Product.BarCode);
                        break;
                    }
            }

            // Returns Policy
            json +=
                ",\"hasMerchantReturnPolicy\" : {" +
                    "\"@type\" : \"MerchantReturnPolicy\"," +
                    "\"applicableCountry\" : \"GB\"," +
                    "\"returnPolicyCategory\" : \"https://schema.org/MerchantReturnFiniteReturnWindow\"," +
                    "\"merchantReturnDays\" : \"30\"," +
                    "\"returnFees\" : \"https://schema.org/FreeReturn\"," +
                    "\"returnMethod\" : \"https://schema.org/ReturnByMail\"" +
            "}";

            // Reviews
            if (Product.FeeFoCount > 0)
            {
                json +=
                    ",\"aggregateRating\" : {" +
                    "\"@type\" : \"AggregateRating\"," +
                    "\"ratingValue\" : \"" + Product.FeeFoRating.ToString("0.00") + "\"," +
                    "\"bestRating\" : \"" +
                    (FeeFoList.Count == 0 ? 0 : FeeFoList.Max(x => x.productRating)).ToString("0.00") + "\"," +
                    "\"worstRating\" : \"" +
                    (FeeFoList.Count == 0 ? 0 : FeeFoList.Min(x => x.productRating)).ToString("0.00") + "\"," +
                    "\"ratingCount\" : \"" + Product.FeeFoCount.ToString() + "\"," +
                    "\"reviewCount\" : \"" + Product.FeeFoCount.ToString() + "\"" +
                    "}";
            }
            //FeeFo Reviews
            if (FeeFoList.Count > 0)
            {
                json += ",\"review\" : [";

                string comma = "";
                foreach (feefoFeedback fb in FeeFoList)
                {
                    string auth = string.IsNullOrEmpty(fb.author) ? "\"anonymous\"" : JsonConvert.ToString(fb.author);
                    json += comma + "{" +
                            "\"@context\" : \"http://schema.org\"," +
                            "\"@type\" : \"Review\"," +
                            "\"name\" : " + auth + "," +
                            "\"reviewBody\" : " + JsonConvert.ToString(fb.productComment) + "," +
                            "\"reviewRating\" : {\"@type\" : \"Rating\", \"ratingValue\" : \"" + fb.productRating.ToString("0.00") + "\"}," +
                            "\"datePublished\" : \"" + fb.feedbackDate.ToString("yyyy-MM-dd") + "\"," +
                            "\"author\" : { \"@type\": \"Person\" , \"name\" : " + auth + " }," +
                            "\"publisher\" : { \"@type\": \"Organization\" , \"name\" : " + JsonConvert.ToString(CommonData["ShortSiteName"]) + " }" +
                            "}";
                    comma = ", ";
                }
                json += "]";
            }

            // Offer
            decimal p = Decimal.Divide(Product.PriceRetIncVat, VatMultiplier);
            bool isConsumable = Product.Type == "Toner" || Product.Type == "Ink" || Product.Type == "Solid Ink" || Product.Type == "Franking";
            if (isConsumable)
            {
                bool isValidPPCPromo = false;
                isValidPPCPromo = Convert.ToBoolean(HttpContext.Current.Session["U_IsFromPPC"])
                                    && (Convert.ToBoolean(ConfigurationManager.AppSettings["PPCOEMPromoIsOn"]) && Product.BrandFlag.Equals(BrandFlag.Original))
                                    || (Convert.ToBoolean(ConfigurationManager.AppSettings["PPCCOMPPromoIsOn"]) && Product.BrandFlag.Equals(BrandFlag.Compatible));


                if ((Product.CompatibleSaleIsApplicable && CompatibleDiscount > 0)
                    || (Product.OEMSaleIsApplicable && OEMDiscount > 0)
                    || isValidPPCPromo)
                {
                    p = Decimal.Divide(Product.PriceSaleIncVat, VatMultiplier);
                }
            }
            json += ",\"offers\": {" +
                    "\"@context\" : \"http://schema.org\"," +
                    "\"@type\" : \"Offer\"," +
                    "\"availability\" : \"http://schema.org/InStock\"," +
                    "\"itemCondition\" : \"https://schema.org/NewCondition\"," +
                    "\"url\" : \"https://" + Utilities.GetItemFromDict(CommonData, "DomainName") + Product.Url + "\"," +
                    "\"priceValidUntil\" : " + JsonConvert.ToString(DateTime.Now.Add(new TimeSpan(0, 1, 0, 0)).ToString("s")) + "," +
                    "\"priceCurrency\" : \"GBP\"," +
                    "\"price\" : \"" + p.ToString("######0.00") + "\"";

            // Shipping Policy
            json +=
                ",\"shippingDetails\" : {" +
                    "\"@type\" : \"OfferShippingDetails\"," +
                    //"\"shippingRate\" : {\"@type\" : \"MonetaryAmount\",\"value\" : \"0\",\"currency\" : \"GBP\"}" +
                    "\"deliveryTime\" : {" +
                        "\"@type\" : \"shippingDeliveryTime\"," +
                        "\"cutOffTime\" : \"17:30:00Z\"," +
                        "\"businessDays\" : {" +
                            "\"@type\" : \"OpeningHoursSpecification\"," +
                            "\"dayOfWeek\" : [\"https://schema.org/Monday\",\"https://schema.org/Tuesday\",\"https://schema.org/Wednesday\",\"https://schema.org/Thursday\",\"https://schema.org/Friday\"]" +
                        "}" +
                        ",\"handlingTime\" : {" +
                            "\"@type\" : \"QuantitativeValue\"," +
                            "\"minValue\" : \"1\"," +
                            "\"maxValue\" : \"2\"," +
                            "\"unitCode\" : \"d\"" +
                        "}" +
                        ",\"transitTime\" : {" +
                            "\"@type\" : \"QuantitativeValue\"," +
                            "\"minValue\" : \"1\"," +
                            "\"maxValue\" : \"2\"," +
                            "\"unitCode\" : \"d\"" +
                        "}" +
                    "}" +
                "}";
            json += "}";

            json += "}";

            return json;
        }

        // Protected Methods

        protected static void GeneratePromoPrices(ProductEntry pe, decimal discount)
        {
            if ((Convert.ToBoolean(ConfigurationManager.AppSettings["PPCCOMPPromoIsOn"]) && pe.BrandFlag.Equals(BrandFlag.Compatible))
                || (Convert.ToBoolean(ConfigurationManager.AppSettings["PPCOEMPromoIsOn"]) && pe.BrandFlag.Equals(BrandFlag.Original)))
            {
                pe.PPCPromoPriceIncVat = pe.PriceRetIncVat * (100 - discount) / 100;
            }
        }

        // Private Mehods

        private void GetProductPdfs()
        {
            ProductPdfs = new List<ProductPdf>();

            if (Product.SpecLine6 == "Paper" || Product.SpecLine6 == "Printer" || Product.SpecLine6 == "Stationery"
                || Product.SpecLine6 == "Audio Visual" || Product.SpecLine6 == "")
            {
                ProductPdfs = EntityAccess.GetProductPdfs(Product.PartNo, Product.Brand);
            }
        }

        private void GetPrinterSupplies()
        {
            PrinterSupplies = new List<MiniProductEntry>();
            if (Product.Type != "Printers") return;

            var sqlParms = new List<SqlParameter>();
            var sqlParm = new SqlParameter("@WebsiteId", SqlDbType.Int)
            {
                Value = int.Parse(ConfigurationManager.AppSettings["WebsiteId"])
            };
            sqlParms.Add(sqlParm);
            sqlParm = new SqlParameter("@EquipName", SqlDbType.VarChar) { Value = "" };
            sqlParms.Add(sqlParm);
            sqlParm = new SqlParameter("@ProductID", SqlDbType.Int) { Value = Product.ProductId };
            sqlParms.Add(sqlParm);
            sqlParm = new SqlParameter("@Account", SqlDbType.VarChar) { Value = "" };
            sqlParms.Add(sqlParm);
            var printerSupplies =
                SQL.ExecuteReadStoredProcedure("netgiantmasterdata", "ngmd.GetPrinter3Results", sqlParms,
                    "printerSupplies").Tables[0];

            PrinterSupplies = new List<MiniProductEntry>();
            foreach (DataRow dr in printerSupplies.Rows)
            {
                PrinterSupplies.Add(CreateMiniProductEntry(dr));
            }
        }

        private static MiniProductEntry CreateMiniProductEntry(DataRow dr)
        {
            var mpe = new MiniProductEntry
            {
                ProductId = int.Parse(dr["ProductID"].ToString()),
                Url = dr["ProductURL"].ToString(),
                ImageUrl = dr["ImageURL"].ToString(),
                Description = dr["Description"].ToString(),
                Brand = Convert.ToString(dr["Brand"]),
                Availability = int.Parse(dr["Availability"].ToString()),
                Reference = dr["ProductReference"].ToString(),
                PartNo = dr["PartNo"].ToString(),
                PriceRetIncVat = Convert.ToDecimal(dr["PriceRetail"]),
                PriceTrExVat = Convert.ToDecimal(dr["PriceTrade"]),
                PageYield = dr["PageYield"] is DBNull ? 0 : Convert.ToInt32(dr["PageYield"]),
                AttDesc8 = Convert.ToString(dr["AttribDesc8"]),
                AttValue8 = Convert.ToInt32(dr["AttribValue8"]),
                AttValue9 = Convert.ToInt32(dr["AttribValue9"]),
                AssemblyCount = Convert.ToInt32(dr["AssemblyCount"])
            };

            if (dr.Table.Columns.Contains("AttribValue4"))
            {
                mpe.AttValue4 = Convert.ToInt32(dr["AttribValue4"]);
            }

            if (mpe.AssemblyCount > 1)
            {
                mpe.PrimarySortSeq = ProductFlag.Assembly;
            }
            else
            {
                mpe.PrimarySortSeq = ProductFlag.Product;
            }
            if (mpe.AttValue9 > 4)
            {
                mpe.PrimarySortSeq = ProductFlag.Ancillary;
            }

            return mpe;
        }
        /// <summary>
        /// Printer bundle cross-sells (brief page 4: "Save When You Buy A Printer Bundle").
        /// Reuses the existing ProductAddon table via the same EF DbContext pattern
        /// CheckoutViewModel.GetAddOn() already uses for the mini-cart's "You May Also Need"
        /// popup - no new relational table was needed for the compatible/original link itself.
        /// Splits by the addon product's own BrandFlag (already used elsewhere in Index.cshtml
        /// to tell Original apart from Compatible) rather than adding a new "bundle type"
        /// column. Only in-stock addons are offered, matching GetAddOn()'s own rule.
        /// </summary>
        private void GetPrinterBundles()
        {
            PrinterBundles = new List<PrinterBundleGroup>();
            PrinterBundleDiscount = 0;

            using (Ngmd db = new Ngmd())
            {
                var addonIds = db.ProductAddons
                    .Where(x => x.ProductId == Product.ProductId && x.IsActive)
                    .OrderBy(x => x.DisplayOrder)
                    .Select(x => x.AddonProductId)
                    .ToList();

                foreach (var addonId in addonIds)
                {
                    ProductEntry addon = GetProductDetailById(addonId);
                    if (addon == null || (addon.Availability != 1 && addon.Availability != 7))
                    {
                        continue;
                    }

                    decimal perMlCost = 0;
                    if (decimal.TryParse(addon.Capacity, out decimal capacityMl) && capacityMl > 0)
                    {
                        perMlCost = Math.Round(addon.PriceTrExVat / capacityMl, 2);
                    }

                    PrinterBundles.Add(new PrinterBundleGroup
                    {
                        IsCompatible = addon.BrandFlag == BrandFlag.Compatible,
                        AddonProduct = addon,
                        BundlePriceIncVat = Product.PriceRetIncVat + addon.PriceRetIncVat,
                        PerMlCost = perMlCost
                    });
                }
            }

            List<SqlParameter> sqlParms = new List<SqlParameter>
            {
                new SqlParameter("@ProductID", SqlDbType.Int) { Value = Product.ProductId }
            };
            DataTable discountTable = SQL
                .ExecuteReadStoredProcedure("netgiantmasterdata", "ngmd.GetPrinterBundleDiscount", sqlParms, "discount")
                .Tables[0];
            if (discountTable.Rows.Count > 0)
            {
                PrinterBundleDiscount = Convert.ToDecimal(discountTable.Rows[0]["DiscountAmount"]);
            }
        }

        /// <summary>
        /// Groups the product's existing spec attributes (ds_attributeView - the same source
        /// Specification.cshtml already reads) into named accordion sections, via the new
        /// ngmd.GetProductAttributeGroups proc. Anything not yet mapped to a named group still
        /// comes back in a "Specification" fallback bucket, so nothing silently disappears just
        /// because a mapping hasn't been added for it yet.
        /// </summary>
        private void GetAttributeGroups()
        {
            AttributeGroups = new List<AttributeGroup>();

            List<SqlParameter> sqlParms = new List<SqlParameter>
            {
                new SqlParameter("@PartNo", SqlDbType.VarChar) { Value = Product.PartNo },
                new SqlParameter("@ManufacturerName", SqlDbType.VarChar) { Value = Product.Brand }
            };
            DataSet ds = SQL.ExecuteReadStoredProcedure("netgiantmasterdata", "ngmd.GetProductAttributeGroups", sqlParms, "attributeGroups");

            var groupsById = new Dictionary<int, AttributeGroup>();
            if (ds.Tables.Count > 0)
            {
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    int groupId = Convert.ToInt32(dr["groupID"]);
                    if (!groupsById.ContainsKey(groupId))
                    {
                        groupsById[groupId] = new AttributeGroup
                        {
                            GroupName = dr["groupName"].ToString(),
                            Sequence = Convert.ToInt32(dr["sequence"]),
                            DefaultOpen = Convert.ToBoolean(dr["defaultOpen"])
                        };
                    }

                    groupsById[groupId].Attributes.Add(new SpecAttributeRow
                    {
                        Name = dr["attrName"].ToString(),
                        Value = dr["attrValue"].ToString()
                    });
                }
            }
            AttributeGroups = groupsById.Values.OrderBy(g => g.Sequence).ToList();

            if (ds.Tables.Count > 1 && ds.Tables[1].Rows.Count > 0)
            {
                var fallback = new AttributeGroup { GroupName = "Specification", Sequence = int.MaxValue, DefaultOpen = false };
                foreach (DataRow dr in ds.Tables[1].Rows)
                {
                    fallback.Attributes.Add(new SpecAttributeRow
                    {
                        Name = dr["attrName"].ToString(),
                        Value = dr["attrValue"].ToString()
                    });
                }
                AttributeGroups.Add(fallback);
            }
        }

        /// <summary>
        /// Downloadable files against this product (brief page 5), via the new
        /// ngmd.GetProductDownloads proc / productDownload table.
        /// </summary>
        private void GetDownloads()
        {
            Downloads = new List<ProductDownloadEntry>();

            List<SqlParameter> sqlParms = new List<SqlParameter>
            {
                new SqlParameter("@ProductID", SqlDbType.Int) { Value = Product.ProductId }
            };
            DataTable dt = SQL
                .ExecuteReadStoredProcedure("netgiantmasterdata", "ngmd.GetProductDownloads", sqlParms, "downloads")
                .Tables[0];

            foreach (DataRow dr in dt.Rows)
            {
                Downloads.Add(new ProductDownloadEntry
                {
                    FileName = dr["fileName"].ToString(),
                    FileUrl = dr["fileURL"].ToString()
                });
            }
        }

        public ProductEntry GetProductDetailById(int masterId)
        {
            List<SqlParameter> sqlParms = new List<SqlParameter>();
            SqlParameter sqlParm = new SqlParameter("@WebsiteId", SqlDbType.Int);
            sqlParm.Value = int.Parse(ConfigurationManager.AppSettings["WebsiteId"].ToString());
            sqlParms.Add(sqlParm);
            sqlParm = new SqlParameter("@ProductID", SqlDbType.Int);
            sqlParm.Value = masterId;
            sqlParms.Add(sqlParm);
            sqlParm = new SqlParameter("@Account", SqlDbType.VarChar);
            sqlParm.Value = HttpContext.Current.Session["U_AccountNo"] != null
                ? HttpContext.Current.Session["U_AccountNo"].ToString()
                : "";
            sqlParms.Add(sqlParm);
            DataSet ds = SQL
                .ExecuteReadStoredProcedure("netgiantmasterdata", "ngmd.GetProductResultsById", sqlParms, "p3results");

            DataTable productDetail = ds.Tables[0];

            // IMPORTANT: build and return a LOCAL ProductEntry here - do NOT assign to the
            // shared `Product` property. This method is used to look up *other* products
            // (e.g. bundle/addon products in GetPrinterBundles(), or "You May Also Need"
            // add-ons in CheckoutViewModel) while `Product` on this same ViewModel instance
            // is still the page's actual/primary product. Previously this overwrote `Product`
            // with whichever addon was looked up last, so after GetPrinterBundles() ran, the
            // printer PDP's own canonical-URL check (ProductController.Index) redirected to
            // the last bundle addon product instead of the printer itself.
            ProductEntry result = null;
            if (productDetail.Rows.Count > 0)
            {
                result = CreateProductEntry(productDetail.Rows[0]);
            }
            return result;
        }
        public BasketContents CreateBasketContent(ProductEntry product)
        {
            return new BasketContents
            {
                ProductId = product.ProductId,
                StockRef = product.Reference,
                PartNo = product.PartNo,
                Description = product.Description,
                ProductUrl = product.Url,
                ImageUrl = product.ImageUrl,

                Availability = product.Availability,
                PriceInc = product.PriceRetIncVat,
                PriceEx = product.PriceTrExVat,

                IsCompatible = product.BrandFlag == BrandFlag.Compatible,
                IsCompatibleInk = product.BrandFlag == BrandFlag.Compatible &&
                                  product.SpecLine6 == "Ink",

                IsBulky = product.SpecLine6 == "Bulky",
                IsSpecialOrder = product.Availability == 10,

                CategoryNo = 0,
                GroupNo = 0,
                GroupName = "",
                Quantity = 1,
                QtyStart = 1,
                Type = 0,
                ItemType = BasketItemType.Item,
                LineUid = 0,

                IsVatExempt = HttpContext.Current.Session["D_IsVatExempt"] != null &&
                              Convert.ToBoolean(HttpContext.Current.Session["D_IsVatExempt"]),

                AffiliateCommissionGroup = "",

                CrossSellingStockRef = "",
                CrossSellingPriceEx = 0,
                CrossSellingAvailability = 0,
                CrossSellingDescription = "",
                CrossSellingImageURL = ""
            };
        }
    }
}