using System;
using System.Linq;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using PagedList;
using System.Data.Entity;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance
{
    public class SalesAreaGroupViewModel : CommonViewModel
    {
        private ngmdEntities _ctx;

        public SalesAreaGroupViewModel()
        {
            _ctx = new ngmdEntities();
        }

        public IQueryable<TelerikSalesAreaGroup> SalesAreaGroupList { get; set; }
        public salesAreaGroup salesAreaGroupSingle { get; set; }

        public SalesAreaGroupViewModel Get()
        {
            SalesAreaGroupList = _ctx.salesAreaGroup
                                   .Select(x => new TelerikSalesAreaGroup
                                   {
                                       Id = x.salesAreaGroupID,
                                       Name = x.salesAreaGroupName,
                                       SalesAreaGroupNo = x.salesAreaGroupNo,
                                       DateLastUpdated = x.dateLastUpdate
                                   })
                                   .AsQueryable();
            return this;
        }

        public SalesAreaGroupViewModel Create(int id)
        {
            using (ngmdEntities db = new ngmdEntities())
            {

                if (id > 0)
                {
                    salesAreaGroupSingle = db.salesAreaGroup.Find(id);
                }
                else
                {
                    salesAreaGroupSingle = new salesAreaGroup();
                }
            }

            return this;
        }

        public bool Save(SalesAreaGroupViewModel sagVm)
        {
            bool success = true;

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    sagVm.salesAreaGroupSingle.dateLastUpdate = DateTime.Now;

                    if (sagVm.salesAreaGroupSingle.salesAreaGroupID > 0)
                    {
                        db.Entry(sagVm.salesAreaGroupSingle).State = System.Data.Entity.EntityState.Modified;
                    }
                    else
                    {
                        db.salesAreaGroup.Add(sagVm.salesAreaGroupSingle);
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

        public SaveReturn Delete(int id)
        {
            var sr = new SaveReturn();

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    salesAreaGroup saGrp = db.salesAreaGroup.Find(id);
                    db.salesAreaGroup.Remove(saGrp);
                    db.SaveChanges();
                    sr.IsSuccess = true;
                }
            }
            catch (Exception ex)
            {
                sr.IsSuccess = false;
                throw new ApplicationException(ex.Message + ex.StackTrace);
            }

            return sr;
        }
    }

    public class TelerikSalesAreaGroup
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string SalesAreaGroupNo { get; set; }
        public DateTime DateLastUpdated { get; set; }
    }
}
