using netGiant.Intranet.DataLayer;
using System.Linq;

namespace ngBatchProcesses.BusinessObjects.Shared
{
    public class ProductFunctions
    {
        enum ImageProvider
        {
            Stock,
            CNET,
            OpenRange
        }

        public static string GetProductImage(string partNo,
            int websiteID,
            bool suppressOpenRangeImage = false)
        {
            string imgURL = string.Empty;

            ProductImagesView img = GetImageFromDB(partNo, websiteID, suppressOpenRangeImage);

            if (img != null)
            {
                ImageProvider provider = GetImageProvider(img.IsOpenRange);

                switch (provider)
                {
                    case ImageProvider.Stock:

                        imgURL = "/" + img.URL;
                        break;

                    case ImageProvider.OpenRange:

                        imgURL = img.URL.Replace("http://", "https://");
                        break;
                }
            }
            else
            {
                imgURL = "/media/stock/noImageMedium.jpg";
            }

            return imgURL;
        }

        public static string GetProductImage(websiteInventory inventory)
        {
            string imgURL = string.Empty;

            ProductImagesView img = GetImageFromDB(inventory.product.partNo,
                inventory.websiteFK,
                inventory.product.AxisFields.supressOpenRangeImage ?? false);

            if (img != null)
            {
                ImageProvider provider = GetImageProvider(img.IsOpenRange);

                switch (provider)
                {
                    case ImageProvider.Stock:

                        imgURL = "/" + img.URL;
                        break;

                    case ImageProvider.OpenRange:

                        imgURL = img.URL.Replace("http://", "https://");
                        break;
                }
            }
            else
            {
                imgURL = "/media/stock/noImageMedium.jpg";
            }

            return imgURL;
        }

        private static ProductImagesView GetImageFromDB(string partNo,
            int websiteId,
            bool suppressOpenRangeImage)
        {
            ProductImagesView img = null;

            using (ngmdEntities db = new ngmdEntities())
            {
                IQueryable<ProductImagesView> query;

                if (!suppressOpenRangeImage)
                {
                    query = db.ProductImagesView
                              .Distinct()
                              .Where(x => x.partNo == partNo && x.websiteFK == websiteId);
                }
                else
                {
                    query = db.ProductImagesView
                              .Distinct()
                              .Where(x => x.partNo == partNo && x.websiteFK == websiteId && x.IsOpenRange == 0);
                }

                img = query.OrderBy(x => x.Main).FirstOrDefault();
            }

            return img;
        }

        private static ImageProvider GetImageProvider(int imgProviderId)
        {
            ImageProvider provider;

            switch (imgProviderId)
            {
                case 0:
                    provider = ImageProvider.Stock;
                    break;
                case 1:
                    provider = ImageProvider.OpenRange;
                    break;
                default:
                    provider = ImageProvider.OpenRange;
                    break;
            }

            return provider;
        }
    }
}
