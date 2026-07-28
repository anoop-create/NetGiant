using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance
{
    public class AxisValueLookupViewModel : CommonViewModel
    {
        private ngmdEntities _ctx;

        public AxisValueLookupViewModel()
        {
            _ctx = new ngmdEntities();
        }

        public IQueryable<TelerikAxisValueLookup> AxisValueLookupList { get; set; }
        public AxisValueLookup AxisValueLookup { get; set; }
        public IQueryable<SelectListItem> AttrNameList { get; set; }

        public AxisValueLookupViewModel Get()
        {
            AxisValueLookupList = _ctx.AxisValueLookup
                                      .Select(x => new TelerikAxisValueLookup
                                      {
                                          Id = x.axisValueLookupID,
                                          AxisTypeName = _ctx.AxisTypeName.Where(y => y.axisTypeNameID == x.axisTypeNameFK).FirstOrDefault().axisTypeName1,
                                          AttributeName = _ctx.AxisTypeLookup.Where(y => y.attrNameID == x.attrNameFK).FirstOrDefault().attrName,
                                          AttributeValueId = x.attrValueID,
                                          AttributeValue = x.attrValueDesc
                                      })
                                      .AsQueryable();
            return this;
        }

        public AxisValueLookupViewModel Create(int id)
        {
            if (id > 0)
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    AxisValueLookup = db.AxisValueLookup.Where(x => x.axisValueLookupID == id).FirstOrDefault();
                    AxisValueLookup.AttributeTypeNameDescription = "Attributes";
                    AxisValueLookup.axisTypeNameFK = 1;
                }
            }
            else
            {
                AxisValueLookup = new AxisValueLookup();
                AxisValueLookup.AttributeTypeNameDescription = "Attributes";
                AxisValueLookup.axisTypeNameFK = 1;
            }
            AttrNameList = GetAttrNames();

            return this;
        }

        private IQueryable<SelectListItem> GetAttrNames()
        {
            IQueryable<SelectListItem> query;

            using (ngmdEntities db = new ngmdEntities())
            {
                query = db.AxisTypeLookup.OrderBy(x => x.attrName).Select(x => new SelectListItem
                {
                    Value = x.attrNameID.ToString(),
                    Text = x.attrName.ToString()
                }).ToList().AsQueryable();
            }
            return query;
        }

        public void Save()
        {
            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    if (AxisValueLookup.axisValueLookupID > 0)
                    {
                        db.Entry(AxisValueLookup).State = EntityState.Modified;
                    }
                    else
                    {
                        db.Entry(AxisValueLookup).State = EntityState.Added;
                    }

                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException(ex.Message + ex.StackTrace);
            }
        }

        public SaveReturn Delete(int id)
        {
            var sr = new SaveReturn();
            try
            {
                if (id > 0)
                {
                    using (ngmdEntities db = new ngmdEntities())
                    {
                        AxisValueLookup eb = db.AxisValueLookup.Where(x => x.axisValueLookupID == id).FirstOrDefault();
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

    public class TelerikAxisValueLookup
    {
        public int Id { get; set; }
        public string AxisTypeName { get; set; }
        public string AttributeName { get; set; }
        public int AttributeValueId { get; set; }
        public string AttributeValue { get; set; }
    }
}
