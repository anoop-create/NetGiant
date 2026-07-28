using System;
using System.Linq;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using PagedList;
using System.Data.Entity;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance
{
    public class DataSupplierViewModel : CommonViewModel
    {
        public DataSupplierViewModel()
        {
            _ctx = new ngmdEntities();
        }

        public dataSupplier dataSupplier { get; set; }
        public IQueryable<TelerikOverrides> DataSupplierOverrides { get; set; }
        public dataSupplierOverride DataSupplierOverride { get; set; }

        private ngmdEntities _ctx;









        public class TelerikDataSupplier
        {
            public string Name { get; set; }
            public DateTime LastUpdate { get; set; }
            public int ID { get; set; }
        }

        public IQueryable<TelerikDataSupplier> DataSupplierList { get; set; }

        public DataSupplierViewModel GetDataSuppliers()
        {
            DataSupplierList = _ctx.dataSupplier
                .Select(x => new TelerikDataSupplier
                {
                    Name = x.dataSupplierName,
                    LastUpdate = x.dateLastUpdate,
                    ID = x.dataSupplierID
                })
                .AsQueryable();
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

