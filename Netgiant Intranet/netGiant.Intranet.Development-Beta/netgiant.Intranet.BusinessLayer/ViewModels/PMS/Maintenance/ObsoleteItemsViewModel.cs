using netGiant.Intranet.BusinessLayer.ViewModels.Shared;
using netGiant.Intranet.DataLayer.NetgiantMasterData;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace netGiant.Intranet.BusinessLayer.ViewModels.PMS.Maintenance
{
    public class ObsoleteItemsViewModel : CommonViewModel
    {
        private ngmdEntities _ctx;

        public ObsoleteItemsViewModel()
        {
            _ctx = new ngmdEntities();
        }

        public IQueryable<TelerikObsoleteItem> ObsoleteItemList { get; set; }
        public obsoleteItem ObsoleteItem { get; set; }
        public IQueryable<SelectListItem> WebsiteNameList { get; set; }

        public ObsoleteItemsViewModel GetObsoleteItemList()
        {
            ObsoleteItemList = _ctx.obsoleteItem
                                   .Select(x => new TelerikObsoleteItem
                                   {
                                       Id = x.obsoleteItemID,
                                       Website = x.Website.WebsiteName,
                                       StockRef = x.stockReference,
                                       Equipment = x.equipmentName,
                                       URL = x.URL
                                   })
                                   .AsQueryable();
            return this;
        }

        public ObsoleteItemsViewModel Create(int id)
        {
            if (id > 0)
            {
                using (var db = new ngmdEntities())
                {
                    ObsoleteItem = db.obsoleteItem
                                     .Where(w => w.obsoleteItemID == id)
                                     .FirstOrDefault();
                }
            }
            else
            {
                ObsoleteItem = new obsoleteItem();
            }
            GetSelectListData();
            return this;
        }

        public void GetSelectListData()
        {
            using (var db = new ngmdEntities())
            {
                var list = db.Website
                                    .OrderBy(x => x.WebsiteName)
                                    .Select(x => new SelectListItem
                                    {
                                        Value = x.WebsiteID.ToString(),
                                        Text = x.WebsiteName.ToString()
                                    })
                                    .ToList();

                list.Add(new SelectListItem
                {
                    Text = "All Websites",
                    Value = "0"
                });

                WebsiteNameList = list.AsQueryable();
            }
        }

        public bool Save()
        {
            bool success = true;
            try
            {
                using (var db = new ngmdEntities())
                {
                    if (ObsoleteItem.obsoleteItemID > 0)
                    {
                        db.Entry(ObsoleteItem).State = EntityState.Modified;
                    }
                    else
                    {
                        if (ObsoleteItem.websiteFK == 0)
                        {
                            int count = db.Website.Where(w => !w.WebURL.Contains("intranet")).Count();

                            while (count > 0)
                            {
                                var item = new obsoleteItem
                                {
                                    websiteFK = count,
                                    stockReference = ObsoleteItem.stockReference,
                                    equipmentName = ObsoleteItem.equipmentName,
                                    URL = ObsoleteItem.URL
                                };

                                count--;

                                if (CheckObsoleteItemExists(db, item)) continue;

                                db.Entry(item).State = EntityState.Added;
                            }
                        }
                        else
                        {
                            if (CheckObsoleteItemExists(db)) throw new Exception("Obsolete Item already exists.");

                            db.Entry(ObsoleteItem).State = EntityState.Added;
                        }
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

        private bool CheckObsoleteItemExists(ngmdEntities db, obsoleteItem item = null)
        {
            if (item == null) item = ObsoleteItem;

            var check = new obsoleteItem();

            check = db.obsoleteItem
                     .Where(w => w.websiteFK == item.websiteFK && w.stockReference == item.stockReference && w.equipmentName == item.equipmentName)
                     .FirstOrDefault();

            return check == null ? false : true;
        }

        public SaveReturn Delete(int id)
        {
            var sr = new SaveReturn();
            try
            {
                if (id > 0)
                {
                    using (var db = new ngmdEntities())
                    {
                        var item = db.obsoleteItem.Where(x => x.obsoleteItemID == id).FirstOrDefault();
                        db.Entry(item).State = EntityState.Deleted;
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
    }

    public class TelerikObsoleteItem
    {
        public int Id { get; set; }
        public string Website { get; set; }
        public string StockRef { get; set; }
        public string Equipment { get; set; }
        public string URL { get; set; }

    }
}