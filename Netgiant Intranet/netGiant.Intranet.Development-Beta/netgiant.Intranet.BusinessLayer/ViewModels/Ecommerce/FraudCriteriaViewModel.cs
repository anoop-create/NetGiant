using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace netGiant.Intranet.BusinessLayer.ViewModels.Ecommerce
{
    public class FraudCriteriaViewModel : CommonViewModel
    {
        private ngmdEntities _ctx;

        public FraudCriteriaViewModel()
        {
            _ctx = new ngmdEntities();
        }

        public FraudCriteria FraudCriteria { get; set; }
        public IQueryable<TelerikFraudCriteria> FraudCriteriaList { get; set; }

        public void GetFraudCriteria()
        {
            FraudCriteriaList = _ctx.FraudCriteria.Select(x => new TelerikFraudCriteria
            {
                Id = x.FraudCriteriaId,
                DateLastUpdated = x.DateLastUpdated,
                PostCode = x.PostCode
            })
            .AsQueryable();
        }

        public FraudCriteriaViewModel CreateFraudCriteria(int id)
        {
            if (id > 0)
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    FraudCriteria = db.FraudCriteria
                        .Where(x => x.FraudCriteriaId == id).FirstOrDefault();
                }
            }
            else
            {
                FraudCriteria = new FraudCriteria();
            }

            return this;
        }

        public bool SaveFraudCriteria()
        {
            bool success = true;
            FraudCriteria.DateLastUpdated = DateTime.Now;
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    if (FraudCriteria.FraudCriteriaId > 0)
                    {
                        db.Entry(FraudCriteria).State = EntityState.Modified;
                    }
                    else
                    {
                        //Check if a Seo Text already exists for specified criteria
                        db.Entry(FraudCriteria).State = EntityState.Added;
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

        public SaveReturn DeleteFraudCriteria(int id)
        {
            SaveReturn sr = new SaveReturn();

            try
            {
                if (id > 0)
                {
                    using (ngmdEntities db = new ngmdEntities())
                    {
                        FraudCriteria fc = db.FraudCriteria.Where(x => x.FraudCriteriaId == id).FirstOrDefault();
                        db.Entry(fc).State = EntityState.Deleted;
                        db.SaveChanges();
                        sr.IsSuccess = true;
                    }
                }
            }
            catch (Exception ex)
            {
                sr.IsSuccess = false;
                sr.Message = ex.Message;
            }

            return sr;
        }

        public class TelerikFraudCriteria
        {
            public int Id { get; set; }
            public DateTime DateLastUpdated { get; set; }
            public string PostCode { get; set; }
        }
    }
}
