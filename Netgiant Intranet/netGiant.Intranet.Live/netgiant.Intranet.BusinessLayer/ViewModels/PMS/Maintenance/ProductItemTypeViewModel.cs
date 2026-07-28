using netGiant.Intranet.DataLayer;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance
{
    public class ProductItemTypeViewModel
    {
        public List<productItemType> ProductItemTypeList { get; set; }
        public int ProductItemTypeListCount { get; set; }
        public productItemType ProductItemType { get; set; }

        public ProductItemTypeViewModel GetProductItemType()
        {
            return GetProductItemType(null, null, null, 1);
        }

        public ProductItemTypeViewModel GetProductItemType(string orderBy, string searchTerm, string searchBy, int? block)
        {
            int blockSize = 50;
            int blockNumber = (block ?? 1);

            using (ngmdEntities db = new ngmdEntities())
            {
                IQueryable<productItemType> query = db.productItemType;

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    switch (searchBy)
                    {
                        case "id":
                            query = query.Where(x => x.productItemTypeID.ToString().Contains(searchTerm.Trim()));
                            break;
                        case "name":
                            query = query.Where(x => x.productItemTypeName.ToLower().Contains(searchTerm.Trim().ToLower()));
                            break;
                        default:
                            break;
                    }
                }

                switch (orderBy)
                {
                    case "idAsc":
                        query = query.OrderBy(x => x.productItemTypeID);
                        break;
                    case "idDesc":
                        query = query.OrderByDescending(x => x.productItemTypeID);
                        break;
                    case "nameAsc":
                        query = query.OrderBy(x => x.productItemTypeName);
                        break;
                    case "nameDesc":
                        query = query.OrderByDescending(x => x.productItemTypeName);
                        break;
                    default:
                        query = query.OrderBy(x => x.productItemTypeID);
                        break;
                }

                ProductItemTypeListCount = query.Count();
                ProductItemTypeList = query
                    .Skip((blockNumber - 1) * blockSize)
                    .Take(blockSize)
                    .ToList();
            }
            return this;
        }

        public ProductItemTypeViewModel Create(int id)
        {
            if (id > 0)
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    ProductItemType = db.productItemType.Where(x => x.productItemTypeID == id).FirstOrDefault();
                }
            }
            else
            {
                ProductItemType = new productItemType();
            }
            return this;
        }

        public void Save()
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    if (ProductItemType.productItemTypeID > 0)
                    {
                        db.Entry(ProductItemType).State = EntityState.Modified;
                    }
                    else
                    {
                        db.Entry(ProductItemType).State = EntityState.Added;
                    }

                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException(ex.Message + ex.StackTrace);
            }
        }

        public void Delete(int id)
        {
            try
            {
                if (id > 0)
                {
                    using (ngmdEntities db = new ngmdEntities())
                    {
                        productItemType pit = db.productItemType.Where(x => x.productItemTypeID == id).FirstOrDefault();
                        db.Entry(pit).State = EntityState.Deleted;
                        db.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException(ex.Message + ex.StackTrace);
            }
        }
    }
}
