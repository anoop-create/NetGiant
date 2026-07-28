using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System;
using System.Linq;

namespace ngBatchProcesses.BusinessObjects.Shared
{
    public class ProductFunctions
    {
        enum ImageProvider
        {
            Stock,
            ThirdParty
        }

        public static string GetProductImage(Website w, product p, string vn, string cloudflareSize = "XL")
        {
            string imgURL = string.Empty;
            ProductImagesView img = null;

            using (ngmdEntities db = new ngmdEntities())
            {
                IQueryable<ProductImagesView> query;

                if (!p.AxisFields.supressOpenRangeImage ?? false)
                {
                    query = db.ProductImagesView
                              .Distinct()
                              .Where(x => x.partNo == p.partNo && x.websiteFK == w.WebsiteID);
                }
                else
                {
                    query = db.ProductImagesView
                              .Distinct()
                              .Where(x => x.partNo == p.partNo && x.websiteFK == w.WebsiteID && x.DataSource == 0);
                }

                if (p.manufacturer.manufacturerName == "HP")
                {
                    img = query.OrderByDescending(x => x.DataSource).ThenByDescending(x => x.Main).ThenBy(x => x.Thumbnail).FirstOrDefault();
                }
                else
                {
                    img = query.OrderByDescending(x => x.Main).ThenBy(x => x.Thumbnail).FirstOrDefault();
                }
            }

            imgURL = img != null ? img.URL : "";
            imgURL = GenerateImageURL(w, imgURL, vn, cloudflareSize);

            return imgURL;
        }

        public static string GenerateImageURL(Website w, string imgURL, string vn, string cloudflareSize)
        {
            using (ngmdEntities db = new ngmdEntities())
            { 
                string sqlQuery = "SELECT ngmd.GenerateImageURL(" + w.WebsiteID + ", '" + imgURL + "', '" + vn + "', 'https://" + w.WebURL + "/', '" + cloudflareSize + "')";
                return db.Database.SqlQuery<string>(sqlQuery).FirstOrDefault();
            }
        }

        //private static string GetImageFromDB(Website w, product p, string vn, string cloudflareSize)
        //{
        //    ProductImagesView img = null;
        //    string i = "";

        //    using (ngmdEntities db = new ngmdEntities())
        //    {
        //        IQueryable<ProductImagesView> query;

        //        if (!p.AxisFields.supressOpenRangeImage ?? false)
        //        {
        //            query = db.ProductImagesView
        //                      .Distinct()
        //                      .Where(x => x.partNo == p.partNo && x.websiteFK == w.WebsiteID);
        //        }
        //        else
        //        {
        //            query = db.ProductImagesView
        //                      .Distinct()
        //                      .Where(x => x.partNo == p.partNo && x.websiteFK == w.WebsiteID && x.DataSource == 0);
        //        }

        //        if (p.manufacturer.manufacturerName == "HP")
        //        {
        //            img = query.OrderByDescending(x => x.DataSource).ThenByDescending(x => x.Main).ThenBy(x => x.Thumbnail).FirstOrDefault();
        //        }
        //        else
        //        {
        //            img = query.OrderByDescending(x => x.Main).ThenBy(x => x.Thumbnail).FirstOrDefault();
        //        }

        //        i = img != null ? img.URL : "";
        //        string sqlQuery = "SELECT ngmd.GenerateImageURL(" + w.WebsiteID + ", '" + i + "', '" + vn + "', 'https://" + w.WebURL + "/', '" + cloudflareSize + "')";
        //        i = db.Database.SqlQuery<string>(sqlQuery).FirstOrDefault();
        //    }

        //    return i;
        //}

        private static ImageProvider GetImageProvider(int imgProviderId)
        {
            ImageProvider provider;

            switch (imgProviderId)
            {
                case 0:
                    provider = ImageProvider.Stock;
                    break;
                default:
                    provider = ImageProvider.ThirdParty;
                    break;
            }

            return provider;
        }
    }
}
