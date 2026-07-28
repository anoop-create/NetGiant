using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System;
using System.Data.Entity;
using System.Linq;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance
{
    public class AxisEBusinessViewModel : CommonViewModel
    {
        private ngmdEntities _ctx;

        public AxisEBusinessViewModel()
        {
            _ctx = new ngmdEntities();
        }

        public IQueryable<TelerikAxisEBusiness> EbusinessGroupList { get; set; }
        public AxisEbusiness AxisEBusinessGroup { get; set; }

        public AxisEBusinessViewModel Get()
        {
            EbusinessGroupList = _ctx.AxisEbusiness
                                     .Select(x => new TelerikAxisEBusiness
                                     {
                                         Id = x.AxisEbusinessID,
                                         Reference = x.eBusinessRef,
                                         Code = x.eBusinessCode,
                                         AffiliateCommissionGroup = x.AffiliateCommissionGroup,
                                         Description = x.description
                                     })
                                     .AsQueryable();
            return this;
        }

        public AxisEBusinessViewModel Create(int id)
        {
            if (id > 0)
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    AxisEBusinessGroup = db.AxisEbusiness.Where(x => x.AxisEbusinessID == id).FirstOrDefault();
                }
            }
            else
            {
                AxisEBusinessGroup = new AxisEbusiness();
            }

            return this;
        }

        public void Save()
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    if (AxisEBusinessGroup.AxisEbusinessID > 0)
                    {
                        db.Entry(AxisEBusinessGroup).State = EntityState.Modified;
                    }
                    else
                    {
                        db.Entry(AxisEBusinessGroup).State = EntityState.Added;
                    }

                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException(ex.Message + ex.StackTrace);
            }
        }

        public SaveReturn Delete(int EBusinessGroupID)
        {
            var sr = new SaveReturn();
            try
            {
                if (EBusinessGroupID > 0)
                {
                    using (ngmdEntities db = new ngmdEntities())
                    {
                        AxisEbusiness eb = db.AxisEbusiness.Where(x => x.AxisEbusinessID == EBusinessGroupID).FirstOrDefault();
                        db.Entry(eb).State = EntityState.Deleted;
                        db.SaveChanges();
                    }
                }
                sr.IsSuccess = true;
            }
            catch (Exception ex)
            {
                sr.IsSuccess = false;
                sr.Message = ex.Message;
            }
            return sr;
        }
    }

    public class TelerikAxisEBusiness
    {
        public int Id { get; set; }
        public string Reference { get; set; }
        public string Code { get; set; }
        public string AffiliateCommissionGroup { get; set; }
        public string Description { get; set; }
    }
}
