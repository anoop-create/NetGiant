using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System;
using System.Data.Entity;
using System.Linq;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Product
{
    public class ProductImageViewModel : CommonViewModel
    {
        public productImage productImage;

        private void UpdateProductImage()
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                db.Entry(productImage).State = EntityState.Modified;
                db.SaveChanges();
            }
        }

        private void AddNewProductImage()
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                db.Entry(productImage).State = EntityState.Added;
                db.SaveChanges();
            }
        }

        public void SaveProductImage()
        {
            try
            {
                if (productImage.productImageID > 0)
                {
                    UpdateProductImage();
                }
                else
                {
                    AddNewProductImage();
                }
            }
            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }
        }
        
        public static productImage CreateProductImage(int webInvId, int id)
        //public static ProductImageViewModel CreateProductImage(int webInvId, int id)
        {
            //ProductImageViewModel model = new ProductImageViewModel();
            productImage model = new productImage();

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    if (id == 0)
                    {
                        model = new productImage();
                        model.websiteInventoryFK = webInvId;
                    }
                    else
                    {
                        model = db.productImage
                            .Include("websiteInventory")
                            .Where(x => x.productImageID == id).First();
                    }
                }
            }

            catch (Exception e)
            {
                throw new ApplicationException(e.Message + e.StackTrace);
            }

            return model;
        }

        public bool DeleteProductImage(int productImageID)
        {
            bool success = true;

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    productImage productImage = db.productImage.Find(productImageID);
                    db.productImage.Remove(productImage);
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                success = false;
                throw new ApplicationException(e.Message + e.StackTrace);
            }

            return success;
        }
    }
}
