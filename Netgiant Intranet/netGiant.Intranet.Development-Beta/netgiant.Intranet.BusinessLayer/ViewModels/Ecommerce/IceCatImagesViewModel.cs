using netGiant.Intranet.BusinessLayer.Utilities;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace netGiant.Intranet.BusinessLayer.ViewModels.Ecommerce
{
    public class IceCatImagesViewModel : CommonViewModel
    {
        public IceCatImagesViewModel(string site)
        {
            //GetOpenRangeImages(site);
            WebsiteNameList = GetWebsiteNames();
            SiteId = site;
            GetCounts();
        }

        public List<Telerik> IceCatImagesList { get; set; }
        public int ActiveProductCount { get; set; }
        public int ProductMatchCount { get; set; }
        public int ImageMatchCount { get; set; }
        public string SiteId { get; set; }
        public IQueryable<SelectListItem> WebsiteNameList { get; set; }
        public string LocalDirectory { get; set; }
        public string FilePath { get; set; }

        public void GetIceCatImages()
        {
            DataSet ds = new DataSet("id");
            DataTable dt = new DataTable();

            string sql = @"SELECT 
	            P.productID
	            , P.partNo
	            , M.manufacturerName
	            , P.productName
	            , WI.websiteFK
	            , W.WebURL As RootUrl
                , AF.supressOpenRangeImage As Suppress3rdPartyImages
	            , STUFF((SELECT ', ' + PI1.url FROM ngmd.productImage PI1
                    WHERE PI1.websiteInventoryFK = WI.websiteInventoryID AND PI1.mainImage = 1
                    GROUP BY PI1.url
                    FOR XML PATH(''), TYPE).value('.', 'varchar(max)')
		            , 1, 2, '') As PMSMainImage
	            , STUFF((SELECT ', ' + PML.url FROM ngmd.pim_mediaLinks PML
                    INNER JOIN ngmd.pim_products PP ON PP.prodID = PML.prodID
                    WHERE PP.manufacturer = M.manufacturerName AND PP.partno = P.partNo
                    GROUP BY PML.url
                    FOR XML PATH(''), TYPE).value('.', 'varchar(max)')
		            , 1, 2, '') As PIMMainImage
                , STUFF((SELECT ', ' + IC.Med FROM ngmd.IcImage IC
                    WHERE IC.ProductFk = P.productID
                    GROUP BY IC.Med
                    FOR XML PATH(''), TYPE).value('.', 'varchar(max)')
		            , 1, 2, '') As IcMainImage
	            , '/' + ngmd.GenerateProductURL(P.productName + '-' + P.partNo + '-' + AF.stockReference) AS [productUrl]
            FROM ngmd.product P
            INNER JOIN ngmd.manufacturer M ON M.manufacturerID = P.manufacturerFK
            INNER JOIN ngmd.websiteInventory WI ON WI.productFK = P.productID AND WI.websiteFK = " + SiteId + @"
            INNER JOIN ngmd.website W ON W.WebsiteID = WI.websiteFK
            LEFT OUTER JOIN ngmd.AxisFields AF ON AF.productFK = P.productID
            WHERE P.productStatusFK IN (1,8)
            AND M.manufacturerName NOT IN ('Own Brand', 'Misc')
            AND WI.websiteFK IS NOT NULL
            AND P.productItemTypeFK IN (1, 3)
            ORDER BY M.manufacturerName, P.productID";

            ds = SQLUtilities.ExecuteReadInline("netgiantmasterdata", sql, "img");
            dt = ds.Tables[0];

            IceCatImagesList = new List<Telerik>();
            Telerik t = new Telerik();
            foreach (DataRow dr in dt.Rows)
            {
                t = new Telerik();
                t.ProductId = Int32.Parse(dr["productID"].ToString());
                t.PartNumber = dr["partNo"].ToString().Trim();
                t.Manufacturer = dr["manufacturerName"].ToString().Trim();
                t.Description = dr["productName"].ToString().Trim();
                t.Suppress3rdPartyImages = string.IsNullOrEmpty(dr["Suppress3rdPartyImages"].ToString()) ? false : Convert.ToBoolean(dr["Suppress3rdPartyImages"].ToString());
                t.PMSMainImages = string.IsNullOrEmpty(dr["PMSMainImage"].ToString()) ? "NO IMAGE IN PMS" : BuildImageHtml(dr["PMSMainImage"].ToString(), t.PartNumber, dr["RootUrl"].ToString());
                t.PimMainImages = string.IsNullOrEmpty(dr["PimMainImage"].ToString()) ? "NO IMAGE IN OR" : BuildImageHtml(dr["PimMainImage"].ToString(), t.PartNumber);
                t.IcMainImages = string.IsNullOrEmpty(dr["IcMainImage"].ToString()) ? "NO IMAGE IN IceCat" : BuildImageHtml(dr["IcMainImage"].ToString(), t.PartNumber, dr["RootUrl"].ToString());

                IceCatImagesList.Add(t);
            }
        }

        private void GetCounts()
        {
            DataSet ds = new DataSet("id");
            DataTable dt = new DataTable();

            string sql = @"SELECT Count(*) AS ProductCount
	            FROM ngmd.product P
	            INNER JOIN ngmd.manufacturer M ON M.manufacturerID = P.manufacturerFK
                INNER JOIN ngmd.websiteInventory WI ON WI.productFK  = P.productID AND WI.websiteFK = " + SiteId + @"
	            WHERE P.productStatusFK IN (1,8)
	            AND M.manufacturerName NOT IN ('Own Brand', 'Misc')
                AND WI.websiteFK IS NOT NULL
                AND P.productItemTypeFK IN (1, 3)";

            ds = SQLUtilities.ExecuteReadInline("netgiantmasterdata", sql, "img");
            dt = ds.Tables[0];

            ActiveProductCount = Int32.Parse(dt.Rows[0]["ProductCount"].ToString());

            sql = @"SELECT Count(*) AS ProductCount
                FROM ngmd.product P
                INNER JOIN ngmd.manufacturer M ON M.manufacturerID = P.manufacturerFK
                INNER JOIN ngmd.websiteInventory WI ON WI.productFK  = P.productID AND WI.websiteFK = " + SiteId + @"
                INNER JOIN ngmd.pim_products PP ON PP.partno = P.partNo AND PP.manufacturer = M.manufacturerName
                WHERE P.productStatusFK IN (1,8)
                AND M.manufacturerName NOT IN ('Own Brand', 'Misc')
                AND WI.websiteFK IS NOT NULL
                AND P.productItemTypeFK IN (1, 3)";

            ds = SQLUtilities.ExecuteReadInline("netgiantmasterdata", sql, "img");
            dt = ds.Tables[0];

            ProductMatchCount = Int32.Parse(dt.Rows[0]["ProductCount"].ToString());

            sql = @"SELECT Count(*) AS ProductCount
                FROM ngmd.product P
                INNER JOIN ngmd.manufacturer M ON M.manufacturerID = P.manufacturerFK
                INNER JOIN ngmd.websiteInventory WI ON WI.productFK  = P.productID AND WI.websiteFK = " + SiteId + @"
                INNER JOIN ngmd.pim_products PP ON PP.partno = P.partNo AND PP.manufacturer = M.manufacturerName
                OUTER APPLY (SELECT TOP 1 url FROM ngmd.pim_mediaLinks PM WHERE PM.prodID = PP.prodID AND PM.[type] IN ('JPG','JPEG')) PML
                WHERE P.productStatusFK IN (1,8)
                AND M.manufacturerName NOT IN ('Own Brand', 'Misc')
                AND PML.url IS NOT NULL
                AND WI.websiteFK IS NOT NULL
                AND P.productItemTypeFK IN (1, 3)";

            ds = SQLUtilities.ExecuteReadInline("netgiantmasterdata", sql, "img");
            dt = ds.Tables[0];

            ImageMatchCount = Int32.Parse(dt.Rows[0]["ProductCount"].ToString());
        }

        private string BuildImageHtml(string imgString, string partNumber, string root = "")
        {
            string[] imgArray = imgString.Split(new string[] { ", " }, StringSplitOptions.None);

            StringBuilder sb = new StringBuilder();
            foreach (string img in imgArray)
            {
                if (!string.IsNullOrEmpty(img))
                {
                    string imgUrl = img;
                    if (root != "")
                    {
                        imgUrl = "https://" + root + "/cdn/" + img;
                    }
                    sb.Append("&nbsp;<img src=\"../../Content/images/noImage.jpg\" data-original=\"" + imgUrl + "\" height=\"60px\" class=\"lazy g-m-r-5 g-b-1dg\"  style=\"cursor: pointer;\" title=\"" + partNumber + "\" />");
                }
            }

            return sb.ToString();
        }

        private IQueryable<SelectListItem> GetWebsiteNames()
        {
            IQueryable<SelectListItem> query;

            using (ngmdEntities db = new ngmdEntities())
            {
                query = db.Website
                    .OrderBy(x => x.WebsiteName)
                    .Where(x => x.WebsiteName != "Intranet")
                    .Select(x => new SelectListItem
                    {
                        Value = x.WebsiteID.ToString(),
                        Text = x.FriendlyName,
                        Selected = x.WebsiteID == 1 ? true : false
                    }).ToList().AsQueryable();
            }
            return query;
        }

        public void CreateImageSpecCSVFile()
        {
            FilePath = LocalDirectory + "\\PMSTempData\\ImageSpecExport_" + DateTime.Now.ToString("dd_MM_yyyy_H_mm_ss") + ".csv";

            GetIceCatImages();
            using (CsvFileWriter writer = new CsvFileWriter(FilePath, ','))
            {
                SetColumnHeadings(writer);

                foreach (Telerik imageSpec in IceCatImagesList)
                {
                    InsertCSVData(writer, imageSpec);
                }
            }
        }

        private void InsertCSVData(CsvFileWriter writer, Telerik imageSpec)
        {
            CsvRow newRow = new CsvRow();
            newRow.Add(imageSpec.ProductId.ToString());
            newRow.Add(imageSpec.PartNumber);
            newRow.Add(imageSpec.Manufacturer);
            newRow.Add(imageSpec.Description);
            newRow.Add(imageSpec.PMSMainImages ?? "");
            newRow.Add(imageSpec.PimMainImages ?? "");
            newRow.Add(imageSpec.IcMainImages ?? "");

            writer.WriteRow(newRow);
        }
        private void SetColumnHeadings(CsvFileWriter writer)
        {
            CsvRow firstRow = new CsvRow();
            firstRow.Add("ProductId");
            firstRow.Add("PartNumber");
            firstRow.Add("Manufacturer");
            firstRow.Add("Description");
            firstRow.Add("PMS Images");
            firstRow.Add("Pimberly Images");
            firstRow.Add("IceCat Images");

            writer.WriteRow(firstRow);
        }

        public class Telerik
        {
            public int ProductId { get; set; }
            public string PartNumber { get; set; }
            public string Manufacturer { get; set; }
            public string Description { get; set; }
            public bool Suppress3rdPartyImages { get; set; }
            public string PMSMainImages { get; set; }
            public string PimMainImages { get; set; }
            public string IcMainImages { get; set; }
        }
    }
}

