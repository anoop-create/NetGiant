using System;
using System.Collections.Generic;
using System.Linq;
using DataAccess.Utilities;
using System.Data;
using System.Data.SqlClient;
using System.Web;
using DataAccess.EntityFramework;
using System.Configuration;
using System.Text;
using LinqKit;
using System.Web.Mvc;
using System.Globalization;

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
        public List<ProductEntry> ProductList { get; set; }
        public List<SearchEntry> SearchList { get; set; }

        public ProductEntry Product { get; set; }

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
        public List<ProductPdf> ProductPdfs { get; set; }
        public bool PpcSuppress { get; set; }
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
            DataTable productDetail = SQL
                .ExecuteReadStoredProcedure("netgiantmasterdata", "ngmd.GetProductResults", sqlParms, "p3results")
                .Tables[0];

            if (productDetail.Rows.Count > 0)
            {
                Product = new ProductEntry();
                Product = CreateProductEntry(productDetail.Rows[0]);

                bool isEntitledToPromo = false;
                decimal promoDiscount = 0;
                if (Convert.ToBoolean(HttpContext.Current.Session["U_IsFromPPC"]) &&
                    Convert.ToBoolean(ConfigurationManager.AppSettings["PPCPromoIsOn"]))
                {
                    isEntitledToPromo = true;
                    promoDiscount = Convert.ToDecimal(ConfigurationManager.AppSettings["PPCPromoDisc"].ToString());
                }
                if ((IsCompatibleSaleActive && Product.BrandFlag.Equals(BrandFlag.Compatible)) 
                    || (IsOEMSaleActive && Product.BrandFlag.Equals(BrandFlag.Original))
                    || (IsStationerySaleActive && Product.IsStationerySaleItem))
                {
                    GenerateSalePrices(Product);
                }
                else
                {
                    if (isEntitledToPromo)
                    {
                        GeneratePromoPrices(Product, promoDiscount);
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

                    Product.ComponentList = new List<ProductComponent>();
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
                foreach (DataRow dr in prdImages.Rows)
                {
                    ImageList.Add(Convert.ToString(dr["ImageURL"]));
                }

                GetPrinterSupplies();

                GetProductPdfs();
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
            bool isEntitledToPromo = false;
            decimal promoDiscount = 0;
            if (Convert.ToBoolean(HttpContext.Current.Session["U_IsFromPPC"]) &&
                Convert.ToBoolean(ConfigurationManager.AppSettings["PPCPromoIsOn"]))
            {
                isEntitledToPromo = true;
                promoDiscount = Convert.ToDecimal(ConfigurationManager.AppSettings["PPCPromoDisc"].ToString());
            }
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

            SearchList = new List<SearchEntry>();
            foreach (DataRow dr in dt.Rows)
            {
                SearchList.Add(new SearchEntry
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
            foreach (SearchEntry se in SearchList)
            {
                ProductFilterList = BuildProductFilter(ProductFilterList, 8, "Colours", se.Product.AttValue8.ToString(),
                    se.Product.AttDesc8);
                ProductFilterList = BuildProductFilter(ProductFilterList, 21, "Product Type",
                    se.Product.BrandFlag.ToString(),
                    se.Product.BrandFlag == BrandFlag.Original ? "Original" : "Compatible");
                ProductFilterList = BuildProductFilter(ProductFilterList, 22, "Manufacturer",
                    se.Product.ManufacturerId.ToString(), se.Product.Brand);
                if (se.Product.AttValue6 != 0 && se.Product.AttValue6 != 25 && !String.IsNullOrEmpty(se.Product.OfferFilterText))
                {
                    ProductFilterList = BuildProductFilter(ProductFilterList, 6, "Promotion",
                        se.Product.AttValue6.ToString(), se.Product.OfferFilterText);
                }

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
                    if (isEntitledToPromo)
                    {
                        GeneratePromoPrices(se.Product, promoDiscount);
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

            Tuple<List<ProductEntry>, List<ProductFilter>, string> tpl;
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
            Breadcrumb = tpl.Item3;
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
                StringBuilder sb = new StringBuilder();
                TagBuilder tag;

                sb.Append(dt.Rows[0]["TopParentBreadcrumb"].ToString());

                if (sb.ToString() != "")
                {
                    tag = new TagBuilder("i");
                    tag.Attributes.Add("class", "fa fa-chevron-right g-fs-xs-i");
                    sb = sb.Append(tag.ToString());
                }

                tag = new TagBuilder("a");
                tag.Attributes.Add("href", "javascript:void(0)");
                tag.Attributes.Add("class", "second");
                tag.InnerHtml = "&nbsp;" + dt.Rows[0]["ParentCategoryName"].ToString() + "&nbsp;";
                sb = sb.Append(tag.ToString());

                Breadcrumb = sb.ToString();
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
            //List<qa_Main> qal = EntityAccess.ReadQandA(x => x.eqEquipmentFK == equipmentId && x.qa_WebsiteMapping.Any(y => y.WebsiteFK == w));

            //EquipmentQA = new List<QandA>();
            //foreach (qa_Main qa in qal)
            //{
            //    QandA q = new QandA();
            //    q.Question = qa.Question;
            //    q.Answer = qa.Answer;
            //    q.Date = qa.AskedDate.ToString("D");

            //    EquipmentQA.Add(q);
            //}
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
                    Data = new {data_ctype = x.eqCartridgeType.eqCartridgeTypeName.ToLower().Replace(' ', '-')}
                })
                .OrderBy(x => x.Text)
                .ToList();
        }

        public void GetMeta(string modelName)
        {
            string free = int.Parse(ConfigurationManager.AppSettings["WebsiteId"]) == 3 ? "" : "Free Delivery | ";
            GetMeta(
                CultureInfo.CurrentCulture.TextInfo.ToTitleCase(modelName.Replace("-", " ")) + " | " + free + Utilities.GetItemFromDict(CommonData, "ShortSiteName"),
                "Shop Cheap " + modelName.Replace("-", " ") + " at " + Utilities.GetItemFromDict(CommonData, "SiteName") +
                " - FREE Next Day Courier Delivery. We won't be beaten on price. Learn more now!"
            );
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
                    switch (int.Parse(ConfigurationManager.AppSettings["WebsiteId"]))
                    {
                        case 1:
                        {
                            title = Product.Description + " | " + Utilities.GetItemFromDict(CommonData, "ShortSiteName").ToString();
                            break;
                        }
                        case 2:
                        case 3:
                        {
                            title = Product.Description + " | " + titleType + " | " + Utilities.GetItemFromDict(CommonData, "ShortSiteName").ToString();
                            break;
                        }
                    }
                }
                if (description == "")
                {
                    switch (int.Parse(ConfigurationManager.AppSettings["WebsiteId"]))
                    {
                        case 1:
                        {
                            description = Utilities.GetItemFromDict(CommonData, "SiteName").ToString() + @" has a fantastic selection of " +
                                          descType +
                                          @" available. Click here to see our great prices on " + title +
                                          @" and our other products!";
                            break;
                        }
                        case 2:
                        {
                            description = @"Buy the " + title +
                                          @" for a great low price and get Superfast Free shipping on everything!";
                            break;
                        }
                        case 3:
                        {
                            description = @"Shop now for the " + title +
                                          @" and get fantastic low prices on all your office supplies at NetGiant.com";
                            break;
                        }
                    }
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
                .Select(x => new DataSupplierAttributeLookup {PartNo = x.PartNo, ManufacturerName = x.Brand}).ToList();
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
                    List<string> filterItems = filter.Split(new string[] {"|||"}, StringSplitOptions.None).ToList();
                    foreach (string filterItem in filterItems)
                    {
                        List<string> filterEntry = filterItem.Split(new string[] {"||"}, StringSplitOptions.None)
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

        public void SetSaleStatus(ProductEntry pe)
        {
            if (IsOEMSaleActive
                && (OEMSaleType == "All"
                    || (OEMSaleType == "Toner" && pe.Type == "Toner")
                    || (OEMSaleType == "Ink" && "Ink,Solid Ink,Franking".Contains(pe.Type))))
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

        protected ProductComponent CreateProductComponent(DataRow dr)
        {
            return new ProductComponent()
            {
                ProductId = Convert.ToInt32(dr["ProductID"]),
                AttValue8 = Convert.ToInt32(dr["AttribValue8"]),
                AttValue9 = Convert.ToInt32(dr["AttribValue9"]),
                AttDesc8 = Convert.ToString(dr["AttribDesc8"]),
                PageYield = Convert.ToInt32(dr["PageYield"]),
                PackQuantity = Convert.ToInt32(dr["PackQuantity"])
            };
        }

        // Static Classes Below this point

        public static ProductEntry CreateProductEntry(DataRow dr, int parentId = 0)
        {
            ProductEntry pe = new ProductEntry();

            // Basics

            pe.ProductId = int.Parse(dr["ProductID"].ToString());
            pe.Url = dr["ProductURL"].ToString();
            pe.ImageUrl = dr["ImageURL"].ToString();
            pe.PartNo = dr["PartNo"].ToString();
            pe.Description = dr["Description"].ToString().Trim();
            pe.Brand = dr["Brand"].ToString();
            pe.ManufacturerId = int.Parse(dr["ManufacturerId"].ToString());
            pe.BoBrandNo = dr["AxisBrandNo"].ToString() != "" ? int.Parse(dr["AxisBrandNo"].ToString()) : 0;
            pe.BrandFlag = int.Parse(dr["BrandFlag"].ToString()) == 1 ? BrandFlag.Original : BrandFlag.Compatible;
            pe.Availability = int.Parse(dr["Availability"].ToString());
            pe.Reference = dr["ProductReference"].ToString();
            pe.PriceRetIncVat = Convert.ToDecimal(dr["PriceRetail"]);
            pe.PriceTrIncVat = Convert.ToDecimal(dr["PriceTrade"]);

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

            // Model Page
            if (dr.Table.Columns.Contains("ProductItemType"))
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
                pe.ProductNotes = dr["ProductNotes"].ToString();
                pe.ProductGroup = dr["ProductGroup"].ToString();
                pe.AxisGroupNo = dr["AxisGroupNo"].ToString();
                pe.CategoryCodeName = dr["CategoryCodeName"].ToString();
                pe.ProductTypeID = int.Parse(dr["ProductTypeID"].ToString());
                pe.ProductVideoURL = dr["ProductVideoURL"].ToString();
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
                pe.DSNotes = dr["DSNotes"].ToString();
                pe.DSSuppress = dr["DSSuppress"].ToString() != "" && Convert.ToBoolean(dr["DSSuppress"].ToString());
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

        // Protected Mehods

        protected static void GeneratePromoPrices(ProductEntry pe, decimal discount)
        {
            pe.PPCPromoPriceIncVat = pe.PriceRetIncVat * (100 - discount) / 100;
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
            sqlParm = new SqlParameter("@EquipName", SqlDbType.VarChar) {Value = ""};
            sqlParms.Add(sqlParm);
            sqlParm = new SqlParameter("@ProductID", SqlDbType.Int) {Value = Product.ProductId};
            sqlParms.Add(sqlParm);
            sqlParm = new SqlParameter("@Account", SqlDbType.VarChar) {Value = ""};
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
                PriceTrIncVat = Convert.ToDecimal(dr["PriceTrade"]),
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
    }
}