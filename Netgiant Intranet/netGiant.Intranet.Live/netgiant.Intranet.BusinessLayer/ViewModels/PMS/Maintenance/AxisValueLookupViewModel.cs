using netGiant.Intranet.DataLayer;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance
{
    public class AxisValueLookupViewModel
    {
        public List<AxisValueLookup> AxisValueLookupList { get; set; }
        public int AxisValueLookupListCount { get; set; }
        public AxisValueLookup AxisValueLookup { get; set; }
        public IQueryable<SelectListItem> AttrNameList { get; set; }

        public AxisValueLookupViewModel GetAxisValueLookup()
        {
            return GetAxisValueLookup(null, null, null, null, 1);
        }

        public AxisValueLookupViewModel GetAxisValueLookup(string orderBy, string searchTerm, string searchBy, int? attributeID, int? block)
        {
            int blockSize = 50;
            int blockNumber = (block ?? 1);

            using (ngmdEntities db = new ngmdEntities())
            {
                IQueryable<AxisValueLookup> query = db.AxisValueLookup;

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    switch (searchBy)
                    {
                        case "valueLookupID":
                            query = query.Where(x => x.axisValueLookupID.ToString().Contains(searchTerm.Trim()));
                            break;
                        case "attrValueID":
                            query = query.Where(x => x.attrValueID.ToString().Contains(searchTerm.Trim().ToLower()));
                            break;
                        case "attrValueDescription":
                            query = query.Where(x => x.attrValueDesc.ToLower().Contains(searchTerm.Trim().ToLower()));
                            break;
                        default:
                            break;
                    }
                }

                if (attributeID != null && attributeID > 0)
                {
                    query = query.Where(x => x.attrNameFK == attributeID);
                }

                switch (orderBy)
                {
                    case "idAsc":
                        query = query.OrderBy(x => x.axisValueLookupID);
                        break;
                    case "idDesc":
                        query = query.OrderByDescending(x => x.axisValueLookupID);
                        break;
                    case "typeNameAsc":
                        query = query.OrderBy(x => x.axisTypeNameFK);
                        break;
                    case "typeNameDesc":
                        query = query.OrderByDescending(x => x.axisTypeNameFK);
                        break;
                    case "attrNameAsc":
                        query = query.OrderBy(x => x.attrNameFK);
                        break;
                    case "attrNameDesc":
                        query = query.OrderByDescending(x => x.attrNameFK);
                        break;
                    case "attrValueAsc":
                        query = query.OrderBy(x => x.attrValueID);
                        break;
                    case "attrValueDesc":
                        query = query.OrderByDescending(x => x.attrValueID);
                        break;
                    case "attrValueDescriptionAsc":
                        query = query.OrderBy(x => x.attrValueDesc);
                        break;
                    case "attrValueDescriptionDesc":
                        query = query.OrderByDescending(x => x.attrValueDesc);
                        break;
                    default:
                        query = query.OrderBy(x => x.axisValueLookupID);
                        break;
                }

                AxisValueLookupListCount = query.Count();
                AxisValueLookupList = query
                    .Skip((blockNumber - 1) * blockSize)
                    .Take(blockSize)
                    .ToList();
                AttrNameList = GetAttrNames();

                foreach (var record in AxisValueLookupList)
                {
                    record.AttributeTypeNameDescription = db.AxisTypeName.Where(x => x.axisTypeNameID == record.axisTypeNameFK).FirstOrDefault().axisTypeName1;
                    record.AttributeNameDescription = db.AxisTypeLookup.Where(x => x.attrNameID == record.attrNameFK).FirstOrDefault().attrName;
                }

            }
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

        public void Delete(int id)
        {
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
            }
            catch (Exception ex)
            {
                throw new ApplicationException(ex.Message + ex.StackTrace);
            }
        }
    }
}
