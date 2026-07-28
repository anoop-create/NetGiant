using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using netGiant.Intranet.DataLayer;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using System.Web.Mvc;
using PagedList;
using System.Data.Entity;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance
{
    public class DataSupplierViewModel
    {
        public DataSupplierViewModel()
        {
            _ctx = new ngmdEntities();
        }

        public IPagedList<dataSupplier> dataSupplierList { get; set; }
        public dataSupplier dataSupplier { get; set; }
        public IQueryable<TelerikOverrides> DataSupplierOverrides { get; set; }
        public dataSupplierOverride DataSupplierOverride { get; set; }

        private ngmdEntities _ctx;

        public DataSupplierViewModel Get(int? page, string searchTerm, string searchBy, string orderBy)
        {
            int pageSize = 21;
            int pageNumber = (page ?? 1);

            using (ngmdEntities db = new ngmdEntities())
            {
                IQueryable<dataSupplier> list = db.dataSupplier.OrderBy(x => x.dataSupplierName);

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    list = list.Where(x => x.dataSupplierName.Contains(searchTerm));
                }

                switch (orderBy)
                {
                    case "dataSupplierNameAsc":
                        list = list.OrderBy(x => x.dataSupplierName);
                        break;
                    case "dataSupplierNameDesc":
                        list = list.OrderByDescending(x => x.dataSupplierName);
                        break;
                    case "dateLastUpdateAsc":
                        list = list.OrderBy(x => x.dateLastUpdate);
                        break;
                    case "dateLastUpdateDesc":
                        list = list.OrderByDescending(x => x.dateLastUpdate);
                        break;
                    default:
                        list = list.OrderBy(x => x.dataSupplierName);
                        break;
                }

                dataSupplierList = list.ToPagedList(pageNumber, pageSize);
            }

            return this;
        }

        public DataSupplierViewModel Create(int id)
        {
            using (ngmdEntities db = new ngmdEntities())
            {

                if (id > 0)
                {
                    dataSupplier = db.dataSupplier.Find(id);
                }
                else
                {
                    dataSupplier = new dataSupplier();
                }
            }

            return this;
        }

        public bool Save(DataSupplierViewModel dsVm)
        {
            bool success = true;

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    dsVm.dataSupplier.dateLastUpdate = DateTime.Now;

                    if (dsVm.dataSupplier.dataSupplierID > 0)
                    {
                        db.Entry(dsVm.dataSupplier).State = System.Data.Entity.EntityState.Modified;
                    }
                    else
                    {
                        db.dataSupplier.Add(dsVm.dataSupplier);
                    }

                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                success = false;
                throw new ApplicationException(ex.Message + ex.StackTrace);
            }

            return success;
        }

        public bool Delete(int id)
        {
            bool success = true;

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    dataSupplier ds = db.dataSupplier.Find(id);
                    db.dataSupplier.Remove(ds);
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                success = false;
                throw new ApplicationException(ex.Message + ex.StackTrace);
            }

            return success;
        }

        public void GetOverrides()
        {
            DataSupplierOverrides = _ctx.dataSupplierOverride.AsQueryable().AsTelerikViewModel();
        }

        public DataSupplierViewModel CreateOverride(int id)
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                if (id > 0)
                {
                    DataSupplierOverride = db.dataSupplierOverride.Find(id);
                }
                else
                {
                    DataSupplierOverride = new dataSupplierOverride();
                }
            }

            return this;
        }

        public bool SaveOverrideEntry()
        {
            bool success = true;
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    if (DataSupplierOverride.dataSupplierOverrideId > 0)
                    {
                        db.Entry(DataSupplierOverride).State = EntityState.Modified;
                    }
                    else
                    {
                        db.Entry(DataSupplierOverride).State = EntityState.Added;
                    }

                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                success = false;
                throw new ApplicationException(ex.Message + ex.StackTrace);
            }

            return success;
        }

        public SaveReturn SetDeletedFlag(int id, bool deleted)
        {
            var saveReturn = new SaveReturn();

            try
            {
                using (var db = new ngmdEntities())
                {
                    var dsoEntry = db.dataSupplierOverride.Find(id);
                    db.Entry(dsoEntry).State = EntityState.Deleted;
                    db.SaveChanges();
                }

                saveReturn.IsSuccess = true;
            }
            catch (Exception e)
            {
                saveReturn.IsSuccess = false;
                saveReturn.Message = e.Message;
            }

            return saveReturn;
        }

        public class TelerikOverrides
        {
            public int DataSupplierOverrideId { get; set; }
            public string DataSupplierName { get; set; }
            public string AttributeName { get; set; }
            public int OverrideType { get; set; }
        }
    }

    public static class DataSupplierExtensions
    {
        public static IQueryable<DataSupplierViewModel.TelerikOverrides> AsTelerikViewModel(this IQueryable<dataSupplierOverride> query)
        {
            return query.Select(o => new DataSupplierViewModel.TelerikOverrides
            {
                DataSupplierOverrideId = o.dataSupplierOverrideId,
                DataSupplierName = o.dataSupplier.dataSupplierName,
                AttributeName = o.attributeName,
                OverrideType = o.overrideType
            });
        }
    }
}

