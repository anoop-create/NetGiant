using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Import.ProductImages
{
    public class SaveRecords
    {
        public void UpdateWebsiteInventoryProductImage(List<ProductImagesImportFields> prodImages, websiteInventory wi)
        {
            if (prodImages == null)
                return;

            using (ngmdEntities db = new ngmdEntities())
            {
                var deletionCandidateURLs = new List<string>();
                db.productImage
                    .Where(x => x.websiteInventoryFK == wi.websiteInventoryID)
                    .ToList()
                    .ForEach(x => deletionCandidateURLs.Add(x.URL));

                var csvURLs = new List<string>();
                prodImages.ToList().ForEach(x => csvURLs.Add(x.URL));

                try
                {
                    foreach (var prod in prodImages)
                    {
                        if (prod.ACDModifier == "D")
                        {
                            productImage prdImage = db.productImage.Where(x => x.URL == prod.URL && x.websiteInventoryFK == wi.websiteInventoryID).FirstOrDefault();
                            db.productImage.Remove(prdImage);
                            db.SaveChanges();
                        }
                        if (prod.ACDModifier == "C")
                        {
                            productImage prdImage = db.productImage
                                .Where(x => x.URL.ToUpper().Equals(prod.URL.ToUpper()) && x.websiteInventoryFK == wi.websiteInventoryID).FirstOrDefault();
                            if (prdImage != null &&
                                (prdImage.thumbnailImage != prod.isThumbnail) || (prdImage.mainImage != prod.isMain))
                            {
                                prdImage.thumbnailImage = prod.isThumbnail;
                                prdImage.mainImage = prod.isMain;
                                db.Entry(prdImage).State = EntityState.Modified;
                                db.SaveChanges();
                            }
                        }
                        if (prod.ACDModifier == "A")
                        {
                            productImage prdImage = new productImage();
                            prdImage.websiteInventoryFK = wi.websiteInventoryID;
                            prdImage.URL = prod.URL;
                            prdImage.mainImage = prod.isMain;
                            prdImage.thumbnailImage = prod.isThumbnail;
                            db.productImage.Add(prdImage);
                            db.SaveChanges();
                        }
                    }
                }
                catch (Exception e)
                {
                    throw new ApplicationException(e.Message + e.StackTrace);
                }
            }
        }

    }
}
