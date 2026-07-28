using System;
using System.Linq;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using PagedList;
using System.Web.Mvc;
using netGiant.Intranet.BusinessLayer.ViewModels.Shared;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance
{
    public class ProductGroupViewModel : CommonViewModel
    {
        private ngmdEntities _ctx;

        public ProductGroupViewModel()
        {
            _ctx = new ngmdEntities();
        }

        public IQueryable<TelerikProductGroup> ProductGroupList { get; set; }
        public productGroup productGroupSingle { get; set; }
        public IQueryable<SelectListItem> AllProductTypes { get; set; }

        public ProductGroupViewModel Get()
        {
            ProductGroupList = _ctx.productGroup
                                   .Select(x => new TelerikProductGroup
                                   {
                                       Id = x.productGroupID,
                                       Name = x.productGroupName,
                                       ProductGroupNo = x.productGroupNo,
                                       ProductType = (_ctx.Lookup
                                            .Where(y => y.LookupType.LookupTypeName == "ProductType" && y.AltLookupId == x.productTypeFK)
                                            .AsQueryable()
                                            .FirstOrDefault()
                                            .LookupName),
                                       DateLastUpdated = x.dateLastUpdate
                                   })
                                   .AsQueryable();
            return this;
        }        

        public ProductGroupViewModel Create(int id)
        {
            using (ngmdEntities db = new ngmdEntities())
            {

                if (id > 0)
                {
                    productGroupSingle = db.productGroup.Find(id);
                }
                else
                {
                    productGroupSingle = new productGroup();
                }

                AllProductTypes = SelectListViewModel.GetNgmdLookupSelectList("ProductType");
            }

            return this;
        }

        public bool Save(ProductGroupViewModel pgVm)
        {
            bool success = true;

            try
            {
                using (ngmdEntities db = new ngmdEntities())
                {
                    pgVm.productGroupSingle.dateLastUpdate = DateTime.Now;

                    if (pgVm.productGroupSingle.productGroupID > 0)
                    {
                        db.Entry(pgVm.productGroupSingle).State = System.Data.Entity.EntityState.Modified;
                    }
                    else
                    {
                        db.productGroup.Add(pgVm.productGroupSingle);
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
                    productGroup pdGrp = db.productGroup.Find(id);
                    db.productGroup.Remove(pdGrp);
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

    public class TelerikProductGroup
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ProductGroupNo { get; set; }
        public string ProductType { get; set; }
        public DateTime DateLastUpdated { get; set; }
    }
}
