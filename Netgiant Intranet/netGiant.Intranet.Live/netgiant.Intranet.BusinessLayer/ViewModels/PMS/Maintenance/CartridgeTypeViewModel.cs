using netGiant.Intranet.DataLayer;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance
{
    public class CartridgeTypeViewModel
    {
        public List<eqCartridgeType> CartridgeTypeList { get; set; }
        public int CartridgeTypeListCount { get; set; }
        public eqCartridgeType CartridgeType { get; set; }

        public CartridgeTypeViewModel GetCartridgeType()
        {
            return GetCartridgeType(null, null, null, 1);
        }

        public CartridgeTypeViewModel GetCartridgeType(string orderBy, string searchTerm, string searchBy, int? block)
        {
            int blockSize = 50;
            int blockNumber = (block ?? 1);

            using (ngmdEntities db = new ngmdEntities())
            {
                IQueryable<eqCartridgeType> query = db.eqCartridgeType;

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    switch (searchBy)
                    {
                        case "id":
                            query = query.Where(x => x.eqCartridgeTypeID.ToString().Contains(searchTerm.Trim()));
                            break;
                        case "name":
                            query = query.Where(x => x.eqCartridgeTypeName.ToLower().Contains(searchTerm.Trim().ToLower()));
                            break;
                        default:
                            break;
                    }
                }

                switch (orderBy)
                {
                    case "idAsc":
                        query = query.OrderBy(x => x.eqCartridgeTypeID);
                        break;
                    case "idDesc":
                        query = query.OrderByDescending(x => x.eqCartridgeTypeID);
                        break;
                    case "nameAsc":
                        query = query.OrderBy(x => x.eqCartridgeTypeName);
                        break;
                    case "nameDesc":
                        query = query.OrderByDescending(x => x.eqCartridgeTypeName);
                        break;
                    default:
                        query = query.OrderBy(x => x.eqCartridgeTypeID);
                        break;
                }

                CartridgeTypeListCount = query.Count();
                CartridgeTypeList = query
                    .Skip((blockNumber - 1) * blockSize)
                    .Take(blockSize)
                    .ToList();
            }
            return this;
        }

        public CartridgeTypeViewModel Create(int id)
        {
            if (id > 0)
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    CartridgeType = db.eqCartridgeType.Where(x => x.ID == id).FirstOrDefault();
                }
            }
            else
            {
                CartridgeType = new eqCartridgeType();
            }

            return this;
        }

        public void Save()
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    if (CartridgeType.ID > 0)
                    {
                        db.Entry(CartridgeType).State = EntityState.Modified;
                    }
                    else
                    {
                        db.Entry(CartridgeType).State = EntityState.Added;
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
                        eqCartridgeType eb = db.eqCartridgeType.Where(x => x.ID == id).FirstOrDefault();
                        db.Entry(eb).State = EntityState.Deleted;
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
